using System;
using System.Globalization;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts.Dialogs {
	/// <summary>#1315 - an NPC's home in its .info: where its AI walks back to, and how far it wanders from it. A GM types a new
	/// home ("x,y,z" or "x,y,z,m") or takes the spot where the NPC stands. A city resident's spot at work is where its gem
	/// stands, so for one the point must be inside its workplace and the gem moves there (ResidentSchedule.SpotFor): the
	/// schedule and a new body after a death keep it; its leash is the schedule's. Any other NPC keeps the new home until it
	/// dies (a spawn's new one starts at the spawn).</summary>
	[ViewDescriptor(typeof(NPC), "NPC")]
	public static class NPCDescriptor {
		[GetMethod("Home", typeof(string))]
		public static object GetHome(object target) {
			NPCAiTimer ai = NPCAiTimer.GetFor((NPC) target);
			return ((ai == null) || !ai.hasHome) ? "" : string.Join(",", ai.homeX.ToString(CultureInfo.InvariantCulture), ai.homeY.ToString(CultureInfo.InvariantCulture),
				ai.homeZ.ToString(CultureInfo.InvariantCulture), ai.homeM.ToString(CultureInfo.InvariantCulture));
		}

		[SetMethod("Home", typeof(string))]
		public static void SetHome(object target, object value) {
			NPC npc = (NPC) target;
			string[] parts = Convert.ToString(value, CultureInfo.InvariantCulture).Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
			int[] n = new int[4];
			n[3] = npc.M;
			if ((parts.Length < 3) || (parts.Length > 4)) {
				throw new SEException("Domov se zadava jako x,y,z nebo x,y,z,m");
			}
			for (int i = 0; i < parts.Length; i++) {
				if (!int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out n[i])) {
					throw new SEException("Domov se zadava jako x,y,z nebo x,y,z,m");
				}
			}
			if ((n[0] < 0) || (n[0] > ushort.MaxValue) || (n[1] < 0) || (n[1] > ushort.MaxValue) || (n[2] < sbyte.MinValue) || (n[2] > sbyte.MaxValue) || (n[3] < 0) || (n[3] > byte.MaxValue)) {
				throw new SEException("Domov mimo mapu");
			}
			MoveHome(npc, new Point4D(n[0], n[1], n[2], (byte) n[3]));
		}

		[GetMethod("Home leash", typeof(int))]
		public static object GetLeash(object target) {
			NPCAiTimer ai = NPCAiTimer.GetFor((NPC) target);
			return (ai == null) ? 0 : ai.WanderLeash;
		}

		[SetMethod("Home leash", typeof(int))]
		public static void SetLeash(object target, object value) {
			NPCAiTimer ai = NPCAiTimer.GetFor((NPC) target);
			if (ai == null) {
				throw new SEException("Tahle postava nema AI");
			}
			if (Residents.GemOf((NPC) target) != null) {
				throw new SEException("Obyvateli mesta nastavuje volnost rozvrh (uvnitr 2, venku 4)");
			}
			ai.WanderLeash = Math.Max(0, Convert.ToInt32(value, CultureInfo.InvariantCulture));
		}

		/// <summary>The home where the NPC stands now.</summary>
		[Button("Home here")]
		public static void HomeHere(object target) {
			NPC npc = (NPC) target;
			MoveHome(npc, new Point4D(npc.X, npc.Y, npc.Z, npc.M));
		}

		internal static void MoveHome(NPC npc, Point4D home) {
			NPCAiTimer ai = NPCAiTimer.GetFor(npc);
			if (ai == null) {
				throw new SEException("Tahle postava nema AI");
			}
			Item gem = Residents.GemOf(npc);
			if (gem != null) {
				CityVenue work = Residents.WorkplaceOf(gem);
				if ((work == null) || (work.M != home.M) || !work.Contains(home.X, home.Y, home.Z)) {
					throw new SEException("Obyvateli jde misto zmenit jen v jeho praci (" + ((work == null) ? "zadna" : work.Label) + "): bod musi byt uvnitr");
				}
				gem.P(home.X, home.Y, home.Z, home.M);
				ResidentSchedule.Apply(gem, npc, false);
				if ((ResidentSchedule.PlaceFor(gem, VenueClock.Shift) != work) && (Globals.SrcCharacter is Character gm)) {
					gm.SysMessage("Ted je jinde, nove misto v praci plati, az tam bude.");
				}
				return;
			}
			ai.homeX = home.X;
			ai.homeY = home.Y;
			ai.homeZ = (sbyte) home.Z;
			ai.homeM = home.M;
			ai.hasHome = true;
		}
	}
}
