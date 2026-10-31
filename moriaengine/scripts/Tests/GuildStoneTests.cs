using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>#884 - server-side helpers for Tests/Protocol/GuildStoneTests.cs.</summary>
	public static class GuildStoneTests {
		private static GuildStone stone;

		/// <summary>A deed in my pack, me a plain player; reports what a client needs.</summary>
		[SteamFunction]
		public static void TestGuildStoneDeed(Character self) {
			self.Account.PLevel = 1;
			Item deed = (Item) ThingDef.GetByDefname("i_deed_GUILDSTONE").Create(self.Backpack);
			Console.WriteLine("GUILD_DEED: uid=" + (uint) deed.FlaggedUid + " pack=" + (uint) self.Backpack.FlaggedUid +
				" me=" + (uint) self.FlaggedUid + " x=" + (self.X + 1) + " y=" + self.Y + " z=" + self.Z);
		}

		/// <summary>The stone the deed made, or a stone gone: the state of the guild.</summary>
		[SteamFunction]
		public static void TestGuildStoneFind(Character self) {
			stone = null;
			foreach (Thing thing in self.GetMap().GetThingsInRectangle(new Regions.ImmutableRectangle(self.X - 3, self.Y - 3, self.X + 3, self.Y + 3))) {
				if (thing is GuildStone found) {
					stone = found;
				}
			}
			Console.WriteLine("GUILD_STONE: uid=" + ((stone != null) ? (uint) stone.FlaggedUid : 0) + " end");
		}

		[SteamFunction]
		public static void TestGuildStoneGoTo(Character self) {
			self.P(stone.X - 1, stone.Y, stone.Z);
			self.Account.PLevel = 1;
			Console.WriteLine("GUILD_ME: uid=" + (uint) self.FlaggedUid);
		}

		/// <summary>A guild for GuildStoneRestartTests: renamed, with a master who shows the
		/// abbreviation and a candidate.</summary>
		[SteamFunction]
		public static void TestGuildStoneForSave(Character self) {
			stone = (GuildStone) ThingDef.GetByDefname("i_guildstone").Create(self.X + 1, self.Y, self.Z, self.M);
			Guild guild = GuildDef.NewGuild(stone);
			guild.AddMember(self);
			guild.Master = self;
			guild.AddCandidate((Character) ThingDef.GetByDefname("c_a_jackrabbit").Create(self.P()));
			guild.Rename("Bratrstvo");
			guild.Abbreviation = "BR";
			guild.ToggleAbbreviation(self);
			Console.WriteLine("GUILD_SAVED: ready");
		}

		[SteamFunction]
		public static void TestGuildStoneAfterRestart(Character self) {
			Guild guild = Guild.FindByName("Bratrstvo");
			Console.WriteLine("GUILD_LOADED: found=" + (guild != null) + " stoneName=" + guild?.Stone?.Name + " sameGuild=" + (Guild.ForStone(guild?.Stone) == guild) +
				" abbrev=" + guild?.Abbreviation + " master=" + guild?.Master?.Name.Replace(' ', '_') + " members=" + guild?.FullMembers.Count +
				" candidates=" + guild?.Candidates.Count + " shown=" + ((guild?.Master != null) ? Guild.NameWithAbbreviation(guild.Master).Replace(' ', '_') : "") + " end");
		}

		[SteamFunction]
		public static void TestGuildStoneReport(Character self) {
			Guild guild = Guild.ForStone(stone);
			Console.WriteLine("GUILD_REPORT: name=" + guild.Name.Replace(' ', '_') + " stoneName=" + stone.Name.Replace(' ', '_') + " abbrev=" + guild.Abbreviation +
				" master=" + ((guild.Master != null) ? guild.Master.Name.Replace(' ', '_') : "none") + " members=" + guild.FullMembers.Count +
				" candidates=" + guild.Candidates.Count + " rank=" + guild.GetRank(self) + " shown=" + Guild.NameWithAbbreviation(self).Replace(' ', '_') + " end");
		}
	}
}
