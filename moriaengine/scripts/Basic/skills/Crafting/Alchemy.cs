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
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public class AlchemySkillDef : CraftingSkillDef {

		public AlchemySkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		//do the "mixing" sound
		private const int FewPeople = 5, SectorSize = 64;

		//one stroke for each resource, then the last that only ends it
		public override int RollStrokes(ItemDef iDef) {
			return ((iDef.Resources == null) ? 0 : System.Linq.Enumerable.Count(iDef.Resources.Entries)) + 1;
		}

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			foreach (LayerNames hand in new[] { LayerNames.Hand1, LayerNames.Hand2 }) {
				Item held = self.FindLayer(hand) as Item;
				if ((held != null) && (held.Type != null) && (held.Type.Defname == "t_weapon_bow_run")) {
					held.Cont = self.Backpack;
				}
			}
			return base.On_Start(skillSeqArgs);
		}

		//Sphere's Skill_Alchemy: the reagents go in one by one as the strokes pass
		protected override bool DoStrokeWork(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			ItemDef potion = (ItemDef) skillSeqArgs.Param1;
			if (potion.Resources == null) {
				return true;
			}
			int stroke = this.RollStrokes(potion) - Convert.ToInt32(skillSeqArgs.Param2);
			IResourceListEntry reagent = System.Linq.Enumerable.ElementAtOrDefault(potion.Resources.Entries, stroke);
			if (reagent == null) {
				return true;
			}
			ItemResource item = reagent as ItemResource;
			string name = (item == null) ? reagent.ParsableString : item.ItemDef.Name;
			IResourceListEntry missing;
			CraftSkillsLoc texts = Loc<CraftSkillsLoc>.Get(self.Language);
			if (!self.IsGM && !ResourcesListParser.Parse(reagent.DesiredCount.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + reagent.ParsableString).ConsumeResourcesOnce(self, ResourcesLocality.Backpack, out missing)) {
				self.SysMessage(string.Format(texts.LackForPotion, name));
				return false;
			}
			if ((item != null) && (item.ItemDef.Type != null) && (item.ItemDef.Type.Defname == "t_reagent") && IsQuietSector(self)) {
				self.Emote(string.Format((stroke == 0) ? texts.StartGrinding : texts.ContinueGrinding, name));
			}
			self.Sound(0x242);
			return true;
		}

		private static bool IsQuietSector(Character self) {
			int left = self.X - (self.X % SectorSize), top = self.Y - (self.Y % SectorSize);
			int people = 0;
			foreach (Thing thing in self.GetMap().GetThingsInRectangle(new ImmutableRectangle(left, top, left + SectorSize - 1, top + SectorSize - 1))) {
				if (thing is Character) {
					people++;
				}
			}
			return people < FewPeople;
		}

		//the reagents are gone already, only the bottle is left to take
		protected override bool ConsumeMade(ItemDef iDef, Character self, out IResourceListEntry missing) {
			return ResourcesListParser.Parse("1 i_bottle_empty").ConsumeResourcesOnce(self, ResourcesLocality.Backpack, out missing);
		}

		protected override void ConsumeFailed(ItemDef iDef, Character self) {
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
			skillSeqArgs.Self.Emote(Loc<CraftSkillsLoc>.Get(skillSeqArgs.Self.Language).TossMixture);
			base.On_Fail(skillSeqArgs);
		}

		/// <summary>Erebor's potions_ lists: who may brew what, shown in the menu only. Any other item passes.</summary>
		internal static bool ClassBrews(ItemDef iDef, Character chr) {
			if (!(iDef is PotionDef)) {
				return true;
			}
			Constant list = Constant.GetByName("potions_" + iDef.PrettyDefname);
			string classes = (list == null) ? null : list.Value as string;
			return !string.IsNullOrEmpty(classes)
				&& Array.Exists(classes.Split(','), one => one.Trim() == "class_" + EreborClass(chr));
		}

		/// <summary>Erebor's short class name, as its lists use it.</summary>
		internal static string EreborClass(Character chr) {
			Player player = chr as Player;
			ProfessionDef profession = (player == null) ? null : player.Profession;
			if (profession == null) {
				return "";
			}
			string name = profession.PrettyDefname.Replace("profession_", "");
			switch (name) {
				case "mage": return "mag";
				case "necromant": return "necro";
				case "mystic": return "mystik";
				default: return name;
			}
		}

		//do the pouring into the flask sound in additional to the standard functionality
		protected override string FailMessage(Character self) {
			return Loc<CraftSkillsLoc>.Get(self.Language).FailedPotion;
		}
	}
}