using System;
using System.Globalization;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {
	/// <summary>Verification harness for the item ATTR flag core infrastructure (#215/#217) -
	/// see Tests/Protocol/AttrFlagTests.cs.</summary>
	public static class AttrFlagTests {
		/// <summary>Confirms a real ".def" "ATTR = attr_static|attr_magic" line (see
		/// AttrFlagTest.def) parsed correctly into AttributesDefault, and that a freshly
		/// created instance's own Attributes got seeded from it at creation time.</summary>
		[SteamFunction]
		public static void TestAttrDefParsing(Character self) {
			ItemDef testDef = (ItemDef) ThingDef.GetByDefname("i_attr_flag_test");
			ItemAttributes defDefault = testDef.AttributesDefault;
			Item instance = (Item) testDef.Create(self.P());
			Console.WriteLine("ATTR_DEF_TEST: defDefault=" + defDefault + " instanceAttributes=" + instance.Attributes +
				" hasStatic=" + instance.HasAttribute(ItemAttributes.attr_static) +
				" hasMagic=" + instance.HasAttribute(ItemAttributes.attr_magic) +
				" hasNewbie=" + instance.HasAttribute(ItemAttributes.attr_newbie));
		}

		/// <summary>Confirms HasAttribute/SetAttribute (the programmatic API #218-225's real
		/// behaviors will consume) correctly read/write the same underlying bitmask.</summary>
		[SteamFunction]
		public static void TestAttrProgrammaticToggle(Character self) {
			ItemDef plainDef = (ItemDef) ThingDef.GetByDefname("i_rock_plain");
			Item instance = (Item) plainDef.Create(self.P());
			bool beforeAny = instance.Attributes != ItemAttributes.none;
			instance.SetAttribute(ItemAttributes.attr_newbie, true);
			instance.SetAttribute(ItemAttributes.attr_invis, true);
			bool afterToggle = instance.HasAttribute(ItemAttributes.attr_newbie) && instance.HasAttribute(ItemAttributes.attr_invis);
			instance.SetAttribute(ItemAttributes.attr_newbie, false);
			bool afterUntoggle = !instance.HasAttribute(ItemAttributes.attr_newbie) && instance.HasAttribute(ItemAttributes.attr_invis);
			Console.WriteLine("ATTR_BOOL_TEST: beforeAny=" + beforeAny + " afterToggle=" + afterToggle + " afterUntoggle=" + afterUntoggle);
		}

		/// <summary>Creates a plain test item near self and reports its uid, so a protocol
		/// test can drive the real D_AttrList dialog against it.</summary>
		[SteamFunction]
		public static void TestCreateAttrTestItem(Character self) {
			ItemDef plainDef = (ItemDef) ThingDef.GetByDefname("i_rock_plain");
			Item instance = (Item) plainDef.Create(self.P());
			Console.WriteLine("ATTR_ITEM_SETUP: uid=" + instance.Uid);
		}

		/// <summary>Reports the current Attributes bitmask of the item at the given uid, after
		/// a D_AttrList dialog round-trip.</summary>
		[SteamFunction]
		public static void TestReportAttrTestItem(Character self, ScriptArgs sa) {
			uint uid = Convert.ToUInt32(sa.Argv[0], System.Globalization.CultureInfo.InvariantCulture);
			Item instance = (Item) Thing.UidGetItem(uid);
			Console.WriteLine("ATTR_ITEM_REPORT: attributes=" + instance.Attributes);
		}

		/// <summary>#218 - drops 5 plain items into self's own backpack, one per death-drop
		/// ATTR flag (plus one plain control item with none), then kills self via a freshly
		/// spawned throwaway NPC and reports whether each item stayed in the backpack (did NOT
		/// fall to the corpse) or moved elsewhere (fell). Calling CauseDeath directly and
		/// synchronously (not deferred via a timer) matches Phase 3's own
		/// TestKillPlayerAndReportResult precedent - a PLAYER's death sound resolves fine
		/// synchronously inside this command's call stack, unlike an NPC's (see
		/// DeferredNpcKillTimer's own doc comment for why that one needed deferring).</summary>
		[SteamFunction]
		public static void TestDeathDropAttrItems(Character self) {
			Player victim = (Player) self;
			Container backpack = victim.Backpack;
			ItemDef plainDef = (ItemDef) ThingDef.GetByDefname("i_rock_plain");

			Item newbieItem = (Item) plainDef.Create(backpack);
			newbieItem.SetAttribute(ItemAttributes.attr_newbie, true);
			Item blessedItem = (Item) plainDef.Create(backpack);
			blessedItem.SetAttribute(ItemAttributes.attr_blessed, true);
			Item cursedItem = (Item) plainDef.Create(backpack);
			cursedItem.SetAttribute(ItemAttributes.attr_cursed, true);
			Item insuredItem = (Item) plainDef.Create(backpack);
			insuredItem.SetAttribute(ItemAttributes.attr_insured, true);
			Item plainItem = (Item) plainDef.Create(backpack);

			NPCDef killerDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC killer = (NPC) killerDef.Create(victim.P());

			victim.CauseDeath(killer);

			Console.WriteLine("ATTR_DEATH_RESULT:" +
				" newbieStayed=" + (newbieItem.Cont == backpack) +
				" blessedStayed=" + (blessedItem.Cont == backpack) +
				" cursedStayed=" + (cursedItem.Cont == backpack) +
				" insuredStayed=" + (insuredItem.Cont == backpack) +
				" plainStayed=" + (plainItem.Cont == backpack) +
				" insuredConsumed=" + !insuredItem.HasAttribute(ItemAttributes.attr_insured));
		}

		/// <summary>#371 - shrinks the configured decay interval to a test-practical speed (same
		/// SpeedUpClockForTest precedent as LightAndWeather/#76), then creates 5 ground/contained
		/// items covering decay-by-default plus every exemption case: a plain item with NO
		/// attributes at all (should decay anyway now - proves the new default-on behavior,
		/// superseding #219's original default-off "noDecayFlag" case), attr_questitem (exempt),
		/// attr_lockeddown (exempt - new #371 exemption), attr_secure (exempt - new #371
		/// exemption), and a plain item sitting inside a backpack (exempt while contained,
		/// regardless of the flag default). Reports their uids so a protocol test can poll each
		/// one.</summary>
		[SteamFunction]
		public static void TestSetupDecayItems(Character self) {
			//#219 (redesigned) - no more global sweep to speed up; each item's own timer reads
			//this setting at the moment it's scheduled (when created/flagged below), so setting
			//it first is all a test needs to do.
			DecaySettings.instance.itemDecayMinutes = 0.01; //0.6 real seconds

			ItemDef plainDef = (ItemDef) ThingDef.GetByDefname("i_rock_plain");

			Item plainNoFlags = (Item) plainDef.Create(self.P());

			Item decayQuestItem = (Item) plainDef.Create(self.P());
			decayQuestItem.SetAttribute(ItemAttributes.attr_questitem, true);

			Item lockeddownItem = (Item) plainDef.Create(self.P());
			lockeddownItem.SetAttribute(ItemAttributes.attr_lockeddown, true);

			Item secureItem = (Item) plainDef.Create(self.P());
			secureItem.SetAttribute(ItemAttributes.attr_secure, true);

			Item decayButContained = (Item) plainDef.Create(((Player) self).Backpack);

			Console.WriteLine("ATTR_DECAY_SETUP: plainNoFlags=" + plainNoFlags.Uid +
				" decayQuestItem=" + decayQuestItem.Uid +
				" lockeddownItem=" + lockeddownItem.Uid +
				" secureItem=" + secureItem.Uid +
				" decayButContained=" + decayButContained.Uid);
		}

		/// <summary>#219 (redesigned) - creates a decay-eligible item and immediately triggers
		/// a real Globals.Save(), so a protocol test can inspect the resulting things.sav and
		/// confirm the per-item ItemDecayTimer actually got serialized (proving the per-item-
		/// timer design is genuinely persistent, which was the user's own explicit motivation
		/// for this redesign - not just assumed because the generic mechanism "should" work).
		/// A long interval (5 real minutes) so it's still pending, not already fired, at the
		/// moment the save happens.</summary>
		[SteamFunction]
		public static void TestSetupPersistentDecayItemAndSave(Character self) {
			DecaySettings.instance.itemDecayMinutes = 5;
			ItemDef plainDef = (ItemDef) ThingDef.GetByDefname("i_rock_plain");
			Item item = (Item) plainDef.Create(self.P());
			item.SetAttribute(ItemAttributes.attr_decay, true);
			Globals.Save();
			Console.WriteLine("ATTR_DECAY_PERSIST_SETUP: uid=" + item.Uid);
		}

		/// <summary>Reports whether the item at the given uid still exists - 0 if it decayed
		/// away (Thing.UidGetItem resolves nothing for a uid no object owns anymore).</summary>
		[SteamFunction]
		public static void TestReportDecayItemExists(Character self, ScriptArgs sa) {
			uint uid = Convert.ToUInt32(sa.Argv[0], System.Globalization.CultureInfo.InvariantCulture);
			bool exists = Thing.UidGetItem(uid) != null;
			Console.WriteLine("ATTR_DECAY_EXISTS: uid=" + uid + " exists=" + exists);
		}

		/// <summary>#220 - creates one plain ground item configured per "mode" (quoted since
		/// this is a bareword-sensitive elevated-account command, see the "sa" arg note on
		/// other Test* functions in this file): "static"/"movenever" should end up
		/// Flag_NonMovable, "movealwaysoverride" sets both attr_static AND attr_move_always to
		/// confirm the override wins, "plain" sets nothing (control case, stays movable).</summary>
		[SteamFunction]
		public static void TestSetupMovementAttrItem(Character self, ScriptArgs sa) {
			string mode = sa.Argv[0].ToString();
			ItemDef plainDef = (ItemDef) ThingDef.GetByDefname("i_rock_plain");
			Item item = (Item) plainDef.Create(self.P());
			switch (mode) {
				case "static":
					item.SetAttribute(ItemAttributes.attr_static, true);
					break;
				case "movenever":
					item.SetAttribute(ItemAttributes.attr_move_never, true);
					break;
				case "movealwaysoverride":
					item.SetAttribute(ItemAttributes.attr_static, true);
					item.SetAttribute(ItemAttributes.attr_move_always, true);
					break;
				case "plain":
					break;
			}
			Console.WriteLine("ATTR_MOVE_SETUP: uid=" + item.Uid + " nonMovable=" + item.Flag_NonMovable);
		}

		/// <summary>Reports whether the item at the given uid is still sitting loose on the
		/// ground (Cont == null) - a real client pickup attempt denied by Flag_NonMovable never
		/// changes Cont away from that.</summary>
		[SteamFunction]
		public static void TestReportItemOnGround(Character self, ScriptArgs sa) {
			uint uid = Convert.ToUInt32(sa.Argv[0], System.Globalization.CultureInfo.InvariantCulture);
			Item item = (Item) Thing.UidGetItem(uid);
			Console.WriteLine("ATTR_MOVE_RESULT: uid=" + uid + " stillOnGround=" + (item.Cont == null));
		}

		/// <summary>#222 - creates one plain item per quality flag (plus a no-flag control) and
		/// reports each one's Name, to confirm the name-prefix display effect. Also confirms
		/// the precedence order (reforged beats everything) on a single item carrying both
		/// attr_reforged and attr_magic at once.</summary>
		[SteamFunction]
		public static void TestSetupQualityDisplayItems(Character self) {
			ItemDef plainDef = (ItemDef) ThingDef.GetByDefname("i_rock_plain");

			//#224 - magic/enchanted/imbued/reforged all need attr_identified to show their
			//prefix; attr_exceptional doesn't (a visible craftsmanship mark, not a hidden
			//magical property) - see exceptionalItem below, deliberately left unidentified.
			Item magicItem = (Item) plainDef.Create(self.P());
			magicItem.SetAttribute(ItemAttributes.attr_magic, true);
			magicItem.SetAttribute(ItemAttributes.attr_identified, true);

			Item exceptionalItem = (Item) plainDef.Create(self.P());
			exceptionalItem.SetAttribute(ItemAttributes.attr_exceptional, true);

			Item enchantedItem = (Item) plainDef.Create(self.P());
			enchantedItem.SetAttribute(ItemAttributes.attr_enchanted, true);
			enchantedItem.SetAttribute(ItemAttributes.attr_identified, true);

			Item imbuedItem = (Item) plainDef.Create(self.P());
			imbuedItem.SetAttribute(ItemAttributes.attr_imbued, true);
			imbuedItem.SetAttribute(ItemAttributes.attr_identified, true);

			Item reforgedAndMagicItem = (Item) plainDef.Create(self.P());
			reforgedAndMagicItem.SetAttribute(ItemAttributes.attr_reforged, true);
			reforgedAndMagicItem.SetAttribute(ItemAttributes.attr_magic, true);
			reforgedAndMagicItem.SetAttribute(ItemAttributes.attr_identified, true);

			//#224 - same magic flag as magicItem above, but deliberately left UNidentified -
			//its prefix must stay hidden.
			Item unidentifiedMagicItem = (Item) plainDef.Create(self.P());
			unidentifiedMagicItem.SetAttribute(ItemAttributes.attr_magic, true);

			Item plainItem = (Item) plainDef.Create(self.P());

			Console.WriteLine("ATTR_QUALITY_TEST:" +
				" magic=" + magicItem.Name +
				" exceptional=" + exceptionalItem.Name +
				" enchanted=" + enchantedItem.Name +
				" imbued=" + imbuedItem.Name +
				" reforgedAndMagic=" + reforgedAndMagicItem.Name +
				" unidentifiedMagic=" + unidentifiedMagicItem.Name +
				" plain=" + plainItem.Name);
		}

		/// <summary>#223 - creates an attr_owned item claimed by self, reporting its position
		/// too so a protocol test can teleport a second (non-owner) character there before
		/// attempting pickup - same "CanReach is checked after the deny-trigger chain, so a
		/// too-far-away picker looks identically denied either way" trap already documented for
		/// #221.</summary>
		[SteamFunction]
		public static void TestSetupOwnedItem(Character self) {
			ItemDef plainDef = (ItemDef) ThingDef.GetByDefname("i_rock_plain");
			Item item = (Item) plainDef.Create(self.P());
			item.OwnedBy = self;
			item.SetAttribute(ItemAttributes.attr_owned, true);
			Console.WriteLine("ATTR_OWNED_SETUP: uid=" + item.Uid + " x=" + item.X + " y=" + item.Y + " z=" + item.Z);
		}

		/// <summary>#223 - a plain ground item flagged attr_nodroptrade, for both the "can't
		/// drop on the ground" and "can't be added to a secure trade" halves.</summary>
		[SteamFunction]
		public static void TestSetupNoDropTradeItem(Character self) {
			ItemDef plainDef = (ItemDef) ThingDef.GetByDefname("i_rock_plain");
			Item item = (Item) plainDef.Create(self.P());
			item.SetAttribute(ItemAttributes.attr_nodroptrade, true);
			Console.WriteLine("ATTR_NODROPTRADE_SETUP: uid=" + item.Uid + " x=" + item.X + " y=" + item.Y + " z=" + item.Z);
		}

		/// <summary>#224 - a plain ground item flagged attr_invis, reporting its position too
		/// so a protocol test can teleport a second character there before checking
		/// visibility.</summary>
		[SteamFunction]
		public static void TestSetupInvisItem(Character self) {
			ItemDef plainDef = (ItemDef) ThingDef.GetByDefname("i_rock_plain");
			Item item = (Item) plainDef.Create(self.P());
			item.SetAttribute(ItemAttributes.attr_invis, true);
			Console.WriteLine("ATTR_INVIS_SETUP: uid=" + item.Uid + " x=" + item.X + " y=" + item.Y + " z=" + item.Z);
		}

		/// <summary>Reports whether self can currently see the given item - the real,
		/// per-viewer check (Character.CanSeeVisibility(Item)), not just the raw flag.</summary>
		[SteamFunction]
		public static void TestCheckCanSeeItem(Character self, ScriptArgs sa) {
			uint uid = Convert.ToUInt32(sa.Argv[0], System.Globalization.CultureInfo.InvariantCulture);
			Item item = (Item) Thing.UidGetItem(uid);
			Console.WriteLine("ATTR_INVIS_CHECK: canSee=" + self.CanSeeVisibility(item));
		}

		/// <summary>#219 (redesigned) - sets deliberately different, tiny npc-vs-player corpse
		/// decay rates so a protocol test can confirm Corpse.Owner (set only for player deaths)
		/// really does pick a different rate, not just that corpses decay at all. A single
		/// combined string argument ("npcMinutes,playerMinutes") sidesteps ScriptArgs' usual
		/// dual-shape ambiguity the same way other multi-value Test* commands in this session
		/// already do.</summary>
		[SteamFunction]
		public static void TestSetupCorpseDecayRates(Character self, ScriptArgs sa) {
			string[] parts = sa.Argv[0].ToString().Split(',');
			DecaySettings.instance.npcCorpseDecayMinutes = Convert.ToDouble(parts[0], CultureInfo.InvariantCulture);
			DecaySettings.instance.playerCorpseDecayMinutes = Convert.ToDouble(parts[1], CultureInfo.InvariantCulture);
			Console.WriteLine("ATTR_CORPSE_RATES_SET");
		}

		/// <summary>Kills a freshly-spawned, nearby NPC and reports its corpse's uid. Deferred
		/// to a near-immediate timer rather than calling CauseDeath synchronously - same #146
		/// lazy-DeathSound-expression/command-authorization trap LevelingTests.
		/// DeferredNpcKillTimer already works around (a monster's DeathSound ".def" expression
		/// gets treated as a command-authorization check if resolved synchronously inside a
		/// chat command's own call stack) - that class's own OnTimeout is hardcoded to report
		/// leveling-specific results, not reusable here, so this is its own small equivalent.</summary>
		[SteamFunction]
		public static void TestKillNpcAndReportCorpse(Character self) {
			NPCDef rabbitDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC rabbit = (NPC) rabbitDef.Create(self.X + 1, self.Y, self.Z, self.M);
			self.AddTimer(DeferredCorpseKillTimer.TimerKey, new DeferredCorpseKillTimer(rabbit, self)).DueInSeconds = 0;
		}

		/// <summary>Kills the player identified by sa.Argv[0]'s uid and reports its corpse's
		/// uid. Unlike the NPC case above, a player's own CauseDeath resolves synchronously
		/// fine from within a chat command (confirmed already, #218's own TestDeathDropAttrItems) -
		/// no deferral needed.</summary>
		[SteamFunction]
		public static void TestKillPlayerAndReportCorpse(Character self, ScriptArgs sa) {
			uint victimUid = Convert.ToUInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			Player victim = (Player) Thing.UidGetCharacter(victimUid);
			if (victim.Flag_Dead) {
				victim.Resurrect();
			}
			victim.CauseDeath((Character) self);
			Corpse corpse = FindNearbyCorpse(victim);
			Console.WriteLine("ATTR_CORPSE_SETUP: uid=" + (corpse != null ? corpse.Uid.ToString(CultureInfo.InvariantCulture) : "none"));
		}

		private static Corpse FindNearbyCorpse(Character self) {
			foreach (AbstractItem item in self.GetMap().GetItemsInRange(self.X, self.Y, 10)) {
				Corpse corpse = item as Corpse;
				if (corpse != null) {
					return corpse;
				}
			}
			return null;
		}

		/// <summary>Reports whether the item/corpse at the given uid still exists.</summary>
		[SteamFunction]
		public static void TestReportThingExists(Character self, ScriptArgs sa) {
			uint uid = Convert.ToUInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			bool exists = Thing.UidGetThing(uid) != null;
			Console.WriteLine("ATTR_THING_EXISTS: uid=" + uid + " exists=" + exists);
		}

		[SaveableClass]
		public class DeferredCorpseKillTimer : BoundTimer {
			internal static readonly TimerKey TimerKey = TimerKey.Acquire("_deferredCorpseKillTimer_");

			private NPC victim;
			private Character killer;

			[LoadingInitializer]
			public DeferredCorpseKillTimer() {
			}

			public DeferredCorpseKillTimer(NPC victim, Character killer) {
				this.victim = victim;
				this.killer = killer;
			}

			protected override void OnTimeout(TagHolder cont) {
				this.victim.CauseDeath(this.killer);
				Corpse corpse = FindNearbyCorpse(this.killer);
				Console.WriteLine("ATTR_CORPSE_SETUP: uid=" + (corpse != null ? corpse.Uid.ToString(CultureInfo.InvariantCulture) : "none"));
			}
		}
	}
}
