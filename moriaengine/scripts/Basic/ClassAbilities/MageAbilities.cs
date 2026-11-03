using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1017 - Erebor's Effects/silence.scp: Mind Blast can keep a monster from casting.</summary>
	public static class MageAbilities {
		internal static readonly TimerKey SilencedTK = TimerKey.Acquire("_silencer_");
		private const int SilenceEffect = 0x3789;

		/// <summary>isStaff's material level, where Erebor counts its metals without silver and gold.</summary>
		internal static int MaterialLevel(Material material) {
			switch (material) {
				case Material.Copper:
					return 1;
				case Material.Iron:
					return 2;
				case Material.Verite:
					return 3;
				case Material.Valorite:
					return 4;
				case Material.Obsidian:
					return 5;
				case Material.Adamantinum:
					return 6;
				case Material.Mithril:
					return 7;
				default:
					return 0;
			}
		}

		/// <summary>Out of 1000. Mind power is Int * staff level / 2, and resist counts against it.</summary>
		internal static int SilenceChance(int points, int intelligence, Material staff, int resist, int mindCharges) {
			ClassAbilitySettings settings = ClassAbilitySettings.instance;
			int mindPower = (intelligence * MaterialLevel(staff)) / 2;
			int chance = (points * settings.silenceChance) + (mindPower - resist);
			if (mindCharges > 0) {
				chance = (chance * (1000 + (mindCharges * settings.chargeSilenceBonus))) / 1000;
			}
			return chance;
		}

		/// <summary>f_silence_npc(1): needs the ability and a staff in hand.</summary>
		internal static bool TrySilence(Character caster, NPC target) {
			int points = ClassAbilities.Points(caster, "mind_blast_silence");
			ColoredStaff staff = caster.FindLayer(LayerNames.Hand2) as ColoredStaff;
			if ((points <= 0) || (staff == null)) {
				return false;
			}
			IObjectWithMaterial made = staff.Def as IObjectWithMaterial;
			Material material = (made == null) ? Material.None : made.Material;
			if (MaterialLevel(material) == 0) {
				return false;
			}
			MagicChargePlugin charges = MagicChargePlugin.Get(caster);
			int mindCharges = (charges == null) ? 0 : charges.GetCharge(MagicChargeSchool.Mind);
			if (Globals.dice.Next(1, 1001) > SilenceChance(points, caster.Int, material, 0, mindCharges)) {
				return false;
			}
			int tenths = Silence(target, ClassAbilities.Points(caster, "mind_blast_silenced_time") + ClassAbilitySettings.instance.silenceTenths);
			caster.SysMessage("Cil je umlcen na " + tenths + "ds.", ClassAbilities.ClassMessageHue);
			return true;
		}

		/// <summary>f_silence_add: a longer silence replaces a shorter one, never the other way.</summary>
		internal static int Silence(NPC target, int tenths) {
			BoundTimer running = target.GetTimer(SilencedTK);
			if (running == null) {
				if (target.CurrentSkillName == SkillName.Magery) {
					target.AbortSkill();
				}
				SilencerTimer timer = new SilencerTimer();
				target.AddTimer(SilencedTK, timer);
				timer.DueInSeconds = tenths / 10.0;
				EffectFactory.StationaryEffect(target, SilenceEffect, 1, (byte) System.Math.Min(255, 8 + (tenths * 2) + (tenths / 4)));
				return tenths;
			}
			if ((running.DueInSeconds * 10) < tenths) {
				running.DueInSeconds = tenths / 10.0;
				return tenths;
			}
			return (int) (running.DueInSeconds * 10);
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class SilencerTimer : BoundTimer {
		[DeepCopyImplementation, LoadingInitializer]
		public SilencerTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
		}
	}
}
