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
using System.Runtime.CompilerServices;
using SteamEngine.Common;

using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// #799 e_explosiondispell (36 monsters). Archive: when hit, tag.e_explosiondispell_chance
	/// (permille, default 50) - at most once per tag.e_explosiondispell_time seconds (default
	/// 30) - to throw a dispel bomb (i_potion_ExplosionDispell) at a random spot within
	/// tag.e_explosiondispell_range (default 5). It counts down 3 s, then dispels every
	/// character within 5 tiles of it - the thrower included: summons vanish, dispellable
	/// effects end (Character.On_Dispell). No real potion item is created; the fuse runs off the
	/// thrower's AI tick, so a bomb whose thrower died with it fizzles.</summary>
	[ViewableClass]
	public class DispelBombAbilityDef : PassiveAbilityDef {
		private FieldValue chance;
		private FieldValue cooldown;
		private FieldValue throwRange;
		private FieldValue fuse;
		private FieldValue blastRange;

		public DispelBombAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("dispelBombChance", new double[] { 50, 50 }, typeof(double[]));
			this.cooldown = this.InitTypedField("dispelBombCooldown", new double[] { 30, 30 }, typeof(double[]));
			this.throwRange = this.InitTypedField("dispelBombThrowRange", new double[] { 5, 5 }, typeof(double[]));
			this.fuse = this.InitTypedField("dispelBombFuse", (double) 3, typeof(double));
			this.blastRange = this.InitTypedField("dispelBombBlastRange", 5, typeof(int));
		}

		/// <summary>Permille (0-1000) {min,max} chance per landed swing taken.</summary>
		public new double[] Chance {
			get { return (double[]) this.chance.CurrentValue; }
			set { this.chance.CurrentValue = value; }
		}

		/// <summary>Seconds before it can throw again.</summary>
		public new double[] Cooldown {
			get { return (double[]) this.cooldown.CurrentValue; }
			set { this.cooldown.CurrentValue = value; }
		}

		/// <summary>How far from itself the bomb can land.</summary>
		public double[] ThrowRange {
			get { return (double[]) this.throwRange.CurrentValue; }
			set { this.throwRange.CurrentValue = value; }
		}

		/// <summary>Seconds from landing to going off.</summary>
		public double Fuse {
			get { return (double) this.fuse.CurrentValue; }
			set { this.fuse.CurrentValue = value; }
		}

		/// <summary>Everyone within this many tiles of the bomb is dispelled.</summary>
		public int BlastRange {
			get { return (int) this.blastRange.CurrentValue; }
			set { this.blastRange.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private const int DispelBombGraphic = 0x0F0D, DispelBombHue = 0x0993;

		private static DispelBombAbilityDef dispelBombAbilityDef;
		internal static DispelBombAbilityDef DispelBombDef {
			get {
				if (dispelBombAbilityDef == null) {
					dispelBombAbilityDef = AbstractScript.GetByDefname("a_tevent_e_explosiondispell") as DispelBombAbilityDef;
				}
				return dispelBombAbilityDef;
			}
		}

		private class PendingDispelBomb {
			internal Point4D point;
			internal double goesOffAt;
		}

		private static readonly ConditionalWeakTable<NPC, List<PendingDispelBomb>> dispelBombs = new ConditionalWeakTable<NPC, List<PendingDispelBomb>>();
		private static readonly TagKey nextDispelBombTimeTK = TagKey.Acquire("_npcNextDispelBombTime_");

		/// <summary>#799 e_explosiondispell - defender side, after a landed swing. Returns true when it threw.</summary>
		public static bool TryApplyDispelBomb(Character defender) {
			NPC self = defender as NPC;
			DispelBombAbilityDef def = DispelBombDef;
			if ((self == null) || !HasAbilityDef(self, def) || !self.IsAliveAndValid) {
				return false;
			}
			NPCDef npcDef = TuningDef(self); //#1022 a form's while Darkmoon has one
			if (!RollChance(RollTunable(npcDef.DispelBombChance, def.Chance))) {
				return false;
			}
			object due = self.GetTag(nextDispelBombTimeTK);
			if ((due is double) && (Globals.TimeInSeconds < (double) due)) {
				return false;
			}
			self.SetTag(nextDispelBombTimeTK, Globals.TimeInSeconds + RollTunable(npcDef.DispelBombCooldown, def.Cooldown));
			int range = (int) RollTunable(npcDef.DispelBombRange, def.ThrowRange);
			ThrowDispelBomb(self, self.X + Globals.dice.Next(-range, range + 1), self.Y + Globals.dice.Next(-range, range + 1));
			return true;
		}

		/// <summary>Throws a bomb at (x, y) without the chance/cooldown - for tests.</summary>
		public static void ThrowDispelBomb(NPC self, int x, int y) {
			DispelBombAbilityDef def = DispelBombDef;
			Point4D point = new Point4D(x, y, self.Z, self.M);
			self.Emote("throws a dispel bomb");
			EffectFactory.EffectFromTo(self, point, DispelBombGraphic, 10, 1, false, false, DispelBombHue, RenderModes.Opaque);
			dispelBombs.GetOrCreateValue(self).Add(new PendingDispelBomb {
				point = point,
				goesOffAt = Globals.TimeInSeconds + ((def == null) ? 3 : def.Fuse)
			});
		}

		/// <summary>Every AI tick: sets off the bombs whose fuse ran out. Returns how many
		/// characters were dispelled.</summary>
		public static int TryDispelBombTick(NPC self) {
			List<PendingDispelBomb> bombs;
			if (!dispelBombs.TryGetValue(self, out bombs) || (bombs.Count == 0)) {
				return 0;
			}
			int dispelled = 0;
			for (int i = bombs.Count - 1; i >= 0; i--) {
				if (bombs[i].goesOffAt <= Globals.TimeInSeconds) {
					Point4D point = bombs[i].point;
					bombs.RemoveAt(i);
					dispelled += ExplodeDispelBomb(self, point);
				}
			}
			return dispelled;
		}

		private static DispellDef dispelSpellDef;
		private static int ExplodeDispelBomb(NPC thrower, Point4D point) {
			if (dispelSpellDef == null) {
				dispelSpellDef = SpellDef.GetByDefname("s_dispel") as DispellDef;
			}
			DispelBombAbilityDef def = DispelBombDef;
			EffectFactory.StationaryEffectAt(point, ExplosionEffect, 6, 15);
			int dispelled = 0;
			foreach (Character ch in CharsAround(thrower.GetMap(), point.X, point.Y, (def == null) ? 5 : def.BlastRange)) {
				if (ch.IsDeleted || ch.Flag_Dead) {
					continue;
				}
				SpellEffectArgs args = SpellEffectArgs.Acquire(thrower, point, ch, dispelSpellDef, 1000, EffectFlag.FromAbility | EffectFlag.HarmfulEffect);
				DispellDef.ShowDispellEffect(ch);
				ch.On_Dispell(args);
				dispelled++;
			}
			return dispelled;
		}
	}
}
