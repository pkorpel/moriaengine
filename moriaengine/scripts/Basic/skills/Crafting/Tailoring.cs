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

using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	/// <summary>#962 - Erebor's i_sewing_kit: on cloth, a hide or leather it opens the menu, on clothes it repairs them.
	/// Its rolls are kept to the letter: the fail roll grows with Tailoring and a good repair takes magic thread, not thread.
	/// Clothes with magic properties have rules of their own, for when magic items exist.</summary>
	public class Targ_SewingKit : CompiledTargetDef {
		private const int LeastSkill = 400, FailRollOf = 1201;

		protected override TargetResult On_TargonThing(Player self, Thing targetted, object parameter) {
			Item item = targetted as Item;
			if (CraftingSkillDef.IsOfType(item, "t_cloth", "t_hide", "t_leather")) {
				CraftingSkillDef.OpenMenu(self, SkillName.Tailoring);
			} else if (CraftingSkillDef.IsOfType(item, "t_clothing") && (item is Destroyable)) {
				Repair(self, (Destroyable) item);
			}
			return TargetResult.Done;
		}

		private static void Repair(Player self, Destroyable clothes) {
			TailoringLoc texts = Loc<TailoringLoc>.Get(self.Language);
			int skill = self.GetSkill(SkillName.Tailoring);
			if (clothes.Durability >= clothes.MaxDurability) {
				self.SysMessage(string.Format(texts.NoNeedOfRepair, clothes.Name));
				return;
			}
			if (skill < LeastSkill) {
				self.SysMessage(texts.TooPoorToRepair);
				return;
			}
			IResourceListEntry missing;
			ResourcesList thread = ResourcesListParser.Parse("1 i_thread");
			if (!thread.HasResourcesPresent(self, ResourcesLocality.Backpack, out missing)) {
				return;
			}
			bool failed = Globals.dice.Next(FailRollOf) < skill;
			if (failed) {
				thread.ConsumeResourcesOnce(self, ResourcesLocality.Backpack, out missing);
			} else {
				ResourcesList magicThread = ResourcesListParser.Parse("1 i_magicka_nit");
				if (magicThread.HasResourcesPresent(self, ResourcesLocality.Backpack, out missing)) {
					magicThread.ConsumeResourcesOnce(self, ResourcesLocality.Backpack, out missing);
				}
			}
			int most = clothes.MaxDurability - 1;
			int now = failed ? (clothes.Durability - 1) : most;
			if ((most < 1) || (now < 1)) {
				self.Emote(string.Format(texts.Destroyed, clothes.Name));
				clothes.Delete();
				return;
			}
			clothes.MaxDurability = (ushort) most;
			clothes.Durability = (ushort) now;
			string text = failed ? (self.IsFemale ? texts.RepairSpoiltFemale : texts.RepairSpoiltMale) : (self.IsFemale ? texts.RepairedFemale : texts.RepairedMale);
			self.SysMessage(string.Format(text, clothes.Name));
		}
	}

	internal class TailoringLoc : CompiledLocStringCollection<TailoringLoc> {
		public string NoNeedOfRepair = "{0} nepotrebuje opravit.";
		public string TooPoorToRepair = "Se svou ubohou dovednosti nic neopravis.";
		public string Destroyed = "destroyed {0}";
		public string RepairSpoiltMale = "Zkazil jsi opravu {0}.";
		public string RepairSpoiltFemale = "Zkazila jsi opravu {0}.";
		public string RepairedMale = "Opravil jsi {0}.";
		public string RepairedFemale = "Opravila jsi {0}.";
	}

	[ViewableClass]
	public class TailoringSkillDef : CraftingSkillDef {

		public TailoringSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		//#962 - the one sound, on success only
		protected override void DoSuccess(SkillSequenceArgs skillSeqArgs, Item newItem) {
			skillSeqArgs.Self.Sound(0x248); //scissors and knitting
			base.DoSuccess(skillSeqArgs, newItem);
		}
	}
}

//502767	What would you like to make?
//502768	You create the item and put it in your backpack.
//502769	You create the item and put it at your feet.
//502770	Please select the cloth you would like to use.
//502771	You cannot reach that.
//502772	That cloth belongs to someone else.
//502773	Someone else is using that cloth.
//502774	There's not enough material on that.
//502775	That cloth belongs to someone else.
//502776	Someone else is using that cloth.
//502777	There's not enough of the right type of material on that.
//502778	That's not the proper material.
//502779	You do not have enough leather to make this item.
//502780	You don't have room for the item in your pack, so you stop working on it.
//502781	You don't have room for the item and leftovers in your pack, so you stop working on it.
//502782	You place the left-over cloth pieces into your backpack
//502783	You place the left-over cloth pieces into your backpack
//502784	Due to your exceptional skill, its quality is higher than average.
//502785	You were barely able to make this item.  It's quality is below average.
//502786	You create the item and put it in your backpack.
//502787	You create the item and put it at your feet.
//502788	You throw the useless pieces away.

//1044043	You failed to create the item, and some of your materials are lost.

//1044454	You don’t have a bolt of cloth.
//1044456	You don't have any ready cloth.
//1044463	You do not have sufficient leather to make that item.