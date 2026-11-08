using System;
using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1250 - an errand's recipe (design 5.1): which givers may offer it and how its steps are filled from the living
	/// city. A template registers itself from its class's Bootstrap (ErrandTemplates.Register), as journal pages do.</summary>
	public abstract class ErrandTemplate {
		public abstract string Key { get; }

		/// <summary>For the journal: "Vzkaz".</summary>
		public abstract string Name { get; }

		public abstract ErrandTier Tier { get; }

		/// <summary>How long a taken errand lasts, in real minutes (a game day is 24).</summary>
		public virtual int Minutes {
			get { return 60; }
		}

		/// <summary>How often it's drawn among the ones a giver could offer.</summary>
		public virtual int Weight {
			get { return 10; }
		}

		/// <summary>#1252 - the share of the tier's pay this errand earns as it ended (a forgiven debt pays half).</summary>
		public virtual int PayPercent(Errand errand) {
			return 100;
		}

		/// <summary>#1291 - the giver's friendship change as the errand is done: the tier's `baseFriend` and anything the errand's
		/// outcome adds or takes, made as one change (friendship floors at 0, so a penalty after a gain would be lost).</summary>
		public virtual int GiverFriendship(Errand errand, int baseFriend) {
			return baseFriend;
		}

		/// <summary>#1293 - a Things step's thing caught or used by the errand's player: true when it counts (the thing done with:
		/// by default it goes). A runaway may slip away the first time instead.</summary>
		public virtual bool OnThing(Player player, Errand errand, Thing thing) {
			thing.Delete();
			return true;
		}

		/// <summary>#1252 - anything the errand does as it's done, before the pay (a debtor's friendship).</summary>
		public virtual void OnDone(Player player, Errand errand) {
		}

		/// <summary>Fills the errand's name, offer, thanks and steps from the context, or returns false when this giver, here and
		/// now, can't offer it (no second resident in the city, say).</summary>
		public abstract bool TryBuild(ErrandContext context, Errand errand);
	}

	/// <summary>What a template builds from: the giver, the player, the city, and helpers to find the city's people.</summary>
	public sealed class ErrandContext {
		public readonly Player Player;
		public readonly Item GiverGem;
		public readonly string City;
		private List<Item> people;

		public ErrandContext(Player player, Item giverGem, string city) {
			this.Player = player;
			this.GiverGem = giverGem;
			this.City = city;
		}

		public string GiverId {
			get { return Residents.IdOf(this.GiverGem); }
		}

		public string GiverName {
			get { return Residents.NameOf(this.GiverGem); }
		}

		/// <summary>The residents who work in this city, the giver left out.</summary>
		public List<Item> Others {
			get {
				if (this.people == null) {
					this.people = new List<Item>();
					foreach (Item gem in Residents.AllGems) {
						CityVenue work = Residents.WorkplaceOf(gem);
						if ((gem != this.GiverGem) && (work != null) && string.Equals(work.City, this.City, StringComparison.OrdinalIgnoreCase)) {
							this.people.Add(gem);
						}
					}
				}
				return this.people;
			}
		}

		/// <summary>A random other resident, or null. `filter` may narrow it (by role, by venue type).</summary>
		public Item PickOther(Predicate<Item> filter = null) {
			List<Item> fitting = new List<Item>();
			foreach (Item gem in this.Others) {
				if ((filter == null) || filter(gem)) {
					fitting.Add(gem);
				}
			}
			return (fitting.Count == 0) ? null : fitting[Globals.dice.Next(fitting.Count)];
		}

		/// <summary>One of `lines`, at random.</summary>
		public static string Pick(params string[] lines) {
			return lines[Globals.dice.Next(lines.Length)];
		}

		/// <summary>"Merimas Brandor (Pekarna)": a resident and where they work, for the journal and the lines.</summary>
		public static string Who(Item gem) {
			CityVenue work = Residents.WorkplaceOf(gem);
			return (work == null) ? Residents.NameOf(gem) : Residents.NameOf(gem) + " (" + work.Label + ")";
		}
	}

	public static class ErrandTemplates {
		private static readonly Dictionary<string, ErrandTemplate> all = new Dictionary<string, ErrandTemplate>(StringComparer.OrdinalIgnoreCase);

		public static void Register(ErrandTemplate template) {
			all[template.Key] = template;
		}

		public static void Unregister(string key) {
			all.Remove(key);
		}

		public static ErrandTemplate Get(string key) {
			ErrandTemplate template;
			return all.TryGetValue(key, out template) ? template : null;
		}

		public static IEnumerable<ErrandTemplate> All {
			get { return all.Values; }
		}
	}
}
