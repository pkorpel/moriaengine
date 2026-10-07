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

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>Dialog listing all object(pluginholder)'s plugins</summary>
	public class D_PluginList : CompiledGumpDef {
		internal static readonly TagKey holderTK = TagKey.Acquire("_plugin_holder_");
		internal static readonly TagKey pluginListTK = TagKey.Acquire("_plugin_list_");
		internal static readonly TagKey pluginCriteriumTK = TagKey.Acquire("_plugin_criterium_");

		private static int width = 700;
		private static int innerWidth = width - 2 * ImprovedDialog.D_BORDER - 2 * ImprovedDialog.D_SPACE;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			//vzit seznam plugin z pluginholdera (char nebo item) prisleho v parametru dialogu
			PluginHolder ph = (PluginHolder) args.GetTag(holderTK); //z koho budeme pluginy brat?
			List<KeyValuePair<PluginKey, Plugin>> pluginList = args.GetTag(pluginListTK) as List<KeyValuePair<PluginKey, Plugin>>;
			if (pluginList == null) {
				//vzit seznam plugin dle vyhledavaciho kriteria
				//toto se provede jen pri prvnim zobrazeni nebo zmene kriteria!
				pluginList = this.ListifyPlugins(ph.GetAllPluginsWithKeys(), TagMath.SGetTag(args, pluginCriteriumTK));
				pluginList.Sort(PluginsComparer.instance); //setridit podle abecedy
				args.SetTag(pluginListTK, pluginList); //ulozime to do argumentu dialogu				
			}
			int firstiVal = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);//prvni index na strance
			int imax = Math.Min(firstiVal + ImprovedDialog.PAGE_ROWS, pluginList.Count);

			ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
			//pozadi    
			dlg.CreateBackground(width);
			dlg.SetLocation(50, 50);

			//nadpis
			dlg.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Seznam všech plugin na " + ph + " (zobrazeno " + (firstiVal + 1) + "-" + imax + " z " + pluginList.Count + ")").Build();
			dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();//cudlik na zavreni dialogu
			dlg.MakeLastTableTransparent();

			//cudlik a input field na zuzeni vyberu
			dlg.AddTable(new GUTATable(1, 130, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Vyhledávací kriterium").Build();
			dlg.LastTable[0, 1] = GUTAInput.Builder.Id(33).Build();
			dlg.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(1).Build();
			dlg.MakeLastTableTransparent();

			//popis sloupcu
			dlg.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH, 200, 0, ButtonMetrics.D_BUTTON_WIDTH));
			dlg.LastTable[0, 0] = GUTAText.Builder.TextLabel("Smaž").Build();
			dlg.LastTable[0, 1] = GUTAText.Builder.TextLabel("Jméno pluginy").Build();
			dlg.LastTable[0, 2] = GUTAText.Builder.TextLabel("Defname").Build();
			dlg.LastTable[0, 3] = GUTAText.Builder.TextLabel("Info").Build();
			dlg.MakeLastTableTransparent();

			//seznam tagu
			dlg.AddTable(new GUTATable(imax - firstiVal));
			dlg.CopyColsFromLastTable();

			//projet seznam v ramci daneho rozsahu indexu
			int rowCntr = 0;
			for (int i = firstiVal; i < imax; i++) {
				KeyValuePair<PluginKey, Plugin> de = pluginList[i];

				dlg.LastTable[rowCntr, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(10 + (2 * i)).Build();
				dlg.LastTable[rowCntr, 1] = GUTAText.Builder.Text(de.Key.Name).Build();
				//plugin defname
				Plugin pl = de.Value;
				dlg.LastTable[rowCntr, 2] = GUTAText.Builder.Text(pl.Def.Defname).Build();
				//odkaz do infodialogu
				dlg.LastTable[rowCntr, 3] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonPaper).Id(11 + (2 * i)).Build();
				rowCntr++;
			}
			dlg.MakeLastTableTransparent(); //zpruhledni zbytek dialogu

			//ted paging
			dlg.CreatePaging(pluginList.Count, firstiVal, 1);

			dlg.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			//seznam plugin bereme z parametru (mohl byt jiz trideny atd, nebudeme ho proto selectit znova)
			List<KeyValuePair<PluginKey, Plugin>> pluginList = (List<KeyValuePair<PluginKey, Plugin>>) args.GetTag(pluginListTK);
			int firstOnPage = TagMath.IGetTag(args, ImprovedDialog.pagingIndexTK);
			int imax = Math.Min(firstOnPage + ImprovedDialog.PAGE_ROWS, pluginList.Count);
			if (gr.PressedButton < 10) { //ovladaci tlacitka (exit, new, vyhledej)				
				switch (gr.PressedButton) {
					case 0: //exit
						DialogStacking.ShowPreviousDialog(gi); //zobrazit pripadny predchozi dialog
						break;
					case 1: //vyhledat dle zadani
						string nameCriteria = gr.GetTextResponse(33);
						args.RemoveTag(ImprovedDialog.pagingIndexTK);//zrusit info o prvnich indexech - seznam se cely zmeni tim kriteriem						
						args.SetTag(pluginCriteriumTK, nameCriteria);
						args.RemoveTag(pluginListTK);//vycistit soucasny odkaz na pluginlist aby se mohl prenacist
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 2: //zobrazit info o vysvetlivkach
						Gump newGi = gi.Cont.Dialog(SingletonScript<D_Settings_Help>.Instance);
						DialogStacking.EnstackDialog(gi, newGi);
						break;
				}
			} else if (ImprovedDialog.PagingButtonsHandled(gi, gr, pluginList.Count, 1)) {//kliknuto na paging?
			} else {
				//zjistime si radek
				int row = (gr.PressedButton - 10) / 2;
				int buttNo = (gr.PressedButton - 10) % 2;
				PluginHolder pluginOwner = (PluginHolder) args.GetTag(holderTK); //z koho budeme pluginu brat?
				KeyValuePair<PluginKey, Plugin> de = pluginList[row];
				switch (buttNo) {
					case 0: //smazat						
						pluginOwner.RemovePlugin(de.Key);
						args.RemoveTag(pluginListTK);//na zaver smazat pluginlist (musi se reloadnout)
						DialogStacking.ResendAndRestackDialog(gi);
						break;
					case 1: //info
						Gump newGi = gi.Cont.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(de.Value));
						DialogStacking.EnstackDialog(gi, newGi);
						break;
				}
			}
		}

		/// <summary>Retreives the list of all plugins the given PluginHolder has</summary>
		private List<KeyValuePair<PluginKey, Plugin>> ListifyPlugins(IEnumerable<KeyValuePair<PluginKey, Plugin>> plugins, string criteria) {
			List<KeyValuePair<PluginKey, Plugin>> pluginsList = new List<KeyValuePair<PluginKey, Plugin>>();
			foreach (KeyValuePair<PluginKey, Plugin> entry in plugins) {
				//entry in this hashtable is TagKey and its object value
				if (criteria == null || criteria.Equals("")) {
					pluginsList.Add(entry);//bereme vse
				} else if (entry.Key.Name.ToUpper().Contains(criteria.ToUpper())) {
					pluginsList.Add(entry);//jinak jen v pripade ze kriterium se vyskytuje v nazvu tagu
				}
			}
			return pluginsList;
		}

		/// <summary>
		/// Display a plugin list. Function accessible from the game.
		/// The function is designed to be triggered using .x PluginList(criteria)"
		/// but it can be used also normally .PluginList(criteria) to display runner's own plugins
		/// </summary>
		[SteamFunction]
		public static void PluginList(PluginHolder self, ScriptArgs text) {
			//zavolat dialog, 
			//parametr self - thing jehoz tagy chceme zobrazit
			//0 - zacneme od prvni pluginy co ma
			DialogArgs newArgs = new DialogArgs();
			newArgs.SetTag(holderTK, self); //na sobe budeme zobrazovat tagy
			if (text == null || text.Argv == null || text.Argv.Length == 0) {
				Globals.SrcCharacter.Dialog(SingletonScript<D_PluginList>.Instance, newArgs);
			} else {
				newArgs.SetTag(D_TagList.tagCriteriumTK, text.Args);//vyhl. kriterium
				Globals.SrcCharacter.Dialog(SingletonScript<D_PluginList>.Instance, newArgs);
			}
		}
	}

	/// <summary>Comparer for sorting tag dictionary entries by tags name asc</summary>
	public class PluginsComparer : IComparer<KeyValuePair<PluginKey, Plugin>> {
		public static readonly PluginsComparer instance = new PluginsComparer();

		private PluginsComparer() {
			//soukromy konstruktor, pristupovat budeme pres instanci
		}

		//we have to make sure that we are sorting a list of DictionaryEntries which are tags
		//otherwise this will crash on some ClassCastException -)
		public int Compare(KeyValuePair<PluginKey, Plugin> x, KeyValuePair<PluginKey, Plugin> y) {
			PluginKey a = x.Key;
			PluginKey b = y.Key;
			return StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name);
		}
	}
}