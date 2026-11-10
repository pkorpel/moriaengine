using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#892 - the Rings of Power of systems/player_functions.scp. A ring makes its wearer a Nazgul,
	/// who rules the ring's city (Town.mayor), or, the leading ring, the First Nazgul. It stays only while
	/// its wearer earns it: mayhavering. A lost ring lies at its shrine until a new one is given out there, the shrine
	/// making that new one since the ring's state starts over anyway (#910, LostRings).</summary>
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
		internal static readonly TagKey questKillTimesTK = TagKey.Acquire("_nazgulQuestKillTimes_");
		private static readonly TagKey[] victimTKs = {
			TagKey.Acquire("_ringVictim0_"), TagKey.Acquire("_ringVictim1_"), TagKey.Acquire("_ringVictim2_")
		};
		private static readonly TimerKey ringTimerTK = TimerKey.Acquire("_ringOfPower_");
		internal static TimerKey RingTimerTK => ringTimerTK;

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

		/// <summary>#1116 - the arena puts the count back as it was.</summary>
		internal static void SetKills(Character ch, int kills) {
			ch.SetTag(killsTK, kills);
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
			LostRings.TakeUp((town != null) ? town.stone : LostRings.LeadingShrine()); //#910 - the one lying at the shrine
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
			ch.RemoveTag(questKillTimesTK);
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
			if (!LostRings.LayAtShrine(ring)) {
				ring.Delete();
			}
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
				DelayedMessages.SendRed(wearer, "Prsten te opustil, ma rad vzruseni z akce a tvoje pasivita nebyla pro nej"); //#901
				return false;
			}
			int killWeight = IsLeading(ring) ? settings.firstRingKillWeight : settings.ringKillWeight;
			if ((settings.ringDeathWeight * DeathsOn(ring)) >= (killWeight * KillsWith(ring, wearer))) {
				DelayedMessages.SendRed(wearer, "Prsten te opustil, tve bojove schopnosti neodpovidaji jeho narokum");
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
			if ((killingPlayer != null) && (killingPlayer.Realm == Realm.Mordor) && (victim.Level >= RealmSettings.instance.ringMinLevel) &&
					(victim.Realm == Realm.Gondor) && !Professions.HasProfession(victim, Professions.Craftsman)) {
				List<double> times = RecentQuestKills(killingPlayer);
				times.Add(Globals.TimeInSeconds);
				killingPlayer.SetTag(questKillTimesTK, string.Join(";", times.Select(time => time.ToString("R", CultureInfo.InvariantCulture))));
			}
			if (recentlyDead) {
				return;
			}
			foreach (Character fighter in LevelingSystem.FindFighters(victim, killer, false)) {
				if ((fighter is Player) && PvpRules.IsEvaluable(victim, fighter)) {
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
				if (!ring.IsDeleted && (ring.Cont == victim)) {
					NazgulFell.Reset(ring, victim); //f_nazghul_default_mount: a death brings the horse back
				}
			}
		}

		/// <summary>i_nazgul_quest: a ring has to be earned first. True if this player has earned it.</summary>
		/// <summary>#1009 - the user's call: Erebor's i_nazgul_quest counted from a click on the shrine for 3 days and then wiped
		/// the count. Here a Mordor player's qualifying kills are always remembered, and those of the last days are what counts.</summary>
		internal static List<double> RecentQuestKills(Player player) {
			List<double> recent = new List<double>();
			double oldest = Globals.TimeInSeconds - (RealmSettings.instance.ringQuestDays * SecondsInDay);
			if (player.GetTag(questKillTimesTK) is string times) {
				foreach (string time in times.Split(';')) {
					double when = double.Parse(time, CultureInfo.InvariantCulture);
					if (when >= oldest) {
						recent.Add(when);
					}
				}
			}
			return recent;
		}

		public static bool PassedQuest(Player player) {
			RealmSettings settings = RealmSettings.instance;
			if (settings.ringQuestKills <= 0) {
				return true;
			}
			int kills = RecentQuestKills(player).Count;
			if (kills >= settings.ringQuestKills) {
				return true;
			}
			player.SysMessage((kills == 0) ? "Aby jsi mohl ziskat prsten, musis nejprve prokazat oddanost bojem proti gondoru." : "Musis se vic snazit");
			return false;
		}
	}

	/// <summary>#909 - Erebor's nazghulfell (player_functions.scp): the First Nazgul, of fame over 9900, switches the
	/// uniform's horse to a fell beast (i_mt_drake's look in 0x692) and back, once in 5 minutes; a death brings the horse
	/// back. The user's call: the same horse changes its look, as the uniform does (#908); the numbers are on .settings.</summary>
	public static class NazgulFell {
		private static readonly TagKey fellTK = TagKey.Acquire("_nazghulFell_");
		private static readonly TagKey nextSwitchTK = TagKey.Acquire("_nextFellSwitch_");

		public static bool IsOn(PowerBadge ring) {
			return (ring != null) && NazgulRings.IsLeading(ring) && (ring.GetTag(fellTK) != null);
		}

		internal static void Reset(PowerBadge ring, Character wearer) {
			if (ring.GetTag(fellTK) != null) {
				ring.RemoveTag(fellTK);
				Uniform.RefreshSoon(wearer);
			}
		}

		[SteamFunction]
		public static void NazghulFell(Character self) {
			if (!self.IsPlayer) {
				return;
			}
			PowerBadge ring = NazgulRings.RingOf(self);
			if ((ring == null) || !NazgulRings.IsLeading(ring)) {
				self.RedMessage("Nejsi Prvni Nazhgul! Nemas pravo privolat Fella.");
			} else if (self.Fame <= RealmSettings.instance.fellFame) {
				self.RedMessage("Pouze jako " + (self.IsFemale ? "Lady" : "Lord") + " muzes privolat Fella.");
			} else if ((ring.GetTag(nextSwitchTK) != null) && (Globals.TimeInSeconds < Convert.ToDouble(ring.GetTag(nextSwitchTK), CultureInfo.InvariantCulture))) { //a whole number loads as an int
				self.RedMessage("Tento prikaz jeste nemuzes pouzit.");
			} else {
				if (ring.GetTag(fellTK) == null) {
					self.RedMessage("Privolavam Fella.");
					ring.SetTag(fellTK, true);
				} else {
					self.RedMessage("Odvolavam Fella.");
					ring.RemoveTag(fellTK);
				}
				ring.SetTag(nextSwitchTK, Globals.TimeInSeconds + (RealmSettings.instance.fellCooldownMinutes * 60));
				Uniform.Refresh(self);
			}
		}
	}

	/// <summary>#910 - the user's call (2026-11-09): a ring that leaves its wearer lies at its shrine in four fire columns,
	/// as Erebor's @dropon_ground (f_ring_column, i_column_ring: i_fire_column in colour 1, 2 tiles each way), so everyone
	/// sees the ring is free. It can't be picked up; a double click on it is the shrine's (its kill quest first, then the
	/// ring); a new ring given out takes it and its fire away. The leading ring lies at the First's shrine.</summary>
	public static class LostRings {
		private static readonly TagKey lyingTK = TagKey.Acquire("_lyingRing_");
		private static readonly TagKey shrineTK = TagKey.Acquire("_ringShrine_");
		private static readonly int[][] columnAt = { new[] { 2, 0 }, new[] { 0, 2 }, new[] { -2, 0 }, new[] { 0, -2 } };

		private static TagKey ColumnTK(int i) {
			return TagKey.Acquire("_ringColumn_" + i.ToString(CultureInfo.InvariantCulture) + "_");
		}

		/// <summary>The ring to its shrine's tile with its fire; false when there's no shrine (the ring is deleted then).</summary>
		internal static bool LayAtShrine(PowerBadge ring) {
			Item shrine = NazgulRings.IsLeading(ring) ? LeadingShrine() : Town.ForCity(NazgulRings.CityOf(ring))?.stone;
			if ((shrine == null) || shrine.IsDeleted) {
				return false;
			}
			TakeUp(shrine); //an older one there
			ring.DeleteTimer(NazgulRings.RingTimerTK);
			ring.P(shrine.X, shrine.Y, shrine.Z, shrine.M);
			ring.SetAttribute(ItemAttributes.attr_move_never, true); //on the ground now: no picking up, no decay
			ring.SetTag(shrineTK, shrine);
			shrine.SetTag(lyingTK, ring);
			for (int i = 0; i < columnAt.Length; i++) {
				Item column = (Item) ThingDef.GetByDefname("i_column_ring").Create(shrine.X + columnAt[i][0], shrine.Y + columnAt[i][1], shrine.Z, shrine.M); //t_script: a sight, no burning
				column.Color = 1;
				column.SetAttribute(ItemAttributes.attr_move_never, true);
				ring.SetTag(ColumnTK(i), column);
			}
			return true;
		}

		private static NazgulShrine leadingShrine;

		/// <summary>i_leading_shrine_power in Fela Nazgul (not the realm office's stone, i_lordnazgulsstone in Barad-dur).</summary>
		public static NazgulShrine LeadingShrine() {
			if ((leadingShrine == null) || leadingShrine.IsDeleted) {
				leadingShrine = null;
				ThingDef def = ThingDef.GetByDefname("i_leading_shrine_power");
				foreach (Thing thing in Thing.AllThings) {
					if ((thing is NazgulShrine shrine) && (shrine.Def == def)) {
						leadingShrine = shrine;
						break;
					}
				}
			}
			return leadingShrine;
		}

		/// <summary>The ring lying at this shrine, or null.</summary>
		public static PowerBadge LyingAt(Item shrine) {
			PowerBadge ring = shrine?.GetTag(lyingTK) as PowerBadge;
			return ((ring == null) || ring.IsDeleted || (ring.Cont != null)) ? null : ring;
		}

		/// <summary>The shrine a lying ring belongs to, or null (not lying).</summary>
		internal static Item ShrineOf(PowerBadge ring) {
			return (ring.Cont == null) ? (ring.GetTag(shrineTK) as Item) : null;
		}

		public static int ColumnsOf(PowerBadge ring) {
			int count = 0;
			for (int i = 0; i < columnAt.Length; i++) {
				if ((ring.GetTag(ColumnTK(i)) is Item column) && !column.IsDeleted) {
					count++;
				}
			}
			return count;
		}

		/// <summary>The lying ring and its fire gone.</summary>
		internal static void TakeUp(Item shrine) {
			PowerBadge ring = LyingAt(shrine);
			if (shrine != null) {
				shrine.RemoveTag(lyingTK);
			}
			if (ring == null) {
				return;
			}
			for (int i = 0; i < columnAt.Length; i++) {
				if (ring.GetTag(ColumnTK(i)) is Item column) {
					column.Delete();
				}
			}
			ring.Delete();
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
			if ((player == null) || MysticHypnosis.RefusesMenu(player)) { //#1070 - not while possessing
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
			LostRings.TakeUp(this); //#910 - its lying ring and fire with it
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
