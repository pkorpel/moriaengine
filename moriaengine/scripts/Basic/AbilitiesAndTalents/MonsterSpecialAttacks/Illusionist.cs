using System;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1022 e_illusionist (macros.scp): one blow in 30 the monster blurs into five copies of one hit each, for 30 s, and
	/// slips to a tile beside itself. Erebor's 60 s wait never worked, it sets tag.waitill and reads tag.waittill, so there's none.</summary>
	[ViewableClass]
	public class IllusionistAbilityDef : PassiveAbilityDef {
		private FieldValue illusionOneIn;
		private FieldValue copyLifetime;
		private FieldValue copyMana;

		public IllusionistAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.illusionOneIn = this.InitTypedField("illusionOneIn", 30, typeof(int));
			this.copyLifetime = this.InitTypedField("copyLifetime", (double) 30, typeof(double));
			this.copyMana = this.InitTypedField("copyMana", 500, typeof(int));
		}

		public int IllusionOneIn {
			get { return (int) this.illusionOneIn.CurrentValue; }
			set { this.illusionOneIn.CurrentValue = value; }
		}

		public double CopyLifetime {
			get { return (double) this.copyLifetime.CurrentValue; }
			set { this.copyLifetime.CurrentValue = value; }
		}

		public int CopyMana {
			get { return (int) this.copyMana.CurrentValue; }
			set { this.copyMana.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static readonly TagKey isIllusionTK = TagKey.Acquire("_npcIsIllusion_");
		private static readonly (int x, int yLeast, int yMost)[] illusionSpots = { (1, -1, 1), (-2, -1, 2), (-2, -2, -2), (-2, -2, -2), (0, 1, 1) };

		internal static IllusionistAbilityDef IllusionistDef { get { return Lookup<IllusionistAbilityDef>("a_tevent_e_illusionist"); } }

		internal static void TryIllusion(NPC self, Character attacker) {
			IllusionistAbilityDef def = IllusionistDef;
			if (HasAbilityDef(self, def) && !self.HasTag(isIllusionTK) && OneIn(def.IllusionOneIn)) {
				MakeIllusions(self, attacker, def);
			}
		}

		internal static void MakeIllusions(NPC self, Character attacker, IllusionistAbilityDef def) {
			attacker.SysMessage("Tvuj pohled se rozmazal.");
			foreach ((int x, int yLeast, int yMost) in illusionSpots) {
				NPC copy = (NPC) self.Def.Create((ushort) (self.X + x), (ushort) (self.Y + Globals.dice.Next(yLeast, yMost + 1)), self.Z, self.M);
				copy.MaxHits = 1;
				copy.Hits = 1;
				copy.MaxMana = (short) def.CopyMana;
				copy.Mana = (short) def.CopyMana;
				copy.SetTag(isIllusionTK, 1);
				SummonSpellDef.MarkAsSummonedCreature(copy);
				copy.AddTimer(summonedCreatureDespawnTimerKey, new SummonSpellDef.SummonDespawnTimer()).DueInSpan = TimeSpan.FromSeconds(def.CopyLifetime);
			}
			self.P((ushort) (self.X + Globals.dice.Next(-1, 2)), (ushort) (self.Y + Globals.dice.Next(-1, 2)), self.Z);
		}
	}
}
