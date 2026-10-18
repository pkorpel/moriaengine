using System;
using System.IO;
using System.Text.RegularExpressions;
using SteamEngine.CompiledScripts;
using Xunit;

namespace SteamEngine.Tests {
	/// <summary>
	/// Pure logic test for WeaponSkillTargetQueuePlugin.GetSkillNameByWeaponType (#157) - no
	/// engine boot needed. Before this fix, OneHandSword/OneHandAxe both fell through to
	/// Fencing - only the TwoHand variants of Sword/Axe trained Swords. Real converted weapon
	/// content's own `type=` field shows the same "sword" category covering both sword- and
	/// axe-named weapons regardless of hand count, so both families now train Swords
	/// consistently; Spike trains Fencing, Blunt trains Macing, unaffected by this change.
	/// </summary>
	public class WeaponSkillMappingTests {
		[Theory]
		[InlineData(WeaponType.OneHandSword, SkillName.Swords)]
		[InlineData(WeaponType.TwoHandSword, SkillName.Swords)]
		[InlineData(WeaponType.OneHandAxe, SkillName.Swords)]
		[InlineData(WeaponType.TwoHandAxe, SkillName.Swords)]
		[InlineData(WeaponType.OneHandSpike, SkillName.Fencing)]
		[InlineData(WeaponType.TwoHandSpike, SkillName.Fencing)]
		[InlineData(WeaponType.OneHandBlunt, SkillName.Macing)]
		[InlineData(WeaponType.TwoHandBlunt, SkillName.Macing)]
		[InlineData(WeaponType.Bow, SkillName.Archery)]
		[InlineData(WeaponType.XBow, SkillName.Marksmanship)]
		[InlineData(WeaponType.BareHands, SkillName.Wrestling)]
		public void WeaponTypeTrainsExpectedSkill(WeaponType weaponType, SkillName expected) {
			Assert.Equal(expected, WeaponSkillTargetQueuePlugin.GetSkillNameByWeaponType(weaponType));
		}

		/// <summary>Data-correctness check (#157): a representative sample of real converted
		/// weapons, one per family, across a couple of material tiers, has the `WeaponType`
		/// its own `type=` field implies (fence→Spike, sword→Sword/Axe by name, mace→Blunt,
		/// bow_run→Bow, xbow→XBow) - not the old blanket-guessed value. Checked directly
		/// against the real `.def` file text (fast tier, no engine boot/script-loading
		/// needed) rather than via `ThingDef.GetByDefname`, which only resolves once content
		/// is actually loaded by a running server.</summary>
		[Theory]
		[InlineData("i_dagger_copper", "WeaponType.OneHandSpike")] // type=t_weapon_fence
		[InlineData("i_mace_copper", "WeaponType.OneHandBlunt")] // type=t_weapon_mace_smith
		[InlineData("i_sword_viking_copper", "WeaponType.OneHandSpike")] // type=t_weapon_fence (not "sword" despite the name - real converted data, not guessed)
		[InlineData("i_axe_copper", "WeaponType.TwoHandAxe")] // type=t_weapon_sword, TwoHanded=true
		[InlineData("i_axe_two_hand_copper", "WeaponType.TwoHandAxe")] // type=t_weapon_sword, TwoHanded=true
		[InlineData("i_bow_copper", "WeaponType.Bow")] // type=t_weapon_bow_run
		[InlineData("i_crossbow_copper", "WeaponType.XBow")] // type=t_weapon_xbow
		[InlineData("i_hatchet", "WeaponType.OneHandAxe")] // #370-adjacent content, type=t_weapon_sword, TwoHanded=false
		public void ConvertedWeaponWeaponTypeMatchesItsOwnTypeField(string defname, string expected) {
			string repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
			string zbrane = Path.Combine(repoRoot, "scripts", "converted", "zbrane");
			string block = null;
			foreach (string file in Directory.GetFiles(zbrane, "*.def")) {
				string content = File.ReadAllText(file);
				// Each block runs from one "[...Def ...]" header to just before the next one.
				// The defname is either the header's own name (i_dagger_copper) or a separate
				// "DEFNAME = i_hatchet"-style line inside the block (others.def's hex-id headers).
				foreach (Match blockMatch in Regex.Matches(content, @"\[(?:ColoredWeaponDef|WeaponDef|ItemDef)\s+(\S+)\](.*?)(?=\r?\n\[|\z)", RegexOptions.Singleline)) {
					string header = blockMatch.Groups[1].Value;
					string body = blockMatch.Groups[2].Value;
					bool isMatch = header.Equals(defname, StringComparison.OrdinalIgnoreCase) ||
						Regex.IsMatch(body, @"DEFNAME\s*=\s*" + Regex.Escape(defname) + @"\b", RegexOptions.IgnoreCase);
					if (isMatch) {
						block = body;
						break;
					}
				}
				if (block != null) {
					break;
				}
			}
			Assert.True(block != null, "Could not find a [...Def " + defname + "] block under " + zbrane);
			Match wt = Regex.Match(block, @"WeaponType\s*=\s*(WeaponType\.\w+)");
			Assert.True(wt.Success, "No WeaponType= line found for " + defname);
			Assert.Equal(expected, wt.Groups[1].Value);
		}
	}
}
