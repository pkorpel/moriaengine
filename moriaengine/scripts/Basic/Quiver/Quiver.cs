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

using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - Erebor's i_toulec: holds up to 5000 arrows of one kind, poison and all. It only stores them, a bow
	/// doesn't shoot out of it.</summary>
	public class t_quiver : CompiledTriggerGroup {
		internal const int MostArrows = 5000;
		internal static readonly TagKey countTK = TagKey.Acquire("arrows");
		internal static readonly TagKey kindTK = TagKey.Acquire("type_arrows");
		internal static readonly TagKey nameTK = TagKey.Acquire("name_arrows");
		private static readonly TagKey poisonPowerTK = TagKey.Acquire("arrow_jed_sila");
		private static readonly TagKey poisonNameTK = TagKey.Acquire("arrow_jed_name");
		private static readonly TagKey poisonKindTK = TagKey.Acquire("arrow_jed_typ");

		internal static int CountIn(Item quiver) {
			return (quiver.GetTag(countTK) is int count) ? count : 0;
		}

		public TriggerResult On_DClick(Item self, Character clicker) {
			if (clicker is Player player) {
				if (self.TopObj() == player) {
					player.Dialog(player, SingletonScript<D_Quiver>.Instance, new DialogArgs(self));
				} else {
					player.RedMessage(Loc<QuiverLoc>.Get(player.Language).CantReachQuiver);
				}
			}
			return TriggerResult.Cancel;
		}

		public TriggerResult On_Click(Item self, Character clicker) {
			QuiverLoc texts = Loc<QuiverLoc>.Get(clicker.Language);
			int count = CountIn(self);
			self.ClickMessage(clicker, (count > 0) ? count.ToString(CultureInfo.InvariantCulture) + " " + self.GetTag(nameTK) : texts.Empty);
			return TriggerResult.Cancel;
		}

		internal static void Add(Player self, Item quiver, Item arrows) {
			QuiverLoc texts = Loc<QuiverLoc>.Get(self.Language);
			int count = CountIn(quiver);
			if (count > 0) {
				if (!ReferenceEquals(quiver.GetTag(kindTK), arrows.Def)) {
					self.RedMessage(texts.OtherKind);
					return;
				}
			} else {
				if (PoisonedItemPlugin.GetPoisonPlugin(arrows) is PoisonedItemPlugin poison) {
					quiver.SetTag(poisonPowerTK, poison.PoisonPower);
					quiver.SetTag(poisonNameTK, poison.PoisonName);
					quiver.SetTag(poisonKindTK, (int) poison.PoisonKind);
				}
				quiver.SetTag(kindTK, arrows.Def);
				quiver.SetTag(nameTK, arrows.Name);
				quiver.Color = arrows.Color;
			}
			int room = MostArrows - count;
			if (arrows.Amount <= room) {
				quiver.SetTag(countTK, count + arrows.Amount);
				arrows.Delete();
			} else {
				arrows.Amount -= room;
				quiver.SetTag(countTK, MostArrows);
			}
		}

		internal static void TakeOut(Player self, Item quiver, int wanted) {
			QuiverLoc texts = Loc<QuiverLoc>.Get(self.Language);
			int count = CountIn(quiver);
			if ((count <= 0) || !(quiver.GetTag(kindTK) is ItemDef kind)) {
				self.RedMessage(texts.NoArrows);
				return;
			}
			Item arrows = (Item) kind.Create(self.Backpack);
			arrows.Amount = System.Math.Min(wanted, count);
			if (quiver.GetTag(poisonPowerTK) is int power) {
				arrows.Color = quiver.Color;
				PoisonedItemPlugin.Bind(arrows, (PoisonKind) (int) quiver.GetTag(poisonKindTK), quiver.GetTag(poisonNameTK) as string, power, 0);
			}
			if (wanted < count) {
				quiver.SetTag(countTK, count - wanted);
				self.SysMessage(string.Format(self.IsFemale ? texts.TookFemale : texts.TookMale, wanted));
			} else {
				quiver.RemoveTag(countTK);
				quiver.RemoveTag(kindTK);
				quiver.RemoveTag(nameTK);
				quiver.RemoveTag(poisonPowerTK);
				quiver.Color = 0;
				self.SysMessage(string.Format(self.IsFemale ? texts.TookLastFemale : texts.TookLastMale, count));
			}
		}
	}

	public class Targ_Quiver : CompiledTargetDef {
		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			QuiverLoc texts = Loc<QuiverLoc>.Get(self.Language);
			if (!(targetted is Projectile)) {
				self.RedMessage(texts.OnlyArrows);
			} else if (!self.CanReach(targetted).Allow) {
				self.RedMessage(texts.CantReachArrows);
			} else {
				t_quiver.Add(self, (Item) parameter, targetted);
			}
			return TargetResult.RestartTargetting;
		}
	}

	public class D_Quiver : CompiledGumpDef {
		private const int AddButton_ = 1, TakeButton = 2, TakeEntry = 5, Hue = 2301;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Item quiver = (Item) args[0];
			QuiverLoc texts = Loc<QuiverLoc>.Get(((Player) sendTo).Language);
			this.SetLocation(180, 230);
			this.AddPage(0);
			this.AddBackground(0, 0, 258, 130, 2620);
			this.AddLabel(90, 8, Hue, texts.Quiver);
			this.AddLabel(14, 34, Hue, texts.ArrowKind);
			this.AddLabel(90, 34, Hue, (quiver.GetTag(t_quiver.nameTK) as string) ?? texts.EmptyKind);
			this.AddLabel(14, 58, Hue, texts.Count);
			this.AddLabel(90, 58, Hue, t_quiver.CountIn(quiver).ToString(CultureInfo.InvariantCulture));
			this.AddLabel(150, 58, Hue, texts.AddArrows);
			this.AddButton(210, 58, 0xfa5, 0xfa7, AddButton_, GumpButtonType.Reply, 0);
			this.AddLabel(14, 84, Hue, texts.TakeOut);
			this.AddTextEntry(90, 84, 50, 20, Hue, TakeEntry, "200");
			this.AddButton(210, 84, 0xfa5, 0xfa7, TakeButton, GumpButtonType.Reply, 0);
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = (Player) gi.Cont;
			Item quiver = (Item) args[0];
			QuiverLoc texts = Loc<QuiverLoc>.Get(player.Language);
			if ((gr.PressedButton == 0) || quiver.IsDeleted) {
				return;
			}
			if (quiver.TopObj() != player) {
				player.RedMessage(texts.CantReachQuiver);
			} else if (gr.PressedButton == AddButton_) {
				player.SysMessage(texts.AimArrows);
				player.Target(SingletonScript<Targ_Quiver>.Instance, quiver);
			} else if (gr.PressedButton == TakeButton) {
				if (t_quiver.CountIn(quiver) <= 0) {
					player.RedMessage(texts.NoArrows);
				} else if (int.TryParse(gr.GetTextResponse(TakeEntry), NumberStyles.Integer, CultureInfo.InvariantCulture, out int wanted) && (wanted > 0)) {
					t_quiver.TakeOut(player, quiver, wanted);
				} else {
					player.RedMessage(texts.WantNumber);
				}
			}
		}
	}

	internal class QuiverLoc : CompiledLocStringCollection<QuiverLoc> {
		public string Quiver = "Toulec";
		public string Empty = "Toulec je prazdny.";
		public string EmptyKind = " prazdny";
		public string ArrowKind = "Druh sipu";
		public string Count = "Pocet";
		public string AddArrows = "Pridat";
		public string TakeOut = "Vyndat";
		public string AimArrows = "Zamer sipy ktere chces pridat do toulce.";
		public string OnlyArrows = "Do toulce muzes dat jen sipy.";
		public string OtherKind = "Uz tam mas jiny druch sipu.";
		public string CantReachQuiver = "Nedosahnes na toulec.";
		public string CantReachArrows = "Nedosahnes na sipy.";
		public string NoArrows = "V toulci nejsou zadne sipy.";
		public string WantNumber = "Hodnota musi byt cislo vetsi nez 0.";
		public string TookMale = "Vyndal jsi {0} sipu.";
		public string TookFemale = "Vyndala jsi {0} sipu.";
		public string TookLastMale = "Vyndal jsi {0} sipu. V toulci uz dalsi sipy nejsou.";
		public string TookLastFemale = "Vyndala jsi {0} sipu. V toulci uz dalsi sipy nejsou.";
	}
}
