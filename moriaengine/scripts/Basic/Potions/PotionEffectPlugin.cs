using System;
using System.Globalization;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	public enum PotionStat {
		Strength,
		Dexterity,
		Intelligence,
		Vitality,
		MagicResist,
		ResistParalyse,
		Armor,
		DragonResist,
		NightSight
	}

	/// <summary>#927 - Erebor's i_a_*_potion runes. Each sits in a slot, as a rune sat in a layer: a new one in
	/// the same slot replaces the old. Sphere has one potion layer, so stat potions don't add up, and potions
	/// aren't dispelled.</summary>
	public partial class PotionEffectPlugin {
		/// <summary>Layer 44, LAYER_FLAG_Potion.</summary>
		public static readonly PluginKey MainSlot = PluginKey.Acquire("_potionEffect_main_");
		public static readonly PluginKey NightSightSlot = PluginKey.Acquire("_potionEffect_nightsight_");
		public static readonly PluginKey SkinSlot = PluginKey.Acquire("_potionEffect_skin_");

		private static readonly TagKey dragonResistTK = TagKey.Acquire("dragon_resist");

		public static void Apply(Character drinker, PluginKey slot, PotionStat stat, int amount, double seconds, string name, int icon) {
			drinker.DeletePlugin(slot);
			PotionEffectPlugin plugin = (PotionEffectPlugin) PluginDef.GetByDefname("p_potion_effect").Create();
			plugin.stat = (int) stat;
			plugin.amount = amount;
			plugin.name = name;
			plugin.icon = icon;
			plugin.Init(drinker, EffectFlag.FromPotion | EffectFlag.BeneficialEffect, amount, TimeSpan.FromSeconds(seconds));
			drinker.AddPlugin(slot, plugin);
		}

		public static int DragonResist(Character ch) {
			return TagMath.IGetTag(ch, dragonResistTK);
		}

		public void On_Assign() {
			this.Change((Character) this.Cont, this.amount);
		}

		public override void On_UnAssign(Character cont) {
			this.Change(cont, -this.amount);
			base.On_UnAssign(cont);
		}

		private void Change(Character ch, int difference) {
			switch ((PotionStat) this.stat) {
				case PotionStat.Strength:
					ch.Str = (short) (ch.Str + difference);
					break;
				case PotionStat.Dexterity:
					ch.Dex = (short) (ch.Dex + difference);
					break;
				case PotionStat.Intelligence:
					ch.Int = (short) (ch.Int + difference);
					break;
				case PotionStat.Vitality:
					if (ch is Player player) {
						player.Vit = (short) (player.Vit + difference);
					}
					break;
				case PotionStat.MagicResist:
					ch.ModifySkillValue(SkillName.MagicResist, difference);
					break;
				case PotionStat.ResistParalyse:
					ch.ResistParalyse += difference;
					break;
				case PotionStat.Armor:
					ch.ArmorClassModifier += difference;
					break;
				case PotionStat.DragonResist:
					ch.SetTag(dragonResistTK, DragonResist(ch) + difference);
					ch.SysMessage(((difference > 0) ? "Vypil jsi lektvar, tvuj draci resist je nyni " : "Ucinek lektvaru skoncil, tvuj draci resist je nyni ") + DragonResist(ch) + "%");
					break;
				case PotionStat.NightSight:
					if (ch is Player seer) {
						seer.SendGlobalLightLevel(LightAndWeather.GetLightAt(seer));
					}
					break;
			}
		}

		public override string EffectName {
			get {
				return this.name;
			}
		}

		public override int EffectIcon {
			get {
				return this.icon;
			}
		}

		public override string EffectDetail {
			get {
				switch ((PotionStat) this.stat) {
					case PotionStat.Strength:
						return ActiveEffects.SignedStat(this.amount, "Str");
					case PotionStat.Dexterity:
						return ActiveEffects.SignedStat(this.amount, "Dex");
					case PotionStat.Intelligence:
						return ActiveEffects.SignedStat(this.amount, "Int");
					case PotionStat.Vitality:
						return ActiveEffects.SignedStat(this.amount, "Vit");
					case PotionStat.MagicResist:
						return "+" + (this.amount / 10.0).ToString("0.0", CultureInfo.InvariantCulture) + " Resist";
					case PotionStat.ResistParalyse:
						return "Odolnost proti paralyze";
					case PotionStat.Armor:
						return ActiveEffects.SignedStat(this.amount, "Armor");
					case PotionStat.DragonResist:
						return "+" + this.amount + "% proti drakum";
					default:
						return "";
				}
			}
		}
	}

	public partial class PotionEffectPluginDef {
	}
}
