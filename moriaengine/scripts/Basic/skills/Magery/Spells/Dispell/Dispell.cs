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

using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	public class DispellDef : SpellDef {
		private static readonly TriggerKey dispellTK = TriggerKey.Acquire("dispell");

		private static ScriptHolder f_dispellEffect_char;
		private static ScriptHolder f_dispellEffect_item;

		public DispellDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override void On_EffectChar(Character target, SpellEffectArgs spellEffectArgs) {
			base.On_EffectChar(target, spellEffectArgs);
			this.Trigger_Dispell(target, spellEffectArgs);
		}

		protected override void On_EffectItem(Item target, SpellEffectArgs spellEffectArgs) {
			base.On_EffectItem(target, spellEffectArgs);
			this.Trigger_Dispell(target, spellEffectArgs);
		}

		private void Trigger_Dispell(Item target, SpellEffectArgs spellEffectArgs) {
			target.TryTrigger(dispellTK, spellEffectArgs.scriptArgs);
			target.On_Dispell(spellEffectArgs);
		}

		private void Trigger_Dispell(Character target, SpellEffectArgs spellEffectArgs) {
			target.TryTrigger(dispellTK, spellEffectArgs.scriptArgs);
			target.On_Dispell(spellEffectArgs);
		}

		public static ScriptHolder F_dispellEffect_char {
			get {
				if (f_dispellEffect_char == null) {
					f_dispellEffect_char = ScriptHolder.GetFunction("f_dispellEffect_char");
				}
				return f_dispellEffect_char;
			}
		}

		public static ScriptHolder F_dispellEffect_item {
			get {
				if (f_dispellEffect_item == null) {
					f_dispellEffect_item = ScriptHolder.GetFunction("f_dispellEffect_item");
				}
				return f_dispellEffect_item;
			}
		}

		public static void ShowDispellEffect(Thing thingBeingDispelled) {
			Character ch = thingBeingDispelled as Character;
			if (ch != null) {
				ShowDispellEffect(ch);
			} else {
				ShowDispellEffect((Item) thingBeingDispelled);
			}
		}

		public static void ShowDispellEffect(Character charBeingDispelled) {
			if ((charBeingDispelled != null) && !charBeingDispelled.IsDeleted) {
				F_dispellEffect_char.Run(charBeingDispelled);
			}
		}

		public static void ShowDispellEffect(Item itemBeingDispelled) {
			if ((itemBeingDispelled != null) && !itemBeingDispelled.IsDeleted) {
				F_dispellEffect_item.Run(itemBeingDispelled);
			}
		}
	}
}