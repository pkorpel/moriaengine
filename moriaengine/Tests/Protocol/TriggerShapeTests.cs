using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1558 P3 - triggers reshaped to the architecture's section 8: each from its own Trigger_X, in the moment it
	/// names, with args a handler can read. Server side: scripts/Tests/TriggerShapeTests.cs. Three logins on its account.</summary>
	[Collection(TriggerShapeCollection.Name)]
	public class TriggerShapeTests : LivingWorldTestBase {
		public TriggerShapeTests(GameServerPairFixture servers)
			: base(servers, "triggershape", "SHAPE") {
		}

		[Fact]
		public void NotificationsCarryTheirArgsAndTheMenuSelectionComesFirst() {
			using (TestGameClient game = this.Login("Notifikator")) {
				this.Run(game, ".TestShapeNotifications", "SHAPE_NOTIFY_DONE");
				this.AssertChecks("disruptionArgs", "noHostileActionWithoutEnemy", "hostileActionWithEnemy", "dispelBombRaisesDispell");
				this.Run(game, ".TestShapeContextMenu", "SHAPE_MENU_DONE");
				this.AssertChecks("menuSelectBeforeAction", "menuSelectCancelStopsAction");
			}
		}

		[Fact]
		public void PotionsActOnceReallyPutDownOrLifted() {
			using (TestGameClient game = this.Login("Lektvarnik")) {
				this.Run(game, ".TestShapePotions", "SHAPE_POTIONS_DONE");
				this.AssertChecks("macroIsADoubleClick", "bombDropArms", "dragonCureBreaksWhenPutDown", "dragonCureMadeOnGroundStays",
					"liftedBombGoesOff", "liftedBombHurtsWhereItLay");
			}
		}

		[Fact]
		public void AScriptedGainExperienceChangesTheAmount() {
			using (TestGameClient game = this.Login("Zkusenak")) {
				this.Run(game, ".TestShapeGainExperience", "SHAPE_GAIN_DONE");
				this.AssertChecks("scriptedGainChangesAmount");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class TriggerShapeCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "TriggerShape";
	}
}
