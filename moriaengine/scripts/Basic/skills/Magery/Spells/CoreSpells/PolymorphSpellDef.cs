using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1001 - Sphere's SPELL_Polymorph, on the caster alone: the form's body, and its Str and Dex within -50 to +150
	/// of yours, for 5 to 8 minutes. A new form ends the old one first. The form is picked in PolymorphMenu.</summary>
	[ViewableClass]
	public class PolymorphSpellDef : DurableSpellDef {
		private const int MostLost = -50, MostGained = 150;
		private static readonly TimerKey timerKey = TimerKey.Acquire("_polymorph_");
		private static readonly TagKey modelTK = TagKey.Acquire("_polymorphModel_");
		private static readonly TagKey strTK = TagKey.Acquire("_polymorphStr_");
		private static readonly TagKey dexTK = TagKey.Acquire("_polymorphDex_");

		public PolymorphSpellDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		internal static bool IsPolymorphed(Character target) {
			return target.HasTag(modelTK);
		}

		protected override void On_EffectChar(Character target, SpellEffectArgs spellEffectArgs) {
			base.On_EffectChar(target, spellEffectArgs);

			NPCDef form = PolymorphMenu.TakeChoice(target);
			if (form == null) {
				return;
			}
			End(target);
			target.SetTag(modelTK, target.Model);
			target.Model = form.Model;
			target.SetTag(strTK, ApplyStat(target.Str, form.RollStr(), value => target.Str = value));
			target.SetTag(dexTK, ApplyStat(target.Dex, form.RollDex(), value => target.Dex = value));
			target.AddTimer(timerKey, new PolymorphEndTimer()).DueInSeconds = this.GetDurationForValue(spellEffectArgs.SpellPower);
		}

		private static int ApplyStat(short current, int ofForm, Action<short> set) {
			if (ofForm <= 0) { //a form with none set leaves it alone
				return 0;
			}
			int change = Math.Clamp(ofForm - current, MostLost, MostGained);
			set((short) (current + change));
			return change;
		}

		internal static void End(Character target) {
			target.DeleteTimer(timerKey);
			if (target.HasTag(modelTK)) {
				target.Model = (int) target.GetTag(modelTK);
				target.Str = (short) (target.Str - (int) target.GetTag(strTK));
				target.Dex = (short) (target.Dex - (int) target.GetTag(dexTK));
				target.Hits = (short) Math.Min(target.Hits, target.MaxHits);
				target.Stam = (short) Math.Min(target.Stam, target.MaxStam);
				target.RemoveTag(modelTK);
				target.RemoveTag(strTK);
				target.RemoveTag(dexTK);
			}
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class PolymorphEndTimer : BoundTimer {
		[LoadingInitializer, DeepCopyImplementation]
		public PolymorphEndTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			PolymorphSpellDef.End((Character) cont);
		}
	}

	/// <summary>#1001 - Sphere's sm_polymorph, a gump or the picture menu, MenuStyles.</summary>
	internal static class PolymorphMenu {
		private const string PolymorphSpell = "s_polymorph";
		private static readonly TagKey choiceTK = TagKey.Acquire("_polymorphChoice_");

		internal static readonly (string icon, string label, string[] forms, int skill)[] rows = {
			("i_pet_man", "Man", new[] { "c_man" }, 400),
			("i_pet_woman", "Woman", new[] { "c_woman" }, 400),
			("i_pet_chicken", "Chicken", new[] { "c_chicken" }, 400),
			("i_pet_dog", "Dog", new[] { "c_dog" }, 600),
			("i_pet_snake_giant", "Giant Serpant", new[] { "c_serpent_giant" }, 600),
			("i_pet_daemon", "Daemon", new[] { "c_daemon" }, 850),
			("i_pet_dragon", "Dragon", new[] { "c_dragon_green", "c_dragon_red" }, 980),
		};

		internal static bool Intercept(SkillSequenceArgs mageryArgs, SpellDef spell) {
			if ((mageryArgs.Self is Player self) && (spell == SpellDef.GetByDefname(PolymorphSpell)) && (mageryArgs.Param2 == null)) {
				Item tool = (Item) mageryArgs.Tool;
				List<int> offered = new List<int>();
				List<PictureMenuRow> pictures = new List<PictureMenuRow>();
				for (int i = 0; i < rows.Length; i++) {
					if (self.GetSkill(SkillName.Magery) >= rows[i].skill) {
						offered.Add(i);
						pictures.Add(new PictureMenuRow(ThingDef.GetByDefname(rows[i].icon).Model, rows[i].label));
					}
				}
				MenuStyles.Show(self, SingletonScript<D_PolymorphMenu>.Instance, new DialogArgs(tool, spell), Loc<PolymorphMenuLoc>.Get(self.Language).Title, pictures,
					index => Chose(self, tool, spell, offered[index]),
					null);
				return true;
			}
			return false;
		}

		internal static void Chose(Player self, Item tool, SpellDef spell, int row) {
			if (tool.IsDeleted) {
				return;
			}
			string[] forms = rows[row].forms;
			NPCDef form = (NPCDef) ThingDef.GetByDefname(forms[Globals.dice.Next(forms.Length)]);
			self.SetTag(choiceTK, form);
			SkillSequenceArgs.Acquire(self, SkillName.Magery, null, null, tool, spell, form).PhaseSelect();
		}

		internal static NPCDef TakeChoice(Character self) {
			NPCDef form = self.GetTag(choiceTK) as NPCDef;
			self.RemoveTag(choiceTK);
			return form;
		}
	}

	public class D_PolymorphMenu : CompiledGumpDef {
		private const int Width = 240, RowHeight = 46;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Player self = (Player) sendTo;
			this.SetLocation(60, 60);
			this.AddBackground(0, 0, Width, 50 + (PolymorphMenu.rows.Length * RowHeight), 5054);
			this.AddLabel(14, 12, 0, Loc<PolymorphMenuLoc>.Get(self.Language).Title);
			int line = 0;
			for (int i = 0; i < PolymorphMenu.rows.Length; i++) {
				if (self.GetSkill(SkillName.Magery) >= PolymorphMenu.rows[i].skill) {
					int y = 40 + (line * RowHeight);
					this.AddButton(14, y + 8, 2103, 2104, i + 1, GumpButtonType.Reply, 0);
					if (ThingDef.GetByDefname(PolymorphMenu.rows[i].icon) is ItemDef icon) {
						this.AddItem(30, y, icon.Model);
					}
					this.AddLabel(84, y + 8, 0, PolymorphMenu.rows[i].label);
					line++;
				}
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			int index = (int) gr.PressedButton - 1;
			if ((index >= 0) && (index < PolymorphMenu.rows.Length)) {
				Player self = (Player) gi.Cont;
				if (self.GetSkill(SkillName.Magery) >= PolymorphMenu.rows[index].skill) {
					PolymorphMenu.Chose(self, (Item) args[0], (SpellDef) args[1], index);
				}
			}
		}
	}

	internal class PolymorphMenuLoc : CompiledLocStringCollection<PolymorphMenuLoc> {
		public string Title = "What do you want to polymorph into?";
	}
}
