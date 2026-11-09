using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1315 - .udalost's dialog for GMs: the events under way (go there, stop one, stop all), and starting one: a kind,
	/// then one of its places. args: the kind picked ("" = the overview) and the event ids or places as listed, so a button
	/// finds what its row showed even if the list changed meanwhile.</summary>
	public class D_WorldEvents : CompiledGumpDef {
		private const int Width = 540, Row = 22, LabelHue = 955, ValueHue = 52, BadHue = 33, MaxRunningRows = 10;
		internal const int StopAll = 2, Back = 3, GoBase = 100, StopBase = 200, KindBase = 300, PlaceBase = 400;

		public static void Show(Player gm, WorldEventKind kind) {
			string[] listed;
			if (kind == null) {
				List<string> ids = new List<string>();
				foreach (WorldEvent e in WorldEvents.Running) {
					ids.Add(e.Id);
				}
				listed = ids.ToArray();
			} else {
				listed = PlacesOf(kind).ToArray();
			}
			gm.DialogForget(SingletonScript<D_WorldEvents>.Instance);
			gm.Dialog(gm, SingletonScript<D_WorldEvents>.Instance, new DialogArgs((kind == null) ? "" : kind.Key, listed));
		}

		internal static List<string> PlacesOf(WorldEventKind kind) {
			List<string> places = new List<string>();
			foreach (string place in kind.Places) {
				if (kind.IsFor(place)) {
					places.Add(place);
				}
			}
			return places;
		}

		private static WorldEvent ById(string id) {
			foreach (WorldEvent e in WorldEvents.Running) {
				if (e.Id == id) {
					return e;
				}
			}
			return null;
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			WorldEventKind kind = WorldEvents.KindOf((string) args[0]);
			string[] listed = (string[]) args[1];
			if (kind == null) {
				this.Overview(listed);
			} else {
				this.Places(kind, listed);
			}
		}

		private void Overview(string[] ids) {
			IList<WorldEventKind> kinds = WorldEvents.Kinds;
			int runningRows = Math.Min(Math.Max(ids.Length, 1), MaxRunningRows);
			int kindRows = (kinds.Count + 1) / 2;
			int height = 120 + ((runningRows + kindRows) * Row) + 40;
			this.SetLocation(40, 40);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, height, 2620);
			this.AddLabel(14, 12, ValueHue, "Udalosti ve svete");
			this.AddLabel(200, 12, WorldEventSettings.instance.enabled ? LabelHue : BadHue,
				"samy od sebe: " + (WorldEventSettings.instance.enabled ? "zapnute" : "vypnute") + " (.settings Udalosti)");
			int y = 40;
			if (ids.Length == 0) {
				this.AddLabel(14, y, LabelHue, "Ted nic nebezi.");
				y += Row;
			}
			for (int i = 0; (i < ids.Length) && (i < MaxRunningRows); i++) {
				WorldEvent e = ById(ids[i]);
				if (e == null) {
					continue;
				}
				this.AddButton(14, y, 4005, 4007, GoBase + i, GumpButtonType.Reply, 0);
				this.AddButton(46, y, 4017, 4019, StopBase + i, GumpButtonType.Reply, 0);
				this.AddLabelCropped(84, y, 160, Row, ValueHue, e.Kind.Name + ", " + e.CityName);
				this.AddLabelCropped(250, y, 220, Row, LabelHue, e.Describe());
				this.AddLabel(Width - 60, y, LabelHue, WorldEvents.MinutesLeft(e) + " min");
				y += Row;
			}
			if (ids.Length > 0) {
				this.AddButton(14, y, 4017, 4019, StopAll, GumpButtonType.Reply, 0);
				this.AddLabel(50, y, LabelHue, "Zastavit vse");
			}
			this.AddLabel(Width - 230, y, LabelHue, "Jit tam / Zastavit");
			y += Row + 10;
			this.AddLabel(14, y, ValueHue, "Zacit udalost:");
			y += Row;
			for (int k = 0; k < kinds.Count; k++) {
				int x = 14 + ((k % 2) * (Width / 2));
				int rowY = y + ((k / 2) * Row);
				this.AddButton(x, rowY, 4005, 4007, KindBase + k, GumpButtonType.Reply, 0);
				this.AddLabelCropped(x + 36, rowY, (Width / 2) - 60, Row, LabelHue, kinds[k].Name + " (" + kinds[k].Key + ")");
			}
		}

		private void Places(WorldEventKind kind, string[] places) {
			int height = 100 + (Math.Max(places.Length, 1) * Row);
			this.SetLocation(40, 40);
			this.AddPage(0);
			this.AddBackground(0, 0, 400, height, 2620);
			this.AddLabel(14, 12, ValueHue, "Zacit: " + kind.Name);
			int y = 40;
			if (places.Length == 0) {
				this.AddLabel(14, y, LabelHue, "Nemuze byt nikde.");
			}
			for (int p = 0; p < places.Length; p++) {
				bool busy = false;
				foreach (WorldEvent e in WorldEvents.Running) {
					busy |= string.Equals(e.City, places[p], StringComparison.OrdinalIgnoreCase);
				}
				this.AddButton(14, y, 4005, 4007, PlaceBase + p, GumpButtonType.Reply, 0);
				this.AddLabel(50, y, busy ? BadHue : LabelHue, kind.PlaceName(places[p]) + (busy ? " (uz tam neco bezi)" : ""));
				y += Row;
			}
			this.AddButton(14, height - 34, 4014, 4016, Back, GumpButtonType.Reply, 0);
			this.AddLabel(50, height - 32, LabelHue, "Zpet");
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if (!(gi.Cont is Player gm)) {
				return;
			}
			WorldEventKind kind = WorldEvents.KindOf((string) args[0]);
			string[] listed = (string[]) args[1];
			int button = gr.PressedButton;
			if (button == 0) {
				return;
			}
			if (kind != null) {
				if ((button >= PlaceBase) && (button - PlaceBase < listed.Length)) {
					gm.SysMessage(WorldEvents.StartByGm(kind, listed[button - PlaceBase]));
				}
				Show(gm, null);
				return;
			}
			IList<WorldEventKind> kinds = WorldEvents.Kinds;
			if ((button >= KindBase) && (button - KindBase < kinds.Count)) {
				Show(gm, kinds[button - KindBase]);
				return;
			}
			if (button == StopAll) {
				int stopped = 0;
				foreach (string id in listed) {
					WorldEvent e = ById(id);
					if (e != null) {
						WorldEvents.Stop(e);
						stopped++;
					}
				}
				gm.SysMessage("Ukonceno udalosti: " + stopped + ".");
			} else if ((button >= StopBase) && (button - StopBase < listed.Length)) {
				WorldEvent e = ById(listed[button - StopBase]);
				if (e != null) {
					WorldEvents.Stop(e);
					gm.SysMessage("Ukonceno: " + e.Kind.Name + ", " + e.CityName + ".");
				}
			} else if ((button >= GoBase) && (button - GoBase < listed.Length)) {
				WorldEvent e = ById(listed[button - GoBase]);
				if ((e != null) && (e.Centre != null)) {
					gm.Go(e.Centre);
				}
			}
			Show(gm, null);
		}
	}
}
