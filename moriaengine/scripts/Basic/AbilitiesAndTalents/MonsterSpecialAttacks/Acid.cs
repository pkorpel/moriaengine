using System;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1022 e_acid_damage and f_acid (aciddamage.scp): a swing can splash every player within 4 tiles in sight. It eats a
	/// shield half the time, else a piece of armor picked at random; with nothing there it burns the skin. Dodging it by
	/// mi_meleeblock and ra_weapdodge waits for class abilities, #1017.</summary>
	[ViewableClass]
	public class AcidAbilityDef : PassiveAbilityDef {
		private FieldValue chance;
		private FieldValue damage;
		private FieldValue range;

		public AcidAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("acidChance", new double[] { 100, 100 }, typeof(double[]));
			this.damage = this.InitTypedField("acidDamage", new double[] { 50, 50 }, typeof(double[]));
			this.range = this.InitTypedField("acidRange", 4, typeof(int));
		}

		public new double[] Chance {
			get { return (double[]) this.chance.CurrentValue; }
			set { this.chance.CurrentValue = value; }
		}

		public double[] Damage {
			get { return (double[]) this.damage.CurrentValue; }
			set { this.damage.CurrentValue = value; }
		}

		public int Range {
			get { return (int) this.range.CurrentValue; }
			set { this.range.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private const int AcidSound = 868, ShieldChance = 50;
		private static readonly (LayerNames layer, int weight, string armorText, string skinText)[] acidLayers = {
			(LayerNames.Pants, 17, "Kyselina ti rozezira nohy.", "Kyselina ti popalila nohy."),
			(LayerNames.Helmet, 17, "Kyselina ti rozezira helmu.", "Kyselina te bolestive zasahla do obliceje."),
			(LayerNames.Gloves, 17, "Kyselina ti rozezira rukavice.", "Kyselina ti poleptala ruce."),
			(LayerNames.Collar, 13, "Kyselina ti rozezira limec.", "Kyselina ti popalila krk."),
			(LayerNames.Chest, 17, "Kyselina ti rozezira hrud.", "Kyselina te spalila na hrudi."),
			(LayerNames.Arms, 17, "Kyselina ti rozezira paze.", "Kyselina ti poleptala paze.")
		};

		internal static AcidAbilityDef AcidDef { get { return Lookup<AcidAbilityDef>("a_tevent_e_acid_damage"); } }

		internal static void TryAcidSplash(Character attacker) {
			AcidAbilityDef def = AcidDef;
			if (!HasAbilityDef(attacker, def)) {
				return;
			}
			NPCDef npcDef = attacker.Def as NPCDef;
			if (!RollChance(RollTunable(npcDef?.AcidChance, def.Chance))) {
				return;
			}
			int damage = (int) RollTunable(npcDef?.AcidDamage, def.Damage);
			foreach (AbstractCharacter near in attacker.GetMap().GetCharsInRange(attacker.X, attacker.Y, def.Range)) {
				if ((near is Player player) && !player.IsGM && !player.Flag_Dead && attacker.CanSeeLOS(player).Allow) {
					Splash(attacker, player, damage);
				}
			}
		}

		internal static void Splash(Character attacker, Player player, int damage) {
			player.Sound(AcidSound);
			if ((player.FindLayer(LayerNames.Hand2) is Destroyable shield) && CraftingSkillDef.IsOfType(shield, "t_shield") && (Globals.dice.Next(100) < ShieldChance)) {
				player.SysMessage("Podarilo se ti vykryt proud kyseliny stitem.");
				Eat(player, shield, (damage * Globals.dice.Next(4, 11)) / Math.Max(1, (int) shield.MaxDurability), "Tvuj stit se rozpadl !");
				return;
			}
			var part = PickAcidLayer();
			if ((player.FindLayer(part.layer) is Destroyable armor) && CraftingSkillDef.IsOfType(armor, "t_armor", "t_armor_leather")) {
				player.SysMessage(part.armorText);
				Eat(player, armor, (damage * 10) / Math.Max(1, (int) armor.MaxDurability), "Cast tve zbroje se rozpadla!");
				return;
			}
			player.SysMessage(part.skinText);
			DamageManager.CauseDamageNoResponse(attacker, player, DamageType.Acid, damage); //#1519 - aciddamage.scp's damage_noresponse
		}

		private static (LayerNames layer, int weight, string armorText, string skinText) PickAcidLayer() {
			int roll = Globals.dice.Next(98);
			foreach (var part in acidLayers) {
				roll -= part.weight;
				if (roll < 0) {
					return part;
				}
			}
			return acidLayers[0];
		}

		private static void Eat(Player player, Destroyable piece, int amount, string goneText) {
			if (piece.Durability > amount) {
				piece.Durability = (ushort) (piece.Durability - amount);
			} else {
				player.RedMessage(goneText);
				piece.Delete();
			}
		}
	}
}
