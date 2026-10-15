/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	/// <summary>One possible monster for a spawn gem, with its own relative weight (#210) -
	/// lets a GM configure a small chance of something rare alongside common monsters. Weights
	/// don't need to sum to anything in particular - RollSpawnDef normalizes them at roll time.</summary>
	[SaveableClass, DeepCopyableClass]
	public sealed class SpawnEntry {

		[SaveableData, CopyableData]
		public IThingFactory SpawnDef;

		[SaveableData, CopyableData]
		public double Probability;

		[LoadingInitializer, DeepCopyImplementation]
		public SpawnEntry() {
		}

		public SpawnEntry(IThingFactory spawnDef, double probability) {
			this.SpawnDef = spawnDef;
			this.Probability = probability;
		}

		/// <summary>Null-safe display name for the spawn dialog's Baseid field - SpawnDef is null
		/// whenever the GM typed a chardef name that doesn't resolve (AbstractDef.GetByDefname
		/// returning null), which the dialog must still be able to redisplay without a NRE.</summary>
		public string SpawnDefDisplayName {
			get {
				ThingDef def = this.SpawnDef as ThingDef;
				return def == null ? "" : def.PrettyDefname;
			}
		}

		/// <summary>One decimal place, matching this dialog's own displayed precision for
		/// MinTime/MaxTime - a plain ToString() on a double can print many more digits than
		/// were ever actually typed in (floating point artifacts).</summary>
		public string ProbabilityDisplay {
			get {
				return this.Probability.ToString("0.#", CultureInfo.InvariantCulture);
			}
		}

		/// <summary>True iff SpawnDef resolved to an actual chardef - checked here (short-circuit
		/// in plain C#) rather than as "SpawnDef == 0 || !SpawnDef.IsCharDef" in LScript, since
		/// LScript's "||" evaluates both operands unconditionally and a null SpawnDef would throw
		/// a NullReferenceException on the second operand.</summary>
		public bool HasValidCharDef {
			get {
				ThingDef def = this.SpawnDef as ThingDef;
				return def != null && def.IsCharDef;
			}
		}
	}

	[SaveableClass, DeepCopyableClass]
	public sealed class SpawnInfo {

		[SaveableData, CopyableData]
		public List<SpawnEntry> Entries = new List<SpawnEntry>();

		[SaveableData, CopyableData]
		public short Amount;

		/// <summary>Minutes - #210, now a decimal (was a whole-number short) so a GM can
		/// configure sub-minute respawn delays (e.g. 0.5 = 30 seconds), and so the random roll
		/// between MinTime/MaxTime is genuinely continuous rather than only ever landing on one
		/// of the two typed-in whole-number endpoints. See RollRespawnDelaySeconds - LScript's own
		/// "{a,b}" random-range operator always truncates both ends to int before rolling
		/// (OpNode_Final_RandomExpression_Simple_Variable.Run), so a genuine decimal roll has to
		/// be computed here in C# instead of via that operator.</summary>
		[SaveableData, CopyableData]
		public double MinTime;

		/// <summary>Minutes - see MinTime's own doc comment.</summary>
		[SaveableData, CopyableData]
		public double MaxTime;

		[SaveableData, CopyableData]
		public short Homedist;


		[LoadingInitializer, DeepCopyImplementation]
		public SpawnInfo() {
		}

		/// <summary>Convenience single-monster constructor - used by spawn.scp's own "no
		/// existing dialog data yet" default.</summary>
		public SpawnInfo(short amount, IThingFactory spawnDef, double minTime, double maxTime, short homedist) {
			this.Amount = amount;
			this.Entries.Add(new SpawnEntry(spawnDef, 1));
			this.MinTime = minTime;
			this.MaxTime = maxTime;
			this.Homedist = homedist;
		}

		/// <summary>Fields-only constructor, Entries left empty - used by spawn.scp's dialog
		/// response handlers to rebuild a SpawnInfo from the submitted top-section fields before
		/// separately populating Entries from the submitted monster rows (#210).</summary>
		public SpawnInfo(short amount, double minTime, double maxTime, short homedist) {
			this.Amount = amount;
			this.MinTime = minTime;
			this.MaxTime = maxTime;
			this.Homedist = homedist;
		}

		public string MinTimeDisplay {
			get {
				return this.MinTime.ToString("0.#", CultureInfo.InvariantCulture);
			}
		}

		public string MaxTimeDisplay {
			get {
				return this.MaxTime.ToString("0.#", CultureInfo.InvariantCulture);
			}
		}

		public int EntryCount {
			get {
				return this.Entries.Count;
			}
		}

		/// <summary>Null-safe positional accessor - LScript resolves this as a plain method
		/// call, avoiding any uncertainty about whether its dotted-expression grammar supports
		/// a generic List&lt;T&gt;'s own indexer the same way.</summary>
		public SpawnEntry GetEntry(int index) {
			if ((index < 0) || (index >= this.Entries.Count)) {
				return null;
			}
			return this.Entries[index];
		}

		public bool HasAnyValidCharDef {
			get {
				foreach (SpawnEntry entry in this.Entries) {
					if (entry.HasValidCharDef) {
						return true;
					}
				}
				return false;
			}
		}

		/// <summary>Picks one entry's SpawnDef, weighted by Probability among entries with a
		/// valid chardef (#210) - weights are relative, not required to sum to 100. Returns null
		/// only if there's truly nothing valid to pick (HasAnyValidCharDef is false) - callers
		/// should check that first via spawnValuesMakeSense rather than relying on this null.</summary>
		public IThingFactory RollSpawnDef() {
			double total = 0;
			foreach (SpawnEntry entry in this.Entries) {
				if (entry.HasValidCharDef && (entry.Probability > 0)) {
					total += entry.Probability;
				}
			}
			if (total <= 0) {
				return null;
			}
			double roll = Globals.dice.NextDouble() * total;
			double cumulative = 0;
			foreach (SpawnEntry entry in this.Entries) {
				if (!entry.HasValidCharDef || (entry.Probability <= 0)) {
					continue;
				}
				cumulative += entry.Probability;
				if (roll < cumulative) {
					return entry.SpawnDef;
				}
			}
			//floating-point rounding edge case at the very top of the range - return the last valid entry
			for (int i = this.Entries.Count - 1; i >= 0; i--) {
				SpawnEntry entry = this.Entries[i];
				if (entry.HasValidCharDef && (entry.Probability > 0)) {
					return entry.SpawnDef;
				}
			}
			return null;
		}

		/// <summary>Genuinely continuous roll between MinTime and MaxTime (inclusive), in
		/// seconds - see MinTime's own doc comment for why this can't just be LScript's "{a,b}"
		/// operator.</summary>
		public double RollRespawnDelaySeconds() {
			double min = Math.Min(this.MinTime, this.MaxTime);
			double max = Math.Max(this.MinTime, this.MaxTime);
			return (min + (Globals.dice.NextDouble() * (max - min))) * 60;
		}
	}
}
