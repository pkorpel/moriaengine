using System;
using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1254 - one extra line in a resident's dialog (D_ResidentTalk): what a venue's people offer, by the venue's type
	/// and the resident's role. Shows decides whether it's offered right now (the bard sings only in the evening); Do runs on
	/// the player's click, after the dialog has checked the reach again.</summary>
	public sealed class VenueAction {
		public readonly string Key, Label;
		private readonly string[] venueTypes, roles;
		private readonly Func<Player, NPC, Item, bool> shows;
		private readonly Action<Player, NPC, Item> act;
		internal int index;

		public VenueAction(string key, string label, string[] venueTypes, string[] roles, Func<Player, NPC, Item, bool> shows, Action<Player, NPC, Item> act) {
			this.Key = key;
			this.Label = label;
			this.venueTypes = venueTypes;
			this.roles = roles;
			this.shows = shows;
			this.act = act;
		}

		public bool Fits(string venueType, string role) {
			return (Array.IndexOf(this.venueTypes, venueType) >= 0) && (Array.IndexOf(this.roles, role) >= 0);
		}

		public bool Shows(Player player, NPC npc, Item gem) {
			return (this.shows == null) || this.shows(player, npc, gem);
		}

		public void Do(Player player, NPC npc, Item gem) {
			this.act(player, npc, gem);
		}
	}

	/// <summary>#1254 - the registry of venue actions, filled from the venue life classes' Bootstrap(). A button is
	/// FirstButton + the action's place in the registry, so it names the same action whatever else the dialog showed.</summary>
	public static class VenueActions {
		public const int FirstButton = 50, MaxActions = 50; //50..99, below D_ResidentTalk's errand steps at 100
		private static readonly List<VenueAction> all = new List<VenueAction>();

		public static void Register(VenueAction action) {
			int at = all.FindIndex(a => a.Key == action.Key);
			if (at >= 0) {
				action.index = at;
				all[at] = action;
				return;
			}
			if (all.Count >= MaxActions) {
				throw new SEException("Too many venue actions, the dialog has buttons for " + MaxActions);
			}
			action.index = all.Count;
			all.Add(action);
		}

		public static VenueAction Get(string key) {
			return all.Find(a => a.Key == key);
		}

		/// <summary>What this resident offers the player now: only at their own workplace, as a patron at home has no dice.</summary>
		public static List<VenueAction> For(Player player, NPC npc, Item gem) {
			List<VenueAction> list = new List<VenueAction>();
			CityVenue work = (gem == null) ? null : Residents.WorkplaceOf(gem);
			if ((work == null) || (npc == null) || (VenueRegistry.At(npc) != work)) {
				return list;
			}
			string type = work.VenueType.Key, role = Residents.RoleKeyOf(gem);
			foreach (VenueAction action in all) {
				if (action.Fits(type, role) && action.Shows(player, npc, gem)) {
					list.Add(action);
				}
			}
			return list;
		}

		public static int ButtonOf(VenueAction action) {
			return FirstButton + action.index;
		}

		public static bool IsButton(int button) {
			return (button >= FirstButton) && (button < FirstButton + MaxActions);
		}

		/// <summary>A clicked button's action, if this resident still offers it.</summary>
		public static VenueAction ByButton(int button, Player player, NPC npc, Item gem) {
			int at = button - FirstButton;
			if ((at < 0) || (at >= all.Count)) {
				return null;
			}
			VenueAction action = all[at];
			return For(player, npc, gem).Contains(action) ? action : null;
		}
	}
}
