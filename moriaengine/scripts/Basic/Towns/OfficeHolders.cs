using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#911 - Erebor's hracskefunkce and d_playertitle (systems/scane.scp), for everyone: who holds the realms'
	/// offices. Gondor's General, its towns' mayors and the keeps' captains (named in the colour the reader sees them in,
	/// as getNameHue); Mordor's First Nazgul and the Nazguls ruling its towns. Erebor listed fixed stones by uid; here the
	/// towns are every town stone there is, by realm and name, and the keeps are the conquest's four.</summary>
	public class D_OfficeHolders : CompiledGumpDef {
		private const int Width = 700, Row = 20, Column = 350, NameX = 180, TitleHue = 42, OfficeHue = 50, GroupHue = 152, PlaceHue = 2301, NameHue = 955;
		private static readonly string[] keeps = { "Gondor_1", "Mordor_1", "Mordor_2", "Gondor_2" }; //Erebor's order

		[SteamFunction]
		public static void HracskeFunkce(Character self) {
			if (self is Player player) {
				player.Dialog(player, SingletonScript<D_OfficeHolders>.Instance, new DialogArgs());
			}
		}

		private static List<Town> TownsOf(Realm realm) {
			List<Town> towns = new List<Town>();
			foreach (Town town in Town.All) {
				if (town.Realm == realm) {
					towns.Add(town);
				}
			}
			towns.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
			return towns;
		}

		private static string NameOf(Character ch) {
			return ((ch == null) || ch.IsDeleted) ? "-" : ch.Name;
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			List<Town> gondor = TownsOf(Realm.Gondor), mordor = TownsOf(Realm.Mordor);
			int leftRows = 3 + gondor.Count + 2 + keeps.Length, rightRows = 3 + mordor.Count;
			int height = 60 + (System.Math.Max(leftRows, rightRows) * Row);
			this.SetLocation(20, 30);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, height, 2620);
			this.AddLabel(14, 12, TitleHue, "Hracske funkce");

			int y = 40;
			this.AddLabel(14, y, OfficeHue, "General Gondoru:");
			this.AddLabel(NameX, y, NameHue, NameOf(RealmOffice.For(Realm.Gondor)?.holder));
			y += Row * 2;
			this.AddLabel(14, y, GroupHue, "Starostove:");
			foreach (Town town in gondor) {
				y += Row;
				this.AddLabel(14, y, PlaceHue, town.Name);
				this.AddLabel(NameX, y, NameHue, NameOf(town.mayor));
			}
			y += Row * 2;
			this.AddLabel(14, y, GroupHue, "Kapitanove:");
			foreach (string fort in keeps) {
				y += Row;
				Player captain = Conquest.CaptainOf(fort);
				int hue = (captain != null) ? Notoriety.GetNameHue(captain.GetHighlightColorFor(sendTo)) : NameHue;
				this.AddLabel(14, y, PlaceHue, KeepCaptains.KeepName(fort).Replace('_', ' '));
				this.AddLabel(NameX, y, hue, NameOf(captain));
			}

			y = 40;
			this.AddLabel(Column + 14, y, OfficeHue, "Prvni nazgul:");
			this.AddLabel(Column + NameX, y, NameHue, NameOf(RealmOffice.For(Realm.Mordor)?.holder));
			y += Row * 2;
			this.AddLabel(Column + 14, y, GroupHue, "Nazgulove:");
			foreach (Town town in mordor) {
				y += Row;
				this.AddLabel(Column + 14, y, PlaceHue, town.Name);
				this.AddLabel(Column + NameX, y, NameHue, NameOf(town.mayor));
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			//only to read, as Erebor's
		}
	}
}
