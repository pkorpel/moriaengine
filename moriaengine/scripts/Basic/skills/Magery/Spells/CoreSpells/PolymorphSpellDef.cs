using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1001 - Sphere's SPELL_Polymorph, on the caster alone: the form's body, and its Str and Dex within -50 to +150
	/// of yours, for 5 to 8 minutes. A new form ends the old one first. The form is picked in PolymorphMenu. #1424 - the
	/// form is its plugin, p_polymorph: it times out, ends at death and gives everything back as it goes.</summary>
	[ViewableClass]
	public class PolymorphSpellDef : DurableCharEffectSpellDef {
		private const int MostLost = -50, MostGained = 150;

		public PolymorphSpellDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		private static PolymorphSpellDef Instance {
			get {
				return (PolymorphSpellDef) SpellDef.GetByDefname("s_polymorph");
			}
		}

		/// <summary>The running form, from a spell or a potion, or null.</summary>
		private static PolymorphEffectPlugin Running(Character target) {
			PolymorphSpellDef def = Instance;
			return (target.GetPlugin(def.EffectPluginKey_Spell) ?? target.GetPlugin(def.EffectPluginKey_Potion)) as PolymorphEffectPlugin;
		}

		internal static bool IsPolymorphed(Character target) {
			return Running(target) != null;
		}

		protected override void On_EffectChar(Character target, SpellEffectArgs spellEffectArgs) {
			NPCDef form = PolymorphMenu.TakeChoice(target);
			if (form == null) {
				return;
			}
			End(target); //the old form, under either key
			base.On_EffectChar(target, spellEffectArgs);
			Running(target)?.Become(form);
		}

		internal static int ApplyStat(short current, int ofForm, Action<short> set) {
			if (ofForm <= 0) { //a form with none set leaves it alone
				return 0;
			}
			int change = Math.Clamp(ofForm - current, MostLost, MostGained);
			set((short) (current + change));
			return change;
		}

		internal static void End(Character target) {
			PolymorphSpellDef def = Instance;
			target.DeletePlugin(def.EffectPluginKey_Spell);
			target.DeletePlugin(def.EffectPluginKey_Potion);
		}
	}

	/// <summary>#1424 - a polymorph's form: the body it replaced and what it did to Str and Dex, all given back as it ends
	/// (its time, a death, a new form). Silent and not dispellable, as before.</summary>
	[ViewableClass]
	public partial class PolymorphEffectPlugin {
		internal void Become(NPCDef form) {
			Character self = (Character) this.Cont;
			this.oldModel = self.Model;
			self.Model = form.Model;
			this.strChange = PolymorphSpellDef.ApplyStat(self.Str, form.RollStr(), value => self.Str = value);
			this.dexChange = PolymorphSpellDef.ApplyStat(self.Dex, form.RollDex(), value => self.Dex = value);
		}

		public override void On_UnAssign(Character cont) {
			if (this.oldModel != 0) {
				cont.Model = this.oldModel;
				cont.Str = (short) (cont.Str - this.strChange);
				cont.Dex = (short) (cont.Dex - this.dexChange);
				cont.Hits = (short) Math.Min(cont.Hits, cont.MaxHits);
				cont.Stam = (short) Math.Min(cont.Stam, cont.MaxStam);
			}
		}

		public override void On_Dispell(SpellEffectArgs spell) {
		}
	}

	[ViewableClass]
	public partial class PolymorphEffectPluginDef {
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
