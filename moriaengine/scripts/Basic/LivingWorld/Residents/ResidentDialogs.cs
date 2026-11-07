using System;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1242 - .resident: a GM's look at a resident (the cursor on its body), with a rename and a "go to your place now".</summary>
	public static class ResidentCommands {
		[SteamFunction]
		public static void Resident(Character self) {
			Player player = self as Player;
			if (player != null) {
				player.SysMessage("Ktereho obyvatele?");
				player.Target(SingletonScript<Targ_Resident>.Instance);
			}
		}
	}

	public class Targ_Resident : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			Item gem = Residents.GemOf(targetted);
			if (gem == null) {
				self.SysMessage("To neni obyvatel mesta.");
				return TargetResult.RestartTargetting;
			}
			D_Resident.Show(self, gem);
			return TargetResult.Done;
		}
	}

	public class D_Resident : CompiledGumpDef {
		private const int LabelHue = 955, ValueHue = 52, ButtonHue = 2301;
		internal const int Rename = 1, PlaceNow = 2, NameEntry = 10;

		public static void Show(Character gm, Item gem) {
			gm.Dialog(gm, SingletonScript<D_Resident>.Instance, new DialogArgs(gem));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Item gem = (Item) args[0];
			CityVenue work = Residents.WorkplaceOf(gem);
			CityVenue home = VenueRegistry.ById(Residents.HomeIdOf(gem));
			CityVenue now = ResidentSchedule.PlaceFor(gem, VenueClock.Shift);
			VenueRole role = Residents.RoleOf(gem);
			this.SetLocation(40, 40);
			this.AddPage(0);
			this.AddBackground(0, 0, 420, 270, 2620);
			this.AddLabel(14, 12, ValueHue, "Obyvatel " + (Residents.IdOf(gem) ?? ""));
			int y = 40;
			Row(this, ref y, "Jmeno:", null);
			this.AddTextEntry(120, y - 24, 280, 20, ValueHue, NameEntry, Residents.NameOf(gem));
			Row(this, ref y, "Role:", (role == null) ? Residents.RoleKeyOf(gem) : role.Name);
			Row(this, ref y, "Pracuje:", (work == null) ? "-" : work.Label);
			Row(this, ref y, "Bydli:", (home == null) ? "v miste prace" : home.Label);
			Row(this, ref y, "Povaha:", string.Join(", ", Residents.TraitsOf(gem)));
			Row(this, ref y, "Ted (" + ((int) VenueClock.Hour) + " h):", (now == null) ? "-" : now.Label);
			Row(this, ref y, "Pratelstvi s tebou:", Residents.Friendship((Character) sendTo, Residents.IdOf(gem) ?? "") + "/" + Residents.MaxFriendship);
			this.AddButton(14, y + 8, 4005, 4007, Rename, GumpButtonType.Reply, 0);
			this.AddLabel(50, y + 8, ButtonHue, "Ulozit jmeno");
			this.AddButton(200, y + 8, 4005, 4007, PlaceNow, GumpButtonType.Reply, 0);
			this.AddLabel(236, y + 8, ButtonHue, "Poslat na misto");
		}

		private static void Row(D_Resident dialog, ref int y, string label, string value) {
			dialog.AddLabel(14, y, LabelHue, label);
			if (value != null) {
				dialog.AddLabel(160, y, ValueHue, value);
			}
			y += 24;
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Item gem = (Item) args[0];
			Character gm = (Character) gi.Cont;
			if (gem.IsDeleted) {
				return;
			}
			switch (gr.PressedButton) {
				case Rename:
					string name = (gr.GetTextResponse(NameEntry) ?? "").Trim();
					if (name.Length > 0) {
						Residents.Rename(gem, name);
					}
					Show(gm, gem);
					break;
				case PlaceNow:
					NPC body = Residents.BodyOf(gem);
					if (body != null) {
						ResidentSchedule.Apply(gem, body, true);
					}
					Show(gm, gem);
					break;
			}
		}
	}
}
