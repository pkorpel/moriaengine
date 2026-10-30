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

using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// #833 - Erebor's Master quest (questy/mistr.scp), the way an Expert becomes a Master (Player.SkillRank 2).
	/// <para>Mistr Lenastir (c_lenastir) asks an Expert of level 6+ for a fertilized egg of the snake queen
	/// (c_hadi_kralovna), and gives them a key (i_klic_teleportu) to an old teleport (i_hadi_teleport) that leads to the
	/// Snake Islands. Entering those islands, by any way, marks the player (r_hadiostrovy, Erebor's tag.mistrquest). The
	/// egg belongs to whoever lifts it first. Lenastir takes it only from its owner, and from a player who has been there:
	/// someone else's egg gets the player sent to the bug area.</para>
	/// <para>The archive doesn't say where the pieces stood; .SetupMasterQuest has its own proposal.</para>
	/// </summary>
	public static class MasterQuest {
		public const string EggDef = "i_hadi_vejce";
		public const string KeyDef = "i_klic_teleportu";
		public const string TeleportDef = "i_hadi_teleport";
		public const string LenastirDef = "c_lenastir";
		public const string QueenDef = "c_hadi_kralovna";
		public const int MinLevel = 6;
		public const int ExperienceReward = 500;
		public const byte TeleportPlane = 10; //Beridor Level 3's Mapplane - the same statics on plane 0 belong to no dungeon

		private static readonly TagKey reachedIslandsTK = TagKey.Acquire("_masterQuestReachedIslands_");
		private static readonly TagKey eggOwnerTK = TagKey.Acquire("_masterQuestEggOwner_");
		private static readonly TagKey acceptedTK = TagKey.Acquire("_masterQuestAccepted_");

		//called by ClassManager on every startup/recompile, see ExpertQuest.Init
		public static void Init() {
			ExpertQuest.Attach(EggDef, SingletonScript<E_MasterQuestEgg>.Instance);
			ExpertQuest.Attach(TeleportDef, SingletonScript<E_MasterQuestTeleport>.Instance);
		}

		public static bool HasReachedIslands(Player player) {
			return player.GetTag(reachedIslandsTK) != null;
		}

		/// <summary>Erebor's r_hadiostrovy @enter.</summary>
		public static void MarkReachedIslands(Character ch) {
			bool first = ch.GetTag(reachedIslandsTK) == null;
			ch.SetTag(reachedIslandsTK, 1);
			Player player = ch as Player;
			if (first && (player != null) && HasAccepted(player)) {
				QuestJournal.Report(player, QuestJournal.Master);
			}
		}

		public static bool HasAccepted(Player player) {
			return player.GetTag(acceptedTK) != null;
		}

		/// <summary>#839 - the journal's page. Erebor's journal had none for this quest.</summary>
		internal static string GetJournalText(Player player) {
			if (player.SkillRank >= SkillRank.Master) {
				return "<src.sex(\"Prinesl\",\"Prinesla\")> jsi Mistru Lenastirovi vejce hadi kralovny a <src.sex(\"byl\",\"byla\")> jsi <src.sex(\"povysen\",\"povysena\")> na mistra.";
			}
			if (!HasAccepted(player)) {
				return null;
			}
			Item egg = FindInBackpack(player, EggDef);
			if ((egg != null) && (GetEggOwner(egg) == player)) {
				return "<src.sex(\"Ziskal\",\"Ziskala\")> jsi vejce hadi kralovny. Odnes ho Mistru Lenastirovi.";
			}
			if (HasReachedIslands(player)) {
				return "<src.sex(\"Dostal\",\"Dostala\")> ses na Hadi ostrovy. Nekde tu musi byt hadi kralovna a jeji vejce.";
			}
			return "Mistr Lenastir po tobe chce oplozene hadi vejce od samotne hadi kralovny. Dal ti klic ke staremu teleportu, ktery ma byt nekde v podzemi pobliz hadich oltaru. Vede pry k ni i jina cesta.";
		}

		public static void ForgetReachedIslands(Player player) {
			player.RemoveTag(reachedIslandsTK);
		}

		public static Character GetEggOwner(Item egg) {
			Character owner = egg.GetTag(eggOwnerTK) as Character;
			return ((owner == null) || owner.IsDeleted) ? null : owner;
		}

		public static void SetEggOwner(Item egg, Character owner) {
			egg.SetTag(eggOwnerTK, owner);
		}

		public static Item FindInBackpack(Character ch, string defname) {
			ThingDef def = ThingDef.GetByDefname(defname);
			foreach (Item item in ch.Backpack.EnumDeep()) {
				if (item.Def == def) {
					return item;
				}
			}
			return null;
		}

		internal static void ConsumeOne(Item item) {
			if (item.Amount > 1) {
				item.Amount -= 1;
			} else {
				item.Delete();
			}
		}

		/// <summary>Erebor's Lenastirovarec, the greetings: the player's standing with the quest.</summary>
		internal static void Greet(NPC lenastir, Player player) {
			bool ready = (player.SkillRank == SkillRank.Expert) && (player.Level >= MinLevel);
			Item egg = FindInBackpack(player, EggDef);
			if (ready && (egg != null)) {
				if (!HasReachedIslands(player)) {
					lenastir.Say("Snazis se me oklamat! Toto je koupene obycejne vejce, ty jsi hadi kralovnu ani koutkem oka nezahledl.");
				} else if (GetEggOwner(egg) == player) {
					Reward(lenastir, player, egg);
				} else {
					Character owner = GetEggOwner(egg);
					lenastir.Say("Snazis se me podvest! Toto vejce na sobe nese otisk jineho hrdiny, jeho jmeno je " + ((owner == null) ? "" : owner.Name) + ", a to ty nejsi!");
					lenastir.Say("Zmiz mi z oci. Dokud nebudes sam schopen vejce ziskat, zustanes slabym!");
					player.Go(370, 2536, 45, 0); //Erebor's src.go(bug), [AREA bug]
				}
			} else if (ready) {
				lenastir.Say("Zdravim te. Mam pro tebe ukol, za jehoz splneni te povysim na mistra. Pokud mas zajem mi pomoci a ukolu se zhostit, rekni ano.");
			} else if (player.SkillRank > SkillRank.Expert) {
				lenastir.Say("Jiz jsi muj ukol splnil.");
			} else {
				lenastir.Say("Nejprve se musis stat expertem a mit alespon 6. level. Muj ukol je pro tebe zatim prilis tezky.");
			}
		}

		private static void Reward(NPC lenastir, Player player, Item egg) {
			lenastir.Say("Vyborne, dekuji ti, velmi jsi mi pomohl. Za odmenu te povysuji na mistra.");
			player.SysMessage(Loc<MasterQuestLoc>.Get(player.Language).Promoted);
			player.SkillRank = SkillRank.Master;
			ForgetReachedIslands(player);
			player.RemoveTag(acceptedTK);
			ConsumeOne(egg);
			Item key = FindInBackpack(player, KeyDef);
			if (key != null) {
				ConsumeOne(key);
			}
			lenastir.Sound((int) SoundNames.lightningstrike);
			LevelingSystem.AddExperience(player, ExperienceReward);
			QuestJournal.Report(player, QuestJournal.Master);
		}

		/// <summary>Erebor's Lenastirovarec on *yes* / *ano*: the task, and the key to an Expert who has none.</summary>
		internal static void Accept(NPC lenastir, Player player) {
			if (FindInBackpack(player, KeyDef) != null) {
				lenastir.Say("Musis mi prinest oplozene hadi vejce od samotne hadi kralovny. Bohuzel neznam misto, kde se kralovna nachazi, ale mohu ti poskytnout tento klic, ktery pry stary teleport otevira. Dej si vsak pozor, hadi jsou velmi nebezpecni.");
			} else if (player.SkillRank == SkillRank.Expert) {
				lenastir.Say("Poslouchej tedy. Musis mi prinest oplozene hadi vejce od samotne hadi kralovny. Bohuzel neznam misto, kde se kralovna nachazi, ale mohu ti poskytnout tento klic, ktery pry stary teleport otevira. Dej vsak pozor, hadi jsou velmi nebezpecni.");
				Item key = (Item) ThingDef.GetByDefname(KeyDef).Create(player.Backpack);
				key.SetAttribute(ItemAttributes.attr_newbie, true);
				if (!HasAccepted(player)) {
					player.SetTag(acceptedTK, 1);
					QuestJournal.Report(player, QuestJournal.Master);
				}
			}
		}

		/// <summary>Erebor's i_hadi_teleport @step: the key is used up either way, only an Expert gets through.</summary>
		internal static void TryPass(Player player) {
			Item key = FindInBackpack(player, KeyDef);
			if (key == null) {
				return;
			}
			ConsumeOne(key);
			if (player.SkillRank == SkillRank.Expert) {
				player.Go(1508, 3860, 10, 0);
			} else {
				player.RedMessage(Loc<MasterQuestLoc>.Get(player.Language).CantPass);
			}
		}

		/// <summary>The .SetupMasterQuest command: Lenastir beside both of Erebor's Expert stones, the teleport between
		/// the serpent pillars of Beridor Level 3, and spawn gems for the snake queen and her serpents on the Snake
		/// Islands. The archive has none of these spots. Replaces what an earlier run placed.</summary>
		[SteamFunction]
		public static void SetupMasterQuest(Character self) {
			Map map = Map.GetMap(0);
			ImmutableRectangle[] lenastirAreas = { Around(2773, 2763, 6), Around(3725, 2955, 6) };
			ImmutableRectangle teleportArea = Around(5331, 2745, 6);
			ImmutableRectangle islandArea = Around(1518, 3890, 30);
			foreach (ImmutableRectangle area in lenastirAreas) {
				ExpertQuest.RemovePlaced(map, LenastirDef, area);
			}
			Map teleportMap = Map.GetMap(TeleportPlane);
			ExpertQuest.RemovePlaced(teleportMap, TeleportDef, teleportArea);
			foreach (string defname in new[] { QueenDef, "c_hadi_obraz", "c_serpent_silver_mistr", "c_serpent_gold_mistr", "c_serpent_black_mistr", "i_worldgem_bit" }) {
				ExpertQuest.RemovePlaced(map, defname, islandArea);
			}

			int lenastirs = 0;
			foreach (int[] spot in new[] { new[] { 2773, 2763 }, new[] { 3725, 2955 } }) {
				int[] at = ExpertQuest.NearestStandable(map, spot[0], spot[1], 5);
				if (at != null) {
					Character lenastir = (Character) ThingDef.GetByDefname(LenastirDef).Create(at[0], at[1], at[2], 0);
					lenastir.Flag_Invulnerable = true; //Erebor's @create
					lenastirs++;
				}
			}
			int teleports = 0;
			int[] teleportAt = ExpertQuest.NearestStandable(teleportMap, 5331, 2745, 5);
			if (teleportAt != null) {
				Item teleport = (Item) ThingDef.GetByDefname(TeleportDef).Create(teleportAt[0], teleportAt[1], teleportAt[2], TeleportPlane);
				teleport.SetAttribute(ItemAttributes.attr_move_never, true);
				teleports++;
			}
			int gems = 0;
			int[] queenAt = ExpertQuest.NearestStandable(map, 1518, 3890, 20);
			int[] serpentsAt = (queenAt == null) ? null : ExpertQuest.NearestStandable(map, queenAt[0] + 4, queenAt[1], 10);
			if (queenAt != null) {
				SpawnInfo queen = new SpawnInfo(1, (IThingFactory) ThingDef.GetByDefname(QueenDef), 30, 60, 5);
				ScriptHolder.GetFunction("f_spawninit").Run(new Point4D(queenAt[0], queenAt[1], queenAt[2], 0), queen);
				gems++;
			}
			if (serpentsAt != null) {
				SpawnInfo serpents = new SpawnInfo(6, 10, 20, 12);
				foreach (string defname in new[] { "c_serpent_silver_mistr", "c_serpent_gold_mistr", "c_serpent_black_mistr" }) {
					serpents.Entries.Add(new SpawnEntry((IThingFactory) ThingDef.GetByDefname(defname), 1));
				}
				ScriptHolder.GetFunction("f_spawninit").Run(new Point4D(serpentsAt[0], serpentsAt[1], serpentsAt[2], 0), serpents);
				gems++;
			}
			string summary = "SETUP_MASTER_QUEST: lenastirs=" + lenastirs + " teleports=" + teleports + " gems=" + gems;
			System.Console.WriteLine(summary);
			self.SysMessage(summary);
		}

		private static ImmutableRectangle Around(int x, int y, int range) {
			return new ImmutableRectangle(x - range, y - range, x + range, y + range);
		}
	}

	/// <summary>#833 - i_hadi_vejce: belongs to the first player to lift it (Erebor's @pickup_pack link), and describes itself
	/// on a click.</summary>
	public class E_MasterQuestEgg : CompiledTriggerGroup {
		public void On_EnterChar(Item self, ItemInCharArgs args) {
			Player player = args.Cont as Player;
			if ((player != null) && (MasterQuest.GetEggOwner(self) == null)) {
				MasterQuest.SetEggOwner(self, player);
				if (MasterQuest.HasAccepted(player)) {
					QuestJournal.Report(player, QuestJournal.Master);
				}
			}
		}

		public void On_Click(Item self, Character clicker) {
			clicker.SysMessage(Loc<MasterQuestLoc>.Get(clicker.Language).EggDescription);
		}
	}

	/// <summary>#833 - i_hadi_teleport.</summary>
	public class E_MasterQuestTeleport : CompiledTriggerGroup {
		public void On_Step(Item self, Character stepping) {
			Player player = stepping as Player;
			if (player != null) {
				MasterQuest.TryPass(player);
			}
		}

		public void On_Click(Item self, Character clicker) {
			clicker.SysMessage(Loc<MasterQuestLoc>.Get(clicker.Language).TeleportDescription);
		}
	}

	/// <summary>#833 - Mistr Lenastir's speech (Erebor's Lenastirovarec).</summary>
	public sealed class MasterQuestSpeech : AbstractSpeech {
		private static readonly string[] greetings = { "dobry", "dobre", "den", "zdravim", "hi", "ahoj", "buy" };
		private static readonly string[] yeses = { "yes", "ano" };
		private static readonly string[] questions = { "teleport", "najdu", "kde", "klic" };

		public static void Bootstrap() {
			new MasterQuestSpeech("Lenastirovarec").Register();
		}

		private MasterQuestSpeech(string defname)
			: base(defname) {
		}

		protected override SpeechResult Handle(AbstractCharacter listener, SpeechArgs speechArgs) {
			return this.TryHandle(listener, speechArgs);
		}

		protected override SpeechResult TryHandle(AbstractCharacter listener, SpeechArgs speechArgs) {
			NPC lenastir = listener as NPC;
			Player player = speechArgs.Speaker as Player;
			string speech = speechArgs.Speech;
			if ((lenastir == null) || (player == null) || (speech == null)) {
				return SpeechResult.IgnoredOrActedUpon;
			}
			//one Sphere block: the first matching ON= wins
			if (SkillTeacherSpeech.ContainsAny(speech, greetings)) {
				MasterQuest.Greet(lenastir, player);
			} else if (SkillTeacherSpeech.ContainsAny(speech, yeses)) {
				MasterQuest.Accept(lenastir, player);
			} else if (SkillTeacherSpeech.ContainsAny(speech, questions)) {
				lenastir.Say("Stary teleport, od ktereho je tento klic, ma byt nekde v podzemi pobliz hadich oltaru, vice nevim. Ale i cesta jina nez pres teleport pry k hadi kralovne vede.");
			}
			return SpeechResult.IgnoredOrActedUpon;
		}
	}

	public class MasterQuestLoc : CompiledLocStringCollection<MasterQuestLoc> {
		public string Promoted = "Congratulations, you have become a Master. This opens the way to the further growth of your character.";
		public string EggDescription = "This is a fertilized snake egg - a very rare ingredient.";
		public string TeleportDescription = "An unknown locked teleport";
		public string CantPass = "You can't go through here!";
	}
}
