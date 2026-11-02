using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1001 - Sphere's SPELL_Create_Food: one of five foods on the spot.</summary>
	[ViewableClass]
	public class CreateFoodSpellDef : SpellDef {
		private static readonly string[] foods = { "i_bacon_slab", "i_sausages", "i_ham", "i_cake", "i_bread_loaf" };

		public CreateFoodSpellDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override void On_EffectGround(IPoint3D target, SpellEffectArgs spellEffectArgs) {
			Point4D point = target.TopPoint as Point4D ?? new Point4D(target.TopPoint, spellEffectArgs.Caster.M);
			ThingDef.GetByDefname(foods[Globals.dice.Next(foods.Length)]).Create(point.X, point.Y, point.Z, point.M);
		}
	}
}
