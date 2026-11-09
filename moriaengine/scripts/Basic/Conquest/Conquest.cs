using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1116 - Erebor's keep conquest (dobyvani/dobyvani-declarator.scp, dobyvani-utils.scp), its core: each realm's
	/// war front (Erebor kept it in tags on that realm's war declarator stone, here it's saved with the world), the right to
	/// declare war, the declaration and its queue of reserve attackers, and the war clock: a war starts at its declared hour
	/// and runs ConquestSettings.warSeconds (Erebor's nastaveni_pevnosti_cas_na_dobyti, 3000 live); if nobody takes the keep
	/// by then, it's defended. Heralds tell each realm. The palantir that is taken, its keep's guards and captain, and the
	/// rewards come with it in later steps of #1116.
	/// Front line: each front keeps the battlefield level of the last keep its realm took (Erebor's "conquered"), so Gondor's
	/// next target is one lower (Mordor_1 from 0), Mordor's one higher (Gondor_1 from 0).
	/// Fixed from Erebor (each noted where it's done):
	/// - real dates: Erebor compared month, day and hour each on its own, so a war for 30.11 never started on 1.12, and a war
	///   missed at its hour started at that hour on any later day; a date in an earlier month means next year (Erebor refused
	///   it, "ZATIM ZRUSIT, BUGUJE!"); a later month keeps the hour and "tomorrow" rules (Erebor skipped them);
	/// - the weekend hours 17-22 never applied on Erebor (isWeekend returned 0), so they aren't offered;
	/// - each front has its own running war (Erebor's one ConqueredRegion served both realms);
	/// - the heralds speak once per fifth minute and once before each hour (Erebor's fired on each 10 s tick of the minute);
	/// - a war lord cancelling a running war ends it as defended (Erebor skipped endWar, so its bonus and state were lost);
	/// - a declaration whose evening passed while the server was down lapses as defended, rather than starting at any hour.</summary>
	[HasSavedMembers]
	public static class Conquest {
		[SavedMember]
		private static WarFront gondorFront;

		[SavedMember]
		private static WarFront mordorFront;

		[SavedMember]
		private static List<KeepState> keeps = new List<KeepState>();

		/// <summary>For a test: the clock the conquest goes by.</summary>
		internal static DateTime? clock;

		public static DateTime Now {
			get { return clock ?? DateTime.Now; }
		}

		public static WarFront Front(Realm realm) {
			if (realm == Realm.Gondor) {
				if (gondorFront == null) {
					gondorFront = new WarFront { realm = Realm.Gondor };
				}
				return gondorFront;
			}
			if (mordorFront == null) {
				mordorFront = new WarFront { realm = Realm.Mordor };
			}
			return mordorFront;
		}

		/// <summary>Erebor: realm 1 is Gondor's, anything else Mordor's (GMs and outcasts too).</summary>
		public static WarFront FrontOf(Character ch) {
			return Front((ch.Realm == Realm.Gondor) ? Realm.Gondor : Realm.Mordor);
		}

		public static Realm Enemy(Realm realm) {
			return (realm == Realm.Gondor) ? Realm.Mordor : Realm.Gondor;
		}

		public static string RealmName(Realm realm) {
			return (realm == Realm.Gondor) ? "Gondor" : "Mordor";
		}

		/// <summary>A keep's state, new keeps held by their own realm (Erebor's palantir @create).</summary>
		public static KeepState Keep(string fort) {
			KeepState keep = keeps.Find(k => k.fort == fort);
			if (keep == null) {
				Realm own = (FortLevel(fort) < 0) ? Realm.Mordor : Realm.Gondor;
				keep = new KeepState { fort = fort, owner = own, conqueror = own, hold = ConquestSettings.instance.holdSeconds, lastConquered = Now };
				keeps.Add(keep);
			}
			return keep;
		}

		/// <summary>The battlefield level of "Mordor_2": -2.</summary>
		public static int FortLevel(string fort) {
			int underscore = fort.IndexOf('_');
			int depth = int.Parse(fort.Substring(underscore + 1), CultureInfo.InvariantCulture);
			return fort.StartsWith("Mordor", StringComparison.Ordinal) ? -depth : depth;
		}

		/// <summary>#911 - a keep's captain, null when it has none (or no state yet; nothing is made by asking).</summary>
		internal static Player CaptainOf(string fort) {
			KeepState keep = keeps.Find(k => k.fort == fort);
			return ((keep != null) && (keep.captain != null) && !keep.captain.IsDeleted) ? keep.captain : null;
		}

		/// <summary>The palantir of a keep, null before one is set up.</summary>
		internal static FortressPalantir PalantirOf(string fort) {
			KeepState keep = keeps.Find(k => k.fort == fort);
			return ((keep != null) && (keep.palantir is FortressPalantir palantir) && !palantir.IsDeleted) ? palantir : null;
		}

		/// <summary>"Gondor_1" from a battlefield level: + Gondor's, - Mordor's.</summary>
		public static string FortName(int level) {
			return ((level < 0) ? "Mordor_" : "Gondor_") + Math.Abs(level);
		}

		/// <summary>d_area_&lt;fort&gt;_1, Erebor's display name of a keep.</summary>
		public static string AreaName(string fort) {
			switch (fort) {
				case "Gondor_1":
					return "Osgiliath zapadni breh";
				case "Gondor_2":
					return "Vychodni Marka";
				case "Mordor_1":
					return "Osgiliath vychodni breh";
				case "Mordor_2":
					return "Minas Morgul";
				case "Mordor_3":
					return "Temny hvozd";
				default:
					return fort;
			}
		}

		internal static string Word(Character ch, string male, string female) {
			return ch.IsFemale ? female : male;
		}

		internal static string When(DateTime time) {
			return time.Day + "." + time.Month + " v " + time.Hour + "h";
		}

		internal static string Slot(DateTime time) {
			return time.Day + "." + time.Month + " - " + time.Hour + "h";
		}

		#region the right to declare war
		private static readonly TagKey warRightTK = TagKey.Acquire("_canDeclareWar_");

		/// <summary>Erebor's can_declare_war_on_Gondor/_Mordor: bought from the war lieutenant, on the realm's enemy.</summary>
		public static bool HasRight(Character ch) {
			return ch.HasTag(warRightTK);
		}

		public static void GiveRight(Character ch) {
			ch.SetTag(warRightTK, true);
		}

		public static void TakeRight(Character ch) {
			ch.RemoveTag(warRightTK);
		}

		/// <summary>Erebor's GM helper allowmedeclarator.</summary>
		[SteamFunction]
		public static void AllowMeDeclarator(Character self) {
			GiveRight(self);
			self.SysMessage("Muzes vyhlasit valku.");
		}
		#endregion

		#region heralds
		/// <summary>warMessage: to the realm's online players, in its chat colour, from its herald.</summary>
		internal static void Herald(Realm realm, string text) {
			string herald = (realm == Realm.Gondor) ? "Gondorsky herold" : "Mordorsky herold";
			ChatSystem.Announce((realm == Realm.Gondor) ? ChatChannel.Gondor : ChatChannel.Mordor, herald, text);
		}
		#endregion

		#region declaring
		/// <summary>warTimePasses and checkDateCorrectness on real dates: the date the entered day, month and hour mean, or a
		/// refusal. A month before this one is next year's.</summary>
		internal static string ResolveStart(Character ch, int day, int month, int hour, out DateTime start) {
			start = default(DateTime);
			ConquestSettings settings = ConquestSettings.instance;
			DateTime now = Now;
			if ((month < 1) || (month > 12)) {
				return "Rok ma jenom dvanact mesicu.";
			}
			int year = (month < now.Month) ? (now.Year + 1) : now.Year;
			int days = DateTime.DaysInMonth(year, month);
			if ((day < 1) || (day > days)) {
				return MonthName(month) + " ma jen " + days + " dni.";
			}
			if ((hour < settings.warFromHour) || (hour > settings.warToHour)) {
				return "Boj muze probihat pouze mezi " + settings.warFromHour + " a " + settings.warToHour + " hod.";
			}
			start = new DateTime(year, month, day, hour, 0, 0);
			if (start.Date <= now.Date) {
				return "Valku lze vyhlasit nejdrive na zitrek.";
			}
			return null;
		}

		private static readonly string[] monthNames = { "Leden", "Unor", "Brezen", "Duben", "Kveten", "Cerven", "Cervenec", "Srpen", "Zari", "Rijen", "Listopad", "Prosinec" };

		internal static string MonthName(int month) {
			return monthNames[month - 1];
		}

		/// <summary>Button 1 of dialog_declare_war: a declaration, or a place in the queue. Null and the reply, or the refusal;
		/// `reopen` when Erebor opened the dialog again.</summary>
		public static string Declare(Player player, WarFront front, int day, int month, int hour, out bool done, out bool reopen) {
			done = false;
			reopen = true;
			string refused = ResolveStart(player, day, month, hour, out DateTime start);
			if (refused != null) {
				return refused;
			}
			WarFront enemy = Front(Enemy(front.realm));
			if ((enemy.lord != null) && (enemy.begins == start)) {
				return "Na tento cas ti valku jiz vyhlasil nepritel.";
			}
			if ((front.lord != null) && (front.begins == start)) {
				return "Na tento cas jiz je valka vyhlasena.";
			}
			foreach (WarQueueEntry queued in front.queue) {
				if (queued.begins == start) {
					return "Na tento cas jiz valku " + Word(queued.player, "naplanoval", "naplanovala") + " " + queued.player.Name + ".";
				}
			}
			string area = AreaName(front.Target);
			if (front.lord == null) {
				front.declaredOn = front.Target;
				front.lord = player;
				front.begins = start;
				front.remaining = 0;
				Logger.WriteInfo(true, "Conquest: " + player.Name + " declared war on " + front.declaredOn + " for " + start.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
			} else {
				reopen = false;
				if ((front.lord == player) || front.IsQueued(player)) {
					return "Ty jiz jsi valku teto pevnosti " + Word(player, "vyhlasil", "vyhlasila") + "!";
				}
				DateTime last = front.LastStart;
				if (start <= last) {
					reopen = true;
					return "Dalsi valku lze vyhlasit jen na pozdejsi dobu, nez je " + last.Day + ". " + last.Month + ". - " + last.Hour + " hod.";
				}
				front.queue.Add(new WarQueueEntry { player = player, begins = start });
				Logger.WriteInfo(true, "Conquest: " + player.Name + " queued for war on " + front.declaredOn + " at " + start.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
			}
			done = true;
			reopen = false;
			return "Valka pevnosti " + area + " vyhlasena na den " + start.Day + ". " + start.Month + ". - " + start.Hour + " hod.";
		}

		/// <summary>Button 2: the war lord's or a queued attacker's cancel. It costs the right, as on Erebor.</summary>
		public static string Cancel(Player player, WarFront front) {
			string area = AreaName(front.declaredOn);
			string reply;
			if (front.lord == player) {
				if (front.IsRunning) { //fixed from Erebor: its cancel skipped endWar
					EndWar(front);
					reply = Word(player, "Zrusil", "Zrusila") + " jsi svuj utok na pevnost " + area + ".";
				} else if (front.queue.Count == 0) {
					front.Clear();
					reply = "S pevnosti " + area + " byl podepsan docasny mir.";
				} else {
					Player next = front.CallNext();
					reply = Word(player, "Zrusil", "Zrusila") + " jsi svuj utok na pevnost " + area + ". Nyni je na rade " + next.Name;
				}
			} else if (front.IsQueued(player)) {
				front.queue.RemoveAll(e => e.player == player);
				reply = Word(player, "Zrusil", "Zrusila") + " jsi svuj utok na pevnost " + area + ".";
			} else {
				return null;
			}
			Logger.WriteInfo(true, "Conquest: " + player.Name + " cancelled the attack on " + area);
			TakeRight(player);
			return reply;
		}
		#endregion

		#region the war clock
		/// <summary>f_war_declarator_OnTimer, each 10 s per front.</summary>
		internal static void Tick(WarFront front) {
			if (front.declaredOn == null) {
				front.remaining = 0;
				return;
			}
			DateTime now = Now;
			Realm attacker = front.realm, defender = Enemy(front.realm);
			string area = AreaName(front.declaredOn);
			if (front.IsRunning) {
				front.remaining -= WarDeclaratorTimer.Period;
				if (front.remaining <= 0) {
					EndWar(front);
					return;
				}
				if (front.lord != null) {
					front.lord.RedMessage("Na uspesne dobyti zbyva " + front.remaining + " s.");
				}
				PalantirOf(front.declaredOn)?.Say("Na uspesne dobyti zbyva " + front.remaining + " s.");
				int minute = (now.Hour * 60) + now.Minute;
				if ((now.Minute % 5 == 0) && (front.lastHerald != minute)) {
					front.lastHerald = minute;
					Herald(attacker, "Dobyvani pevnosti " + area + " - jeste " + front.remaining + " s!");
					Herald(defender, "Brante pevnost " + area + " - jeste " + front.remaining + " s!");
				}
				return;
			}
			if (now >= front.begins) {
				if (now.Date != front.begins.Date || (now.Hour > ConquestSettings.instance.warToHour)) { //the evening passed with the server down
					Logger.WriteInfo(true, "Conquest: the war on " + front.declaredOn + " lapsed, its evening passed");
					EndWar(front);
					return;
				}
				StartWar(front);
				return;
			}
			if (now.Minute == 59) {
				int hourMark = (now.DayOfYear * 24) + now.Hour;
				if (front.lastReminder != hourMark) {
					front.lastReminder = hourMark;
					Herald(attacker, "Dobyvani pevnosti " + area + " zacne " + When(front.begins));
					Herald(defender, "Braneni nasi pevnosti " + area + " zacne " + When(front.begins));
				}
			}
		}

		internal static void StartWar(WarFront front) {
			string area = AreaName(front.declaredOn);
			front.remaining = ConquestSettings.instance.warSeconds;
			front.lastHerald = -1;
			KeepCaptains.RespawnGuards(front.declaredOn);
			if (front.lord != null) {
				front.lord.DelayedRedMessage("Valka muze zacit!");
			}
			Herald(front.realm, "Dobyvani pevnosti " + area + " prave zacalo");
			Herald(Enemy(front.realm), "Braneni nasi pevnosti " + area + " prave zacalo");
			Logger.WriteInfo(true, "Conquest: the war on " + front.declaredOn + " starts");
		}

		/// <summary>endWar: the countdown ran out, the keep was defended. The war lord keeps his right, as on Erebor.</summary>
		internal static void EndWar(WarFront front) {
			string area = AreaName(front.declaredOn);
			if (front.lord != null) {
				front.lord.RedMessage("Cas na dobyti pevnosti vyprsel. Pevnost byla ubranena!");
			}
			if (front.declaredOn != null) {
				PalantirOf(front.declaredOn)?.Say("Cas na dobyti pevnosti vyprsel. Pevnost byla ubranena!");
				Palantirs.Reset(Keep(front.declaredOn));
			}
			Logger.WriteInfo(true, "Conquest: EndWar on " + front.declaredOn + ", the keep was defended");
			Herald(front.realm, "Nepritel pevnost " + area + " ubranil!");
			Herald(Enemy(front.realm), "Ubranili jsme pevnost " + area + "!");
			ConquestRewards.GiveBonus(front, Enemy(front.realm));
			front.remaining = 0;
			if (front.queue.Count == 0) {
				front.Clear();
			} else {
				front.CallNext();
			}
		}
		#endregion

		/// <summary>The .dobyvani command (Erebor's Dobyvani): what one's own realm prepares, and what's coming at it.</summary>
		[SteamFunction]
		public static void Dobyvani(Character self) {
			WarFront own = FrontOf(self);
			WarFront enemy = Front(Enemy(own.realm));
			if (own.declaredOn == null) {
				self.SysMessage("Aktualne se " + RealmName(own.realm) + " nepripravuje na zadny utok.", (int) Hues.Blue); //fixed: Erebor named Mordor to Gondor's players
			} else {
				string area = AreaName(own.declaredOn);
				self.SysMessage(own.IsRunning ? ("Dobyvani pevnosti " + area + " prave probiha!") : ("Dobyvani pevnosti " + area + " zacne " + When(own.begins)), (int) Hues.Blue);
				if (own.queue.Count > 0) {
					self.SysMessage("Nasledujici dobyvani pevnosti " + area + " zacne " + When(own.queue[0].begins), (int) Hues.Blue);
				}
			}
			if (enemy.declaredOn == null) {
				self.RedMessage("Zadny utok na nasi pevnost nebyl vyhlasen.");
			} else {
				string area = AreaName(enemy.declaredOn);
				self.RedMessage(enemy.IsRunning ? ("Braneni nasi pevnosti " + area + " prave probiha!") : ("Braneni nasi pevnosti " + area + " zacne " + When(enemy.begins)));
				if (enemy.queue.Count > 0) {
					self.RedMessage("Nasledujici braneni nasi pevnosti " + area + " zacne " + When(enemy.queue[0].begins));
				}
			}
		}

		/// <summary>Erebor's GM helper resetWars: no declaration, no queue, no war on the front of the realm where the GM stands.</summary>
		[SteamFunction]
		public static void ResetWars(Character self) {
			FlaggedRegion area = RegionEventSystem.AreaOf(self.Region);
			WarFront front = Front(((area != null) && (area.Realm == Realm.Gondor)) ? Realm.Gondor : Realm.Mordor);
			front.Clear();
			self.SysMessage(RealmName(front.realm) + ": zadna valka.");
		}

		/// <summary>For a test: both fronts as new.</summary>
		internal static void Reset() {
			gondorFront = null;
			mordorFront = null;
			keeps.Clear();
		}
	}

	/// <summary>One realm's side of the conquest, what Erebor kept on its war declarator stone.</summary>
	[SaveableClass, DeepCopyableClass]
	public class WarFront {
		[SaveableData, CopyableData]
		public Realm realm;

		/// <summary>The battlefield level of the last keep this realm took; 0 at the start.</summary>
		[SaveableData, CopyableData]
		public int conquered;

		/// <summary>The fort this realm declared war on, null for none.</summary>
		[SaveableData, CopyableData]
		public string declaredOn;

		[SaveableData, CopyableData]
		public Player lord;

		[SaveableData, CopyableData]
		public DateTime begins;

		/// <summary>Seconds left of the running war, 0 while none runs (Erebor's more1).</summary>
		[SaveableData, CopyableData]
		public int remaining;

		[SaveableData, CopyableData]
		public List<WarQueueEntry> queue = new List<WarQueueEntry>();

		/// <summary>Who took part in the running war, with their points (Erebor's bonus stone).</summary>
		[SaveableData, CopyableData]
		public List<WarParticipant> participants = new List<WarParticipant>();

		internal int lastHerald = -1, lastReminder = -1;

		[LoadingInitializer, DeepCopyImplementation]
		public WarFront() {
		}

		public bool IsRunning {
			get { return this.remaining > 0; }
		}

		/// <summary>The keep next on the front line: one lower for Gondor, one higher for Mordor.</summary>
		public string Target {
			get { return Conquest.FortName((this.realm == Realm.Gondor) ? (this.conquered - 1) : (this.conquered + 1)); }
		}

		public bool IsQueued(Player player) {
			return this.queue.Exists(e => e.player == player);
		}

		/// <summary>The latest time taken, the war lord's or the last queued one's.</summary>
		public DateTime LastStart {
			get { return (this.queue.Count > 0) ? this.queue[this.queue.Count - 1].begins : this.begins; }
		}

		internal void Clear() {
			this.declaredOn = null;
			this.lord = null;
			this.begins = default(DateTime);
			this.remaining = 0;
			this.queue.Clear();
		}

		/// <summary>callOtherWarlord: the first queued one takes over the declaration with his own time.</summary>
		internal Player CallNext() {
			WarQueueEntry next = this.queue[0];
			this.queue.RemoveAt(0);
			this.lord = next.player;
			this.begins = next.begins;
			this.remaining = 0;
			next.player.DelayedMessage("Tvuj utok na pevnost " + Conquest.AreaName(this.declaredOn) + " zacne " + Conquest.When(next.begins));
			return next.player;
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class WarQueueEntry {
		[SaveableData, CopyableData]
		public Player player;

		[SaveableData, CopyableData]
		public DateTime begins;

		[LoadingInitializer, DeepCopyImplementation]
		public WarQueueEntry() {
		}
	}

	/// <summary>The war clock of the front of a declarator's realm, every 10 s, as Erebor's memory items on the guardian.</summary>
	[SaveableClass, DeepCopyableClass]
	public class WarDeclaratorTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_warDeclaratorTimer_");
		internal const int Period = 10;

		[LoadingInitializer, DeepCopyImplementation]
		public WarDeclaratorTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if (cont is WarDeclarator stone) {
				Conquest.Tick(Conquest.Front(stone.FrontRealm));
			}
			this.DueInSeconds = Period;
			this.PeriodInSeconds = Period;
		}
	}

	/// <summary>#1116 - Erebor's nastaveni_pevnosti_* and the war hours, on .settings as "Dobyvani".</summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class ConquestSettings : SettingsMetaCategory {
		[SavedMember]
		public static ConquestSettings instance = new ConquestSettings();

		[LoadingInitializer]
		public ConquestSettings() {
		}

		/// <summary>nastaveni_pevnosti_cas_na_dobyti (3000 live): a war's length.</summary>
		[SaveableData]
		public int warSeconds = 3000;

		/// <summary>nastaveni_pevnosti_doba_prolomeni (900 live): how long a breached palantir must be held.</summary>
		[SaveableData]
		public int holdSeconds = 900;

		/// <summary>d_fort_warFrom, d_fort_warTo: the hours a war may start, server time.</summary>
		[SaveableData]
		public int warFromHour = 19;

		[SaveableData]
		public int warToHour = 21;

		/// <summary>nastaveni_pevnosti_vzdal_guard_pevnost: the farthest a keep guard's post from its captain stone.</summary>
		[SaveableData]
		public int keepGuardReach = 10;

		/// <summary>nastaveni_pevnosti_vzdal_guard_vzajemne: the nearest two keep guards' posts may be.</summary>
		[SaveableData]
		public int keepGuardSpacing = 5;
	}
}
