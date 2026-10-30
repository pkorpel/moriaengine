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
	/// #833 - Erebor's Expert quest (questy/expert.scp), the way a Novice becomes an Expert (Player.SkillRank 1).
	/// <para>A player of level 3+ double-clicks a start stone (i_expert_quest_first; Erebor's stood in Gondor at
	/// 2771,2761 and Mordor at 3723,2953) and is taken to a free quest site (i_expert_quest_second, an invisible stone
	/// GM-linked to the start stone, in the a_expert cave). Six clue signs GM-linked to the site (i_expert_quest_sign)
	/// get a random arrangement of clues about five names; a quest master (c_quest_expert) appears and, through its
	/// gump, asks three questions whose answers follow from the clues. Three right answers make the player an Expert.
	/// A wrong answer, giving up, the 60-minute limit or leaving the site's region ends the quest; except for leaving,
	/// the player is taken back to Gondor or Mordor.</para>
	/// <para>The pieces are world objects, placed and linked by GMs as in Erebor (double-click a site or a sign as a
	/// GM, target what it belongs to), or by scripts/Setup.</para>
	/// <para>Differences from Erebor: only the player on the quest gets the master's gump (in Erebor anyone talking
	/// to it could answer); the signs stay visible (Erebor created them invisible, which no player can read); the
	/// sign gump stays open after a step; the quest-stone statistics (questMenu_questStone) aren't kept; the master is
	/// invulnerable.</para>
	/// </summary>
	public static class ExpertQuest {
		public const string StartStoneDef = "i_expert_quest_first";
		public const string SiteDef = "i_expert_quest_second";
		public const string SignDef = "i_expert_quest_sign";
		public const string MasterDef = "c_quest_expert";
		public const int MinLevel = 3;
		public const int TimeLimitMinutes = 60;
		public const int MasterTalkDistance = 4;

		/// <summary>Erebor's exq_jmeno - the five names of the puzzle.</summary>
		public static readonly string[] Names = { "Burin", "Tilar", "Armel", "Koren", "Sarod" };

		private static readonly TagKey sitesTK = TagKey.Acquire("_expertQuestSites_");
		private static readonly TagKey signsTK = TagKey.Acquire("_expertQuestSigns_");
		private static readonly TagKey playerTK = TagKey.Acquire("_expertQuestPlayer_");
		private static readonly TagKey masterTK = TagKey.Acquire("_expertQuestMaster_");
		private static readonly TagKey minutesTK = TagKey.Acquire("_expertQuestMinutes_");
		private static readonly TagKey namesTK = TagKey.Acquire("_expertQuestNames_");
		private static readonly TagKey clueTK = TagKey.Acquire("_expertQuestClue_");
		private static readonly TagKey siteOfMasterTK = TagKey.Acquire("_expertQuestSite_");
		private static readonly TagKey questionTK = TagKey.Acquire("_expertQuestQuestion_");
		internal static readonly TimerKey timerTK = TimerKey.Acquire("_expertQuestTimer_");

		//called by ClassManager on every startup/recompile: Erebor's items carry their behavior in their own
		//triggers, which the converter can't port - the C# trigger groups are attached to the converted defs here
		public static void Init() {
			QuestNpc.Attach(StartStoneDef, SingletonScript<E_ExpertQuestStartStone>.Instance);
			QuestNpc.Attach(SiteDef, SingletonScript<E_ExpertQuestSite>.Instance);
			QuestNpc.Attach(SignDef, SingletonScript<E_ExpertQuestSign>.Instance);
			QuestNpc.Attach(MasterDef, SingletonScript<E_ExpertQuestMaster>.Instance);
		}

		#region links
		public static List<Item> GetSites(Item startStone) {
			return GetLinked(startStone, sitesTK);
		}

		public static List<Item> GetSigns(Item site) {
			return GetLinked(site, signsTK);
		}

		private static List<Item> GetLinked(Item holder, TagKey tk) {
			List<Item> list = holder.GetTag(tk) as List<Item>;
			if (list == null) {
				list = new List<Item>();
				holder.SetTag(tk, list);
			}
			list.RemoveAll(i => (i == null) || i.IsDeleted);
			return list;
		}

		/// <summary>Erebor's @create lines live here: the converter drops them, and everything placed, by the setup or
		/// by a GM, comes through a link. i_expert_quest_second is hidden from players.</summary>
		public static void LinkSite(Item startStone, Item site) {
			site.SetAttribute(ItemAttributes.attr_invis, true);
			List<Item> sites = GetSites(startStone);
			if (!sites.Contains(site)) {
				sites.Add(site);
			}
		}

		public static void LinkSign(Item site, Item sign) {
			sign.Name = "Cedule s napisem";
			List<Item> signs = GetSigns(site);
			if (!signs.Contains(sign)) {
				signs.Add(sign);
			}
		}

		public static Player GetQuestPlayer(Item site) {
			Player player = site.GetTag(playerTK) as Player;
			return ((player == null) || player.IsDeleted) ? null : player;
		}

		public static Item GetSiteOf(NPC master) {
			Item site = master.GetTag(siteOfMasterTK) as Item;
			return ((site == null) || site.IsDeleted) ? null : site;
		}

		/// <summary>#839 - the journal's page. Erebor's journal had none for this quest.</summary>
		internal static string GetJournalText(Player player) {
			if (player.SkillRank >= SkillRank.Expert) {
				return "<src.sex(\"Prosel\",\"Prosla\")> jsi zkouskou v jeskyni a <src.sex(\"stal\",\"stala\")> ses expertem.";
			}
			if (player.GetTag(questionTK) != null) {
				return "Jsi v jeskyni zkousky. Precti si cedule s indiciemi na stenach, pak pozdrav Mistra a odpovez na jeho tri otazky. Na vyreseni mas omezeny cas a jeskyni nesmis opustit.";
			}
			return null;
		}

		public static int GetQuestion(Player player) {
			object tag = player.GetTag(questionTK);
			return (tag == null) ? 0 : Convert.ToInt32(tag, CultureInfo.InvariantCulture);
		}

		/// <summary>The clue a sign shows: Erebor's six f_expertSignTxt_N lines, and the name it's about.</summary>
		public static bool TryGetClue(Item sign, out int clue, out string name) {
			string tag = sign.GetTag(clueTK) as string;
			clue = -1;
			name = null;
			if (tag == null) {
				return false;
			}
			string[] parts = tag.Split(',');
			clue = int.Parse(parts[0], CultureInfo.InvariantCulture);
			name = parts[1];
			return true;
		}

		/// <summary>The right answer to question 0, 1 or 2 of the quest running at `site`.</summary>
		public static string GetAnswer(Item site, int question) {
			int[] names = GetNameOrder(site);
			return Names[names[(question == 0) ? 4 : 3] - 1]; //Erebor's f_expert_checkAnswer: randar_4, randar_3, randar_3
		}

		private static int[] GetNameOrder(Item site) {
			string tag = (string) site.GetTag(namesTK);
			return Array.ConvertAll(tag.Split(','), s => int.Parse(s, CultureInfo.InvariantCulture));
		}
		#endregion links

		#region start and end
		/// <summary>Erebor's i_expert_quest_first @userdclick.</summary>
		public static void TryStart(Item startStone, Player player) {
			ExpertQuestLoc loc = Loc<ExpertQuestLoc>.Get(player.Language);
			if (player.SkillRank >= SkillRank.Expert) {
				player.SysMessage(loc.AlreadyWalkedThisPath);
				return;
			}
			Item freeSite = null;
			foreach (Item site in GetSites(startStone)) {
				if (GetQuestPlayer(site) == null) {
					freeSite = site;
					break;
				}
			}
			if (freeSite == null) {
				player.SysMessage(loc.WaitUntilFree);
			} else if (player.Level < MinLevel) {
				player.SysMessage(string.Format(CultureInfo.InvariantCulture, loc.LevelTooLow, MinLevel));
			} else {
				Begin(freeSite, player);
			}
		}

		/// <summary>Erebor's f_expert_quest_begin: the player to the site, the clues shuffled onto its signs, a fresh master.</summary>
		public static void Begin(Item site, Player player) {
			player.Go(site.X, site.Y, site.Z, site.M);
			player.SetTag(questionTK, 0);
			site.SetTag(playerTK, player);
			site.SetTag(minutesTK, TimeLimitMinutes);
			site.AddTimer(timerTK, new ExpertQuestTimer()).DueInSeconds = 60;

			int[] signOrder = RandomOrder(6);
			int[] nameOrder = RandomOrder(5);
			site.SetTag(namesTK, string.Join(",", nameOrder));
			List<Item> signs = GetSigns(site);
			foreach (Item sign in signs) {
				sign.RemoveTag(clueTK);
			}
			for (int clue = 0; clue < 6; clue++) {
				int signIndex = signOrder[clue] - 1;
				if (signIndex < signs.Count) {
					int nameSlot = (clue < 5) ? clue : 3; //Erebor's 6th sign speaks of the same one as the 4th
					signs[signIndex].SetTag(clueTK, clue.ToString(CultureInfo.InvariantCulture) + "," + Names[nameOrder[nameSlot] - 1]);
				}
			}

			RemoveMaster(site);
			NPC master = (NPC) ThingDef.GetByDefname(MasterDef).Create(site.X, site.Y, site.Z, site.M);
			master.Flag_Invulnerable = true; //one attempt has one master: killing it would leave the player stuck until the limit
			master.SetTag(siteOfMasterTK, site);
			site.SetTag(masterTK, master);
			QuestJournal.Report(player, QuestJournal.Expert);
		}

		/// <summary>Erebor's f_expert_quest_end: the site is free again and its master gone; with `sendHome` the
		/// player is taken back to Mordor or Gondor (not when they already left the site's region).</summary>
		public static void End(Item site, bool sendHome) {
			Player player = GetQuestPlayer(site);
			site.RemoveTag(playerTK);
			site.DeleteTimer(timerTK);
			RemoveMaster(site);
			if (player != null) {
				player.RemoveTag(questionTK);
				if (sendHome) {
					bool mordor = (player.Realm == Realm.Mordor) || (player.Realm == Realm.MordorOutcast);
					int x = mordor ? 3735 : 2757, y = mordor ? 2962 : 2757;
					Map map = Map.GetMap(0);
					player.Go(x, y, WorldSpots.StandZ(map, x, y), 0);
				}
			}
		}

		private static void RemoveMaster(Item site) {
			NPC master = site.GetTag(masterTK) as NPC;
			if ((master != null) && !master.IsDeleted) {
				master.Delete();
			}
			site.RemoveTag(masterTK);
		}

		/// <summary>Erebor's i_expert_quest_second @timer, once a minute.</summary>
		public static void OnMinute(Item site) {
			Player player = GetQuestPlayer(site);
			if (player == null) {
				End(site, false);
				return;
			}
			if (player.Region != site.Region) {
				End(site, false); //left the cave - nowhere to take them back from
				return;
			}
			int minutes = Convert.ToInt32(site.GetTag(minutesTK), CultureInfo.InvariantCulture) - 1;
			site.SetTag(minutesTK, minutes);
			if (minutes > 0) {
				player.SysMessage(string.Format(CultureInfo.InvariantCulture, Loc<ExpertQuestLoc>.Get(player.Language).MinutesLeft, minutes));
				site.AddTimer(timerTK, new ExpertQuestTimer()).DueInSeconds = 60;
			} else {
				End(site, true);
			}
		}

		/// <summary>Erebor's f_expert_checkAnswer, for the name the player picked in the master's gump.</summary>
		internal static void Answer(NPC master, Player player, string name) {
			Item site = GetSiteOf(master);
			if ((site == null) || (GetQuestPlayer(site) != player)) {
				return;
			}
			int question = GetQuestion(player);
			bool female = player.Gender == Gender.Female;
			if (!string.Equals(name, GetAnswer(site, question), StringComparison.OrdinalIgnoreCase)) {
				master.Say((female ? "Selhala" : "Selhal") + " jsi. Quest ukoncen.");
				End(site, true);
				return;
			}
			if (question < 2) {
				player.SetTag(questionTK, question + 1);
				master.Dialog(player, SingletonScript<D_ExpertQuestMaster>.Instance, master, true);
				return;
			}
			if (player.SkillRank < SkillRank.Expert) {
				player.SkillRank = SkillRank.Expert;
				QuestJournal.Report(player, QuestJournal.Expert);
			}
			master.Say("Uspesne jsi se " + (female ? "stala" : "stal") + " expertem.");
			End(site, true);
			QuestNpc.CelebrateRank(player);
		}

		/// <summary>Erebor's f_expertMistrDClick behind the master's speech and @UserDClick. False if `player` isn't on
		/// this master's quest; they get nothing here.</summary>
		internal static bool TryTalk(NPC master, Player player) {
			Item site = GetSiteOf(master);
			if ((site == null) || (GetQuestPlayer(site) != player)) {
				return false;
			}
			if (Point2D.GetSimpleDistance(master, player) > MasterTalkDistance) {
				player.SysMessage(Loc<ExpertQuestLoc>.Get(player.Language).ComeCloser);
			} else {
				master.Dialog(player, SingletonScript<D_ExpertQuestMaster>.Instance, master, false);
			}
			return true;
		}

		internal static void GiveUp(NPC master, Player player) {
			Item site = GetSiteOf(master);
			if ((site == null) || (GetQuestPlayer(site) != player)) {
				return;
			}
			master.Say("Snad budes mit priste vice stesti. Ukazu ti vychod..");
			End(site, true);
		}

		//Erebor's random_array: 1..n in random order
		private static int[] RandomOrder(int n) {
			int[] order = new int[n];
			for (int i = 0; i < n; i++) {
				order[i] = i + 1;
			}
			for (int i = n - 1; i > 0; i--) {
				int j = Globals.dice.Next(i + 1);
				int swap = order[i];
				order[i] = order[j];
				order[j] = swap;
			}
			return order;
		}
		#endregion start and end
	}

	/// <summary>#833 - the quest site's once-a-minute countdown (Erebor's i_expert_quest_second @timer).</summary>
	[SaveableClass]
	public class ExpertQuestTimer : BoundTimer {
		[LoadingInitializer]
		public ExpertQuestTimer() {
		}

		protected sealed override void OnTimeout(TagHolder cont) {
			Item site = cont as Item;
			if ((site != null) && !site.IsDeleted) {
				ExpertQuest.OnMinute(site);
			}
		}
	}

	/// <summary>#833 - i_expert_quest_first @userdclick: start the Expert quest.</summary>
	public class E_ExpertQuestStartStone : CompiledTriggerGroup {
		public TriggerResult On_DClick(Item self, Character clicker) {
			Player player = clicker as Player;
			if (player != null) {
				ExpertQuest.TryStart(self, player);
			}
			return TriggerResult.Cancel;
		}
	}

	/// <summary>#833 - i_expert_quest_second @userdclick (GMs): target the start stone this quest site belongs to.</summary>
	public class E_ExpertQuestSite : CompiledTriggerGroup {
		public TriggerResult On_DClick(Item self, Character clicker) {
			Player gm = clicker as Player;
			if ((gm != null) && gm.IsGM) {
				gm.SysMessage("Target the Expert quest start stone to link this site to.");
				gm.Target(SingletonScript<Targ_ExpertQuestLinkSite>.Instance, self);
			}
			return TriggerResult.Cancel;
		}
	}

	/// <summary>#833 - i_expert_quest_sign @userdclick: GMs target the quest site the sign belongs to, players read it.</summary>
	public class E_ExpertQuestSign : CompiledTriggerGroup {
		public TriggerResult On_DClick(Item self, Character clicker) {
			Player player = clicker as Player;
			if (player == null) {
				return TriggerResult.Cancel;
			}
			if (player.IsGM) {
				player.SysMessage("Target the Expert quest site to link this sign to.");
				player.Target(SingletonScript<Targ_ExpertQuestLinkSign>.Instance, self);
			} else {
				int clue;
				string name;
				if (ExpertQuest.TryGetClue(self, out clue, out name)) {
					self.Dialog(player, SingletonScript<D_ExpertQuestSign>.Instance, clue, name);
				}
			}
			return TriggerResult.Cancel;
		}
	}

	/// <summary>#833 - c_quest_expert @UserDClick: the same as greeting it. Anyone else gets the usual paperdoll.</summary>
	public class E_ExpertQuestMaster : CompiledTriggerGroup {
		public TriggerResult On_DClick(NPC self, Character clicker) {
			Player player = clicker as Player;
			return ((player != null) && ExpertQuest.TryTalk(self, player)) ? TriggerResult.Cancel : TriggerResult.Continue;
		}
	}

	public class Targ_ExpertQuestLinkSite : CompiledTargetDef {
		protected override TargetResult On_TargonThing(Player self, Thing targetted, object parameter) {
			Item stone = targetted as Item;
			if ((stone == null) || (stone.Def != ThingDef.GetByDefname(ExpertQuest.StartStoneDef))) {
				self.SysMessage("That is not an Expert quest start stone.");
				return TargetResult.Done;
			}
			ExpertQuest.LinkSite(stone, (Item) parameter);
			self.SysMessage("OK");
			return TargetResult.Done;
		}
	}

	public class Targ_ExpertQuestLinkSign : CompiledTargetDef {
		protected override TargetResult On_TargonThing(Player self, Thing targetted, object parameter) {
			Item site = targetted as Item;
			if ((site == null) || (site.Def != ThingDef.GetByDefname(ExpertQuest.SiteDef))) {
				self.SysMessage("That is not an Expert quest site.");
				return TargetResult.Done;
			}
			ExpertQuest.LinkSign(site, (Item) parameter);
			self.SysMessage("OK");
			return TargetResult.Done;
		}
	}

	/// <summary>#833 - the quest master's speech (Erebor's spk_quest_expert): greeting it opens its gump, for the
	/// player on its quest, within 4 tiles.</summary>
	public sealed class ExpertQuestMasterSpeech : AbstractSpeech {
		public static void Bootstrap() {
			new ExpertQuestMasterSpeech("spk_quest_expert").Register();
		}

		private ExpertQuestMasterSpeech(string defname)
			: base(defname) {
		}

		protected override SpeechResult Handle(AbstractCharacter listener, SpeechArgs speechArgs) {
			return this.TryHandle(listener, speechArgs);
		}

		protected override SpeechResult TryHandle(AbstractCharacter listener, SpeechArgs speechArgs) {
			NPC master = listener as NPC;
			Player player = speechArgs.Speaker as Player;
			if ((master == null) || (player == null) || !IsGreeting(speechArgs.Speech)) {
				return SpeechResult.IgnoredOrActedUpon;
			}
			ExpertQuest.TryTalk(master, player);
			return SpeechResult.IgnoredOrActedUpon;
		}

		//Erebor: on=*hi* / *ano* / vim* / ahoj / *zdravim*
		private static bool IsGreeting(string speech) {
			if (speech == null) {
				return false;
			}
			string s = speech.Trim();
			return (s.IndexOf("hi", StringComparison.OrdinalIgnoreCase) >= 0) || (s.IndexOf("ano", StringComparison.OrdinalIgnoreCase) >= 0) ||
				s.StartsWith("vim", StringComparison.OrdinalIgnoreCase) || s.Equals("ahoj", StringComparison.OrdinalIgnoreCase) ||
				(s.IndexOf("zdravim", StringComparison.OrdinalIgnoreCase) >= 0);
		}
	}

	/// <summary>#833 - Erebor's d_expert_sign: one clue, its name in Erebor's spaced capitals on top.</summary>
	public class D_ExpertQuestSign : CompiledGumpDef {
		private const int Hue = 2305;

		//Erebor's f_expertSignTxt_00..5: (x, y, text) per line, {0} = the name
		private static readonly object[][][] clues = {
			new[] { L(63, 70, "Trpaslik {0}, ktery se zivi"), L(52, 98, "obchodem s drahym kamenim, dava"), L(88, 126, "v boji prednost sekere.") },
			new[] { L(63, 70, "Tento hurecky minstrel nema"), L(112, 98, "rad mece ani kopi.") },
			new[] { L(63, 70, "Elf {0} se nezivi ani tancem,"), L(66, 98, "ani bojem, ale je velmi zrucny"), L(96, 126, "v zachazeni s lukem.") },
			new[] { L(47, 70, "Mec vyhovuje bojovnikovi, ktery se"), L(112, 98, "nejmenuje {0}.") },
			new[] { L(76, 70, "Hobit {0} take netancuje,"), L(121, 98, "ani nefarmari.") },
			new[] { L(93, 70, "{0} nebojuje nozem.") },
		};

		private static object[] L(int x, int y, string text) {
			return new object[] { x, y, text };
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			int clue = Convert.ToInt32(args[0], CultureInfo.InvariantCulture);
			string name = (string) args[1];
			this.SetLocation(220, 200);
			this.AddBackground(20, 20, 300, 160, 9300);
			this.AddLabel(123, 35, Hue, string.Join(" ", name.ToUpperInvariant().ToCharArray()));
			foreach (object[] line in clues[clue]) {
				this.AddLabel((int) line[0], (int) line[1], Hue, string.Format(CultureInfo.InvariantCulture, (string) line[2], name));
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
		}
	}

	/// <summary>#833 - Erebor's d_expert_mistr: the master's portrait, a greeting page and the question page with the
	/// five names to pick from. args: the master NPC, whether the last answer was right.</summary>
	public class D_ExpertQuestMaster : CompiledGumpDef {
		private const int GiveUpButton = 10;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Player player = (Player) sendTo;
			bool lastWasRight = (bool) args[1];
			int question = ExpertQuest.GetQuestion(player);
			bool female = player.Gender == Gender.Female;

			this.SetLocation(5, 5);
			this.AddPage(0);
			this.AddBackground(20, 200, 640, 350, 2620);
			this.AddImage(30, 210, 0x3182);
			this.AddImage(40, 215, 0x3001);
			this.AddLabel(220, 210, 52, "Mistr");
			this.AddButton(160, 330, 0x988, 0x988, 0, GumpButtonType.Page, 2);
			this.AddLabel(215, 331, 1152, "Otazky");
			this.AddButton(490, 330, 0x988, 0x988, GiveUpButton, GumpButtonType.Reply, 0);
			this.AddLabel(556, 331, 142, "Konec");

			this.AddPage(1);
			string greeting;
			if (question == 0) {
				string worthy = female ? "hodna" : "hoden";
				greeting = "Vitam te tu! Zde se ukaze, jestli jsi " + worthy + " povyseni sveho titulu. Jsi pouhy novic, ktery se snazi dostat mezi zkusene bojovniky. V teto jeskyni zazijes svou prvni zkousku, pri ktere muzes prokazat, ze jsi " + worthy +
					" dalsiho postupu. Po stenach jeskyne jsou poveseny cedule s indiciemi, ktere ti napovi odpovedi na me tri otazky. Az budes " + (female ? "pripravena" : "pripraven") + ", muzu ti polozit prvni z nich.";
			} else if (lastWasRight) {
				greeting = "Spravne! Muzeme pristoupit k dalsi otazce?";
			} else {
				greeting = "Znas odpoved na mou dalsi otazku?";
			}
			this.AddHtml(170, 230, 440, 100, greeting, false, true); //without a scrollbar the client draws it black on this dark gump

			this.AddPage(2);
			string[] questions = { "Ma prvni otazka zni:<br><br>Kdo se zivi bojem?", "Ma druha otazka zni:<br><br>Kdo tancuje?", "Ma treti a posledni otazka zni:<br><br>Kdo bojuje kopim?" };
			this.AddHtml(170, 230, 440, 100, questions[Math.Min(question, 2)], false, true);
			for (int i = 0; i < ExpertQuest.Names.Length; i++) {
				this.AddButton(25, 395 + (20 * i), 0xfa5, 0xfa7, i + 1, GumpButtonType.Reply, 0);
				this.AddLabel(60, 398 + (20 * i), 152, ExpertQuest.Names[i]);
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = gi.Cont as Player;
			NPC master = args[0] as NPC;
			if ((player == null) || (master == null) || master.IsDeleted) {
				return;
			}
			int button = gr.PressedButton;
			if (button == GiveUpButton) {
				ExpertQuest.GiveUp(master, player);
			} else if ((button >= 1) && (button <= ExpertQuest.Names.Length)) {
				ExpertQuest.Answer(master, player, ExpertQuest.Names[button - 1]);
			}
		}
	}

	public class ExpertQuestLoc : CompiledLocStringCollection<ExpertQuestLoc> {
		public string AlreadyWalkedThisPath = "You have already walked this path.";
		public string WaitUntilFree = "You must wait until the quest is free.";
		public string LevelTooLow = "You must be at least level {0}.";
		public string MinutesLeft = "You have {0} more minutes to solve the quest.";
		public string ComeCloser = "There will be no shouting. You must come closer.";
	}
}
