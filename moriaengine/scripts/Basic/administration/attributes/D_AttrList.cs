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

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>Dedicated GM dialog listing every Sphere-style item ATTR flag (#215/#217) as a
	/// checkbox, modeled on the existing D_TagList/D_PluginList dialogs (same "separate dialog
	/// reachable from .info, plus its own direct command" shape, per explicit user request -
	/// not a handful of scattered boolean fields mixed into the generic .info field list).</summary>
	public class D_AttrList : CompiledGumpDef {
		internal static readonly TagKey holderTK = TagKey.Acquire("_attr_holder_");

		//every real flag except "none" itself - order matches declaration order in ItemAttributes
		private static readonly ItemAttributes[] allFlags = {
			ItemAttributes.attr_identified,
			ItemAttributes.attr_decay,
			ItemAttributes.attr_newbie,
			ItemAttributes.attr_move_always,
			ItemAttributes.attr_move_never,
			ItemAttributes.attr_magic,
			ItemAttributes.attr_owned,
			ItemAttributes.attr_invis,
			ItemAttributes.attr_cursed,
			ItemAttributes.attr_blessed,
			ItemAttributes.attr_static,
			ItemAttributes.attr_exceptional,
			ItemAttributes.attr_enchanted,
			ItemAttributes.attr_imbued,
			ItemAttributes.attr_questitem,
			ItemAttributes.attr_insured,
			ItemAttributes.attr_nodroptrade,
			ItemAttributes.attr_norepair,
			ItemAttributes.attr_lockeddown,
			ItemAttributes.attr_secure,
			ItemAttributes.attr_reforged,
			ItemAttributes.attr_opened,
		};

		private static int width = 300;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Item item = (Item) args.GetTag(holderTK);

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			dlg.CreateBackground(width);
			dlg.SetLocation(50, 50);

			//headline
			dlg.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Attributes - " + item.Name).Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();
			dlg.MakeLastTableTransparent();

			//one row per flag - checkbox (id 10+i) + its real Sphere name as the label
			dlg.AddTable(new GUTATable(allFlags.Length, ButtonMetrics.D_BUTTON_WIDTH, 0));
			for (int i = 0; i < allFlags.Length; i++) {
				ItemAttributes flag = allFlags[i];
				dlg.LastTable[i, 0] = GUTACheckBox.Builder.Id(10 + i).Checked(item.HasAttribute(flag)).Build();
				dlg.LastTable[i, 1] = GUTAText.Builder.TextLabel(flag.ToString()).Build();
			}
			dlg.MakeLastTableTransparent();

			//confirm button
			dlg.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH, 0));
			dlg.LastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonOK).Id(1).Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.TextLabel("Confirm").Build();
			dlg.MakeLastTableTransparent();

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if (gr.PressedButton == 0) { //exit without saving
				DialogStacking.ShowPreviousDialog(gi);
				return;
			}

			//button 1 (confirm) - every checkbox's current state becomes the item's new Attributes
			Item item = (Item) args.GetTag(holderTK);
			for (int i = 0; i < allFlags.Length; i++) {
				item.SetAttribute(allFlags[i], gr.IsSwitched(10 + i));
			}
			DialogStacking.ShowPreviousDialog(gi);
		}

		/// <summary>Opens the attribute checkbox dialog directly for the given item - shared by
		/// both entry points below (the ".AttrList" target-cursor command, and the "Attributes"
		/// button wired into the regular .info dialog via ItemDescriptor).</summary>
		public static void ShowFor(Player viewer, Item item) {
			DialogArgs newArgs = new DialogArgs();
			newArgs.SetTag(holderTK, item);
			viewer.Dialog(SingletonScript<D_AttrList>.Instance, newArgs);
		}

		/// <summary>GM-only item-attribute checkbox list via target cursor (#217) - matches the
		/// existing ".flip"/".kill" pattern (CompiledTargetDef), unlike ".TagList"'s own "self"
		/// signature, since an item has no "self" context of its own to invoke a command as -
		/// a GM always has to target one externally.</summary>
		[SteamFunction]
		public static void AttrList(Player self) {
			self.SysMessage("Target the item to view/edit its attributes.");
			self.Target(SingletonScript<Targ_AttrList>.Instance);
		}

		public class Targ_AttrList : CompiledTargetDef {
			protected override TargetResult On_TargonThing(Player self, Thing targetted, object parameter) {
				Item item = targetted as Item;
				if (item == null) {
					self.SysMessage("That is not an item.");
					return TargetResult.Done;
				}
				ShowFor(self, item);
				return TargetResult.Done;
			}
		}
	}
}
