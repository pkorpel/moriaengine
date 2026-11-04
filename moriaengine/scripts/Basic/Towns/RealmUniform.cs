using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1133 - Erebor's paid realm uniform (systems/uniforms.scp's e_uniform_realm, f_uniformRealm_*): for a number of
	/// months a player of skill rank 3 or more wears their realm's colours. Legs, gloves, chest, arms and cape make the uniform
	/// (helm, gorget and sash join in), each armor piece of verite or better, the cape and sash any; worn complete, each piece
	/// takes the realm's colour (Gondor's armor 0x57C and clothes 0x98F, Mordor's 0x5EB and 0x993) and the name "Gondorska
	/// Uniforma" or "Mordorska Uniforma", and gets its own back when taken off. A GM gives or lengthens it in months from a
	/// player's .info ("Uniforma"), .adminunif lists everyone who has one; a war lord who becomes a keep's captain gets 12
	/// months, and being dismissed as captain ends it, as on Erebor. Someone in an office's uniform (General, Nazgul) is left
	/// out, as Erebor's e_uniform went first.
	/// Not ported: Erebor's denyDye/denyScissoring item tags (nothing here reads them) and the player switch
	/// deniedUniform (nothing on Erebor set it).</summary>
	[HasSavedMembers]
	public static class RealmUniform {
		[SavedMember]
		private static List<RealmUniformEntry> entries = new List<RealmUniformEntry>();

		private static readonly TagKey origColorTK = TagKey.Acquire("_realmUniformColor_");
		private static readonly TagKey origNameTK = TagKey.Acquire("_realmUniformName_");
		private static readonly TimerKey refreshTK = TimerKey.Acquire("_realmUniformRefresh_");

		internal const int GondorArmor = 0x57C, GondorClothes = 0x98F, MordorArmor = 0x5EB, MordorClothes = 0x993;
		private const int LayerLegs = 4, LayerHelm = 6, LayerGloves = 7, LayerGorget = 10, LayerChest = 13, LayerSash = 17, LayerArms = 19, LayerCape = 20;
		private static readonly int[] relevantLayers = { LayerLegs, LayerHelm, LayerGloves, LayerGorget, LayerChest, LayerSash, LayerArms, LayerCape };
		private static readonly int[] requiredLayers = { LayerLegs, LayerGloves, LayerChest, LayerArms, LayerCape };
		private static readonly string[] clothDefs = { "i_cape", "i_sash" };
		private static readonly int[] clothModels = { 0x1530, 0x1542 };

		/// <summary>For a test: the date the uniform goes by.</summary>
		internal static DateTime? clock;

		private static DateTime Now {
			get { return clock ?? DateTime.Now; }
		}

		internal static RealmUniformEntry EntryOf(Character ch) {
			return entries.Find(e => e.player == ch);
		}

		/// <summary>f_uniformRealm_checkValidity: a uniform not run out. One that has is taken away.</summary>
		public static bool HasValid(Character ch) {
			RealmUniformEntry entry = EntryOf(ch);
			if (entry == null) {
				return false;
			}
			if (entry.until <= Now) {
				Remove(ch);
				return false;
			}
			return true;
		}

		/// <summary>f_uniformRealm_set, or f_uniformRealm_updateDate when one runs: months more.</summary>
		public static void Grant(Character ch, int months, Character editor) {
			if (!(ch is Player player) || (months == 0)) {
				return;
			}
			RealmUniformEntry entry = HasValid(player) ? EntryOf(player) : null;
			if (entry == null) {
				entry = new RealmUniformEntry { player = player, until = Now.AddMonths(months) };
				entries.Add(entry);
			} else {
				entry.until = entry.until.AddMonths(months);
			}
			entry.editor = editor as Player;
			Logger.WriteInfo(true, ((editor == null) ? "" : (editor.Name + ": ")) + player.Name + "'s realm uniform until " + entry.until.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
			Refresh(player);
		}

		/// <summary>f_uniformRealm_removeEvent.</summary>
		public static void Remove(Character ch) {
			RealmUniformEntry entry = EntryOf(ch);
			if (entry == null) {
				return;
			}
			entries.Remove(entry);
			foreach (int layer in relevantLayers) {
				if (ch.FindLayer(layer) is Item item) {
					Restore(item);
				}
			}
			Logger.WriteInfo(true, ch.Name + "'s realm uniform ends");
		}

		private static bool Qualifies(Item item, int layer) {
			if ((layer == LayerCape) || (layer == LayerSash)) {
				foreach (string defname in clothDefs) {
					if (item.Def == ThingDef.GetByDefname(defname)) {
						return true;
					}
				}
				return Array.IndexOf(clothModels, (int) item.Model) >= 0;
			}
			return (item.Def is IObjectWithMaterial made) && (made.MaterialType != MaterialType.Wood) && (made.Material >= Material.Verite);
		}

		/// <summary>e_uniform_realm @itemEquip and @itemUnequip: the worn pieces as they should look now.</summary>
		public static void Refresh(Character ch) {
			if (EntryOf(ch) == null) {
				return;
			}
			bool on = HasValid(ch) && (ch is Player player) && ((int) player.SkillRank >= (int) SkillRank.Grandmaster) &&
				((ch.Realm == Realm.Gondor) || (ch.Realm == Realm.Mordor)) && (PowerBadge.Find(ch) == null);
			Dictionary<int, Item> pieces = new Dictionary<int, Item>();
			foreach (int layer in relevantLayers) {
				if ((ch.FindLayer(layer) is Item item) && Qualifies(item, layer)) {
					pieces[layer] = item;
				}
			}
			foreach (int layer in requiredLayers) {
				on &= pieces.ContainsKey(layer);
			}
			foreach (KeyValuePair<int, Item> piece in pieces) {
				if (on) {
					Dress(piece.Value, piece.Key, ch.Realm);
				} else {
					Restore(piece.Value);
				}
			}
		}

		/// <summary>i_uni_unequiptimer: a moment after the equipping is over.</summary>
		public static void RefreshSoon(Character ch) {
			if ((EntryOf(ch) != null) && (ch.GetTimer(refreshTK) == null)) {
				ch.AddTimer(refreshTK, new RealmUniformRefreshTimer()).DueInSeconds = 0.1;
			}
		}

		/// <summary>f_uniformRealm_giveColor.</summary>
		private static void Dress(Item item, int layer, Realm realm) {
			if (item.GetTag(origColorTK) == null) {
				item.SetTag(origColorTK, item.Color);
			}
			if (item.GetTag(origNameTK) == null) {
				item.SetTag(origNameTK, item.Name);
			}
			bool gondor = realm == Realm.Gondor;
			item.Name = gondor ? "Gondorska Uniforma" : "Mordorska Uniforma";
			bool cloth = (layer == LayerCape) || (layer == LayerSash);
			item.Color = cloth ? (gondor ? GondorClothes : MordorClothes) : (gondor ? GondorArmor : MordorArmor);
		}

		/// <summary>f_uniformRealm_removeColor: the piece's own colour and name back.</summary>
		public static void Restore(AbstractItem item) {
			object color = item.GetTag(origColorTK);
			if (color != null) {
				item.Color = Convert.ToInt32(color, CultureInfo.InvariantCulture);
				item.RemoveTag(origColorTK);
			}
			if (item.GetTag(origNameTK) is string name) {
				item.Name = name;
				item.RemoveTag(origNameTK);
			}
		}

		internal static List<RealmUniformEntry> All {
			get {
				entries.RemoveAll(e => (e.player == null) || e.player.IsDeleted);
				return entries;
			}
		}

		/// <summary>adminunif: the list of everyone with a uniform.</summary>
		[SteamFunction]
		public static void AdminUnif(Character self) {
			if (self is Player gm) {
				gm.Dialog(gm, SingletonScript<D_RealmUniforms>.Instance, new DialogArgs(0));
			}
		}

		internal static void ShowFor(Player gm, Character who) {
			gm.Dialog(gm, SingletonScript<D_RealmUniform>.Instance, new DialogArgs(who));
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class RealmUniformEntry {
		[SaveableData, CopyableData]
		public Player player;

		[SaveableData, CopyableData]
		public DateTime until;

		[SaveableData, CopyableData]
		public Player editor;

		[LoadingInitializer, DeepCopyImplementation]
		public RealmUniformEntry() {
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class RealmUniformRefreshTimer : BoundTimer {
		[DeepCopyImplementation, LoadingInitializer]
		public RealmUniformRefreshTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			RealmUniform.Refresh((Character) cont);
		}
	}

	/// <summary>d_uniform_realm: "Platnost do", months to add (entry 1), cancel it (2), OK (1).</summary>
	public class D_RealmUniform : CompiledGumpDef {
		internal const int OkButton = 1, CancelUniformButton = 2, MonthsEntry = 1;
		private const int Width = 362, Row = 22, Hue = 955, TitleHue = 42;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Character who = (Character) args[0];
			RealmUniformEntry entry = RealmUniform.HasValid(who) ? RealmUniform.EntryOf(who) : null;
			this.SetLocation(225, 5);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, 152, 5054);
			this.AddLabel(10, 10, TitleHue, "Nastaveni placenych uniforem - " + who.Name);
			this.AddLabel(10, 10 + Row, Hue, "Platnost do:");
			this.AddLabel(145, 10 + Row, Hue, (entry == null) ? "- uniforma neni aktivni -" : entry.until.ToString("d.M.yyyy", CultureInfo.InvariantCulture));
			this.AddLabel(10, 10 + (2 * Row), Hue, "Pridat mesice:");
			this.AddTextEntry(145, 10 + (2 * Row), 198, 20, Hue, MonthsEntry, "0");
			this.AddLabel(10, 10 + (3 * Row), Hue, "Zrusit platnost:");
			this.AddButton(145, 10 + (3 * Row), 0xfb4, 0xfb6, CancelUniformButton, GumpButtonType.Reply, 0);
			this.AddButton(13, 117, 0xfb7, 0xfb9, OkButton, GumpButtonType.Reply, 0);
			this.AddButton(46, 117, 0xfb1, 0xfb3, 0, GumpButtonType.Reply, 0);
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Character who = (Character) args[0];
			if (!(gi.Cont is Player gm) || !gm.IsGM) {
				return;
			}
			if (gr.PressedButton == 0) {
				gm.SysMessage("Nastaveni stornovano");
				return;
			}
			if (gr.PressedButton == CancelUniformButton) {
				RealmUniform.Remove(who);
			} else if (gr.PressedButton == OkButton) {
				int.TryParse((gr.GetTextResponse(MonthsEntry) ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int months);
				if (months == 0) {
					return;
				}
				RealmUniform.Grant(who, months, gm);
			}
			RealmUniform.ShowFor(gm, who);
		}
	}

	/// <summary>d_uniform_realm_info: everyone with a uniform, 15 a page; a row's button opens that player's uniform.</summary>
	public class D_RealmUniforms : CompiledGumpDef {
		private const int Width = 735, Rows = 15, Row = 20, FirstRowButton = 100, PreviousButton = 1, NextButton = 2, Hue = 2301, HeadHue = 955;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			int page = (int) args[0];
			List<RealmUniformEntry> all = RealmUniform.All;
			this.SetLocation(25, 5);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, 90 + (Rows * Row), 5054);
			this.AddLabel(10, 10, 42, "Seznam placenych uniforem");
			this.AddLabel(10, 35, HeadHue, "Num:");
			this.AddLabel(60, 35, HeadHue, "Postava:");
			this.AddLabel(244, 35, HeadHue, "Account:");
			this.AddLabel(375, 35, HeadHue, "Platnost do:");
			this.AddLabel(552, 35, HeadHue, "Editoval:");
			int first = page * Rows;
			for (int i = first; (i < all.Count) && (i < first + Rows); i++) {
				RealmUniformEntry entry = all[i];
				int y = 58 + ((i - first) * Row);
				this.AddLabel(10, y, Hue, (i + 1).ToString(CultureInfo.InvariantCulture));
				this.AddLabel(60, y, Hue, entry.player.Name);
				this.AddButton(205, y, 0xfa5, 0xfa7, FirstRowButton + i, GumpButtonType.Reply, 0);
				this.AddLabel(244, y, Hue, (entry.player.Account == null) ? "" : entry.player.Account.Name);
				this.AddLabel(375, y, Hue, entry.until.ToString("d.M.yyyy", CultureInfo.InvariantCulture));
				this.AddLabel(552, y, Hue, (entry.editor == null) ? "" : entry.editor.Name);
			}
			if (page > 0) {
				this.AddButton(Width - 30, 39, 0x983, 0x984, PreviousButton, GumpButtonType.Reply, 0);
			}
			if (all.Count > first + Rows) {
				this.AddButton(Width - 30, 58 + (Rows * Row), 0x985, 0x986, NextButton, GumpButtonType.Reply, 0);
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			int page = (int) args[0];
			if (!(gi.Cont is Player gm) || !gm.IsGM) {
				return;
			}
			List<RealmUniformEntry> all = RealmUniform.All;
			int button = gr.PressedButton;
			if (button == PreviousButton) {
				gm.Dialog(gm, this, new DialogArgs(Math.Max(0, page - 1)));
			} else if (button == NextButton) {
				gm.Dialog(gm, this, new DialogArgs(page + 1));
			} else if ((button >= FirstRowButton) && ((button - FirstRowButton) < all.Count)) {
				RealmUniform.ShowFor(gm, all[button - FirstRowButton].player);
			}
		}
	}
}
