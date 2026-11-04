using System;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1152 - server side of Tests/Protocol/RiddenMountTests.cs.</summary>
	public static class RiddenMountTests {
		private static NPC mount;
		private static int mountX, mountY;

		[SteamFunction]
		public static void TestRiddenMount(Character self, ScriptArgs sa) {
			string what = sa.Args.Trim().Trim('"');
			string line;
			switch (what) {
				case "ride": {
						MovementSpeedTests.TestTeleportToOpenGround(self);
						VerificationTestUtils.CleanupNearbyNpcs(self, 12);
						mount = (NPC) ThingDef.GetByDefname("c_m_dark_zostrich").Create(self.X + 1, self.Y, self.Z, self.M);
						mount.ControlMaster = self;
						NPCAiTimer.EnsureInstalled(mount);
						self.Mount = mount;
						mountX = mount.X;
						mountY = mount.Y;
						//the rider moves on: the mount stays out of the world where it was mounted
						self.P(self.X + 6, self.Y + 6, self.Z);
						line = "riding=" + (self.Mount == mount) + " mountOut=" + mount.Flag_Disconnected;
						break;
					}
				case "after":
					//a few AI ticks later the ridden mount hasn't tried to walk after its rider
					line = "mountStayed=" + ((mount.X == mountX) && (mount.Y == mountY)) + " state=" + NPCAiTimer.GetFor(mount)?.LastState;
					self.Mount = null;
					line += " dismountedUnderRider=" + ((mount.X == self.X) && (mount.Y == self.Y)) + " back=" + !mount.Flag_Disconnected;
					mount.Delete();
					break;
				default:
					line = "unknown";
					break;
			}
			Console.WriteLine("MOUNT_TEST: " + what + " " + line + " |");
		}
	}
}
