using System;

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

		/// <summary>#219 - shrinks both the configured decay interval and the sweep's own check
		/// cadence to a test-practical speed (same SpeedUpClockForTest precedent as
		/// LightAndWeather/#76), then creates 4 ground/contained items covering every exemption
		/// case: plain attr_decay (should eventually decay), attr_decay+attr_questitem (exempt),
		/// no attr_decay at all (never eligible), and attr_decay but sitting inside a backpack
		/// (exempt while contained). Reports their uids so a protocol test can poll each one.</summary>
		[SteamFunction]
		public static void TestSetupDecayItems(Character self) {
			DecaySettings.instance.decayIntervalMinutes = 0.01; //0.6 real seconds
			ItemDecaySystem.SpeedUpSweepForTest(1, 1); //check every 1 real second

			ItemDef plainDef = (ItemDef) ThingDef.GetByDefname("i_rock_plain");

			Item decayPlain = (Item) plainDef.Create(self.P());
			decayPlain.SetAttribute(ItemAttributes.attr_decay, true);

			Item decayQuestItem = (Item) plainDef.Create(self.P());
			decayQuestItem.SetAttribute(ItemAttributes.attr_decay, true);
			decayQuestItem.SetAttribute(ItemAttributes.attr_questitem, true);

			Item noDecayFlag = (Item) plainDef.Create(self.P());

			Item decayButContained = (Item) plainDef.Create(((Player) self).Backpack);
			decayButContained.SetAttribute(ItemAttributes.attr_decay, true);

			Console.WriteLine("ATTR_DECAY_SETUP: decayPlain=" + decayPlain.Uid +
				" decayQuestItem=" + decayQuestItem.Uid +
				" noDecayFlag=" + noDecayFlag.Uid +
				" decayButContained=" + decayButContained.Uid);
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

			Item magicItem = (Item) plainDef.Create(self.P());
			magicItem.SetAttribute(ItemAttributes.attr_magic, true);

			Item exceptionalItem = (Item) plainDef.Create(self.P());
			exceptionalItem.SetAttribute(ItemAttributes.attr_exceptional, true);

			Item enchantedItem = (Item) plainDef.Create(self.P());
			enchantedItem.SetAttribute(ItemAttributes.attr_enchanted, true);

			Item imbuedItem = (Item) plainDef.Create(self.P());
			imbuedItem.SetAttribute(ItemAttributes.attr_imbued, true);

			Item reforgedAndMagicItem = (Item) plainDef.Create(self.P());
			reforgedAndMagicItem.SetAttribute(ItemAttributes.attr_reforged, true);
			reforgedAndMagicItem.SetAttribute(ItemAttributes.attr_magic, true);

			Item plainItem = (Item) plainDef.Create(self.P());

			Console.WriteLine("ATTR_QUALITY_TEST:" +
				" magic=" + magicItem.Name +
				" exceptional=" + exceptionalItem.Name +
				" enchanted=" + enchantedItem.Name +
				" imbued=" + imbuedItem.Name +
				" reforgedAndMagic=" + reforgedAndMagicItem.Name +
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
	}
}
