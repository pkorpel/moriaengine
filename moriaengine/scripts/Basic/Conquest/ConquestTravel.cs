using System;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1116 - Erebor's battlefield rules for Recall and Gate Travel (sphereevents.scp's e_allplayers @spellcast), checked
	/// when the spell starts, GMs aside:
	/// - onto a rune in a keep's battlefield, only a player of the realm holding the keep ("Toto uzemi bys napred musel dobyt."),
	///   and not while a war on that keep runs or on its day from an hour before its start ("Na uzemi, na kterem brzy Gondor
	///   zacne (nebo uz zacal) valku, se nelze premistovat.");
	/// - a gate from inside a keep's battlefield, not at those times ("Z uzemi, kteremu Gondor vyhlasil valku, nelze otvirat
	///   cestovni brany."). Recall out isn't limited, as on Erebor.
	/// Fixed from Erebor: Mordor's gate-out check read Gondor's declarator's day, and both compared the day of the month alone.</summary>
	public static class ConquestTravel {
		/// <summary>The keep of the battlefield at a point, null for none.</summary>
		public static string FortAt(IPoint4D point) {
			FlaggedRegion area = RegionEventSystem.AreaOf(point.GetMap().GetRegionFor(point));
			return ((area == null) || (area.Battlefield == 0)) ? null : Conquest.FortName(area.Battlefield);
		}

		/// <summary>A war on the keep runs, or it's the war's day and the hour before its start has come.</summary>
		internal static bool WarIsNear(WarFront front, string fort) {
			if (front.declaredOn != fort) {
				return false;
			}
			if (front.IsRunning) {
				return true;
			}
			DateTime now = Conquest.Now;
			return (now.Date == front.begins.Date) && (now >= front.begins.AddHours(-1));
		}

		public static bool AllowsIn(Character caster, IPoint4D destination) {
			if (caster.IsGM) {
				return true;
			}
			string fort = FortAt(destination.TopPoint);
			if (fort == null) {
				return true;
			}
			if (Conquest.Keep(fort).owner != caster.Realm) {
				caster.RedMessage("Toto uzemi bys napred " + Conquest.Word(caster, "musel", "musela") + " dobyt.");
				return false;
			}
			foreach (Realm realm in new[] { Realm.Gondor, Realm.Mordor }) {
				if (WarIsNear(Conquest.Front(realm), fort)) {
					caster.RedMessage("Na uzemi, na kterem brzy " + Conquest.RealmName(realm) + " zacne (nebo uz zacal) valku, se nelze premistovat.");
					return false;
				}
			}
			return true;
		}

		public static bool AllowsGateOut(Character caster) {
			if (caster.IsGM) {
				return true;
			}
			string fort = FortAt(caster);
			if (fort == null) {
				return true;
			}
			foreach (Realm realm in new[] { Realm.Gondor, Realm.Mordor }) {
				if (WarIsNear(Conquest.Front(realm), fort)) {
					caster.RedMessage("Z uzemi, kteremu " + Conquest.RealmName(realm) + " vyhlasil valku, nelze otvirat cestovni brany.");
					return false;
				}
			}
			return true;
		}
	}
}
