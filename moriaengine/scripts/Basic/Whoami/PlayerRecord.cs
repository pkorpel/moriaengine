using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1011 - Erebor's [function whoami] of funkce.scp and what it reads: monsterkillstotal, as f_countfight every
	/// player who fought a dying NPC gets one, Sphere's deaths and age. Player kills are NazgulRings', which a Nazgul's ring
	/// takes some off again, as on Erebor. Those three start from when this came in.</summary>
	public static class PlayerRecord {
		private const double SecondsInDay = 24 * 60 * 60;
		private static readonly TagKey monsterKillsTK = TagKey.Acquire("_monsterKillsTotal_");
		private static readonly TagKey deathsTK = TagKey.Acquire("_deathsTotal_");
		private static readonly TagKey bornTK = TagKey.Acquire("_bornAt_");

		private static void Add(Player player, TagKey key) {
			player.SetTag(key, Count(player, key) + 1);
		}

		private static int Count(Player player, TagKey key) {
			return (player.GetTag(key) is int count) ? count : 0;
		}

		/// <summary>The first login is as near to a character's birth as there is.</summary>
		internal static void Login(Player player) {
			if (player.GetTag(bornTK) == null) {
				player.SetTag(bornTK, Globals.TimeInSeconds);
			}
		}

		internal static void MonsterDied(NPC victim, Character killedBy) {
			foreach (Character fighter in LevelingSystem.FindFighters(victim, killedBy, false)) {
				if (fighter is Player player) {
					Add(player, monsterKillsTK);
				}
			}
		}

		internal static void Died(Player player) {
			Add(player, deathsTK);
		}

		internal static void Show(Player player) {
			PlayerRecordLoc texts = Loc<PlayerRecordLoc>.Get(player.Language);
			List<string> who = new List<string>();
			string profession = player.Profession?.Name;
			string name = string.IsNullOrEmpty(profession) ? player.Name : char.ToUpperInvariant(profession[0]) + profession.Substring(1) + " " + player.Name;
			if (player.Nation != Nation.None) {
				name += " of " + NationData.GetDisplayName(player.Nation); //the user's call, where Erebor had tag.nation, Rohir
			}
			who.Add(name);
			if (!string.IsNullOrEmpty(player.Title)) {
				who.Add(player.Title);
			}
			double born = (player.GetTag(bornTK) is double when) ? when : Globals.TimeInSeconds;
			player.SysMessage(string.Format(texts.Identity, string.Join(", ", who)));
			player.SysMessage(string.Format(player.IsFemale ? texts.BeatenFemale : texts.BeatenMale, NazgulRings.KillsOf(player), Count(player, monsterKillsTK)));
			player.SysMessage(string.Format(player.IsFemale ? texts.DefeatedFemale : texts.DefeatedMale, Count(player, deathsTK)));
			player.SysMessage(string.Format(texts.Alive, (int) ((Globals.TimeInSeconds - born) / SecondsInDay)));
		}

		[SteamFunction]
		public static void WhoAmI(Player self) {
			Show(self);
		}
	}

	internal class PlayerRecordLoc : CompiledLocStringCollection<PlayerRecordLoc> {
		public string Identity = "Jsem {0}.";
		public string BeatenMale = "Premohl jsem {0} nepratel a {1} monster.";
		public string BeatenFemale = "Premohla jsem {0} nepratel a {1} monster.";
		public string DefeatedMale = "Ja sam jsem byl porazen {0}krat.";
		public string DefeatedFemale = "Ja sama jsem byla porazena {0}krat.";
		public string Alive = "Ziji jiz {0} dni.";
	}
}
