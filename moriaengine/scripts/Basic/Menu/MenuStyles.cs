using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Networking;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1004 - which way the menus of Tracking, Summon and Polymorph open, until one of them is picked for good.</summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class MenuStyleSettings : SettingsMetaCategory {
		[SavedMember]
		public static MenuStyleSettings instance = new MenuStyleSettings();

		[LoadingInitializer]
		public MenuStyleSettings() {
		}

		/// <summary>For whoever hasn't used .menustyle: 0 a dialog, 1 Sphere's picture menu, 2 both at once.</summary>
		[SaveableData]
		public int defaultStyle = MenuStyles.Dialog;
	}

	/// <summary>A row of Sphere's picture menu, packet 0x7C: an item's picture and a text.</summary>
	internal sealed class PictureMenuRow : OpenDialogBoxPacket.IItemMenuEntry {
		public int Model { get; }
		public int Color { get; }
		public string Text { get; }

		internal PictureMenuRow(int model, string text, int color = 0) {
			this.Model = model;
			this.Text = text;
			this.Color = color;
		}
	}

	/// <summary>#1004 - opens a menu as the player's style says. With both open the first answer counts: the picture menu closes
	/// the dialog, and once the dialog is gone what the picture menu says is ignored, as a server can't close that one.</summary>
	internal static class MenuStyles {
		internal const int Dialog = 0, Picture = 1, Both = 2;
		private const int Styles = 3;
		private static readonly TagKey styleTK = TagKey.Acquire("_menuStyle_");

		internal static int Of(Player self) {
			return (self.GetTag(styleTK) is int style) ? style : MenuStyleSettings.instance.defaultStyle;
		}

		internal static int Next(Player self) {
			int style = (Of(self) + 1) % Styles;
			self.SetTag(styleTK, style);
			return style;
		}

		internal static void Show(Player self, GumpDef dialog, DialogArgs args, string header, IList<PictureMenuRow> rows, Action<int> chosen, Action cancelled) {
			int style = Of(self);
			if (style != Picture) {
				self.Dialog(self, dialog, args);
			}
			GameState state = self.GameState;
			if ((style != Dialog) && (rows.Count > 0) && (state != null)) { //Sphere's addItemMenu sends no empty menu
				bool both = style == Both;
				state.ItemMenu(header, rows,
					(answering, index, parameter) => {
						if (!both) {
							chosen(index);
						} else if (self.HasOpenedDialog(dialog)) {
							self.DialogForget(dialog);
							chosen(index);
						}
					},
					(answering, parameter) => {
						if (!both) {
							cancelled?.Invoke();
						}
					},
					null);
			}
		}

		/// <summary>The picture of a character: its ICON, and for those that have none a man's or a woman's.</summary>
		internal static int IconOf(Character character) {
			int icon = (character.Def is NPCDef def) ? PetFigurine.IconOf(def) : 0;
			if (icon != 0) {
				return icon;
			}
			return ThingDef.GetByDefname(character.IsFemale ? "i_pet_woman" : "i_pet_man").Model;
		}
	}

	public static class MenuStyleCommands {
		[SteamFunction]
		public static void MenuStyle(Player self) {
			MenuStyleLoc texts = Loc<MenuStyleLoc>.Get(self.Language);
			switch (MenuStyles.Next(self)) {
				case MenuStyles.Dialog:
					self.SysMessage(texts.NowDialog);
					break;
				case MenuStyles.Picture:
					self.SysMessage(texts.NowPicture);
					break;
				default:
					self.SysMessage(texts.NowBoth);
					break;
			}
		}
	}

	internal class MenuStyleLoc : CompiledLocStringCollection<MenuStyleLoc> {
		public string NowDialog = "Menu se ted otevira jako dialog.";
		public string NowPicture = "Menu se ted otevira jako obrazkove menu.";
		public string NowBoth = "Menu se ted otevira obema zpusoby, plati prvni volba.";
	}
}
