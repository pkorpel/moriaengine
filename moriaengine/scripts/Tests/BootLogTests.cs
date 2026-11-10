using System;

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
		private static readonly PluginKey paralyzeKey = PluginKey.Acquire("_bootTestParalyze_");
		private static readonly PluginKey ignitionKey = PluginKey.Acquire("_bootTestIgnition_");
		private static readonly PluginKey craftingKey = PluginKey.Acquire("_bootTestCrafting_");

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

				Console.WriteLine("BOOT_STATES: setup sheepBefore=" + sheep.GetTag(preStatsTK) + " sheepDiscorded=" + Stats(sheep) +
					" regenBefore=" + self.GetTag(preRegenTK) + " regenIgnited=" + self.HitsRegenSpeed);
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
}
