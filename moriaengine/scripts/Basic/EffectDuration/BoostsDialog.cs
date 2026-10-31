using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#923 - the .boosts page of the .settings dialog.</summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class BoostsSettings : SettingsMetaCategory {

		[SavedMember]
		public static BoostsSettings instance = new BoostsSettings();

		[LoadingInitializer]
		public BoostsSettings() {
		}

		/// <summary>Whether .boosts updates by itself for a player who hasn't chosen. Off until it's
		/// been tried on a real 3.0.6m client: see D_Boosts.</summary>
		[SaveableData]
		public bool liveByDefault = false;

		/// <summary>How often a live .boosts redraws for its countdowns. It always does at once when
		/// an effect starts or ends.</summary>
		[SaveableData]
		public double refreshSeconds = 5;
	}

	/// <summary>#923 - Erebor's .boosty. Most players run 3.0.6m clients, which can't redraw a dialog in
	/// place and don't tell where it was dragged to, so it opens where Pozice says, and live means closed
	/// and sent anew there.</summary>
	public class D_Boosts : CompiledGumpDef {
		private const int Width = 460, ListRowHeight = 48, TextRowHeight = 20, StripStep = 55, StripRow = 86, StripColumns = 10, ButtonsWidth = 110;
		private const int RefreshButton = 1, CompactButton = 2, PositionButton = 3, LiveButton = 4;
		private const int TitleHue = 0x481, TextHue = 2301, GoodHue = 67, BadHue = 33, CooldownHue = 52;

		private static readonly TagKey compactTK = TagKey.Acquire("_boosts_compact_");
		private static readonly TagKey liveTK = TagKey.Acquire("_boosts_live_");
		internal static readonly TagKey xTK = TagKey.Acquire("_boosts_x_");
		internal static readonly TagKey yTK = TagKey.Acquire("_boosts_y_");
		internal static readonly TimerKey liveTimerTK = TimerKey.Acquire("_boosts_live_timer_");

		public static void Toggle(Player viewer, Character target) {
			D_Boosts def = SingletonScript<D_Boosts>.Instance;
			if (viewer.HasOpenedDialog(def)) {
				viewer.DialogClose(def, 0);
			} else {
				Show(viewer, target);
			}
		}

		public static void Show(Player viewer, Character target) {
			viewer.Dialog(viewer, SingletonScript<D_Boosts>.Instance, new DialogArgs(target));
			viewer.DeleteTimer(liveTimerTK);
			if (IsLive(viewer)) {
				BoostsLiveTimer timer = new BoostsLiveTimer();
				timer.target = target;
				timer.lastShown = Signature(target);
				timer.nextRedraw = Globals.TimeInSeconds + BoostsSettings.instance.refreshSeconds;
				viewer.AddTimer(liveTimerTK, timer);
				timer.DueInSeconds = 1;
				timer.PeriodInSeconds = 1;
			}
		}

		internal static bool IsLive(Player viewer) {
			object chosen = viewer.GetTag(liveTK);
			return (chosen != null) ? (bool) chosen : BoostsSettings.instance.liveByDefault;
		}

		/// <summary>What's shown, without the time left.</summary>
		internal static string Signature(Character target) {
			List<string> lines = new List<string>();
			foreach (ActiveEffect effect in ActiveEffects.Of(target)) {
				lines.Add(effect.Name + "|" + effect.Detail);
			}
			return string.Join("\n", lines);
		}

		private static int Hue(EffectSection section) {
			switch (section) {
				case EffectSection.Positive:
					return GoodHue;
				case EffectSection.Negative:
					return BadHue;
				case EffectSection.Cooldown:
					return CooldownHue;
				default:
					return TextHue;
			}
		}

		private static string Title(EffectSection section) {
			switch (section) {
				case EffectSection.Positive:
					return "Pozitivni vlivy";
				case EffectSection.Negative:
					return "Negativni vlivy";
				case EffectSection.Neutral:
					return "Ostatni";
				case EffectSection.Cooldown:
					return "Cooldowny";
				default:
					return "Stav";
			}
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Player viewer = (Player) sendTo;
			Character target = (Character) args[0];
			List<ActiveEffect> effects = ActiveEffects.Of(target);
			this.SetLocation(TagMath.IGetTag(viewer, xTK), TagMath.IGetTag(viewer, yTK));
			this.AddPage(0);
			if (viewer.HasTag(compactTK)) {
				this.Strip(viewer, effects);
			} else {
				this.List(viewer, target, effects);
			}
		}

		private void Buttons(Player viewer, int x, int y, bool compact) {
			this.Line(x, y, RefreshButton, "Obnovit");
			this.Line(x, y + TextRowHeight, CompactButton, compact ? "Seznam" : "Kompaktni");
			this.Line(x, y + (2 * TextRowHeight), PositionButton, "Pozice");
			this.Line(x, y + (3 * TextRowHeight), LiveButton, "Zive: " + (IsLive(viewer) ? "ano" : "ne"));
		}

		private void Line(int x, int y, int button, string text) {
			this.AddButton(x, y, 0xfa5, 0xfa7, button, GumpButtonType.Reply, 0);
			this.AddLabel(x + 34, y, TitleHue, text);
		}

		private void List(Player viewer, Character target, List<ActiveEffect> effects) {
			int height = 110;
			EffectSection? seen = null;
			foreach (ActiveEffect effect in effects) {
				height += HasIcons(effect.Section) ? ListRowHeight : TextRowHeight;
				if (effect.Section != seen) {
					height += TextRowHeight + 6;
					seen = effect.Section;
				}
			}
			this.AddBackground(0, 0, Width, height + 80, 2620);
			this.AddLabel(14, 12, TitleHue, (target == viewer) ? "Boosty" : ("Boosty: " + target.Name));
			int y = 36;
			if (effects.Count == 0) {
				this.AddLabel(14, y, TextHue, "Nic na tebe nepusobi.");
				y += TextRowHeight;
			}
			seen = null;
			foreach (ActiveEffect effect in effects) {
				if (effect.Section != seen) {
					seen = effect.Section;
					y += 6;
					this.AddLabel(14, y, Hue(effect.Section), Title(effect.Section));
					y += TextRowHeight;
				}
				y = this.ListRow(effect, y);
			}
			this.Buttons(viewer, 14, y + 8, false);
		}

		private static bool HasIcons(EffectSection section) {
			return (section != EffectSection.Cooldown) && (section != EffectSection.Status);
		}

		private int ListRow(ActiveEffect effect, int y) {
			int hue = Hue(effect.Section);
			if (!HasIcons(effect.Section)) {
				this.AddLabel(14, y, hue, effect.Name);
				this.AddLabel(170, y, TextHue, (effect.Section == EffectSection.Status) ? effect.Detail : ActiveEffects.FormatTime(effect.SecondsLeft));
				return y + TextRowHeight;
			}
			if (effect.Icon != 0) {
				this.AddImage(14, y, effect.Icon);
			}
			this.AddLabel(66, y, hue, effect.Name);
			this.AddLabel(370, y, TextHue, ActiveEffects.FormatTime(effect.SecondsLeft));
			this.AddLabel(66, y + 20, TextHue, ActiveEffects.Join(effect.Detail, effect.Source));
			return y + ListRowHeight;
		}

		/// <summary>Erebor's strip, in rows when it's long, a coloured time under each icon.</summary>
		private void Strip(Player viewer, List<ActiveEffect> effects) {
			List<ActiveEffect> shown = effects.FindAll(effect => effect.Section != EffectSection.Status);
			int columns = System.Math.Min(System.Math.Max(shown.Count, 1), StripColumns);
			int rows = System.Math.Max((shown.Count + StripColumns - 1) / StripColumns, 1);
			this.AddBackground(0, 0, (columns * StripStep) + ButtonsWidth + 20, System.Math.Max(rows * StripRow, 100) + 10, 2620);
			for (int i = 0; i < shown.Count; i++) {
				ActiveEffect effect = shown[i];
				int x = 10 + ((i % StripColumns) * StripStep);
				int y = 8 + ((i / StripColumns) * StripRow);
				if (effect.Icon != 0) {
					this.AddImage(x, y, effect.Icon);
				} else {
					this.AddLabelCropped(x, y + 12, StripStep - 4, 20, Hue(effect.Section), effect.Name);
				}
				this.AddLabel(x + 2, y + 50, Hue(effect.Section), ActiveEffects.FormatTime(effect.SecondsLeft));
			}
			this.Buttons(viewer, 10 + (columns * StripStep), 8, true);
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player viewer = (Player) gi.Cont;
			Character target = (Character) args[0];
			switch (gr.PressedButton) {
				case RefreshButton:
					Show(viewer, target);
					break;
				case CompactButton:
					if (viewer.HasTag(compactTK)) {
						viewer.RemoveTag(compactTK);
					} else {
						viewer.SetTag(compactTK, true);
					}
					Show(viewer, target);
					break;
				case PositionButton:
					viewer.DeleteTimer(liveTimerTK);
					viewer.Dialog(viewer, SingletonScript<D_BoostsPosition>.Instance, new DialogArgs(target));
					break;
				case LiveButton:
					viewer.SetTag(liveTK, !IsLive(viewer));
					Show(viewer, target);
					break;
				default:
					viewer.DeleteTimer(liveTimerTK);
					break;
			}
		}
	}

	/// <summary>d_boostyxy: an old client doesn't say where a dialog was dragged to.</summary>
	public class D_BoostsPosition : CompiledGumpDef {
		private const int XEntry = 1, YEntry = 2, OkButton = 1;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Player viewer = (Player) sendTo;
			this.SetLocation(40, 40);
			this.AddPage(0);
			this.AddBackground(0, 0, 220, 120, 2620);
			this.AddLabel(14, 12, 0x481, "Pozice dialogu");
			this.AddLabel(14, 40, 2301, "X:");
			this.AddTextEntry(40, 40, 60, 20, 52, XEntry, TagMath.IGetTag(viewer, D_Boosts.xTK).ToString(CultureInfo.InvariantCulture));
			this.AddLabel(110, 40, 2301, "Y:");
			this.AddTextEntry(136, 40, 60, 20, 52, YEntry, TagMath.IGetTag(viewer, D_Boosts.yTK).ToString(CultureInfo.InvariantCulture));
			this.AddButton(14, 80, 0xfb7, 0xfb9, OkButton, GumpButtonType.Reply, 0);
			this.AddButton(47, 80, 0xfb1, 0xfb3, 0, GumpButtonType.Reply, 0);
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player viewer = (Player) gi.Cont;
			if (gr.PressedButton == OkButton) {
				if (int.TryParse((gr.GetTextResponse(XEntry) ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int x) && (x >= 0)) {
					viewer.SetTag(D_Boosts.xTK, x);
				}
				if (int.TryParse((gr.GetTextResponse(YEntry) ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int y) && (y >= 0)) {
					viewer.SetTag(D_Boosts.yTK, y);
				}
			}
			D_Boosts.Show(viewer, (Character) args[0]);
		}
	}

	/// <summary>Redraws an open .boosts of a player who chose live: at once when what's shown changes,
	/// otherwise now and then for the countdowns.</summary>
	[SaveableClass, DeepCopyableClass]
	public class BoostsLiveTimer : BoundTimer {
		[SaveableData, CopyableData]
		public Character target;

		internal string lastShown = "";
		internal double nextRedraw;

		[LoadingInitializer, DeepCopyImplementation]
		public BoostsLiveTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			Player viewer = cont as Player;
			D_Boosts def = SingletonScript<D_Boosts>.Instance;
			if ((viewer == null) || (this.target == null) || this.target.IsDeleted || !viewer.HasOpenedDialog(def)) {
				this.Delete();
			} else if ((Globals.TimeInSeconds >= this.nextRedraw) || (D_Boosts.Signature(this.target) != this.lastShown)) {
				viewer.DialogForget(def);
				D_Boosts.Show(viewer, this.target);
			}
		}
	}

	public class Targ_BoostsOf : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			D_Boosts.Show(self, targetted);
			return TargetResult.Done;
		}
	}

	public static class BoostsCommands {
		[SteamFunction]
		public static void Boosts(Character self) {
			if (self is Player player) {
				D_Boosts.Toggle(player, player);
			}
		}

		[SteamFunction]
		public static void BoostsOf(Player self) {
			self.Target(SingletonScript<Targ_BoostsOf>.Instance);
		}
	}
}
