using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	public partial class ReactiveArmorPlugin {
	}

	[ViewableClass]
	public partial class ReactiveArmorPluginDef {
		private static readonly PluginKey key = PluginKey.Acquire("spellEffect_reactive_armor");

		public static bool HasReactiveArmor(Character target) {
			return target.GetPlugin(key) is ReactiveArmorPlugin;
		}

		/// <summary>f_dmgFinal_applyReactiveAR's arithmetic: whole percents of the blow.</summary>
		public static void Calculate(int damage, int absorbPercent, int returnPercent, out int damageTaken, out int damageReturned) {
			damageTaken = damage - ((damage * absorbPercent) / 100);
			damageReturned = (damage * returnPercent) / 100;
		}
	}
}
