using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#892 - the Rings of Power of systems/player_functions.scp. A ring makes its wearer a Nazgul,
	/// who rules the ring's city (Town.mayor), or, the leading ring, the First Nazgul. It stays only while
	/// its wearer earns it: mayhavering. Erebor left a lost ring lying at the shrine, and here the shrine
	/// makes a new one, since the ring's state starts over anyway.</summary>
	public static class NazgulRings {
		public const string RingDef = "i_ring_power", LeadingRingDef = "i_leading_ring_power", FirstTitle = "Prvni Nazgul ";
		private const int RingLayer = 8;
		private const double SecondsInDay = 24 * 60 * 60, SecondsInMinute = 60;
		private const int RememberedVictims = 3;

		private static readonly TagKey cityTK = TagKey.Acquire("_ringCity_");
		private static readonly TagKey equippedAtTK = TagKey.Acquire("_ringEquippedAt_");
		private static readonly TagKey onlineMinutesTK = TagKey.Acquire("_ringOnlineMinutes_");
		private static readonly TagKey lastOnlineTK = TagKey.Acquire("_ringLastOnline_");
		private static readonly TagKey baseKillsTK = TagKey.Acquire("_ringBaseKills_");
		private static readonly TagKey deathsTK = TagKey.Acquire("_ringDeaths_");
		private static readonly TagKey nextDecayTK = TagKey.Acquire("_ringNextDecay_");
		private static readonly TagKey killsTK = TagKey.Acquire("_playerKillsTotal_");
		private static readonly TagKey questKillsTK = TagKey.Acquire("_nazgulQuestKills_");
		private static readonly TagKey questEndsTK = TagKey.Acquire("_nazgulQuestEnds_");
		private static readonly TagKey[] victimTKs = {
			TagKey.Acquire("_ringVictim0_"), TagKey.Acquire("_ringVictim1_"), TagKey.Acquire("_ringVictim2_")
		};
		private static readonly TimerKey ringTimerTK = TimerKey.Acquire("_ringOfPower_");

		public static PowerBadge RingOf(Character ch) {
			PowerBadge badge = PowerBadge.Find(ch);
			return ((badge != null) && (badge.Layer == RingLayer)) ? badge : null;
		}

		/// <summary>isnazgul</summary>
		public static bool IsNazgul(Character ch) {
			return RingOf(ch) != null;
		}

		public static bool IsLeading(PowerBadge ring) {
			return ring.Def == ThingDef.GetByDefname(LeadingRingDef);
		}

		public static bool IsFirst(Character ch) {
			PowerBadge ring = RingOf(ch);
			return (ring != null) && IsLeading(ring);
		}

		public static string CityOf(PowerBadge ring) {
			return ring.GetTag(cityTK) as string;
		}

		private static int Int(TagHolder holder, TagKey key) {
			object value = holder.GetTag(key);
			return (value != null) ? Convert.ToInt32(value) : 0;
		}

		private static double Time(TagHolder holder, TagKey key) {
			object value = holder.GetTag(key);
			return (value != null) ? Convert.ToDouble(value) : 0;
		}

		public static int KillsOf(Character ch) {
			return Int(ch, killsTK);
		}

		public static int DeathsOn(PowerBadge ring) {
			return Int(ring, deathsTK);
		}

		public static int OnlineMinutesOn(PowerBadge ring) {
			return Int(ring, onlineMinutesTK);
		}

		/// <summary>doneplayerkills</summary>
		public static int KillsWith(PowerBadge ring, Character wearer) {
			return KillsOf(wearer) - Int(ring, baseKillsTK);
		}

		/// <summary>The ring's @equip: who may wear one at all.</summary>
		public static bool CanWear(Character ch) {
			return (ch.Realm == Realm.Mordor) && (((Player) ch).Level >= RealmSettings.instance.ringMinLevel) && !Professions.HasProfession(ch, Professions.Craftsman);
		}

		public static PowerBadge Give(Character ch, Town town) {
			PowerBadge ring = PowerBadge.GiveTo(ch, (town != null) ? RingDef : LeadingRingDef);
			RealmSettings settings = RealmSettings.instance;
			double now = Globals.TimeInSeconds;
			if (town != null) {
				ring.SetTag(cityTK, town.city);
				town.SetMayor(ch);
				ch.SysMessage("Stal jsi se Nositelem Prstenu, Nazgulem. Mas nyni ve sprave mesto " + town.Name);
			} else {
				PowerBadge.SetTitle(ch, FirstTitle);
				ch.SysMessage("Stal jsi se Nositelem Vedouciho Prstenu, Prvnim Nazgulem");
			}
			ring.SetTag(equippedAtTK, now);
			ring.SetTag(lastOnlineTK, now);
			ring.SetTag(baseKillsTK, KillsOf(ch));
			ring.SetTag(nextDecayTK, now + (((town != null) ? settings.ringDecayDays : settings.firstRingDecayDays) * SecondsInDay));
			foreach (TagKey key in victimTKs) {
				ring.RemoveTag(key);
			}
			ch.RemoveTag(questKillsTK);
			ch.RemoveTag(questEndsTK);
			RingTimer timer = new RingTimer();
			ring.AddTimer(ringTimerTK, timer);
			timer.DueInSeconds = SecondsInMinute;
			timer.PeriodInSeconds = SecondsInMinute;
			Uniform.RefreshSoon(ch);
			return ring;
		}

		/// <summary>The end of a ring: its city has no Nazgul and the realm perhaps no First.</summary>
		public static void TakeAway(PowerBadge ring, Character wearer) {
			Uniform.Clear(wearer);
			if ((wearer.Realm == Realm.Mordor) && (wearer is Player player) && ((int) player.SkillRank == 3)) { //#931 - one who could have it
				if (IsLeading(ring)) {
					ChatSystem.Announce(ChatChannel.Mordor, ring.Name, "Muj pan,prvni Nazgul si me nezaslouzi.Muzete me nyni hledat");
				} else {
					ChatSystem.Announce(ChatChannel.Mordor, wearer.Name, "Muj pan z mesta " + CityOf(ring) + " si me nezaslouzi.Muzete me hledat");
				}
			}
			if (IsLeading(ring)) {
				PowerBadge.ClearTitle(wearer);
				RealmOffice office = RealmOffice.For(Realm.Mordor);
				if (office != null) {
					office.holder = null;
				}
			} else {
				Town town = Town.ForCity(CityOf(ring));
				if ((town != null) && (town.mayor == wearer)) {
					town.ClearMayor();
				}
			}
			ring.Delete();
			NazgulGate.RefreshAll();
		}

		/// <summary>mayhavering. The first day is safe.</summary>
		public static bool MayKeep(PowerBadge ring, Character wearer) {
			RealmSettings settings = RealmSettings.instance;
			double now = Globals.TimeInSeconds;
			if ((now - Time(ring, lastOnlineTK)) > (settings.ringMaxAwayDays * SecondsInDay)) {
				return false;
			}
			double minutesWorn = (now - Time(ring, equippedAtTK)) / SecondsInMinute;
			if (minutesWorn < (settings.ringSafeDays * 24 * 60)) {
				return true;
			}
			int online = OnlineMinutesOn(ring);
			if ((minutesWorn - online) >= (settings.ringOfflineRatio * online)) {
				wearer.RedMessage("Prsten te opustil, ma rad vzruseni z akce a tvoje pasivita nebyla pro nej");
				return false;
			}
			int killWeight = IsLeading(ring) ? settings.firstRingKillWeight : settings.ringKillWeight;
			if ((settings.ringDeathWeight * DeathsOn(ring)) >= (killWeight * KillsWith(ring, wearer))) {
				wearer.RedMessage("Prsten te opustil, tve bojove schopnosti neodpovidaji jeho narokum");
				return false;
			}
			return true;
		}

		/// <summary>Every minute: the online count, and what the shrine's timer did every week (5 days for
		/// the First): take kills off and see if the wearer still deserves the ring.</summary>
		internal static void Tick(PowerBadge ring) {
			Character wearer = ring.Cont as Character;
			if (wearer == null) {
				return;
			}
			double now = Globals.TimeInSeconds;
			if (wearer.IsOnline) {
				ring.SetTag(onlineMinutesTK, OnlineMinutesOn(ring) + 1);
				ring.SetTag(lastOnlineTK, now);
			}
			if (now >= Time(ring, nextDecayTK)) {
				RealmSettings settings = RealmSettings.instance;
				wearer.SetTag(killsTK, Math.Max(0, KillsOf(wearer) - settings.ringKillDecay));
				ring.SetTag(nextDecayTK, now + ((IsLeading(ring) ? settings.firstRingDecayDays : settings.ringDecayDays) * SecondsInDay));
				Check(ring, wearer);
			}
		}

		/// <summary>f_shrinePower_chkRing</summary>
		public static void Check(PowerBadge ring, Character wearer) {
			if (!MayKeep(ring, wearer)) {
				wearer.RedMessage("Prsten te opustil");
				TakeAway(ring, wearer);
			}
		}

		/// <summary>isPvPEvaluable, without the checks of systems this engine doesn't have.</summary>
		private static bool Counts(Player victim, Character killer) {
			FlaggedRegion area = RegionEventSystem.AreaOf(victim.Region);
			return (victim.Realm != killer.Realm) && ((area == null) || !area.Flag_Arena) &&
				!Professions.HasProfession(victim, Professions.Craftsman) && ((int) victim.SkillRank >= 3);
		}

		/// <summary>f_fightNazgulKillTest: the last few a Nazgul killed don't count again.</summary>
		private static bool IsNewVictim(PowerBadge ring, Character victim) {
			foreach (TagKey key in victimTKs) {
				if (ring.GetTag(key) == victim) {
					return false;
				}
			}
			for (int i = 0; i < (RememberedVictims - 1); i++) {
				ring.SetTag(victimTKs[i], ring.GetTag(victimTKs[i + 1]));
			}
			ring.SetTag(victimTKs[RememberedVictims - 1], victim);
			return true;
		}

		/// <summary>What a player's death means: to the quest, whose kill it is, to everyone who fought them
		/// (f_countfight), and to the victim's ring. Dying again right after dying counts for nothing,
		/// on either side (i_checkstats_timer).</summary>
		public static void OnDeath(Player victim, Character killer, bool recentlyDead = false) {
			Player killingPlayer = killer as Player;
			if ((killingPlayer != null) && (killingPlayer.GetTag(questEndsTK) != null) && (victim.Level >= RealmSettings.instance.ringMinLevel) &&
					(victim.Realm == Realm.Gondor) && !Professions.HasProfession(victim, Professions.Craftsman)) {
				killingPlayer.SetTag(questKillsTK, Int(killingPlayer, questKillsTK) + 1);
			}
			if (recentlyDead) {
				return;
			}
			foreach (Character fighter in LevelingSystem.FindFighters(victim, killer, false)) {
				if ((fighter is Player) && Counts(victim, fighter)) {
					PowerBadge fightersRing = RingOf(fighter);
					if ((fightersRing == null) || IsNewVictim(fightersRing, victim)) {
						fighter.SetTag(killsTK, KillsOf(fighter) + 1);
					}
				}
			}
			PowerBadge ring = RingOf(victim);
			if (ring != null) {
				if (killingPlayer != null) {
					ring.SetTag(deathsTK, DeathsOn(ring) + 1);
				}
				Check(ring, victim);
			}
		}

		/// <summary>i_nazgul_quest: a ring has to be earned first. True if this player has earned it.</summary>
		public static bool PassedQuest(Player player) {
			RealmSettings settings = RealmSettings.instance;
			if (settings.ringQuestKills <= 0) {
				return true;
			}
			if (Time(player, questEndsTK) < Globals.TimeInSeconds) {
				player.SetTag(questEndsTK, Globals.TimeInSeconds + (settings.ringQuestDays * SecondsInDay));
				player.SetTag(questKillsTK, 0);
				player.SysMessage("Aby jsi mohl ziskat prsten, musis nejprve prokazat oddanost bojem proti gondoru.");
				return false;
			}
			if (Int(player, questKillsTK) < settings.ringQuestKills) {
				player.SysMessage("Musis se vic snazit");
				return false;
			}
			return true;
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class RingTimer : BoundTimer {
		[DeepCopyImplementation, LoadingInitializer]
		public RingTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			NazgulRings.Tick((PowerBadge) cont);
		}
	}

	[ViewableClass]
	public partial class NazgulShrineDef {
	}

	/// <summary>t_shrine_power: a Mordor city's seat and where its ring comes from. i_leading_shrine_power
	/// has no city: it stands in Fela Nazgul and holds the leading ring.</summary>
	[ViewableClass]
	public partial class NazgulShrine {
		private bool IsLeadingShrine {
			get { return this.Def == ThingDef.GetByDefname("i_leading_shrine_power"); }
		}

		public override void On_DClick(AbstractCharacter from) {
			Player player = from as Player;
			if (player == null) {
				return;
			}
			if (this.IsLeadingShrine) {
				this.UseLeading(player);
				return;
			}
			FlaggedRegion area = RegionEventSystem.AreaOf(this.Region);
			if ((area == null) || string.IsNullOrEmpty(area.City) || (area.Realm != Realm.Mordor)) {
				player.SysMessage("Mesto neni pripraveno, kontaktuj GM");
			} else if (RegionEventSystem.AreaOf(player.Region) != area) {
				player.SysMessage("Musis byt v prostoru mesta!");
			} else {
				this.UseCity(player, Town.ForStone(this, area.City));
			}
		}

		private void UseCity(Player player, Town town) {
			town.CheckTimers();
			if (town.GetRank(player) >= Town.RankMayor) {
				D_TownMenu.Show(town, player);
			} else if (town.mayor != null) {
				player.SysMessage("Nositelem prstenu je " + town.mayor.Name + ".");
				this.OfferDonation(player, town);
			} else if (NazgulRings.IsNazgul(player)) {
				player.SysMessage("Jeden prsten uz mas");
			} else if (player.Realm != Realm.Mordor) {
				player.SysMessage("Nejsi hoden stat se nositelem prstenu");
			} else if (!NazgulRings.CanWear(player)) {
				player.SysMessage("Nejsi zpusobily byt vlastnikem prstenu moci");
			} else if (NazgulRings.PassedQuest(player)) {
				NazgulRings.Give(player, town);
			}
		}

		private void OfferDonation(Player player, Town town) {
			if (!player.IsGM) {
				D_TownInput.Ask(player, "Chces-li zadej castku kterou prispejes do pokladny", "0", text => {
					if (int.TryParse(text, out int amount) && (amount > 0) && GoldUtils.Pay(player, amount)) {
						town.treasury += amount;
						player.SysMessage("Zaplatil jsi " + amount + "gp.");
					}
				});
			}
		}

		private void UseLeading(Player player) {
			RealmOffice office = RealmOffice.For(Realm.Mordor) ?? RealmOffice.ForStone(this, Realm.Mordor);
			if (office.holder != null) {
				player.SysMessage("Nositelem prstenu je " + office.holder.Name + ".");
			} else if (!NazgulRings.IsNazgul(player)) {
				player.SysMessage("Nejsi hoden vedouciho prstenu, nejsi Nazgulem");
			} else {
				office.MakeFirst(player);
			}
		}

		public override void On_Destroy() {
			Town town = Town.OfStone(this);
			if (town != null) {
				town.Remove();
			}
			base.On_Destroy();
		}

		public override void Save(SaveStream output) {
			base.Save(output);
			Town town = Town.OfStone(this);
			if (town != null) {
				ObjectSaver.Save(town);
			}
			RealmOffice office = RealmOffice.OfStone(this); //used before the realm's stone ever was
			if (office != null) {
				ObjectSaver.Save(office);
			}
		}
	}

	[ViewableClass]
	public partial class NazgulGateDef {
	}

	/// <summary>i_lordnazguls_gate and i_lordnazguls_gate_out. With no First Nazgul, a Nazgul opens the way
	/// into Fela Nazgul, and up to 3 of Mordor pass with half an hour for the trial; the leading shrine is inside.
	/// The gate is red while there's a First Nazgul, and shut.</summary>
	[ViewableClass]
	public partial class NazgulGate {
		private const int OpenColor = 0x692, ShutColor = 0x689;
		private static readonly TagKey passesTK = TagKey.Acquire("_gatePasses_");
		private static readonly TimerKey resetTK = TimerKey.Acquire("_gateReset_");

		private bool IsWayOut {
			get { return this.Def == ThingDef.GetByDefname("i_lordnazguls_gate_out"); }
		}

		internal static void RefreshAll() {
			foreach (Thing thing in Thing.AllThings) {
				if ((thing is NazgulGate gate) && !gate.IsWayOut) {
					gate.Refresh();
				}
			}
		}

		internal void Refresh() {
			RealmOffice office = RealmOffice.For(Realm.Mordor);
			this.Color = ((office != null) && (office.holder != null)) ? ShutColor : OpenColor;
		}

		internal void Reset() {
			this.RemoveTag(passesTK);
			this.Refresh();
		}

		public override void On_Step(AbstractCharacter stepping, bool repeated, MovementType movementType) {
			base.On_Step(stepping, repeated, movementType);
			Player player = stepping as Player;
			if ((player == null) || repeated) {
				return;
			}
			if (this.IsWayOut) {
				NazgulTrial.End(player);
				player.Go(this.MoreP);
			} else if (player.IsGM) {
				player.Go(this.MoreP);
			} else {
				this.TryPass(player);
			}
		}

		private void TryPass(Player player) {
			RealmOffice office = RealmOffice.For(Realm.Mordor);
			object passedTag = this.GetTag(passesTK);
			int passed = (passedTag != null) ? Convert.ToInt32(passedTag) : 0;
			if (player.Realm != Realm.Mordor) {
				player.RedMessage("Co ty bys tam chtel delat? Co?");
			} else if ((office != null) && (office.holder != null)) {
				player.SysMessage("Dokud vladne Prvni Nazgul, nikdo se nedostane na tato mista!");
			} else if ((passed == 0) && !NazgulRings.IsNazgul(player)) {
				player.SysMessage("Pouze mi nejblizsi sluzebnici s odznakem sve oddanosti mohou aktivovat tento portal");
			} else if (passed >= RealmSettings.instance.trialPasses) {
				player.SysMessage("Tri jsou dostacujici");
			} else {
				this.SetTag(passesTK, passed + 1);
				if (passed == 0) {
					this.AddTimer(resetTK, new NazgulGateReset()).DueInSeconds = RealmSettings.instance.trialMinutes * 60;
				}
				player.Go(this.MoreP);
				NazgulTrial.Begin(player);
			}
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class NazgulGateReset : BoundTimer {
		[DeepCopyImplementation, LoadingInitializer]
		public NazgulGateReset() {
		}

		protected override void OnTimeout(TagHolder cont) {
			((NazgulGate) cont).Reset();
		}
	}

	/// <summary>i_nazggate_timer: half an hour inside Fela Nazgul, told every 10 minutes.</summary>
	[SaveableClass, DeepCopyableClass]
	public class NazgulTrial : BoundTimer {
		private static readonly TimerKey trialTK = TimerKey.Acquire("_nazgulTrial_");
		private const int NoticeSeconds = 600;

		[SaveableData, CopyableData]
		public int secondsLeft;

		[DeepCopyImplementation, LoadingInitializer]
		public NazgulTrial() {
		}

		internal static void Begin(Player player) {
			player.SysMessage("Nyni se ukaze, jsi-li hoden stat se Prvnim, cas Tve zkousky je pul hodiny");
			NazgulTrial trial = new NazgulTrial { secondsLeft = RealmSettings.instance.trialMinutes * 60 };
			player.AddTimer(trialTK, trial);
			trial.DueInSeconds = NoticeSeconds;
			trial.PeriodInSeconds = NoticeSeconds;
		}

		public static void BeginForTest(Player player) {
			Begin(player);
		}

		internal static void End(Player player) {
			Timer trial = player.GetTimer(trialTK);
			if (trial != null) {
				trial.Delete();
			}
		}

		protected override void OnTimeout(TagHolder cont) {
			Player player = (Player) cont;
			this.secondsLeft -= NoticeSeconds;
			if (this.secondsLeft > 0) {
				player.SysMessage("Do konce zkousky zbyva " + (this.secondsLeft / 60) + " minut");
				return;
			}
			this.Delete();
			if (RegionEventSystem.AreaOf(player.Region) == StaticRegion.GetByDefname("a_Fela_Nazgul")) {
				player.SysMessage("Nejsi hoden stat se Prvnim");
				player.Go(TownSetup.CityArea("Barad-dur").P);
			}
		}
	}

	[ViewableClass]
	public partial class NazgulBoardDef {
	}

	/// <summary>t_lordnazgulboard: what the First Nazgul has done, for all to read, which also checks the ring.</summary>
	[ViewableClass]
	public partial class NazgulBoard {
		public override void On_DClick(AbstractCharacter from) {
			RealmOffice office = RealmOffice.For(Realm.Mordor);
			Character first = office?.holder;
			PowerBadge ring = (first != null) ? NazgulRings.RingOf(first) : null;
			if (ring == null) {
				from.SysMessage("V tuto chvili nevladne Prvni Nazgul.");
				return;
			}
			NazgulRings.Check(ring, first);
			if (ring.IsDeleted) {
				from.SysMessage("V tuto chvili nevladne Prvni Nazgul.");
				return;
			}
			from.SysMessage("Marghulovym vudcem vojsk je " + first.Name + ".");
			from.SysMessage("(spolu)zabil " + NazgulRings.KillsWith(ring, first) + " bojovniku druhe strany.");
			from.SysMessage("Byl " + NazgulRings.DeathsOn(ring) + "x smrtelne ranen.");
			if (((Character) from).IsGM) {
				from.SysMessage("Byl " + NazgulRings.OnlineMinutesOn(ring) + " minut online.");
			}
		}
	}

	/// <summary>i_target_nazg's 4: a GM makes a Nazgul of someone, for the shrine's city.</summary>
	public class Targ_MakeNazgul : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			Town town = (Town) parameter;
			if (town.mayor != null) {
				self.SysMessage("Mesto " + town.Name + " jiz Nazgula ma");
			} else if (town.GetRank(targetted) == Town.RankEnemy) {
				self.SysMessage(targetted.Name + " je na cerne listine!");
			} else if (NazgulRings.IsNazgul(targetted)) {
				self.SysMessage(targetted.Name + " jiz je Nazgulem");
			} else if (!(targetted is Player) || !NazgulRings.CanWear(targetted)) {
				self.SysMessage("Nejsi zpusobily byt vlastnikem prstenu moci");
			} else {
				NazgulRings.Give(targetted, town);
			}
			return TargetResult.Done;
		}
	}
}
