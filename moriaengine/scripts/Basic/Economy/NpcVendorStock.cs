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

using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// One fixed stock list shared by every basic NPC vendor (enhancements Phase 14.5, issue
	/// #36) - there's no existing NPC merchant purchase flow at all to extend (only the
	/// player-vendor stub, #25, now fixed separately), so this deliberately stays simple rather
	/// than building a full per-NPC/restocking configuration system. Per-vendor stock is a
	/// natural follow-up once there's real content that needs it.
	///
	/// Entries are resolved from itemdefs confirmed to actually exist and be in active use
	/// elsewhere in the codebase (BandageSkillDef.CleanBandageDef uses "i_bandage";
	/// zbrane/3verite.def's "i_dagger_verite" and ShiningItem.def's "i_torch" are intact, active
	/// converted content, not stripped by the old converter) - not guessed at.
	/// </summary>
	public static class NpcVendorStock {
		public sealed class Entry {
			public readonly string Defname;
			public readonly uint BuyPrice;

			private ThingDef def;
			public ThingDef Def {
				get {
					if (this.def == null) {
						this.def = ThingDef.GetByDefname(this.Defname);
						if (this.def == null) {
							throw new SEException("Unable to find itemdef " + this.Defname + " in scripts.");
						}
					}
					return this.def;
				}
			}

			/// <summary>What this vendor pays a player selling this item back - half the buy price, a simple, predictable markup rather than a general item-appraisal system this codebase doesn't have.</summary>
			public uint SellPrice {
				get { return this.BuyPrice / 2; }
			}

			public Entry(string defname, uint buyPrice) {
				this.Defname = defname;
				this.BuyPrice = buyPrice;
			}
		}

		public static readonly IList<Entry> Entries = new List<Entry> {
			new Entry("i_torch", 3),
			new Entry("i_bandage", 2),
			new Entry("i_dagger_verite", 150),
		}.AsReadOnly();

		/// <summary>Null if no stock entry sells this exact itemdef.</summary>
		public static Entry FindByDef(ThingDef def) {
			foreach (Entry entry in Entries) {
				if (entry.Def == def) {
					return entry;
				}
			}
			return null;
		}
	}
}
