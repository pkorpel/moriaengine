using System;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1296 - .settings "Skutky": what a deed's tier pays (the first tier's gold and experience, times `growth` for each
	/// tier after it).</summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class DeedSettings : SettingsMetaCategory {
		[SavedMember]
		public static DeedSettings instance = new DeedSettings();

		[LoadingInitializer]
		public DeedSettings() {
		}

		/// <summary>The first tier's gold and experience; each tier after it pays `growth` times the one before.</summary>
		[SaveableData]
		public int gold = 300, exp = 500, growth = 3;
	}

	/// <summary>#1296 - a deed (design 9.6): what it counts, its tiers and their titles.</summary>
	public sealed class Deed {
		public readonly string Key, Name, What;
		public readonly int[] Tiers;
		public readonly string[] Titles;

		/// <summary>Counted (a tag on the player) or worked out when read (friend of the cities).</summary>
		public readonly Func<Player, int> Computed;

		public Deed(string key, string name, string what, int[] tiers, string[] titles, Func<Player, int> computed = null) {
			this.Key = key;
			this.Name = name;
			this.What = what;
			this.Tiers = tiers;
			this.Titles = titles;
			this.Computed = computed;
		}
	}

	/// <summary>#1296 - deeds and titles: counts of what players do anyway; each tier reached pays experience and gold and names a
	/// title, shown in the journal's Skutky tab (a record, not a cosmetic). Counts are player tags.</summary>
	public static class Deeds {
		public const string Slayer = "lovec", Courier = "posel", Explorer = "cestovatel", Defender = "obrance", Festival = "slavnosti", Friend = "pritel";

		public static readonly Deed[] All = {
			new Deed(Slayer, "Lovec", "zabitych nestvur", new[] { 10, 100, 1000, 10000 }, new[] { "Lovec zverat", "Lovec", "Postrach divociny", "Legenda lovu" }),
			new Deed(Courier, "Posel", "splnenych ukolu", new[] { 5, 25, 100, 500 }, new[] { "Pomocnik", "Spolehlivy posel", "Mistr poslu", "Hlas mest" }),
			new Deed(Explorer, "Cestovatel", "navstivenych mest", new[] { 3, 8, 15, 30 }, new[] { "Poutnik", "Cestovatel", "Svetobeznik", "Znalec Stredozeme" }),
			new Deed(Defender, "Obrance", "vyhranych udalosti", new[] { 1, 5, 20, 50 }, new[] { "Obrance", "Strazce", "Hrdina mest", "Stit kraje" }),
			new Deed(Festival, "Host slavnosti", "navstivenych slavnosti", new[] { 1, 5, 15, 30 }, new[] { "Host", "Stamgast slavnosti", "Duse slavnosti", "Kral slavnosti" }),
			new Deed(Friend, "Pritel mest", "mest, kde jsi vazeny", new[] { 1, 2, 4, 6 }, new[] { "Vitany host", "Pritel mesta", "Pritel mest", "Milacek Stredozeme" }, CitiesThatRespect)
		};

		private static TagKey CountKey(string key) {
			return TagKey.Acquire("_deed_" + key);
		}

		private static TagKey TierKey(string key) {
			return TagKey.Acquire("_deed_tier_" + key);
		}

		private static readonly TagKey visitedTK = TagKey.Acquire("_deed_cities_");

		public static Deed Get(string key) {
			return Array.Find(All, d => d.Key == key);
		}

		/// <summary>How far the player is in the deed.</summary>
		public static int Count(Player player, Deed deed) {
			if (deed.Computed != null) {
				return deed.Computed(player);
			}
			object value = player.GetTag(CountKey(deed.Key));
			return (value == null) ? 0 : Convert.ToInt32(value, CultureInfo.InvariantCulture);
		}

		/// <summary>The tiers reached and paid (0 = none yet).</summary>
		public static int Tier(Player player, Deed deed) {
			object value = player.GetTag(TierKey(deed.Key));
			return (value == null) ? 0 : Convert.ToInt32(value, CultureInfo.InvariantCulture);
		}

		/// <summary>The title of the highest tier reached, or null.</summary>
		public static string Title(Player player, Deed deed) {
			int tier = Tier(player, deed);
			return (tier == 0) ? null : deed.Titles[tier - 1];
		}

		/// <summary>Adds to a counted deed and pays any tier it reaches. GMs don't do deeds.</summary>
		public static void Add(Player player, string key, int amount = 1) {
			Deed deed = Get(key);
			if ((player == null) || player.IsDeleted || player.IsGM || (deed == null) || (deed.Computed != null) || (amount <= 0)) {
				return;
			}
			player.SetTag(CountKey(key), Count(player, deed) + amount);
			Check(player, deed);
		}

		/// <summary>Pays the tiers the player's count has reached and not been paid for, one by one.</summary>
		public static void Check(Player player, Deed deed) {
			if ((player == null) || player.IsGM) {
				return;
			}
			int count = Count(player, deed);
			int tier = Tier(player, deed);
			DeedSettings s = DeedSettings.instance;
			while ((tier < deed.Tiers.Length) && (count >= deed.Tiers[tier])) {
				long factor = 1;
				for (int i = 0; i < tier; i++) {
					factor *= Math.Max(1, s.growth);
				}
				tier++;
				player.SetTag(TierKey(deed.Key), tier);
				int gold = (int) Math.Min(int.MaxValue, s.gold * factor), exp = (int) Math.Min(int.MaxValue, s.exp * factor);
				if (gold > 0) {
					GoldUtils.Grant(player, (uint) gold);
				}
				if (exp > 0) {
					LevelingSystem.AddExperience(player, exp);
				}
				player.SysMessage("Skutek: " + deed.Name + " (" + deed.Tiers[tier - 1] + " " + deed.What + "). Novy titul: " + deed.Titles[tier - 1] + ". Odmena: "
					+ gold + " zlatych, " + exp + " zkusenosti.");
			}
		}

		/// <summary>The deeds worked out when read (friend of the cities): their tiers paid as they're reached.</summary>
		public static void CheckComputed(Player player) {
			foreach (Deed deed in All) {
				if (deed.Computed != null) {
					Check(player, deed);
				}
			}
		}

		#region the counts' sources
		/// <summary>From NPC.On_Death: a monster killed counts for every player who fought it (as experience counts fighters); one
		/// worth no experience, a resident, a pet or a summoned creature doesn't.</summary>
		public static void OnKill(NPC npc, Character killedBy) {
			if ((npc.ExperienceReward <= 0) || (npc.ControlMaster != null) || (Residents.GemOf(npc) != null) || SummonSpellDef.IsSummonedCreature(npc)) {
				return;
			}
			foreach (Character fighter in LevelingSystem.FindFighters(npc, killedBy, false)) {
				if (fighter is Player player) {
					Add(player, Slayer);
				}
			}
		}

		/// <summary>From Reputation.OnEnterCity: a city never entered before counts.</summary>
		public static void OnEnterCity(Player player, string city) {
			if ((player == null) || player.IsGM || string.IsNullOrEmpty(city)) {
				return;
			}
			string word = "|" + Reputation.Word(city) + "|";
			string visited = player.GetTag(visitedTK) as string ?? "|";
			if (visited.Contains(word)) {
				return;
			}
			player.SetTag(visitedTK, visited + Reputation.Word(city) + "|");
			Add(player, Explorer);
		}

		/// <summary>Friend of the cities: the cities where the player's standing is at least Vazeny.</summary>
		private static int CitiesThatRespect(Player player) {
			int count = 0;
			foreach (string city in Reputation.CitiesOf(player)) {
				if (Reputation.Level(player, city) >= ReputationLevel.Vazeny) {
					count++;
				}
			}
			return count;
		}
		#endregion the counts' sources

		public static void Bootstrap() {
			QuestJournalPages.Register(D_QuestJournal.SectionDeeds, DeedsPage.Add);
		}

		/// <summary>Tests: every count, tier and city seen forgotten.</summary>
		public static void ForgetForTests(Player player) {
			foreach (Deed deed in All) {
				player.RemoveTag(CountKey(deed.Key));
				player.RemoveTag(TierKey(deed.Key));
			}
			player.RemoveTag(visitedTK);
		}
	}

	/// <summary>#1296 - the journal's Skutky tab: each deed, how far toward its next tier, and the title reached.</summary>
	public static class DeedsPage {
		private const int HintHue = 0x21, LabelHue = 955, CountHue = 52, TitleHue = 68, NoTitleHue = 905, Row = 17;

		internal static void Add(CompiledGumpDef gump, Player player, int width, int height) {
			Deeds.CheckComputed(player);
			int y = 135;
			bool any = false;
			foreach (Deed deed in Deeds.All) {
				any |= Deeds.Count(player, deed) > 0;
			}
			if (!any) {
				gump.AddLabel(85, y, HintHue, "Zatim nemas zadne skutky.");
			}
			y += Row + 6;
			foreach (Deed deed in Deeds.All) {
				int count = Deeds.Count(player, deed), tier = Deeds.Tier(player, deed);
				string title = Deeds.Title(player, deed);
				gump.AddLabel(85, y, LabelHue, deed.Name);
				gump.AddLabel(250, y, CountHue, (tier < deed.Tiers.Length) ? (count.ToString(CultureInfo.InvariantCulture) + "/" + deed.Tiers[tier].ToString(CultureInfo.InvariantCulture))
					: count.ToString(CultureInfo.InvariantCulture));
				gump.AddLabel(95, y + Row, (title == null) ? NoTitleHue : TitleHue, title ?? "bez titulu");
				y += (2 * Row) + 6;
			}
		}
	}
}
