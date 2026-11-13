using System;
using SteamEngine.Timers;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	/// <summary>#937 - what a clean boot must not have cost: defs that shared a model number or were a dupe with a
	/// name of their own, and the engine's own flag values over Sphere's.</summary>
	public static class BootLogTests {
		private static readonly string[] Defs = {
			"c_orc", "c_orc_nb", "c_vrk", "c_alligator",
			"i_door_sliding", "i_crop_sprouts3", "i_fruit_lettuce", "i_sign_post_new", "i_wooden_sign_post",
			"i_cleric_surcoat", "i_clerikova_stuha", "i_rock_3", "i_pet_horse", "i_multi_tower", "i_lemon_halved",
			"i_qv1_zakladzdi",
		};

		[SteamFunction]
		public static void TestBootDefs(Character self) {
			string missing = "";
			foreach (string defname in Defs) {
				if (ThingDef.GetByDefname(defname) == null) {
					missing += " " + defname;
				}
			}
			Console.WriteLine("BOOT_DEFS: missing=[" + missing.Trim() + "] attrStatic=" + Constant.GetValue("attr_static")
				+ " statfWar=" + Constant.GetValue("statf_war") + " sewingKit=" + (TemplateDef.GetByDefname("VENDOR_S_TAILOR") != null));
		}

		private static readonly TagKey stateHolderTK = TagKey.Acquire("_bootStateHolder_");
		private static readonly TagKey preStatsTK = TagKey.Acquire("_bootStatePreStats_");
		private static readonly TagKey preRegenTK = TagKey.Acquire("_bootStatePreRegen_");
		private static readonly TagKey preOrderTK = TagKey.Acquire("_bootStatePreOrder_");
		private static readonly PluginKey paralyzeKey = PluginKey.Acquire("_bootTestParalyze_");
		private static readonly PluginKey ignitionKey = PluginKey.Acquire("_bootTestIgnition_");
		private static readonly PluginKey craftingKey = PluginKey.Acquire("_bootTestCrafting_");

		/// <summary>#1483 - the order the holder's plugins hear its triggers in.</summary>
		private static string ListOrder(PluginHolder holder) {
			string order = "";
			foreach (Plugin plugin in holder.GetNonSimplePlugins()) {
				order += ((order.Length == 0) ? "" : ",") + plugin.Def.PrettyDefname;
			}
			return "[" + order + "]";
		}

		/// <summary>#1483 - a dupe's plugins hear its triggers in the original's order (an item: an NPC's AI timer can't be
		/// copied).</summary>
		private static bool CopyKeepsOrder(Character self) {
			Item original = (Item) ThingDef.GetByDefname("i_dagger").Create(self.Backpack);
			int n = 0;
			foreach (PluginDef def in new PluginDef[] { ConvictPluginDef.instance, CraftingProcessPluginDef.instance, HiddenPluginDef.instance, HungerPluginDef.instance }) {
				original.AddNewPlugin(PluginKey.Acquire("_bootOrder" + (n++) + "_"), def);
			}
			Thing copy = original.Dupe();
			bool same = (ListOrder(original) == ListOrder((PluginHolder) copy)) && (ListOrder(original).Split(',').Length >= 4);
			Console.WriteLine("BOOT_ORDER: original=" + ListOrder(original) + " copy=" + ListOrder((PluginHolder) copy));
			copy.Delete();
			original.Delete();
			return same;
		}

		/// <summary>#1485 - an NPC's dupe is a new NPC of its def beside it, as Sphere's (a deep copy failed on its AI
		/// timer): it thinks with its own AI, and has none of the original's state (frozen, timers, the spawn gem's link).
		/// An item's dupe copies no timers either, as Sphere's, so one that can't be copied no longer fails it.</summary>
		private static string DupeTimers(Character self) {
			NPC original = (NPC) ThingDef.GetByDefname("c_a_jackrabbit").Create(self.X + 2, self.Y, self.Z, self.M);
			NPCAiTimer.Freeze(original, 600);
			original.AddTimer(UncopiedTimer.Key, new UncopiedTimer()).DueInSeconds = 600;
			original.AddTimer(CopiedTimer.Key, new CopiedTimer()).DueInSeconds = 600;
			Item gem = (Item) ThingDef.GetByDefname("i_dagger").Create(self.Backpack);
			original.SetTag(TagKey.Acquire("spawnGem"), gem);
			original.AddTriggerGroup(TriggerGroup.GetByDefname("e_spawned_char"));
			string line;
			NPC copy = null;
			bool itemDupe = false;
			try {
				copy = (NPC) original.Dupe();
				NPCAiTimer ai = NPCAiTimer.GetFor(copy);
				line = "npcDupe=" + ((copy != original) && (copy.Def == original.Def) && (Point2D.GetSimpleDistance(copy, original) <= 1) && (copy.M == original.M)) +
					" npcCopyThinks=" + ((ai != null) && (ai != NPCAiTimer.GetFor(original)) && !ai.IsPaused) +
					" npcCopyFresh=" + (!NPCAiTimer.IsFrozen(copy) && NPCAiTimer.IsFrozen(original)) +
					" timersStayBehind=" + (!copy.HasTimer(UncopiedTimer.Key) && !copy.HasTimer(CopiedTimer.Key) && original.HasTimer(CopiedTimer.Key)) +
					" spawnLinkDropped=" + (!copy.HasTag(TagKey.Acquire("spawnGem")) && !copy.HasTriggerGroup(TriggerGroup.GetByDefname("e_spawned_char")) &&
						original.HasTag(TagKey.Acquire("spawnGem")));
			} catch (Exception e) {
				line = "npcDupe=False(" + e.GetType().Name + ")";
			} finally {
				if (copy != null) {
					copy.Delete();
				}
			}
			Item withUncopied = (Item) ThingDef.GetByDefname("i_dagger").Create(self.Backpack);
			withUncopied.AddTimer(UncopiedTimer.Key, new UncopiedTimer()).DueInSeconds = 600;
			try {
				Item itemCopy = (Item) withUncopied.Dupe();
				itemDupe = !itemCopy.HasTimer(UncopiedTimer.Key);
				itemCopy.Delete();
			} catch (SEException) {
				itemDupe = false;
			}
			withUncopied.Delete();
			line += " itemDupeWithUncopiedTimer=" + itemDupe;
			line += " itemDupeLeavesRunningPlugins=" + DupeTimedPlugins.Check(self); //#1493
			original.RemoveTag(TagKey.Acquire("spawnGem")); //before the delete: the gem's count isn't this test's
			original.RemoveTriggerGroup(TriggerGroup.GetByDefname("e_spawned_char"));
			original.Delete();
			gem.Delete();
			return line;
		}

		/// <summary>#1492 - a deep copy keeps a [CopyableData] reference field (a copy of it): it used to stay null. #1496 - arrays
		/// too (no implementor was found for them).</summary>
		private static bool DeepCopyKeepsFields() {
			DeepCopyFieldProbe original = new DeepCopyFieldProbe();
			original.names = new System.Collections.Generic.List<string> { "a", "b" };
			original.text = "kept";
			original.counts = new[] { 3, 5 };
			original.args = new object[] { "x", 7 };
			DeepCopyFieldProbe copy;
			try {
				copy = (DeepCopyFieldProbe) DeepCopyFactory.GetCopy(original);
			} catch (Exception) { //the old generator handed the member's callback the whole object
				return false;
			}
			return (copy != original) && (copy.names != null) && (copy.names != original.names) && (copy.names.Count == 2) &&
				(copy.names[1] == "b") && (copy.text == "kept") &&
				(copy.counts != null) && (copy.counts != original.counts) && (copy.counts.Length == 2) && (copy.counts[0] == 3) && (copy.counts[1] == 5) && //#1496
				(copy.args != null) && (copy.args != original.args) && ((string) copy.args[0] == "x") && ((int) copy.args[1] == 7);
		}

		/// <summary>#1492 - reference members (a list, #1496 arrays) and a simple one.</summary>
		[DeepCopyableClass]
		public class DeepCopyFieldProbe {
			[CopyableData]
			public System.Collections.Generic.List<string> names;
			[CopyableData]
			public string text;
			[CopyableData]
			public int[] counts;
			[CopyableData]
			public object[] args;

			[DeepCopyImplementation]
			public DeepCopyFieldProbe() {
			}
		}

		/// <summary>#1485 - a timer that can be copied.</summary>
		[SaveableClass, DeepCopyableClass]
		public class CopiedTimer : BoundTimer {
			internal static readonly TimerKey Key = TimerKey.Acquire("_bootCopied_");

			[LoadingInitializer, DeepCopyImplementation]
			public CopiedTimer() {
			}

			protected override void OnTimeout(TagHolder cont) {
			}
		}

		/// <summary>#1485 - a timer with no copy implementation.</summary>
		[SaveableClass]
		public class UncopiedTimer : BoundTimer {
			internal static readonly TimerKey Key = TimerKey.Acquire("_bootUncopied_");

			[LoadingInitializer]
			public UncopiedTimer() {
			}

			protected override void OnTimeout(TagHolder cont) {
			}
		}

		private static string Stats(Character ch) {
			return ch.Str + "," + ch.Dex + "," + ch.Int + "," + ch.MaxHits + "," + ch.MaxMana + "," + ch.MaxStam;
		}

		/// <summary>#1163 - one of every plugin whose def is made in code and wasn't there when a save loaded, on a player, a sheep
		/// and a dagger: "setup" before the save, "check" after booting on it. Discordance and ignition change stats when they start
		/// and put them back when they end, so check ends both and compares.</summary>
		[SteamFunction]
		public static void TestBootStates(Character self, ScriptArgs sa) {
			string what = sa.Args.Trim().Trim('"');
			if (what == "setup") {
				NPC sheep = (NPC) ThingDef.GetByDefname("c_sheep_woolly").Create(self.X + 1, self.Y, self.Z, self.M);
				NPCAiTimer.Freeze(sheep, 7200);
				Item dagger = (Item) ThingDef.GetByDefname("i_dagger").Create(self.Backpack);
				self.SetTag(stateHolderTK, "player");
				sheep.SetTag(stateHolderTK, "sheep");
				dagger.SetTag(stateHolderTK, "dagger");

				WeaponSkillTargetQueuePlugin.AcquireCombatPlugin(sheep);
				WeaponSkillTargetTrackerPlugin.InstallTargetTracker(self, sheep);
				ParalyzeEffectPluginDef.Paralyze(self, paralyzeKey, TimeSpan.FromHours(2));

				sheep.SetTag(preStatsTK, Stats(sheep));
				DiscordanceEffectPlugin discord = (DiscordanceEffectPlugin) DiscordanceEffectPluginDef.instance.Create();
				discord.discordEffectPower = 1000;
				sheep.AddPluginAsSimple(DiscordanceSkillDef.effectPluginKey, discord);
				discord.Timer = 7200;

				WarcryEffectPlugin warcry = (WarcryEffectPlugin) WarcryEffectPluginDef.instance.Create();
				warcry.Init(sheep, EffectFlag.FromAbility | EffectFlag.HarmfulEffect, 1, TimeSpan.FromHours(2));
				self.AddPlugin(WarcryEffectPlugin.warcyEffectPluginKey, warcry);

				double chargeSeconds = ClassAbilitySettings.instance.chargeDurationSeconds;
				ClassAbilitySettings.instance.chargeDurationSeconds = 7200;
				try {
					MagicChargePlugin.AddCharge(self, MagicChargeSchool.Fire);
				} finally {
					ClassAbilitySettings.instance.chargeDurationSeconds = chargeSeconds;
				}

				self.SetTag(preRegenTK, self.HitsRegenSpeed);
				IgnitionEffectPlugin ignition = (IgnitionEffectPlugin) IgnitionEffectPluginDef.instance.Create();
				ignition.Init(sheep, EffectFlag.FromAbility | EffectFlag.HarmfulEffect, 0.5, TimeSpan.FromHours(2));
				self.AddPlugin(ignitionKey, ignition);

				PoisonedItemPlugin.Bind(dagger, PoisonKind.AbsoluteDamage, "boot test poison", 10, 5);
				self.AddNewPlugin(craftingKey, CraftingProcessPluginDef.instance);
				SnoopingPlugin snoop = (SnoopingPlugin) self.AddNewPlugin(SnoopingPlugin.snoopedPluginKey, SnoopingPluginDef.instance);
				snoop.Timer = 7200;

				self.SetTag(preOrderTK, ListOrder(self));
				Console.WriteLine("BOOT_STATES: setup sheepBefore=" + sheep.GetTag(preStatsTK) + " sheepDiscorded=" + Stats(sheep) +
					" regenBefore=" + self.GetTag(preRegenTK) + " regenIgnited=" + self.HitsRegenSpeed + " copyKeepsOrder=" + CopyKeepsOrder(self) + " " + DupeTimers(self) + " deepCopyKeepsFields=" + DeepCopyKeepsFields() +
					" pluginOrder=" + self.GetTag(preOrderTK));
				return;
			}

			Character player = null, sheepAgain = null;
			Item daggerAgain = null;
			foreach (Thing thing in Thing.AllThings) {
				switch (thing.GetTag(stateHolderTK) as string) {
					case "player":
						player = (Character) thing;
						break;
					case "sheep":
						sheepAgain = (Character) thing;
						break;
					case "dagger":
						daggerAgain = (Item) thing;
						break;
				}
			}
			string line = "found=" + (player != null) + "," + (sheepAgain != null) + "," + (daggerAgain != null);
			if ((player != null) && (sheepAgain != null) && (daggerAgain != null)) {
				//#1483 - the player's plugins hear its triggers in the order they did before the save
				line += " sameOrder=" + (ListOrder(player) == (player.GetTag(preOrderTK) as string)) + " orderNow=" + ListOrder(player)
					+ " orderBefore=" + player.GetTag(preOrderTK);
				line += " queue=" + (sheepAgain.GetPlugin(PluginKey.Acquire("_combatPlugin_")) is WeaponSkillTargetQueuePlugin)
					+ " tracker=" + (player.GetPlugin(PluginKey.Acquire("_weaponSkillTarget_")) is WeaponSkillTargetTrackerPlugin)
					+ " paralyze=" + (player.GetPlugin(paralyzeKey) is ParalyzeEffectPlugin)
					+ " discord=" + (sheepAgain.GetPlugin(DiscordanceSkillDef.effectPluginKey) is DiscordanceEffectPlugin)
					+ " warcry=" + (player.GetPlugin(WarcryEffectPlugin.warcyEffectPluginKey) is WarcryEffectPlugin)
					+ " charge=" + (player.GetPlugin(PluginKey.Acquire("_magicCharge_")) is MagicChargePlugin)
					+ " ignition=" + (player.GetPlugin(ignitionKey) is IgnitionEffectPlugin)
					+ " poison=" + (PoisonedItemPlugin.GetPoisonPlugin(daggerAgain) != null)
					+ " crafting=" + (player.GetPlugin(craftingKey) is CraftingProcessPlugin)
					+ " snoop=" + (player.GetPlugin(SnoopingPlugin.snoopedPluginKey) is SnoopingPlugin);
				//the effects end: what they took comes back
				sheepAgain.DeletePlugin(DiscordanceSkillDef.effectPluginKey);
				player.DeletePlugin(ignitionKey);
				line += " sheepStats=" + Stats(sheepAgain) + " sheepBefore=" + sheepAgain.GetTag(preStatsTK)
					+ " regen=" + player.HitsRegenSpeed + " regenBefore=" + player.GetTag(preRegenTK);
			}
			Console.WriteLine("BOOT_STATES: check " + line + " |");
		}
	}

	/// <summary>#1493 - a dupe leaves an item's running timer plugins with the original: the copy would get no timer and the
	/// plugin would never end. One waiting with no timer running, and a plain one, go along.</summary>
	internal static class DupeTimedPlugins {
		internal static bool Check(Character self) {
			PluginKey runningKey = PluginKey.Acquire("_dupeRunningProbe_"), waitingKey = PluginKey.Acquire("_dupeWaitingProbe_"),
				plainKey = PluginKey.Acquire("_dupePlainProbe_");
			Item kit = (Item) ThingDef.GetByDefname("i_dagger").Create(self.Backpack);
			Item copy = null;
			try {
				TimedProbePlugin running = (TimedProbePlugin) TimedProbePluginDef.instance.Create();
				kit.AddPlugin(runningKey, running);
				running.Timer = 600;
				TimedProbePlugin waiting = (TimedProbePlugin) TimedProbePluginDef.instance.Create();
				kit.AddPlugin(waitingKey, waiting);
				waiting.Timer = -1; //a paused timer: it goes along
				kit.AddPlugin(plainKey, KillCreditProbePluginDef.instance.Create());
				copy = (Item) kit.Dupe();
				int copied = 0;
				foreach (Plugin plugin in copy.GetAllPlugins()) {
					copied++;
				}
				Console.WriteLine("DUPE_TIMED_PLUGINS: running=" + (copy.GetPlugin(runningKey) != null) + " waiting=" + (copy.GetPlugin(waitingKey) != null)
					+ " plain=" + (copy.GetPlugin(plainKey) != null) + " copied=" + copied + " originalRunning=" + running.Timer);
				return (copy.GetPlugin(runningKey) == null) && (copy.GetPlugin(waitingKey) is TimedProbePlugin) && (copy.GetPlugin(plainKey) is KillCreditProbePlugin)
					&& (copied == 2) && (kit.GetPlugin(runningKey) == running) && (running.Timer > 500);
			} finally {
				if (copy != null) {
					copy.Delete();
				}
				kit.Delete();
			}
		}
	}

	public class TimedProbePluginDef : PluginDef {
		public static readonly TimedProbePluginDef instance =
			(TimedProbePluginDef) new TimedProbePluginDef("p_timed_probe", "C# scripts", -1).Register();

		public TimedProbePluginDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override Plugin CreateImpl() {
			return new TimedProbePlugin();
		}

		public new static void Bootstrap() {
			RegisterPluginDef(typeof(TimedProbePluginDef), typeof(TimedProbePlugin));
		}
	}

	/// <summary>#1493 - a TimerPlugin that does nothing on its timer. Never saved.</summary>
	[DeepCopyableClass]
	[SaveableClass]
	public class TimedProbePlugin : TimerPlugin {
		[DeepCopyImplementation]
		public TimedProbePlugin(TimedProbePlugin copyFrom)
			: base(copyFrom) {
		}

		[LoadingInitializer]
		public TimedProbePlugin() {
		}

		public void On_Timer() {
		}
	}
}
