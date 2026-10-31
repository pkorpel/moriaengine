using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {

	/// <summary>The potion of perception, systems/ZobrazovaniHP.scp: while it lasts a player sees hitpoints
	/// over those around being hit or healed, and over a character they click.</summary>
	public static class HpSight {
		private const int Range = 18;
		private static readonly TagKey untilTK = TagKey.Acquire("_hpsight_until_");
		private static readonly TagKey offTK = TagKey.Acquire("_hpsight_off_");
		private static readonly TagKey compactTK = TagKey.Acquire("_hpsight_compact_");
		private static readonly TagKey colorBlindTK = TagKey.Acquire("_hpsight_colorblind_");
		private static readonly TagKey byTargetTK = TagKey.Acquire("_hpsight_bytarget_");

		private static readonly int[] percents = { 95, 80, 65, 50, 40, 30, 20, 0 };
		private static readonly int[] colors = { 0x44, 0x47, 0xFE, 0x35, 0x32, 0x30, 0x27, 0x20 };
		private static readonly int[] blindColors = { 0x5A, 0x5A, 0x63, 0x35, 0x35, 0x142, 0x142, 0x20 };

		private sealed class Source : IActiveEffectSource {
			public void AddEffects(Character ch, List<ActiveEffect> into) {
				double left = SecondsLeft(ch);
				if (left > 0) {
					into.Add(new ActiveEffect(EffectSection.Positive, "Vnimani") { SecondsLeft = left, Detail = "Vidis zivoty ostatnich" });
				}
			}
		}

		private static readonly Source source = new Source();

		public static void Bootstrap() {
			ActiveEffects.Register(source);
		}

		internal static double SecondsLeft(Character ch) {
			return PotionDrinking.Time(ch, untilTK) - Globals.TimeInSeconds;
		}

		/// <summary>add_HPsight: drinking wants to see, and adds to what's left.</summary>
		public static void Add(Character drinker, double seconds) {
			double left = SecondsLeft(drinker);
			drinker.SetTag(untilTK, Globals.TimeInSeconds + System.Math.Max(left, 0) + seconds);
			drinker.RemoveTag(offTK);
		}

		internal static bool Is(Character ch, TagKey option) {
			return ch.HasTag(option);
		}

		internal static void Flip(Character ch, TagKey option) {
			if (ch.HasTag(option)) {
				ch.RemoveTag(option);
			} else {
				ch.SetTag(option, true);
			}
		}

		internal static TagKey Off { get { return offTK; } }
		internal static TagKey Compact { get { return compactTK; } }
		internal static TagKey ColorBlind { get { return colorBlindTK; } }
		internal static TagKey ByTarget { get { return byTargetTK; } }

		private static bool Sees(Character viewer) {
			return !viewer.HasTag(offTK) && ((SecondsLeft(viewer) > 0) || viewer.IsGM);
		}

		internal static int ColorOf(int percent, bool colorBlind) {
			for (int i = 0; i < percents.Length; i++) {
				if (percent >= percents[i]) {
					return colorBlind ? blindColors[i] : colors[i];
				}
			}
			return colors[colors.Length - 1];
		}

		private static string Percent(int part, int whole, bool compact) {
			if (compact) {
				return ((part * 100) / whole).ToString(CultureInfo.InvariantCulture);
			}
			return ((part * 100.0) / whole).ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',');
		}

		private static bool IsShown(Character target) {
			return !target.IsGM && !target.Flag_InvisByMagic && !target.Flag_Hidden && !target.Flag_Insubst && (target.MaxHits > 0);
		}

		private static void Send(Character viewer, Character target, int hits, string change) {
			GameState state = viewer.GameState;
			if (state == null) {
				return;
			}
			bool compact = viewer.HasTag(compactTK);
			string text = change + "[" + Percent(hits, target.MaxHits, compact) + "%]";
			int color = viewer.HasTag(byTargetTK) ? ((target == viewer) ? 0x35 : Notoriety.GetNameHue(target.GetHighlightColorFor(viewer)))
				: ColorOf((hits * 100) / target.MaxHits, viewer.HasTag(colorBlindTK));
			PacketSequences.SendNameFrom(state.Conn, target, text, color);
		}

		/// <summary>show_damage_and_hitpoints, for everyone around who sees it. Heal is already in target's hits.</summary>
		public static void ShowChange(Character target, int amount, bool isHeal) {
			if ((amount <= 0) || !IsShown(target) || (target.Hits <= 0) || target.IsDeleted) {
				return;
			}
			foreach (AbstractCharacter around in target.GetMap().GetPlayersInRange(target.X, target.Y, Range)) {
				if ((around is Character viewer) && Sees(viewer)) {
					string change = (isHeal ? "+" : "-") + Percent(amount, target.MaxHits, viewer.HasTag(compactTK)) + "% ";
					Send(viewer, target, target.Hits, change);
				}
			}
		}

		public static void ShowOnClick(AbstractCharacter clicker, Character target) {
			if ((clicker is Character viewer) && Sees(viewer) && !target.IsGM && (target.Hits > 0) && (target.MaxHits > 0)) {
				Send(viewer, target, target.Hits, "");
			}
		}
	}
}
