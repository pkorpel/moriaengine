using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1001 - Sphere's SPELL_Incognito: the name of your race and a skin picked at random, for 2 to 8 minutes.
	/// A second cast only runs the time anew. Sphere's cast on an item (clearing ATTR_IDENTIFIED) has no counterpart here.
	/// #1424 - the disguise is its plugin, p_incognito: it times out, ends at death and gives the name back as it goes.</summary>
	[ViewableClass]
	public class IncognitoSpellDef : DurableCharEffectSpellDef {
		public IncognitoSpellDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		private static IncognitoSpellDef Instance {
			get {
				return (IncognitoSpellDef) SpellDef.GetByDefname("s_incognito");
			}
		}

		/// <summary>The running disguise, from a spell or a potion, or null.</summary>
		private static IncognitoEffectPlugin Running(Character target) {
			IncognitoSpellDef def = Instance;
			return (target.GetPlugin(def.EffectPluginKey_Spell) ?? target.GetPlugin(def.EffectPluginKey_Potion)) as IncognitoEffectPlugin;
		}

		protected override void On_EffectChar(Character target, SpellEffectArgs spellEffectArgs) {
			IncognitoEffectPlugin running = Running(target);
			if (running != null) { //a second cast: the same name and skin, the time anew
				running.Timer = this.GetDurationForValue(spellEffectArgs.SpellPower);
				return;
			}
			base.On_EffectChar(target, spellEffectArgs);
		}

		internal static void End(Character target) {
			IncognitoSpellDef def = Instance;
			target.DeletePlugin(def.EffectPluginKey_Spell);
			target.DeletePlugin(def.EffectPluginKey_Potion);
		}

		internal static bool HasIncognito(Character target) {
			return Running(target) != null;
		}

		/// <summary>#1070 - the name the incognito hides and gives back at its end; the thief's mask renames this one
		/// while it lasts (Erebor's i_rune_incognito).</summary>
		internal static string TrueName(Character target) {
			return Running(target)?.trueName;
		}

		internal static void SetTrueName(Character target, string name) {
			IncognitoEffectPlugin running = Running(target);
			if (running != null) {
				running.trueName = name;
			}
		}
	}

	/// <summary>#1424 - an incognito: the true name and skin it hides, given back as it ends (its time, a death). Silent and
	/// not dispellable, as before.</summary>
	[ViewableClass]
	public partial class IncognitoEffectPlugin {
		private const int SkinHueLow = 0x83ea, SkinHueHigh = 0x8422;

		public void On_Assign() {
			Character self = (Character) this.Cont;
			this.trueName = self.Name;
			this.color = self.Color;
			self.Name = self.Def.Name;
			if (self.IsHuman && !PolymorphSpellDef.IsPolymorphed(self)) {
				self.Color = (ushort) Globals.dice.Next(SkinHueLow, SkinHueHigh);
			}
		}

		public override void On_UnAssign(Character cont) {
			if (this.trueName != null) {
				cont.Name = this.trueName;
				if (!PolymorphSpellDef.IsPolymorphed(cont)) {
					cont.Color = (ushort) this.color;
				}
			}
		}

		public override void On_Dispell(SpellEffectArgs spell) {
		}

		/// <summary>.boosts doesn't name who disguised you.</summary>
		protected override bool ShowsSource {
			get {
				return false;
			}
		}
	}

	[ViewableClass]
	public partial class IncognitoEffectPluginDef {
	}
}
