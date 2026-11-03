using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1001 - Sphere's SPELL_Incognito: the name of your race and a skin picked at random, for 2 to 8 minutes.
	/// A second cast only runs the time anew. Sphere's cast on an item (clearing ATTR_IDENTIFIED) has no counterpart here.</summary>
	[ViewableClass]
	public class IncognitoSpellDef : DurableSpellDef {
		private const int SkinHueLow = 0x83ea, SkinHueHigh = 0x8422;
		internal static readonly TimerKey timerKey = TimerKey.Acquire("_incognito_");
		private static readonly TagKey nameTK = TagKey.Acquire("_incognitoName_");
		private static readonly TagKey colorTK = TagKey.Acquire("_incognitoColor_");

		public IncognitoSpellDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override void On_EffectChar(Character target, SpellEffectArgs spellEffectArgs) {
			base.On_EffectChar(target, spellEffectArgs);

			if (!target.HasTag(nameTK)) {
				target.SetTag(nameTK, target.Name);
				target.SetTag(colorTK, (int) target.Color);
				target.Name = target.Def.Name;
				if (target.IsHuman && !PolymorphSpellDef.IsPolymorphed(target)) {
					target.Color = (ushort) Globals.dice.Next(SkinHueLow, SkinHueHigh);
				}
			}
			target.DeleteTimer(timerKey);
			target.AddTimer(timerKey, new IncognitoEndTimer()).DueInSeconds = this.GetDurationForValue(spellEffectArgs.SpellPower);
		}

		internal static void End(Character target) {
			target.DeleteTimer(timerKey);
			if (target.HasTag(nameTK)) {
				target.Name = (string) target.GetTag(nameTK);
				if (!PolymorphSpellDef.IsPolymorphed(target)) {
					target.Color = (ushort) (int) target.GetTag(colorTK);
				}
				target.RemoveTag(nameTK);
				target.RemoveTag(colorTK);
			}
		}

		internal static bool HasIncognito(Character target) {
			return target.HasTag(nameTK);
		}

		/// <summary>#1070 - the name the incognito hides and gives back at its end; the thief's mask renames this one
		/// while it lasts (Erebor's i_rune_incognito).</summary>
		internal static string TrueName(Character target) {
			return target.GetTag(nameTK) as string;
		}

		internal static void SetTrueName(Character target, string name) {
			target.SetTag(nameTK, name);
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class IncognitoEndTimer : BoundTimer {
		[LoadingInitializer, DeepCopyImplementation]
		public IncognitoEndTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			IncognitoSpellDef.End((Character) cont);
		}
	}
}
