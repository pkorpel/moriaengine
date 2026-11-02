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

using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - systems/NecroBook.scp: the necromancer's spells go past the client's 64, so they're cast by these
	/// commands or the buttons of d_nec, from the book that holds them.</summary>
	public static class NecroBook {
		[SteamFunction]
		public static void FrostBolt(Player self) {
			Cast(self, "s_frost_bolt");
		}

		[SteamFunction]
		public static void NecroBolt(Player self) {
			Cast(self, "s_fire_bolt");
		}

		[SteamFunction]
		public static void NecroLight(Player self) {
			Cast(self, "s_light");
		}

		[SteamFunction]
		public static void NecroAnimate(Player self) {
			Cast(self, "s_animate_dead");
		}

		[SteamFunction]
		public static void NecroSummon(Player self) {
			Cast(self, "s_summon_creature");
		}

		internal static void Cast(Player self, string spellDefname) {
			MagerySkillDef.TryCastSpellFromBook(self, SpellDef.GetByDefname(spellDefname));
		}
	}

	/// <summary>f_necrobook: what a necromancer sees of a book holding spells of theirs. Its first page's back arrow opens the
	/// plain book.</summary>
	public class D_NecroBook : CompiledGumpDef {
		private const int FireBolt = 1, AnimateDead = 2, FrostBolt = 3, SummonUndead = 4, BoneArmorButton = 5, PlainBook = 6, Light = 7;
		private const int ButtonGump = 2299, NameHue = 967, Frame = 83, Stroke = 2101;
		private const int SummonInBook = 40;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			SpellBook book = (SpellBook) args[0];
			NecroBookLoc texts = Loc<NecroBookLoc>.Get(((Player) sendTo).Language);
			this.SetLocation(40, 63);
			this.AddImage(77, 53, 2220);

			this.AddPage(1);
			this.AddButton(127, 61, 2205, 2205, PlainBook, GumpButtonType.Reply, 0);
			this.AddButton(398, 61, 2206, 2206, 0, GumpButtonType.Page, 2);
			this.Entry(true, FireBolt, 14068, book.HasSpell(69) ? texts.FireBolt : texts.NotInBook);
			this.Entry(false, AnimateDead, 4414, book.HasSpell(66) ? texts.AnimateDead : texts.NotInBook);
			this.AddItem(332, 105, 3789);

			this.AddPage(2);
			this.AddButton(127, 61, 2205, 2205, 0, GumpButtonType.Page, 1);
			this.AddButton(398, 61, 2206, 2206, 0, GumpButtonType.Page, 3);
			this.Entry(true, FrostBolt, 9007, book.HasSpell(70) ? texts.FrostBolt : texts.NotInBook);
			this.Entry(false, SummonUndead, 8428, (book.HasSpell(65) && book.HasSpell(SummonInBook)) ? texts.SummonUndead : texts.NotInBook);

			this.AddPage(3);
			this.AddButton(127, 61, 2205, 2205, 0, GumpButtonType.Page, 2);
			this.Entry(true, BoneArmorButton, 5204, book.HasSpell(67) ? texts.BoneArmor : texts.NotInBook);
			this.Entry(false, Light, 2594, book.HasSpell(68) ? texts.Light : texts.NotInBook);
		}

		private void Entry(bool leftSide, int button, int picture, string name) {
			int frameX = leftSide ? 165 : 325;
			this.AddButton(leftSide ? 173 : 330, 100, ButtonGump, ButtonGump, button, GumpButtonType.Reply, 0);
			this.AddBackground(frameX, 90, 60, 60, Frame);
			this.AddItem(leftSide ? 172 : 330, 100, picture);
			int strokeX = leftSide ? 140 : 300;
			for (int i = 0; i < 7; i++) {
				this.AddImage(strokeX + (i * 15), 170, Stroke);
			}
			this.AddLabel(leftSide ? 145 : 300, 190, NameHue, name);
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = (Player) gi.Cont;
			SpellBook book = (SpellBook) args[0];
			switch (gr.PressedButton) {
				case FireBolt:
					NecroBook.Cast(player, "s_fire_bolt");
					break;
				case FrostBolt:
					NecroBook.Cast(player, "s_frost_bolt");
					break;
				case Light:
					NecroBook.Cast(player, "s_light");
					break;
				case AnimateDead:
					NecroBook.Cast(player, "s_animate_dead");
					break;
				case SummonUndead:
					NecroBook.Cast(player, "s_summon_creature");
					break;
				case BoneArmorButton:
					BoneArmor.NecroArmor(player);
					break;
				case PlainBook:
					if (!book.IsDeleted) {
						book.DisplayTo(player);
					}
					break;
			}
		}
	}

	internal class NecroBookLoc : CompiledLocStringCollection<NecroBookLoc> {
		public string NotInBook = "Neni v knize";
		public string FireBolt = "Firebolt";
		public string AnimateDead = "Animate Dead";
		public string FrostBolt = "Frost bolt";
		public string SummonUndead = "Summon Undead";
		public string BoneArmor = "Bone Armor";
		public string Light = "Light";
	}
}
