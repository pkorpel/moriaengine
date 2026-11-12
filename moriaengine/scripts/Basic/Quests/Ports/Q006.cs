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
using System.Globalization;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// #1264 - Erebor's q006 "Erioluv vnuk" (questy/q006_erioluv_vnuk.scp): the healer Eriol's grandson, lost and then sick with the
	/// barrow-wights' touch. 24 stages, Erebor's qtag(q006) numbers: torches, a shrunk dog, cures, grizzly thighs in a collector
	/// bag, the shaman Qworet, the wizard Unas and his four essence bags, the innkeeper, then the potion of purification. A player
	/// with Alchemy 100 mixes it at Eriol's and gets the alchemist's robe (+10 Alchemy for its owner); one without hands the ingredients
	/// in and stops at stage 22, until they reach Alchemy 100 and come back to mix it.
	/// <para>Unas's parchment and the potion are player tags, the quest-item rule; the potion can't be brewed (the user's call). Qworet's and Unas's own quest (q023) isn't ported: only their
	/// q006 lines and their plain pages. The innkeeper (q007's c_hospod) gets only Eriol's line here; q007 is step 14.</para>
	/// </summary>
	public static class Q006 {
		public const string EriolDef = "c_q006_eriol";
		public const string QworetDef = "c_q023_ShamanQworet";
		public const string UnasDef = "c_q023_CarodejUnas";
		public const string InnkeeperDef = "c_hospod";
		public const string RobeDef = "i_roba_alchemisty";
		public const string ThighBag = "i_q006_vacek_stehno";
		public static readonly string[] EssenceBags = { "i_q006_vacek_led", "i_q006_vacek_kamen", "i_q006_vacek_zem", "i_q006_vacek_snih" };
		public const int UnasMinLevel = 15;
		public const int RobePrice = 2000000;
		public const int RobeBonus = 100; //tenths: Erebor's src.alchemy+10.0
		public const int Done = 24, DoneWithoutRobe = 22;

		private static readonly TagKey stageTK = TagKey.Acquire("_q006Stage_");
		private static readonly TagKey parchmentTK = TagKey.Acquire("_q006Pergamen_");
		private static readonly TagKey potionTK = TagKey.Acquire("_q006Lektvar_");
		internal static readonly TagKey RobeOwnerTK = TagKey.Acquire("_q006RobeOwner_");
		internal static readonly TagKey RobeBoostTK = TagKey.Acquire("_q006RobeBoost_");

		public static QuestJournalEntry Journal { get; private set; }

		public static void Bootstrap() {
			Journal = QuestJournal.Register(new QuestJournalEntry("Erioluv vnuk", 0x3070, GetJournalText, p => GetStage(p) == Done));
		}

		//called by ClassManager on every startup/recompile, see ExpertQuest.Init
		public static void Init() {
			foreach (string npc in new[] { EriolDef, QworetDef, UnasDef }) { //the innkeeper is q007's (D_Q007Hospodsky has Eriol's line)
				QuestNpc.Attach(npc, SingletonScript<E_Q006Npc>.Instance);
			}
			QuestNpc.Attach(RobeDef, SingletonScript<E_Q006Robe>.Instance);
		}

		public static int GetStage(Player player) {
			object tag = player.GetTag(stageTK);
			return (tag == null) ? 0 : Convert.ToInt32(tag, CultureInfo.InvariantCulture);
		}

		public static void SetStage(Player player, int stage) {
			player.SetTag(stageTK, stage);
		}

		/// <summary>Erebor's qtag(q006,n,22): the next stage, announced by the journal's scroll.</summary>
		public static void Advance(Player player, int stage) {
			SetStage(player, stage);
			QuestJournal.Report(player, Journal);
		}

		public static bool HasParchment(Player player) {
			return player.GetTag(parchmentTK) != null;
		}

		internal static void SetParchment(Player player, bool has) {
			if (has) {
				player.SetTag(parchmentTK, true);
			} else {
				player.RemoveTag(parchmentTK);
			}
		}

		/// <summary>The potion of purification: a quest item, so a tag (the user, 2026-11-07: not brewable, only from the quest's NPC).</summary>
		public static bool HasPotion(Player player) {
			return player.GetTag(potionTK) != null;
		}

		internal static void SetPotion(Player player, bool has) {
			if (has) {
				player.SetTag(potionTK, true);
			} else {
				player.RemoveTag(potionTK);
			}
		}

		internal static string GetJournalText(Player player) {
			int stage = GetStage(player);
			return ((stage < 1) || (stage > Done)) ? null : JournalTexts[stage - 1];
		}

		internal static int Alchemy(Player player) {
			return player.GetSkill((int) SkillName.Alchemy);
		}

		#region goods
		/// <summary>Erebor's rescount: what the pack holds, its bags too. Errand items don't count.</summary>
		internal static int Count(Player player, string defname) {
			ItemDef def = ThingDef.GetByDefname(defname) as ItemDef;
			return ((def == null) || (player.Backpack == null)) ? 0 : Errands.CountIn(player.Backpack, def);
		}

		internal static void Consume(Player player, string defname, int count) {
			ItemDef def = ThingDef.GetByDefname(defname) as ItemDef;
			if ((def != null) && (player.Backpack != null)) {
				Errands.Consume(player.Backpack, def, count);
			}
		}

		/// <summary>A collector bag the quest hands out, newbie and fixed in the pack as Erebor's ATTR=010|04 (the converter drops
		/// @create's ATTR): it can't be dropped, sold, traded or lost at death, and nothing would give another.</summary>
		internal static Item GiveBag(Player player, string defname) {
			Item bag = Give(player, defname);
			bag.SetAttribute(ItemAttributes.attr_newbie, true);
			bag.SetAttribute(ItemAttributes.attr_move_never, true);
			return bag;
		}

		internal static Item Give(Player player, string defname, int amount = 1) {
			Item item = (Item) ((ItemDef) ThingDef.GetByDefname(defname)).Create(player.Backpack);
			if (amount > 1) {
				item.Amount = amount;
			}
			return item;
		}

		/// <summary>Erebor's i_pet_dog: a dog shrunk into a figurine. Only a real dog, c_dog, as Eriol asks.</summary>
		internal static PetFigurine FindDog(Player player) {
			return (player.Backpack == null) ? null : FindDog(player.Backpack);
		}

		private static PetFigurine FindDog(Container container) {
			foreach (AbstractItem item in container) {
				if ((item is PetFigurine figurine) && (figurine.petDef != null) && (figurine.petDef == ThingDef.GetByDefname("c_dog"))) {
					return figurine;
				}
				if ((item is Container inside) && (inside.Count > 0)) {
					PetFigurine inner = FindDog(inside);
					if (inner != null) {
						return inner;
					}
				}
			}
			return null;
		}

		/// <summary>Erebor's f_q006_hasSubquestResources_7.</summary>
		internal static bool HasPotionIngredients(Player player) {
			return (Count(player, "i_potion_curegreat") >= 3) && (Count(player, "i_bylina") >= 1) && (Count(player, "i_srdiczko") >= 2);
		}

		/// <summary>The bag Unas wants at this stage (14-17), or null.</summary>
		internal static string EssenceBagFor(int stage) {
			return ((stage >= 14) && (stage <= 17)) ? EssenceBags[stage - 14] : null;
		}

		internal static void Reward(Player player, int experience) {
			LevelingSystem.AddExperience(player, experience);
		}

		/// <summary>The robe, bound to `owner` (Erebor's tag.owner = uid).</summary>
		internal static Item GiveRobe(Player owner) {
			Item robe = Give(owner, RobeDef);
			robe.SetTag(RobeOwnerTK, owner);
			return robe;
		}
		#endregion goods

		#region the robe
		/// <summary>Erebor's i_roba_alchemisty @equip: +10 Alchemy for its owner, if they're at 100 already.</summary>
		internal static void OnEquip(Character ch, Item item) {
			if (item.Def != ThingDef.GetByDefname(RobeDef)) {
				return;
			}
			Player player = ch as Player;
			if ((player == null) || (Alchemy(player) <= 999)) {
				return;
			}
			Player owner = item.GetTag(RobeOwnerTK) as Player;
			if (owner == null) {
				player.SysMessage("Magie obsazena v robe te odmitla!");
			} else if ((owner == player) && (item.GetTag(RobeBoostTK) == null)) {
				player.ModifySkillValue(SkillName.Alchemy, RobeBonus);
				item.SetTag(RobeBoostTK, player);
				player.SysMessage("Citis mistrovstvi spjate s touto robou ...");
			}
		}

		/// <summary>@unequip: what @equip gave goes back. The robe keeps who it gave to, so a save between the two is fine.</summary>
		internal static void OnUnequip(Character ch, Item item) {
			if (item.GetTag(RobeBoostTK) is Character boosted) {
				item.RemoveTag(RobeBoostTK);
				if (!boosted.IsDeleted) {
					boosted.ModifySkillValue(SkillName.Alchemy, -RobeBonus);
				}
			}
		}
		#endregion the robe

		#region talking
		/// <summary>The NPC's @UserDClick or speech.</summary>
		internal static void Talk(NPC npc, Player player) {
			if (player.Flag_Dead) {
				return;
			}
			npc.Dialog(player, DialogOf(npc), npc);
		}

		//by the def itself: PrettyDefname needn't keep the case its .def wrote (c_q023_ShamanQworet), #1266 found it
		internal static QuestDialogDef DialogOf(NPC npc) {
			if (npc.Def == ThingDef.GetByDefname(EriolDef)) {
				return SingletonScript<D_Q006Eriol>.Instance;
			} else if (npc.Def == ThingDef.GetByDefname(QworetDef)) {
				return SingletonScript<D_Q006Qworet>.Instance;
			}
			return SingletonScript<D_Q006Unas>.Instance;
		}
		#endregion talking

		/// <summary>Erebor's d_questy_erioluv_vnuk texts, stage 1 to 24.</summary>
		internal static readonly string[] JournalTexts = {
			"<?src.sex(Slibil,Slibila)?> jsi Eriolovi deset pochodni.",
			"<?src.sex(Prinesl,Prinesla)?> jsi Eriolovi deset pochodni.",
			"<?src.sex(Slibil,Slibila)?> jsi Eriolovi vycviceneho a zmenseneho psa.",
			"<?src.sex(Prinesl,Prinesla)?> jsi Eriolovi psa.",
			"<?src.sex(Slibil,Slibila)?> jsi Eriolovi tri slabsi protijedy.",
			"<?src.sex(Prinesl,Prinesla)?> jsi Eriolovi tri slabsi protijedy.",
			"<?src.sex(Slibil,Slibila)?> jsi Eriolovi deset nejsilnejsich protijedu.",
			"<?src.sex(Prinesl,Prinesla)?> jsi Eriolovi deset nejsilnejsich protijedu.",
			"<?src.sex(Slibil,Slibila)?> jsi Eriolovi tri silne lektvary osvezeni a deset stehen medveda grizzlyho.",
			"<?src.sex(Prinesl,Prinesla)?> jsi Eriolovi tri silne lektvary osvezeni a deset stehen medveda grizzlyho.",
			"<?src.sex(Slibil,Slibila)?> jsi Eriolovi, ze si promluvis se samanem Qworetem, ktery prebyva nekde pobliz Dolu a Osamele Hory.",
			"<?src.sex(Mluvil,Mluvila)?> jsi se samanem Qworetem, ale nic jsi se <?src.sex(nedozvedel,nedozvedela)?>, jdi tedy zpet k Eriolovi.",
			"Eriol te poslal za carodejem Unasem, ktery mu mozna bude umet poradit. Carodej Unas prebyva nekde blizko Rhunskych mocalu.",
			"<?src.sex(Slibil,Slibila)?> jsi Unasovi dvacet kusu esence z ledoveho elementala.",
			"<?src.sex(Prinesl,Prinesla)?> jsi Unasovi dvacet kusu esence z ledoveho elementala. Ted po tobe chce dvacet kusu esence z kamenneho elementala.",
			"<?src.sex(Prinesl,Prinesla)?> jsi Unasovi dvacet kusu esence z kamenneho elementala. Ted po tobe chce dvacet kusu esence z elementala zeme.",
			"<?src.sex(Prinesl,Prinesla)?> jsi Unasovi dvacet kusu esence z elementala zeme. Ted po tobe chce dvacet kusu esence ze snezneho elementala.",
			"<?src.sex(Ziskal,Ziskala)?> jsi od Unase pergamen pro Eriola.",
			"Eriol te s pergamenem poslal pro radu za hospodskym, pritelem alchymisty Terlaka.",
			"Hospodsky ti rekl o Lektvaru Ocisteni, ktery by mohl pomoci. K jeho priprave jsou potreba dve srdce ptaka krvavce, tri nejsilnejsi protijedy a vzacna bylina. Musis to rict Eriolovi.",
			"Musis Eriolovi prinest dve srdce ptaka krvavce, tri nejsilnejsi protijedy a vzacnou bylinu.",
			"<?src.sex(Prinesl,Prinesla)?> jsi Eriolovi vsechny prisady na Lektvar Ocisteni, za coz ti je velmi vdecny. Eriol ale stale jeste hleda nekoho, kdo by mu tento lektvar namichal.",
			"Eriol ceka, az mu dorucis hotovy Lektvar Ocisteni.",
			"<?src.sex(Pomohl,Pomohla)?> jsi Eriolovi vylecit jeho vnuka, za coz je ti velmi vdecny a bohate se ti odvdecil kouzelnou robou."
		};
	}

	/// <summary>#1264 - @UserDClick of q006's NPCs.</summary>
	public class E_Q006Npc : CompiledTriggerGroup {
		public TriggerResult On_DClick(NPC self, Character clicker) {
			Player player = clicker as Player;
			if (player == null) {
				return TriggerResult.Continue;
			}
			if (QuestNpc.IsCloseEnough(self, player)) {
				Q006.Talk(self, player);
			}
			return TriggerResult.Cancel;
		}
	}

	/// <summary>#1264 - i_roba_alchemisty's click line, and (#1475) its own @equip and @unequip.</summary>
	public class E_Q006Robe : CompiledTriggerGroup {
		/// <summary>#1475 - i_roba_alchemisty's own @equip and @unequip.</summary>
		public void On_Equip(Item self, ItemInCharArgs args) {
			if (args.Cont is Character ch) {
				Q006.OnEquip(ch, self);
			}
		}

		public void On_UnEquip(Item self, ItemInCharArgs args) {
			if (args.Cont is Character ch) {
				Q006.OnUnequip(ch, self);
			}
		}

		public TriggerResult On_Click(Item self, Character clicker) {
			if (clicker is Player player) {
				self.ClickMessage(player, "+ 10% ke skillu Alchemy");
			}
			return TriggerResult.Continue;
		}
	}

	/// <summary>#1264 - spk_q006_eriol, spk_q023_ShamanQworet and spk_q023_CarodejUnas: their greetings, from close enough. The
	/// innkeeper's spk_q007_hospodsky is Q007Speech's.</summary>
	public sealed class Q006Speech : AbstractSpeech {
		private static readonly string[] greetings = { "ahoj", "zdravi", "dobry", "ukol" };
		private readonly string[] patterns;

		public new static void Bootstrap() {
			new Q006Speech("spk_q006_eriol", greetings).Register();
			new Q006Speech("spk_q023_ShamanQworet", greetings).Register();
			new Q006Speech("spk_q023_CarodejUnas", greetings).Register();
		}

		private Q006Speech(string defname, string[] patterns)
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
				Q006.Talk(npc, player);
			}
			return SpeechResult.IgnoredOrActedUpon;
		}
	}
}
