using System;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1116 - Erebor's training arena (systems/arena.scp's e_arena and arenaleave, questy/arena_crunch.scp,
	/// questy/arena_chars.scp). The Umbar Company's agent (c_arena_obchodnik) takes 200 gold for "ARENA" and 30 seconds
	/// later sends a Gondor player to the Gondorska bojova arena (plane 5), a Mordor one to the Mordorska (plane 55). Going in
	/// keeps the player's karma, murders, fame, realm, deaths and kill counts; leaving puts them all back - through the
	/// exit gate (I_gate_arena_vystup, which then sends them home) or by being moved out of the arena some other way.
	/// While inside, Recall, Mark and Gate Travel don't work, and other players' bodies can't be looted or carved.
	/// Fixed from Erebor: a saved 0 is put back too (Erebor's "if (TAG(x))" skipped it, so arena murders stuck to anyone
	/// who came in with none), and a player of neither realm is refused before paying (Erebor took the gold and sent
	/// them nowhere). Erebor's net was "a double-click outside an arena ends it and sends you home"; here leaving the
	/// arena area ends it. The agent's lines on seeing a new player aren't ported (no such NPC hook here). The older
	/// "pits" arena (spk_go_arena, i_delay_arena, I_gate_arena_vstup to 5521,3701,35, an area that was no arena) and
	/// tim/turnaj.scp's telepads were dead on Erebor and aren't ported.</summary>
	public static class Arena {
		private static readonly TagKey inArenaTK = TagKey.Acquire("_inArena_");
		private static readonly TagKey karmaTK = TagKey.Acquire("_arenaKarma_");
		private static readonly TagKey murdersTK = TagKey.Acquire("_arenaMurders_");
		private static readonly TagKey fameTK = TagKey.Acquire("_arenaFame_");
		private static readonly TagKey realmTK = TagKey.Acquire("_arenaRealm_");
		private static readonly TagKey deathsTK = TagKey.Acquire("_arenaDeaths_");
		private static readonly TagKey playerKillsTK = TagKey.Acquire("_arenaPlayerKills_");
		private static readonly TagKey monsterKillsTK = TagKey.Acquire("_arenaMonsterKills_");
		private static readonly TagKey[] snapshot = { karmaTK, murdersTK, fameTK, realmTK, deathsTK, playerKillsTK, monsterKillsTK };

		internal const int Price = 200, CountdownSeconds = 30;
		internal const string GondorArena = "a_Gondorska_bojova_arena", MordorArena = "a_Mordorska_bojova_arena", ExitGateDef = "I_gate_arena_vystup";
		private const int RecallSpell = 32, MarkSpell = 45, GateSpell = 52;

		public static bool IsIn(Character ch) {
			return ch.HasTag(inArenaTK);
		}

		/// <summary>The arena a realm's player goes to: Gondor's for Gondor, Mordor's for Mordor (Erebor's realm 1 and 2).</summary>
		internal static FlaggedRegion ArenaFor(Realm realm) {
			string name = (realm == Realm.Gondor) ? GondorArena : ((realm == Realm.Mordor) ? MordorArena : null);
			return (name == null) ? null : StaticRegion.GetByDefname(name) as FlaggedRegion;
		}

		/// <summary>The enter block: keep what the arena could change, then mark them.</summary>
		internal static void Enter(Player player) {
			if (!IsIn(player)) {
				player.SetTag(karmaTK, (int) player.Karma);
				player.SetTag(murdersTK, Notoriety.GetMurderCount(player));
				player.SetTag(fameTK, (int) player.Fame);
				player.SetTag(realmTK, (int) player.Realm);
				player.SetTag(deathsTK, PlayerRecord.Deaths(player));
				player.SetTag(playerKillsTK, NazgulRings.KillsOf(player));
				player.SetTag(monsterKillsTK, PlayerRecord.MonsterKills(player));
				player.SetTag(inArenaTK, true);
			}
			player.SysMessage(Loc<ArenaLoc>.Get(player.Language).Entered(player));
		}

		private static int Kept(Player player, TagKey key) {
			return Convert.ToInt32(player.GetTag(key) ?? 0, CultureInfo.InvariantCulture);
		}

		/// <summary>arenaleave: everything back, the crime forgiven; `home` sends them home as the exit gate does.</summary>
		public static void Leave(Player player, bool home) {
			if (!IsIn(player)) {
				return;
			}
			player.RemoveTag(inArenaTK);
			Notoriety.ClearCriminal(player);
			player.Karma = (short) Kept(player, karmaTK);
			Notoriety.SetMurderCount(player, Kept(player, murdersTK));
			player.Fame = (short) Kept(player, fameTK);
			player.Realm = (Realm) Kept(player, realmTK);
			PlayerRecord.Restore(player, Kept(player, deathsTK), Kept(player, monsterKillsTK));
			NazgulRings.SetKills(player, Kept(player, playerKillsTK));
			foreach (TagKey key in snapshot) {
				player.RemoveTag(key);
			}
			player.SysMessage(Loc<ArenaLoc>.Get(player.Language).Left(player));
			if (home) {
				player.Go(player.Home ?? PlayerCreation.startingPosition);
			}
		}

		/// <summary>Leaving the arena area some other way than the gate ends it there.</summary>
		internal static void OnAreaChanged(Character ch) {
			if ((ch is Player player) && IsIn(player)) {
				FlaggedRegion area = RegionEventSystem.AreaOf(player.Region);
				if ((area == null) || !area.Flag_Arena) {
					Leave(player, false);
				}
			}
		}

		/// <summary>The exit gate's @STEP.</summary>
		internal static void OnStep(Character ch, Item item) {
			if ((ch is Player player) && (item.Def == ThingDef.GetByDefname(ExitGateDef))) {
				Leave(player, true);
			}
		}

		/// <summary>e_arena's @spellcast: Recall, Mark and Gate Travel.</summary>
		internal static bool RefusesSpell(Character caster, SpellDef spell) {
			if (IsIn(caster) && ((spell.Id == RecallSpell) || (spell.Id == MarkSpell) || (spell.Id == GateSpell))) {
				caster.SysMessage(Loc<ArenaLoc>.Get(caster.Language).NoSpell);
				return true;
			}
			return false;
		}

		/// <summary>e_arena's @itemPICKUP_PACK: nothing from another player's body. Null when it's allowed.</summary>
		internal static string RefusesLooting(Character picker, AbstractItem item) {
			if ((picker != null) && IsIn(picker) && (item.TopObj() is Corpse corpse) && (corpse.Owner != null) && (corpse.Owner != picker)) {
				return Loc<ArenaLoc>.Get(picker.Language).NoLooting;
			}
			return null;
		}

		/// <summary>e_arena's @itemtargon_item: no blade on another player's body.</summary>
		internal static bool RefusesCarving(Character carver, Corpse corpse) {
			if (IsIn(carver) && (corpse.Owner != null) && (corpse.Owner != carver)) {
				carver.SysMessage(Loc<ArenaLoc>.Get(carver.Language).NoCarving);
				return true;
			}
			return false;
		}

		/// <summary>spk_crunch_arenaVchod's ARENA: the fee, then the countdown (i_portitko_arena).</summary>
		internal static void Buy(NPC agent, Player player) {
			ArenaLoc texts = Loc<ArenaLoc>.Get(player.Language);
			FlaggedRegion arena = ArenaFor(player.Realm);
			if ((arena == null) || !GoldUtils.Pay(player, Price)) {
				agent.Say(texts.NoMoney);
				return;
			}
			ArenaCountdown timer = new ArenaCountdown();
			timer.left = CountdownSeconds;
			timer.destination = arena.P;
			player.AddTimer(ArenaCountdown.Key, timer);
			timer.PeriodInSeconds = 1;
			timer.DueInSeconds = 1;
		}
	}

	/// <summary>i_portitko_arena: "Cas do presunu: n s" each second, then into the arena.</summary>
	[SaveableClass, DeepCopyableClass]
	public class ArenaCountdown : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_arenaCountdown_");

		[SaveableData, CopyableData]
		public int left;

		[SaveableData, CopyableData]
		public Point4D destination;

		[LoadingInitializer, DeepCopyImplementation]
		public ArenaCountdown() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if (!(cont is Player player) || player.IsDeleted) {
				return;
			}
			if (this.left > 0) {
				player.SysMessage(string.Format(Loc<ArenaLoc>.Get(player.Language).Countdown, this.left));
				this.left--;
				return;
			}
			player.DeleteTimer(Key);
			Arena.Enter(player);
			player.Go(this.destination);
		}
	}

	/// <summary>spk_crunch_arenaVchod (the agent) and spk_crunch_strazceAreny (the arena guard), in Erebor's order.</summary>
	public sealed class ArenaSpeech : AbstractSpeech {
		private readonly bool agent;

		public new static void Bootstrap() {
			new ArenaSpeech("spk_crunch_arenaVchod", true).Register();
			new ArenaSpeech("spk_crunch_strazceAreny", false).Register();
		}

		private ArenaSpeech(string defname, bool agent)
			: base(defname) {
			this.agent = agent;
		}

		protected override SpeechResult Handle(AbstractCharacter listener, SpeechArgs speechArgs) {
			return this.TryHandle(listener, speechArgs);
		}

		private static bool Says(string speech, params string[] words) {
			foreach (string word in words) {
				if (speech.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0) {
					return true;
				}
			}
			return false;
		}

		protected override SpeechResult TryHandle(AbstractCharacter listener, SpeechArgs speechArgs) {
			NPC npc = listener as NPC;
			Player player = speechArgs.Speaker as Player;
			string speech = speechArgs.Speech;
			if ((npc == null) || (player == null) || (speech == null)) {
				return SpeechResult.IgnoredOrActedUpon;
			}
			ArenaLoc texts = Loc<ArenaLoc>.Get(player.Language);
			if (this.agent) {
				if (Says(speech, "hi", "dobry den", "ahoj")) {
					npc.Say(texts.AgentHello);
				} else if (Says(speech, "arena")) {
					Arena.Buy(npc, player);
				}
			} else if (Says(speech, "hi", "ahoj", "zdravim", "cus", "hoj")) {
				npc.Say(string.Format(texts.GuardHello, player.Name));
			} else if (Says(speech, "help", "pomoc")) {
				npc.Say(texts.GuardHelp);
			} else if (Says(speech, "arena")) {
				npc.Say(texts.GuardEntry);
			} else if (Says(speech, "exit", "vychod")) {
				npc.Say(texts.GuardExit);
			}
			return SpeechResult.IgnoredOrActedUpon;
		}
	}

	public class ArenaLoc : CompiledLocStringCollection<ArenaLoc> {
		public string EnteredMale = "Vstoupil jsi do areny - pro jeji opusteni pouzij prislusnou gatu, bude ti vracena karma.";
		public string EnteredFemale = "Vstoupila jsi do areny - pro jeji opusteni pouzij prislusnou gatu, bude ti vracena karma.";
		public string LeftMale = "Odesel jsi z areny - byla ti vracena karma.";
		public string LeftFemale = "Odesla jsi z areny - byla ti vracena karma.";
		public string NoSpell = "V arene nelze pouzit.";
		public string NoLooting = "Tady nesmis okradat cizi tela.";
		public string NoCarving = "Tady nesmis rozrezavat cizi tela.";
		public string Countdown = "Cas do presunu: {0} s";
		public string AgentHello = "Zdravim! Mas zajem podivat se do areny? Poplatek je 200 zlatych. Mas-li je, staci rict ARENA!";
		public string NoMoney = "Nemas u sebe 200 zlatych. Je mi lito, ale nejsme charita.";
		public string GuardHello = "Vitej v arene {0}, potrebujes pomoci?";
		public string GuardHelp = "Zde muzes trenovat svoje dovednosti v boji proti svym protivnikum. Potrebujes vedet, kde je vychod? Nebo chces do areny?";
		public string GuardEntry = "Vchod do trenovaci areny je ta modra brana.";
		public string GuardExit = "Tou cervenou branou.";

		internal string Entered(Character ch) {
			return ch.IsFemale ? this.EnteredFemale : this.EnteredMale;
		}

		internal string Left(Character ch) {
			return ch.IsFemale ? this.LeftFemale : this.LeftMale;
		}
	}
}
