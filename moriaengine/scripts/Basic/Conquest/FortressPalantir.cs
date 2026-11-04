using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	public partial class FortressPalantirDef {
	}

	public partial class FortCashBoxDef {
	}

	/// <summary>#1116 - Erebor's i_fortress_palantir (dobyvani/dobyvani-palantir.scp): the keep is taken at it. While the war
	/// declared on its keep runs, an attacker with the right breaches it by a double click; then the attackers must hold it ConquestSettings.holdSeconds (900 live). A defender's double click turns the
	/// breach back, and the countdown fills up again second by second (a new breach goes on from where it stands). When it
	/// reaches 0, the keep is the attackers'.
	/// A GM's double click sets its keep from the battlefield it stands on.
	/// Fixed from Erebor: the palantir works while the war runs (Erebor's canbewar also wanted the hour 19-21, so every war
	/// stopped dead at 22:00 whatever its countdown said).</summary>
	[ViewableClass]
	public partial class FortressPalantir {
		private static readonly TagKey fortTK = TagKey.Acquire("_palantirFort_");

		internal const int GondorHue = 0x05, MordorHue = 0x22;

		public string Fort {
			get { return this.GetTag(fortTK) as string; }
		}

		public void SetFort(string fort) {
			this.SetTag(fortTK, fort);
			this.Name = "Palantir " + Conquest.AreaName(fort);
			Conquest.Keep(fort).palantir = this;
		}

		public override void On_DClick(AbstractCharacter from) {
			if (!(from is Character ch)) {
				return;
			}
			if (ch.IsGM) {
				FlaggedRegion area = RegionEventSystem.AreaOf(this.GetMap().GetRegionFor(this));
				int level = (area == null) ? 0 : area.Battlefield;
				if (level == 0) {
					ch.RedMessage("Palantir musi stat na bojisti pevnosti.");
					return;
				}
				this.SetFort(Conquest.FortName(level));
				ch.SysMessage("Palantir pevnosti " + Conquest.AreaName(this.Fort));
				return;
			}
			if (ch is Player player) {
				Palantirs.Use(this, player);
			}
		}
	}

	/// <summary>t_fort_cashbox: the gold a capture brought, 1 for every 5 s since the keep was last taken, for the war lord who
	/// took it, as a cheque. Erebor overwrote what the last one didn't collect, as here.</summary>
	[ViewableClass]
	public partial class FortCashBox {
		private static readonly TagKey fortTK = TagKey.Acquire("_cashBoxFort_");

		public string Fort {
			get { return this.GetTag(fortTK) as string; }
			set { this.SetTag(fortTK, value); }
		}

		public override void On_DClick(AbstractCharacter from) {
			string fort = this.Fort;
			if ((fort == null) || !(from is Player player)) {
				return;
			}
			KeepState keep = Conquest.Keep(fort);
			if ((keep.cashCollector != player) || (keep.cashGold <= 0)) {
				return;
			}
			int gold = keep.cashGold;
			this.Say("Bylo ve me nashromazdeno " + gold + " zlatych.");
			CashiersItem.CreateCheck(player, gold);
			keep.cashGold = 0;
			keep.cashCollector = null;
			Logger.WriteInfo(true, "Conquest: " + player.Name + " collected " + gold + " gp from the cash box of " + fort);
		}
	}

	/// <summary>The palantir's rules, from its double click and its 1 s timer.</summary>
	public static class Palantirs {
		/// <summary>Is the war on this keep running? (Erebor's wasDeclaredWar with canbewar.)</summary>
		internal static bool WarRuns(KeepState keep) {
			WarFront front = Conquest.Front(Conquest.Enemy(keep.owner));
			return (front.declaredOn == keep.fort) && front.IsRunning;
		}

		internal static void Use(FortressPalantir palantir, Player player) {
			string fort = palantir.Fort;
			if (player.Flag_Dead) {
				player.RedMessage("Tahni pryc, mrtvolo!");
				return;
			}
			if (fort == null) {
				player.RedMessage("Pevnost neni nastavena, kontaktuj GM.");
				return;
			}
			KeepState keep = Conquest.Keep(fort);
			WarFront attackers = Conquest.Front(Conquest.Enemy(keep.owner));
			if (attackers.declaredOn != fort) {
				player.RedMessage("Teto pevnosti nebyla vyhlasena valka.");
				return;
			}
			if (!attackers.IsRunning) {
				player.RedMessage("Jeste neprisla hodina valky.");
				return;
			}
			string area = Conquest.AreaName(fort);
			if (player.Realm != keep.owner) {
				if (keep.breached) {
					player.SysMessage("Obrana jiz prolomena byla.");
					return;
				}
				if (player.Realm != attackers.realm) {
					return; //Erebor's wasDeclaredWar(fort, src.realm) - outcasts end here, silently
				}
				if (!Conquest.HasRight(player)) {
					player.RedMessage("Nejsi " + Conquest.Word(player, "vyzyvatelem", "vyzyvatelkou") + ", nejsi " + Conquest.Word(player, "opravnen", "opravnena") + ".");
					return;
				}
				keep.breached = true;
				keep.conqueror = player.Realm;
				palantir.Color = (player.Realm == Realm.Gondor) ? FortressPalantir.GondorHue : FortressPalantir.MordorHue;
				palantir.Say("Obrana pevnosti byla prolomena! Uhlidej palantir po dobu " + keep.hold + " s.");
				Logger.WriteInfo(true, "Conquest: " + player.Name + " breached the palantir of " + fort);
				Conquest.Herald(player.Realm, "Prolomili jsme obranu pevnosti " + area + "!");
				Conquest.Herald(keep.owner, "Obrana pevnosti " + area + " byla prolomena!");
				StartTimer(palantir);
			} else if (!keep.breached) {
				player.SysMessage("Palantir pevnosti " + area + ". Dobre jej hlidej!");
			} else {
				palantir.Say("Prolomeni bylo prozatim zvraceno.");
				palantir.Color = (keep.owner == Realm.Gondor) ? FortressPalantir.GondorHue : FortressPalantir.MordorHue;
				keep.breached = false;
				keep.conqueror = player.Realm;
				Logger.WriteInfo(true, "Conquest: " + player.Name + " turned back the breach of " + fort);
				StartTimer(palantir);
			}
		}

		/// <summary>For a test: the palantir only ticks when the test says.</summary>
		internal static bool ticksByHand;

		private static void StartTimer(FortressPalantir palantir) {
			if (!ticksByHand && (palantir.GetTimer(PalantirTimer.Key) == null)) {
				PalantirTimer timer = new PalantirTimer();
				palantir.AddTimer(PalantirTimer.Key, timer);
				timer.DueInSeconds = 1;
				timer.PeriodInSeconds = 1;
			}
		}

		private static void StopTimer(FortressPalantir palantir) {
			palantir.DeleteTimer(PalantirTimer.Key);
		}

		/// <summary>The palantir's @timer, each second while it's breached or filling up again.</summary>
		internal static void Tick(FortressPalantir palantir) {
			string fort = palantir.Fort;
			if (fort == null) {
				StopTimer(palantir);
				return;
			}
			KeepState keep = Conquest.Keep(fort);
			int full = ConquestSettings.instance.holdSeconds;
			string area = Conquest.AreaName(fort);
			if (!WarRuns(keep)) {
				palantir.Say("Cas na uspesne dobyti pevnosti vyprsel. Pevnost byla ubranena.");
				Reset(keep);
				return;
			}
			if (keep.breached) {
				if (keep.hold > 0) {
					keep.hold--;
				}
				if (keep.hold > 0) {
					if (keep.hold % 10 == 0) {
						palantir.Say("Uhlidej palantir jeste " + keep.hold + " s.");
					}
					if (keep.hold % 60 == 0) {
						Conquest.Herald(keep.conqueror, "Jeste " + keep.hold + "s a pevnost " + area + " je nase!");
						Conquest.Herald(keep.owner, "Jeste " + keep.hold + "s a prijdeme o pevnost " + area + "!");
					}
					return;
				}
				palantir.Say("Pevnost byla dobyta.");
				Realm loser = keep.owner;
				Capture(keep);
				Conquest.Herald(keep.owner, "Dobyli jsme pevnost " + area + "!");
				Conquest.Herald(loser, "Ztratili jsme pevnost " + area + "!");
				return;
			}
			if (keep.hold < full) {
				keep.hold++;
				int left = full - keep.hold;
				if ((left > 0) && (left % 10 == 0)) {
					palantir.Say("Nedovol nepriteli dotknout se palantiru jeste alespon " + left + " s.");
				}
			}
			if (keep.hold >= full) {
				palantir.Say("Prolomeni odvraceno.");
				StopTimer(palantir);
				palantir.Color = 0;
			}
		}

		/// <summary>No breach, the countdown full, the palantir as it was.</summary>
		internal static void Reset(KeepState keep) {
			keep.breached = false;
			keep.conqueror = keep.owner;
			keep.hold = ConquestSettings.instance.holdSeconds;
			if (keep.palantir is FortressPalantir palantir && !palantir.IsDeleted) {
				StopTimer(palantir);
				palantir.Color = 0;
			}
		}

		/// <summary>handleConqueredFort: the keep changes hands, the attackers' declaration and queue go (and their rights),
		/// both fronts' lines move, the loser's own declaration aims at its new target, the cash box fills for the war lord
		/// (the keep's captain stones and guards, and the rewards, come with the next steps of #1116).</summary>
		internal static void Capture(KeepState keep) {
			Realm winner = keep.conqueror, loser = keep.owner;
			int level = Conquest.FortLevel(keep.fort);
			keep.owner = winner;
			Reset(keep);

			WarFront winners = Conquest.Front(winner);
			Player lord = winners.lord;
			foreach (WarQueueEntry queued in winners.queue) {
				Conquest.TakeRight(queued.player);
			}
			winners.Clear();

			winners.conquered = (((winner == Realm.Gondor) && (level == 1)) || ((winner == Realm.Mordor) && (level == -1))) ? 0 : level;
			WarFront losers = Conquest.Front(loser);
			losers.conquered = (loser == Realm.Gondor) ? (level + 1) : (level - 1);
			if (losers.declaredOn != null) {
				losers.declaredOn = losers.Target;
			}

			DateTime now = Conquest.Now;
			keep.cashGold = (int) Math.Max(0, (now - keep.lastConquered).TotalSeconds / 5);
			keep.cashCollector = lord;
			keep.lastConquered = now;

			if (lord != null) {
				Conquest.TakeRight(lord);
			}
			Logger.WriteInfo(true, "Conquest: " + Conquest.RealmName(winner) + " took " + keep.fort + ((lord == null) ? "" : (", war lord " + lord.Name)));
		}
	}

	/// <summary>The palantir's 1 s @timer.</summary>
	[SaveableClass, DeepCopyableClass]
	public class PalantirTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_palantirTimer_");

		[LoadingInitializer, DeepCopyImplementation]
		public PalantirTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if (cont is FortressPalantir palantir) {
				Palantirs.Tick(palantir);
			}
		}
	}

	/// <summary>One keep: who holds it, the breach and its countdown, the cash box. Erebor kept this on the palantir.</summary>
	[SaveableClass, DeepCopyableClass]
	public class KeepState {
		[SaveableData, CopyableData]
		public string fort;

		[SaveableData, CopyableData]
		public Realm owner;

		/// <summary>The realm whose double click last changed the breach (Erebor's conquerors_realm).</summary>
		[SaveableData, CopyableData]
		public Realm conqueror;

		[SaveableData, CopyableData]
		public bool breached;

		/// <summary>Seconds the attackers must still hold the palantir (Erebor's timer_prolomeni).</summary>
		[SaveableData, CopyableData]
		public int hold;

		[SaveableData, CopyableData]
		public DateTime lastConquered;

		[SaveableData, CopyableData]
		public int cashGold;

		[SaveableData, CopyableData]
		public Player cashCollector;

		[SaveableData, CopyableData]
		public Thing palantir;

		[LoadingInitializer, DeepCopyImplementation]
		public KeepState() {
		}
	}
}
