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
	/// egg goes to whoever opens her corpse first, as lifting it out was on Erebor. #1090, the user's rule: the key and the
	/// egg are tags on the player (QuestItems), never items, so nobody can buy, pass on or drop either; Erebor's checks
	/// against someone else's egg had nothing left to catch and are gone.</para>
	/// <para>The archive doesn't say where the pieces stood; scripts/Setup has its own proposal.</para>
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
		private static readonly TagKey acceptedTK = TagKey.Acquire("_masterQuestAccepted_");

		//called by ClassManager on every startup/recompile, see ExpertQuest.Init
		public static void Init() {
			QuestNpc.Attach(TeleportDef, SingletonScript<E_MasterQuestTeleport>.Instance);
			QuestItems.Register(KeyDef, "klic ke staremu teleportu");
			QuestItems.Register(EggDef, "oplodnene vejce hadi kralovny", player => {
				if (HasAccepted(player)) {
					QuestJournal.Report(player, QuestJournal.Master);
				}
			});
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
			if (QuestItems.Has(player, EggDef)) {
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

		/// <summary>Erebor's Lenastirovarec, the greetings: the player's standing with the quest. An egg with no visit to the
		/// islands (a GM's) still gets Erebor's "bought egg" answer.</summary>
		internal static void Greet(NPC lenastir, Player player) {
			bool ready = (player.SkillRank == SkillRank.Expert) && (player.Level >= MinLevel);
			if (ready && QuestItems.Has(player, EggDef)) {
				if (!HasReachedIslands(player)) {
					lenastir.Say("Snazis se me oklamat! Toto je koupene obycejne vejce, ty jsi hadi kralovnu ani koutkem oka nezahledl.");
				} else {
					Reward(lenastir, player);
				}
			} else if (ready) {
				lenastir.Say("Zdravim te. Mam pro tebe ukol, za jehoz splneni te povysim na mistra. Pokud mas zajem mi pomoci a ukolu se zhostit, rekni ano.");
			} else if (player.SkillRank > SkillRank.Expert) {
				lenastir.Say("Jiz jsi muj ukol splnil.");
			} else {
				lenastir.Say("Nejprve se musis stat expertem a mit alespon 6. level. Muj ukol je pro tebe zatim prilis tezky.");
			}
		}

		private static void Reward(NPC lenastir, Player player) {
			lenastir.Say("Vyborne, dekuji ti, velmi jsi mi pomohl. Za odmenu te povysuji na mistra.");
			player.SysMessage(Loc<MasterQuestLoc>.Get(player.Language).Promoted);
			player.SkillRank = SkillRank.Master;
			ForgetReachedIslands(player);
			player.RemoveTag(acceptedTK);
			QuestItems.Take(player, EggDef);
			QuestItems.Take(player, KeyDef);
			lenastir.Sound((int) SoundNames.lightningstrike);
			LevelingSystem.AddExperience(player, ExperienceReward);
			QuestNpc.CelebrateRank(player);
			QuestJournal.Report(player, QuestJournal.Master);
		}

		/// <summary>Erebor's Lenastirovarec on *yes* / *ano*: the task, and the key to an Expert who has none.</summary>
		internal static void Accept(NPC lenastir, Player player) {
			if (QuestItems.Has(player, KeyDef)) {
				lenastir.Say("Musis mi prinest oplozene hadi vejce od samotne hadi kralovny. Bohuzel neznam misto, kde se kralovna nachazi, ale mohu ti poskytnout tento klic, ktery pry stary teleport otevira. Dej si vsak pozor, hadi jsou velmi nebezpecni.");
			} else if (player.SkillRank == SkillRank.Expert) {
				lenastir.Say("Poslouchej tedy. Musis mi prinest oplozene hadi vejce od samotne hadi kralovny. Bohuzel neznam misto, kde se kralovna nachazi, ale mohu ti poskytnout tento klic, ktery pry stary teleport otevira. Dej vsak pozor, hadi jsou velmi nebezpecni.");
				QuestItems.Give(player, KeyDef);
				if (!HasAccepted(player)) {
					player.SetTag(acceptedTK, 1);
					QuestJournal.Report(player, QuestJournal.Master);
				}
			}
		}

		/// <summary>Erebor's i_hadi_teleport @step: the key is used up either way, only an Expert gets through.</summary>
		internal static void TryPass(Player player) {
			if (!QuestItems.Take(player, KeyDef)) {
				return;
			}
			if (player.SkillRank == SkillRank.Expert) {
				player.Go(1508, 3860, 10, 0);
			} else {
				player.RedMessage(Loc<MasterQuestLoc>.Get(player.Language).CantPass);
			}
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
			self.ClickMessage(clicker, Loc<MasterQuestLoc>.Get(clicker.Language).TeleportDescription);
		}
	}

	/// <summary>#833 - Mistr Lenastir's speech (Erebor's Lenastirovarec).</summary>
	public sealed class MasterQuestSpeech : AbstractSpeech {
		private static readonly string[] greetings = { "dobry", "dobre", "den", "zdravim", "hi", "ahoj", "buy" };
		private static readonly string[] yeses = { "yes", "ano" };
		private static readonly string[] questions = { "teleport", "najdu", "kde", "klic" };

		public new static void Bootstrap() {
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
		public string TeleportDescription = "An unknown locked teleport";
		public string CantPass = "You can't go through here!";
	}
}
