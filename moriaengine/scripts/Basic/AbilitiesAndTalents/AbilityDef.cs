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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public class AbilityDef : AbstractIndexedDef<AbilityDef, string> {
		//private static Dictionary<string, AbilityDef> byName = new Dictionary<string, AbilityDef>(StringComparer.OrdinalIgnoreCase);

		//private static Dictionary<string, ConstructorInfo> abilityDefCtorsByName = new Dictionary<string, ConstructorInfo>(StringComparer.OrdinalIgnoreCase);
		//string-ConstructorInfo pairs  ("AbilityDef" - AbilityDef.ctor)

		#region Accessors
		//private string name; //logical name of the ability

		private FieldValue chance;
		private FieldValue cooldown;
		private FieldValue icon;
		private FieldValue resources;//resourcelist of resources to be consumed for ability using
		private FieldValue requirements;//resourcelist of resources that player must have intending to run the ability
		private FieldValue effectPower;
		private FieldValue effectDuration;

		public new static AbilityDef GetByDefname(string defname) {
			return AbstractScript.GetByDefname(defname) as AbilityDef;
		}

		public static AbilityDef GetByName(string key) {
			return GetByDefIndex(key);
		}

		public static int AbilitiesCount {
			get {
				return IndexedCount;
			}
		}

		/// <summary>Return enumerable containing all abilities (copying the values from the main dictionary)</summary>
		//public static Dictionary<string,AbilityDef>.ValueCollection AllAbilities {
		public static ICollection<AbilityDef> AllAbilities {
			get {
				return AllIndexedDefs;
			}
		}

		public AbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.cooldown = this.InitTypedField("cooldown", 0, typeof(double));
			this.icon = this.InitTypedField("icon", 0, typeof(int));
			this.chance = this.InitTypedField("chance", 1, typeof(double));
			this.resources = this.InitTypedField("resources", null, typeof(ResourcesList));
			this.requirements = this.InitTypedField("requirements", null, typeof(ResourcesList));
			this.effectPower = this.InitTypedField("effectPower", 1.0, typeof(double));
			this.effectDuration = this.InitTypedField("effectDuration", 5.0, typeof(double));
		}

		public ResourcesList Requirements {
			get {
				return (ResourcesList) this.requirements.CurrentValue;
			}
			set {
				this.requirements.CurrentValue = value;
			}
		}

		public ResourcesList Resources {
			get {
				return (ResourcesList) this.resources.CurrentValue;
			}
			set {
				this.resources.CurrentValue = value;
			}
		}

		public string Name {
			get {
				return this.DefIndex;
			}
		}

		/// <summary>
		/// Used in some abilities to compute the probabilty of their success. 
		/// Typically 1.0 = 100%
		/// </summary>
		public double Chance {
			get {
				return (double) this.chance.CurrentValue;
			}
			set {
				this.chance.CurrentValue = value;
			}
		}

		public bool CheckSuccess(Character target) {
			return this.CheckSuccess(target.GetAbility(this));
		}

		public bool CheckSuccess(Ability ab) {
			return this.CheckSuccess(ab.ModifiedPoints);
		}

		public bool CheckSuccess(int points) {
			return Globals.dice.NextDouble() <= (points * this.Chance);
		}

		/// <summary>
		/// Field for holding the number of seconds between next activation try.
		/// You can use 0 for no delay
		/// </summary>
		/// <summary>#923 - the gump picture of this ability on .boosts, 0 for none.</summary>
		public int Icon {
			get {
				return (int) this.icon.CurrentValue;
			}
			set {
				this.icon.CurrentValue = value;
			}
		}

		public double Cooldown {
			get {
				return (double) this.cooldown.CurrentValue;
			}
			set {
				this.cooldown.CurrentValue = value;
			}
		}

		/// <summary>
		/// Field for holding the number of seconds between next activation try.
		/// You can use 0 for no delay
		/// </summary>
		[NoShow]
		public TimeSpan CooldownAsSpan {
			get {
				return TimeSpan.FromSeconds(this.Cooldown);
			}
		}

		/// <summary>Used in some abilities to compute the power of their effect</summary>
		public double EffectPower {
			get {
				return (double) this.effectPower.CurrentValue;
			}
			set {
				this.effectPower.CurrentValue = value;
			}
		}

		/// <summary>Used in some abilities to compute the duration of their effect. Typically in seconds.</summary>
		public double EffectDuration {
			get {
				return (double) this.effectDuration.CurrentValue;
			}
			set {
				this.effectDuration.CurrentValue = value;
			}
		}


		#endregion Accessors

		#region Factory methods
		/// <summary>Method for instatiating Abilities.</summary>
		public virtual Ability Create(Character chr) {
			return new Ability(this, chr);
		}

		/// <summary>
		/// Overall method for running the abilites. Its basic implementation looks if the character has given ability
		/// and in case he has, it runs the protected activation method
		/// </summary>
		public virtual void Activate(Character chr) {
			Ability ab = chr.GetAbilityObject(this);

			DenyResult retVal = this.Trigger_DenyActivate(chr, ab); //return value means only that the trigger has been cancelled

			if (retVal.Allow) {
				ab.LastUsage = Globals.TimeAsSpan; //set the last usage time
				this.Trigger_Activate(chr, ab);
			} else {
				retVal.SendDenyMessage(chr); //send result(message) of the "activate" call to the client
			}
		}
		#endregion Factory methods

		#region Trigger methods

		internal static readonly TriggerKey tkAssign = TriggerKey.Acquire("assign");
		internal static readonly TriggerKey tkUnAssign = TriggerKey.Acquire("unAssign");
		internal static readonly TriggerKey tkAbilityValueChanged = TriggerKey.Acquire("abilityValueChanged");
		internal static readonly TriggerKey tkValueChanged = TriggerKey.Acquire("valueChanged");
		internal static readonly TriggerKey tkActivateAbility = TriggerKey.Acquire("activateAbility");
		internal static readonly TriggerKey tkActivate = TriggerKey.Acquire("activate");
		internal static readonly TriggerKey tkDenyActivateAbility = TriggerKey.Acquire("denyActivateAbility");
		internal static readonly TriggerKey tkDenyActivate = TriggerKey.Acquire("denyActivate");

		private TriggerGroup scriptedTriggers;

		/// <summary>
		/// LScript based @activate triggers
		/// Gets called when every prerequisity has been fulfilled and the ability can be run now
		/// </summary>
		protected void Trigger_Activate(Character chr, Ability ab) {
			ScriptArgs sa = new ScriptArgs(this, ab);

			var result = chr.TryCancellableTrigger(tkActivateAbility, sa);
			if (result != TriggerResult.Cancel) {
				try {
					result = chr.On_ActivateAbility(this, ab);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (result != TriggerResult.Cancel) {
					result = this.TryCancellableTrigger(chr, tkActivate, null);
					if (result != TriggerResult.Cancel) {
						try {
							this.On_Activate(chr, ab);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					}
				}
			}
		}

		/// <summary>C# based @activate trigger method</summary>
		protected virtual void On_Activate(Character chr, Ability ab) {
			chr.SysMessage(string.Format(CultureInfo.InvariantCulture,
				Loc<AbilityDefLoc>.Get(chr.Language).AbilityActivated,
				ab.Def.Name));
		}

		/// <summary>
		/// This method fires the @denyUse triggers. 
		/// Their purpose is to check if all requirements for running the ability have been met
		/// </summary>
		protected DenyResult Trigger_DenyActivate(Character chr, Ability ab) {
			if (ab == null || ab.ModifiedPoints == 0) {
				return DenyResultMessages_Abilities.Deny_DoesntHaveAbility;
			}

			DenyAbilityArgs denyArgs = new DenyAbilityArgs(chr, this, ab);

			var result = chr.TryCancellableTrigger(tkDenyActivateAbility, denyArgs);
			if (result != TriggerResult.Cancel) {
				try {
					result = chr.On_DenyActivateAbility(denyArgs);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (result != TriggerResult.Cancel) {
					result = this.TryCancellableTrigger(chr, tkDenyActivate, denyArgs);
					if (result != TriggerResult.Cancel) {
						try {
							this.On_DenyActivate(denyArgs);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					}
				}
			}
			return denyArgs.Result;
		}

		/// <summary>C# based @denyUse trigger method, implementation of common checks</summary>
		protected virtual void On_DenyActivate(DenyAbilityArgs args) {
			Ability ab = args.ranAbility;
			//check cooldown - Ability's own ctor comment explains why LastUsage's "never used"
			//sentinel is TimeSpan.MinValue, not Zero. That sentinel must never reach the
			//subtraction below: Globals.TimeAsSpan - TimeSpan.MinValue always throws
			//OverflowException (MinValue has no positive counterpart, same class as
			//int.MinValue negation) - confirmed as the real cause of a repeating "TimeSpan
			//overflowed" error spam on any never-yet-activated ability with a real Cooldown
			//(e.g. the stub abilities the 2026-10-26 monster-testing batch grants NPCs).
			//Short-circuits via && so the overflow-prone subtraction is never evaluated at all
			//when nothing has been recorded yet - "never used" is never on cooldown, period.
			if (ab.LastUsage != TimeSpan.MinValue
				&& ((Globals.TimeAsSpan - ab.LastUsage) <= this.CooldownAsSpan)
				&& !args.abiliter.IsGM) { //check the timing if OK
				args.Result = DenyResultMessages_Abilities.Deny_NotYetCooledDown;
				return;
			}

			//check resources present (if needed)
			ResourcesList resPresent = this.Requirements;
			if (resPresent != null) {
				IResourceListEntry missingItem;
				if (!resPresent.HasResourcesPresent(args.abiliter, ResourcesLocality.BackpackAndLayers, out missingItem)) {
					args.abiliter.SysMessage(missingItem.GetResourceMissingMessage(args.abiliter.Language));
					args.Result = DenyResultMessages_Abilities.Deny_NotEnoughResourcesPresent;
					return;
				}
			}

			//check consumable resources
			ResourcesList resConsum = this.Resources;
			if (resConsum != null) {
				//look to the backpack and among the items that we are wearing
				IResourceListEntry missingItem;
				if (!resConsum.ConsumeResourcesOnce(args.abiliter, ResourcesLocality.BackpackAndLayers, out missingItem)) {
					args.abiliter.SysMessage(missingItem.GetResourceMissingMessage(args.abiliter.Language));
					args.Result = DenyResultMessages_Abilities.Deny_NotEnoughResourcesToConsume;
					return;
				}
			}

			args.Result = args.abiliter.CheckAlive();
		}

		//this is not a character trigger. I think for them the @valuechanged should be enough
		private void Trigger_Assign(Character chr, Ability ab, ScriptArgs sa) {
			this.TryTrigger(chr, tkAssign, sa);
			try {
				this.On_Assign(chr, ab);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		/// <summary>This method implements the assigning of the first point to the Ability</summary>
		protected virtual void On_Assign(Character ch, Ability ab) {
			//ch.SysMessage("Abilita " + Name + " nem� implementaci trigger metody On_Assign");
		}

		//this is not a character trigger. I think for them the @valuechanged should be enough
		internal void Trigger_UnAssign(Character chr, Ability ab, ScriptArgs sa) {
			this.TryTrigger(chr, tkUnAssign, sa);
			try {
				this.On_UnAssign(chr, ab);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		/// <summary>This method implements the assigning of the first point to the Ability</summary>
		protected virtual void On_UnAssign(Character ch, Ability ab) {
			//ch.SysMessage("Abilita " + Name + " nem� implementaci trigger metody On_Assign");
		}

		internal void Trigger_ValueChanged(Character chr, Ability ab, int previousValue) {
			ScriptArgs sa = new ScriptArgs(this, ab, previousValue);
			chr.TryTrigger(tkAbilityValueChanged, sa);
			try {
				chr.On_AbilityValueChanged(this, ab, previousValue);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }

			this.TryTrigger(chr, tkValueChanged, sa);
			try {
				this.On_ValueChanged(chr, ab, previousValue);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }

			int newValue = ab.ModifiedPoints;
			if (previousValue == 0) {
				if (newValue > 0) {
					this.Trigger_Assign(chr, ab, sa);
				} else {
					Logger.WriteWarning("previousValue == 0 && newValue == " + newValue, new StackTrace());
				}
			} else if (newValue == 0) { //should mean that the previous value was positive
				if (previousValue > 0) {
					this.Trigger_UnAssign(chr, ab, sa);
				} else {
					Logger.WriteWarning("newValue == 0 && previousValue == " + previousValue, new StackTrace());
				}
			}
		}

		protected virtual void On_ValueChanged(Character ch, Ability ab, int previousValue) {
		}

		public TriggerResult TryCancellableTrigger(AbstractCharacter self, TriggerKey td, ScriptArgs sa) {
			//cancellable trigger just for the one triggergroup
			if (this.scriptedTriggers != null) {
				if (TagMath.Is1(this.scriptedTriggers.TryRun(self, td, sa))) {
					return TriggerResult.Cancel;
				}
			}
			return TriggerResult.Continue;
		}

		public void TryTrigger(AbstractCharacter self, TriggerKey td, ScriptArgs sa) {
			if (this.scriptedTriggers != null) {
				this.scriptedTriggers.TryRun(self, td, sa);
			}
		}
		#endregion Trigger methods

		#region Loading from scripts
		public override void LoadScriptLines(PropsSection ps) {
			PropsLine p = ps.PopPropsLine("name");
			this.DefIndex = ConvertTools.LoadSimpleQuotedString(p.Value);
			this.Unregister();

			base.LoadScriptLines(ps);

			if (ps.TriggerCount > 0) {
				ps.HeaderName = "t__" + this.Defname + "__";
				this.scriptedTriggers = ScriptedTriggerGroup.Load(ps);
			}
		}

		protected override void On_AfterLoadFromScripts() {
			base.On_AfterLoadFromScripts();

			ResourcesList.ThrowIfNotConsumable(this.Resources);
		}

		public override void Unload() {
			if (this.scriptedTriggers != null) {
				this.scriptedTriggers.Unload();
			}
			base.Unload();
		}
		#endregion Loading from scripts
	}

	public class DenyAbilityArgs : DenyTriggerArgs {
		public readonly Character abiliter;
		public readonly AbilityDef ranAbilityDef;
		public readonly Ability ranAbility;

		public DenyAbilityArgs(Character abiliter, AbilityDef ranAbilityDef, Ability ranAbility)
			: base(DenyResultMessages.Allow, abiliter, ranAbilityDef, ranAbility) {
			this.abiliter = abiliter;
			this.ranAbilityDef = ranAbilityDef;
			this.ranAbility = ranAbility; //this can be null (if we dont have the ability)
		}
	}

	//abilities running possible results
	public static class DenyResultMessages_Abilities {
		public static readonly DenyResult Deny_DoesntHaveAbility =
			new CompiledLocDenyResult<AbilityDefLoc>("YouDontHaveThisAbility"); //we don't have the ability (no points in it)
		public static readonly DenyResult Deny_NotYetCooledDown =
			new CompiledLocDenyResult<AbilityDefLoc>("NotYetCooledDown"); //the ability usage timer has not yet passed
		public static readonly DenyResult Deny_HasBeenSwitchedOff =
			new CompiledLocDenyResult<AbilityDefLoc>("HasBeenSwitchedOff"); //the ability was currently running (for ActivableAbilities only) so we switched it off
		public static readonly DenyResult Deny_NotEnoughResourcesToConsume =
			new CompiledLocDenyResult<AbilityDefLoc>("NotEnoughResourcesToConsume"); //missing some resources from "to consume" list
		public static readonly DenyResult Deny_NotEnoughResourcesPresent =
			new CompiledLocDenyResult<AbilityDefLoc>("NotEnoughResourcesPresent"); //missing some resources from "has present" list
		public static readonly DenyResult Deny_NotAllowedToHaveThisAbility =
			new CompiledLocDenyResult<AbilityDefLoc>("NotAllowedToHaveAbility"); //other reason why not allow to have the ability (e.g. wrong profession etc.)
	}

	public class AbilityDefLoc : CompiledLocStringCollection<AbilityDefLoc> {
		public string AbilityActivated = "Abilita {0} aktivovana.";
		public string YouDontHaveThisAbility = "O teto abilite nevi vubec nic.";
		public string NotYetCooledDown = "Abilitu nelze pouzit tak brzy po predchozim pouziti.";
		public string HasBeenSwitchedOff = "Abilita deaktivovana.";
		public string NotEnoughResourcesToConsume = "Nedostatek zdroju ke spotrebe pro aktivaci ability.";
		public string NotEnoughResourcesPresent = "Nedostatek zdroju pro aktivaci ability.";
		public string NotAllowedToHaveAbility = "Nejsi opravnen mit tuto abilitu.";
	}
}
