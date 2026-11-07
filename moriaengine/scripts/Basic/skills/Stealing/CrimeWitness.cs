using System;
using System.Runtime.CompilerServices;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - Sphere's reactions to a seen crime: OnNoticeCrime, NPC_OnNoticeSnoop, and its karma. A player who saw one
	/// remembers it for a guard call, and an NPC raises the alarm itself.</summary>
	public static class CrimeWitness {
		private const int FirstCrimeShout = 1013549, CrimeShouts = 16, FleeSteps = 20, FleeOneIn = 4, GuardReach = 32;
		private const int KarmaFloorOfScale = 64;

		private static readonly int[] karmaLevels = { 9900, 5000, 1000, 500, 100, -100, -500, -1000, -5000, -9900 };
		private const int LastGoodLevel = 4, NeutralLevel = 5;

		private sealed class Memory {
			internal Character criminal;
			internal double when;
		}

		private static readonly ConditionalWeakTable<Character, Memory> memories = new ConditionalWeakTable<Character, Memory>();

		public static void Notice(Character witness, Character criminal, Character mark) {
			if (witness == criminal) {
				return;
			}
			NPC npc = witness as NPC;
			if (npc == null) {
				Remember(witness, criminal);
				return;
			}
			if ((witness != mark) && (npc.ControlMaster == criminal)) {
				return;
			}
			if (witness == mark) {
				Remember(witness, criminal);
				NPCAiTimer.DefendAgainst(npc, criminal);
			}
			if (!npc.IsHuman) {
				return;
			}
			Reputation.OnCrime(criminal, mark, npc); //#1243 - the city the witness belongs to hears of it
			Notoriety.FlagCriminal(criminal);
			FlaggedRegion area = npc.Region as FlaggedRegion;
			if ((area == null) || !area.Flag_Guarded) {
				return;
			}
			if (!GuardSpeech.IsGuard(npc)) {
				npc.ClilocSay(FirstCrimeShout + Globals.dice.Next(CrimeShouts));
			}
			foreach (AbstractCharacter other in npc.GetMap().GetCharsInRange(npc.X, npc.Y, GuardReach)) {
				NPC guard = other as NPC;
				if ((guard != null) && (guard != npc) && GuardSpeech.IsGuard(guard) && GuardSpeech.SendAfter(guard, criminal)) {
					return;
				}
			}
		}

		/// <summary>What an NPC does on seeing someone in its own pack, and sometimes it runs.</summary>
		public static void Protest(NPC mark, Character thief) {
			if ((mark == null) || !mark.IsHuman) {
				return;
			}
			CrimeLoc texts = Loc<CrimeLoc>.Get(mark.Language);
			string[] lines = { texts.Busybody, texts.WhatAreYouDoing, texts.AwayWithYou, texts.Beware };
			mark.Say(lines[Globals.dice.Next(lines.Length)]);
			NPCAiTimer timer = NPCAiTimer.GetFor(mark);
			if ((timer != null) && (Globals.dice.Next(FleeOneIn) == 0)) {
				timer.FleeFrom(thief, FleeSteps);
			}
		}

		private static void Remember(Character witness, Character criminal) {
			memories.Remove(witness);
			memories.Add(witness, new Memory { criminal = criminal, when = Globals.TimeInSeconds });
		}

		/// <summary>CallGuards' look around: someone the caller saw at it, still in sight, is a criminal now.</summary>
		public static Character SeenBy(Character caller) {
			Memory memory;
			if (!memories.TryGetValue(caller, out memory) || memory.criminal.IsDeleted
					|| ((Globals.TimeInSeconds - memory.when) > NpcAggroSettings.instance.guardCallMemorySeconds)
					|| (Point2D.GetSimpleDistance(caller, memory.criminal) > Globals.MaxUpdateRange)) {
				return null;
			}
			Notoriety.FlagCriminal(memory.criminal);
			return memory.criminal;
		}

		/// <summary>Sphere's Noto_KarmaChangeMessage: the good lose double, and only a change of level is told.</summary>
		public static void ChangeKarma(Character self, int change, int limit) {
			int karma = self.Karma;
			if (karma > 0) {
				change = (change < 0) ? (change * 2) : (change / 2);
			}
			if ((change == 0) || ((change > 0) && (change < (karma / KarmaFloorOfScale))) || ((karma <= limit) && (change < 0))) {
				return;
			}
			int before = LevelOf(karma);
			int now = (change < 0) ? Math.Max(karma + change, Math.Min(karma, limit)) : Math.Min(karma + change, 10000);
			self.Karma = (short) now;
			if (LevelOf(now) != before) {
				string good = (LevelOf(now) <= LastGoodLevel) ? "The Good " : (LevelOf(now) == NeutralLevel) ? "" : "The Evil ";
				self.SysMessage("You are now " + good + self.Name);
			}
		}

		private static int LevelOf(int karma) {
			int level = 0;
			while ((level < karmaLevels.Length) && (karma < karmaLevels[level])) {
				level++;
			}
			return level;
		}
	}

	internal class CrimeLoc : CompiledLocStringCollection<CrimeLoc> {
		public string Busybody = "Get thee hence, busybody!";
		public string WhatAreYouDoing = "What do you think your doing ?";
		public string AwayWithYou = "Ack, away with you!";
		public string Beware = "Beware or I'll call the guards!";
	}
}
