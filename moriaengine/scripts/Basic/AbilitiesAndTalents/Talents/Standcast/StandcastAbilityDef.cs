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

namespace SteamEngine.CompiledScripts {

	/// <summary>#483/#491, #1386 - Erebor's standcast command (systems/specabilities.scp): one command switches it on and
	/// off, for mages, necromancers and shamans by their profession alone (no ability points, as Erebor's). Not while the
	/// energy shield is up, nor while a looter's pick holds the caster. Its work is StandcastPlugin's.</summary>
	public class StandcastAbilityDef : ActivableAbilityDef {
		private static ActivableAbilityDef energyShieldDef;
		internal static ActivableAbilityDef EnergyShieldDef {
			get {
				if (energyShieldDef == null) {
					energyShieldDef = (ActivableAbilityDef) AbilityDef.GetByDefname("a_energy_shield");
				}
				return energyShieldDef;
			}
		}

		private static StandcastAbilityDef standcastDef;
		private static StandcastAbilityDef StandcastDef {
			get {
				if (standcastDef == null) {
					standcastDef = (StandcastAbilityDef) AbilityDef.GetByDefname("a_standcast");
				}
				return standcastDef;
			}
		}

		public StandcastAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		private static bool ShieldIsUp(Character ch) {
			return EnergyShieldPlugin.IsUp(ch); //#1401 - Erebor's tag.energyofshield: not while it rests
		}

		/// <summary>The standcast command.</summary>
		public void Toggle(Character ch) {
			if (ShieldIsUp(ch)) {
				ch.SysMessage("Jsi prilis " + (ch.IsFemale ? "vycerpana" : "vycerpany") + " udrzovanim stitu.");
				return;
			}
			if (!CorpseLooting.CheckCanMove(ch)) { //i_looter_immobilizer
				return;
			}
			if (this.IsActive(ch)) {
				this.Deactivate(ch); //a freeze already running runs out, as Erebor's only drops the events
				ch.SysMessage("Tve usili polevilo.", ClassAbilities.ClassMessageHue);
				return;
			}
			ProfessionDef profession = (ch as Player)?.Profession;
			if ((profession == Professions.Mage) || (profession == Professions.Necromant) || (profession == Professions.Shaman)) {
				this.SwitchOn(ch);
			} else {
				ch.SysMessage("Tato funkce neni urcena pro tebe.", ClassAbilities.ClassMessageHue);
			}
		}

		private void SwitchOn(Character ch) {
			ch.AddPlugin(this.PluginKey, this.PluginDef.Create());
			ch.SysMessage("Nyni budes kouzlit s vetsim usilim.", ClassAbilities.ClassMessageHue);
		}

		/// <summary>A shield going up ends the standcast (energyshield, voodooshield).</summary>
		public static void SwitchOff(Character ch, string message) {
			StandcastAbilityDef def = StandcastDef;
			if ((def != null) && def.IsActive(ch)) {
				def.Deactivate(ch);
				ch.SysMessage(message, ClassAbilities.ClassMessageHue);
			}
		}

		protected override void On_DenyActivate(DenyAbilityArgs args) {
			if (ShieldIsUp(args.abiliter)) {
				args.Result = DenyResultMessages_Abilities.Deny_NotAllowedToHaveThisAbility;
				return;
			}
			base.On_DenyActivate(args);
		}

		/// <summary>The ability system's way in (a GM's abilities dialog): Erebor's own line instead of the generic
		/// "ability activated".</summary>
		protected override void On_Activate(Character chr, Ability ab) {
			this.SwitchOn(chr);
		}
	}
}
