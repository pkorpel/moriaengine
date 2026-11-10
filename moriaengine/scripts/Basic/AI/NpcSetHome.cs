using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1228 item 9: .sethome, a GM's way to give an NPC a new home, as Sphere's HOME. A GM-moved NPC keeps its old home
	/// (#1206, the user's choice) and walks back to it; this is what moves it. First the NPC, then the new home: a spot on the ground,
	/// a thing (its place), or the NPC itself again (where it stands). The same rules as the .info dialog's Home
	/// (NPCDescriptor.MoveHome): a city resident's home is its spot at work, so it must be inside its workplace. A spawned NPC's
	/// next body starts at its spawn again.</summary>
	public static class NpcSetHomeCommand {
		private static readonly TagKey spawnGemTK = TagKey.Acquire("spawnGem");

		[SteamFunction]
		public static void SetHome(Player self) {
			self.SysMessage("Komu nastavit domov?");
			self.Target(SingletonScript<Targ_SetHome>.Instance);
		}

		internal static void Set(Player gm, NPC npc, int x, int y, int z, byte m) {
			if (npc.IsDeleted) {
				gm.SysMessage("Uz neexistuje.");
				return;
			}
			try {
				NPCDescriptor.MoveHome(npc, new Point4D(x, y, z, m));
			} catch (SEException e) {
				gm.SysMessage(e.Message);
				return;
			}
			Logger.WriteInfo(true, gm.Name + " set the home of " + npc.Name + " (" + npc.Uid + ") to " + x + "," + y + "," + z + "," + m + " (.sethome)");
			gm.SysMessage("Domov nastaven: " + x + "," + y + "," + z + "," + m + "." +
				((npc.GetTag(spawnGemTK) is Thing) ? " Plati, dokud zije: dalsi ze spawnu zacne u spawnu." : ""));
		}
	}

	/// <summary>First the NPC (no parameter), then its home (the NPC as the parameter).</summary>
	public class Targ_SetHome : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			if (parameter is NPC npc) {
				NpcSetHomeCommand.Set(self, npc, targetted.X, targetted.Y, targetted.Z, targetted.M);
				return TargetResult.Done;
			}
			if (targetted is NPC chosen) {
				self.SysMessage("Kde ma byt jeho domov? (Na nej samotneho = kde stoji.)");
				self.Target(this, chosen);
				return TargetResult.Done;
			}
			self.SysMessage("Vyber NPC.");
			return TargetResult.RestartTargetting;
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			if (parameter is NPC npc) {
				Thing top = targetted.TopObj(); //an item in a pack: where the pack's owner stands
				NpcSetHomeCommand.Set(self, npc, top.X, top.Y, top.Z, top.M);
				return TargetResult.Done;
			}
			self.SysMessage("Vyber NPC.");
			return TargetResult.RestartTargetting;
		}

		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			if (parameter is NPC npc) {
				NpcSetHomeCommand.Set(self, npc, targetted.X, targetted.Y, targetted.Z, self.M);
				return TargetResult.Done;
			}
			self.SysMessage("Vyber NPC.");
			return TargetResult.RestartTargetting;
		}
	}
}
