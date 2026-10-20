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
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#483/#489 - Paralyze Field was a dead stub before this: old pre-port SPELLFLAG_*
	/// tokens that parse to SpellFlag.None, no ItemDef at all (WallSpellDef.On_EffectGround
	/// dereferences wallDef.Height unconditionally - a guaranteed crash on cast), and even once
	/// given a real ItemDef (mirroring Energy Field's shape in wallSpells.def), plain
	/// WallSpellDef only produces a passive blocking wall with zero paralyze behavior. This class
	/// adds the missing per-tile mechanism via WallSpellDef's own OnWallItemCreated extension
	/// point: each created field tile gets a recurring BoundTimer (plain hand-written class, same
	/// pattern as SpellEffectItem's own SpellEffectTimer - avoids needing a new Plugin subclass
	/// and its generated code-behind) that periodically scans for characters standing on it and
	/// reuses the already-working ParalyzeEffectPluginDef.Paralyze (from Circle V's shipped
	/// Paralyze spell) to paralyze them.</summary>
	[ViewableClass]
	public class ParalyzeFieldSpellDef : WallSpellDef {
		private static readonly TimerKey tickTimerKey = TimerKey.Acquire("_paralyzeFieldTick_");
		private static readonly PluginKey paralyzeKey = PluginKey.Acquire("_paralyzeField_");

		public ParalyzeFieldSpellDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override void OnWallItemCreated(SpellEffectItem item, Character caster) {
			base.OnWallItemCreated(item, caster);

			BoundTimer timer = new ParalyzeFieldTickTimer();
			item.AddTimer(tickTimerKey, timer);
			double interval = MagerySettings.instance.paralyzeFieldTickIntervalSeconds;
			timer.DueInSeconds = interval;
			timer.PeriodInSeconds = interval;
		}

		[SaveableClass]
		[DeepCopyableClass]
		public class ParalyzeFieldTickTimer : BoundTimer {
			[DeepCopyImplementation]
			[LoadingInitializer]
			public ParalyzeFieldTickTimer() {
			}

			protected override void OnTimeout(TagHolder cont) {
				Item item = cont as Item;
				if (item == null) {
					return;
				}
				Map map = item.GetMap();
				if (map == null) {
					return;
				}

				// GetCharsInRange(x, y, 0) is a pure X/Y query (ImmutableRectangle, no Z filtering
				// at all) - deliberately NOT also requiring character.Z == item.Z on top of that:
				// SpellEffectItem.CheckPositionForItem's own Z-search (offsets 0-9 looking for a
				// free spot) can legitimately place the item a step below/above a standing
				// character's exact Z depending on what else occupies the column, which would
				// make an exact-Z check wrongly miss someone who is, in every practical sense,
				// standing in the field. The X/Y column match is the right granularity here.
				double chance = MagerySettings.instance.paralyzeFieldParalyzeChance;
				TimeSpan duration = TimeSpan.FromSeconds(MagerySettings.instance.paralyzeFieldParalyzeDurationSeconds);
				foreach (AbstractCharacter ac in map.GetCharsInRange(item.X, item.Y, 0)) {
					Character character = ac as Character;
					if ((character != null) &&
						((chance >= 1.0) || (Globals.dice.NextDouble() < chance))) {
						ParalyzeEffectPluginDef.Paralyze(character, paralyzeKey, duration);
					}
				}
			}
		}
	}
}
