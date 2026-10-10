/*
This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
Or visit http://www.gnu.org/copyleft/gpl.html
*/

using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Networking;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	//temporary? Mozna to bude neco jako MovementBrain, kazdopadne to chci takhle na testovani
	[ViewableClass]
	[SaveableClass, DeepCopyableClass]
	public sealed class DynamicMovementSettings : IMovementSettings {

		[SaveableData, CopyableData]
		public bool canCrossLand;

		[SaveableData, CopyableData]
		public bool canSwim;

		[SaveableData, CopyableData]
		public bool canCrossLava;

		[SaveableData, CopyableData]
		public bool canFly;

		[SaveableData, CopyableData]
		public bool ignoreDoors;

		[SaveableData, CopyableData]
		public int climbPower;

		[LoadingInitializer, DeepCopyImplementation]
		public DynamicMovementSettings() {
			this.canCrossLand = true;
			this.canSwim = false;
			this.canCrossLava = false;
			this.canFly = false;
			this.ignoreDoors = false;
			this.climbPower = 2;
		}

		public void ShowValues() {
			Globals.SrcWriteLine("canCrossLand: " + this.canCrossLand);
			Globals.SrcWriteLine("canSwim: " + this.canSwim);
			Globals.SrcWriteLine("canCrossLava: " + this.canCrossLava);
			Globals.SrcWriteLine("canFly: " + this.canFly);
			Globals.SrcWriteLine("ignoreDoors: " + this.ignoreDoors);
			Globals.SrcWriteLine("climbPower: " + this.climbPower);
		}

		public DynamicMovementSettings(bool canCrossLand, bool canSwim, bool canCrossLava,
					bool canFly, bool ignoreDoors, int climbPower) {
			this.canCrossLand = canCrossLand;
			this.canSwim = canSwim;
			this.canCrossLava = canCrossLava;
			this.canFly = canFly;
			this.ignoreDoors = ignoreDoors;
			this.climbPower = climbPower;
		}

		bool IMovementSettings.CanCrossLand {
			get {
				return this.canCrossLand;
			}
		}
		bool IMovementSettings.CanSwim {
			get {
				return this.canSwim;
			}
		}
		bool IMovementSettings.CanCrossLava {
			get {
				return this.canCrossLava;
			}
		}
		bool IMovementSettings.CanFly {
			get {
				return this.canFly;
			}
		}
		bool IMovementSettings.IgnoreDoors {
			get {
				return this.ignoreDoors;
			}
		}
		int IMovementSettings.ClimbPower {
			get {
				return this.climbPower;
			}
		} //max positive difference in 1 step
	}

	/// <summary>How an NPC decides whether to attack a player it notices - see NPCAiTimer.ShouldAggroOn. Deliberately extensible: NPC/NPCDef subclasses can override ShouldAggroOn for policies beyond these three builtins (e.g. time-of-day, criminal-only) without touching the AI tick loop.</summary>
	public enum AggroMode {
		/// <summary>Never initiates an attack - the default, used by townsfolk/vendors/etc.</summary>
		Calm,
		/// <summary>Attacks any valid player that comes into aggro range, regardless of realm/notoriety - wild monsters.</summary>
		Hostile,
		/// <summary>Attacks players whose Realm is on the opposite side of this NPC's own Realm (Gondor vs Mordor, including outcasts) - town guards.</summary>
		GuardsRealm
	}

	[ViewableClass]
	public partial class NPCDef {
		private static readonly TagKey aggroModeTK = TagKey.Acquire("_aggroMode_");

		public AggroMode AggroMode {
			get {
				object tag = this.GetTag(aggroModeTK);
				if (tag is AggroMode mode) {
					return mode;
				}
				return AggroMode.Calm;
			}
			set {
				this.SetTag(aggroModeTK, value);
			}
		}

		private static readonly TagKey tamingDifficultyTK = TagKey.Acquire("_tamingDifficulty_");

		/// <summary>Minimum effective AnimalTaming skill needed to tame this NPC type (see TamingSkillDef), on the classic 0-1000 permille scale matching CheckSuccess's difficulty parameter. Negative (the default) means not tameable at all.</summary>
		public double TamingDifficulty {
			get {
				object tag = this.GetTag(tamingDifficultyTK);
				if (tag is double difficulty) {
					return difficulty;
				}
				return -1;
			}
			set {
				this.SetTag(tamingDifficultyTK, value);
			}
		}

		private static readonly TagKey defaultMovementTK = TagKey.Acquire("_defaultMovementCapability_");

		/// <summary>The movement capability template new NPCs of this type get on creation (see NPC.On_Create) - each instance gets its own deep copy, not this shared object, so runtime effects can mutate one NPC's capability without affecting every other NPC of the same type.</summary>
		public IMovementSettings DefaultMovementSettings {
			get {
				return (IMovementSettings) this.GetTag(defaultMovementTK);
			}
			set {
				this.SetTag(defaultMovementTK, value);
			}
		}

		/// <summary>
		/// Restores the legacy "CAN = MT_WALK|MT_FLY|..." .def property (converted content,
		/// enhancements #12) as a real DefaultMovementSettings template. Only the subset that
		/// actually maps to IMovementSettings is handled (WALK/RUN -> land, SWIM, FLY) -
		/// MT_USEHANDS/MT_EQUIP/MT_FIRE_IMMUNE/MT_MALE/MT_FEMALE/MT_GHOST/MT_NONMOVER aren't
		/// movement-capability concepts in this engine (equip/gender/immunity/etc - gender in
		/// particular is already derived from the model via CharModelInfo, not a separate flag)
		/// and are intentionally ignored rather than guessed at.
		/// </summary>
		protected override void LoadScriptLine(string filename, int line, string param, string args) {
			if (param == "can") {
				this.DefaultMovementSettings = ParseMovementCapability(args);
				return;
			}
			if (param == "tamingdifficulty") {
				this.TamingDifficulty = ConvertTools.ParseDouble(args);
				return;
			}
			base.LoadScriptLine(filename, line, param, args);
		}

		private static DynamicMovementSettings ParseMovementCapability(string args) {
			DynamicMovementSettings settings = new DynamicMovementSettings();
			settings.canCrossLand = false; //start from nothing, grant only what's explicitly listed
			foreach (string rawToken in args.Split('|')) {
				switch (rawToken.Trim().ToUpperInvariant()) {
					case "MT_WALK":
					case "MT_RUN":
						settings.canCrossLand = true;
						break;
					case "MT_SWIM":
						settings.canSwim = true;
						break;
					case "MT_FLY":
						settings.canFly = true;
						break;
				}
			}
			return settings;
		}
	}

	[ViewableClass]
	public partial class NPC : Character {

		public override void On_Create() {
			base.On_Create();
			NPCAiTimer.EnsureInstalled(this);

			IMovementSettings template = ((NPCDef) this.Def).DefaultMovementSettings;
			if (template != null) {
				this.MovementSettings = (IMovementSettings) DeepCopyFactory.GetCopy(template);
			}
		}

		/// <summary>Decides whether this NPC should pick `candidate` as an aggro target - see AggroMode. Virtual so NPC subclasses/scripts can layer on additional policies beyond the three builtins.</summary>
		public virtual bool ShouldAggroOn(Character candidate) {
			if ((candidate == null) || !candidate.IsPlayer || !candidate.IsAliveAndValid || (candidate == this)) {
				return false;
			}
			if (this.ControlMaster != null) {
				return false; //tamed pets don't pick fights on their own - no "guard me"/"kill" command system yet (follow-up)
			}
			switch (((NPCDef) this.Def).AggroMode) {
				case AggroMode.Hostile:
					return true;
				case AggroMode.GuardsRealm:
					return NPCAiTimer.IsOppositeRealm(this.Realm, candidate.Realm);
				default:
					return false;
			}
		}

		public override short MaxHits {
			get {
				return this.maxHitpoints;
			}
			set {
				if (value != this.maxHitpoints) {
					CharSyncQueue.AboutToChangeHitpoints(this);
					this.maxHitpoints = value;

					//check the hitpoints regeneration
					RegenerationPlugin.TryInstallPlugin(this, this.Hits, this.maxHitpoints, this.HitsRegenSpeed);
				}
			}
		}

		public override short MaxMana {
			get {
				return this.maxMana;
			}
			set {
				if (value != this.maxMana) {
					CharSyncQueue.AboutToChangeMana(this);
					this.maxMana = value;

					//regeneration...
					RegenerationPlugin.TryInstallPlugin(this, this.Mana, this.maxMana, this.ManaRegenSpeed);
					
					//meditation finish
					if (this.Mana >= this.MaxMana) {
						this.DeletePlugin(MeditationPlugin.meditationPluginKey);
					}
				}
			}
		}

		public override short MaxStam {
			get {
				return this.maxStamina;
			}
			set {
				if (value != this.maxStamina) {
					CharSyncQueue.AboutToChangeStamina(this);
					this.maxStamina = value;

					//regeneration...
					RegenerationPlugin.TryInstallPlugin(this, this.Stam, this.maxStamina, this.StamRegenSpeed);
				}
			}
		}

		public override IMovementSettings MovementSettings {
			get
			{
				if (this.movementSettings == null) {
					return base.MovementSettings;
				}
				return this.movementSettings;
			}
			set {
				this.movementSettings = value;
			}
		}
	}
}