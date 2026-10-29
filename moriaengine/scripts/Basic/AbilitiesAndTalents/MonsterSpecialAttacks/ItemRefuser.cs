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
using System.Globalization;

using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// #807 e_item_refuser (119 NPCs - vendors and quest NPCs). Archive (systems/npc_ai.scp):
	/// on=@ReceiveItem it says "Hmm, &lt;item&gt; nepotrebuju. To si racej strcit za klobouk,
	/// vasnosto." and returns 1 - Sphere's "handled", so the NPC never takes the item and the
	/// giver keeps it. Here: NPC.On_PutItemOn cancels, and AbstractCharacter.TryPutItemOnChar
	/// then bounces the item back to the giver.</summary>
	[ViewableClass]
	public class ItemRefuserAbilityDef : PassiveAbilityDef {
		private FieldValue message;

		public ItemRefuserAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.message = this.InitTypedField("itemRefuserMessage", "Hmm, I don't need {0}. Stick it in your hat, your grace.", typeof(string));
		}

		/// <summary>What the NPC says when it turns an item down; {0} is the item's name.</summary>
		public string Message {
			get { return (string) this.message.CurrentValue; }
			set { this.message.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static ItemRefuserAbilityDef itemRefuserAbilityDef;
		internal static ItemRefuserAbilityDef ItemRefuserDef {
			get {
				if (itemRefuserAbilityDef == null) {
					itemRefuserAbilityDef = AbstractScript.GetByDefname("a_tevent_e_item_refuser") as ItemRefuserAbilityDef;
				}
				return itemRefuserAbilityDef;
			}
		}

		/// <summary>#807 e_item_refuser - NPC.On_PutItemOn, an item dropped on this NPC. Returns true
		/// when it refused (the caller cancels, so the item goes back to the giver).</summary>
		public static bool TryRefuseItem(NPC self, AbstractItem item) {
			ItemRefuserAbilityDef def = ItemRefuserDef;
			if (!HasAbilityDef(self, def)) {
				return false;
			}
			string message = def.Message;
			if (!string.IsNullOrEmpty(message)) {
				self.Say(string.Format(CultureInfo.InvariantCulture, message, item.Name));
			}
			return true;
		}
	}
}
