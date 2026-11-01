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

	[ViewableClass]
	public partial class CraftingProcessPlugin {
		private static PluginKey craftingProcessPK = PluginKey.Acquire("_craftingProcess_");
		private const int CraftMessageColor = 0x57;
		private const double SecondsBetweenItems = 0.1;

		private bool stopped;

		public static void UnInstallCraftingPlugin(Character self) {
			CraftingProcessPlugin p = self.GetPlugin(craftingProcessPK) as CraftingProcessPlugin;
			if (p != null) {
				p.Delete();
			}
		}

		public static SimpleQueue<CraftingSelection> GetCraftingQueue(Character self) {
			CraftingProcessPlugin p = self.GetPlugin(craftingProcessPK) as CraftingProcessPlugin;
			if (p != null) {
				return p.craftingOrder.SelectionQueue;
			}
			return null;
		}

		public static void StartCrafting(Character self, CraftingOrder crOrd) {
			PlayerHunger.HurtIfStarving(self);
			if (!WorkLight.CheckWithMessage(self)) {
				return;
			}
			Announce(self, crOrd);
			CraftingProcessPlugin p = InstallCraftingPlugin(self, crOrd);
			p.StartCrafting();
		}

		private static void Announce(Character self, CraftingOrder order) {
			CraftSkillsLoc texts = Loc<CraftSkillsLoc>.Get(self.Language);
			self.SysMessage(texts.MakingTheseThings, CraftMessageColor);
			SimpleQueue<CraftingSelection> queue = order.SelectionQueue;
			for (int i = queue.Count; i > 0; i--) { //round it goes, ending as it was
				CraftingSelection selection = queue.Dequeue();
				self.SysMessage(selection.ItemDef.Name, CraftMessageColor);
				queue.Enqueue(selection);
			}
		}

		private void StartCrafting() {
			SkillSequenceArgs ssa = SkillSequenceArgs.Acquire((Character) this.Cont, this.craftingOrder.CraftingSkill);

			CraftingSelection csl = this.craftingOrder.SelectionQueue.Peek();
			ssa.Param1 = (csl != null ? csl.ItemDef : null);  //either selected ItemDef or null (both is acceptable)

			ssa.PhaseSelect();
		}

		public void On_Timer() {
			this.StartCrafting();
		}

		/// <summary>#962 - Erebor's e_makestring: a blow, a spell or a skill stops what's queued, using an item
		/// stops it sooner. What's in the hands gets finished.</summary>
		public static void Stop(Character self, bool byUsingAnItem) {
			CraftingProcessPlugin pl = self.GetPlugin(craftingProcessPK) as CraftingProcessPlugin;
			if ((pl != null) && !pl.stopped) {
				pl.stopped = true;
				CraftSkillsLoc texts = Loc<CraftSkillsLoc>.Get(self.Language);
				self.RedMessage(byUsingAnItem ? texts.MakingCutShort : texts.MakingStopped);
				if (pl.Timer > 0) { //between two items, nothing's in the hands
					pl.Delete();
				}
			}
		}

		/// <summary>One attempt is over. Done with the count, or with a failure the same again, a moment later as Erebor's
		/// i_make_delay. A negative count goes on until the materials end it.</summary>
		internal static void MakeFinished(SkillSequenceArgs skillSeqArgs) {
			Character cont = skillSeqArgs.Self;
			CraftingProcessPlugin pl = (CraftingProcessPlugin) cont.GetPlugin(craftingProcessPK);
			if (pl == null) {
				return;
			}
			if (pl.stopped) {
				pl.Delete();
				return;
			}
			CraftSkillsLoc texts = Loc<CraftSkillsLoc>.Get(cont.Language);
			if (skillSeqArgs.Success) {
				CraftingSelection crSel = pl.craftingOrder.SelectionQueue.Peek();
				if (crSel.Count > 0) {
					crSel.Count -= 1;
					if (crSel.Count == 0) {
						pl.craftingOrder.SelectionQueue.Dequeue();
					}
				}
				if (pl.craftingOrder.SelectionQueue.Count == 0) {
					cont.SysMessage(texts.FinishCrafting, CraftMessageColor);
					pl.Delete();
					return;
				}
			} else {
				cont.SysMessage(texts.FailedTryAgain, CraftMessageColor);
			}
			pl.Timer = SecondsBetweenItems;
		}

		/// <summary>Erebor's makethis failing: what can't be made ends the whole queue, whatever follows in it.</summary>
		internal static void MakeImpossible(SkillSequenceArgs skillSeqArgs) {
			Character cont = skillSeqArgs.Self;
			CraftSkillsLoc texts = Loc<CraftSkillsLoc>.Get(cont.Language);
			cont.RedMessage(texts.NotEnoughResources);
			cont.RedMessage(string.Format(texts.CantMakeThat, ((ItemDef) skillSeqArgs.Param1).Name));
			cont.RedMessage(texts.MakingStopped);
			UnInstallCraftingPlugin(cont);
		}

		private static CraftingProcessPlugin InstallCraftingPlugin(Character self, CraftingOrder craftingOrder) {
			CraftingProcessPlugin p = self.GetPlugin(craftingProcessPK) as CraftingProcessPlugin;
			if (p == null) {
				p = (CraftingProcessPlugin) self.AddNewPlugin(craftingProcessPK, CraftingProcessPluginDef.instance);
			}
			p.craftingOrder = craftingOrder;
			return p;
		}
	}

	[ViewableClass]
	public partial class CraftingProcessPluginDef {
	}

	partial class CraftingProcessPluginDef {
		public static CraftingProcessPluginDef instance = (CraftingProcessPluginDef)
			new CraftingProcessPluginDef("p_craftingProcess", "C# scripts", -1).Register();
	}
}
