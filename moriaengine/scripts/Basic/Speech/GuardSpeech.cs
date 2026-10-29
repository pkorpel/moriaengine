/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#826 - Erebor's [speech spk_guard] (systems/npc_ai.scp), on 76 guard chardefs: saying "guard" or "straz"
	/// near a guard sends it after whoever the speaker has been fighting (FightMemory, Sphere's MEMORY_FIGHT link) -
	/// unless that one is under this guard's protection (Sphere's ismyguard):
	/// <list type="bullet">
	/// <item>a Gondor guard (archive tag.guard=1) protects Gondor players who are neither criminal nor murderers,</item>
	/// <item>a Mordor guard (tag.guard=2) protects every Mordor player.</item>
	/// </list>
	/// Only players are targeted, never an invulnerable one. Every guard in earshot answers on its own, as each NPC
	/// hears on its own in Sphere. Replaces the converted [SpeechStub spk_guard].
	/// Not ported, because the engine has no such system: town citizenship, blacklists and realm/keep bans
	/// (isbannedplayer, isfrommytown - so a criminal is never "from this guard's town"), Mystic hypnosis
	/// (tag hypno_mystik), and the guard's temporary karma (guardkarma_*, i_memory_cleaner). The archive's
	/// teleport-to-target (e_tele_to_target/i_attack_guard) only ever fires in its 7 insta-logout areas (jail,
	/// tutorial) - guards here walk to their target as every NPC does.</summary>
	public sealed class GuardSpeech : AbstractSpeech {
		private static readonly string[] keywords = { "guard", "straz" };

		public static void Bootstrap() {
			new GuardSpeech("spk_guard").Register();
		}

		private GuardSpeech(string defname)
			: base(defname) {
		}

		protected override SpeechResult Handle(AbstractCharacter listener, SpeechArgs speechArgs) {
			return this.TryHandle(listener, speechArgs);
		}

		protected override SpeechResult TryHandle(AbstractCharacter listener, SpeechArgs speechArgs) {
			NPC guard = listener as NPC;
			Character caller = speechArgs.Speaker as Character;
			if ((guard != null) && (caller != null) && IsGuardCall(speechArgs.Speech)) {
				TryAnswerCall(guard, caller);
			}
			return SpeechResult.IgnoredOrActedUpon; //the other guards in earshot answer too
		}

		//Sphere's on=*guard* / on=*straz*
		private static bool IsGuardCall(string speech) {
			if (speech == null) {
				return false;
			}
			foreach (string keyword in keywords) {
				if (speech.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0) {
					return true;
				}
			}
			return false;
		}

		/// <summary>Sends `guard` after the caller's fight opponent, if the guard doesn't protect that one. True if it did.</summary>
		public static bool TryAnswerCall(NPC guard, Character caller) {
			if (guard.IsDeleted || guard.Flag_Dead || (guard.ControlMaster != null)) {
				return false;
			}
			Character target = FightMemory.GetOpponent(caller, TimeSpan.FromSeconds(NpcAggroSettings.instance.guardCallMemorySeconds));
			if ((target == null) || (target == guard) || !target.IsPlayer || target.Flag_Invulnerable) {
				return false;
			}
			if (IsProtectedBy(guard, target)) {
				return false;
			}
			NPCAiTimer timer = NPCAiTimer.GetFor(guard);
			if (timer == null) {
				return false;
			}
			guard.Say(Shout(guard, target));
			timer.EngageTarget(target);
			return true;
		}

		/// <summary>Sphere's ismyguard: whether `guard` protects `target`. A guard of no realm protects everyone
		/// (it isn't a realm guard, so it never takes sides).</summary>
		public static bool IsProtectedBy(NPC guard, Character target) {
			switch (guard.Realm) {
				case Realm.Gondor:
					return (target.Realm == Realm.Gondor) && !Notoriety.IsCriminal(target) && !Notoriety.IsMurderer(target);
				case Realm.Mordor:
					return target.Realm == Realm.Mordor;
				default:
					return true;
			}
		}

		//Erebor's dorand 2 lines, in its own Czech: "<name>, leave or die!" / "<name>, you have no business in <region>!"
		private static string Shout(NPC guard, Character target) {
			Region region = guard.Region;
			string regionName = (region == null) ? null : region.Name;
			if (string.IsNullOrEmpty(regionName) || (Globals.dice.Next(2) == 0)) {
				return target.Name + ", odejdi nebo zemri!";
			}
			return target.Name + ", v " + regionName + " nemas co pohledavat!";
		}
	}
}
