using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1476 (#1399's #13) - on a character while something magic is worn (MagicItems.IsUnderMagic: items or a
	/// magic mount), the wearer's side of Erebor's magic item events. The sums stay the wearer's tags (MagicItems.WearerValue);
	/// this answers the triggers that read them, as Erebor's events did: the slayers on a blow (@AfterSwing = our
	/// @swingLanding) and on a spell (@BeforeDoEffect), the freezing blow (after the hit), the reflex and the block
	/// (@BeforeGetEffect). Installed and removed with the worn count (MagicItems.Sync), so a load brings it back as saved.
	/// Leech isn't here: it's class points + 2 x the items' (MagicCombat.LeechSum), whoever wears nothing.</summary>
	[DeepCopyableClass]
	[SaveableClass]
	public class MagicWearerPlugin : Plugin {
		internal static readonly PluginKey Key = PluginKey.Acquire("_magicWearer_");

		[DeepCopyImplementation]
		public MagicWearerPlugin(MagicWearerPlugin copyFrom)
			: base(copyFrom) {
		}

		[LoadingInitializer]
		public MagicWearerPlugin() {
		}

		internal static MagicWearerPlugin Of(Character ch) {
			return (ch == null) ? null : ch.GetPlugin(Key) as MagicWearerPlugin;
		}

		/// <summary>On while something magic is worn, off when the last is taken off.</summary>
		internal static void Sync(Character ch) {
			bool under = MagicItems.IsUnderMagic(ch);
			MagicWearerPlugin plugin = Of(ch);
			if (under && (plugin == null)) {
				ch.AddPlugin(Key, MagicWearerPluginDef.instance.Create());
			} else if (!under && (plugin != null)) {
				ch.DeletePlugin(Key);
			}
		}

		/// <summary>The slayers on the wearer's own blow.</summary>
		public void On_SwingLanding(WeaponSwingArgs swing) {
			MagicCombat.ModifyBlow(swing.attacker, swing.defender, swing);
		}

		/// <summary>e_paralysing: a landed blow may freeze.</summary>
		public void On_AfterHit(WeaponSwingArgs swing, int damage) {
			MagicCombat.TryFreeze(swing.attacker, swing.defender, damage);
		}

		/// <summary>The slayers on the wearer's spell, after the target's defense.</summary>
		public void On_BeforeDoEffect(SpellPowerArgs args) {
			if (args.Target is Character target) {
				args.SpellPower = MagicCombat.ModifySpellPower(args.Caster, target, args.SpellPower);
			}
		}

		/// <summary>e_spellreflex and e_spellblock on the wearer as a spell's target: a cancel means it doesn't land here.</summary>
		public TriggerResult On_BeforeGetEffect(SpellEffectArgs args) {
			Character self = (Character) this.Cont;
			SpellDef spell = args.SpellDef;
			if (MagicCombat.ReflectsOrBlocks(spell, self, args, reflected => spell.RunEffectOnReflected(reflected, args))) {
				return TriggerResult.Cancel;
			}
			return TriggerResult.Continue;
		}
	}

	public class MagicWearerPluginDef : PluginDef {
		public static readonly MagicWearerPluginDef instance =
			(MagicWearerPluginDef) new MagicWearerPluginDef("p_magicWearer", "C# scripts", -1).Register();

		public MagicWearerPluginDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override Plugin CreateImpl() {
			return new MagicWearerPlugin();
		}

		public new static void Bootstrap() {
			RegisterPluginDef(typeof(MagicWearerPluginDef), typeof(MagicWearerPlugin));
		}
	}
}
