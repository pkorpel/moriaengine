using System.Collections.Generic;
using SteamEngine.Communication.TCP;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Networking;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	public partial class TravelStoneDef {
	}

	/// <summary>#1035 - i_stone_travel (t_stone_travel), a coach station. See TravelStops.</summary>
	[ViewableClass]
	public partial class TravelStone {
		private const int ClassMessageHue = 0x57;

		public override void On_Create() {
			base.On_Create();
			this.SetAttribute(ItemAttributes.attr_move_never, true);
		}

		public override void On_Click(AbstractCharacter clicker, GameState clickerState, TcpConnection<GameState> clickerConn) {
			base.On_Click(clicker, clickerState, clickerConn);
			this.Tell(clicker);
		}

		public override void On_AosClick(AbstractCharacter clicker, GameState clickerState, TcpConnection<GameState> clickerConn) {
			base.On_AosClick(clicker, clickerState, clickerConn);
			this.Tell(clicker);
		}

		private void Tell(AbstractCharacter clicker) {
			Character ch = clicker as Character;
			if (ch != null) {
				this.ClickMessage(ch, "Stanice dostavniku a povozu. Nase cesty jsou naprosto bezpecne. Zpoplatneni je podle vzdalenosti.");
				this.ClickMessage(ch, "Pokud vyuzijes nasich sluzeb, budes jiste spokojen, poutniku.");
			}
		}

		public override void On_DClick(AbstractCharacter from) {
			Player player = from as Player;
			if ((player == null) || MysticHypnosis.RefusesMenu(player)) { //#1070 - not while possessing
				return;
			}
			if ((player.Realm == Realm.MordorOutcast) || (player.Realm == Realm.GondorOutcast)) {
				player.RedMessage("Nase sluzby nejsou pro takove jako jsi Ty!");
			} else {
				D_TravelStone.Show(player, this, 0);
			}
		}

		/// <summary>f_gotopoint. The money goes when the ride is booked, and a ride already booked is
		/// lost even if this one can't be paid for.</summary>
		internal void Book(Player player, TravelStop stop) {
			if (ShamanVoodoo.RefusesRide(player)) { //#1070 - the voodoo's e_noport
				return;
			}
			if (TravelStops.IsEnemyLand(player, stop.RealmTag)) {
				player.SysMessage("Nemuzes cestovat na nepratelske uzemi.", ClassMessageHue);
				D_TravelStone.Show(player, this, 0);
				return;
			}
			if (TravelStops.NeedsEscort(player, this)) {
				player.SysMessage("Platis priplatek za eskortu z nepratelskeho uzemi.", ClassMessageHue);
			}
			int price = TravelStops.Price(player, this, stop);
			player.DeleteTimer(TravelRide.Key);
			if (!GoldUtils.Pay(player, price)) {
				return;
			}
			if (player.IsGM) {
				TravelRide.Arrive(player, stop);
			} else {
				player.AddTimer(TravelRide.Key, new TravelRide(this, stop));
			}
		}
	}

	/// <summary>i_travelstone_delay, a count above the head. Stray too far by the end and the coach leaves
	/// without you, the money stays with the coachman.</summary>
	[SaveableClass, DeepCopyableClass]
	public class TravelRide : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_travelRide_");
		private const int TeleportEffect = 0x3728;
		private const int TeleportSound = 0x1fe;

		[SaveableData, CopyableData]
		public int fromX, fromY, stopIndex, countLeft;

		[SaveableData, CopyableData]
		public byte fromM;

		[DeepCopyImplementation, LoadingInitializer]
		public TravelRide() {
		}

		internal TravelRide(TravelStone stone, TravelStop stop) {
			this.fromX = stone.X;
			this.fromY = stone.Y;
			this.fromM = stone.M;
			this.stopIndex = System.Array.IndexOf(TravelStops.All, stop);
			this.countLeft = TravelSettings.instance.rideSeconds;
			this.DueInSeconds = 1;
			this.PeriodInSeconds = 1;
		}

		protected override void OnTimeout(TagHolder cont) {
			Character self = cont as Character;
			if (self == null) {
				return;
			}
			if (this.countLeft > 0) {
				self.Message(TravelStops.Count(this.countLeft));
				this.countLeft--;
				return;
			}
			this.Delete();
			if ((self.M != this.fromM) || (Point2D.GetSimpleDistance(self, new Point2D(this.fromX, this.fromY)) > TravelSettings.instance.strayTiles)) {
				self.RedMessage("Prilis jsi se " + (self.IsFemale ? "vzdalila" : "vzdalil") + " od dostavniku !");
			} else {
				Arrive(self, TravelStops.All[this.stopIndex]);
			}
		}

		/// <summary>Sphere's go: it reveals, takes along what follows, and shows the teleport both ends.</summary>
		internal static void Arrive(Character self, TravelStop stop) {
			int[] spot = stop.Arrival;
			if (spot == null) {
				self.RedMessage("Tato trasa nyni nejede, kontaktuj GM.");
				return;
			}
			Point4D point = new Point4D(spot[0], spot[1], spot[2], stop.Destination.M);
			HidingSkillDef.UnHide(self);
			List<NPC> followers = new List<NPC>();
			foreach (AbstractCharacter near in self.GetMap().GetCharsInRange(self.X, self.Y, Globals.MaxUpdateRange)) {
				NPC npc = near as NPC;
				if ((npc != null) && IsFollowing(npc, self)) {
					followers.Add(npc);
				}
			}
			foreach (NPC follower in followers) {
				Move(follower, point);
			}
			Move(self, point);
		}

		/// <summary>#1043 - really walking after self, as NPCAiTimer.Think decides it. Sphere asks the same of
		/// its NPCACT_FOLLOW_TARG. "Stop" leaves a wild NPC's FollowTarget set, to mark it as told to stand.</summary>
		private static bool IsFollowing(NPC npc, Character self) {
			return npc.ShouldFollowOwner && (npc.PetGoalPoint == null) && ((npc.FollowTarget ?? npc.ControlMaster) == self);
		}

		private static void Move(Character ch, Point4D point) {
			EffectFactory.StationaryEffect(ch, TeleportEffect, 9, 20);
			ch.Go(point);
			EffectFactory.StationaryEffect(ch, TeleportEffect, 9, 20);
			ch.Sound(TeleportSound);
		}
	}
}
