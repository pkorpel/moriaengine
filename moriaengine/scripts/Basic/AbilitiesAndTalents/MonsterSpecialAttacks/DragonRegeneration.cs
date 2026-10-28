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
using SteamEngine.Common;

using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// #797 e_dragon_regeneration (25 monsters). Archive: an i_dragon_regenerator memory item
	/// heals tag.regeneration_hp (default 100) every tag.regeneration_speed seconds (default
	/// 10) while the monster is hurt, with an "is regenerating." emote. Checked once per AI
	/// think-tick in any state (NPCAiTimer.OnTimeout), so a monster that broke off still heals.</summary>
	[ViewableClass]
	public class DragonRegenerationAbilityDef : PassiveAbilityDef {
		private FieldValue amount;
		private FieldValue interval;

		public DragonRegenerationAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.amount = this.InitTypedField("regenerationAmount", new double[] { 100, 100 }, typeof(double[]));
			this.interval = this.InitTypedField("regenerationInterval", new double[] { 10, 10 }, typeof(double[]));
		}

		/// <summary>Hit points healed per pulse.</summary>
		public double[] Amount {
			get { return (double[]) this.amount.CurrentValue; }
			set { this.amount.CurrentValue = value; }
		}

		/// <summary>Seconds between pulses.</summary>
		public double[] Interval {
			get { return (double[]) this.interval.CurrentValue; }
			set { this.interval.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static DragonRegenerationAbilityDef dragonRegenerationAbilityDef;
		internal static DragonRegenerationAbilityDef DragonRegenerationDef {
			get {
				if (dragonRegenerationAbilityDef == null) {
					dragonRegenerationAbilityDef = AbstractScript.GetByDefname("a_tevent_e_dragon_regeneration") as DragonRegenerationAbilityDef;
				}
				return dragonRegenerationAbilityDef;
			}
		}

		private static readonly TagKey nextRegenerationTimeTK = TagKey.Acquire("_npcNextRegenerationTime_");

		/// <summary>#797 e_dragon_regeneration - returns the hit points healed this call (0 when
		/// not due). The first pulse comes one interval after the monster is first found hurt.</summary>
		public static int TryApplyDragonRegeneration(NPC self) {
			DragonRegenerationAbilityDef def = DragonRegenerationDef;
			if (!HasAbilityDef(self, def)) {
				return 0;
			}
			if (self.Hits >= self.MaxHits) {
				self.RemoveTag(nextRegenerationTimeTK);
				return 0;
			}
			NPCDef npcDef = (NPCDef) self.Def;
			object due = self.GetTag(nextRegenerationTimeTK);
			double now = Globals.TimeInSeconds;
			if (!(due is double)) {
				self.SetTag(nextRegenerationTimeTK, now + RollTunable(npcDef.RegenerationInterval, def.Interval));
				return 0;
			}
			if (now < (double) due) {
				return 0;
			}
			self.SetTag(nextRegenerationTimeTK, now + RollTunable(npcDef.RegenerationInterval, def.Interval));
			int healed = Math.Min((int) Math.Round(RollTunable(npcDef.RegenerationHp, def.Amount)), self.MaxHits - self.Hits);
			self.Hits = (short) (self.Hits + healed);
			self.Emote("is regenerating.");
			return healed;
		}
	}
}
