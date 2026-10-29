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

using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// #808 - an item dropped on an NPC, after @ReceiveItem-style refusals (e_item_refuser, #807):
	/// Sphere 0.99z8's NPC_OnItemGive (EreborNext CChar.NpcGive.cs) as far as Erebor's content
	/// reaches it.
	///  - a GM: the item goes into the NPC's pack;
	///  - the NPC's owner/friend (Character.IsPetOf): a humanoid pet eats food it's hungry for
	///    ("Mmm. Thank you." / "No thank you."), otherwise carries the item in its pack if it can
	///    ("Tell me to drop this when you want it back." / "I'm too weak to carry that."); an animal
	///    pet eats food it's hungry for, anything else falls through to the refusal below;
	///  - everyone else is refused. Sphere first asks NPC_WantThisItem (DESIRES, food while hungry,
	///    vendor buys it), but on Erebor's content that's never true for an unowned NPC: DESIRES is
	///    only on mounts/elementals (not converted) and only pets track hunger. So gold on a banker
	///    is refused too (user decision on #808). The refusal runs the NPC's @NPCRefuseItem (Erebor's
	///    guards: "you can't bribe me"); unless that returns 1, the giver sees "They don't appear to
	///    want the item" over the NPC.
	/// A refused or eaten item bounces back to the giver (what's left of it).
	/// </summary>
	public static class NpcItemGive {
		private static readonly TriggerKey npcRefuseItemTK = TriggerKey.Acquire("npcRefuseItem");

		/// <summary>Continue = the item goes into the NPC's pack (TryPutItemOnChar's default path);
		/// Cancel = handled here, the item (if anything is left of it) goes back to the giver.</summary>
		public static TriggerResult OnItemGive(NPC npc, Character giver, AbstractItem item) {
			if (giver == null) {
				return TriggerResult.Continue;
			}
			if (giver.IsGM) {
				return TriggerResult.Continue;
			}

			Item asItem = item as Item;
			if (npc.IsPetOf(giver)) {
				bool isFood = (asItem != null) && PetFeedingSystem.IsFoodFor(npc, asItem);
				if (npc.IsHuman) {
					if (isFood) {
						npc.Say(PetFeedingSystem.TryFeedWith(npc, asItem) ? "Mmm. Thank you." : "No thank you.");
						return TriggerResult.Cancel;
					}
					if (MovementStamina.CarriedWeight(npc) + item.Weight > MovementStamina.MaxCarryWeight(npc.Str)) {
						npc.Say("I'm too weak to carry that.");
						return TriggerResult.Cancel;
					}
					if (item.Def == GoldUtils.GoldDef) {
						npc.Say("Thank thee.");
					}
					npc.Say("Tell me to drop this when you want it back.");
					return TriggerResult.Continue;
				}
				if (isFood && PetFeedingSystem.TryFeedWith(npc, asItem)) {
					return TriggerResult.Cancel;
				}
			}

			Refuse(npc, giver, item);
			return TriggerResult.Cancel;
		}

		private static void Refuse(NPC npc, Character giver, AbstractItem item) {
			if (npc.TryCancellableTrigger(npcRefuseItemTK, new ScriptArgs(giver, item)) == TriggerResult.Cancel) {
				return; //the NPC's own @NPCRefuseItem said its line
			}
			GameState state = giver.GameState;
			if (state != null) {
				PacketSequences.SendOverheadMessageFrom(state.Conn, npc, "They don't appear to want the item.", 0);
			}
		}
	}
}
