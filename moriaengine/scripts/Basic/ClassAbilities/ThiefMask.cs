using System;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1017, #914 - the thief's mask, specabilities.scp: a second identity, its own name, karma,
	/// murders, realm and home, swapped with the real one while hidden. Erebor's maskattribs, "name,karma,kills,realm",
	/// with the home in maskHome.</summary>
	public static class ThiefMask {
		private static readonly TagKey maskTK = TagKey.Acquire("_maskAttribs_");
		private static readonly TagKey maskHomeTK = TagKey.Acquire("_maskHome_");
		private static readonly TagKey nextMaskTK = TagKey.Acquire("_nextMask_");
		private const byte CampPlane = 1;

		internal static string[] Attributes(Character self) {
			string stored = self.GetTag(maskTK) as string;
			return string.IsNullOrEmpty(stored) ? null : stored.Split('|');
		}

		internal static void SetAttributes(Character self, string name, int karma, int murders, Realm realm) {
			self.SetTag(maskTK, string.Join("|", name, karma.ToString(CultureInfo.InvariantCulture),
				murders.ToString(CultureInfo.InvariantCulture), ((int) realm).ToString(CultureInfo.InvariantCulture)));
		}

		[SteamFunction]
		public static void Mask(Player self) {
			ClassAbilitySettings settings = ClassAbilitySettings.instance;
			FlaggedRegion area = RegionEventSystem.AreaOf(self.Region);
			string[] mask = Attributes(self);
			if (self.IsGM) {
				ShowMask(self, self);
			}
			if (!ThiefAbilities.IsThief(self) && !self.IsGM) {
				self.SysMessage("Nemuzes pouzit tuto schopnost.", ClassAbilities.ClassMessageHue);
			} else if (NazgulRings.IsNazgul(self)) {
				self.SysMessage("Moc prstenu te zcela pohltila. Nemuzes pouzit svou gondorskou identitu", ClassAbilities.ClassMessageHue);
			} else if (self.Level < 5) {
				self.SysMessage("Na pouziti teto schopnosti jeste nejsi dost " + (self.IsFemale ? "zkusena" : "zkuseny") + ".", ClassAbilities.ClassMessageHue);
			} else if ((area != null) && ((area.RealmTag == 4) || (area.HouseNumber != 0))) {
				self.SysMessage("Zde se nelze premaskovat!", ClassAbilities.ClassMessageHue);
			} else if (!self.Flag_Hidden && !self.IsGM) {
				self.SysMessage("Pozor! Nesmi te pri tom videt.", ClassAbilities.ClassMessageHue);
			} else if (self.M == CampPlane) {
				self.SysMessage("v lagru se nelze premaskovat", ClassAbilities.ClassMessageHue);
			} else if (mask == null) {
				self.SysMessage("Vyber si sve druhe jmeno.", ClassAbilities.ClassMessageHue);
				self.Dialog(self, SingletonScript<D_MaskName>.Instance, new DialogArgs());
			} else if ((ClassAbilities.SecondsTag(self, nextMaskTK) > Globals.TimeInSeconds) && !self.IsGM) {
				int left = (int) (ClassAbilities.SecondsTag(self, nextMaskTK) - Globals.TimeInSeconds);
				self.SysMessage("Pokusit se o premaskovani muzes znova za " + (left / 60) + " minut, " + (left % 60) + " sekund.", ClassAbilities.ClassMessageHue);
			} else if ((MaskRealm(mask) != self.Realm) && (area != null) && (area.Realm != Realm.None) && (area.Realm == self.Realm)) {
				self.SysMessage("Nemuzes pouzit masku na domovskem uzemi.", ClassAbilities.ClassMessageHue);
			} else if (!self.IsGM && (Globals.dice.Next(100001) > CombatCalculator.CalculateClassAbility(self.EffectiveLevel))) {
				self.SetTag(nextMaskTK, Globals.TimeInSeconds + settings.maskFailSeconds);
				self.SysMessage("Nepovedlo se ti premaskovat se.", ClassAbilities.ClassMessageHue);
			} else {
				self.SetTag(nextMaskTK, Globals.TimeInSeconds + settings.maskSuccessSeconds);
				self.SysMessage("Uspesne jsi se " + (self.IsFemale ? "premaskovala" : "premaskoval") + ".", ClassAbilities.ClassMessageHue);
				Swap(self, mask);
			}
		}

		private static Realm MaskRealm(string[] mask) {
			return (Realm) int.Parse(mask[3], CultureInfo.InvariantCulture);
		}

		/// <summary>maskparse, with the current identity stored as the next mask.</summary>
		internal static void Swap(Player self, string[] mask) {
			string name = self.Name;
			int karma = self.Karma, murders = Notoriety.GetMurderCount(self);
			Realm realm = self.Realm;
			self.Name = mask[0];
			self.Karma = short.Parse(mask[1], CultureInfo.InvariantCulture);
			Notoriety.SetMurderCount(self, int.Parse(mask[2], CultureInfo.InvariantCulture));
			self.Realm = MaskRealm(mask);
			SetAttributes(self, name, karma, murders, realm);
			Point4D otherHome = self.GetTag(maskHomeTK) as Point4D;
			Point4D home = self.Home;
			if (otherHome != null) {
				self.Home = otherHome;
			}
			if (home != null) {
				self.SetTag(maskHomeTK, home);
			}
			self.Resend();
		}

		/// <summary>showMask, for a GM.</summary>
		internal static void ShowMask(Character gm, Character of) {
			string[] mask = Attributes(of);
			if (mask == null) {
				gm.SysMessage("Tento hrac nema nastavenou masku");
			} else {
				gm.SysMessage("Jmeno masky je: " + mask[0]);
				gm.SysMessage("Realm masky je: " + MaskRealm(mask));
			}
		}

		/// <summary>clearmask.</summary>
		[SteamFunction]
		public static void ClearMask(Character self) {
			self.RemoveTag(maskTK);
		}
	}
}

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>d_maskname: the second name, for good.</summary>
	public class D_MaskName : CompiledGumpDef {
		private const int OkButton = 1, NameEntry = 3;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			this.SetLocation(190, 175);
			this.AddPage(0);
			this.AddBackground(0, 0, 330, 120, 2620);
			this.AddLabel(60, 20, 995, "Jake ma byt tve druhe jmeno?");
			this.AddLabel(40, 40, 995, "(vybirej peclive, zustane ti naporad...)");
			this.AddBackground(30, 60, 270, 22, 3000);
			this.AddTextEntry(35, 61, 253, 25, 0, NameEntry, "");
			this.AddButton(150, 85, 2104, 2103, OkButton, GumpButtonType.Reply, 0);
			this.AddLabel(165, 80, 0, "OK");
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = (Player) gi.Cont;
			string name = (gr.GetTextResponse(NameEntry) ?? "").Trim();
			if (gr.PressedButton != OkButton) {
				player.SysMessage("Jmeno zustalo nenastaveno.");
				return;
			}
			if ((name.Length == 0) || string.Equals(name, "Man", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Woman", StringComparison.OrdinalIgnoreCase)) {
				player.SysMessage("Nemuzes se jmenovat " + name + "...", ClassAbilities.ClassMessageHue);
				player.Dialog(player, this, new DialogArgs());
				return;
			}
			ThiefMask.SetAttributes(player, name, player.Karma, Notoriety.GetMurderCount(player), player.Realm);
			player.SysMessage("Jmeno nastaveno, muzes se premaskovat.", ClassAbilities.ClassMessageHue);
		}
	}
}
