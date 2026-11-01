using SteamEngine.CompiledScripts;
using Xunit;

namespace SteamEngine.Tests {
	/// <summary>#962 - Sphere's skill check and Skill_Experience, the numbers worked out by hand from EreborNext.</summary>
	public class SkillGainFormulaTests {
		private static readonly double[] Smith = { 10.0, 400.0, 1600.0 };
		private static readonly double[] Mining = { 1010.0, 400.0, 1600.0 };

		[Theory]
		[InlineData(0, 500)]
		[InlineData(100, 750)]
		[InlineData(-100, 250)]
		[InlineData(200, 875)]
		public void TheSkillCheckIsSpheres(int skillOverDifficulty, int chance) {
			Assert.Equal(chance, SkillUtils.GetSCurve(skillOverDifficulty));
		}

		[Theory]
		[InlineData(0, 1000)] //10.0 successes to gain: every time
		[InlineData(250, 48)] //halfway to 400.0: 205.0 successes
		[InlineData(500, 25)]
		[InlineData(1000, 6)]
		public void TheCurveIsSpheres(int value, int chance) {
			Assert.Equal(chance, SkillUtils.GetAdvanceChance(Smith, value));
		}

		[Fact]
		public void NoGainWithoutACurve() {
			Assert.Equal(0, SkillUtils.GetAdvanceChance(new double[] { 0 }, 500));
		}

		[Fact]
		public void MiningStartsSlowest() {
			//a job of 10.0 at no skill reads the curve below its start
			Assert.Equal(8, SkillGainSystem.GainChance(Mining, 0, 100));
		}

		[Theory]
		[InlineData(-300, 50, 17)] //half of the skill, less 8
		[InlineData(-300, 299, 141)]
		[InlineData(-300, 300, 0)] //from 30.0 failing teaches nothing
		[InlineData(1500, 500, 1000)]
		[InlineData(300, 500, 300)]
		public void WhatAnAttemptCounts(int difficulty, int skill, int effort) {
			Assert.Equal(effort, SkillGainSystem.EffortOf(difficulty, skill));
		}

		[Theory]
		[InlineData(600, 200, 6)] //the curve is read at skill + (skill - effort): 1000 here
		[InlineData(600, 199, 3)] //over 40.0 under the skill: half as often
		[InlineData(600, 100, 2)]
		[InlineData(600, 99, 0)] //over 50.0 under: nothing
		public void EasyJobsTeachLess(int skill, int effort, int chance) {
			Assert.Equal(chance, SkillGainSystem.GainChance(Smith, skill, effort));
		}
	}
}
