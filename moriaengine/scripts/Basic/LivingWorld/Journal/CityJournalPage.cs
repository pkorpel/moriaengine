using System.Collections.Generic;
using System.Globalization;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1245 - the journal's Mesto tab: a page per city, the one the player stands in first. The city's view of the player
	/// and of its factions (as .povest), then the residents there who are the player's friends. A button opens .povest.</summary>
	public static class CityJournalPage {
		internal const int AllCitiesButton = QuestJournalPages.FirstButton;
		private const int HeadHue = 95, LabelHue = 955, ValueHue = 52, BadHue = 33, FriendHue = 68, Row = 20, FriendsShown = 3;

		public static void Bootstrap() {
			QuestJournalPages.Register(D_QuestJournal.SectionCity, Add, OnButton);
		}

		/// <summary>The cities in the order the tab pages them: where the player stands, then .povest's list.</summary>
		public static List<string> CitiesFor(Player player) {
			List<string> cities = Reputation.CitiesOf(player);
			string here = Reputation.CityAt(player);
			if (here != null) {
				cities.Remove(here);
				cities.Insert(0, here);
			}
			return cities;
		}

		/// <summary>The residents of `city` who think of the player as a friend, best friends first.</summary>
		public static List<Item> FriendsIn(Player player, string city) {
			List<Item> friends = new List<Item>();
			foreach (Item gem in Residents.AllGems) {
				CityVenue work = Residents.WorkplaceOf(gem);
				if ((work != null) && (work.City == city) && (Residents.Friendship(player, Residents.IdOf(gem)) > 0)) {
					friends.Add(gem);
				}
			}
			friends.Sort((a, b) => Residents.Friendship(player, Residents.IdOf(b)).CompareTo(Residents.Friendship(player, Residents.IdOf(a))));
			return friends;
		}

		private static void Add(CompiledGumpDef gump, Player player, int width, int height) {
			gump.AddButton(85, height - 12, 0xFA5, 0xFA7, AllCitiesButton, GumpButtonType.Reply, 0);
			gump.AddLabel(120, height - 10, LabelHue, "Povest ve vsech mestech");
			List<string> cities = CitiesFor(player);
			for (int i = 0; i < cities.Count; i++) {
				int page = i + 1;
				gump.AddPage(page);
				if (page > 1) {
					gump.AddButton(93, 47, 0x15E3, 0x15E7, 0, GumpButtonType.Page, page - 1);
				}
				if (page < cities.Count) {
					gump.AddButton(width - 30, 47, 0x15E1, 0x15E5, 0, GumpButtonType.Page, page + 1);
				}
				AddCity(gump, player, cities[i]);
			}
		}

		private static void AddCity(CompiledGumpDef gump, Player player, string city) {
			int y = 135;
			ReputationLevel level = Reputation.Level(player, city);
			gump.AddLabel(85, y, HeadHue, city.Replace('_', ' '));
			gump.AddLabel(200, y, (level <= ReputationLevel.Podezrely) ? BadHue : ValueHue, Reputation.LevelName(level));
			gump.AddLabel(85, y + Row, LabelHue, "Povest " + Reputation.Of(player, city).ToString(CultureInfo.InvariantCulture));
			y += 2 * Row + 6;
			foreach (Faction faction in Faction.All) {
				if (faction.City == city) {
					ReputationLevel factionLevel = Reputation.FactionLevel(player, faction.Key);
					gump.AddLabel(90, y, LabelHue, faction.Name);
					gump.AddLabel(230, y, (factionLevel <= ReputationLevel.Podezrely) ? BadHue : ValueHue, Reputation.LevelName(factionLevel));
					y += Row;
				}
			}
			y += 8;
			List<Item> friends = FriendsIn(player, city);
			gump.AddLabel(85, y, HeadHue, "Pratele");
			y += Row;
			if (friends.Count == 0) {
				gump.AddLabel(90, y, LabelHue, "Zatim tu nikoho neznas.");
				return;
			}
			for (int i = 0; (i < friends.Count) && (i < FriendsShown); i++) {
				Item gem = friends[i];
				VenueRole role = Residents.RoleOf(gem);
				gump.AddLabel(90, y, FriendHue, Residents.NameOf(gem));
				if (role != null) {
					gump.AddLabel(180, y, LabelHue, role.Name);
				}
				gump.AddLabel(275, y, ValueHue, Residents.Friendship(player, Residents.IdOf(gem)).ToString(CultureInfo.InvariantCulture) + "/" + Residents.MaxFriendship.ToString(CultureInfo.InvariantCulture));
				y += Row;
			}
			if (friends.Count > FriendsShown) {
				gump.AddLabel(90, y, LabelHue, "a dalsi: " + (friends.Count - FriendsShown).ToString(CultureInfo.InvariantCulture));
			}
		}

		private static void OnButton(Player player, int button) {
			QuestJournal.Open(player, D_QuestJournal.SectionCity);
			if (button == AllCitiesButton) {
				Reputation.Povest(player);
			}
		}
	}
}
