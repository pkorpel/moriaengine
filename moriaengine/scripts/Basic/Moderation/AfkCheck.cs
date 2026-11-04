using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1107 - Erebor's AFK check (systems/administrace/afk.scp, afk_defs.scp). A player gets a dialog of four random
	/// letters drawn as dots and has AfkSettings.answerSeconds to type them. A failure counts within a period
	/// (AfkSettings.periodDays); AfkSettings.allowedFailures are let go, the next one sends the player to the labour camp
	/// for AFK - or, already there, doubles what they owe. A run of correct answers wipes a failure. A check or its
	/// warning can't be escaped by logging out: it carries on at login.
	/// The automatic round warns every gathering or crafting player some seconds ahead; it's off by default, as on
	/// Erebor, where GMs ran the checks (.afk, .runafk, .forceafk).
	/// Not ported: Erebor's fake letters for catching scripts (nastaveni_afk_fakeonoff, off there) and its world-save
	/// pardon (a check ending during a save).</summary>
	public static class AfkCheck {
		private static readonly TagKey failsTK = TagKey.Acquire("_afkFails_");
		private static readonly TagKey correctTK = TagKey.Acquire("_afkCorrect_");
		private static readonly TagKey periodEndTK = TagKey.Acquire("_afkPeriodEnd_");
		private static readonly TagKey caughtTK = TagKey.Acquire("_afkCaught_");

		private static readonly SkillName[] gathering = { SkillName.Cooking, SkillName.Blacksmith, SkillName.Alchemy, SkillName.Carpentry, SkillName.Lumberjacking,
			SkillName.Mining, SkillName.Fishing, SkillName.Tinkering, SkillName.Fletching, SkillName.Tailoring, SkillName.Inscribe, SkillName.Cartography,
			SkillName.AnimalTaming, SkillName.Lockpicking };

		/// <summary>ismacroing: gathering or crafting right now.</summary>
		public static bool IsMacroing(Character ch) {
			return Array.IndexOf(gathering, ch.CurrentSkillName) >= 0;
		}

		/// <summary>myaction, for the log.</summary>
		internal static string ActionOf(Character ch) {
			SkillName skill = ch.CurrentSkillName;
			return (skill == SkillName.None) ? "-" : ("Skill_" + skill);
		}

		public static bool HasCheck(Character ch) {
			return ch.HasTimer(AfkCheckTimer.Key) || ch.HasTimer(AfkCountdownTimer.Key);
		}

		private static long Now {
			get { return DateTime.UtcNow.Ticks; }
		}

		/// <summary>runAFK / afkProximity: the warning first, then the check.</summary>
		public static void StartCountdown(Player player) {
			if (HasCheck(player)) {
				return;
			}
			AfkCountdownTimer timer = new AfkCountdownTimer();
			timer.endsAt = Now + TimeSpan.FromSeconds(AfkSettings.instance.countdownSeconds).Ticks;
			timer.action = ActionOf(player);
			player.AddTimer(AfkCountdownTimer.Key, timer);
			timer.PeriodInSeconds = 1;
			timer.DueInSeconds = 0.1;
			AfkLog.Add(player, "odpocet pred AFK kontrolou", null);
		}

		/// <summary>userafk: the check now, with `seconds` to answer (0 = AfkSettings.answerSeconds).</summary>
		public static void Start(Player player, int seconds, string action) {
			if (player.HasTimer(AfkCheckTimer.Key)) {
				return;
			}
			if (seconds <= 0) {
				seconds = AfkSettings.instance.answerSeconds;
			}
			AfkCheckTimer timer = new AfkCheckTimer();
			timer.letters = RandomLetters();
			timer.endsAt = Now + TimeSpan.FromSeconds(seconds).Ticks;
			timer.action = action ?? ActionOf(player);
			player.AddTimer(AfkCheckTimer.Key, timer);
			timer.PeriodInSeconds = 1;
			timer.DueInSeconds = 1;
			player.RedMessage(Loc<AfkLoc>.Get(player.Language).Check);
			player.Sound(1153);
			AfkLog.Add(player, "zacala AFK kontrola (" + seconds + " s)", null);
			Show(player, timer);
		}

		private static string RandomLetters() {
			char[] letters = new char[4];
			for (int i = 0; i < letters.Length; i++) {
				letters[i] = (char) ('a' + Globals.dice.Next(26));
			}
			return new string(letters);
		}

		internal static void Show(Player player, AfkCheckTimer timer) {
			timer.shown = true;
			player.Dialog(player, SingletonScript<D_AfkCheck>.Instance, new DialogArgs(timer.letters));
		}

		internal static int SecondsLeft(AfkCheckTimer timer) {
			return (int) Math.Ceiling((timer.endsAt - Now) / (double) TimeSpan.TicksPerSecond);
		}

		/// <summary>The dialog's OK: the letters anywhere in what was typed, in any case, as Erebor's strmatch.</summary>
		internal static void Answer(Player player, string written) {
			AfkLoc texts = Loc<AfkLoc>.Get(player.Language);
			if (!(player.GetTimer(AfkCheckTimer.Key) is AfkCheckTimer timer)) {
				AfkLog.Add(player, "chyba pri AFK kontrole - chybel kontrolni item", null);
				return;
			}
			if ((written ?? "").IndexOf(timer.letters, StringComparison.OrdinalIgnoreCase) >= 0) {
				if (SecondsLeft(timer) >= AfkSettings.instance.answerSeconds - 2) {
					AfkLog.Add(player, "byl podezrele rychly pri AFK - zbyl cas " + SecondsLeft(timer) + "s", null);
				}
				player.SysMessage(texts.Correct);
				player.DeleteTimer(AfkCheckTimer.Key);
				WipeExpiredPeriods(player);
				if (player.HasTag(failsTK)) {
					CountCorrect(player);
				}
				return;
			}
			player.SysMessage(texts.Wrong);
			AfkLog.Add(player, "zadal neplatne reseni AFK. Mel napsat: " + timer.letters + ", napsal: " + written + ", jeste ma cas..", null);
			if (SecondsLeft(timer) > 0) {
				player.SysMessage(texts.TryAgain);
			}
		}

		/// <summary>The time ran out.</summary>
		internal static void TimedOut(Player player, AfkCheckTimer timer) {
			player.DeleteTimer(AfkCheckTimer.Key);
			player.DialogClose(SingletonScript<D_AfkCheck>.Instance, 0);
			player.SysMessage(Loc<AfkLoc>.Get(player.Language).TimeUp);
			if (!DeservesCamp(player, timer)) {
				return;
			}
			Caught(player);
			if (Camp.IsInCamp(player)) {
				Camp.SetLevel(player.Account, true, Camp.Level(player.Account, true) + 1);
				Camp.SetSentence(player, Camp.Sentence(player) * 2);
				player.RedMessage(string.Format(Loc<AfkLoc>.Get(player.Language).SentenceDoubled, Camp.Sentence(player)));
				AfkLog.Add(player, "trestanec zdvojnasoben pocet logu - nyni: " + Camp.Sentence(player) + ". Celkem byl " + Camp.Level(player.Account, true) + "x AFK. Mel napsat: " + timer.letters, timer.action);
				return;
			}
			AfkLog.Add(player, "makrar do lagru za " + timer.action + ". Celkem byl " + (Camp.Level(player.Account, true) + 1) + "x AFK. Mel napsat: " + timer.letters, timer.action);
			Camp.Send(player, true);
		}

		private static void Caught(Character player) {
			player.SetTag(caughtTK, Convert.ToInt32(player.GetTag(caughtTK) ?? 0, CultureInfo.InvariantCulture) + 1);
		}

		/// <summary>deservesLager: a failure within the period is counted and let go while there are some left; the one past
		/// AfkSettings.allowedFailures ends the period and sends to the camp.</summary>
		private static bool DeservesCamp(Player player, AfkCheckTimer timer) {
			AfkLoc texts = Loc<AfkLoc>.Get(player.Language);
			AfkSettings settings = AfkSettings.instance;
			if (!(player.GetTag(periodEndTK) is long end)) {
				StartPeriod(player);
				player.SysMessage(string.Format(texts.FirstFailure, settings.allowedFailures));
				AfkLog.Add(player, "po 1. selhal vubec pri AFK. Jeho skill: " + timer.action + ". Mel napsat: " + timer.letters, null);
				return false;
			}
			if (Now > end) {
				StartPeriod(player);
				player.SysMessage(texts.FirstFailureOfPeriod);
				AfkLog.Add(player, "po 1. selhal pri AFK v novem obdobi. Jeho skill: " + timer.action + ". Mel napsat: " + timer.letters, null);
				return false;
			}
			int fails = Failures(player);
			if (!player.HasTag(failsTK)) {
				player.SetTag(failsTK, 1);
				player.SetTag(correctTK, 0);
				player.SysMessage(string.Format(texts.NthFailure, 1));
				return false;
			}
			if (fails < settings.allowedFailures) {
				player.SetTag(failsTK, fails + 1);
				player.SysMessage(string.Format(texts.NthFailure, fails + 1));
				AfkLog.Add(player, "po " + (fails + 1) + ". selhal pri AFK v obdobi. Jeho skill: " + timer.action + ". Mel napsat: " + timer.letters, null);
				return false;
			}
			player.SysMessage(texts.ToleranceGone);
			AfkLog.Add(player, "presahl limit AFK selhani. Sanci na uspech mel " + fails + ". Mel napsat: " + timer.letters, null);
			ClearPeriod(player);
			return true;
		}

		private static void StartPeriod(Character player) {
			player.SetTag(periodEndTK, Now + TimeSpan.FromDays(AfkSettings.instance.periodDays).Ticks);
			player.SetTag(failsTK, 1);
			player.SetTag(correctTK, 0);
		}

		private static void ClearPeriod(Character player) {
			player.RemoveTag(failsTK);
			player.RemoveTag(correctTK);
			player.RemoveTag(periodEndTK);
		}

		public static int Failures(Character player) {
			return Convert.ToInt32(player.GetTag(failsTK) ?? 0, CultureInfo.InvariantCulture);
		}

		/// <summary>decreaseAFKTag: one failure fewer, the period a period longer; none left ends it.</summary>
		private static void WipeOne(Character player) {
			int fails = Failures(player) - 1;
			if (fails <= 0) {
				ClearPeriod(player);
				return;
			}
			player.SetTag(failsTK, fails);
			player.SetTag(correctTK, 0);
			long end = (player.GetTag(periodEndTK) is long e) ? e : Now;
			player.SetTag(periodEndTK, end + TimeSpan.FromDays(AfkSettings.instance.periodDays).Ticks);
		}

		/// <summary>A correct answer: each period that has run out takes a failure with it.</summary>
		private static void WipeExpiredPeriods(Character player) {
			while ((player.GetTag(periodEndTK) is long end) && (Now > end)) {
				WipeOne(player);
			}
		}

		/// <summary>increaseAFKTagSuccess: AfkSettings.correctToWipeFailure correct answers wipe a failure.</summary>
		private static void CountCorrect(Character player) {
			int correct = Convert.ToInt32(player.GetTag(correctTK) ?? 0, CultureInfo.InvariantCulture) + 1;
			if (correct >= AfkSettings.instance.correctToWipeFailure) {
				WipeOne(player);
			} else {
				player.SetTag(correctTK, correct);
			}
		}

		/// <summary>The AFK part of lagrodvolat: one catch and one failure fewer.</summary>
		public static void ForgiveFailure(Character player) {
			int caught = Convert.ToInt32(player.GetTag(caughtTK) ?? 0, CultureInfo.InvariantCulture) - 1;
			if (caught <= 0) {
				player.RemoveTag(caughtTK);
			} else {
				player.SetTag(caughtTK, caught);
			}
			int fails = Failures(player) - 1;
			if (fails <= 0) {
				ClearPeriod(player);
			} else {
				player.SetTag(failsTK, fails);
			}
		}

		/// <summary>e_afk_controlled's @login: a check whose time ran out while offline gets one more second.</summary>
		internal static void OnLogin(Player player) {
			AfkTrigger.EnsureRunning();
			if (player.GetTimer(AfkCheckTimer.Key) is AfkCheckTimer timer) {
				if (timer.endsAt < Now) {
					AfkLog.Add(player, "se lognul " + (-SecondsLeft(timer)) + "s po konci AFK kontroly", null);
					timer.endsAt = Now + TimeSpan.TicksPerSecond;
				} else {
					AfkLog.Add(player, "po lognuti zbyva " + SecondsLeft(timer) + "s do konce AFK kontroly", null);
				}
				timer.shown = false;
			}
		}

		/// <summary>e_afk_controlled's @logout.</summary>
		internal static void OnLogout(Player player) {
			if (player.HasTimer(AfkCheckTimer.Key)) {
				AfkLog.Add(player, "se odlogoval pri AFK kontrole", null);
			} else if (player.HasTimer(AfkCountdownTimer.Key)) {
				AfkLog.Add(player, "se odlogoval pri odpocitavani AFK kontroly", null);
			}
		}

		/// <summary>One automatic round: every gathering or crafting player gets the warning.</summary>
		internal static void Round() {
			foreach (AbstractCharacter ch in new List<AbstractCharacter>(SteamEngine.Networking.GameServer.GetAllPlayers())) {
				if ((ch is Player player) && player.IsOnline && !player.IsGM && !player.Flag_Dead && IsMacroing(player) && !HasCheck(player)) {
					StartCountdown(player);
				}
			}
		}

		[SteamFunction("Afk")]
		public static void AfkCommand(Player self, ScriptArgs sa) {
			int seconds = 0;
			if ((sa != null) && (sa.Args != null)) {
				int.TryParse(sa.Args.Trim().Trim('"'), NumberStyles.Integer, CultureInfo.InvariantCulture, out seconds);
			}
			self.SysMessage(Loc<AfkLoc>.Get(self.Language).WhichPlayer);
			self.Target(SingletonScript<Targ_Afk>.Instance, seconds);
		}

		[SteamFunction]
		public static void RunAfk(Player self) {
			self.SysMessage(Loc<AfkLoc>.Get(self.Language).WhichPlayer);
			self.Target(SingletonScript<Targ_Afk>.Instance, -1);
		}

		[SteamFunction]
		public static void ForceAfk(Player self) {
			Logger.WriteInfo(true, self.Name + " started a round of AFK checks");
			Round();
		}

		/// <summary>pocetafk: how many failures count now.</summary>
		[SteamFunction]
		public static void PocetAfk(Character self) {
			WipeExpiredPeriods(self);
			AfkLoc texts = Loc<AfkLoc>.Get(self.Language);
			self.SysMessage(string.Format(self.IsFemale ? texts.MissedFemale : texts.MissedMale, Failures(self)));
		}
	}

	public class Targ_Afk : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			if (!(targetted is Player player) || !player.IsOnline) {
				self.SysMessage(Loc<AfkLoc>.Get(self.Language).OnlyOnline);
				return TargetResult.RestartTargetting;
			}
			int seconds = (int) parameter;
			if (seconds < 0) {
				AfkCheck.StartCountdown(player);
				AfkLog.Add(player, "odpocet pred AFK kontrolou pustil " + self.Name, null);
			} else {
				AfkCheck.Start(player, seconds, null);
				AfkLog.Add(player, "AFK kontrolu pustil " + self.Name + ((seconds > 0) ? (" s limitem " + seconds + "s") : " s default limitem"), null);
			}
			return TargetResult.Done;
		}
	}

	/// <summary>i_afk_dialog: the check, ticking each second. The red count every other second; a dialog that was answered
	/// wrong or closed comes back.</summary>
	[SaveableClass, DeepCopyableClass]
	public class AfkCheckTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_afkCheck_");

		[SaveableData, CopyableData]
		public string letters;

		[SaveableData, CopyableData]
		public long endsAt;

		[SaveableData, CopyableData]
		public string action;

		//not saved: after a restart or a login the dialog has to be sent again
		internal bool shown;

		[LoadingInitializer, DeepCopyImplementation]
		public AfkCheckTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if (!(cont is Player player) || player.IsDeleted || !player.IsOnline) {
				return;
			}
			int left = AfkCheck.SecondsLeft(this);
			if (left <= 0) {
				AfkCheck.TimedOut(player, this);
				return;
			}
			if (!this.shown) {
				AfkCheck.Show(player, this);
			} else if ((left % 2) == 1) {
				player.RedMessage(string.Format(Loc<AfkLoc>.Get(player.Language).SecondsToCamp, left));
			}
		}
	}

	/// <summary>i_afk_countdown: the warning, a red line each second, then the check with what's left of its time.</summary>
	[SaveableClass, DeepCopyableClass]
	public class AfkCountdownTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_afkCountdown_");

		[SaveableData, CopyableData]
		public long endsAt;

		[SaveableData, CopyableData]
		public string action;

		[LoadingInitializer, DeepCopyImplementation]
		public AfkCountdownTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if (!(cont is Player player) || player.IsDeleted || !player.IsOnline) {
				return;
			}
			long now = DateTime.UtcNow.Ticks;
			if (now < this.endsAt) {
				int left = (int) Math.Ceiling((this.endsAt - now) / (double) TimeSpan.TicksPerSecond);
				player.RedMessage(string.Format(Loc<AfkLoc>.Get(player.Language).CheckIn, left));
				return;
			}
			player.DeleteTimer(Key);
			long late = (now - this.endsAt) / TimeSpan.TicksPerSecond;
			int seconds = (int) Math.Max(1, AfkSettings.instance.answerSeconds - late);
			AfkCheck.Start(player, seconds, this.action);
		}
	}

	/// <summary>i_afk_trigger: the automatic rounds, while AfkSettings.automaticCheck is on. Not saved: it starts again with
	/// the first login after a restart.</summary>
	public sealed class AfkTrigger : SteamEngine.Timers.Timer {
		private static AfkTrigger running;

		internal static void EnsureRunning() {
			if ((running == null) || running.IsDeleted) {
				running = new AfkTrigger();
				running.Schedule();
			}
		}

		private void Schedule() {
			AfkSettings settings = AfkSettings.instance;
			bool soon = (settings.soonAfterOneIn > 0) && (Globals.dice.Next(settings.soonAfterOneIn) == 0);
			int low = soon ? settings.soonMinSeconds : settings.minSecondsBetween;
			int high = soon ? settings.soonMaxSeconds : settings.maxSecondsBetween;
			this.DueInSeconds = Globals.dice.Next(Math.Min(low, high), Math.Max(low, high) + 1);
		}

		protected override void OnTimeout() {
			if (AfkSettings.instance.automaticCheck) {
				AfkCheck.Round();
			}
			this.Schedule();
		}
	}

	/// <summary>d_afk_kontrola: four letters drawn in red dots (Erebor's dot font, the converted d_afk_&lt;letter&gt;_&lt;n&gt;
	/// points on a 150 x 150 field), each at a random size and place, the whole dialog at a random spot.</summary>
	public class D_AfkCheck : CompiledGumpDef {
		internal const int OkButton = 1, TextEntryId = 1;
		private const int FieldWidth = 150, Width = 600, LettersHeight = 150, Height = 240, MaxX = 40, MaxY = 220, Gap = 3, Row = 19;
		private const int LettersBackground = 5120, InfoBackground = 2620, Dot = 2360, Separator = 2524, TextBackground = 5120, Hue = 0x481;
		private static readonly int[] scaleNumerator = { 4, 6, 2 }, scaleDenominator = { 8, 8, 2 }, maxOffset = { 67, 34, 0 };

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			string letters = (string) args[0];
			AfkLoc texts = Loc<AfkLoc>.Get(sendTo.Language);
			int x = Globals.dice.Next(MaxX + 1), y = Globals.dice.Next(MaxY + 1);
			this.SetLocation(0, 0);
			this.NoClose();
			this.NoMove();
			this.AddPage(0);
			this.AddBackground(x, y, Width, LettersHeight, LettersBackground);
			for (int u = 0; u < letters.Length; u++) {
				int scale = Globals.dice.Next(3);
				int offset = Globals.dice.Next(maxOffset[scale] + 1);
				int fieldX = x + (FieldWidth * u) + offset, fieldY = y + offset;
				foreach (int[] point in Points(letters[u])) {
					this.AddImage(fieldX + (scaleNumerator[scale] * point[0] / scaleDenominator[scale]), fieldY + (scaleNumerator[scale] * point[1] / scaleDenominator[scale]), Dot);
				}
			}
			for (int u = 1; u < 4; u++) {
				this.AddImageTiled(x + (u * FieldWidth) - 1, y, Gap, LettersHeight, Separator);
			}
			this.AddBackground(x, y + LettersHeight, Width, Height - LettersHeight, InfoBackground);
			this.AddLabel(x + 10, y + LettersHeight + (2 * Gap), Hue, texts.Check);
			this.AddLabel(x + 10, y + LettersHeight + 8 + Row, Hue, texts.CopyTheText);
			this.AddBackground(x + 5, y + LettersHeight + (3 * Row), 200, Row, TextBackground);
			this.AddTextEntry(x + 10, y + LettersHeight + (3 * Row), 200, 20, Hue, TextEntryId, "TEXT: ");
			this.AddButton(x + 220, y + LettersHeight + (3 * Row), 2450, 2451, OkButton, GumpButtonType.Reply, 0);
		}

		/// <summary>The converted d_afk_&lt;letter&gt;_1..length points.</summary>
		internal static List<int[]> Points(char letter) {
			List<int[]> points = new List<int[]>();
			int length = Camp.Table("d_afk_" + letter + "_length");
			for (int i = 1; i <= length; i++) {
				Constant constant = Constant.GetByName("d_afk_" + letter + "_" + i);
				int[] point = (constant == null) ? null : Point(constant.Value);
				if (point != null) {
					points.Add(point);
				}
			}
			return points;
		}

		private static int[] Point(object value) {
			if (value is IList list && (list.Count >= 2)) {
				return new[] { Convert.ToInt32(list[0], CultureInfo.InvariantCulture), Convert.ToInt32(list[1], CultureInfo.InvariantCulture) };
			}
			string[] parts = Convert.ToString(value, CultureInfo.InvariantCulture).Split(',');
			if ((parts.Length >= 2) && int.TryParse(parts[0].Trim(), out int px) && int.TryParse(parts[1].Trim(), out int py)) {
				return new[] { px, py };
			}
			return null;
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if (!(gi.Cont is Player player)) {
				return;
			}
			if (player.GetTimer(AfkCheckTimer.Key) is AfkCheckTimer timer) {
				timer.shown = false; //an answer closes it on the client; the next tick shows it again if the check goes on
			}
			if (gr.PressedButton == OkButton) {
				AfkCheck.Answer(player, gr.GetTextResponse(TextEntryId));
			}
		}
	}

	public class AfkLoc : CompiledLocStringCollection<AfkLoc> {
		public string Check = "AFK KONTROLA!";
		public string CopyTheText = "OPIS TEXT CO VIDIS NAHORE";
		public string Correct = "Spravne, prijemnou hru";
		public string Wrong = "Spatne";
		public string TryAgain = "Zkus to znovu";
		public string TimeUp = "Cas AFK kontroly vyprsel";
		public string SecondsToCamp = "{0} sekund do lagru";
		public string CheckIn = "AFK kontrola za {0} sekund. Doporucuji vypnout makro!";
		public string FirstFailure = "Tve 1. AFK selhani. Davej si lepsi pozor. Limit je {0} selhani v danem obdobi.";
		public string FirstFailureOfPeriod = "Tve 1. AFK selhani v novem obdobi. Davej si lepsi pozor";
		public string NthFailure = "Tve {0}. AFK selhani v pocitanem obdobi. Davej si lepsi pozor!";
		public string ToleranceGone = "Tvoje tolerance k AFK selhani vyprsela. Vitej v Lagru";
		public string SentenceDoubled = "Trest zvysen na {0}";
		public string MissedMale = "Zmeskal jsem {0} krat.";
		public string MissedFemale = "Zmeskala jsem {0} krat.";
		public string WhichPlayer = "Na kterou postavu?";
		public string OnlyOnline = "Jen hrace, kteri jsou ve hre.";
	}
}
