/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// #1268 - Erebor's q017 "Alchymista Terlak" (questy/q017_alchymista_terlak.scp and the innkeeper's q017 pages in q007's file).
	/// <para>The innkeeper (after q007's piglet) tells of his friend, the alchemist Terlak, and brews the protection potion against
	/// Terlak's guardian spell: the first one free, every later one for 200 deer eyes (a collector bag) and 10,000 gold. The entry
	/// gate (level 35) wants red and blue gate crystals, 1 + 1 the first time and then as many of each as the run count tag says
	/// (2, 4, 6...); it hurts and refuses anyone without the potion, lets one hero in every 4 hours, and moves to another of four
	/// dungeons after each entry. In Gargamel's dungeon a 4-hour limit runs; a death or the time running out ends the run outside.
	/// The exit gate gives the hero's profession its magic item and raises the run count by 2.</para>
	/// <para>The crystals had no source on Erebor (GM-given); here they drop for a player on this quest from monsters killed in the
	/// four dungeons the gate visits, at a chance on .settings "Questy". They're counted tags, as quest items are.</para>
	/// </summary>
	public static class Q017 {
		public const string EntryGateDef = "i_scout_vstupni_gate";
		public const string ExitGateDef = "i_scout_vystupni_gate";
		public const string GargamelDef = "c_crunch_zoldak";
		public const string CollectorNpcDef = "c_q017_sberatel";
		public const string DeerEyesBag = "i_q017_kolektor_srnky";
		public const int MinLevel = 35, LockoutSeconds = 4 * 3600, RunSeconds = 4 * 3600, PotionPrice = 10000;
		public const int RewardColor = 0x0531;

		/// <summary>Where the entry gate stands, in turn at random (Erebor's dorand 4).</summary>
		public static readonly Point4D[] GateSpots = {
			new Point4D(5286, 730, -18, 10), //Etheria Level 2
			new Point4D(4988, 451, 0, 10), //Dung Snagum level 1
			new Point4D(478, 1524, -8, 2), //Arachnon level 1
			new Point4D(5219, 170, 0, 10) //Mount Dorlas level 2
		};
		public static readonly string[] CrystalRegions = { "a_Etheria_Level_2", "a_Dung_snagum_Level_1", "a_Arachnon_Level_1", "a_Mount_Dorlas_Level_2" };
		public static readonly Point4D DungeonStart = new Point4D(5719, 111, 0, 10);
		public static readonly Point4D FailPoint = new Point4D(1350, 3027, 0, 0); //Erebor's i_alchymistuv_timer morep

		private static readonly TagKey runsTK = TagKey.Acquire("_q017Terlak_"); //qtag q017_terlak
		private static readonly TagKey protectionTK = TagKey.Acquire("_q017Ochrana_"); //qtag q017_terlak_ochrana
		private static readonly TagKey redTK = TagKey.Acquire("_q017RedCrystals_"), blueTK = TagKey.Acquire("_q017BlueCrystals_");
		private static readonly TagKey runLeftTK = TagKey.Acquire("_q017RunLeft_"), returnTK = TagKey.Acquire("_q017Return_");
		internal static readonly TagKey LastEntryTK = TagKey.Acquire("_q017LastEntry_");
		internal static readonly TagKey RewardTK = TagKey.Acquire("_q017Reward_");
		internal static readonly TimerKey RunTimerTK = TimerKey.Acquire("_q017RunTimer_");

		public static QuestJournalEntry Journal { get; private set; }

		public static void Bootstrap() {
			Journal = QuestJournal.Register(new QuestJournalEntry("Alchymista Terlak", 0x3000, GetJournalText, p => GetRuns(p) != 0));
		}

		#region state
		private static int Get(Player player, TagKey tk) {
			object tag = player.GetTag(tk);
			return (tag == null) ? 0 : Convert.ToInt32(tag, CultureInfo.InvariantCulture);
		}

		private static void Set(Player player, TagKey tk, int value) {
			if (value == 0) {
				player.RemoveTag(tk);
			} else {
				player.SetTag(tk, value);
			}
		}

		/// <summary>0 before the first finished run, then 2, 4, 6...: the crystals of each kind the next entry wants.</summary>
		public static int GetRuns(Player player) {
			return Get(player, runsTK);
		}

		internal static void SetRuns(Player player, int runs) {
			Set(player, runsTK, Math.Max(0, runs));
		}

		/// <summary>1 = the innkeeper gave the deer-eyes bag and waits for it, 2 = the potion drunk and working.</summary>
		public static int GetProtection(Player player) {
			return Get(player, protectionTK);
		}

		internal static void SetProtection(Player player, int value) {
			Set(player, protectionTK, value);
		}

		public static int RedCrystals(Player player) {
			return Get(player, redTK);
		}

		public static int BlueCrystals(Player player) {
			return Get(player, blueTK);
		}

		internal static void AddCrystals(Player player, int red, int blue) {
			Set(player, redTK, RedCrystals(player) + red);
			Set(player, blueTK, BlueCrystals(player) + blue);
		}

		public static bool IsOnRun(Player player) {
			return player.GetTag(runLeftTK) != null;
		}

		/// <summary>The quest is under way: the innkeeper's potion asked for or drunk, or a run done.</summary>
		public static bool IsOnQuest(Player player) {
			return (GetProtection(player) != 0) || (GetRuns(player) != 0) || IsOnRun(player);
		}

		/// <summary>Who may start it at the innkeeper: past q007's piglet.</summary>
		public static bool CanHear(Player player) {
			return Q007.GetStage(player) > 6;
		}

		internal static string GetJournalText(Player player) {
			int protection = GetProtection(player);
			if (protection == 1) {
				return JournalTexts[0];
			} else if (protection == 2) {
				return JournalTexts[1];
			} else if (GetRuns(player) != 0) {
				return JournalTexts[2];
			}
			return null;
		}
		#endregion state

		#region the innkeeper's potion
		/// <summary>The innkeeper's button 8: the free first potion, or one for the full bag of deer eyes and 10,000 gold.</summary>
		internal static void BrewPotion(NPC innkeeper, Player player) {
			if (GetProtection(player) == 1) {
				if (!Collectors.IsFull(player, DeerEyesBag)) {
					return;
				}
				if (!GoldUtils.Pay(player, PotionPrice, PaySource.Usual, "")) {
					innkeeper.Say("Nemas u sebe dost penez! Je mi lito, ale zadarmo to tentokrat nebude...");
					return;
				}
				Collectors.FindIn(player, DeerEyesBag).Delete();
				SetProtection(player, 2);
				player.Sound(49);
				innkeeper.Say("Doufam ze ti to alespon chutna...");
			} else if ((GetProtection(player) == 0) && (GetRuns(player) == 0)) { //the free one; Erebor's page 9 shows it only then
				SetProtection(player, 2);
				player.Sound(49);
				innkeeper.Say("Tak a je to ! Chutna vyborne zeano ? Ale to je asi vse, co z toho budes mit...");
			} else {
				return;
			}
			Q007.Reward(player, 200);
			QuestJournal.Report(player, Journal);
		}

		/// <summary>No potion and no bag, or a bag asked for and lost (Erebor's button 9 gave another any time).</summary>
		internal static bool NeedsDeerBag(Player player) {
			int protection = GetProtection(player);
			return (protection == 0) || ((protection == 1) && (Collectors.FindIn(player, DeerEyesBag) == null));
		}

		/// <summary>The innkeeper's button 9: the bag for 200 deer eyes, newbie and fixed as Erebor's ATTR=010|04.</summary>
		internal static void GiveDeerBag(NPC innkeeper, Player player) {
			if (!NeedsDeerBag(player) || !CanHear(player)) {
				return;
			}
			Q006.GiveBag(player, DeerEyesBag);
			SetProtection(player, 1);
			innkeeper.Say("Tady mas ...");
			QuestJournal.Report(player, Journal);
		}
		#endregion the innkeeper's potion

		#region the gates
		/// <summary>Character.On_ItemStep: the entry and exit gates' @step.</summary>
		internal static void OnStep(Player player, Item item) {
			if (item.Def == ThingDef.GetByDefname(EntryGateDef)) {
				TryEnter(item, player);
			} else if (item.Def == ThingDef.GetByDefname(ExitGateDef)) {
				TryLeave(player);
			}
		}

		/// <summary>The entry gate's @step.</summary>
		public static void TryEnter(Item gate, Player player) {
			if (player.Level < MinLevel) {
				StepBack(player);
				player.SysMessage("Tak sem te nikdo nedostane... vydat se po hlave do neznama, to chce hodne zkusenosti a odvahy...");
				return;
			}
			double now = Globals.TimeInSeconds;
			object last = gate.GetTag(LastEntryTK);
			if (last != null) {
				double waitSeconds = Convert.ToDouble(last, CultureInfo.InvariantCulture) + LockoutSeconds - now;
				if (waitSeconds > 0) {
					player.SysMessage("Jiny hrdina plni ukoly, musis jeste pockat " + Remaining(waitSeconds) + ".");
					return;
				}
			}
			int runs = GetRuns(player);
			int needed = Math.Max(1, runs);
			if ((RedCrystals(player) < needed) || (BlueCrystals(player) < needed)) {
				player.SysMessage((runs == 0) ? "Ke vstupu potrebujes 1 cerveny a 1 modry krystal."
					: "Ke vstupu potrebujes " + runs + " cervenych a " + runs + " modrych krystalu.");
				return;
			}
			if (GetProtection(player) != 2) {
				DamageManager.CauseDamage(null, player, DamageType.Irresistable, (runs == 0) ? 30 : 24);
				player.RedMessage("Prudka vlna energie trha tve telo na kusy !");
				return;
			}
			SetProtection(player, 0);
			AddCrystals(player, -needed, -needed);
			player.SysMessage("Krystaly se rozpousteji, citis kolem sebe vir energie...");
			player.SetTag(returnTK, new Point4D(gate.X, gate.Y, gate.Z, gate.M));
			StartRun(player);
			player.Go(DungeonStart);
			gate.SetTag(LastEntryTK, now); //Erebor set it on later runs only, so first-timers could follow each other in
			MoveGate(gate);
		}

		//the gate jumps to another of its four dungeons
		internal static void MoveGate(Item gate) {
			Point4D spot = GateSpots[Globals.dice.Next(GateSpots.Length)];
			gate.P(spot);
		}

		private static readonly int[] stepX = { 0, 1, 1, 1, 0, -1, -1, -1 }, stepY = { -1, -1, 0, 1, 1, 1, 0, -1 };

		//Erebor's src.move(<dir_revert(dir)>): a step back the way they came
		private static void StepBack(Player player) {
			int dir = ((int) player.Direction) & 7;
			player.Go(player.X - stepX[dir], player.Y - stepY[dir], player.Z, player.M);
		}

		internal static string Remaining(double seconds) {
			int minutes = (int) Math.Ceiling(seconds / 60);
			return (minutes >= 60) ? ((minutes / 60) + " h " + (minutes % 60) + " min") : (minutes + " min");
		}

		/// <summary>The exit gate's @step: Terlak's spirit gives the reward.</summary>
		public static void TryLeave(Player player) {
			if (!IsOnRun(player)) {
				return;
			}
			int runs = GetRuns(player);
			SetRuns(player, (runs > 0) ? runs + 2 : 2);
			GiveReward(player);
			player.SysMessage("Diky " + ((player.Gender == Gender.Female) ? "chrabra hrdinko" : "chrabry hrdino") + " za vysvobozeni me duse, prijmi prosim tuto odmenu. Pronesl duch Alchymisty Terlaka...");
			Point4D back = (player.GetTag(returnTK) as Point4D) ?? FailPoint;
			EndRun(player);
			player.Go(back);
			QuestJournal.Report(player, Journal);
		}
		#endregion the gates

		#region the run
		/// <summary>The run's time counts only while the hero is in the game, as Erebor's timer item didn't tick for someone logged out.</summary>
		internal static void StartRun(Player player) {
			SetSecondsLeft(player, RunSeconds);
			Q017RunTimer timer = new Q017RunTimer();
			player.AddTimer(RunTimerTK, timer); //replaces a leftover one
			timer.DueInSeconds = Q017RunTimer.Tick;
			timer.PeriodInSeconds = Q017RunTimer.Tick;
		}

		internal static void EndRun(Player player) {
			player.RemoveTag(runLeftTK);
			player.RemoveTag(returnTK);
			player.DeleteTimer(RunTimerTK);
		}

		internal static int SecondsLeft(Player player) {
			return Get(player, runLeftTK);
		}

		internal static void SetSecondsLeft(Player player, int seconds) {
			player.SetTag(runLeftTK, seconds); //0 still means on a run, about to end
		}

		/// <summary>Out of time: back outside, the run lost.</summary>
		internal static void TimeOut(Player player) {
			EndRun(player);
			GoToFailPoint(player);
			player.RedMessage("Ve stanovenem limitu jsi " + ((player.Gender == Gender.Female) ? "nestihla" : "nestihl") + " dojit na konec tve cesty, muzes se pokusit znovu.");
		}

		/// <summary>Erebor's e_scout_alchymista @death: the ghost goes outside, the run is lost.</summary>
		internal static void OnDeath(Player player) {
			if (!IsOnRun(player)) {
				return;
			}
			player.RemoveTag(runLeftTK);
			player.RemoveTag(returnTK);
			Q017RunTimer timer = player.GetTimer(RunTimerTK) as Q017RunTimer;
			if (timer == null) {
				timer = new Q017RunTimer();
				player.AddTimer(RunTimerTK, timer);
			}
			timer.died = true; //ported a moment later, not in the middle of the death
			timer.PeriodInSeconds = -1;
			timer.DueInSeconds = 1;
		}

		internal static void GoToFailPoint(Player player) {
			Map map = Map.GetMap(FailPoint.M);
			player.Go(FailPoint.X, FailPoint.Y, (sbyte) WorldSpots.StandZ(map, FailPoint.X, FailPoint.Y), FailPoint.M);
		}
		#endregion the run

		#region rewards
		/// <summary>Erebor's <profession>ova_odmena: the item, its magic (imi_value numbers), its name.</summary>
		internal sealed class Reward {
			internal readonly string profession, itemDef, name, onlyFor;
			internal readonly int[] values; //property, value, property, value...

			internal Reward(string profession, string itemDef, string name, string onlyFor, params int[] values) {
				this.profession = profession;
				this.itemDef = itemDef;
				this.name = name;
				this.onlyFor = onlyFor;
				this.values = values;
			}
		}

		internal static readonly Reward[] Rewards = {
			new Reward("profession_warrior", "i_obsidian_heater_shield", "Stit obrance", "Stit urceny pouze pro wary!", 134, 10, 136, 5),
			new Reward("profession_mage", "i_hat_wizards", "Klobouk maga", "Klobouk urcen pouze pro magy!", 25, 10, 62, 10),
			new Reward("profession_priest", "i_obsidian_heater_shield", "Stit lecitele", "Stit urcen pouze pro priesty!", 17, 10, 71, 5),
			new Reward("profession_necromant", "i_robe", "Roba temneho", "Roba urcena pouze pro necromancery!", 16, 5, 17, 5, 25, 10),
			new Reward("profession_thief", "i_obsidian_metal_shield", "Stit neplechy", "Stit urcen pouze pro zlodeje!", 131, 5, 26, 15),
			new Reward("profession_mystic", "i_necklace_gold", "Nahrdelnik mysli", "Nahrdelnik urcen pouze pro mystiky!", 72, 15, 43, 10),
			new Reward("profession_craftsman", "i_pickaxe", "Adamantiovy krumpac", "Krumpac urcen pouze pro crafty!", 45, 10),
			new Reward("profession_ranger", "i_obsidian_chainmail_gloves", "Rukavice presneho zasahu", "Rukavice urceny pouze pro rangery!", 21, 5, 27, 10, 31, 10),
			new Reward("profession_shaman", "i_obsidian_ringmail_leggings", "Voodoo kalhoty", "Kalhoty urceny pouze pro shamany!", 17, 10, 72, 10)
		};

		internal static Reward RewardOf(ProfessionDef profession) {
			return (profession == null) ? null : Array.Find(Rewards, r => ProfessionDef.GetByDefname(r.profession) == profession);
		}

		internal static Reward RewardOf(Item item) {
			string profession = item.GetTag(RewardTK) as string;
			return (profession == null) ? null : Array.Find(Rewards, r => r.profession == profession);
		}

		/// <summary>The hero's profession's item, in their pack; none for someone with no profession.</summary>
		public static Item GiveReward(Player player) {
			Reward reward = RewardOf(player.Profession);
			if (reward == null) {
				return null;
			}
			Item item = (Item) ThingDef.GetByDefname(reward.itemDef).Create(player.Backpack);
			for (int i = 0; i + 1 < reward.values.Length; i += 2) {
				MagicItems.SetValue(item, reward.values[i], reward.values[i + 1]);
			}
			MagicItems.Finish(item);
			item.Name = reward.name;
			item.Color = RewardColor;
			item.SetTag(RewardTK, reward.profession);
			CraftingSkillDef.SetCrafter(item, player); //Erebor's craftedby: no magic-item exchange takes it
			return item;
		}

		/// <summary>t_scout_*_odmena's @equip: its profession only, level 35, one at a time. Erebor took the item off again (or the
		/// other one, for a second); here it isn't put on.</summary>
		internal static bool RefusesEquip(Item item, Character ch) {
			Reward reward = RewardOf(item);
			if ((reward == null) || !(ch is Player player)) {
				return false;
			}
			if (player.Profession != ProfessionDef.GetByDefname(reward.profession)) {
				player.RedMessage(reward.onlyFor);
				return true;
			}
			if (player.Level < MinLevel) {
				player.RedMessage("Musis mit level vyssi nez 35!");
				return true;
			}
			foreach (Thing worn in player.VisibleEquip) {
				if ((worn != item) && (worn is Item other) && (RewardOf(other) != null)) {
					player.RedMessage("Nemuzes nosit dve odmeny soucasne.");
					return true;
				}
			}
			return false;
		}
		#endregion rewards

		#region crystals
		/// <summary>The crystals' source (none on Erebor): a monster killed in one of the gate's four dungeons, by a player on the
		/// quest, may carry one; red or blue at even odds.</summary>
		internal static void OnKillCredit(Player player, NPC victim) {
			if ((player == null) || !IsOnQuest(player) || IsOnRun(player)) {
				return;
			}
			FlaggedRegion area = RegionEventSystem.AreaOf(victim.Region);
			if ((area == null) || (Array.IndexOf(CrystalRegions, area.Defname) < 0)) {
				return;
			}
			if (Globals.dice.Next(100) >= QuestPortSettings.instance.terlakCrystalChance) {
				return;
			}
			bool red = Globals.dice.Next(2) == 0;
			AddCrystals(player, red ? 1 : 0, red ? 0 : 1);
			player.SysMessage("Na tele " + victim.Name + " jsi " + ((player.Gender == Gender.Female) ? "nasla" : "nasel") + (red ? " cerveny" : " modry") + " krystal bran.");
		}
		#endregion crystals

		/// <summary>Erebor's d_alchymista_terlak texts.</summary>
		internal static readonly string[] JournalTexts = {
			"Hospodsky ti vypravel pribeh o davnem priteli, Terlakovi. Drive spolu pripravili lektvar, ktery te muze ochranit proti urcitemu druhu magie. Tentokrat chce za sve sluzby zaplatit 10 000 gp a navic mu musis donest 200 srncich oci.",
			"Hospodsky ti uvaril ochranny lektvar a ty jsi jej <?src.sex(vypil,vypila)?>... Ted jiz jenom zjistit, kde takovou ochranu upotrebit. Jedinym voditkem je ti Terlak, ktery takove ochranne kouzlo jiste znal. Hospodsky tvrdi, ze opilec mel schuzku s terlakem kdesi ve skalach severne od Belfalske zatoky.",
			"<?src.sex(Nasel,Nasla)?> jsi misto, kde Gargamel - lovec lebek - brutalne zavrazdil opilce a drzel ve sve moci dusi Alchymisty Terlaka. Po vysvobozeni duse jsi <?src.sex(\"byl obdarovan\",\"byla obdarovana\")?> magickym predmetem. Zrejme se Terlakovi precijen podarilo najit spravne prisady pro svuj lektvar... Hospodsky by byl asi velmi prekvapeny, kdyby se to dozvedel..."
		};
	}

	/// <summary>#1268 - Erebor's i_alchymistuv_timer: the run's 4 hours of play, told now and then, and a dead hero's way out.</summary>
	[SaveableClass, DeepCopyableClass]
	public class Q017RunTimer : BoundTimer {
		internal const int Tick = 30;

		[SaveableData, CopyableData]
		public bool died;

		[LoadingInitializer, DeepCopyImplementation]
		public Q017RunTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			Player player = cont as Player;
			if (player == null) {
				this.Delete();
				return;
			}
			if (this.died) {
				Q017.GoToFailPoint(player);
				player.SysMessage((player.Gender == Gender.Female) ? "Nezvladla jsi tento narocny ukol, jdi tedy!" : "Nezvladl jsi tento narocny ukol, jdi tedy!");
				this.Delete();
				return;
			}
			if (player.Flag_Disconnected) {
				return; //logged out: the clock stands, as Erebor's timer item didn't tick offline
			}
			int left = Q017.SecondsLeft(player) - Tick;
			if (left <= 0) {
				Q017.TimeOut(player);
				return;
			}
			Q017.SetSecondsLeft(player, left);
			//Erebor told the seconds every 30 s; here every 10 minutes, and each half minute of the last two
			if ((left <= 120) || ((left % 600) < Tick)) {
				player.SysMessage("Zbyva ti " + Q017.Remaining(left) + ".");
			}
		}
	}

	/// <summary>#1268 - .settings "Questy": the ported quests' own numbers.</summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class QuestPortSettings : SettingsMetaCategory {
		[SavedMember]
		public static QuestPortSettings instance = new QuestPortSettings();

		[LoadingInitializer]
		public QuestPortSettings() {
		}

		/// <summary>q017: the percent chance a monster killed in the entry gate's four dungeons carries a gate crystal, for a player on
		/// the quest (Erebor had no source for them).</summary>
		[SaveableData]
		public int terlakCrystalChance = 3;

		/// <summary>#1307 - the cheque the collector Sberatel gives for one of Terlak's artifacts (Erebor's 50 000).</summary>
		[SaveableData]
		public int terlakCollectorPrice = 50000;
	}

	/// <summary>#1268 - the collector Sberatel (spk_q017_sberatel): Erebor's bought only the OLD versions of Terlak's items. #1307, the
	/// user's call: he buys the current ones as Erebor did the old: the player's own profession's artifact from the pack for a
	/// cheque (terlakCollectorPrice), and the next run's entry asks 2 crystals fewer.</summary>
	public sealed class Q017CollectorSpeech : AbstractSpeech {
		private static readonly string[] greetings = { "hi", "ahoj", "zdrav", "cau" };
		private static readonly string[] trade = { "vymen", "vymena", "vymenit", "zamen", "artefakt" };
		private static readonly string[] agree = { "ano", "prodam", "beru", "plati" };
		internal static readonly TagKey OfferTK = TagKey.Acquire("_q017CollectorOffer_");
		internal const double OfferSeconds = 30;

		public new static void Bootstrap() {
			new Q017CollectorSpeech("spk_q017_sberatel").Register();
		}

		private Q017CollectorSpeech(string defname)
			: base(defname) {
		}

		protected override SpeechResult Handle(AbstractCharacter listener, SpeechArgs speechArgs) {
			return this.TryHandle(listener, speechArgs);
		}

		protected override SpeechResult TryHandle(AbstractCharacter listener, SpeechArgs speechArgs) {
			NPC npc = listener as NPC;
			Player player = speechArgs.Speaker as Player;
			string speech = speechArgs.Speech;
			if ((npc == null) || (player == null) || (speech == null) || !QuestNpc.IsCloseEnough(npc, player)) {
				return SpeechResult.IgnoredOrActedUpon;
			}
			if (SkillTeacherSpeech.ContainsAny(speech, trade)) {
				Offer(npc, player);
			} else if (SkillTeacherSpeech.ContainsAny(speech, agree) && (player.GetTag(OfferTK) is double at) && (Globals.TimeInSeconds - at < OfferSeconds)) {
				player.RemoveTag(OfferTK);
				Buy(npc, player);
			} else if (SkillTeacherSpeech.ContainsAny(speech, greetings)) {
				npc.Say("Zdravim, sbiram artefakty od alchymisty Terlaka! Nemas nejaky na VYMENu ?");
			}
			return SpeechResult.IgnoredOrActedUpon;
		}

		/// <summary>#1307 - what he'd give for the player's artifact; an "ano" within OfferSeconds sells it.</summary>
		internal static void Offer(NPC npc, Player player) {
			if (OwnReward(player) == null) {
				npc.Say("Nevidim u tebe zadnou vec kterou potrebuji");
				return;
			}
			player.SetTag(OfferTK, Globals.TimeInSeconds);
			npc.Say("Ano to je ono! Dam ti za to sek na " + Math.Max(1, QuestPortSettings.instance.terlakCollectorPrice) + " zlatych. Prodas? (ANO)");
		}

		/// <summary>#1307 - the player's own profession's artifact from the pack for a cheque; the next entry asks 2 crystals fewer, but
		/// never fewer than after a first finished run (2: 0 would be the quest never begun, its free potion and cheap entry again).
		/// False when there's none to buy.</summary>
		internal static bool Buy(NPC npc, Player player) {
			Item artifact = OwnReward(player);
			if (artifact == null) {
				npc.Say("Nevidim u tebe zadnou vec kterou potrebuji");
				return false;
			}
			npc.Say("Obchod plati.");
			artifact.Delete();
			CashiersItem.CreateCheck(player, Math.Max(1, QuestPortSettings.instance.terlakCollectorPrice));
			Q017.SetRuns(player, Math.Max(2, Q017.GetRuns(player) - 2));
			return true;
		}

		//the player's own profession's artifact in the pack (not worn), as Erebor's findid on the pack
		internal static Item OwnReward(Player player) {
			Q017.Reward own = Q017.RewardOf(player.Profession);
			if ((own == null) || (player.Backpack == null)) {
				return null;
			}
			foreach (AbstractItem item in player.Backpack) {
				if ((item is Item mine) && (Q017.RewardOf(mine) == own)) {
					return mine;
				}
			}
			return null;
		}
	}
}
