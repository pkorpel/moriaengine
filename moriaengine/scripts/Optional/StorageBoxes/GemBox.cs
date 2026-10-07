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
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public partial class GemBox : Item {

		public void EnsureDictionary() {
			if (this.inBoxGems == null) {
				this.inBoxGems = new Dictionary<ItemDef, int>();
			}
		}


		public override void On_DClick(AbstractCharacter ac) {
			this.Dialog(ac, SingletonScript<D_GemBox>.Instance);
		}

		public override void On_Create() {
			this.Color = 2448;
		}

	}

	[ViewableClass]
	public partial class GemBoxDef {
	}
}

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>Surprisingly the dialog that will display the GemBox guts</summary>
	public class D_GemBox : CompiledGumpDef {

		private static readonly TagKey buttonsForGemsTK = TagKey.Acquire("_rb_ButtonsForGems_");
		private static readonly TagKey buttonsCountTK = TagKey.Acquire("_rb_ButtonsCount_");

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			int i;
			Dictionary<int, ItemDef> dictButtonForGems = new Dictionary<int, ItemDef>();
			int buttonsCount = 0;
			int radku = 0;
			GemBox box = (GemBox) focus;
			if (box.inBoxGems == null) {
				radku = 0;
				i = 0;
			} else {
				i = box.inBoxGems.Count;
				radku = (i - 1) / 4;
			}
			int baseX = 20;
			int baseY = 60;
			this.SetLocation(70, 25);
			this.ResizePic(0, 0, 5054, 660, 165 + radku * 80);
			this.ResizePic(10, 10, 3000, 640, 145 + radku * 80);
			this.Button(15, 25, 4005, 4007, true, 0, 1);		// add gems
			this.Button(620, 10, 4017, 4019, true, 0, 0);	// close dialog
			this.HtmlGumpA(255, 15, 200, 20, "Bedýnka na drahé kameny", false, false);
			this.HtmlGumpA(55, 27, 100, 20, "Pøidat kameny", false, false);
			if ((radku == 0) && (i == 0)) {
				this.HtmlGumpA(baseX, 75, 200, 20, "Bedna na drahé kameny je prázdná", false, false);
			} else {
				i = 0;
				foreach (KeyValuePair<ItemDef, int> pair in box.inBoxGems) {
					this.Button(baseX, baseY, 4017, 4019, true, 0, 1000 + buttonsCount);
					this.HtmlGumpA(baseX + 35, baseY, 110, 20, pair.Key.Name, false, false);
					this.HtmlGumpA(baseX + 35, baseY + 20, 100, 20, "Pocet:", false, false);
					this.HtmlGumpA(baseX + 75, baseY + 20, 100, 20, Convert.ToString(pair.Value), false, false);
					this.CheckBox(baseX, baseY + 38, 9903, 9904, false, buttonsCount);
					this.HtmlGumpA(baseX + 35, baseY + 38, 50, 20, "Vyndat:", false, false);
					this.NumberEntryA(baseX + 80, baseY + 38, 65, 20, 0, buttonsCount, 0);
					this.TilePic(baseX + 110, baseY, pair.Key.Model);
					dictButtonForGems.Add(buttonsCount, pair.Key);
					i++;
					buttonsCount++;
					if (i < 4) {
						baseX += 157;
					} else {
						baseX = 20;
						baseY += 80;
						i = 0;
					}
				}
			}
			args.SetTag(buttonsCountTK, buttonsCount);
			args.SetTag(buttonsForGemsTK, dictButtonForGems);
			this.Button(20, 125 + radku * 80, 4023, 4025, true, 0, 2);		// OK
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			GemBox box = (GemBox) gi.Focus;
			if (!((Player) gi.Cont).CanPickUpWithMessage(box)) {
				return;
			}
			if (gr.PressedButton == 0) {			// cancel
			} else if (gr.PressedButton == 1) {		// Add gems
				((Player) gi.Cont).Target(SingletonScript<Targ_GemBox>.Instance, gi.Focus);
			} else if (gr.PressedButton == 2) {		// OK -> give selected gems
				Dictionary<int, ItemDef> buttonShowItemDef = (Dictionary<int, ItemDef>) args.GetTag(buttonsForGemsTK);
				int buttonsCount = TagMath.IGetTag(args, buttonsCountTK);
				int i = 0;
				int gemsToGive = 0;
				while (i < buttonsCount) {
					int desiredCount = (int) gr.GetNumberResponse(i);
					if ((gr.IsSwitched(i)) && (desiredCount > 0)) {	// player wants to take at least one gem
						if (box.inBoxGems[buttonShowItemDef[i]] < desiredCount) {
							((Player) gi.Cont).RedMessage("Snažíš se vyndat pøíliš mnoho gemù: " + buttonShowItemDef[i].Name + ". Vyndáváš plný poèet.");
							gemsToGive = box.inBoxGems[buttonShowItemDef[i]];
						} else {
							gemsToGive = desiredCount;
						}
						buttonShowItemDef[i].Create(((Player) gi.Cont).Backpack);
						Globals.LastNewItem.Amount = gemsToGive;
						gi.Cont.SysMessage("Vyndáváš z bedny " + Convert.ToString(gemsToGive) + "ks " + buttonShowItemDef[i].Name + ".");
						box.inBoxGems[buttonShowItemDef[i]] -= gemsToGive;
						box.pocetGemu -= gemsToGive;
						if (box.inBoxGems[buttonShowItemDef[i]] == 0) {
							box.inBoxGems.Remove(buttonShowItemDef[i]);
						}
					}
					i++;
				}
			} else if (gr.PressedButton >= 1000) {
				int thisButtonValue = gr.PressedButton - 1000;
				Dictionary<int, ItemDef> buttonShowItemDef = (Dictionary<int, ItemDef>) args.GetTag(buttonsForGemsTK);
				buttonShowItemDef[thisButtonValue].Create(((Player) gi.Cont).Backpack);
				Globals.LastNewItem.Amount = box.inBoxGems[buttonShowItemDef[thisButtonValue]];
				box.inBoxGems.Remove(buttonShowItemDef[thisButtonValue]);
				box.Dialog(gi.Cont, SingletonScript<D_GemBox>.Instance);
			}
		}
	}

	public class Targ_GemBox : CompiledTargetDef {

		protected override void On_Start(Player self, object parameter) {
			self.SysMessage("Zamìø drahý kámen, který chceš vložit do bedny.");
			base.On_Start(self, parameter);
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			GemBox gemBox = (GemBox) parameter;
			if (!self.CanReachWithMessage(gemBox)) {
				return TargetResult.Done;
			}

			if (self.CanPickUpWithMessage(targetted)) {
				if (targetted.Type == SingletonScript<t_gem>.Instance) {
					int previousCount;
					gemBox.EnsureDictionary();
					if (!gemBox.inBoxGems.TryGetValue(targetted.TypeDef, out previousCount)) {
						previousCount = 0;
					}

					if (gemBox.pocetGemu + targetted.Amount > gemBox.TypeDef.Capacity) {	// poresime prekroceni nosnosti bedny -> do bedny se prida jen tolik gemu, kolik skutecne lze pridat
						int gemsToTake = gemBox.TypeDef.Capacity - gemBox.pocetGemu;
						targetted.Amount -= gemsToTake;
						gemBox.pocetGemu += gemsToTake;
						gemBox.inBoxGems[targetted.TypeDef] = previousCount + gemsToTake;
					} else {
						gemBox.pocetGemu += targetted.Amount;
						gemBox.inBoxGems[targetted.TypeDef] = previousCount + targetted.Amount;
						targetted.Delete();
					}
				} else {
					self.SysMessage("Do bedny mùžeš pøidat jen drahé kameny.");
				}
			}
			return TargetResult.RestartTargetting;
		}

		protected override void On_TargonCancel(Player self, object parameter) {
			GemBox focus = parameter as GemBox;
			focus.Dialog(self, SingletonScript<D_GemBox>.Instance);
		}
	}
}