using SteamEngine.Common;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {
	/// <summary>#905 - Erebor's remember, remembered and rememberclear (starostove.scp). A ban goes by name, and a mask
	/// changes the name: .remember keeps a player and the name they have now. .remembered answers the open cursor with them
	/// from anywhere (Sphere's LAST, as Erebor's), and a blacklist or a suggestion then files the remembered name
	/// (navrhni, blacklist_him, realmban_him). .rememberclear forgets. Fixed from Erebor: its remembered answered any cursor
	/// from anywhere (a pact, a house friend, a party invite...); here a player's answers only the blacklist cursors
	/// (IRememberedTarget), the use Erebor gave it; a GM's any. Erebor's blacklist_him then overwrote the remembered name
	/// with the current one (nazguls.scp), so a mayor's or a Nazgul's own ban ignored it; here all three honour it.</summary>
	public static class Remember {
		private static readonly TagKey whoTK = TagKey.Acquire("_remember_who_");
		private static readonly TagKey nameTK = TagKey.Acquire("_remember_name_");

		/// <summary>The name to file for `target`: the remembered one when it's the remembered player.</summary>
		public static string NameFor(Character self, Character target) {
			if ((self.GetTag(whoTK) == target) && (self.GetTag(nameTK) is string name)) {
				return name;
			}
			return target.Name;
		}

		internal static void Keep(Character self, Character who) {
			self.SetTag(whoTK, who);
			self.SetTag(nameTK, who.Name); //he may put on another mask meanwhile
		}

		[SteamFunction("Remember")]
		public static void RememberCommand(Character self) {
			if (self is Player player) {
				player.Target(SingletonScript<Targ_Remember>.Instance);
			}
		}

		[SteamFunction]
		public static void Remembered(Character self) {
			if (!(self.GetTag(whoTK) is Character who) || who.IsDeleted) {
				self.SysMessage("Nemas nikoho ulozeneho v pameti");
				return;
			}
			GameState state = self.GameState;
			if ((state == null) || (state.PendingTargetOwner == null)) {
				self.SysMessage("Nejdriv otevri kurzor navrhu nebo blacklistu.");
			} else if (!self.IsGM && !(state.PendingTargetOwner is IRememberedTarget)) {
				self.SysMessage("Pamet pouzijes jen pro navrh nebo zapis na blacklist.");
			} else {
				state.AnswerTarget(who);
			}
		}

		[SteamFunction]
		public static void RememberClear(Character self) {
			self.RemoveTag(whoTK);
			self.RemoveTag(nameTK);
		}
	}

	/// <summary>#905 - a cursor .remembered may answer for a player: the blacklists'.</summary>
	public interface IRememberedTarget {
	}

	/// <summary>i_remember</summary>
	public class Targ_Remember : CompiledTargetDef {
		protected override void On_Start(Player self, object parameter) {
			self.SysMessage("Koho si chces pamatovat?");
			base.On_Start(self, parameter);
		}

		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			if (!targetted.IsPlayer) {
				self.SysMessage("Zameruj hrace");
				return TargetResult.RestartTargetting;
			}
			Remember.Keep(self, targetted);
			self.SysMessage(targetted.Name + " " + (targetted.IsFemale ? "zapamatovana" : "zapamatovan") + ".");
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			self.SysMessage("Zameruj lidi");
			return TargetResult.RestartTargetting;
		}
	}
}
