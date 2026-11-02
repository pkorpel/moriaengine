using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1001 - Sphere's SPELL_Telekin: Use_Obj without the touch test, a double click from where you stand.</summary>
	[ViewableClass]
	public class TelekinesisSpellDef : SpellDef {
		public TelekinesisSpellDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override void On_EffectChar(Character target, SpellEffectArgs spellEffectArgs) {
			target.Trigger_DClickFromAfar(spellEffectArgs.Caster);
		}

		protected override void On_EffectItem(Item target, SpellEffectArgs spellEffectArgs) {
			target.Trigger_DClickFromAfar(spellEffectArgs.Caster);
		}
	}
}
