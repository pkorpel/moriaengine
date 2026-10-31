using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace SteamEngine.Tests {
	/// <summary>
	/// #860 - the converter applies the nastaveni_* values of Erebor's save/sphereworld.scp over
	/// the item definitions. The archive isn't in the repo, so the expected numbers here are
	/// copied from it.
	/// </summary>
	public class EreborCombatValuesTests {
		private static readonly string converted = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "scripts", "converted"));

		private static string Field(string folder, string defname, string field) {
			foreach (string file in Directory.GetFiles(Path.Combine(converted, folder), "*.def")) {
				Match block = Regex.Match(File.ReadAllText(file), @"^\[\w+\s+" + Regex.Escape(defname) + @"\][^\n]*\n(.*?)(?=^\[|\z)",
					RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnoreCase);
				if (block.Success) {
					Match line = Regex.Match(block.Groups[1].Value, @"^" + field + @"\s*=\s*([^\s/]+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
					return line.Success ? line.Groups[1].Value : null;
				}
			}
			return null;
		}

		[Theory]
		[InlineData("i_sword_long_iron", "33", "76", "250")]
		[InlineData("i_sword_long_mithril", "50", "76", "250")]
		[InlineData("i_mace_war_copper", "32", "74", "250")]
		[InlineData("i_bow_copper", "36", "58", "0")]
		[InlineData("i_bow_mithril", "59", "58", "0")]
		public void WeaponsHaveEreborsValues(string defname, string attack, string speed, string piercing) {
			Assert.Equal(attack, Field("zbrane", defname, "attackVsP"));
			Assert.Equal(attack, Field("zbrane", defname, "attackVsM"));
			Assert.Equal(speed, Field("zbrane", defname, "speed"));
			Assert.Equal(piercing, Field("zbrane", defname, "piercing"));
		}

		[Theory]
		[InlineData("i_iron_platemail", "65", "15")]
		[InlineData("i_iron_chainmail_tunic", "35", null)]
		[InlineData("i_bone_chest_mithril", "45", null)]
		public void ArmorHasEreborsValues(string defname, string armor, string mindDefense) {
			Assert.Equal(armor, Field("brneni", defname, "armorVsP"));
			Assert.Equal(armor, Field("brneni", defname, "armorVsM"));
			if (mindDefense != null) {
				Assert.Equal(mindDefense, Field("brneni", defname, "mindDefenseVsP"));
			}
		}

		[Theory]
		[InlineData("i_iron_metal_shield", "5")] // 35/6
		[InlineData("i_iron_heater_shield", "2")] // 65/6 - 8
		[InlineData("i_mithril_heater_shield", "8")] // 97/6 - 8
		public void ShieldsGetASixthAndPlateShieldsLoseEight(string defname, string armor) {
			Assert.Equal(armor, Field("brneni", defname, "armorVsP"));
		}

		[Theory]
		[InlineData("i_arrow_copper", "1.05", "0")]
		[InlineData("i_bolt_jagged_copper", "0.8", "285")]
		[InlineData("i_xbolt_piercing_mithril", "1.05", "360")]
		public void AmmoHasEreborsValues(string defname, string multiplier, string piercing) {
			Assert.Equal(multiplier, Field("zbrane", defname, "DamageMultiplier"));
			Assert.Equal(piercing, Field("zbrane", defname, "Piercing"));
		}

		[Theory]
		[InlineData("i_iron_platemail", "396")]         //1100 * layerar[13] 36%
		[InlineData("i_iron_platemail_gorget", "66")]   //6%
		[InlineData("i_iron_heater_shield", "55")]      //1100/6 * layerar[2] 30%
		public void ArmorHasEreborsWeight(string defname, string weight) {
			Assert.Equal(weight, Field("brneni", defname, "WEIGHT"));
		}

		[Theory]
		[InlineData("i_kryss_iron", "0.45")]
		[InlineData("i_dagger_iron", "0.1")]
		[InlineData("i_sword_long_iron", "0")]
		public void WeaponsHaveEreborsBleed(string defname, string efficiency) {
			Assert.Equal(efficiency, Field("zbrane", defname, "BleedingEfficiency"));
		}

		[Theory]
		[InlineData("zbrane", "i_sword_long_iron", "260")]        //200 * 1300 / 1000
		[InlineData("brneni", "i_iron_platemail", "50")]          //200 * 700 * 36 / 100000
		[InlineData("brneni", "i_mithril_platemail_gorget", "58")] //1400 * 700 * 6 / 100000
		public void ItemsHaveEreborsDurability(string folder, string defname, string durability) {
			Assert.Equal(durability, Field(folder, defname, "maxDurability"));
		}

		[Fact]
		public void StavesHaveEreborsMindPower() {
			Assert.Equal("805", Field("zbrane", "i_staff_black_iron", "MindPowerVsP"));
		}
	}
}
