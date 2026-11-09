using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1331 - Erebor's .presun (funkce.scp, [function presun] and its memory i_presouvatko): a GM targets a character or
	/// an item, then where it goes: the ground, a static, or another thing (its place). The item leaves a container or a wearer for
	/// the ground there; a character is teleported. Erebor's memory removed itself after 60 s; a cursor here needs no cleanup, and
	/// a first target on the ground asks again instead of silently ending.</summary>
	public static class PresunCommand {
		[SteamFunction]
		public static void Presun(Player self) {
			self.SysMessage("Co chces presunout?");
			self.Target(SingletonScript<Targ_Presun>.Instance);
		}

		/// <summary>The thing to its new place, at the GM's say; false when it's gone meanwhile.</summary>
		internal static bool MoveTo(Player gm, Thing what, int x, int y, int z, byte m) {
			if ((what == null) || what.IsDeleted) {
				gm.SysMessage("Uz to neexistuje.");
				return false;
			}
			if (what is Character ch) {
				Logger.WriteInfo(true, gm.Name + " moved " + ch.Name + " (" + ch.Uid + ") from " + ch.P() + " to " + x + "," + y + "," + z + "," + m + " (.presun)");
			}
			what.P(x, y, z, m);
			gm.SysMessage("Presunuto.");
			return true;
		}
	}

	/// <summary>First the thing (no parameter), then its place (the thing as the parameter).</summary>
	public class Targ_Presun : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			return this.Picked(self, targetted, parameter, "Kam s nim?");
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			return this.Picked(self, targetted, parameter, "Kam s tim?");
		}

		private TargetResult Picked(Player self, Thing targetted, object parameter, string where) {
			if (parameter is Thing what) {
				Thing top = targetted.TopObj(); //an item in a pack: where the pack's owner stands, as Sphere's TARGP
				PresunCommand.MoveTo(self, what, top.X, top.Y, top.Z, top.M);
				return TargetResult.Done;
			}
			self.SysMessage(where);
			self.Target(this, targetted);
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			if (parameter is Thing what) {
				PresunCommand.MoveTo(self, what, targetted.X, targetted.Y, targetted.Z, self.M);
				return TargetResult.Done;
			}
			self.SysMessage("Vyber postavu nebo predmet.");
			return TargetResult.RestartTargetting;
		}
	}
}
