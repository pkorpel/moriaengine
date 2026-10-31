using SteamEngine.Common;
using SteamEngine.Networking;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>.throwexplosion, .throwdispel: a bomb thrown at a spot. It lands a second later and counts
	/// down there. Without the throwing ability it can land anywhere near.</summary>
	public static class BombThrowing {
		private static readonly string[] bombsBestFirst = { "i_potion_Lava", "i_potion_ExplosionGreat", "i_potion_Explosion", "i_potion_ExplosionLess" };
		private const string DispelBomb = "i_potion_ExplosionDispell";
		private const int MissRadius = 7, ThrowAnim = 11, ThrowSound = 0x145, FlyingBottle = 0xF0D;
		private static readonly TagKey nextThrowTK = TagKey.Acquire("_next_throw_");

		internal static Item FindBomb(Character ch, bool dispel) {
			foreach (string defname in dispel ? new[] { DispelBomb } : bombsBestFirst) {
				foreach (Item item in VarGate.Carried(ch)) {
					if (VarGate.Matches(item, defname, false)) {
						return item;
					}
				}
			}
			return null;
		}

		public static void Aim(Player thrower, bool dispel) {
			if (FindBomb(thrower, dispel) == null) {
				thrower.SysMessage("Nemas u sebe zadnou bombu");
			} else {
				thrower.SysMessage("Zamer cil");
				thrower.Target(SingletonScript<Targ_ThrowBomb>.Instance, dispel);
			}
		}

		internal static void Throw(Player thrower, IPoint3D target, bool dispel) {
			PotionSettings settings = PotionSettings.instance;
			if (Globals.TimeInSeconds < PotionDrinking.Time(thrower, nextThrowTK)) {
				thrower.SysMessage("Jeste nemuzes hodit bombu.");
				return;
			}
			if (!thrower.CanSeeLOS(target).Allow) {
				thrower.SysMessage("Nevidis na cil.");
				return;
			}
			if (Point2D.GetSimpleDistance(thrower.X, thrower.Y, target.X, target.Y) >= settings.throwRange) {
				thrower.SysMessage("Cil je prilis daleko.");
				return;
			}
			IPoint3D landing = target;
			AbilityDef ability = AbilityDef.GetByDefname("a_throwing") as AbilityDef;
			int points = (ability != null) ? thrower.GetAbility(ability) : 0;
			if (Globals.dice.Next(10000) > (points * 1000)) { //goes wherever
				int[] at = WorldSpots.NearestStandable(thrower.GetMap(), thrower.X + Globals.dice.Next(-MissRadius, MissRadius + 1), thrower.Y + Globals.dice.Next(-MissRadius, MissRadius + 1), 2);
				if (at != null) {
					Point3D astray = new Point3D(at[0], at[1], at[2]);
					if (thrower.CanSeeLOS(astray).Allow) {
						landing = astray;
					}
				}
			}
			thrower.SetTag(nextThrowTK, Globals.TimeInSeconds + settings.throwDelaySeconds);
			BombFlightTimer flight = new BombFlightTimer { x = landing.X, y = landing.Y, z = landing.Z, dispel = dispel };
			thrower.AddTimer(BombFlightTimer.Key, flight).DueInSeconds = 1;
		}

		/// <summary>hazim: a new one where it landed, armed, fixed there.</summary>
		internal static void Land(Character thrower, int x, int y, int z, bool dispel) {
			Item bomb = FindBomb(thrower, dispel);
			if (bomb == null) {
				thrower.SysMessage("Nemas explosion");
				return;
			}
			thrower.Anim(ThrowAnim);
			PacketSequences.SendSound(thrower, ThrowSound, Globals.MaxUpdateRange);
			Point4D point = new Point4D(x, y, z, thrower.M);
			EffectFactory.EffectFromTo(thrower, point, FlyingBottle, 5, 0, false, false);
			Potion landed = (Potion) ((ThingDef) bomb.Def).Create(x, y, z, thrower.M);
			landed.SetAttribute(ItemAttributes.attr_move_never, true);
			BombPotionKind.Arm(landed);
			bomb.Consume(1);
		}
	}

	public class Targ_ThrowBomb : CompiledTargetDef {
		protected override TargetResult On_TargonPoint(Player self, IPoint3D targetted, object parameter) {
			BombThrowing.Throw(self, targetted, (bool) parameter);
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonThing(Player self, Thing targetted, object parameter) {
			BombThrowing.Throw(self, targetted, (bool) parameter);
			return TargetResult.Done;
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class BombFlightTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_bomb_flight_");

		[SaveableData, CopyableData] public int x;
		[SaveableData, CopyableData] public int y;
		[SaveableData, CopyableData] public int z;
		[SaveableData, CopyableData] public bool dispel;

		[LoadingInitializer, DeepCopyImplementation]
		public BombFlightTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if (cont is Character thrower) {
				BombThrowing.Land(thrower, this.x, this.y, this.z, this.dispel);
			}
		}
	}
}
