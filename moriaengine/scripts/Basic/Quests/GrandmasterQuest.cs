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
	/// #833 - Erebor's Grandmaster quest (questy/velmistr.scp, the q000 line), the way a Master becomes a Grandmaster
	/// (Player.SkillRank 3). The player's progress is one number, Erebor's q000_velmistr:
	/// <para>0 not started, a Master of level 12+ can start it with Elqira; 1 she lost her daughters east of the Ice Plains;
	/// 2 found Elvira first, spellbound; 3 found Elmira first, she asks to find Elvira; 4 found Elvira after Elmira; 5 Elmira
	/// asked for the root of Este; 6 pulled the root in the Ered Mithrin cave, once its guards are dead; 7 gave it to Elvira;
	/// 8 Elvira told of Rinald and his name; 9 killed Rinald in his cave, which gives the key; 10 opened Elmira's cage;
	/// 11 told Elqira, who rewards the player; 12 (optional) other NPCs said the envoy looks for them; 13 the envoy of
	/// Gondor or Mordor made them a Grandmaster.</para>
	/// <para>Everything the player reads is Erebor's Czech. The pieces are placed by .SetupGrandmasterQuest.</para>
	/// </summary>
	public static class GrandmasterQuest {
		public const string ElqiraDef = "c_q000_elqira";
		public const string ElmiraDef = "c_q000_elmira";
		public const string ElviraDef = "c_q000_elvira";
		public const string RinaldDef = "c_q000_rinald";
		public const string GondorEnvoyDef = "c_q30_zmocnenecg";
		public const string MordorEnvoyDef = "c_q30_zmocnenecm";
		public const string RootDef = "i_q000_rootOfEste";
		public const string AlarmDef = "i_q000_rootOfEste_Alarm";
		public const string CageDoorDef = "i_q000_cageDoor";
		public const string CaveEntranceDef = "i_q000_caveEntrance";
		public const string CaveExitDef = "i_q000_caveExit";
		public const string CaveRegion = "a_Slunecni_jeskyne"; //Erebor's tag.q000_dungeon, GROUP=vm quest
		//The dark cave. Erebor's a_Ered_Mithrinska_Jeskyne is on mapplane 2, by its coordinates another map file: this engine
		//reads map0.mul for every plane, so there it's open sea. We have the tunnels around a_Ered_Mithrin_vstup, on the
		//surface map, from this mouth on the east side of the mountains.
		public static readonly Point3D RootCaveMouth = new Point3D(2919, 725, 45);
		public static readonly ImmutableRectangle RootCave = new ImmutableRectangle(2790, 690, 2925, 810);
		public const int MinLevel = 12;
		public const int CaveMinutes = 60;
		public const int GuardRange = 10;

		internal static readonly TimerKey caveTimerTK = TimerKey.Acquire("_q000CaveTimer_");
		private static readonly TagKey stageTK = TagKey.Acquire("_q000Stage_");
		private static readonly TagKey caveMinutesTK = TagKey.Acquire("_q000CaveMinutes_");
		private static readonly TagKey caveReturnTK = TagKey.Acquire("_q000CaveReturn_");
		private static readonly TagKey rinaldTK = TagKey.Acquire("_q000Rinald_");
		private static readonly TagKey lastAlarmTK = TagKey.Acquire("_q000LastAlarm_");

		//called by ClassManager on every startup/recompile, see ExpertQuest.Init
		public static void Init() {
			foreach (string npc in new[] { ElqiraDef, ElmiraDef, ElviraDef, RinaldDef, GondorEnvoyDef, MordorEnvoyDef }) {
				ExpertQuest.Attach(npc, SingletonScript<E_Q000Npc>.Instance);
			}
			ExpertQuest.Attach(RootDef, SingletonScript<E_Q000Root>.Instance);
			ExpertQuest.Attach(AlarmDef, SingletonScript<E_Q000Alarm>.Instance);
			ExpertQuest.Attach(CageDoorDef, SingletonScript<E_Q000CageDoor>.Instance);
			ExpertQuest.Attach(CaveEntranceDef, SingletonScript<E_Q000CaveEntrance>.Instance);
			ExpertQuest.Attach(CaveExitDef, SingletonScript<E_Q000CaveExit>.Instance);
		}

		public static int GetStage(Player player) {
			object tag = player.GetTag(stageTK);
			return (tag == null) ? 0 : Convert.ToInt32(tag, CultureInfo.InvariantCulture);
		}

		public static void SetStage(Player player, int stage) {
			player.SetTag(stageTK, stage);
		}

		/// <summary>Erebor's qtag(q000_velmistr,n,38): the next stage, announced by the journal's scroll.</summary>
		public static void Advance(Player player, int stage) {
			SetStage(player, stage);
			QuestJournal.Report(player, QuestJournal.Grandmaster);
		}

		internal static string GetJournalText(Player player) {
			int stage = GetStage(player);
			return (stage == 0) ? null : GrandmasterQuestJournal.Texts[stage - 1];
		}

		/// <summary>Erebor's f_q000_playerIsReady.</summary>
		public static bool IsReady(Player player) {
			return (GetStage(player) == 0) && (player.Level >= MinLevel) && (player.SkillRank == SkillRank.Master);
		}

		/// <summary>Erebor's f_q000_canBePromotedVM.</summary>
		public static bool CanBePromoted(Player player) {
			int stage = GetStage(player);
			return (stage == 11) || (stage == 12);
		}

		public static bool IsPriest(Player player) {
			return player.Profession == Professions.Priest;
		}

		public static bool IsShaman(Player player) {
			return player.Profession == Professions.Shaman;
		}

		internal static string Sex(Player player, string male, string female) {
			return (player.Gender == Gender.Female) ? female : male;
		}

		internal static void Reward(Player player, int experience) {
			LevelingSystem.AddExperience(player, experience);
		}

		#region talking
		/// <summary>The NPC's @UserDClick or speech. `clicked` matters to Elvira, who takes the root of Este only by hand.</summary>
		internal static void Talk(NPC npc, Player player, bool clicked) {
			if (player.Flag_Dead) {
				return;
			}
			string defname = ((NPCDef) npc.Def).PrettyDefname;
			if (defname == ElqiraDef) {
				TalkUnlessUpset(npc, player, SingletonScript<D_Q000Elqira>.Instance);
			} else if (defname == ElmiraDef) {
				TalkUnlessUpset(npc, player, SingletonScript<D_Q000Elmira>.Instance);
			} else if (defname == ElviraDef) {
				TalkToElvira(npc, player, clicked);
			} else if (defname == RinaldDef) {
				TalkToRinald(npc, player);
			} else {
				TalkToEnvoy(npc, player, defname == GondorEnvoyDef);
			}
		}

		private static void TalkUnlessUpset(NPC npc, Player player, QuestDialogDef dialog) {
			if (QuestNpc.IsUpset(npc)) {
				QuestNpc.ReactUpset(npc);
			} else {
				npc.Dialog(player, dialog, npc);
			}
		}

		private static readonly string[] spellbound = { "Aaaa ji aa ooa.", "Oo oOoo Ooo.", "Iia ai aia yaah.", "Gaaaah ah...", "Ajikido olaja pakauu!", "Rijaaaaa hi!", "Agika kajaka hajitaa.", "Bala ka burburak!", "Big bada bum!" };
		private static readonly string[] spellboundEmotes = { "je jako uhranuta.", "si skube vlasy.", "nepritomne zira.", "nekomunikuje.", "te ignoruje.", "je naprosto mimo sebe." };

		/// <summary>Erebor's f_q000_ElviraDClick + f_q000_Elvira.</summary>
		private static void TalkToElvira(NPC elvira, Player player, bool clicked) {
			int stage = GetStage(player);
			if (clicked && (stage == 6)) {
				player.SysMessage("Podarilo se ti podat Elvire koren Este a vypada to, ze zabira!");
				Advance(player, 7);
				Reward(player, 50);
				elvira.Say("Aaaaah...");
				elvira.Emote("zacala prudce dychat.");
				return;
			}
			if (stage >= 7) {
				elvira.Dialog(player, SingletonScript<D_Q000Elvira>.Instance, elvira);
				return;
			}
			int roll = Globals.dice.Next(spellbound.Length + spellboundEmotes.Length);
			if (roll < spellbound.Length) {
				elvira.Say(spellbound[roll]);
			} else {
				elvira.Emote(spellboundEmotes[roll - spellbound.Length]);
			}
			if (stage > 0) {
				player.SysMessage("Tohle musi byt Elvira. Je strasne podobna sve matce");
				if ((stage == 1) || (stage == 3)) {
					player.SysMessage("Zda se, ze Elvira je tezce nemocna ci naprosto silena.");
					Advance(player, (stage == 1) ? 2 : 4);
					Reward(player, 50);
				}
			}
		}

		private static void TalkToRinald(NPC rinald, Player player) {
			if (IsTransformed(rinald)) {
				return;
			}
			if (AreGuardsAlive(rinald, rinald)) {
				player.RedMessage("Nemuzes s nim mluvit, dokud te obtezuji ty bestie kolem!");
			} else {
				rinald.Dialog(player, SingletonScript<D_Q000Rinald>.Instance, rinald);
			}
		}

		//Erebor's f_gq030_refuseQDialog / f_mq030_refuseQDialog
		private static void TalkToEnvoy(NPC envoy, Player player, bool gondor) {
			if (player.Realm != (gondor ? Realm.Gondor : Realm.Mordor)) {
				envoy.Say(gondor ? "Vyhni se memu meci, prisluhovaci Mordoru !" : "Vyhni se memu meci, prisluhovaci Gondoru !");
			} else {
				envoy.Dialog(player, SingletonScript<D_Q000Envoy>.Instance, envoy);
			}
		}

		/// <summary>Erebor's f_guarditAdv_CheckSpawns: the spawns set to guard a thing. Those were picked by hand per spot, here
		/// it's any hostile creature around.</summary>
		internal static bool AreGuardsAlive(Thing guarded, Thing except) {
			ImmutableRectangle area = new ImmutableRectangle(guarded.X - GuardRange, guarded.Y - GuardRange, guarded.X + GuardRange, guarded.Y + GuardRange);
			foreach (Thing t in guarded.GetMap().GetThingsInRectangle(area)) {
				NPC npc = t as NPC;
				if ((npc != null) && (npc != except) && !npc.Flag_Dead && (npc.AggroMode == AggroMode.Hostile)) {
					return true;
				}
			}
			return false;
		}
		#endregion talking

		#region Rinald and his cave
		internal static bool IsTransformed(NPC rinald) {
			return rinald.Name == "Rinald";
		}

		/// <summary>Erebor's f_q000_rinaldTransfer, some seconds after the player said his name: he turns into a daemon and
		/// can be hurt. Not ported: his breath (f_dragon_fire), and e_rinald's weakening by profession.</summary>
		internal static void TransformRinald(NPC rinald) {
			rinald.Sound((int) SoundNames.lightningstrike);
			rinald.Model = ThingDef.GetByDefname("c_daemon_mage1").Model;
			rinald.Karma = -10000;
			rinald.Color = 0x445;
			rinald.Flag_Invulnerable = false;
			rinald.AggroMode = AggroMode.Hostile;
		}

		/// <summary>Erebor's f_q000_vg_CaveEntrance_test + f_q000_vg_CaveEntrance.</summary>
		internal static void TryEnterCave(Item entrance, Player player) {
			int stage = GetStage(player);
			if ((player.SkillRank < SkillRank.Master) || (stage < 8) || (player.Level < MinLevel)) {
				player.SysMessage("Vystrasena zena v kleci pred vstupem do temne jeskyne? Ne, tam nejdes.");
			} else if (stage > 8) {
				player.SysMessage("Tady uz jsi " + Sex(player, "udelal vse, co jsi mohl", "udelala vse, co jsi mohla") + ".");
			} else {
				EnsureRinald(entrance);
				player.SetTag(caveMinutesTK, CaveMinutes);
				player.SetTag(caveReturnTK, new Point4D(player.X + 1, player.Y + 1, player.Z, player.M));
				player.AddTimer(caveTimerTK, new Q000CaveTimer()).DueInSeconds = 1;
				player.SysMessage("Musis spechat! Hrozi ti tu velike nebezpeci.");
				Point4D arrival = StaticRegion.GetByDefname(CaveRegion).P;
				player.Go(arrival.X, arrival.Y, arrival.Z, arrival.M);
			}
		}

		//a Rinald who already turned into a daemon starts over for the next hero
		private static void EnsureRinald(Item entrance) {
			NPC rinald = GetRinald(entrance);
			if ((rinald != null) && IsTransformed(rinald)) {
				rinald.Delete();
				rinald = null;
			}
			if (rinald == null) {
				int[] spot = (int[]) entrance.GetTag(rinaldSpotTK);
				PlaceRinald(entrance, spot[0], spot[1], spot[2], (byte) spot[3]);
			}
		}

		private static readonly TagKey rinaldSpotTK = TagKey.Acquire("_q000RinaldSpot_");

		private static void PlaceRinald(Item entrance, int x, int y, int z, byte m) {
			NPC rinald = (NPC) ThingDef.GetByDefname(RinaldDef).Create(x, y, z, m);
			rinald.Flag_Invulnerable = true;
			NPCAiTimer.GetFor(rinald).WanderLeash = 0;
			entrance.SetTag(rinaldTK, rinald);
			entrance.SetTag(rinaldSpotTK, new[] { x, y, z, m });
		}

		internal static NPC GetRinald(Item entranceOrExit) {
			NPC rinald = entranceOrExit.GetTag(rinaldTK) as NPC;
			return ((rinald == null) || rinald.IsDeleted) ? null : rinald;
		}

		/// <summary>Erebor's f_q000_vg_CaveExit_test + f_q000_vg_CaveExit.</summary>
		internal static void TryLeaveCave(Item exit, Player player) {
			if (GetRinald(exit) != null) {
				player.RedMessage("Ted to preci neotocis!");
				return;
			}
			if (GetStage(player) == 8) {
				player.SysMessage("Pred odchodem jsi z Rinaldova tela jeste " + Sex(player, "sebral", "sebrala") + " klic.");
				Advance(player, 9);
				Reward(player, 1000);
			} else {
				player.SysMessage("Nejsi moc " + Sex(player, "moudry", "moudra") + " z toho, co se tady stalo...");
			}
			LeaveCave(player);
		}

		/// <summary>Erebor's f_q000_dungeonPort: out of the cave now, the way the time limit does it.</summary>
		internal static void ExpelFromCave(Player player) {
			if (player.HasTimer(caveTimerTK)) {
				player.SetTag(caveMinutesTK, 0);
				player.GetTimer(caveTimerTK).DueInSeconds = 0;
			}
		}

		private static void LeaveCave(Player player) {
			player.DeleteTimer(caveTimerTK);
			Point4D back = player.GetTag(caveReturnTK) as Point4D;
			player.RemoveTag(caveMinutesTK);
			player.RemoveTag(caveReturnTK);
			if (back != null) {
				player.Go(back.X, back.Y, back.Z, back.M);
			}
		}

		/// <summary>i_q000_delayer's @timer, once a minute: the time left, and out when it's up. A player who died in there
		/// is taken out with the next tick, without their corpse.</summary>
		internal static void OnCaveMinute(Player player) {
			Region cave = StaticRegion.GetByDefname(CaveRegion);
			if (player.Region != cave) {
				player.RemoveTag(caveMinutesTK);
				player.RemoveTag(caveReturnTK);
				return;
			}
			int minutes = player.Flag_Dead ? 0 : Convert.ToInt32(player.GetTag(caveMinutesTK), CultureInfo.InvariantCulture);
			if (minutes > 0) {
				if (minutes > 4) {
					player.SysMessage("Zbyva " + minutes + " minut.");
				} else if (minutes > 1) {
					player.RedMessage("Zbyvaji " + minutes + " minuty.");
				} else {
					player.RedMessage("Zbyva ti posledni minuta.");
				}
				player.SetTag(caveMinutesTK, minutes - 1);
				player.AddTimer(caveTimerTK, new Q000CaveTimer()).DueInSeconds = 60;
				return;
			}
			bool dead = player.Flag_Dead;
			LeaveCave(player);
			if (dead) {
				player.SysMessage("Tohle se ti nepovedlo...");
			} else if (GetStage(player) < 8) {
				player.SysMessage(Sex(player, "Byl jsi vyhnan", "Byla jsi vyhnana") + " z jeskyne neznamym muzem!");
			} else {
				player.SysMessage("Rinald te vyhnal z jeskyne!");
			}
		}
		#endregion Rinald and his cave

		#region the root and the cage
		private static readonly string[] rootDone = { "Tady si uz nic nevezmes.", "Kvetina je jiz zcela otrhana.", "Necht posledni vselek uhniva v pokoji.", "Este by byla jiste rada, ze jeji koren pomohl dobre veci.", "Prohrabovat se tu dale je naprosto zbytecne.", "Zbytky teto legendarni byliny jsou diky tobe odsouzeny k vecnemu zaniku", "Dalsim stouranim se v hline tu jen ztracis cas." };
		private static readonly string[] doorDone = { "Dvere jsou jiz otevrene.", "Odemcene dvere do klece.", "Stare rezave dvere klece, ktere Rinald pouzival k vezneni Elmiry.", "Tady uz moc neudelas.", "Nemuzes s temi dvermi udelat nic vic.", "Jednou dvere, porad dvere.", "Takoveto pevne dvere donuti k zamysleni nad zpusobem obzivy kazdeho zlodeje.", "Pevne dvere jsou zaklad kazdeho zalare." };

		/// <summary>i_q000_rootOfEste @userdclick.</summary>
		internal static void TryPullRoot(Item root, Player player) {
			int stage = GetStage(player);
			if (player.Flag_Dead) {
				return;
			}
			if (stage == 5) {
				if (AreGuardsAlive(root, null)) {
					player.RedMessage("Kvetinu stale nekdo hlida. Na jeji sebrani potrebujes klid!");
				} else {
					player.SysMessage("Podarilo se ti vytrhnout koren Este!");
					Advance(player, 6);
					Reward(player, 100);
				}
			} else if (stage > 5) {
				player.SysMessage(rootDone[Globals.dice.Next(rootDone.Length)]);
			}
		}

		/// <summary>i_q000_cageDoor @userdclick.</summary>
		internal static void UseCageDoor(Item door, Player player) {
			int stage = GetStage(player);
			if (stage == 9) {
				player.SysMessage(Sex(player, "Odemkl", "Odemkla") + " jsi vrata klece pomoci klice nalezeneho u Rinalda");
				NPC elmira = FindNear(door, ElmiraDef);
				if (elmira != null) {
					elmira.Say("Dekuji ti " + Sex(player, "muj hrdino", "ma hrdinko") + "!");
				}
				Reward(player, 50);
				Advance(player, 10);
			} else if (stage < 9) {
				player.SysMessage("Nevis, jak bys " + Sex(player, "mohl", "mohla") + " tyto pevne mrize otevrit.");
			} else {
				player.SysMessage(doorDone[Globals.dice.Next(doorDone.Length)]);
			}
		}

		private static NPC FindNear(Thing thing, string defname) {
			ThingDef def = ThingDef.GetByDefname(defname);
			ImmutableRectangle area = new ImmutableRectangle(thing.X - GuardRange, thing.Y - GuardRange, thing.X + GuardRange, thing.Y + GuardRange);
			foreach (Thing t in thing.GetMap().GetThingsInRectangle(area)) {
				if (t.Def == def) {
					return (NPC) t;
				}
			}
			return null;
		}

		/// <summary>i_q000_rootOfEste_Alarm @step: at most once in 5 seconds.</summary>
		internal static void OnAlarmStep(Item alarm, Player player) {
			if (player.IsGM || (GetStage(player) != 5)) {
				return;
			}
			object last = alarm.GetTag(lastAlarmTK);
			if ((last == null) || (Convert.ToDouble(last, CultureInfo.InvariantCulture) < Globals.TimeInSeconds)) {
				alarm.SetTag(lastAlarmTK, Globals.TimeInSeconds + 5);
				player.SysMessage(Sex(player, "Zahledl", "Zahledla") + " jsi bylinu, pro kterou te Elmira poslala!");
			}
		}
		#endregion the root and the cage

		#region setup
		private static int[] Beside(Map map, int x, int y, int range) {
			return ExpertQuest.NearestStandable(map, x, y, range);
		}

		private static NPC PlaceStill(string defname, Map map, int[] at, byte m) {
			NPC npc = (NPC) ThingDef.GetByDefname(defname).Create(at[0], at[1], at[2], m);
			npc.Flag_Invulnerable = true; //Erebor's @create
			NPCAiTimer.GetFor(npc).WanderLeash = 0;
			return npc;
		}

		private static Item PlaceFixed(string defname, int[] at, byte m) {
			Item item = (Item) ThingDef.GetByDefname(defname).Create(at[0], at[1], at[2], m);
			item.SetAttribute(ItemAttributes.attr_move_never, true);
			return item;
		}

		private static void Spawn(string defname, int[] at, byte m, short amount) {
			SpawnInfo info = new SpawnInfo(amount, (IThingFactory) ThingDef.GetByDefname(defname), 20, 40, 8);
			ScriptHolder.GetFunction("f_spawninit").Run(new Point4D(at[0], at[1], at[2], m), info);
		}

		private static void Clear(Map map, ImmutableRectangle area, params string[] defnames) {
			foreach (string defname in defnames) {
				ExpertQuest.RemovePlaced(map, defname, area);
			}
		}

		/// <summary>The .SetupGrandmasterQuest command. The archive defines Rinald's cave, the regions of the story and, in the
		/// journal's texts, roughly where things are: Elvira in the forest east of the Ice Plains (a_Severni_Spoust), Elmira's
		/// cage by the rock face a little way off, with the cave's entrance, the root of Este at the far end of the dark cave
		/// (see RootCaveMouth). Exact tiles are proposals, as are Elqira beside both Expert stones and the envoys at the spawn points
		/// of Minas Tirith and Rhun. Replaces what an earlier run placed.</summary>
		[SteamFunction]
		public static void SetupGrandmasterQuest(Character self) {
			Map world = Map.GetMap(0);
			Point4D spoust = StaticRegion.GetByDefname("a_Severni_Spoust").P;
			Point4D cave = StaticRegion.GetByDefname(CaveRegion).P;
			Map caveMap = Map.GetMap(cave.M);
			Point4D tirith = StaticRegion.GetByDefname("a_Minas_Tirith").P;
			Map tirithMap = Map.GetMap(tirith.M);
			Point4D rhun = StaticRegion.GetByDefname("a_Rhun").P;

			Clear(world, new ImmutableRectangle(2760, 2755, 2775, 2770), ElqiraDef);
			Clear(world, new ImmutableRectangle(3712, 2947, 3727, 2962), ElqiraDef);
			Clear(world, new ImmutableRectangle(spoust.X - 45, spoust.Y - 45, spoust.X + 45, spoust.Y + 45), ElviraDef, ElmiraDef, CageDoorDef, CaveEntranceDef); //where the first version put them
			Clear(world, new ImmutableRectangle(3970, 495, 4125, 545), ElviraDef, ElmiraDef, CageDoorDef, CaveEntranceDef);
			Clear(caveMap, new ImmutableRectangle(5200, 1512, 5297, 1600), RinaldDef, CaveExitDef, "c_hellhound", "i_worldgem_bit");
			Clear(Map.GetMap(2), new ImmutableRectangle(1, 794, 189, 1196), RootDef, AlarmDef, "c_q000_phoenix", "i_worldgem_bit"); //where the first version put them
			Clear(world, RootCave, RootDef, AlarmDef, "c_q000_phoenix", "i_worldgem_bit");
			Clear(world, new ImmutableRectangle(2926, 690, 3300, 900), RootDef, AlarmDef, "i_worldgem_bit");
			Clear(tirithMap, new ImmutableRectangle(tirith.X - 8, tirith.Y - 8, tirith.X + 8, tirith.Y + 8), GondorEnvoyDef);
			Clear(world, new ImmutableRectangle(rhun.X - 8, rhun.Y - 8, rhun.X + 8, rhun.Y + 8), MordorEnvoyDef);

			PlaceStill(ElqiraDef, world, Beside(world, 2769, 2763, 5), 0);
			PlaceStill(ElqiraDef, world, Beside(world, 3721, 2955, 5), 0);
			IMovementSettings walker = self.MovementSettings;
			PlaceStill(ElviraDef, world, Beside(world, 4004, 512, 10), 0);
			int[] elmiraAt = Beside(world, 4104, 530, 6);
			PlaceStill(ElmiraDef, world, elmiraAt, 0);
			List<int[]> aroundElmira = ExpertQuest.WalkableRing(world, new Point3D(elmiraAt[0], elmiraAt[1], elmiraAt[2]), walker, 3);
			PlaceFixed(CageDoorDef, aroundElmira[0], 0);
			Item entrance = PlaceFixed(CaveEntranceDef, aroundElmira[aroundElmira.Count / 2], 0);

			int[] rinaldAt = ExpertQuest.WalkableRing(caveMap, cave, walker, 30)[0];
			PlaceRinald(entrance, rinaldAt[0], rinaldAt[1], rinaldAt[2], cave.M);
			Item exit = PlaceFixed(CaveExitDef, ExpertQuest.WalkableRing(caveMap, cave, walker, 3)[0], cave.M);
			exit.SetTag(rinaldTK, GetRinald(entrance));
			Spawn("c_hellhound", ExpertQuest.WalkableRing(caveMap, new Point3D(rinaldAt[0], rinaldAt[1], rinaldAt[2]), walker, 3)[0], cave.M, 4);

			int[] rootAt = ExpertQuest.WalkableRing(world, RootCaveMouth, walker, int.MaxValue, RootCave)[0]; //the end of the dark cave
			PlaceFixed(RootDef, rootAt, 0);
			Point3D rootPoint = new Point3D(rootAt[0], rootAt[1], rootAt[2]);
			List<int[]> aroundRoot = ExpertQuest.WalkableRing(world, rootPoint, walker, 6, RootCave);
			for (int i = 0; i < 4; i++) {
				PlaceFixed(AlarmDef, aroundRoot[i * aroundRoot.Count / 4], 0);
			}
			Spawn("c_q000_phoenix", ExpertQuest.WalkableRing(world, rootPoint, walker, 2, RootCave)[0], 0, 3);

			PlaceStill(GondorEnvoyDef, tirithMap, Beside(tirithMap, tirith.X + 2, tirith.Y, 6), tirith.M);
			PlaceStill(MordorEnvoyDef, world, Beside(world, rhun.X + 2, rhun.Y, 6), 0);

			string summary = "SETUP_GRANDMASTER_QUEST: done rinald=" + rinaldAt[0] + "," + rinaldAt[1] + "," + rinaldAt[2] + " root=" + rootAt[0] + "," + rootAt[1] + "," + rootAt[2];
			Console.WriteLine(summary);
			self.SysMessage(summary);
		}
		#endregion setup
	}

	/// <summary>#833 - the cave's minute timer on the player (Erebor's i_q000_delayer @timer).</summary>
	[SaveableClass]
	public class Q000CaveTimer : BoundTimer {
		[LoadingInitializer]
		public Q000CaveTimer() {
		}

		protected sealed override void OnTimeout(TagHolder cont) {
			Player player = cont as Player;
			if ((player != null) && !player.IsDeleted) {
				GrandmasterQuest.OnCaveMinute(player);
			}
		}
	}

	/// <summary>#833 - Rinald a few seconds after hearing his name (Erebor's delayFunction(3,f_q000_rinaldTransfer)).</summary>
	[SaveableClass]
	public class Q000RinaldTimer : BoundTimer {
		[LoadingInitializer]
		public Q000RinaldTimer() {
		}

		protected sealed override void OnTimeout(TagHolder cont) {
			NPC rinald = cont as NPC;
			if ((rinald != null) && !rinald.IsDeleted) {
				GrandmasterQuest.TransformRinald(rinald);
			}
		}
	}

	/// <summary>#833 - @UserDClick of the quest's NPCs.</summary>
	public class E_Q000Npc : CompiledTriggerGroup {
		public TriggerResult On_DClick(NPC self, Character clicker) {
			Player player = clicker as Player;
			if (player == null) {
				return TriggerResult.Continue;
			}
			if (QuestNpc.IsCloseEnough(self, player)) {
				GrandmasterQuest.Talk(self, player, true);
			}
			return TriggerResult.Cancel;
		}
	}

	public class E_Q000Root : CompiledTriggerGroup {
		public TriggerResult On_DClick(Item self, Character clicker) {
			Player player = clicker as Player;
			if (player != null) {
				GrandmasterQuest.TryPullRoot(self, player);
			}
			return TriggerResult.Cancel;
		}

		public TriggerResult On_Click(Item self, Character clicker) {
			Player player = clicker as Player;
			int stage = (player == null) ? 0 : GrandmasterQuest.GetStage(player);
			if (stage == 5) {
				player.SysMessage("V teto byline poznavas koren Este, pro ktery te poslala Elmira");
				self.Message("Koren Este");
				return TriggerResult.Cancel;
			} else if (stage > 5) {
				self.Message("Otrhane zbytky korenu Este");
				return TriggerResult.Cancel;
			}
			return TriggerResult.Continue;
		}
	}

	public class E_Q000Alarm : CompiledTriggerGroup {
		public void On_Step(Item self, Character stepping) {
			Player player = stepping as Player;
			if (player != null) {
				GrandmasterQuest.OnAlarmStep(self, player);
			}
		}
	}

	public class E_Q000CageDoor : CompiledTriggerGroup {
		public TriggerResult On_DClick(Item self, Character clicker) {
			Player player = clicker as Player;
			if (player != null) {
				GrandmasterQuest.UseCageDoor(self, player);
			}
			return TriggerResult.Cancel;
		}
	}

	public class E_Q000CaveEntrance : CompiledTriggerGroup {
		public void On_Step(Item self, Character stepping) {
			Player player = stepping as Player;
			if (player != null) {
				GrandmasterQuest.TryEnterCave(self, player);
			}
		}
	}

	public class E_Q000CaveExit : CompiledTriggerGroup {
		public void On_Step(Item self, Character stepping) {
			Player player = stepping as Player;
			if (player != null) {
				GrandmasterQuest.TryLeaveCave(self, player);
			}
		}
	}

	public partial class D_Q000Elqira {
		protected override void OnButton(string button, Character npc, Player player) {
			int q = GrandmasterQuest.GetStage(player);
			if (GrandmasterQuest.IsReady(player)) {
				if (button == "q000_questAssignment") {
					GrandmasterQuest.Advance(player, 1);
					npc.Say("... Nemuzes si je splest!");
				} else if (button == "q000_wrongChoice") {
					npc.Say("Nechci se ted s tebou bavit.. Jdi pryc.");
					QuestNpc.Upset(npc, 60);
				} else if (button == "q000_wrongChoice2") {
					npc.Say("Ty me vubec neposlouchas! Jdi pryc!");
					QuestNpc.Upset(npc, 150);
				}
			} else if ((q == 10) && (button == "q000_questSolved")) {
				GrandmasterQuest.Advance(player, 11);
				ThingDef.GetByDefname("i_mi_bag").Create(player.Backpack);
				GoldUtils.Grant(player, 2500);
			}
		}
	}

	public partial class D_Q000Elmira {
		protected override void OnButton(string button, Character npc, Player player) {
			int q = GrandmasterQuest.GetStage(player);
			if ((q == 1) && (button == "q000_goFindElvira")) {
				GrandmasterQuest.Advance(player, 3);
			} else if (((q == 2) || (q == 3)) && (button == "q000_refuse")) {
				npc.Say("Jdi pryc!");
				QuestNpc.Upset(npc, 60);
			} else if ((q == 2) || (q == 4)) {
				if (button == "q000_goForHerb") {
					npc.Say("Pospes si!");
					GrandmasterQuest.Advance(player, 5);
					GrandmasterQuest.Reward(player, 50);
				} else if (button == "q000_refuseHelp") {
					npc.Say("Ty mi nepomuzes!");
					QuestNpc.Upset(npc, 60);
				} else if (button == "q000_wrongChoice") {
					npc.Say("Ty me vubec neposlouchas!");
					QuestNpc.Upset(npc, 120);
				} else if ((q == 4) && (button == "q000_notFoundElvira")) {
					npc.Say("Musis jit! Prosim, najdi ji!");
				}
			} else if ((q == 3) && (button == "q000_notFoundElvira")) {
				npc.Say("Musis jit! Prosim, najdi ji!");
			}
		}
	}

	public partial class D_Q000Elvira {
		protected override void OnButton(string button, Character npc, Player player) {
			if ((GrandmasterQuest.GetStage(player) == 7) && (button == "q000_goKillRinald")) {
				npc.Say("Dekuji ti " + GrandmasterQuest.Sex(player, "hrdino", "hrdinko") + "!");
				GrandmasterQuest.Advance(player, 8);
			}
		}
	}

	public partial class D_Q000Rinald {
		protected override void OnButton(string button, Character npc, Player player) {
			if (button == "q000_wrongName") {
				npc.Say("HA! Vypadni!");
				GrandmasterQuest.ExpelFromCave(player);
			} else if ((GrandmasterQuest.GetStage(player) == 8) && (button == "q000_tellHisName")) {
				npc.Name = "Rinald";
				npc.Say(GrandmasterQuest.Sex(player, "Ubozaku", "Ubozacko") + "! To, ze znas me jmeno, nic nedokazuje! Zemres bidnou smrti!");
				npc.AddTimer(rinaldTimerTK, new Q000RinaldTimer()).DueInSeconds = 3;
			}
		}

		private static readonly TimerKey rinaldTimerTK = TimerKey.Acquire("_q000RinaldTimer_");
	}

	public partial class D_Q000Envoy {
		protected override int Face {
			get {
				return 0x3002;
			}
		}

		protected override void OnButton(string button, Character npc, Player player) {
			if ((button == "q000_getVM") && GrandmasterQuest.CanBePromoted(player)) {
				GrandmasterQuest.Advance(player, 13);
				player.SkillRank = SkillRank.Grandmaster;
			}
		}
	}

	/// <summary>#833 - the speech of the quest's NPCs: any of the greetings of its block, from close enough.</summary>
	public sealed class Q000Speech : AbstractSpeech {
		private static readonly string[] greetings = { "dobry", "den", "zdravim", "hi", "hello" };
		private readonly string[] patterns;

		public static void Bootstrap() {
			new Q000Speech("spk_q000_Elqira", greetings).Register();
			new Q000Speech("spk_q000_elmira", greetings).Register();
			new Q000Speech("spk_q000_elvira", greetings).Register();
			new Q000Speech("spk_q000_rinald", new[] { "dobry", "dobre", "den", "zdravim", "hi", "hello", "rinald" }).Register();
			string[] envoy = { "hi", "ahoj", "zdravim", "dobry den", "bud zdrav" };
			new Q000Speech("spk_gq030_zmocnenec", envoy).Register();
			new Q000Speech("spk_mq030_zmocnenec", envoy).Register();
		}

		private Q000Speech(string defname, string[] patterns)
			: base(defname) {
			this.patterns = patterns;
		}

		protected override SpeechResult Handle(AbstractCharacter listener, SpeechArgs speechArgs) {
			return this.TryHandle(listener, speechArgs);
		}

		protected override SpeechResult TryHandle(AbstractCharacter listener, SpeechArgs speechArgs) {
			NPC npc = listener as NPC;
			Player player = speechArgs.Speaker as Player;
			if ((npc != null) && (player != null) && (speechArgs.Speech != null) && SkillTeacherSpeech.ContainsAny(speechArgs.Speech, this.patterns)
					&& QuestNpc.IsCloseEnough(npc, player)) {
				GrandmasterQuest.Talk(npc, player, false);
			}
			return SpeechResult.IgnoredOrActedUpon;
		}
	}
}
