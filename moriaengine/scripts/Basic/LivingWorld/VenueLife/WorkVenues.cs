using System;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1258 - the smithy, the temple and the Houses of Healing in a resident's dialog: the bellows and ore, an offering
	/// and a night vigil, dressing a patient's wounds (design 9.2). The armoury's life is in its errands (WorkErrands.cs).</summary>
	public static class WorkVenues {
		internal static readonly TagKey BellowsAtTK = TagKey.Acquire("_smithy_bellows_at_");
		internal static readonly TagKey BellowsDayTK = TagKey.Acquire("_smithy_bellows_day_");
		private static readonly TagKey bellowsCountTK = TagKey.Acquire("_smithy_bellows_count_");
		internal static readonly TagKey OfferingTK = TagKey.Acquire("_temple_offering_");
		internal static readonly TagKey VigilDoneTK = TagKey.Acquire("_temple_vigil_done_");

		private static readonly VenueGoods.Lines oreLines = new VenueGoods.Lines {
			Ask = wage => "Zelezna ruda se vzdycky hodi. Zaplatim " + wage + " zlatych za kus.",
			Full = "Rudy mam na dnesek dost. Prijd zitra.",
			Thanks = (pieces, pay) => "Diky, " + pieces + " rudy se hodi. Tady mas " + pay + " zlatych."
		};

		public static void Bootstrap() {
			VenueActions.Register(new VenueAction("smithy_bellows", "Pomoct u mechu", new[] { "smithy" }, new[] { "smith", "apprentice" }, null, WorkBellows));
			VenueActions.Register(new VenueAction("smithy_ore", "Prinest rudu", new[] { "smithy" }, new[] { "smith" }, null,
				(player, npc, gem) => VenueGoods.Buy(player, npc, gem, "i_ore_iron", Settings.oreWage, Settings.orePerDay, "smithore", oreLines)));
			VenueActions.Register(new VenueAction("temple_offering", "Obetovat", new[] { "temple" }, new[] { "priest", "acolyte" }, null, Offer));
			VenueActions.Register(new VenueAction("temple_vigil", "Nocni bdeni", new[] { "temple" }, new[] { "priest" },
				(player, npc, gem) => (VenueClock.Shift == VenueShift.Night) && (player.GetTimer(VigilTimer.Key) == null), StartVigil));
			VenueActions.Register(new VenueAction("healing_bandage", "Obvazat", new[] { "healing" }, new[] { "patient" }, null, Bandage));
		}

		private static VenueLifeSettings Settings {
			get { return VenueLifeSettings.instance; }
		}

		private static bool WithinADay(object ticks) {
			return (ticks is long at) && ((Errands.Now.Ticks - at) < TimeSpan.TicksPerDay);
		}

		#region The smithy
		internal static void WorkBellows(Player player, NPC npc, Item gem) {
			long now = Errands.Now.Ticks;
			if ((player.GetTag(BellowsAtTK) is long at) && ((now - at) < Settings.bellowsMinutes * TimeSpan.TicksPerMinute)) {
				npc.Say("Odpocin si chvili, vyhen to vydrzi.");
				return;
			}
			bool newDay = !WithinADay(player.GetTag(BellowsDayTK));
			int today = newDay ? 0 : ((player.GetTag(bellowsCountTK) is int c) ? c : 0);
			if (today >= Settings.bellowsPerDay) {
				npc.Say("Na dnesek staci. Prijd zitra.");
				return;
			}
			if (player.Stam < Settings.bellowsStamina) {
				npc.Say("Vypadas unavene. Prijd, az naberes sily.");
				return;
			}
			player.Stam = (short) (player.Stam - Settings.bellowsStamina);
			player.SetTag(BellowsAtTK, now);
			player.SetTag(bellowsCountTK, today + 1);
			if (newDay) {
				player.SetTag(BellowsDayTK, now);
				string id = Residents.IdOf(gem);
				if (id != null) {
					Residents.AddFriendship(player, id, 1);
				}
			}
			GoldUtils.Grant(player, (uint) Math.Max(0, Settings.bellowsWage));
			npc.Say(ErrandContext.Pick("Tak, poradne! Uhli zari jako slunce.", "Dobre, jeste chvili... a dost. Diky.", "Takhle se zeleze kuje. Tady mas za praci."));
		}
		#endregion

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

		internal static void StartVigil(Player player, NPC npc, Item gem) {
			if (WithinADay(player.GetTag(VigilDoneTK))) {
				npc.Say("Dnesni bdeni uz mas za sebou. Prijd zitra v noci.");
				return;
			}
			CityVenue temple = Residents.WorkplaceOf(gem);
			if (temple == null) {
				return;
			}
			if (VenueRegistry.At(player) != temple) {
				npc.Say("Pojd dovnitr chramu. Bdi se uvnitr.");
				return;
			}
			VigilTimer timer = new VigilTimer { venueId = temple.VenueId, priestId = Residents.IdOf(gem), left = Math.Max(1, Settings.vigilSeconds) };
			player.AddTimer(VigilTimer.Key, timer);
			timer.DueInSeconds = VigilTimer.StepSeconds;
			timer.PeriodInSeconds = VigilTimer.StepSeconds;
			npc.Say("Zustan tady a v tichu rozjimej. Kdo odejde, bdeni prerusi.");
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

	/// <summary>#1258 - a night vigil in a temple: every StepSeconds the player must still be in it, alive and online; at the end
	/// experience, karma and the priest's friendship.</summary>
	[SaveableClass, DeepCopyableClass]
	public class VigilTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_temple_vigil_");
		public const int StepSeconds = 10;

		[SaveableData, CopyableData]
		public string venueId, priestId;

		[SaveableData, CopyableData]
		public int left;

		[LoadingInitializer, DeepCopyImplementation]
		public VigilTimer() {
		}

		/// <summary>The vigil after this many seconds; false once it has ended, either way.</summary>
		public bool Step(Player player, int seconds) {
			CityVenue temple = VenueRegistry.ById(this.venueId);
			if ((player == null) || (temple == null)) {
				return false;
			}
			if (player.Flag_Disconnected || player.Flag_Dead || (VenueRegistry.At(player) != temple)) {
				player.SysMessage("Bdeni je preruseno: v chramu uz nejsi.");
				return false;
			}
			this.left -= seconds;
			if (this.left > 0) {
				return true;
			}
			VenueLifeSettings s = VenueLifeSettings.instance;
			player.SetTag(WorkVenues.VigilDoneTK, Errands.Now.Ticks);
			LevelingSystem.AddExperience(player, Math.Max(0, s.vigilExp));
			player.Karma = (short) Math.Min(short.MaxValue, player.Karma + s.vigilKarma);
			if (this.priestId != null) {
				Residents.AddFriendship(player, this.priestId, 1);
			}
			player.SysMessage("Noc v tichu chramu skoncila. Citis klid a jasnou mysl.");
			return false;
		}

		protected override void OnTimeout(TagHolder cont) {
			if (!this.Step(cont as Player, StepSeconds)) {
				this.Delete();
			}
		}
	}
}
