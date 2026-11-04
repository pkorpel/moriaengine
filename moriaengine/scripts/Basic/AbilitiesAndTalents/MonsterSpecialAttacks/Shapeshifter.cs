using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1022 e_menavec, e_menavec_powerful (macros.scp), e_menavec_ledovka (verbatim.scp), with i_morfovatko and
	/// e_pojistka_morf: every other blow a Menivec may turn into something else for 20 s, a look only, its stats stay. Near
	/// death it returns to itself for good. Forms Erebor lists that don't exist are skipped.</summary>
	public abstract class ShapeshifterAbilityDef : PassiveAbilityDef {
		private FieldValue forms;
		private FieldValue morphOneIn;
		private FieldValue firstDelay;
		private FieldValue formSeconds;
		private FieldValue restSeconds;
		private FieldValue nearDeathRest;
		private FieldValue morphName;
		private FieldValue morphEmote;

		protected ShapeshifterAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.forms = this.InitTypedField("forms", "", typeof(string));
			this.morphOneIn = this.InitTypedField("morphOneIn", 2, typeof(int));
			this.firstDelay = this.InitTypedField("firstDelay", (double) 5, typeof(double));
			this.formSeconds = this.InitTypedField("formSeconds", (double) 20, typeof(double));
			this.restSeconds = this.InitTypedField("restSeconds", (double) 10, typeof(double));
			this.nearDeathRest = this.InitTypedField("nearDeathRest", (double) 1000, typeof(double));
			this.morphName = this.InitTypedField("morphName", "Menivec", typeof(string));
			this.morphEmote = this.InitTypedField("morphEmote", "polymorphing!", typeof(string));
		}

		/// <summary>The name it goes by in a form.</summary>
		public string MorphName {
			get { return (string) this.morphName.CurrentValue; }
			set { this.morphName.CurrentValue = value; }
		}

		/// <summary>The emote at each change.</summary>
		public string MorphEmote {
			get { return (string) this.morphEmote.CurrentValue; }
			set { this.morphEmote.CurrentValue = value; }
		}

		/// <summary>Defnames, split by commas.</summary>
		public string Forms {
			get { return (string) this.forms.CurrentValue; }
			set { this.forms.CurrentValue = value; }
		}

		public int MorphOneIn {
			get { return (int) this.morphOneIn.CurrentValue; }
			set { this.morphOneIn.CurrentValue = value; }
		}

		public double FirstDelay {
			get { return (double) this.firstDelay.CurrentValue; }
			set { this.firstDelay.CurrentValue = value; }
		}

		public double FormSeconds {
			get { return (double) this.formSeconds.CurrentValue; }
			set { this.formSeconds.CurrentValue = value; }
		}

		public double RestSeconds {
			get { return (double) this.restSeconds.CurrentValue; }
			set { this.restSeconds.CurrentValue = value; }
		}

		public double NearDeathRest {
			get { return (double) this.nearDeathRest.CurrentValue; }
			set { this.nearDeathRest.CurrentValue = value; }
		}
	}

	[ViewableClass]
	public class MenivecAbilityDef : ShapeshifterAbilityDef {
		public MenivecAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}
	}

	[ViewableClass]
	public class PowerfulMenivecAbilityDef : ShapeshifterAbilityDef {
		public PowerfulMenivecAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}
	}

	[ViewableClass]
	public class IceMenivecAbilityDef : ShapeshifterAbilityDef {
		public IceMenivecAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}
	}

	/// <summary>#1022 e_ztracene_mesto_druid_medved (the Ztracene mesto druid): Menivec's mechanism with a bear and a wolf, as
	/// "Druid", "changing!". A look only, as Menivec's port.</summary>
	[ViewableClass]
	public class DruidMorphAbilityDef : ShapeshifterAbilityDef {
		public DruidMorphAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}
	}

	public static partial class MonsterSpecialAttacks {
		private const int SparkleEffect = 0x375A; //i_fx_sparkle; was 0x373A before #1022's druid
		internal static readonly TimerKey shapeshiftTK = TimerKey.Acquire("_npcShapeshift_");
		internal static readonly TagKey nextShapeshiftTK = TagKey.Acquire("_npcNextShapeshift_");
		private static readonly TagKey ownModelTK = TagKey.Acquire("_npcOwnModel_");
		private static readonly TagKey ownNameTK = TagKey.Acquire("_npcOwnName_");

		internal static MenivecAbilityDef MenivecDef { get { return Lookup<MenivecAbilityDef>("a_tevent_e_menavec"); } }
		internal static PowerfulMenivecAbilityDef PowerfulMenivecDef { get { return Lookup<PowerfulMenivecAbilityDef>("a_tevent_e_menavec_powerful"); } }
		internal static IceMenivecAbilityDef IceMenivecDef { get { return Lookup<IceMenivecAbilityDef>("a_tevent_e_menavec_ledovka"); } }
		internal static DruidMorphAbilityDef DruidMorphDef { get { return Lookup<DruidMorphAbilityDef>("a_tevent_e_ztracene_mesto_druid_medved"); } }

		private static ShapeshifterAbilityDef ShapeshifterOf(NPC self) {
			if (HasAbilityDef(self, MenivecDef)) {
				return MenivecDef;
			}
			if (HasAbilityDef(self, DruidMorphDef)) {
				return DruidMorphDef;
			}
			return HasAbilityDef(self, PowerfulMenivecDef) ? PowerfulMenivecDef : (HasAbilityDef(self, IceMenivecDef) ? (ShapeshifterAbilityDef) IceMenivecDef : null);
		}

		internal static void TryShapeshift(NPC self) {
			ShapeshifterAbilityDef def = ShapeshifterOf(self);
			if (def == null) {
				return;
			}
			double now = Globals.TimeInSeconds;
			if (self.GetTimer(shapeshiftTK) != null) {
				if (self.Hits < (self.MaxHits / 10)) {
					self.SetTag(nextShapeshiftTK, now + def.NearDeathRest);
					EndShapeshift(self, def, false);
				}
				return;
			}
			if (!(self.GetTag(nextShapeshiftTK) is double next)) {
				self.SetTag(nextShapeshiftTK, now + def.FirstDelay);
			} else if ((now >= next) && OneIn(def.MorphOneIn)) {
				Shapeshift(self, def);
			}
		}

		internal static void Shapeshift(NPC self, ShapeshifterAbilityDef def) {
			string[] forms = def.Forms.Split(',');
			NPCDef form = ThingDef.GetByDefname(forms[Globals.dice.Next(forms.Length)].Trim()) as NPCDef;
			if (form == null) {
				return;
			}
			self.SetTag(ownModelTK, self.Model);
			self.SetTag(ownNameTK, self.Name);
			EffectFactory.StationaryEffect(self, SparkleEffect, 6, 20);
			self.Emote(def.MorphEmote);
			self.Model = form.Model;
			self.Name = def.MorphName;
			self.AddTimer(shapeshiftTK, new ShapeshiftTimer()).DueInSeconds = def.FormSeconds;
		}

		internal static void EndShapeshift(NPC self, ShapeshifterAbilityDef def, bool rest) {
			self.DeleteTimer(shapeshiftTK);
			if (self.GetTag(ownModelTK) is int model) {
				self.Model = model;
				self.Name = (string) self.GetTag(ownNameTK);
				self.RemoveTag(ownModelTK);
				self.RemoveTag(ownNameTK);
				EffectFactory.StationaryEffect(self, SparkleEffect, 6, 20);
				self.Emote(def.MorphEmote);
			}
			if (rest) {
				self.SetTag(nextShapeshiftTK, Globals.TimeInSeconds + def.RestSeconds);
			}
		}

		internal static void ShapeshiftEnded(NPC self) {
			ShapeshifterAbilityDef def = ShapeshifterOf(self);
			if (def != null) {
				EndShapeshift(self, def, true);
			}
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class ShapeshiftTimer : BoundTimer {
		[LoadingInitializer, DeepCopyImplementation]
		public ShapeshiftTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			MonsterSpecialAttacks.ShapeshiftEnded((NPC) cont);
		}
	}
}
