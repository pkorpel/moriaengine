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
	/// <summary>Skill point-spend dialog (#156), opened from .explevel's new "Skills" button -
	/// same plain parchment style as explevel.scp itself. Lists every skill the player's
	/// profession actually configures (GetSkillCap &gt; 0 - unconfigured skills stay hidden,
	/// same "incomplete content stays permissive" spirit used elsewhere), one row per skill
	/// with a "+1" button, paginated client-side (no server round-trip for page switching) since
	/// a profession can configure 30+ skills - too many for one screen.</summary>
	public class D_LevelSpendSkills : CompiledGumpDef {
		private const int RowsPerPage = 8;
		private const int CloseButtonId = 1;
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

			int pageCount = ((relevantSkills.Count - 1) / RowsPerPage) + 1;
			if (pageCount < 1) {
				pageCount = 1;
			}
			int height = 90 + (RowsPerPage * 26) + 40;

			this.AddPage(0);
			this.AddBackground(0, 0, 420, height, 2620);
			this.AddLabel(20, 20, 37, "Skills");
			this.AddLabel(20, 45, 3, profession != null
				? ("Unspent skill points: " + player.UnspentSkillPoints)
				: "No profession assigned - nothing to spend on.");
			this.AddButton(20, height - 30, 2151, 2152, CloseButtonId, GumpButtonType.Reply, 0);
			this.AddLabel(55, height - 28, 3, "Close");

			for (int p = 0; p < pageCount; p++) {
				int navY = height - 30;
				if (p > 0) {
					this.AddButton(150, navY, 2151, 2152, 0, GumpButtonType.Page, p); // page is 1-based client-side
					this.AddLabel(185, navY + 2, 3, "Prev");
				}
				if (p < (pageCount - 1)) {
					this.AddButton(250, navY, 2151, 2152, 0, GumpButtonType.Page, p + 2);
					this.AddLabel(285, navY + 2, 3, "Next");
				}
			}

			for (int p = 0; p < pageCount; p++) {
				this.AddPage(p + 1);
				int y = 70;
				for (int i = p * RowsPerPage; (i < relevantSkills.Count) && (i < ((p + 1) * RowsPerPage)); i++) {
					AbstractSkillDef skillDef = relevantSkills[i];
					int current = player.GetRealSkillValue(skillDef.Id);
					int cap = profession.GetSkillCap(skillDef.Id);
					this.AddLabel(20, y, 3, skillDef.PrettyDefname + ": " + current + " / " + cap);
					this.AddButton(320, y - 2, 2151, 2152, SkillButtonIdBase + skillDef.Id, GumpButtonType.Reply, 0);
					y += 26;
				}
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if (gr.PressedButton == CloseButtonId) {
				return;
			}
			Player player = (Player) gi.Cont;
			int skillId = gr.PressedButton - SkillButtonIdBase;
			if (skillId >= 0) {
				if (!LevelingSystem.TrySpendSkillPoints(player, (SkillName) skillId, 1)) {
					player.RedMessage("Could not spend a point there - check your remaining points and this skill's cap.");
				}
			}
			player.Dialog(player, this, args);
		}
	}
}
