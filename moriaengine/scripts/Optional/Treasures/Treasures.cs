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
using SteamEngine.Persistence;

/* TO DO !:
 * ? Exceptions in getters of TreasureChest's private fields ? // dunno whether it should be there or not
 * System.Collections.ObjectModel.ReadOnlyCollection for Spawn&ItemEntry - I guess I'll pend this one... at least for now
 * adding Gold -> goldBags and goldBoxes are needed to be done first...
 * Clean unnecessary functions ... are those functions in english ? or is there a word like 'methode' or sth ... grh..
 * Add lockpicking
 * ?Add chest hiding? and probably re-seting after timer (I'll think this over, there may be another aproach..)
 * Add monster spawning and guarding system
 */


namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public partial class TreasureChest : Container {
		private static CharacterDef defaultTreasureSpawn;
		private static ItemDef defaultTreasureItem;

		public CharacterDef DefaultTreasureSpawn {
			get {
				if (defaultTreasureSpawn == null) {
					defaultTreasureSpawn = (CharacterDef) ThingDef.GetByDefname("c_ostard_zostrich");
				}
				return defaultTreasureSpawn;
			}
		}

		public ItemDef DefaultTreasureItem {
			get {
				if (defaultTreasureItem == null) {
					defaultTreasureItem = (ItemDef) ThingDef.GetByDefname("i_bag");
				}
				return defaultTreasureItem;
			}
		}

		public override void On_DClick(AbstractCharacter ac) {
			Player p = ac as Player;
			if (p.IsGM) {
				this.Dialog(ac, SingletonScript<D_TreasureChest>.Instance);
			} else {
				p.SysMessage("otvirame poklad");
				this.EnsureListItemEntry();
				this.EnsureListSpawnEntry();
				if (this.GetLastopenTimeDifference() /this.cycleTime >= 1) {	//it's time to generate another treasure;
					/*if (isLocked) { // some isLocked condition and locked container condidito and blah blah ...
					 * ac.SysMessage("");
					}
					if (!wasSpawned) {
					 * ac.SysMessage("");
					 * SpawnMonsters();
					}*/
					int per;
					//removeGuts of the container so that in the treasure will be just those items I really want to..
					foreach (TreasureItemEntry tie in this.treasureItems) {
						if (tie.periodic > 0) {
							per = (int) (this.GetLastopenTimeDifference() /this.cycleTime) * tie.periodic;
							p.SysMessage("Periode is counted as: " + per);

						} else {
							per = 1;
						}
						for (int i = 0; i < tie.amount; i++) {
							for (int j = 0; j < per; j++) {	//periodes
								if (tie.chance > 0) {	// zero chance equals 100% chance...
									if (!(tie.chance > Globals.dice.Next(0, 99))) {	//chance failed
										continue;
									}
								}
								tie.itemID.Create(this);
							}
						}
					}
					this.SetLastopen();	// we set the lastOpen field at the very time in which we generate the treasure.
					this.OpenTo(ac);
				} else {
					this.OpenTo(ac);	//Too early, there is no reward to give yet.
				}
			}
		}

		public override void On_Create() {
			this.SetLastopen();
		}

		public void SetLastopen() {
			this.lastOpen = (long) Globals.TimeInSeconds;
		}

		/// <summary>Assigns a time in seconds to the lastOpen attribute representing the serverTime 'secBack' seconds before.</summary>
		public void SetLastopen(int secBack) {
			this.lastOpen = (long) Globals.TimeInSeconds - secBack;
		}

		/// <summary>Returns a time in seconds from the last treasure opening.</summary>
		public long GetLastopenTimeDifference() {
			return (long) Globals.TimeInSeconds - this.lastOpen;
		}

		/// <summary>Returns a string containing easy to read info on how long it is from the last treasure opening.</summary>
		public string GetLastopenTimeDifference(bool isTimeString)
		{
			if (isTimeString) {
				long d, h, m, s;
				s = this.GetLastopenTimeDifference();
				d = s / 86400;
				s -= d * 86400;
				h = s / 3600;
				s -= h * 3600;
				m = s / 60;
				s -= m * 60;
				return d + "d " + h + "h " + m + "min " + s + "s";
			}
			return Convert.ToString(Globals.TimeInSeconds - this.lastOpen);
		}

		public List<TreasureItemEntry> TreasureItems {
			get {
				this.EnsureListItemEntry();
				return this.treasureItems;
			}
		}

		public List<TreasureSpawnEntry> TreasureSpawns {
			get {
				this.EnsureListSpawnEntry();
				return this.treasureSpawns;
			}
		}

		public int MoneyCoefficient {
			get {
				return this.moneyCoefficient;
			}
			set {
				if (value > 0) {
					this.moneyCoefficient = value;
				}
			}
		}

		public int Check {
			get {
				return this.check;
			}
			set {
				if (value > 0) {
					this.check = value;
				}
			}
		}

		public int CycleTime {
			get {
				return this.cycleTime;
			}
			set {
				if (value > 0) {
				}
			}
		}

		public int Lockpick {
			get {
				return this.lockpick;
			}
			set {
				if (value > 0) {
					this.lockpick = value;
				}
			}
		}

		public void EnsureListItemEntry() {
			if (this.treasureItems == null) {
				this.treasureItems = new List<TreasureItemEntry>();
			}
		}

		public void EnsureListSpawnEntry() {
			if (this.treasureSpawns == null) {
				this.treasureSpawns = new List<TreasureSpawnEntry>();
			}
		}

		/// <summary>Adds new item into TreasureItem List</summary>
		public void AddTreasureItem(ItemDef item, int amount, int chance, int periodic) {
			this.EnsureListItemEntry();
			TreasureItemEntry newItem = new TreasureItemEntry();
			newItem.itemID = item;
			newItem.amount = amount;
			newItem.chance = chance;
			newItem.periodic = periodic;
			this.treasureItems.Add(newItem);
		}

		/// <summary>Overwrites attributes of one paticular TreasureItemEntry in the list of treasureItems</summary>
		public void OverwriteTreasureItem(int itemIndex, ItemDef item, int amount, int chance, int periodic) {
			if (this.treasureItems.Count <= itemIndex) {
				// chyba !
				return;
			}
			this.treasureItems[itemIndex].itemID = item;
			this.treasureItems[itemIndex].amount = amount;
			this.treasureItems[itemIndex].chance = chance;
			this.treasureItems[itemIndex].periodic = periodic;
		}

		/// <summary>Removes an item from the list of treasureItems</summary>
		public void RemoveTreasureItem(int itemIndex) {
			if (this.treasureItems.Count <= itemIndex) {
				// chyba !
				return;
			}
			this.treasureItems.RemoveAt(itemIndex);
		}

		/// <summary>Adds new item into treasureSpawns List</summary>
		public void AddTreasureSpawn(CharacterDef charDef, int amount) {
			this.EnsureListSpawnEntry();
			TreasureSpawnEntry newSpawn = new TreasureSpawnEntry();
			newSpawn.charDef = charDef;
			newSpawn.amount = amount;
			this.treasureSpawns.Add(newSpawn);
		}

		/// <summary>Overwrites attributes of one paticular TreasureSpawnEntry in the list of treasureSpawns</summary>
		public void OverwriteTreasureSpawn(int spawnIndex, CharacterDef charDef, int amount) {
			if (this.treasureSpawns.Count <= spawnIndex) {
				// chyba !
				return;
			}
			this.treasureSpawns[spawnIndex].charDef = charDef;
			this.treasureSpawns[spawnIndex].amount = amount;
		}

		/// <summary>Removes an item from the list of treasureSpawns</summary>
		public void RemoveTreasureSpawn(int spawnIndex) {
			if (this.treasureSpawns.Count <= spawnIndex) {
				// chyba !
				return;
			}
			this.treasureSpawns.RemoveAt(spawnIndex);
		}
	}

	[ViewableClass]
	public partial class TreasureChestDef {

	}

	[SaveableClass, DeepCopyableClass]
	[ViewableClass]
	public sealed class TreasureItemEntry {
		[SaveableData, CopyableData]
		public ItemDef itemID;

		[SaveableData, CopyableData]
		public int amount;

		[SaveableData, CopyableData]
		public int chance;

		[SaveableData, CopyableData]
		public int periodic;

		[LoadingInitializer, DeepCopyImplementation]
		public TreasureItemEntry() {
		}
	}

	[SaveableClass, DeepCopyableClass]
	[ViewableClass]
	public sealed class TreasureSpawnEntry {

		[SaveableData, CopyableData]
		public CharacterDef charDef;

		[SaveableData, CopyableData]
		public int amount;

		[LoadingInitializer, DeepCopyImplementation]
		public TreasureSpawnEntry() {
		}
	}
}

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>The dialog that will display the treasure menu</summary>
	public class D_TreasureChest : CompiledGumpDef {

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			ImprovedDialog dialogHandler = new ImprovedDialog(this.GumpInstance);
			TreasureChest treasure = focus as TreasureChest;
			treasure.EnsureListItemEntry();
			treasure.EnsureListSpawnEntry();
			dialogHandler.CreateBackground(300);
			dialogHandler.SetLocation(70, 50);

			// Headline
			dialogHandler.AddTable(new GUTATable(1, 0));
			dialogHandler.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Menu Pokladu").Build();
			dialogHandler.MakeLastTableTransparent();

			// First table
			dialogHandler.LastTable.RowHeight = ButtonMetrics.D_BUTTON_HEIGHT;
			dialogHandler.AddTable(new GUTATable(6, 100, 0, ButtonMetrics.D_BUTTON_WIDTH));

			dialogHandler.LastTable[0, 0] = GUTAText.Builder.TextLabel("Peníze (exp):").Build();
			dialogHandler.LastTable[0, 1] = GUTAInput.Builder.Type(LeafComponentTypes.InputNumber).Id(1).Text(Convert.ToString(treasure.MoneyCoefficient)).Build();
			dialogHandler.LastTable[0, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSend).Id(10).Build(); // goto treasuremoneyCoefficientHlp gump

			dialogHandler.LastTable[1, 0] = GUTAText.Builder.TextLabel("Šek:").Build();
			dialogHandler.LastTable[1, 1] = GUTAInput.Builder.Type(LeafComponentTypes.InputNumber).Id(2).Text(Convert.ToString(treasure.Check)).Build();
			dialogHandler.LastTable[1, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSend).Id(11).Build(); // goto treasureCheckHlp gump

			dialogHandler.LastTable[2, 0] = GUTAText.Builder.TextLabel("Perioda:").Build();
			dialogHandler.LastTable[2, 1] = GUTAInput.Builder.Type(LeafComponentTypes.InputNumber).Id(3).Text(Convert.ToString(treasure.CycleTime)).Build();
			dialogHandler.LastTable[2, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSend).Id(12).Build(); // goto treasureperiodicHlp gump

			dialogHandler.LastTable[3, 0] = GUTAText.Builder.TextLabel("Lockpick:").Build();
			dialogHandler.LastTable[3, 1] = GUTAInput.Builder.Type(LeafComponentTypes.InputNumber).Id(4).Text(Convert.ToString(treasure.Lockpick)).Build();
			dialogHandler.LastTable[3, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSend).Id(13).Build(); // goto treasureLockpickHlp gump

			dialogHandler.LastTable[4, 0] = GUTAText.Builder.TextLabel("LastOpened:").Build();
			dialogHandler.LastTable[4, 1] = GUTAInput.Builder.Type(LeafComponentTypes.InputNumber).Id(5).Text(Convert.ToString(treasure.GetLastopenTimeDifference())).Build();
			dialogHandler.LastTable[4, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonSend).Id(14).Build(); // goto treasureLastOpenHlp gump

			dialogHandler.LastTable[5, 1] = GUTAText.Builder.TextLabel(treasure.GetLastopenTimeDifference(true)).Build();
			dialogHandler.MakeLastTableTransparent();

			// Second table
			// Head
			dialogHandler.AddTable(new GUTATable(1, ButtonMetrics.D_BUTTON_WIDTH + 100, 0));
			dialogHandler.LastTable[0, 1] = GUTAText.Builder.TextLabel(" pocet itemu:").Build();
			// Body
			dialogHandler.AddTable(new GUTATable(2, ButtonMetrics.D_BUTTON_WIDTH, 100, 0));
			dialogHandler.LastTable[0, 0] = GUTAButton.Builder.Id(2).Build(); // goto treasureBounty gump
			dialogHandler.LastTable[0, 1] = GUTAText.Builder.TextLabel("Poklad").Build();
			dialogHandler.LastTable[0, 2] = GUTAText.Builder.Text(" " + Convert.ToString(treasure.TreasureItems.Count)).Build();
			dialogHandler.LastTable[1, 0] = GUTAButton.Builder.Id(3).Build(); // goto treasureSpawns gump
			dialogHandler.LastTable[1, 1] = GUTAText.Builder.TextLabel("Spawny").Build();
			dialogHandler.LastTable[1, 2] = GUTAText.Builder.Text(" " + Convert.ToString(treasure.TreasureSpawns.Count)).Build();

			dialogHandler.MakeLastTableTransparent();

			dialogHandler.AddTable(new GUTATable(1, 0));
			dialogHandler.LastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonOK).Id(1).Build(); //OK
			dialogHandler.LastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).XPos(ButtonMetrics.D_BUTTON_WIDTH).Id(0).Build(); // Cancel

			//finish creating
			dialogHandler.WriteOut();

		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			TreasureChest treasure = (TreasureChest) gi.Focus;
			Player p = gi.Cont as Player;
			int button = gr.PressedButton;
			switch (gr.PressedButton) {
				case 0:	// cancel
					p.SysMessage("Nastaveni zustava nezmeneno.");
					return;
				case 1:	// OK
					if (gr.GetNumberResponse(1) > 0) {
						treasure.MoneyCoefficient = (int) gr.GetNumberResponse(1);
					} else {
						p.RedMessage("Hodnota moneyCoefficient musí být kladná !");
					}
					if (gr.GetNumberResponse(2) >= 0) {
						treasure.Check = (int) gr.GetNumberResponse(2);
					} else {
						p.RedMessage("Hodnota Check musí být kladná !");
					}
					if (gr.GetNumberResponse(3) > 0) {
						treasure.CycleTime = (int) gr.GetNumberResponse(3);
					} else {
						p.RedMessage("Hodnota Perioda musí být kladná !");
					}
					if (gr.GetNumberResponse(4) >= 0) {
						treasure.Lockpick = (int) gr.GetNumberResponse(4);
					} else {
						p.RedMessage("Hodnota lockpick musí být kladná !");
					}
					if (gr.GetNumberResponse(5) >= 0) {
						treasure.SetLastopen((int) gr.GetNumberResponse(5));
					} else {
						p.RedMessage("Hodnota lastOpened musí být kladná !");
					}
					return;

				// opening of setting Dialogs
				case 2:	// treasureBounty gump
					treasure.Dialog(p, SingletonScript<D_TreasureBounty>.Instance);
					p.SysMessage("Open treasureBounty gump...");
					return;
				case 3:	// treasureSpawns gump
					treasure.Dialog(p, SingletonScript<D_TreasureSpawns>.Instance);
					p.SysMessage("Open treasureSpawns gump...");
					return;

				// Help dialogy
				case 10:	//help moneyCoefficient
					p.Dialog(SingletonScript<D_Display_Text>.Instance, "Help - Peníze", "Promìnná 'Peníze' udává poèet penìz generovaných stáøím pokladu");
					break;
				case 11:	//help check
					p.Dialog(SingletonScript<D_Display_Text>.Instance, "Help - Šek", "Promìnná 'Šek' je konstantní poèet penìz, který bude pokaždé do pokladu vložen.<br>" +
																								"Nemá vliv na hodnotu 'moneyCoefficient'. Suma vypoèítaná z této složky se prostì pøiète k celkovému výnosu.");
					break;
				case 12:	//help perioda
					p.Dialog(SingletonScript<D_Display_Text>.Instance, "Help - Perioda", "Promìnná 'perioda' je èas v sekundách, po kterém se násobí periodic itemy. Èas posledního otevøení pokladu " +
																								"je uložen a po dalším otevøení hráèi se rozdíl èasù podìlí periodic konstantou. Výsledná hodnota udává poèet cyklù, " +
																								"které budou generovat itemy z pokladu s kladným attributem periodic.<br>" +
																								" vzorec pro periodic itemy:<br><\t> (lastOpened/Perioda)*itemPeriodic");
					break;
				case 13:	//help lockpick
					p.Dialog(SingletonScript<D_Display_Text>.Instance, "Help - Lockpick", "Promìnná 'lockpick' udává minimální potøebný skill pro odemèení pokladu. Nulová hodnota nechá poklad" +
																								"otevøený, bez nutnosti používat lockpicky.");
					break;
				case 14:	//help lastOpened
					p.Dialog(SingletonScript<D_Display_Text>.Instance, "Help - LastOpened", "Promìnná 'lastOpened' udává, pøed jakou dobou v sekundách byl naposledy poklad otevøen. " +
																									"Pøepoèet na dny je možné vidìt o øádek níž.");
					break;
				default:
					p.RedMessage("Nestandardní button ! Piš scripterùm!");
					break;
			}
			treasure.Dialog(p, this);
		}
	}


	/// <summary>The dialog that will display items generated it the trasure.</summary>
	public class D_TreasureBounty : CompiledGumpDef {

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			ImprovedDialog dialogHandler = new ImprovedDialog(this.GumpInstance);
			TreasureChest treasure = focus as TreasureChest;
			List<TreasureItemEntry> trItems = (focus as TreasureChest).TreasureItems;
			int rowCount = treasure.TreasureItems.Count;
			int i = 0;
			//TreasureChest treasure = focus as TreasureChest;
			dialogHandler.CreateBackground(500);
			dialogHandler.SetLocation(70, 50);

			// Headline
			dialogHandler.AddTable(new GUTATable(1, 0));
			dialogHandler.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Obsah Pokladu").Build();
			dialogHandler.MakeLastTableTransparent();

			// First row
			dialogHandler.LastTable.RowHeight = ButtonMetrics.D_BUTTON_HEIGHT;
			dialogHandler.AddTable(new GUTATable(1, 220, 70, 70, 80, ButtonMetrics.D_BUTTON_WIDTH));
			dialogHandler.LastTable[0, 0] = GUTAText.Builder.TextLabel(" Itemdef").Build();
			dialogHandler.LastTable[0, 1] = GUTAText.Builder.TextLabel(" Amount").Build();
			dialogHandler.LastTable[0, 2] = GUTAText.Builder.TextLabel(" Chance").Build();
			dialogHandler.LastTable[0, 3] = GUTAText.Builder.TextLabel(" Periodic").Build();

			// Second row
			dialogHandler.AddTable(new GUTATable(rowCount, 220, 70, 70, 80, ButtonMetrics.D_BUTTON_WIDTH));
			foreach (TreasureItemEntry tie in trItems) {
				dialogHandler.LastTable[i, 0] = GUTAInput.Builder.Id((i * 10) + 1).Text(tie.itemID.PrettyDefname).Build();
				dialogHandler.LastTable[i, 1] = GUTAInput.Builder.Type(LeafComponentTypes.InputNumber).Id((i * 10) + 2).Text(tie.amount.ToString()).Build();
				dialogHandler.LastTable[i, 2] = GUTAInput.Builder.Type(LeafComponentTypes.InputNumber).Id((i * 10) + 3).Text(tie.chance.ToString()).Build();
				dialogHandler.LastTable[i, 3] = GUTAInput.Builder.Type(LeafComponentTypes.InputNumber).Id((i * 10) + 4).Text(tie.periodic.ToString()).Build();
				dialogHandler.LastTable[i, 4] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(10 + i).Build();
				i++;
			}

			// last row
			dialogHandler.AddTable(new GUTATable(1, 360, 80, ButtonMetrics.D_BUTTON_WIDTH));
			dialogHandler.LastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonOK).Id(1).Build();	// OK
			dialogHandler.LastTable[0, 1] = GUTAText.Builder.TextLabel(" Add item").Build();
			dialogHandler.LastTable[0, 2] = GUTAButton.Builder.Id(2).Build();	// Add button

			dialogHandler.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player p = gi.Cont as Player;
			TreasureChest treasure = (TreasureChest) gi.Focus;
			switch (gr.PressedButton) {
				case 0:	//exit
					p.SysMessage("Nastavení nezmìnìno.");
					break;
				default:
					bool err = false;
					string thisDef;
					ItemDef thisItem;
					int ignore = -1;					// variable ignore has assigned a safe value, which garantees that all values will be modified
					if (gr.PressedButton > 2) {	// not OK or add button - i.e. removing button
						ignore = Convert.ToInt32(gr.PressedButton) - 10;	// item with this index will be removed anyway, so we'll skip it's modifications
					}
					for (int i = 0; i < treasure.TreasureItems.Count; i++) {
						if (i != ignore) {
							thisDef = gr.GetTextResponse(i * 10 + 1);
							thisItem = ThingDef.GetByDefname(thisDef) as ItemDef;
							if (thisItem == null) {
								p.RedMessage("'" + thisDef + "' neni platny defname!");
								err = true;
							} else if (gr.GetNumberResponse(i * 10 + 2) < 1) {
								p.RedMessage("amount pro '" + thisDef + "' musi byt kladny!");
								err = true;
							} else if (gr.GetNumberResponse(i * 10 + 3) < 1) {
								p.RedMessage("chance pro '" + thisDef + "' musi byt kladny!");
								err = true;
							} else if (gr.GetNumberResponse(i * 10 + 4) < 0) {
								p.RedMessage("periodic pro '" + thisDef + "' nesmi byt zaporny!");
								err = true;
							} else {
								treasure.OverwriteTreasureItem(i, thisItem, (int) gr.GetNumberResponse(i * 10 + 2), (int) gr.GetNumberResponse(i * 10 + 3), (int) gr.GetNumberResponse(i * 10 + 4));
							}
						}
					}
					if (gr.PressedButton > 2) { // not OK or add button
						treasure.RemoveTreasureItem(Convert.ToInt32(gr.PressedButton) - 10);	//Gets the value correspondent to the position of an item in the List of treasureItems
						treasure.Dialog(p, this);
						return;
					}
					if (gr.PressedButton == 2) { //add
						p.SysMessage("Pøidán defaultní item.");
						treasure.AddTreasureItem(treasure.DefaultTreasureItem, 1, 100, 0);
						treasure.Dialog(p, this);
						return;
					}
					if (err) {
						treasure.Dialog(p, this);
						return;
					}
					p.SysMessage("Potvrzeno zadání hodnot.");
					break;
			}
			treasure.Dialog(p, SingletonScript<D_TreasureChest>.Instance);
		}
	}


	/// <summary>The dialog that will display spawns guarding the trasure.</summary>
	public class D_TreasureSpawns : CompiledGumpDef {

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			ImprovedDialog dialogHandler = new ImprovedDialog(this.GumpInstance);
			List<TreasureSpawnEntry> trSpawns = ((TreasureChest) focus).TreasureSpawns;
			TreasureChest treasure = focus as TreasureChest;
			int rowCount = treasure.TreasureSpawns.Count;
			int i = 0;
			//TreasureChest treasure = focus as TreasureChest;
			dialogHandler.CreateBackground(430);
			dialogHandler.SetLocation(70, 50);

			// Headline
			dialogHandler.AddTable(new GUTATable(1, 0));
			dialogHandler.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Seznam guard spawnù").Build();
			dialogHandler.MakeLastTableTransparent();

			// First row
			dialogHandler.LastTable.RowHeight = ButtonMetrics.D_BUTTON_HEIGHT;
			dialogHandler.AddTable(new GUTATable(1, 300, 70, ButtonMetrics.D_BUTTON_WIDTH));
			dialogHandler.LastTable[0, 0] = GUTAText.Builder.TextLabel(" Character Def").Build();
			dialogHandler.LastTable[0, 1] = GUTAText.Builder.TextLabel(" Amount").Build();

			// Second row
			dialogHandler.AddTable(new GUTATable(rowCount, 300, 70, ButtonMetrics.D_BUTTON_WIDTH));
			foreach (TreasureSpawnEntry tse in trSpawns) {
				dialogHandler.LastTable[i, 0] = GUTAInput.Builder.Id((i * 10) + 1).Text(tse.charDef.PrettyDefname).Build();
				dialogHandler.LastTable[i, 1] = GUTAInput.Builder.Type(LeafComponentTypes.InputNumber).Id((i * 10) + 2).Text(tse.amount.ToString()).Build();
				dialogHandler.LastTable[i, 2] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(10 + i).Build();
				i++;
			}

			// last row
			dialogHandler.AddTable(new GUTATable(1, 270, 100, ButtonMetrics.D_BUTTON_WIDTH));
			dialogHandler.LastTable[0, 0] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonOK).Id(1).Build();	// OK
			dialogHandler.LastTable[0, 1] = GUTAText.Builder.TextLabel(" Add spawn").Build();
			dialogHandler.LastTable[0, 2] = GUTAButton.Builder.Id(2).Build();	// Add button

			dialogHandler.WriteOut();
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player p = gi.Cont as Player;
			TreasureChest treasure = (TreasureChest) gi.Focus;
			switch (gr.PressedButton) {
				case 0:	//Cancel
					p.SysMessage("Nastavení nezmìnìno.");
					break;
				default:
					bool err = false;
					string thisDef;
					CharacterDef thisChar;
					int ignore = -1;					// a safe value was assigned to the variable ignore, which garantees that all values will be modified
					if (gr.PressedButton > 2) {	// not OK or Add button - i.e. removing button
						ignore = Convert.ToInt32(gr.PressedButton) - 10;	// item with this index will be removed anyway, so we'll skip it's modifications
					}
					for (int i = 0; i < treasure.TreasureSpawns.Count; i++) {
						if (i != ignore) {
							thisDef = gr.GetTextResponse(i * 10 + 1);
							thisChar = ThingDef.GetByDefname(thisDef) as CharacterDef;
							if (thisChar == null) {
								p.RedMessage("'" + thisDef + "' neni platny characterDef!");
								err = true;
							} else if (gr.GetNumberResponse(i * 10 + 2) < 1) {
								p.RedMessage("amount pro characterDef '" + thisDef + "' musi byt kladny!");
								err = true;
							} else {
								treasure.OverwriteTreasureSpawn(i, thisChar, (int) gr.GetNumberResponse(i * 10 + 2));
							}
						}
					}
					if (gr.PressedButton > 2) { // not OK or Add button
						treasure.RemoveTreasureSpawn(Convert.ToInt32(gr.PressedButton) - 10);	//Gets the value correspondent to the position of an item in the List of treasureItems
						treasure.Dialog(p, this);
						return;
					}
					if (gr.PressedButton == 2) { // pressed Add button
						p.SysMessage("Pøidán defaultní spawn.");
						treasure.AddTreasureSpawn((CharacterDef) ThingDef.GetByDefname("c_ostard_zostrich"), 1);
						treasure.Dialog(p, this);
						return;
					}
					if (err) {
						treasure.Dialog(p, this);
						return;
					}

					p.SysMessage("Potvrzeno zadání hodnot.");
					break;
			}
			treasure.Dialog(p, SingletonScript<D_TreasureChest>.Instance);
		}
	}
}