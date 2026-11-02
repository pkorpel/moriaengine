/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - Erebor's systems/mistri.scp. The craft master sells a craftsman lessons for what they made themselves,
	/// each thing once. The ship builders of Gondor and Mordor pay experience for logs, ore and ingots, and count ships.</summary>
	public static class CraftMasters {
		private const int LevelsThatCount = 56, Rounding = 5000, Scale = 10000, Halves = 50, Hundred = 100;
		private const int MithrilHandicap = 100;

		internal static int ConstantOf(string name) {
			Constant constant = SteamEngine.Constant.GetByName(name);
			return (constant == null) ? 0 : Convert.ToInt32(constant.Value, CultureInfo.InvariantCulture);
		}

		private static long SkillSum(ItemDef def) {
			long sum = 0;
			foreach (IResourceListEntry entry in def.SkillMake.Entries) {
				if ((entry is SkillResource skill) && (skill.DesiredCount > 1)) {
					sum += (long) skill.DesiredCount * ConstantOf("craftexp_skill_" + skill.SkillDef.Key.ToLowerInvariant());
				}
			}
			return sum;
		}

		private static long ResourceSum(ItemDef def) {
			long sum = 0;
			foreach (IResourceListEntry entry in def.Resources.Entries) {
				if (entry is ItemResource resource) {
					sum += (long) resource.DesiredCount * resource.ItemDef.ItemValue;
				}
			}
			return sum;
		}

		/// <summary>getitemexp: only what you made yourself and haven't had a lesson for.</summary>
		internal static long ExperienceFor(Character self, Item item) {
			ItemDef def = (ItemDef) item.Def;
			if ((CraftingSkillDef.GetCrafter(item) != self) && !(self.IsGM && (CraftingSkillDef.GetCrafter(item) != null))) {
				return 0;
			}
			if ((def.SkillMake == null) || (def.Resources == null) || item.Flag_Newbied) {
				return 0;
			}
			long value = ((SkillSum(def) + Halves) / Hundred) * ((ResourceSum(def) + Halves) / Hundred);
			value = ((value * CraftingSkillDef.GetQuality(item) * item.Amount) + Rounding) / Scale;
			return ((value * ConstantOf("craftexp_factor")) + Rounding) / Scale;
		}

		/// <summary>f_mistr_craft_computeGp.</summary>
		internal static long PriceOf(Player self, long experience) {
			return (experience * ConstantOf("craftexp_gold") * (Math.Min(self.Level, LevelsThatCount) + 1)) / 1000;
		}

		internal static void Sell(Player self, Item item, long experience) {
			if (GoldUtils.Pay(self, PriceOf(self, experience))) {
				LevelingSystem.AddExperience(self, (int) experience);
				CraftingSkillDef.SetCrafter(item, null);
			}
		}

		/// <summary>getitemexp_log.</summary>
		internal static long ExperienceForMaterial(Character self, Item item) {
			string defname = item.Def.PrettyDefname.ToLowerInvariant();
			Constant resource = SteamEngine.Constant.GetByName("materialskill_" + defname);
			if (resource == null) {
				return 0;
			}
			bool log = CraftingSkillDef.IsOfType(item, "t_log");
			int skill = self.GetSkill(log ? SkillName.Lumberjacking : SkillName.Mining);
			if (defname.EndsWith("_mithril", StringComparison.Ordinal)) {
				skill += MithrilHandicap;
			}
			return (skill >= Convert.ToInt32(resource.Value, CultureInfo.InvariantCulture)) ? item.Amount * ConstantOf("craftexp_" + defname) : 0;
		}
	}

	internal abstract class CraftMasterSpeech : AbstractSpeech {
		private static readonly string[] words = { "sell", "hi", "hello", "prod", "exp" };

		protected CraftMasterSpeech(string defname)
			: base(defname) {
		}

		protected virtual string[] Words {
			get {
				return words;
			}
		}

		protected override SpeechResult Handle(AbstractCharacter listener, SpeechArgs speechArgs) {
			return this.TryHandle(listener, speechArgs);
		}

		protected override SpeechResult TryHandle(AbstractCharacter listener, SpeechArgs speechArgs) {
			string speech = speechArgs.Speech;
			if ((listener is NPC master) && (speechArgs.Speaker is Player speaker) && (speech != null)
					&& Array.Exists(this.Words, word => speech.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)) {
				this.Answer(master, speaker);
			}
			return SpeechResult.IgnoredOrActedUpon;
		}

		protected abstract void Answer(NPC master, Player speaker);

		protected static bool MayUse(Player speaker) {
			return Professions.HasProfession(speaker, Professions.Craftsman) || speaker.IsGM;
		}
	}

	internal sealed class ItemMasterSpeech : CraftMasterSpeech {
		public new static void Bootstrap() {
			new ItemMasterSpeech("spk_mistr_craft").Register();
		}

		private ItemMasterSpeech(string defname)
			: base(defname) {
		}

		protected override void Answer(NPC master, Player speaker) {
			CraftMastersLoc texts = Loc<CraftMastersLoc>.Get(speaker.Language);
			if (MayUse(speaker)) {
				master.Say(texts.Lessons);
				speaker.Dialog(speaker, SingletonScript<D_CraftMaster>.Instance, new DialogArgs());
			} else {
				master.Say(texts.CantServe);
			}
		}
	}

	internal sealed class ShipBuilderSpeech : CraftMasterSpeech {
		private const int Reach = 6;
		private static readonly string[] words = { "sell", "hi", "hello", "prod", "exp", "ahoj", "zdravim", "odevzdat" };

		public new static void Bootstrap() {
			new ShipBuilderSpeech("spk_mistr_craft_log").Register();
		}

		private ShipBuilderSpeech(string defname)
			: base(defname) {
		}

		protected override string[] Words {
			get {
				return words;
			}
		}

		protected override void Answer(NPC master, Player speaker) {
			if (speaker.Flag_Dead) {
				return;
			}
			CraftMastersLoc texts = Loc<CraftMastersLoc>.Get(speaker.Language);
			bool gondor = master.Def.PrettyDefname.Equals("c_mistr_craft_log_gondor", StringComparison.OrdinalIgnoreCase);
			if (Point2D.GetSimpleDistance(master, speaker) > Reach) {
				speaker.SysMessage(texts.TooFarToDeal);
			} else if (!speaker.IsGM && (speaker.Realm == Realm.None)) {
				master.Say(gondor ? texts.OnlyGondor : texts.OnlyMordor);
			} else if (!speaker.IsGM && (speaker.Realm != (gondor ? Realm.Gondor : Realm.Mordor))) {
				master.Say(texts.Begone);
			} else if (!MayUse(speaker)) {
				master.Say(texts.CantServe);
			} else if (!speaker.IsGM && ActiveEffects.Of(speaker).Exists(effect => effect.Section == EffectSection.Positive)) {
				speaker.RedMessage(texts.NoMagic);
			} else {
				speaker.Dialog(speaker, SingletonScript<D_ShipBuilder>.Instance, new DialogArgs(master));
			}
		}
	}

	public class D_CraftMaster : CompiledGumpDef {
		private const int FirstRow = 40, RowHeight = 24, Width = 460, OkButton = 1, CheckBase = 100, MostRows = 18;

		internal static List<Item> ItemsOf(Player self) {
			List<Item> items = new List<Item>();
			foreach (AbstractItem held in self.Backpack) {
				if ((held is Item item) && (CraftMasters.ExperienceFor(self, item) > 0) && (items.Count < MostRows)) {
					items.Add(item);
				}
			}
			return items;
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Player viewer = (Player) sendTo;
			List<Item> items = ItemsOf(viewer);
			args.SetTag(itemsTK, items);
			this.SetLocation(40, 40);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, FirstRow + (items.Count * RowHeight) + 50, 2620);
			this.AddLabel(50, 12, 2301, "Nazev itemu");
			this.AddLabel(260, 12, 2301, "Cena za lekci");
			this.AddLabel(370, 12, 2301, "zkusenosti");
			for (int i = 0; i < items.Count; i++) {
				int y = FirstRow + (i * RowHeight);
				long experience = CraftMasters.ExperienceFor(viewer, items[i]);
				this.AddCheck(14, y, 9904, 9903, false, CheckBase + i);
				this.AddLabel(50, y, 2301, items[i].Name);
				this.AddLabel(260, y, 2301, CraftMasters.PriceOf(viewer, experience).ToString(CultureInfo.InvariantCulture));
				this.AddLabel(370, y, 2301, experience.ToString(CultureInfo.InvariantCulture));
			}
			this.AddButton(14, FirstRow + (items.Count * RowHeight) + 12, 0xfb7, 0xfb9, OkButton, GumpButtonType.Reply, 0);
		}

		private static readonly TagKey itemsTK = TagKey.Acquire("_craftMasterItems_");

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = (Player) gi.Cont;
			List<Item> items = (List<Item>) args.GetTag(itemsTK);
			if ((gr.PressedButton == OkButton) && (items != null)) {
				for (int i = 0; i < items.Count; i++) {
					if (gr.IsSwitched(CheckBase + i) && !items[i].IsDeleted) {
						long experience = CraftMasters.ExperienceFor(player, items[i]);
						if (experience > 0) {
							CraftMasters.Sell(player, items[i], experience);
						}
					}
				}
			}
		}
	}

	/// <summary>d_craftmistrlog with the ship count of d_craftmistrlog_gondor and _mordor. Their spoken pages aren't ported.</summary>
	public class D_ShipBuilder : CompiledGumpDef {
		private const int FirstRow = 70, RowHeight = 24, Width = 460, OkButton = 1, CheckBase = 100, MostRows = 18;
		private static readonly TagKey itemsTK = TagKey.Acquire("_shipBuilderItems_");

		private static TagKey TotalOf(string kind) {
			return TagKey.Acquire(kind + "Total");
		}

		private static int ShipsOf(NPC builder) {
			bool gondor = builder.Def.PrettyDefname.Equals("c_mistr_craft_log_gondor", StringComparison.OrdinalIgnoreCase);
			return Math.Min(Total(builder, "i_log_valorite") / (gondor ? 1500 : 1200),
				Math.Min(Total(builder, "i_log_obsidian") / (gondor ? 300 : 250), Total(builder, "i_log_adamantinum") / (gondor ? 50 : 40)));
		}

		private static int Total(NPC builder, string defname) {
			return (builder.GetTag(TotalOf(defname)) is int total) ? total : 0;
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Player viewer = (Player) sendTo;
			NPC builder = (NPC) args[0];
			CraftMastersLoc texts = Loc<CraftMastersLoc>.Get(viewer.Language);
			List<Item> items = new List<Item>();
			foreach (AbstractItem held in viewer.Backpack) {
				if ((held is Item item) && (CraftMasters.ExperienceForMaterial(viewer, item) > 0) && (items.Count < MostRows)) {
					items.Add(item);
				}
			}
			args.SetTag(itemsTK, items);
			this.SetLocation(40, 40);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, FirstRow + (items.Count * RowHeight) + 50, 2620);
			this.AddLabel(14, 12, 52, texts.ShipBuilder);
			this.AddLabel(14, 36, 2301, string.Format(texts.Ships, ShipsOf(builder)));
			for (int i = 0; i < items.Count; i++) {
				int y = FirstRow + (i * RowHeight);
				this.AddCheck(14, y, 9904, 9903, false, CheckBase + i);
				this.AddLabel(50, y, 2301, items[i].Name + " (" + items[i].Amount + " ks)");
				this.AddLabel(370, y, 2301, CraftMasters.ExperienceForMaterial(viewer, items[i]).ToString(CultureInfo.InvariantCulture));
			}
			this.AddButton(14, FirstRow + (items.Count * RowHeight) + 12, 0xfb7, 0xfb9, OkButton, GumpButtonType.Reply, 0);
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = (Player) gi.Cont;
			NPC builder = (NPC) args[0];
			List<Item> items = (List<Item>) args.GetTag(itemsTK);
			if ((gr.PressedButton == OkButton) && (items != null) && !builder.IsDeleted) {
				for (int i = 0; i < items.Count; i++) {
					Item item = items[i];
					if (gr.IsSwitched(CheckBase + i) && !item.IsDeleted && (item.TopObj() == player)) {
						long experience = CraftMasters.ExperienceForMaterial(player, item);
						if (experience > 0) {
							LevelingSystem.AddExperience(player, (int) experience);
							string defname = item.Def.PrettyDefname.ToLowerInvariant();
							builder.SetTag(TotalOf(defname), Total(builder, defname) + item.Amount);
							item.Delete();
						}
					}
				}
			}
		}
	}

	internal class CraftMastersLoc : CompiledLocStringCollection<CraftMastersLoc> {
		public string Lessons = "Poskytuji lekce na zaklade tvych zkusenosti s remeslnou vyrobou";
		public string CantServe = "Nemohu slouzit...";
		public string TooFarToDeal = "Na takovou vzdalenost s tebou jednat nebude";
		public string Begone = "Tahni!";
		public string OnlyGondor = "Sluzby poskytuju jen gondorum!";
		public string OnlyMordor = "Sluzby poskytuju jen mordorum!";
		public string NoMagic = "Nesmis byt pod zadnym magickym vlivem...";
		public string ShipBuilder = "Stavitel Lodi";
		public string Ships = "Do teto chvile bylo postaveno {0} lodi.";
	}
}
