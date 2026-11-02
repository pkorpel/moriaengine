using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1011 - what .whoami shows, from Erebor's [function whoami] of funkce.scp: monsterkillstotal, as f_countfight every
	/// player who fought a dying NPC gets one, Sphere's deaths and age, and the user's additions, time played and the last
	/// killer. Player kills are NazgulRings', which a Nazgul's ring takes some off again, as on Erebor. All of these but the
	/// player kills start from when they came in.</summary>
	public static class PlayerRecord {
		private const double SecondsInDay = 24 * 60 * 60, SecondsInMinute = 60;
		private const int MinutesInHour = 60;
		private static readonly TagKey monsterKillsTK = TagKey.Acquire("_monsterKillsTotal_");
		private static readonly TagKey deathsTK = TagKey.Acquire("_deathsTotal_");
		private static readonly TagKey bornTK = TagKey.Acquire("_bornAt_");
		private static readonly TagKey playedTK = TagKey.Acquire("_playedSeconds_");
		private static readonly TagKey sessionStartTK = TagKey.Acquire("_sessionStart_");
		private static readonly TagKey lastKillerTK = TagKey.Acquire("_lastKilledBy_");

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
			player.SetTag(sessionStartTK, Globals.TimeInSeconds);
		}

		internal static void MonsterDied(NPC victim, Character killedBy) {
			foreach (Character fighter in LevelingSystem.FindFighters(victim, killedBy, false)) {
				if (fighter is Player player) {
					Add(player, monsterKillsTK);
				}
			}
		}

		internal static void Died(Player player, Character killedBy) {
			Add(player, deathsTK);
			if (killedBy != null) {
				player.SetTag(lastKillerTK, killedBy.Name);
			}
		}

		internal static void LoggedOut(Player player) {
			if (player.GetTag(sessionStartTK) is double started) {
				player.SetTag(playedTK, Played(player, started));
				player.RemoveTag(sessionStartTK);
			}
		}

		private static double Played(Player player, double sessionStart) {
			return ((player.GetTag(playedTK) is double played) ? played : 0) + (Globals.TimeInSeconds - sessionStart);
		}

		internal static string PlayedText(Player player, PlayerRecordLoc texts) {
			double started = (player.GetTag(sessionStartTK) is double session) ? session : Globals.TimeInSeconds;
			int minutes = (int) (Played(player, started) / SecondsInMinute);
			return string.Format(texts.HoursAndMinutes, minutes / MinutesInHour, minutes % MinutesInHour);
		}

		internal static string Ratio(Player player) {
			int deaths = Count(player, deathsTK);
			double ratio = NazgulRings.KillsOf(player) / (double) Math.Max(deaths, 1);
			return ratio.ToString("0.00", CultureInfo.InvariantCulture);
		}

		internal static int Monsters(Player player) {
			return Count(player, monsterKillsTK);
		}

		internal static int Deaths(Player player) {
			return Count(player, deathsTK);
		}

		internal static int Days(Player player) {
			double born = (player.GetTag(bornTK) is double when) ? when : Globals.TimeInSeconds;
			return (int) ((Globals.TimeInSeconds - born) / SecondsInDay);
		}

		internal static string LastKiller(Player player) {
			return player.GetTag(lastKillerTK) as string;
		}

		/// <summary>The office they hold, or null.</summary>
		internal static string Office(Player player, PlayerRecordLoc texts) {
			if (NazgulRings.IsFirst(player)) {
				return texts.FirstNazgul;
			}
			Town ruled = Town.OfRuler(player);
			if (NazgulRings.IsNazgul(player)) {
				return (ruled != null) ? string.Format(texts.NazgulOf, ruled.Name) : texts.Nazgul;
			}
			if ((player.Realm == Realm.Gondor) && (RealmOffice.For(Realm.Gondor)?.holder == player)) {
				return texts.General;
			}
			return (ruled != null) ? string.Format(texts.MayorOf, ruled.Name) : null;
		}

		/// <summary>#1009's kills of the last days, for those a ring could still go to.</summary>
		internal static string RingProgress(Player player, PlayerRecordLoc texts) {
			RealmSettings settings = RealmSettings.instance;
			if ((player.Realm != Realm.Mordor) || NazgulRings.IsNazgul(player) || (settings.ringQuestKills <= 0) ||
					Professions.HasProfession(player, Professions.Craftsman)) {
				return null;
			}
			return string.Format(texts.RingKills, NazgulRings.RecentQuestKills(player).Count, settings.ringQuestKills, settings.ringQuestDays);
		}

		internal static void Show(Player player) {
			player.DialogForget(SingletonScript<D_WhoAmI>.Instance);
			player.Dialog(player, SingletonScript<D_WhoAmI>.Instance, new DialogArgs());
		}

		[SteamFunction]
		public static void WhoAmI(Player self) {
			Show(self);
		}
	}

	/// <summary>#1011 - the user's call: .whoami as a window, where Erebor had four lines of chat, and more in it.</summary>
	public class D_WhoAmI : CompiledGumpDef {
		private const int Width = 400, LineHeight = 22, ValueX = 150, Margin = 14, TextHue = 0, HeadHue = 1152;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Player self = (Player) sendTo;
			PlayerRecordLoc texts = Loc<PlayerRecordLoc>.Get(self.Language);
			string profession = self.Profession?.Name;
			string name = string.IsNullOrEmpty(profession) ? self.Name : char.ToUpperInvariant(profession[0]) + profession.Substring(1) + " " + self.Name;
			if (self.Nation != Nation.None) {
				name += " of " + NationData.GetDisplayName(self.Nation); //the user's call, where Erebor had tag.nation, Rohir
			}
			List<(string label, string value)> rows = new List<(string, string)> {
				(texts.Level, self.Level.ToString(CultureInfo.InvariantCulture)),
				(texts.Rank, self.SkillRank.ToString()),
				(texts.Realm, self.Realm.ToString()),
				(texts.HomeTown, Town.OfCitizen(self)?.Name),
				(texts.Guild, Guild.GetGuild(self)?.Name),
				(texts.Office, PlayerRecord.Office(self, texts)),
				(texts.Enemies, NazgulRings.KillsOf(self).ToString(CultureInfo.InvariantCulture)),
				(texts.Monsters, PlayerRecord.Monsters(self).ToString(CultureInfo.InvariantCulture)),
				(texts.Defeated, string.Format(texts.Times, PlayerRecord.Deaths(self))),
				(texts.Ratio, PlayerRecord.Ratio(self)),
				(texts.LastKiller, PlayerRecord.LastKiller(self)),
				(texts.Played, PlayerRecord.PlayedText(self, texts)),
				(texts.Alive, string.Format(texts.Days, PlayerRecord.Days(self))),
				(texts.Ring, PlayerRecord.RingProgress(self, texts))
			};
			rows.RemoveAll(row => string.IsNullOrEmpty(row.value));

			this.SetLocation(60, 60);
			this.AddBackground(0, 0, Width, 70 + (rows.Count * LineHeight), 5054);
			this.AddLabel(Margin, 12, HeadHue, name);
			if (!string.IsNullOrEmpty(self.Title)) {
				this.AddLabel(Margin, 32, TextHue, self.Title);
			}
			for (int i = 0; i < rows.Count; i++) {
				int y = 58 + (i * LineHeight);
				this.AddLabel(Margin, y, TextHue, rows[i].label);
				this.AddLabel(ValueX, y, TextHue, rows[i].value);
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
		}
	}

	internal class PlayerRecordLoc : CompiledLocStringCollection<PlayerRecordLoc> {
		public string Level = "Uroven";
		public string Rank = "Hodnost";
		public string Realm = "Rise";
		public string HomeTown = "Domovske mesto";
		public string Guild = "Guilda";
		public string Office = "Funkce";
		public string Enemies = "Premozeni nepratele";
		public string Monsters = "Premozena monstra";
		public string Defeated = "Porazen";
		public string Ratio = "Pomer zabiti/smrti";
		public string LastKiller = "Naposledy zabit od";
		public string Played = "Odehrany cas";
		public string Alive = "Zije";
		public string Ring = "Prsten moci";
		public string Times = "{0}krat";
		public string Days = "{0} dni";
		public string HoursAndMinutes = "{0} hod {1} min";
		public string RingKills = "{0} z {1} zabiti za poslednich {2} dni";
		public string FirstNazgul = "Prvni Nazgul";
		public string Nazgul = "Nazgul";
		public string NazgulOf = "Nazgul mesta {0}";
		public string General = "General";
		public string MayorOf = "Starosta mesta {0}";
	}
}
