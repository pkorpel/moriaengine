using System;
using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1558 P3 - the fixes that needed no decision: each trigger raised from its own Trigger_X, in the moment
	/// it names, with args a handler can read (and, for @gainExperience, change from LScript too). Protocol side:
	/// Tests/Protocol/TriggerShapeTests.cs.</summary>
	public static class TriggerShapeTests {
		internal static readonly List<string> log = new List<string>();

		private static void Check(string name, bool ok, string detail = "") {
			Console.WriteLine("SHAPE_CHECK: " + name + "=" + ok + ((detail.Length > 0) ? (" (" + detail + ")") : ""));
		}

		private static string Logged() {
			string s = string.Join(",", log);
			log.Clear();
			return s;
		}

		private static void Ready(Character self) {
			self.Account.PLevel = 1;
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 10);
			self.Hits = self.MaxHits;
			log.Clear();
			E_ShapeProbe.cancelMenu = false;
		}

		/// <summary>@disruption's args say whether casting breaks; @hostileAction needs an enemy; the dispel bomb raises
		/// @dispell as the spell does.</summary>
		[SteamFunction]
		public static void TestShapeNotifications(Character self) {
			Ready(self);
			self.AddTriggerGroup(SingletonScript<E_ShapeProbe>.Instance);
			NPC bomber = null;
			try {
				self.Trigger_Disrupt(false);
				self.Trigger_Disrupt();
				Check("disruptionArgs", Logged() == "disruption:False,disruption:True");

				self.Trigger_HostileAction(null);
				Check("noHostileActionWithoutEnemy", Logged() == "");
				bomber = (NPC) ThingDef.GetByDefname("c_orc").Create(self.X + 2, self.Y, self.Z, self.M);
				NPCAiTimer.Freeze(bomber, 300);
				self.Trigger_HostileAction(bomber);
				Check("hostileActionWithEnemy", Logged() == "hostileAction");

				MonsterAbilitiesSettings settings = MonsterAbilitiesSettings.instance;
				double oldFuse = settings.DispelBombFuse;
				settings.DispelBombFuse = 0;
				try {
					MonsterSpecialAttacks.ThrowDispelBomb(bomber, self.X, self.Y);
					MonsterSpecialAttacks.TryDispelBombTick(bomber);
				} finally {
					settings.DispelBombFuse = oldFuse;
				}
				Check("dispelBombRaisesDispell", Logged().Contains("dispell"));
			} finally {
				self.RemoveTriggerGroup(SingletonScript<E_ShapeProbe>.Instance);
				if ((bomber != null) && !bomber.IsDeleted) {
					bomber.Delete();
				}
			}
			Console.WriteLine("SHAPE_NOTIFY_DONE");
		}

		/// <summary>@contextMenuSelect comes before the entry's action and its cancel stops it.</summary>
		[SteamFunction]
		public static void TestShapeContextMenu(Character self) {
			Ready(self);
			Item dagger = (Item) ThingDef.GetByDefname("i_dagger").Create(self.X + 1, self.Y, self.Z, self.M);
			dagger.AddTriggerGroup(SingletonScript<E_ShapeProbe>.Instance);
			try {
				dagger.Trigger_ContextMenuSelect(self, 1);
				Check("menuSelectBeforeAction", Logged() == "contextMenuSelect,action");
				E_ShapeProbe.cancelMenu = true;
				dagger.Trigger_ContextMenuSelect(self, 1);
				Check("menuSelectCancelStopsAction", Logged() == "contextMenuSelect");
			} finally {
				E_ShapeProbe.cancelMenu = false;
				dagger.Delete();
			}
			Console.WriteLine("SHAPE_MENU_DONE");
		}

		/// <summary>The potion macro is a whole double click; a potion acts on the ground once it's really there (a bomb
		/// starts counting, a dragon cure breaks), and a lying bomb goes off once lifted.</summary>
		[SteamFunction]
		public static void TestShapePotions(Character self) {
			Ready(self);
			self.AddTriggerGroup(SingletonScript<E_ShapeProbe>.Instance);
			Character dummy = null;
			try {
				//the macro: the clicker's @itemDClick fires, as for a real double click
				PotionDrinking.ForgetAttempts(self);
				PotionDrinking.Wait(self, -1);
				self.Hits = (short) (self.MaxHits / 2);
				ThingDef.GetByDefname("i_potion_HealLess").Create(self.Backpack);
				PotionCommands.PotionHeal(self);
				Check("macroIsADoubleClick", Logged().Contains("itemDClick"));

				//a bomb put down from the hand starts counting on the ground, and doesn't burn the hand
				PotionDrinking.Wait(self, -1);
				self.RemoveTimer(BombInHandTimer.Key);
				Potion bomb = (Potion) ThingDef.GetByDefname("i_potion_ExplosionLess").Create(self.Backpack);
				self.PickupItem(bomb, 1);
				DenyResult dropped = self.TryPutItemOnGround(self.X + 1, self.Y, self.Z);
				Check("bombDropArms", dropped.Allow && bomb.IsOnGround && !bomb.IsDeleted && bomb.HasTimer(BombFuseTimer.Key)
					&& !self.HasTimer(BombInHandTimer.Key), "allow=" + dropped.Allow + " onGround=" + bomb.IsOnGround + " fuse=" + bomb.HasTimer(BombFuseTimer.Key) + " inHand=" + self.HasTimer(BombInHandTimer.Key));
				bomb.Delete();

				//a dragon cure put down breaks; one made on the ground doesn't
				Potion cure = (Potion) ThingDef.GetByDefname("i_potion_dragon_fire").Create(self.Backpack);
				self.PickupItem(cure, 1);
				self.TryPutItemOnGround(self.X + 1, self.Y, self.Z);
				Check("dragonCureBreaksWhenPutDown", cure.HasTimer(BrokenBottleTimer.Key));
				cure.Delete();
				Potion made = (Potion) ThingDef.GetByDefname("i_potion_dragon_fire").Create(self.X + 1, self.Y, self.Z, self.M);
				Check("dragonCureMadeOnGroundStays", !made.HasTimer(BrokenBottleTimer.Key));
				made.Delete();

				//a bomb lying on the ground goes off when lifted, where it lay
				dummy = (Character) ThingDef.GetByDefname("c_a_jackrabbit").Create(self.X + 2, self.Y, self.Z, self.M);
				dummy.Hits = 200;
				Potion lying = (Potion) ThingDef.GetByDefname("i_potion_Explosion").Create(self.X + 1, self.Y, self.Z, self.M);
				DenyResult lifted = self.TryPickupItem(lying, 1);
				BombLiftedTimer goesOff = (BombLiftedTimer) lying.GetTimer(BombLiftedTimer.Key);
				Check("liftedBombGoesOff", lifted.Allow && (goesOff != null) && (goesOff.x == self.X + 1), "allow=" + lifted.Allow + " timer=" + (goesOff != null));
				goesOff?.Delete();
				((BombPotionKind) PotionKinds.Find(lying)).Explode(lying, new Point4D((ushort) (self.X + 1), self.Y, self.Z, self.M));
				Check("liftedBombHurtsWhereItLay", lying.IsDeleted && (dummy.Hits < 200), "deleted=" + lying.IsDeleted + " hits=" + dummy.Hits);
			} finally {
				self.RemoveTriggerGroup(SingletonScript<E_ShapeProbe>.Instance);
				self.RemoveTimer(BombInHandTimer.Key);
				if ((dummy != null) && !dummy.IsDeleted) {
					dummy.Delete();
				}
				self.Hits = self.MaxHits;
			}
			Console.WriteLine("SHAPE_POTIONS_DONE");
		}

		/// <summary>An LScript @gainExperience handler changes the amount through argv[1].</summary>
		[SteamFunction]
		public static void TestShapeGainExperience(Character self) {
			Ready(self);
			Player player = (Player) self;
			int experience = player.Experience;
			TriggerGroup tg = TriggerGroup.GetByDefname("e_shape_gainexp");
			self.AddTriggerGroup(tg);
			try {
				int before = player.Experience;
				LevelingSystem.AddExperience(player, 50);
				Check("scriptedGainChangesAmount", player.Experience - before == 55, (player.Experience - before).ToString());
			} finally {
				self.RemoveTriggerGroup(tg);
				player.Experience = experience;
			}
			Console.WriteLine("SHAPE_GAIN_DONE");
		}
	}

	public partial class E_ShapeProbe : CompiledTriggerGroup {
		internal static bool cancelMenu;

		public void On_Disruption(Character self, DisruptionArgs args) { TriggerShapeTests.log.Add("disruption:" + args.InterruptCasting); }
		public void On_HostileAction(Character self, ScriptArgs args) { TriggerShapeTests.log.Add("hostileAction"); }
		public void On_Dispell(Character self, SpellEffectArgs args) { TriggerShapeTests.log.Add("dispell"); }
		public void On_ItemDClick(Character self, ScriptArgs args) { TriggerShapeTests.log.Add("itemDClick"); }

		public void On_BuildContextMenu(Item self, ContextMenu menu, AbstractCharacter requester) {
			menu.Add(3006123, who => TriggerShapeTests.log.Add("action"));
		}

		public TriggerResult On_ContextMenuSelect(Item self, ScriptArgs args) {
			TriggerShapeTests.log.Add("contextMenuSelect");
			return cancelMenu ? TriggerResult.Cancel : TriggerResult.Continue;
		}
	}
}
