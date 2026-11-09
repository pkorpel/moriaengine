using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1240 - what D_Venue edits before it's saved: a new venue, or an existing one's copy.</summary>
	public sealed class VenueDraft {
		public CityVenue Existing;
		public int TypeIndex;
		public string Title = "";
		public int Opens, Closes;
		public string City;
		public byte M;
		public List<ImmutableRectangle> Rects = new List<ImmutableRectangle>();
		public int ZMin = CityVenue.AnyZMin, ZMax = CityVenue.AnyZMax;
		public List<int[]> Outline;
		internal int[] FirstCorner;

		public VenueType Type {
			get { return VenueType.All[this.TypeIndex]; }
		}

		public void SetType(int index) {
			int count = VenueType.All.Length;
			this.TypeIndex = ((index % count) + count) % count;
			this.Opens = this.Type.Opens;
			this.Closes = this.Type.Closes;
		}

		public static VenueDraft For(CityVenue venue) {
			VenueDraft draft = new VenueDraft();
			draft.Existing = venue;
			VenueType type = venue.VenueType ?? VenueType.All[0];
			draft.TypeIndex = type.IndexOf();
			draft.Title = venue.Title;
			draft.Opens = venue.Opens;
			draft.Closes = venue.Closes;
			draft.City = venue.City;
			draft.M = venue.M;
			draft.Rects = new List<ImmutableRectangle>(venue.Rectangles);
			draft.ZMin = venue.ZMin;
			draft.ZMax = venue.ZMax;
			return draft;
		}

		/// <summary>A new venue from where the GM stands: the room there, or nothing to set by corners.</summary>
		public static VenueDraft At(Character gm) {
			FoundRoom room;
			return At(gm, out room);
		}

		public static VenueDraft At(Character gm, out FoundRoom room) {
			VenueDraft draft = new VenueDraft();
			draft.SetType(0);
			draft.M = gm.M;
			draft.City = VenueRegistry.CityAt(gm);
			room = RoomScan.Scan(gm.GetMap(), gm, gm.MovementSettings);
			draft.TakeRoom(room);
			return draft;
		}

		public static VenueDraft OfRoom(FoundRoom room, byte m, string city) {
			VenueDraft draft = new VenueDraft();
			draft.SetType(0);
			draft.M = m;
			draft.City = city;
			draft.TakeRoom(room);
			return draft;
		}

		internal void TakeRoom(FoundRoom room) {
			if (room == null) {
				return;
			}
			this.Rects = room.Rectangles;
			this.ZMin = room.ZMin;
			this.ZMax = room.ZMax;
			this.Outline = room.Edge();
		}

		/// <summary>Saves it: a new marker or the existing one changed. Null with a reason when it can't be.</summary>
		public CityVenue Save(out string problem) {
			problem = null;
			if (this.Rects.Count == 0) {
				problem = "Misto nema zadnou plochu: najdi mistnost nebo nastav rohy.";
				return null;
			}
			if ((this.Opens < 0) || (this.Opens > 24) || (this.Closes < 0) || (this.Closes > 24)) {
				problem = "Hodiny jsou 0 az 24.";
				return null;
			}
			if (this.Existing != null && !this.Existing.IsDeleted) {
				string id = this.Existing.VenueId ?? VenueRegistry.NextId(this.City);
				bool moved = CityVenue.FormatRects(this.Existing.Rectangles) != CityVenue.FormatRects(this.Rects);
				if (this.Existing.VenueType != this.Type) {
					Residents.ClearCounts(this.Existing); //#1315 - a new type starts from its own counts (roles of the same name differ by type)
				}
				VenueRegistry.Fill(this.Existing, id, this.Type, this.Title, this.City, this.Opens, this.Closes, this.Rects, this.ZMin, this.ZMax);
				if (moved) { //the marker stands in the venue, where .venues sends a GM
					Point4D spot = VenueRegistry.MarkerSpot(this.Rects, this.ZMin, this.ZMax, this.M);
					this.Existing.P(spot.X, spot.Y, spot.Z, spot.M);
				}
				return this.Existing;
			}
			CityVenue venue = VenueRegistry.Create(VenueRegistry.NextId(this.City), this.Type, this.Title, this.City, this.Opens, this.Closes, this.Rects, this.ZMin, this.ZMax, this.M);
			this.Existing = venue;
			return venue;
		}

		/// <summary>Short-lived sparkles along the outline, or on the rectangles' corners for a venue set by hand.</summary>
		public void ShowOutline(Character gm) {
			List<int[]> points = this.Outline;
			if (points == null) {
				points = new List<int[]>();
				int z = gm.Z;
				foreach (ImmutableRectangle rect in this.Rects) {
					points.Add(new[] { rect.MinX, rect.MinY, z });
					points.Add(new[] { rect.MaxX, rect.MinY, z });
					points.Add(new[] { rect.MinX, rect.MaxY, z });
					points.Add(new[] { rect.MaxX, rect.MaxY, z });
				}
			}
			int step = Math.Max(1, points.Count / 80);
			for (int i = 0; i < points.Count; i += step) {
				EffectFactory.StationaryEffectAt(new Point4D(points[i][0], points[i][1], points[i][2], this.M), 0x376A, 9, 60);
			}
		}

		public int TileCount {
			get {
				int tiles = 0;
				foreach (ImmutableRectangle rect in this.Rects) {
					tiles += (rect.Width + 1) * (rect.Height + 1);
				}
				return tiles;
			}
		}
	}

	/// <summary>#1240 - a venue's settings: type, name, hours, its area (found from the room, or corners by cursor).</summary>
	public class D_Venue : CompiledGumpDef {
		private const int Width = 420, LabelHue = 955, ValueHue = 52, ButtonHue = 2301;
		internal const int Save = 1, Rescan = 2, Corners = 3, AddRect = 4, Outline = 5, Delete = 6, FillRoles = 7, RoleCounts = 8, PrevType = 10, NextType = 11;
		internal const int TitleEntry = 20, OpensEntry = 21, ClosesEntry = 22;

		public static void Show(Character gm, VenueDraft draft) {
			gm.Dialog(gm, SingletonScript<D_Venue>.Instance, new DialogArgs(draft));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			VenueDraft draft = (VenueDraft) args[0];
			this.SetLocation(40, 40);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, 354, 2620);
			this.AddLabel(14, 12, ValueHue, (draft.Existing == null) ? "Nove misto ve meste" : "Misto " + (draft.Existing.VenueId ?? ""));
			this.AddLabel(14, 40, LabelHue, "Mesto:");
			this.AddLabel(110, 40, ValueHue, draft.City ?? "(mimo mesto)");
			this.AddLabel(14, 66, LabelHue, "Druh:");
			this.AddButton(110, 66, 5603, 5607, PrevType, GumpButtonType.Reply, 0);
			this.AddLabel(135, 66, ValueHue, draft.Type.Name + (draft.Type.Mordor ? " (Mordor)" : ""));
			this.AddButton(300, 66, 5601, 5605, NextType, GumpButtonType.Reply, 0);
			this.AddLabel(14, 92, LabelHue, "Jmeno:");
			this.AddTextEntry(110, 92, 290, 20, ValueHue, TitleEntry, draft.Title);
			this.AddLabel(14, 118, LabelHue, "Otevreno od:");
			this.AddTextEntry(110, 118, 40, 20, ValueHue, OpensEntry, draft.Opens.ToString(CultureInfo.InvariantCulture));
			this.AddLabel(160, 118, LabelHue, "do:");
			this.AddTextEntry(190, 118, 40, 20, ValueHue, ClosesEntry, draft.Closes.ToString(CultureInfo.InvariantCulture));
			this.AddLabel(14, 144, LabelHue, "Plocha:");
			this.AddLabel(110, 144, ValueHue, (draft.Rects.Count == 0) ? "zadna" :
				draft.Rects.Count + " obdelnik(u), " + draft.TileCount + " policek" + ((draft.ZMin == CityVenue.AnyZMin) ? ", pod sirym nebem" : ""));
			int y = 176;
			Button(this, y, Rescan, "Najit mistnost odtud");
			Button(this, y += 24, Corners, "Nastavit rohy kurzorem");
			Button(this, y += 24, AddRect, "Pridat dalsi obdelnik");
			Button(this, y += 24, Outline, "Ukazat obrys");
			if (draft.Existing != null) {
				int wanted = (draft.Type == draft.Existing.VenueType) ? Residents.RoleCountOf(draft.Existing) : draft.Type.RoleCount;
				Button(this, y += 24, FillRoles, "Obsadit role (" + Residents.OfVenue(draft.Existing.VenueId).Count + "/" + wanted + ")");
				this.AddButton(230, y, 4005, 4007, RoleCounts, GumpButtonType.Reply, 0); //#1315 - how many of each role
				this.AddLabel(266, y, ButtonHue, "Pocty obyvatel");
				Button(this, y += 24, Delete, "Smazat misto");
			}
			this.AddButton(14, 324, 4023, 4025, Save, GumpButtonType.Reply, 0);
			this.AddLabel(50, 324, ButtonHue, "Ulozit");
			this.AddButton(150, 324, 4017, 4019, 0, GumpButtonType.Reply, 0);
			this.AddLabel(186, 324, ButtonHue, "Zavrit");
		}

		private static void Button(D_Venue dialog, int y, int id, string text) {
			dialog.AddButton(14, y, 4005, 4007, id, GumpButtonType.Reply, 0);
			dialog.AddLabel(50, y, ButtonHue, text);
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			VenueDraft draft = (VenueDraft) args[0];
			Character gm = (Character) gi.Cont;
			if (gr.PressedButton == 0) {
				return;
			}
			draft.Title = (gr.GetTextResponse(TitleEntry) ?? draft.Title).Trim();
			int hour;
			if (int.TryParse((gr.GetTextResponse(OpensEntry) ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out hour)) {
				draft.Opens = hour;
			}
			if (int.TryParse((gr.GetTextResponse(ClosesEntry) ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out hour)) {
				draft.Closes = hour;
			}
			switch (gr.PressedButton) {
				case PrevType:
				case NextType:
					draft.SetType(draft.TypeIndex + ((gr.PressedButton == NextType) ? 1 : -1));
					Show(gm, draft);
					break;
				case Rescan:
					FoundRoom room = RoomScan.Scan(gm.GetMap(), gm, gm.MovementSettings);
					if (room == null) {
						gm.SysMessage("Tady nad tebou neni strecha. Pod sirym nebem nastav rohy kurzorem.");
					} else {
						draft.TakeRoom(room);
						draft.City = draft.City ?? VenueRegistry.CityAt(gm);
						draft.ShowOutline(gm);
						if (room.TooBig) {
							gm.SysMessage("Mistnost je prilis velka, vzata jen cast. Zkontroluj obrys.");
						}
					}
					Show(gm, draft);
					break;
				case Corners:
				case AddRect:
					if (gr.PressedButton == Corners) {
						draft.Rects = new List<ImmutableRectangle>();
						draft.ZMin = CityVenue.AnyZMin;
						draft.ZMax = CityVenue.AnyZMax;
					}
					draft.Outline = null;
					draft.FirstCorner = null;
					gm.SysMessage("Zamer prvni roh.");
					((Player) gm).Target(SingletonScript<Targ_VenueCorner>.Instance, draft);
					break;
				case Outline:
					draft.ShowOutline(gm);
					Show(gm, draft);
					break;
				case FillRoles:
					if ((draft.Existing != null) && !draft.Existing.IsDeleted) {
						string problem2;
						CityVenue saved = draft.Save(out problem2); //roles follow the type picked now
						if (saved == null) {
							gm.RedMessage(problem2);
						} else {
							int stray = Residents.RemoveStrayRoles(saved);
							gm.SysMessage("Novych obyvatel: " + Residents.FillRoles(saved) + "." + ((stray > 0) ? " Odeslo obyvatel jine role: " + stray + "." : ""));
						}
						Show(gm, VenueDraft.For(draft.Existing));
					}
					break;
				case RoleCounts:
					if ((draft.Existing != null) && !draft.Existing.IsDeleted) {
						string problem3;
						CityVenue saved = draft.Save(out problem3); //the counts are of the type picked now
						if (saved == null) {
							gm.RedMessage(problem3);
							Show(gm, draft);
						} else {
							int stray = Residents.RemoveStrayRoles(saved);
							if (stray > 0) {
								gm.SysMessage("Odeslo obyvatel jine role: " + stray + ".");
							}
							D_VenueRoles.Show(gm, saved);
						}
					}
					break;
				case Delete:
					if ((draft.Existing != null) && !draft.Existing.IsDeleted) {
						int gone = Residents.OfVenue(draft.Existing.VenueId).Count; //#1242 - its residents go with it (CityVenue.On_Destroy)
						gm.SysMessage("Misto " + draft.Existing.Label + " smazano" + ((gone > 0) ? ", i s " + gone + " obyvateli." : "."));
						draft.Existing.Delete();
					}
					break;
				case Save:
					string problem;
					CityVenue venue = draft.Save(out problem);
					if (venue == null) {
						gm.RedMessage(problem);
						Show(gm, draft);
					} else {
						int stray = Residents.RemoveStrayRoles(venue); //#1242 - a new type drops the old type's people
						gm.SysMessage("Ulozeno: " + venue.Label + " (" + venue.VenueId + ")." + ((stray > 0) ? " Odeslo obyvatel jine role: " + stray + "." : ""));
						Show(gm, VenueDraft.For(venue)); //#1315 - back, now with the saved venue's buttons (fill its roles)
					}
					break;
			}
		}
	}

	/// <summary>#1315 - how many people of each role a venue has: fewer for a small house, more for a big one. A lower count sends
	/// the extra residents away, a higher one fills the role at once. args: the venue.</summary>
	public class D_VenueRoles : CompiledGumpDef {
		private const int LabelHue = 955, ValueHue = 52, ButtonHue = 2301, Row = 24;
		internal const int Back = 1, MinusBase = 100, PlusBase = 200;

		public static void Show(Character gm, CityVenue venue) {
			List<string> keys = new List<string>();
			if (venue.VenueType != null) {
				foreach (VenueRole role in venue.VenueType.Roles) {
					keys.Add(role.Key);
				}
			}
			gm.Dialog(gm, SingletonScript<D_VenueRoles>.Instance, new DialogArgs(venue, keys.ToArray()));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			CityVenue venue = (CityVenue) args[0];
			VenueRole[] roles = (venue.VenueType == null) ? new VenueRole[0] : venue.VenueType.Roles;
			int height = 110 + (roles.Length * Row);
			this.SetLocation(40, 40);
			this.AddPage(0);
			this.AddBackground(0, 0, 420, height, 2620);
			this.AddLabel(14, 12, ValueHue, "Pocty obyvatel: " + venue.Label);
			this.AddLabel(14, 40, LabelHue, "Role");
			this.AddLabel(180, 40, LabelHue, "Pocet");
			this.AddLabel(290, 40, LabelHue, "Ted / druh");
			int y = 64;
			for (int i = 0; i < roles.Length; i++) {
				int people = 0;
				foreach (Item gem in Residents.OfVenue(venue.VenueId)) {
					people += (Residents.RoleKeyOf(gem) == roles[i].Key) ? 1 : 0;
				}
				this.AddLabel(14, y, LabelHue, roles[i].Name);
				this.AddButton(170, y, 5603, 5607, MinusBase + i, GumpButtonType.Reply, 0);
				this.AddLabel(198, y, ValueHue, Residents.CountOf(venue, roles[i]).ToString(CultureInfo.InvariantCulture));
				this.AddButton(222, y, 5601, 5605, PlusBase + i, GumpButtonType.Reply, 0);
				this.AddLabel(290, y, LabelHue, people.ToString(CultureInfo.InvariantCulture) + " / " + roles[i].Count.ToString(CultureInfo.InvariantCulture));
				y += Row;
			}
			this.AddButton(14, height - 34, 4014, 4016, Back, GumpButtonType.Reply, 0);
			this.AddLabel(50, height - 32, ButtonHue, "Zpet k mistu");
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			CityVenue venue = (CityVenue) args[0];
			Character gm = (Character) gi.Cont;
			if ((venue == null) || venue.IsDeleted || (gr.PressedButton == 0)) {
				return;
			}
			if (gr.PressedButton == Back) {
				D_Venue.Show(gm, VenueDraft.For(venue));
				return;
			}
			string[] keys = (string[]) args[1]; //the rows as shown: a type changed meanwhile doesn't shift them onto other roles
			bool plus = gr.PressedButton >= PlusBase;
			int i = gr.PressedButton - (plus ? PlusBase : MinusBase);
			VenueRole role = ((i >= 0) && (i < keys.Length) && (venue.VenueType != null)) ? Array.Find(venue.VenueType.Roles, r => r.Key == keys[i]) : null;
			if (role != null) {
				Residents.SetCount(venue, role, Residents.CountOf(venue, role) + (plus ? 1 : -1));
				if (plus) {
					gm.SysMessage(role.Name + ": " + Residents.CountOf(venue, role) + ", novych obyvatel " + Residents.FillRoles(venue) + ".");
				} else {
					gm.SysMessage(role.Name + ": " + Residents.CountOf(venue, role) + ", odeslo obyvatel " + Residents.RemoveStrayRoles(venue) + ".");
				}
			}
			Show(gm, venue);
		}
	}

	/// <summary>#1240 - two corners of a rectangle, then back to the dialog.</summary>
	public class Targ_VenueCorner : CompiledTargetDef {
		protected override TargetResult On_TargonPoint(Player self, IPoint3D targetted, object parameter) {
			VenueDraft draft = (VenueDraft) parameter;
			if (draft.FirstCorner == null) {
				draft.FirstCorner = new[] { targetted.X, targetted.Y };
				self.SysMessage("Zamer protilehly roh.");
				return TargetResult.RestartTargetting;
			}
			int[] a = draft.FirstCorner;
			draft.FirstCorner = null;
			draft.Rects.Add(new ImmutableRectangle(Math.Min(a[0], targetted.X), Math.Min(a[1], targetted.Y), Math.Max(a[0], targetted.X), Math.Max(a[1], targetted.Y)));
			draft.City = draft.City ?? VenueRegistry.CityAt(new Point4D(targetted.X, targetted.Y, targetted.Z, self.M));
			draft.ShowOutline(self);
			D_Venue.Show(self, draft);
			return TargetResult.Done;
		}

		protected override void On_TargonCancel(Player self, object parameter) {
			D_Venue.Show(self, (VenueDraft) parameter);
		}
	}

	/// <summary>#1240 - the venues of a city, or all: go, edit.</summary>
	public class D_Venues : CompiledGumpDef {
		private const int PageSize = 14, LabelHue = 955, ValueHue = 52;
		private const int GoBase = 100, EditBase = 200, NextPage = 2, PrevPage = 3, Proposals = 4;

		public static void Show(Character gm, string city, int page) {
			gm.Dialog(gm, SingletonScript<D_Venues>.Instance, new DialogArgs(city ?? "", page));
		}

		internal static int Row(int page, int row) {
			return (page * PageSize) + row;
		}

		private static List<CityVenue> Listed(string city) {
			List<CityVenue> list = (city.Length == 0) ? new List<CityVenue>(VenueRegistry.All) : VenueRegistry.InCity(city);
			list.Sort((a, b) => string.Compare(a.VenueId, b.VenueId, StringComparison.OrdinalIgnoreCase));
			return list;
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			string city = (string) args[0];
			int page = Convert.ToInt32(args[1], CultureInfo.InvariantCulture);
			List<CityVenue> list = Listed(city);
			this.SetLocation(40, 40);
			this.AddPage(0);
			this.AddBackground(0, 0, 560, 120 + (PageSize * 22), 2620);
			this.AddLabel(14, 12, ValueHue, "Mista " + ((city.Length == 0) ? "vsech mest" : "mesta " + city) + " (" + list.Count + ")");
			int y = 44;
			for (int i = page * PageSize; (i < list.Count) && (i < (page + 1) * PageSize); i++) {
				CityVenue venue = list[i];
				int row = i - (page * PageSize); //ids by row on the page: absolute ones of a long list run into the next range
				this.AddButton(14, y, 4005, 4007, GoBase + row, GumpButtonType.Reply, 0);
				this.AddButton(46, y, 4011, 4013, EditBase + row, GumpButtonType.Reply, 0);
				this.AddLabel(84, y, LabelHue, venue.Label);
				this.AddLabel(330, y, ValueHue, (venue.City ?? "-") + ", " + venue.TileCount + " pol., role " + Residents.RoleCountOf(venue));
				y += 22;
			}
			if (page > 0) {
				this.AddButton(14, 80 + (PageSize * 22), 4014, 4016, PrevPage, GumpButtonType.Reply, 0);
			}
			if ((page + 1) * PageSize < list.Count) {
				this.AddButton(500, 80 + (PageSize * 22), 4005, 4007, NextPage, GumpButtonType.Reply, 0);
			}
			this.AddButton(200, 80 + (PageSize * 22), 4011, 4013, Proposals, GumpButtonType.Reply, 0); //#1315 - .venues navrh
			this.AddLabel(236, 82 + (PageSize * 22), LabelHue, "Budovy bez mista");
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			string city = (string) args[0];
			int page = Convert.ToInt32(args[1], CultureInfo.InvariantCulture);
			Character gm = (Character) gi.Cont;
			List<CityVenue> list = Listed(city);
			int button = gr.PressedButton;
			if (button == NextPage || button == PrevPage) {
				Show(gm, city, page + ((button == NextPage) ? 1 : -1));
			} else if (button == Proposals) {
				VenueCommands.Propose(gm, (city.Length > 0) ? city : VenueRegistry.CityAt(gm));
			} else if ((button >= EditBase) && (button < EditBase + PageSize) && (Row(page, button - EditBase) < list.Count)) {
				D_Venue.Show(gm, VenueDraft.For(list[Row(page, button - EditBase)]));
			} else if ((button >= GoBase) && (button < GoBase + PageSize) && (Row(page, button - GoBase) < list.Count)) {
				CityVenue venue = list[Row(page, button - GoBase)];
				gm.Go(new Point4D(venue.X, venue.Y, venue.Z, venue.M));
				Show(gm, city, page);
			}
		}
	}

	/// <summary>#1240 - rooms of a city that aren't venues yet (.venues navrh): go and look, mark one, or skip it for this list.</summary>
	public class D_VenueProposals : CompiledGumpDef {
		private const int PageSize = 14, LabelHue = 955, ValueHue = 52;
		private const int GoBase = 100, MarkBase = 300, SkipBase = 500, NextPage = 2, PrevPage = 3;

		public static void Show(Character gm, string city, List<FoundRoom> rooms, int page) {
			gm.Dialog(gm, SingletonScript<D_VenueProposals>.Instance, new DialogArgs(city, rooms, page));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			string city = (string) args[0];
			List<FoundRoom> rooms = (List<FoundRoom>) args[1];
			int page = Convert.ToInt32(args[2], CultureInfo.InvariantCulture);
			this.SetLocation(40, 40);
			this.AddPage(0);
			this.AddBackground(0, 0, 480, 120 + (PageSize * 22), 2620);
			this.AddLabel(14, 12, ValueHue, "Budovy bez mista, mesto " + city + " (" + rooms.Count + ")");
			int y = 44;
			for (int i = page * PageSize; (i < rooms.Count) && (i < (page + 1) * PageSize); i++) {
				FoundRoom room = rooms[i];
				int[] middle = room.Middle;
				int row = i - (page * PageSize);
				this.AddButton(14, y, 4005, 4007, GoBase + row, GumpButtonType.Reply, 0);
				this.AddButton(46, y, 4011, 4013, MarkBase + row, GumpButtonType.Reply, 0);
				this.AddButton(78, y, 4017, 4019, SkipBase + row, GumpButtonType.Reply, 0);
				this.AddLabel(116, y, LabelHue, middle[0] + "," + middle[1] + "," + middle[2]);
				this.AddLabel(260, y, ValueHue, room.Tiles.Count + " policek" + (room.TooBig ? " (cast)" : ""));
				y += 22;
			}
			this.AddLabel(14, 52 + (PageSize * 22), LabelHue, "Jit tam / Oznacit jako misto / Preskocit");
			if (page > 0) {
				this.AddButton(14, 80 + (PageSize * 22), 4014, 4016, PrevPage, GumpButtonType.Reply, 0);
			}
			if ((page + 1) * PageSize < rooms.Count) {
				this.AddButton(420, 80 + (PageSize * 22), 4005, 4007, NextPage, GumpButtonType.Reply, 0);
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			string city = (string) args[0];
			List<FoundRoom> rooms = (List<FoundRoom>) args[1];
			int page = Convert.ToInt32(args[2], CultureInfo.InvariantCulture);
			Character gm = (Character) gi.Cont;
			int button = gr.PressedButton;
			if (button == NextPage || button == PrevPage) {
				Show(gm, city, rooms, page + ((button == NextPage) ? 1 : -1));
			} else if ((button >= SkipBase) && (button < SkipBase + PageSize) && (D_Venues.Row(page, button - SkipBase) < rooms.Count)) {
				rooms.RemoveAt(D_Venues.Row(page, button - SkipBase));
				Show(gm, city, rooms, Math.Min(page, Math.Max(0, (rooms.Count - 1) / PageSize)));
			} else if ((button >= MarkBase) && (button < MarkBase + PageSize) && (D_Venues.Row(page, button - MarkBase) < rooms.Count)) {
				FoundRoom room = rooms[D_Venues.Row(page, button - MarkBase)];
				rooms.RemoveAt(D_Venues.Row(page, button - MarkBase));
				D_Venue.Show(gm, VenueDraft.OfRoom(room, room.M, city));
			} else if ((button >= GoBase) && (button < GoBase + PageSize) && (D_Venues.Row(page, button - GoBase) < rooms.Count)) {
				FoundRoom room = rooms[D_Venues.Row(page, button - GoBase)];
				int[] middle = room.Middle;
				gm.Go(new Point4D(middle[0], middle[1], middle[2], room.M));
				Show(gm, city, rooms, page);
			}
		}
	}
}
