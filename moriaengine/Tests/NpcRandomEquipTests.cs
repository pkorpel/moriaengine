using System;
using Xunit;
using SteamEngine.CompiledScripts;
using SteamEngine.Tests.Fixtures;

namespace SteamEngine.Tests {
	/// <summary>
	/// #775 - Sphere weighted random picks in starting equipment, e.g. converted
	/// "Equip = {i_bardiche_iron 1 i_axe_battle_large_Iron 1  i_halberd_Iron 1}". The whole "{...}"
	/// used to be looked up as one defname, so these NPCs spawned without that item.
	/// </summary>
	[Collection(EngineCollection.Name)]
	public class NpcRandomEquipTests {
		[Fact]
		public void ParsesWeightedList() {
			string[] names;
			int[] weights;
			Assert.True(NPCDef.TryParseRandomDefnames("{i_bardiche_iron 1 i_axe_battle_large_Iron 1  i_halberd_Iron 3}", out names, out weights));
			Assert.Equal(new[] { "i_bardiche_iron", "i_axe_battle_large_Iron", "i_halberd_Iron" }, names);
			Assert.Equal(new[] { 1, 1, 3 }, weights);
		}

		[Fact]
		public void ParsesCommaSeparatedAndUnweightedLists() {
			string[] names;
			int[] weights;
			Assert.True(NPCDef.TryParseRandomDefnames("{ i_a, 2, i_b, 5 }", out names, out weights));
			Assert.Equal(new[] { "i_a", "i_b" }, names);
			Assert.Equal(new[] { 2, 5 }, weights);

			Assert.True(NPCDef.TryParseRandomDefnames("{i_a i_b i_c}", out names, out weights));
			Assert.Equal(new[] { "i_a", "i_b", "i_c" }, names);
			Assert.Equal(new[] { 1, 1, 1 }, weights);
		}

		[Theory]
		[InlineData("i_bardiche")]
		[InlineData("{}")]
		[InlineData("{1 i_a}")]
		[InlineData("{i_a 0}")]
		[InlineData("{i_a 1")]
		public void RejectsMalformedLists(string text) {
			string[] names;
			int[] weights;
			Assert.False(NPCDef.TryParseRandomDefnames(text, out names, out weights));
		}

		[Theory]
		[InlineData("i_bow", "i_bow", null)]
		[InlineData("i_arrow, 20", "i_arrow", "20")]
		[InlineData("i_arrow, 10 20", "i_arrow", "10 20")]
		[InlineData("{i_a 1 i_b 1}", "{i_a 1 i_b 1}", null)]
		[InlineData("{i_a, 1, i_b, 1}, 5", "{i_a, 1, i_b, 1}", "5")] //commas inside the braces aren't the amount separator
		public void SplitsItemFromAmount(string args, string item, string amount) {
			string itemPart, amountPart;
			NPCDef.SplitEquipLine(args, out itemPart, out amountPart);
			Assert.Equal(item, itemPart);
			Assert.Equal(amount, amountPart);
		}

		[Fact]
		public void WeightedPickFollowsTheWeights() {
			string[] names = { "common", "rare" };
			int[] weights = { 9, 1 };
			Random random = new Random(775);
			int rare = 0;
			for (int i = 0; i < 10000; i++) {
				if (NPCDef.PickWeighted(names, weights, random) == "rare") {
					rare++;
				}
			}
			Assert.InRange(rare, 850, 1150); //10% of 10000
		}

		[Fact]
		public void EntryWithoutAListAlwaysPicksItsDefname() {
			NPCDef.StartingEquipmentEntry entry = new NPCDef.StartingEquipmentEntry { Defname = "i_bow" };
			Assert.Equal("i_bow", entry.PickDefname(new Random(1)));
		}

		[Fact]
		public void EntryWithAListPicksOneOfItsNames() {
			NPCDef.StartingEquipmentEntry entry = new NPCDef.StartingEquipmentEntry {
				Defname = "{i_a 1 i_b 1}", RandomDefnames = new[] { "i_a", "i_b" }, RandomWeights = new[] { 1, 1 }
			};
			Random random = new Random(2);
			bool sawA = false, sawB = false;
			for (int i = 0; i < 200; i++) {
				string picked = entry.PickDefname(random);
				Assert.Contains(picked, new[] { "i_a", "i_b" });
				sawA |= picked == "i_a";
				sawB |= picked == "i_b";
			}
			Assert.True(sawA && sawB);
		}
	}
}
