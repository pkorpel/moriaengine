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
	}
}
