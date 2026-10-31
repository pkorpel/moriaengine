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
using System.Globalization;
using System.Text;

namespace SteamEngine.CompiledScripts {
	/// <summary>Skill point-spend dialog (#156), opened from .explevel's new "Skills" button -
	/// same plain parchment style as explevel.scp itself. Lists every skill the player's
	/// profession actually configures (GetSkillCap &gt; 0 - unconfigured skills stay hidden,
	/// same "incomplete content stays permissive" spirit used elsewhere), one row per skill with
	/// a "+1" button, laid out in fixed-count columns (not pagination - #202, user didn't like
	/// having to click through pages) so every skill (professions configure up to ~36) is
	/// visible on one page at once.</summary>
	public class D_LevelSpendSkills : CompiledGumpDef {
		private const int Columns = 3;
		private const int ColumnWidth = 300;
		private const int RowHeight = 26;
		private const int BackButtonId = 1;
		private const int CloseButtonId = 2;
		// Per-skill "+1" buttons are identified by 1000 + skill id (SkillName values are all
		// well under 1000), so no separate id-to-skill lookup table is needed in OnResponse.
		private const int SkillButtonIdBase = 1000;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Player player = (Player) sendTo;
			ProfessionDef profession = player.Profession;

			this.SetLocation(50, 50);
			List<AbstractSkillDef> relevantSkills = new List<AbstractSkillDef>();
			if (profession != null) {
				foreach (AbstractSkillDef skillDef in SkillDef.AllSkillDefs) {
					if (profession.GetSkillCap(skillDef.Id) > 0) {
						relevantSkills.Add(skillDef);
					}
				}
			}
			// #204 - was in SkillName enum id order, which has no obvious logic to a player.
			relevantSkills.Sort((a, b) => string.CompareOrdinal(SkillDisplayName(a), SkillDisplayName(b)));

			int rowsPerColumn = ((relevantSkills.Count - 1) / Columns) + 1;
			if (rowsPerColumn < 1) {
				rowsPerColumn = 1;
			}
			int width = 40 + (Columns * ColumnWidth);
			int height = 90 + (rowsPerColumn * RowHeight) + 40;

			this.AddBackground(0, 0, width, height, 2620);
			this.AddLabel(20, 20, 37, "Skills");
			this.AddLabel(20, 45, 3, profession != null
				? ("Unspent skill points: " + player.UnspentSkillPoints)
				: "No profession assigned - nothing to spend on.");

			// Back/Close bottom row, same "paper tab" style as explevel.scp's own buttons (#202) -
			// Back pinned to the left edge, Close to the right edge (#204: they overlapped when
			// both sat near the left edge only ~100px apart - the 2445 graphic renders wider
			// than that).
			this.AddButton(20, height - 30, 2445, 2445, BackButtonId, GumpButtonType.Reply, 0);
			this.AddLabel(25, height - 28, 3, "Back");
			this.AddButton(width - 120, height - 30, 2445, 2445, CloseButtonId, GumpButtonType.Reply, 0);
			this.AddLabel(width - 115, height - 28, 3, "Close");

			for (int i = 0; i < relevantSkills.Count; i++) {
				AbstractSkillDef skillDef = relevantSkills[i];
				int column = i / rowsPerColumn;
				int row = i % rowsPerColumn;
				int x = 20 + (column * ColumnWidth);
				int y = 70 + (row * RowHeight);

				int current = player.GetRealSkillValue(skillDef.Id);
				int cap = profession.GetSkillCap(skillDef.Id);
				this.AddLabel(x, y, 3, SkillDisplayName(skillDef) + ": " + FormatSkillValue(current) + " / " + FormatSkillValue(cap));
				// #204 - craft/gathering skills (e.g. Fishing) can't be bought with points at
				// all, only trained by use - no button for those rows, just a note instead.
				// Real-user-reported: the button also needs to disappear once there's nothing
				// left to spend it on - no unspent points at all, or this specific skill is
				// already at its profession cap (spending would always fail silently otherwise).
				bool canSpendHere = LevelingSystem.IsSkillPointSpendable((SkillName) skillDef.Id)
					&& (player.UnspentSkillPoints > 0)
					&& (current < cap);
				if (canSpendHere) {
					this.AddButton(x + ColumnWidth - 30, y - 2, 2151, 2152, SkillButtonIdBase + skillDef.Id, GumpButtonType.Reply, 0);
				} else if (!LevelingSystem.IsSkillPointSpendable((SkillName) skillDef.Id)) {
					this.AddLabel(x + ColumnWidth - 90, y, 3, "(trained)");
				}
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = (Player) gi.Cont;
			// Button 0 is the real UO client signal for "closed via right-click" (not this
			// dialog's own explicit Close button) - must also return without reopening, or a
			// right-click just silently gets trapped back into the same dialog (#199).
			if ((gr.PressedButton == CloseButtonId) || (gr.PressedButton == 0)) {
				return;
			}
			if (gr.PressedButton == BackButtonId) {
				player.Dialog(player, GumpDef.GetByDefname("d_explevel"));
				return;
			}
			int skillId = gr.PressedButton - SkillButtonIdBase;
			if (skillId >= 0) {
				string refusal = LevelingSystem.RaiseSkill(player, (SkillName) skillId);
				if (refusal != null) {
					player.RedMessage(refusal);
				}
			}
			player.Dialog(player, this, args);
		}

		/// <summary>SkillDef.PrettyDefname is useless for display here - its primary Defname is
		/// always "skill_&lt;id&gt;" (AbstractSkillDef.ParseDefnames always derives it from the
		/// script header number; the readable "Skill_Anatomy" etc. from the script's own
		/// DEFNAME= line becomes the Altdefname instead, same convention as other converted
		/// content this session already hit, e.g. i_worldgem_bit). SkillName's own enum member
		/// names are already clean ("AnimalLore") - just need a space before each internal
		/// capital.</summary>
		private static string SkillDisplayName(AbstractSkillDef skillDef) {
			string raw = ((SkillName) skillDef.Id).ToString();
			StringBuilder sb = new StringBuilder(raw.Length + 4);
			for (int i = 0; i < raw.Length; i++) {
				if ((i > 0) && char.IsUpper(raw[i]) && char.IsLower(raw[i - 1])) {
					sb.Append(' ');
				}
				sb.Append(raw[i]);
			}
			return sb.ToString();
		}

		/// <summary>Skill values are stored internally *10 (classic UO convention - a trainable
		/// skill can gain 0.1 at a time, max 1000 = 100.0%), so the raw int must never be shown
		/// to the player directly.</summary>
		private static string FormatSkillValue(int rawValue) {
			return (rawValue / 10.0).ToString("F1", CultureInfo.InvariantCulture) + "%";
		}
	}
}
