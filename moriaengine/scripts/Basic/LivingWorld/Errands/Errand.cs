using System;
using System.Collections.Generic;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1250 - what a step of an errand asks (design section 5.1).</summary>
	public enum ErrandStepKind {
		/// <summary>Talk to a resident.</summary>
		Talk = 0,
		/// <summary>Bring a resident a message (nothing to carry) or an item given at the start of the step.</summary>
		Deliver = 1,
		/// <summary>Bring a resident N of an item def, which they take.</summary>
		Fetch = 2,
		/// <summary>Bring a resident a collector bag (#1247) with at least N in it; they take the bag.</summary>
		Collect = 3,
		/// <summary>Come to a resident between two game hours.</summary>
		Wait = 4,
		/// <summary>Answer a resident one of two or three ways; the answer stays on the errand.</summary>
		Choose = 5
	}

	public enum ErrandTier {
		Small = 0,
		Medium = 1,
		Large = 2
	}

	/// <summary>One step: whom to see and what they want. The texts are filled when the errand is made, so the journal and the
	/// dialog show the same words later.</summary>
	[SaveableClass, DeepCopyableClass]
	public sealed class ErrandStep {
		[SaveableData, CopyableData]
		public int kind;

		/// <summary>The resident to see (Residents.IdOf).</summary>
		[SaveableData, CopyableData]
		public string residentId = "";

		/// <summary>Deliver: the item given and taken back (empty = a message); Fetch: what to bring; Collect: the bag's def.</summary>
		[SaveableData, CopyableData]
		public string itemDef = "";

		[SaveableData, CopyableData]
		public int count = 1;

		/// <summary>Wait: the game hours the resident can be found in (from, to; to may be past midnight).</summary>
		[SaveableData, CopyableData]
		public int fromHour, toHour;

		/// <summary>The journal's line: "Dones vzkaz Merimas Brandorovi".</summary>
		[SaveableData, CopyableData]
		public string task = "";

		/// <summary>What the player says in the resident's dialog to do the step.</summary>
		[SaveableData, CopyableData]
		public string say = "";

		/// <summary>What the resident answers when the step is done.</summary>
		[SaveableData, CopyableData]
		public string reply = "";

		/// <summary>#1252 - Deliver: the item lies in this venue (CityVenue.VenueId) to be found, instead of in the pack.</summary>
		[SaveableData, CopyableData]
		public string venueId = "";

		/// <summary>#1252 - the item laid in the venue, so it can be cleared away if the errand ends without it.</summary>
		[SaveableData, CopyableData]
		public Item placed;

		/// <summary>#1252 - a resident who follows the player for this step and must be beside them when it's done (a slow walk).</summary>
		[SaveableData, CopyableData]
		public string companionId = "";

		/// <summary>Choose: the answers, and the resident's line to each.</summary>
		[SaveableData, CopyableData]
		public List<string> choices = new List<string>();

		[SaveableData, CopyableData]
		public List<string> choiceReplies = new List<string>();

		/// <summary>Choose: which answer the player gave, -1 before.</summary>
		[SaveableData, CopyableData]
		public int chosen = -1;

		[LoadingInitializer, DeepCopyImplementation]
		public ErrandStep() {
		}

		public ErrandStepKind Kind {
			get { return (ErrandStepKind) this.kind; }
		}
	}

	/// <summary>An errand a player has taken: who gave it, its steps and how far along it is, when it runs out, and its tier,
	/// which decides the pay (design 5.6).</summary>
	[SaveableClass, DeepCopyableClass]
	public sealed class Errand {
		/// <summary>The player's own number for it, given when taken: dialogs' buttons name an errand by it, not by its place in a
		/// list that can change while a dialog is open.</summary>
		[SaveableData, CopyableData]
		public int id;

		/// <summary>The template's key (ErrandTemplate.Key).</summary>
		[SaveableData, CopyableData]
		public string key = "";

		[SaveableData, CopyableData]
		public string name = "";

		[SaveableData, CopyableData]
		public string giverId = "";

		[SaveableData, CopyableData]
		public string city = "";

		[SaveableData, CopyableData]
		public int tier;

		[SaveableData, CopyableData]
		public List<ErrandStep> steps = new List<ErrandStep>();

		[SaveableData, CopyableData]
		public int index;

		/// <summary>Real time (UTC ticks) it runs out at, set when taken.</summary>
		[SaveableData, CopyableData]
		public long expiresTicks;

		/// <summary>The giver's words when offering it, and when it's done.</summary>
		[SaveableData, CopyableData]
		public string offer = "";

		[SaveableData, CopyableData]
		public string thanks = "";

		/// <summary>The share of the full pay this one earns: less for the same template again and again (design 5.5).</summary>
		[SaveableData, CopyableData]
		public int payPercent = 100;

		[LoadingInitializer, DeepCopyImplementation]
		public Errand() {
		}

		public ErrandTier Tier {
			get { return (ErrandTier) this.tier; }
		}

		public ErrandStep Current {
			get { return (this.index < this.steps.Count) ? this.steps[this.index] : null; }
		}

		public bool IsDone {
			get { return this.index >= this.steps.Count; }
		}

		public DateTime Expires {
			get { return new DateTime(this.expiresTicks, DateTimeKind.Utc); }
		}

		/// <summary>The answer given at a Choose step of this errand, -1 if none (or no such step).</summary>
		public int ChoiceAt(int stepIndex) {
			return ((stepIndex >= 0) && (stepIndex < this.steps.Count)) ? this.steps[stepIndex].chosen : -1;
		}
	}

	/// <summary>A player's errands: the taken ones, and the day's counts for the limits. On the player as one tag.</summary>
	[SaveableClass, DeepCopyableClass]
	public sealed class ErrandLog {
		[SaveableData, CopyableData]
		public List<Errand> active = new List<Errand>();

		/// <summary>The game day the counts below are for.</summary>
		[SaveableData, CopyableData]
		public int day = -1;

		/// <summary>Per city ("Edoras") and per giver (a resident id) errands taken this game day, as "name=count" entries.</summary>
		[SaveableData, CopyableData]
		public List<string> counts = new List<string>();

		/// <summary>The last template done, and how many times in a row.</summary>
		[SaveableData, CopyableData]
		public string lastKey = "";

		[SaveableData, CopyableData]
		public int streak;

		[SaveableData, CopyableData]
		public int done;

		[SaveableData, CopyableData]
		public int nextId = 1;

		[LoadingInitializer, DeepCopyImplementation]
		public ErrandLog() {
		}

		internal void NewDayIfNeeded(int today) {
			if (this.day != today) {
				this.day = today;
				this.counts.Clear();
			}
		}

		internal int Count(string name) {
			string prefix = name + "=";
			foreach (string entry in this.counts) {
				if (entry.StartsWith(prefix, StringComparison.Ordinal)) {
					return int.Parse(entry.Substring(prefix.Length), System.Globalization.CultureInfo.InvariantCulture);
				}
			}
			return 0;
		}

		internal void Add(string name) {
			int now = this.Count(name) + 1;
			string prefix = name + "=";
			this.counts.RemoveAll(entry => entry.StartsWith(prefix, StringComparison.Ordinal));
			this.counts.Add(prefix + now.ToString(System.Globalization.CultureInfo.InvariantCulture));
		}
	}
}
