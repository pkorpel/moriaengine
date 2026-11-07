using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1258 - the smithy, the temple and the Houses of Healing in a resident's dialog: the bellows and ore, an offering
	/// and a night vigil, dressing a patient's wounds (design 9.2). The armoury's life is in its errands (WorkErrands.cs).</summary>
	public static class WorkVenues {
		internal static readonly TagKey OfferingTK = TagKey.Acquire("_temple_offering_");

		private static readonly VenueLabour.Lines bellowsLines = new VenueLabour.Lines {
			Rest = "Odpocin si chvili, vyhen to vydrzi.",
			Tired = "Vypadas unavene. Prijd, az naberes sily.",
			Done = "Na dnesek staci. Prijd zitra.",
			Thanks = new[] { "Tak, poradne! Uhli zari jako slunce.", "Dobre, jeste chvili... a dost. Diky.", "Takhle se zeleze kuje. Tady mas za praci." }
		};

		private static readonly VenueGoods.Lines oreLines = new VenueGoods.Lines {
			Ask = wage => "Zelezna ruda se vzdycky hodi. Zaplatim " + wage + " zlatych za kus.",
			Full = "Rudy mam na dnesek dost. Prijd zitra.",
			Thanks = (pieces, pay) => "Diky, " + pieces + " rudy se hodi. Tady mas " + pay + " zlatych."
		};

		public static void Bootstrap() {
			VenueActions.Register(new VenueAction("smithy_bellows", "Pomoct u mechu", new[] { "smithy" }, new[] { "smith", "apprentice" }, null,
				(player, npc, gem) => VenueLabour.Work(player, npc, gem, "smithy", bellowsLines)));
			VenueActions.Register(new VenueAction("smithy_ore", "Prinest rudu", new[] { "smithy" }, new[] { "smith" }, null,
				(player, npc, gem) => VenueGoods.Buy(player, npc, gem, "i_ore_iron", Settings.oreWage, Settings.orePerDay, "smithore", oreLines)));
			VenueActions.Register(new VenueAction("temple_offering", "Obetovat", new[] { "temple" }, new[] { "priest", "acolyte" }, null, Offer));
			VenueActions.Register(new VenueAction("temple_vigil", "Nocni bdeni", new[] { "temple" }, new[] { "priest" },
				(player, npc, gem) => (VenueClock.Shift == VenueShift.Night) && !VenueStay.IsStaying(player), (player, npc, gem) => VenueStay.Begin(player, npc, gem, "vigil")));
			VenueStay.Register(new StayKind("vigil", () => Settings.vigilSeconds, "Zustan tady a v tichu rozjimej. Kdo odejde, bdeni prerusi.", VigilEnds));
			VenueActions.Register(new VenueAction("healing_bandage", "Obvazat", new[] { "healing" }, new[] { "patient" }, null, Bandage));
		}

		private static VenueLifeSettings Settings {
			get { return VenueLifeSettings.instance; }
		}

		private static bool WithinADay(object ticks) {
			return (ticks is long at) && ((Errands.Now.Ticks - at) < TimeSpan.TicksPerDay);
		}

		#region The temple
		internal static void Offer(Player player, NPC npc, Item gem) {
			if (WithinADay(player.GetTag(OfferingTK))) {
				npc.Say("Tvuj dnesni dar uz byl prijat. Prijd zitra.");
				return;
			}
			int price = Settings.offeringPrice;
			if (!GoldUtils.Pay(player, price, PaySource.Usual, "")) {
				npc.Say("Dar chramu je " + price + " zlatych. Tolik u sebe nemas.");
				return;
			}
			player.SetTag(OfferingTK, Errands.Now.Ticks);
			player.Karma = (short) Math.Min(short.MaxValue, player.Karma + Settings.offeringKarma);
			string id = Residents.IdOf(gem);
			if (id != null) {
				Residents.AddFriendship(player, id, 1);
			}
			npc.Say(ErrandContext.Pick("At te Valar provazeji.", "Tvuj dar pomuze chudym. Bud pozehnan.", "Svetlo at ti sviti na cestu."));
		}

		//the night in the temple's quiet: experience, karma and the priest's friendship
		private static void VigilEnds(Player player, CityVenue temple, string priestId) {
			LevelingSystem.AddExperience(player, Math.Max(0, Settings.vigilExp));
			player.Karma = (short) Math.Min(short.MaxValue, player.Karma + Settings.vigilKarma);
			if (priestId != null) {
				Residents.AddFriendship(player, priestId, 1);
			}
			player.SysMessage("Noc v tichu chramu skoncila. Citis klid a jasnou mysl.");
		}
		#endregion

		#region The Houses of Healing
		internal static void Bandage(Player player, NPC npc, Item gem) {
			string id = Residents.IdOf(gem);
			if (id == null) {
				return;
			}
			TagKey key = Tavern.KeyFor("_heal_bandage_", id);
			if (WithinADay(player.GetTag(key))) {
				npc.Say("Obvaz mam dnes cerstvy. Diky.");
				return;
			}
			if (player.GetSkill(SkillName.Healing) < Settings.bandageHealing) {
				npc.Say("Radsi ne... na tohle je potreba zkusena ruka.");
				return;
			}
			ItemDef bandage = ThingDef.GetByDefname("i_bandage") as ItemDef;
			if ((bandage == null) || (Errands.CountIn(player.Backpack, bandage) <= 0)) {
				npc.Say("Nemas ani obvaz...");
				return;
			}
			if (Errands.Consume(player.Backpack, bandage, 1) > 0) {
				return;
			}
			player.SetTag(key, Errands.Now.Ticks);
			Residents.AddFriendship(player, id, 1);
			GoldUtils.Grant(player, (uint) Math.Max(0, Settings.bandageWage));
			npc.Say(ErrandContext.Pick("Hned je to lepsi. Dekuju.", "Au... ale uz to tolik nepali. Diky.", "Mas jemne ruce. Dekuju."));
			player.SysMessage("Lecebna ti za obvaz zaplatila " + Settings.bandageWage + " zlatych.");
		}
		#endregion
	}
}
