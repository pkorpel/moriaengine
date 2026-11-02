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
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - Erebor's i_scroll_case (systems/svitkovac.scp): counts of scrolls by spell, on tags. Sound scrolls go
	/// in by dropping, by the cursor or a whole container at a time.</summary>
	public class t_svitkovac : CompiledTriggerGroup {
		private const int MostScrolls = 999999; //maxscrolls
		internal const int MostAtOnce = 60000; //an amount overflows past 65535
		internal const int LastSphereScroll = 70, ManaShieldId = 104;

		private static TagKey KeyOf(int spellId) {
			return TagKey.Acquire("scroll_" + spellId.ToString(CultureInfo.InvariantCulture));
		}

		internal static int CountIn(Item scrollCase, int spellId) {
			return (scrollCase.GetTag(KeyOf(spellId)) is int count) ? count : 0;
		}

		internal static bool CanUse(Player self, Item scrollCase) {
			if (!self.CanReach(scrollCase).Allow) {
				self.RedMessage(Loc<ScrollCaseLoc>.Get(self.Language).CantReach);
				return false;
			}
			return true;
		}

		public TriggerResult On_DClick(Item self, Character clicker) {
			if ((clicker is Player player) && !self.IsLockedAgainst(player) && CanUse(player, self)) {
				player.Sound(85);
				player.Dialog(player, SingletonScript<D_ScrollCase>.Instance, new DialogArgs(self, 0));
			}
			return TriggerResult.Cancel;
		}

		public TriggerResult On_PutItemOn(Item self, Character puttingChar, Item dropped, Item target) {
			if (puttingChar is Player player) {
				if (dropped is SpellScroll scroll) {
					if (!Add(player, self, scroll)) {
						scroll.Cont = player.Backpack;
					}
				} else {
					player.RedMessage(Loc<ScrollCaseLoc>.Get(player.Language).OnlyScrolls);
				}
			}
			return TriggerResult.Cancel;
		}

		/// <summary>f_svitkovacAddScroll. False when some of it didn't fit.</summary>
		internal static bool Add(Player self, Item scrollCase, SpellScroll scroll) {
			ScrollCaseLoc texts = Loc<ScrollCaseLoc>.Get(self.Language);
			int spellId = scroll.SpellId;
			int inCase = CountIn(scrollCase, spellId);
			int amount = scroll.Amount;
			int taken = System.Math.Min(amount, MostScrolls - inCase);
			string name = scroll.Name;
			if (taken < amount) {
				if (taken <= 0) {
					self.RedMessage(string.Format(CultureInfo.InvariantCulture, texts.Full, name));
				} else {
					self.RedMessage(string.Format(CultureInfo.InvariantCulture, self.IsFemale ? texts.SomeFemale : texts.SomeMale, taken, name));
				}
			} else {
				self.SysMessage(string.Format(CultureInfo.InvariantCulture, self.IsFemale ? texts.AddedFemale : texts.AddedMale, taken, name));
			}
			scrollCase.SetTag(KeyOf(spellId), inCase + taken);
			if (taken > 0) {
				scroll.Consume(taken);
			}
			return taken >= amount;
		}

		/// <summary>f_scroll_case_remove</summary>
		internal static void TakeOut(Player self, Item scrollCase, int spellId, int wanted) {
			ScrollCaseLoc texts = Loc<ScrollCaseLoc>.Get(self.Language);
			int inCase = CountIn(scrollCase, spellId);
			wanted = System.Math.Min(System.Math.Min(wanted, MostAtOnce), inCase);
			if (wanted <= 0) {
				self.SysMessage(texts.WantNumber);
				return;
			}
			Item scroll = NewScroll(self, spellId);
			if (scroll != null) {
				scroll.Amount = wanted;
				scrollCase.SetTag(KeyOf(spellId), inCase - wanted);
				self.SysMessage(string.Format(CultureInfo.InvariantCulture, self.IsFemale ? texts.TookFemale : texts.TookMale, wanted, scroll.Name));
			}
		}

		/// <summary>f_svitkovacRemoveOne, for a button that takes one of everything.</summary>
		internal static void TakeOneOfEach(Player self, Item scrollCase) {
			List<int> ids = new List<int>();
			foreach (SpellDef spell in SpellDef.AllSpellDefs) {
				ids.Add(spell.Id);
			}
			foreach (int spellId in ids) {
				int inCase = CountIn(scrollCase, spellId);
				if ((inCase > 0) && (NewScroll(self, spellId) != null)) {
					scrollCase.SetTag(KeyOf(spellId), inCase - 1);
				}
			}
		}

		private static Item NewScroll(Player self, int spellId) {
			SpellDef spell = SpellDef.GetById(spellId);
			return (spell?.ScrollItem == null) ? null : (Item) spell.ScrollItem.Create(self.Backpack);
		}
	}

	public class Targ_ScrollCase : CompiledTargetDef {
		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			Item scrollCase = (Item) parameter;
			ScrollCaseLoc texts = Loc<ScrollCaseLoc>.Get(self.Language);
			if (scrollCase.IsDeleted || !t_svitkovac.CanUse(self, scrollCase)) {
				return TargetResult.Done;
			}
			if (targetted.TopObj() != self) {
				self.RedMessage(string.Format(CultureInfo.InvariantCulture, texts.MustCarry, targetted.Name));
			} else if (targetted is SpellScroll scroll) {
				t_svitkovac.Add(self, scrollCase, scroll);
			} else if (targetted is Container container) {
				List<SpellScroll> scrolls = new List<SpellScroll>();
				foreach (Item inside in container) {
					if (inside is SpellScroll one) {
						scrolls.Add(one);
					}
				}
				foreach (SpellScroll one in scrolls) {
					t_svitkovac.Add(self, scrollCase, one);
				}
			} else {
				self.RedMessage(texts.NotAScroll);
			}
			self.SysMessage(texts.AimNext);
			return TargetResult.RestartTargetting;
		}
	}

	/// <summary>d_scroll_case2. The second argument is the page's first spell id less one: 0, 16, 32, 48 are the circles,
	/// 64 necromancy, 96 the rest.</summary>
	public class D_ScrollCase : CompiledGumpDef {
		private const int AddScrolls = 18, TakeAll = 19, Circles1 = 30, Circles3 = 31, Circles5 = 32, Circles7 = 33, Necro = 34, Other = 35;
		private const int NecroPage = 64, OtherPage = 96, LinesPerSide = 8, LineHeight = 15;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Item scrollCase = (Item) args[0];
			int page = (int) args[1];
			ScrollCaseLoc texts = Loc<ScrollCaseLoc>.Get(((Player) sendTo).Language);
			this.SetLocation(100, 100);
			this.AddPage(0);
			this.AddImage(10, 10, 2200);
			this.AddImage(35, 50, 57);
			for (int x = 55; x <= 130; x += 15) {
				this.AddImage(x, 50, 58);
			}
			this.AddImage(140, 50, 59);
			this.AddImage(193, 50, 57);
			for (int x = 220; x <= 295; x += 15) {
				this.AddImage(x, 50, 58);
			}
			this.AddImage(305, 50, 59);

			this.AddButton(305, 38, 4033, 4033, TakeAll, GumpButtonType.Reply, 0);
			this.AddButton(140, 38, 4033, 4033, AddScrolls, GumpButtonType.Reply, 0);
			this.AddButton(40, 185, 2225, 2225, Circles1, GumpButtonType.Reply, 0);
			this.AddButton(77, 185, 2226, 2226, Circles1, GumpButtonType.Reply, 0);
			this.AddButton(113, 185, 2227, 2227, Circles3, GumpButtonType.Reply, 0);
			this.AddButton(147, 185, 2228, 2228, Circles3, GumpButtonType.Reply, 0);
			this.AddButton(210, 185, 2229, 2229, Circles5, GumpButtonType.Reply, 0);
			this.AddButton(243, 185, 2230, 2230, Circles5, GumpButtonType.Reply, 0);
			this.AddButton(279, 185, 2231, 2231, Circles7, GumpButtonType.Reply, 0);
			this.AddButton(316, 185, 2232, 2232, Circles7, GumpButtonType.Reply, 0);
			this.AddButton(0, 15, 0x2635, 0x2635, Necro, GumpButtonType.Reply, 0);
			this.AddButton(0, 40, 0x2634, 0x2634, Other, GumpButtonType.Reply, 0);

			this.AddLabel(35, 15, 0, texts.Necro);
			this.AddLabel(35, 38, 0, texts.Other);
			this.AddLabel(102, 35, 0, texts.Add);
			this.AddLabel(262, 35, 0, texts.Take);
			if (page < NecroPage) {
				this.AddLabel(90, 14, 0, string.Format(CultureInfo.InvariantCulture, texts.Circle, (page / 8) + 1));
				this.AddLabel(250, 14, 0, string.Format(CultureInfo.InvariantCulture, texts.Circle, (page / 8) + 2));
			} else {
				this.AddLabel(90, 12, 0, (page < OtherPage) ? texts.Necro : texts.Other);
			}

			int line = 0, column = 35;
			for (int i = 1; i <= (LinesPerSide * 2); i++) {
				int spellId = page + i;
				int count = t_svitkovac.CountIn(scrollCase, spellId);
				if (count > 0) {
					this.AddButton(column, 65 + (line * LineHeight), 2103, 2104, i, GumpButtonType.Reply, 0);
					this.AddLabel(column + 12, 60 + (line * LineHeight), 0, FormatAmount(count));
					this.AddLabel(column + 48, 60 + (line * LineHeight), 0, ShortName(spellId));
					line++;
				}
				if (i == LinesPerSide) {
					column = 195;
					line = 0;
				}
			}
		}

		private static string FormatAmount(int count) {
			return (count > 999) ? (count / 1000).ToString(CultureInfo.InvariantCulture) + "k" : count.ToString(CultureInfo.InvariantCulture);
		}

		internal static string ShortName(int spellId) {
			Constant shortName = Constant.GetByName("svitkovac_spell_shortcut[" + spellId.ToString(CultureInfo.InvariantCulture) + "]");
			if (shortName != null) {
				return (string) shortName.Value;
			}
			return SpellDef.GetById(spellId)?.Name ?? "";
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = (Player) gi.Cont;
			Item scrollCase = (Item) args[0];
			int page = (int) args[1];
			int button = (int) gr.PressedButton;
			if (button == 0) {
				player.Sound(85);
				return;
			}
			if (scrollCase.IsDeleted || !t_svitkovac.CanUse(player, scrollCase)) {
				return;
			}
			switch (button) {
				case AddScrolls:
					player.Target(SingletonScript<Targ_ScrollCase>.Instance, scrollCase);
					return;
				case TakeAll:
					t_svitkovac.TakeOneOfEach(player, scrollCase);
					return;
				case Circles1:
				case Circles3:
				case Circles5:
				case Circles7:
					page = (button - Circles1) * 16;
					break;
				case Necro:
					page = NecroPage;
					break;
				case Other:
					page = OtherPage;
					break;
				default:
					if (button <= (LinesPerSide * 2)) {
						player.Dialog(player, SingletonScript<D_ScrollCaseRemove>.Instance, new DialogArgs(scrollCase, page, page + button));
					}
					return;
			}
			player.Dialog(player, this, new DialogArgs(scrollCase, page));
		}
	}

	public class D_ScrollCaseRemove : CompiledGumpDef {
		private const int RemoveAll = 1, RemoveCount = 2, CountEntry = 3;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Item scrollCase = (Item) args[0];
			int spellId = (int) args[2];
			ScrollCaseLoc texts = Loc<ScrollCaseLoc>.Get(((Player) sendTo).Language);
			this.SetLocation(100, 100);
			this.AddBackground(0, 0, 340, 180, 0x9D8);
			this.AddPage(0);
			this.AddLabel(90, 30, 1152, D_ScrollCase.ShortName(spellId) + " ( " + t_svitkovac.CountIn(scrollCase, spellId).ToString(CultureInfo.InvariantCulture) + " )");
			this.AddLabel(90, 50, 995, texts.HowMany);
			this.AddButton(140, 120, 2103, 2104, RemoveAll, GumpButtonType.Reply, 0);
			this.AddLabel(160, 120, 995, texts.AllOfThem);
			this.AddImage(30, 80, 1143);
			if (spellId <= 64) {
				this.AddImage(35, 35, 0x8BF + spellId);
			}
			this.AddTextEntry(40, 83, 250, 20, 32, CountEntry, "1");
			this.AddButton(40, 120, 249, 248, RemoveCount, GumpButtonType.Reply, 0);
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = (Player) gi.Cont;
			Item scrollCase = (Item) args[0];
			int page = (int) args[1];
			int spellId = (int) args[2];
			if (scrollCase.IsDeleted) {
				return;
			}
			if ((gr.PressedButton != 0) && t_svitkovac.CanUse(player, scrollCase)) {
				if (gr.PressedButton == RemoveAll) {
					t_svitkovac.TakeOut(player, scrollCase, spellId, t_svitkovac.CountIn(scrollCase, spellId));
				} else if (gr.PressedButton == RemoveCount) {
					int.TryParse(gr.GetTextResponse(CountEntry), NumberStyles.Integer, CultureInfo.InvariantCulture, out int wanted);
					t_svitkovac.TakeOut(player, scrollCase, spellId, wanted);
				}
			}
			player.Dialog(player, SingletonScript<D_ScrollCase>.Instance, new DialogArgs(scrollCase, page));
		}
	}

	internal class ScrollCaseLoc : CompiledLocStringCollection<ScrollCaseLoc> {
		public string CantReach = "Nedosahnes na svitkovac.";
		public string OnlyScrolls = "Muzes vkladat pouze svitky.";
		public string NotAScroll = "Toto neni svitek.";
		public string MustCarry = "{0} musis mit u sebe.";
		public string AimNext = "Uloz dalsi svitek";
		public string Full = "Nemuzes dat vic {0} do svitkovace. Svitkovac je plny.";
		public string SomeMale = "Do svitkovace jsi vlozil pouze {0} {1}. Pro zbytek tam neni misto.";
		public string SomeFemale = "Do svitkovace jsi vlozila pouze {0} {1}. Pro zbytek tam neni misto.";
		public string AddedMale = "Vlozil jsi do svitkovace {0} {1}.";
		public string AddedFemale = "Vlozila jsi do svitkovace {0} {1}.";
		public string TookMale = "Vybral jsi ze svitkovace {0} {1}.";
		public string TookFemale = "Vybrala jsi ze svitkovace {0} {1}.";
		public string WantNumber = "Cislo musi byt vetsi nez 0.";
		public string Necro = "Nekro";
		public string Other = "Jine";
		public string Add = "Pridat";
		public string Take = "Vybrat";
		public string Circle = "{0}. Kruh";
		public string HowMany = "Kolik svitku odebrat?";
		public string AllOfThem = "Vsechny (max 60k)";
	}
}
