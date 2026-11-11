using System;
using System.Collections.Generic;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1419 - the caster's @spellPower (before the target's defense, a change reaches the effect) and
	/// @spellSuccess (after a successful Magery cast). A trigger group and a plugin on the caster; the casts go through
	/// SkillSequenceArgs.PhaseSuccess, Magery's success path without the cast's wait. Protocol side:
	/// Tests/Protocol/SpellTriggersTests.cs.</summary>
	public static class SpellTriggersTests {
		private static readonly PluginKey probeKey = PluginKey.Acquire("_spellTriggersProbe_");
		internal static int powers, successes, groundPowers;
		internal static int addPower, seenPower = -1, effectPower = -1;
		internal static SpellDef successSpell;
		internal static IPoint3D successTarget;

		private static void Check(string name, bool ok) {
			Console.WriteLine("SPELLTRIG_CHECK: " + name + "=" + ok);
		}

		private static SpellBook BookOf(Character self) {
			SpellBook book = self.Backpack.FindByClass(typeof(SpellBook)) as SpellBook;
			if (book == null) {
				book = (SpellBook) ThingDef.GetByDefname("i_spellbook_3").Create(self.Backpack);
				foreach (SpellDef spell in SpellDef.AllSpellDefs) {
					book.AddSpell(spell);
				}
			}
			return book;
		}

		/// <summary>Magery's success phase: pays, runs the effect, then the after-cast work.</summary>
		private static void Succeed(Character self, string spellName, IPoint4D target) {
			self.Mana = self.MaxMana;
			SkillSequenceArgs.Acquire(self, SkillName.Magery, target, null, BookOf(self), SpellDef.GetByDefname(spellName), null).PhaseSuccess();
		}

		[SteamFunction]
		public static void TestSpellTriggers(Character self) {
			int plevel = self.Account.PLevel;
			SpellTriggersProbePlugin probe = (SpellTriggersProbePlugin) SpellTriggersProbePluginDef.instance.Create();
			NPC dummy = null;
			List<AbstractItem> fields = new List<AbstractItem>();
			try {
				self.Account.PLevel = 1;
				MovementSpeedTests.TestTeleportToOpenGround(self);
				VerificationTestUtils.CleanupNearbyNpcs(self, 10);
				self.SetRealSkillValue((int) SkillName.Magery, 1000);
				self.MaxMana = 1000;
				foreach (AbstractScript script in AbstractScript.AllScripts) {
					if ((script is ItemDef def) && def.PrettyDefname.StartsWith("i_reag_", StringComparison.OrdinalIgnoreCase)) {
						((Item) def.Create(self.Backpack)).Amount = 50;
					}
				}
				dummy = (NPC) ((NPCDef) ThingDef.GetByDefname("c_sheep_woolly")).Create(self.X + 2, self.Y, self.Z, self.M);
				dummy.MaxHits = 10000;
				dummy.Hits = 10000;
				dummy.MindDefenseModifier = 20; //a defense, so a power seen after it would differ from the one before it
				NPCAiTimer.Freeze(dummy, 120);
				powers = 0;
				successes = 0;
				groundPowers = 0;
				addPower = 0;
				seenPower = -1;
				effectPower = -1;
				successSpell = null;
				successTarget = null;
				self.AddTriggerGroup(SingletonScript<E_SpellTriggersProbe>.Instance);
				self.AddPlugin(probeKey, probe);

				//a cast on a monster: @spellPower before its defense, then the effect, then @spellSuccess
				Succeed(self, "s_magic_arrow", dummy);
				int seenPlain = seenPower, effectPlain = effectPower;
				Console.WriteLine("SPELLTRIG_PLAIN: powers=" + powers + " seen=" + seenPlain + " effect=" + effectPlain + " successes=" + successes
					+ " plugin=" + probe.powers + "/" + probe.successes);
				int defense = CombatCalculator.GetSpellDefense(self, dummy);
				Console.WriteLine("SPELLTRIG_DEFENSE: " + defense);
				Check("spellPowerRaised", (powers == 1) && (probe.powers == 1) && (seenPlain > 0) && (effectPlain >= 0));
				Check("spellPowerBeforeDefense", (defense > 0) && (seenPlain - effectPlain == defense));
				Check("spellSuccessRaised", (successes == 1) && (probe.successes == 1) && (successSpell == SpellDef.GetByDefname("s_magic_arrow"))
					&& (successTarget == dummy) && !probe.fromScroll);

				//the same cast with 500 added in @spellPower reaches the effect 500 stronger, the defense taken after it
				addPower = 500;
				Succeed(self, "s_magic_arrow", dummy);
				addPower = 0;
				Console.WriteLine("SPELLTRIG_ADDED: seen=" + seenPower + " effect=" + effectPower);
				Check("spellPowerChangesIt", (seenPower == seenPlain) && (effectPower == effectPlain + 500));

				//a ground cast raises it too, with the spot as its target
				HashSet<AbstractItem> before = new HashSet<AbstractItem>(self.GetMap().GetItemsInRange(self.X, self.Y - 3, 3));
				Succeed(self, "s_fire_field", new Point4D(self.X, self.Y - 3, self.Z, self.M));
				foreach (AbstractItem item in self.GetMap().GetItemsInRange(self.X, self.Y - 3, 3)) {
					if (!before.Contains(item)) {
						fields.Add(item);
					}
				}
				Check("groundSpellPower", (groundPowers == 1) && (successes == 3));
			} finally {
				addPower = 0;
				self.RemoveTriggerGroup(SingletonScript<E_SpellTriggersProbe>.Instance);
				self.DeletePlugin(probeKey);
				foreach (AbstractItem item in fields) {
					if (!item.IsDeleted) {
						item.Delete();
					}
				}
				if ((dummy != null) && !dummy.IsDeleted) {
					dummy.Delete();
				}
				self.Account.PLevel = (byte) plevel;
			}
			Console.WriteLine("SPELLTRIG_DONE");
		}
	}

	public class E_SpellTriggersProbe : CompiledTriggerGroup {
		public void On_SpellPower(Character self, SpellPowerArgs args) {
			if (args.TargetChar == null) {
				SpellTriggersTests.groundPowers++;
				return;
			}
			SpellTriggersTests.powers++;
			SpellTriggersTests.seenPower = args.SpellPower;
			args.SpellPower += SpellTriggersTests.addPower;
		}

		public void On_CauseSpellEffect(Character self, SpellEffectArgs args) {
			SpellTriggersTests.effectPower = args.SpellPower;
		}

		public void On_SpellSuccess(Character self, SpellSuccessArgs args) {
			SpellTriggersTests.successes++;
			SpellTriggersTests.successSpell = args.Spell;
			SpellTriggersTests.successTarget = args.Target;
		}
	}

	public class SpellTriggersProbePluginDef : PluginDef {
		public static readonly SpellTriggersProbePluginDef instance =
			(SpellTriggersProbePluginDef) new SpellTriggersProbePluginDef("p_spellTriggers_probe", "C# scripts", -1).Register();

		public SpellTriggersProbePluginDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override Plugin CreateImpl() {
			return new SpellTriggersProbePlugin();
		}

		public new static void Bootstrap() {
			RegisterPluginDef(typeof(SpellTriggersProbePluginDef), typeof(SpellTriggersProbePlugin));
		}
	}

	/// <summary>Counts @spellPower on characters and @spellSuccess. Never saved.</summary>
	[DeepCopyableClass]
	[SaveableClass]
	public class SpellTriggersProbePlugin : Plugin {
		internal int powers, successes;
		internal bool fromScroll;

		[DeepCopyImplementation]
		public SpellTriggersProbePlugin(SpellTriggersProbePlugin copyFrom)
			: base(copyFrom) {
		}

		[LoadingInitializer]
		public SpellTriggersProbePlugin() {
		}

		public void On_SpellPower(SpellPowerArgs args) {
			if (args.TargetChar != null) {
				this.powers++;
			}
		}

		public void On_SpellSuccess(SpellSuccessArgs args) {
			this.successes++;
			this.fromScroll = args.FromScroll;
		}
	}
}
