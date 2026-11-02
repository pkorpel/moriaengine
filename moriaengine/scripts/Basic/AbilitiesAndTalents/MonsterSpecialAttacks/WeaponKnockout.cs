using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1022 e_getwpn (npc_ai.scp): a blow that hurts a player knocks the weapon into their pack, one time in 3.</summary>
	[ViewableClass]
	public class WeaponKnockoutAbilityDef : PassiveAbilityDef {
		private FieldValue knockoutOneIn;

		public WeaponKnockoutAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.knockoutOneIn = this.InitTypedField("knockoutOneIn", 3, typeof(int));
		}

		public int KnockoutOneIn {
			get { return (int) this.knockoutOneIn.CurrentValue; }
			set { this.knockoutOneIn.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		internal static WeaponKnockoutAbilityDef WeaponKnockoutDef { get { return Lookup<WeaponKnockoutAbilityDef>("a_tevent_e_getwpn"); } }

		internal static void TryKnockWeaponOut(Character attacker, Character defender, int damage) {
			WeaponKnockoutAbilityDef def = WeaponKnockoutDef;
			if (HasAbilityDef(attacker, def) && (damage > 0) && (defender is Player) && (defender.Weapon != null) && OneIn(def.KnockoutOneIn)) {
				defender.DisArm();
				defender.SysMessage(defender.IsFemale ? "Dostala jsi strasnou ranu, pri ktere ti vypadla zbran z ruky." : "Dostal jsi strasnou ranu, pri ktere ti vypadla zbran z ruky.");
			}
		}
	}
}
