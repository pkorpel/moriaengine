using System;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	public partial class WarDeclaratorDef {
	}

	/// <summary>#1116 - Erebor's i_war_declarator (dobyvani-declarator.scp): a GM's double click sets its realm from the area
	/// it stands in; a player of that realm with the right to declare war gets dialog_declare_war, anyone else is refused.
	/// It carries its realm's war clock (WarDeclaratorTimer).</summary>
	[ViewableClass]
	public partial class WarDeclarator {
		private static readonly TagKey realmTK = TagKey.Acquire("_warDeclaratorRealm_");

		/// <summary>Erebor's my_realm: the realm whose front this stone serves.</summary>
		public Realm FrontRealm {
			get {
				object realm = this.GetTag(realmTK);
				return (realm is Realm known) ? known : Realm.None;
			}
		}

		/// <summary>The GM's double click, or ConquestSetup: the realm and its name ("Gondorsky vyhlasovac valky").</summary>
		public void SetRealm(Realm realm) {
			this.SetTag(realmTK, realm);
			this.Name = ((realm == Realm.Gondor) ? "Gondorsky" : "Mordorsky") + " vyhlasovac valky";
			this.EnsureClock();
		}

		public void EnsureClock() {
			if ((this.FrontRealm != Realm.None) && (this.GetTimer(WarDeclaratorTimer.Key) == null)) {
				this.AddTimer(WarDeclaratorTimer.Key, new WarDeclaratorTimer()).DueInSeconds = WarDeclaratorTimer.Period;
			}
		}

		public override void On_DClick(AbstractCharacter from) {
			if (!(from is Character ch)) {
				return;
			}
			if (ch.IsGM) {
				FlaggedRegion area = RegionEventSystem.AreaOf(this.GetMap().GetRegionFor(this));
				Realm realm = (area == null) ? Realm.None : area.Realm;
				if ((realm != Realm.Gondor) && (realm != Realm.Mordor)) {
					ch.RedMessage("Vyhlasovac musi stat na uzemi Gondoru nebo Mordoru.");
					return;
				}
				this.SetRealm(realm);
				ch.SysMessage(this.Name + " nastaven.");
				return;
			}
			this.EnsureClock();
			Realm front = this.FrontRealm;
			if ((front == Realm.None) || !(ch is Player player)) {
				return;
			}
			if ((player.Realm != front) || !Conquest.HasRight(player)) {
				ch.RedMessage("Nejsi " + Conquest.Word(ch, "opravnen", "opravnena") + " vyhlasit " + ((front == Realm.Gondor) ? "Mordoru" : "Gondoru") + " valku.");
				return;
			}
			D_DeclareWar.Open(player, Conquest.Front(front), null);
		}
	}

	/// <summary>#1116 - Erebor's dialog_declare_war: the one keep next on the front line (checkbox 101), day 201, month 202,
	/// hour 203, OK 1; then the current war lord and the queue, with cancel (2) on one's own row.</summary>
	public class D_DeclareWar : CompiledGumpDef {
		internal const int OkButton = 1, CancelButton = 2, FortCheck = 101, DayEntry = 201, MonthEntry = 202, HourEntry = 203;
		private const int Width = 400, Row = 22, Header = 0x481, Text = 955, LordHeader = 44, LordHue = 52, QueueHeader = 0x2e, QueueHue = 0x31;

		internal static void Open(Player player, WarFront front, string[] entered) {
			player.Dialog(player, SingletonScript<D_DeclareWar>.Instance, new DialogArgs(front.realm, entered));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			WarFront front = Conquest.Front((Realm) args[0]);
			string[] entered = args[1] as string[];
			DateTime tomorrow = Conquest.Now.AddDays(1);
			int queued = front.queue.Count;
			int rows = 6 + ((front.lord != null) ? 2 : 0) + ((queued > 0) ? (queued + 1) : 0);
			this.SetLocation(20, 30);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, (rows * Row) + 20, 5054);
			int y = 10;
			string target = front.Target;
			this.AddLabel(10, y, Header, "Vyhlaseni valky " + ((target.StartsWith("Mordor", StringComparison.Ordinal)) ? "Mordorske" : "Gondorske") + " pevnosti");
			this.AddButton(Width - 40, y, 0xfb1, 0xfb3, 0, GumpButtonType.Reply, 0);
			y += Row;
			this.AddLabel(10, y, Text, Conquest.AreaName(target));
			this.AddCheck(Width - 40, y, 210, 211, true, FortCheck);
			y += Row;
			ConquestSettings settings = ConquestSettings.instance;
			this.AddLabel(10, y, Header, "Mozne casy dobyvani: " + settings.warFromHour + "-" + settings.warToHour);
			y += Row;
			this.AddLabel(10, y, Text, "Den:");
			this.AddTextEntry(50, y, 40, 20, Text, DayEntry, (entered != null) ? entered[0] : tomorrow.Day.ToString(CultureInfo.InvariantCulture));
			this.AddLabel(110, y, Text, "Mesic:");
			this.AddTextEntry(160, y, 40, 20, Text, MonthEntry, (entered != null) ? entered[1] : tomorrow.Month.ToString(CultureInfo.InvariantCulture));
			this.AddLabel(220, y, Text, "Hodina:");
			this.AddTextEntry(280, y, 40, 20, Text, HourEntry, (entered != null) ? entered[2] : settings.warFromHour.ToString(CultureInfo.InvariantCulture));
			y += Row;
			this.AddButton(10, y, 0xfb7, 0xfb9, OkButton, GumpButtonType.Reply, 0);
			y += Row + 4;
			if (front.lord != null) {
				this.AddLabel(10, y, LordHeader, "Aktualni vyhlasovatel");
				this.AddLabel(250, y, LordHeader, "Cas");
				y += Row;
				this.AddLabel(10, y, LordHue, front.lord.Name);
				this.AddLabel(250, y, LordHue, Conquest.Slot(front.begins));
				if (front.lord == sendTo) {
					this.AddButton(Width - 40, y, 0xfb1, 0xfb3, CancelButton, GumpButtonType.Reply, 0);
				}
				y += Row;
			}
			if (queued > 0) {
				this.AddLabel(10, y, QueueHeader, "Dalsi vyhlasovatel(e)");
				this.AddLabel(250, y, QueueHeader, "Cas");
				y += Row;
				foreach (WarQueueEntry entry in front.queue) {
					this.AddLabel(10, y, QueueHue, entry.player.Name);
					this.AddLabel(250, y, QueueHue, Conquest.Slot(entry.begins));
					if (entry.player == sendTo) {
						this.AddButton(Width - 40, y, 0xfb1, 0xfb3, CancelButton, GumpButtonType.Reply, 0);
					}
					y += Row;
				}
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if (!(gi.Cont is Player player)) {
				return;
			}
			WarFront front = Conquest.Front((Realm) args[0]);
			if (gr.PressedButton == CancelButton) {
				string reply = Conquest.Cancel(player, front);
				if (reply != null) {
					player.SysMessage(reply);
				}
				return;
			}
			if (gr.PressedButton != OkButton) {
				return;
			}
			string[] entered = { (gr.GetTextResponse(DayEntry) ?? "").Trim(), (gr.GetTextResponse(MonthEntry) ?? "").Trim(), (gr.GetTextResponse(HourEntry) ?? "").Trim() };
			if ((entered[0].Length == 0) || (entered[1].Length == 0) || (entered[2].Length == 0)) {
				player.RedMessage("Zadej vsechno: den, mesic i hodinu zacatku valky.");
				Open(player, front, entered);
				return;
			}
			if (!gr.IsSwitched(FortCheck)) {
				player.RedMessage("Musis vybrat pevnost k dobyvani.");
				Open(player, front, entered);
				return;
			}
			int[] numbers = new int[3];
			for (int i = 0; i < 3; i++) {
				int.TryParse(entered[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out numbers[i]); //Erebor's fixnumber: not a number is 0
			}
			string answer = Conquest.Declare(player, front, numbers[0], numbers[1], numbers[2], out bool done, out bool reopen);
			if (done) {
				player.SysMessage(answer);
			} else {
				player.RedMessage(answer);
			}
			if (reopen) {
				Open(player, front, entered);
			}
		}
	}
}
