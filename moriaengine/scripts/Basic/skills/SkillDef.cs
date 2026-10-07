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
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public abstract class SkillDef : AbstractSkillDef {

		#region Accessors
		//this is from spheretables: all these should or could be defined here...
		//CSKILLDEFPROP(AdvRate,		0, "")	<--- done
		//CSKILLDEFPROP(Bonus_Dex,	0, "
		//CSKILLDEFPROP(Bonus_Int,	0, "")
		//CSKILLDEFPROP(Bonus_Str,	0, "")
		//CSKILLDEFPROP(BonusStats,	0, "")
		//CSKILLDEFPROP(Delay,		0, "")	<--- done
		//CSKILLDEFPROP(Effect,		0, "")	<--- done
		//CSKILLDEFPROP(Key,			0, "")	<--- is in AbstractSkillDef already
		//CSKILLDEFPROP(PromptMsg,	0, "")
		//CSKILLDEFPROP(Stat_Dex,		0, "")
		//CSKILLDEFPROP(Stat_Int,		0, "")
		//CSKILLDEFPROP(Stat_Str,		0, "")
		//CSKILLDEFPROP(Title,		0, "")
		//CSKILLDEFPROP(Values,		0, "")

		private FieldValue advRate;
		private FieldValue delay;
		private FieldValue effect;

		public SkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.advRate = this.InitTypedField("advRate", 0, typeof(double[]));
			this.delay = this.InitTypedField("delay", 0, typeof(double[]));
			this.effect = this.InitTypedField("effect", 0, typeof(double[]));
		}

		protected override void LoadScriptLine(string filename, int line, string param, string args) {
			base.LoadScriptLine(filename, line, param, args);//the AbstractThingDef Loadline
		}

		public static SkillDef GetBySkillName(SkillName name) {
			return (SkillDef) GetById((int) name);
		}

		public static ICollection<AbstractSkillDef> AllSkillDefs {
			get {
				return AllIndexedDefs;
			}
		}

		public double[] AdvRate {
			get {
				return (double[]) this.advRate.CurrentValue;
			}
			set {
				this.advRate.CurrentValue = value;
			}
		}

		public double MinAdvRate {
			get {
				return this.AdvRate[0];
			}
		}

		public double MaxAdvRate {
			get {
				double[] arr = this.AdvRate;
				return arr[arr.Length - 1];
			}
		}

		public int SkillValueOfChar(Character ch) {
			return ch.GetSkill(this.Id);
		}

		public static int SkillValueOfChar(Character ch, ushort id) {
			return ch.GetSkill(id);
		}

		public static int SkillValueOfChar(Character ch, SkillName id) {
			return ch.GetSkill((int) id);
		}

		public double AdvRateForValue(ushort skillValue) {
			return ScriptUtil.EvalRangePermille(skillValue, this.AdvRate);
		}

		public double AdvRateOfChar(Character ch) {
			return ScriptUtil.EvalRangePermille(this.SkillValueOfChar(ch), this.AdvRate);
		}

		public double[] Delay {
			get {
				return (double[]) this.delay.CurrentValue;
			}
			set {
				this.delay.CurrentValue = value;
			}
		}

		public double MinDelay {
			get {
				return this.Delay[0];
			}
		}

		public double MaxDelay {
			get {
				double[] arr = this.Delay;
				return arr[arr.Length - 1];
			}
		}

		public double GetDelayForValue(ushort skillValue) {
			return ScriptUtil.EvalRangePermille(skillValue, this.Delay);
		}

		public double GetDelayForChar(Character ch)
		{
			//GM is always immediate
			if (ch.IsGM) {
				return 0;
			}
			return ScriptUtil.EvalRangePermille(this.SkillValueOfChar(ch), this.Delay);
		}

		public double[] Effect {
			get {
				return (double[]) this.effect.CurrentValue;
			}
			set {
				this.effect.CurrentValue = value;
			}
		}

		public double GetEffectForValue(int skillValue) {
			return ScriptUtil.EvalRangePermille(skillValue, this.Effect);
		}

		public double GetEffectForChar(Character ch)
		{
			//GM is treated as having the maximal skill
			if (ch.IsGM) {
				return ScriptUtil.EvalRangePermille(1000.0, this.Effect);
			}
			return ScriptUtil.EvalRangePermille(this.SkillValueOfChar(ch), this.Effect);
		}
		#endregion Accessors

		public static bool CheckSuccess(int skillValue, int difficulty) {
			return SkillUtils.CheckSuccess(skillValue, difficulty);
		}

		public bool CheckSuccess(Character ch, int difficulty)
		{
			//GM is always successfull
			if (ch.IsGM) {
				return true;
			}
			return SkillUtils.CheckSuccess(this.SkillValueOfChar(ch), difficulty);
		}

		#region Triggers

		public static readonly TriggerKey tkAbort = TriggerKey.Acquire("abort");
		public static readonly TriggerKey tkSkillAbort = TriggerKey.Acquire("skillAbort");
		public static readonly TriggerKey tkFail = TriggerKey.Acquire("fail");
		public static readonly TriggerKey tkSkillFail = TriggerKey.Acquire("skillFail");
		public static readonly TriggerKey tkMakeItem = TriggerKey.Acquire("makeItem");
		public static readonly TriggerKey tkSkillMakeItem = TriggerKey.Acquire("skillMakeItem");
		public static readonly TriggerKey tkSelect = TriggerKey.Acquire("select");
		public static readonly TriggerKey tkSkillSelect = TriggerKey.Acquire("skillSelect");
		public static readonly TriggerKey tkStart = TriggerKey.Acquire("start");
		public static readonly TriggerKey tkSkillStart = TriggerKey.Acquire("skillStart");
		public static readonly TriggerKey tkStroke = TriggerKey.Acquire("stroke");
		public static readonly TriggerKey tkSkillStroke = TriggerKey.Acquire("skillStroke");
		public static readonly TriggerKey tkSuccess = TriggerKey.Acquire("success");
		public static readonly TriggerKey tkSkillSuccess = TriggerKey.Acquire("skillSuccess");
		public static readonly TriggerKey tkSkillChange = TriggerKey.Acquire("skillChange");
		//public static readonly TriggerKey tkGain = TriggerKey.Get("Gain");
		//public static readonly TriggerKey tkSkillGain = TriggerKey.Get("SkillGain");


		internal static void Trigger_ValueChanged(Character character, Skill skill, int oldModifiedValue) {
			int newValue = skill.ModifiedValue;
			ScriptArgs sa = new ScriptArgs(skill, oldModifiedValue);
			character.TryTrigger(tkSkillChange, sa);
			try {
				character.On_SkillChange(skill, oldModifiedValue);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		internal TriggerResult Trigger_Select(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			if (!self.CheckAliveWithMessage()) {
				return TriggerResult.Cancel;
			}

			var result = self.TryCancellableTrigger(tkSkillSelect, skillSeqArgs.scriptArgs);
			if (result != TriggerResult.Cancel) {
				try {
					result = self.On_SkillSelect(skillSeqArgs);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (result != TriggerResult.Cancel) {
					result = this.TryCancellableTrigger(self, tkSelect, skillSeqArgs.scriptArgs);
					if (result != TriggerResult.Cancel) {
						try {
							result = this.On_Select(skillSeqArgs);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					}
				}
			}
			return result;
		}

		internal TriggerResult Trigger_Start(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			var result = self.TryCancellableTrigger(tkSkillStart, skillSeqArgs.scriptArgs);
			if (result != TriggerResult.Cancel) {
				try {
					result = self.On_SkillStart(skillSeqArgs);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (result != TriggerResult.Cancel) {
					result = this.TryCancellableTrigger(self, tkStart, skillSeqArgs.scriptArgs);
					if (result != TriggerResult.Cancel) {
						try {
							result = this.On_Start(skillSeqArgs);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					}
				}
			}
			return result;
		}

		internal TriggerResult Trigger_Stroke(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			var result = self.TryCancellableTrigger(tkSkillStroke, skillSeqArgs.scriptArgs);
			if (result != TriggerResult.Cancel) {
				try {
					result = self.On_SkillStroke(skillSeqArgs);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (result != TriggerResult.Cancel) {
					result = this.TryCancellableTrigger(self, tkStroke, skillSeqArgs.scriptArgs);
					if (result != TriggerResult.Cancel) {
						try {
							result = this.On_Stroke(skillSeqArgs);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					}
				}
			}
			return result;
		}

		internal void Trigger_Fail(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			var result = self.TryCancellableTrigger(tkSkillFail, skillSeqArgs.scriptArgs);
			if (result != TriggerResult.Cancel) {
				try {
					result = self.On_SkillFail(skillSeqArgs);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (result != TriggerResult.Cancel) {
					result = this.TryCancellableTrigger(self, tkFail, skillSeqArgs.scriptArgs);
					if (result != TriggerResult.Cancel) {
						try {
							this.On_Fail(skillSeqArgs);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					}
				}
			}
		}

		internal void Trigger_Success(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			var result = self.TryCancellableTrigger(tkSkillSuccess, skillSeqArgs.scriptArgs);
			if (result != TriggerResult.Cancel) {
				try {
					result = self.On_SkillSuccess(skillSeqArgs);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (result != TriggerResult.Cancel) {
					result = this.TryCancellableTrigger(self, tkSuccess, skillSeqArgs.scriptArgs);
					if (result != TriggerResult.Cancel) {
						try {
							this.On_Success(skillSeqArgs);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					}
				}
			}
		}

		internal void Trigger_Abort(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			self.TryCancellableTrigger(tkSkillAbort, skillSeqArgs.scriptArgs);
			try {
				self.On_SkillAbort(skillSeqArgs);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			this.TryCancellableTrigger(self, tkAbort, skillSeqArgs.scriptArgs);
			try {
				this.On_Abort(skillSeqArgs);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		/// <summary>This method implements the Select phase of the skill.</summary>
		protected abstract TriggerResult On_Select(SkillSequenceArgs skillSeqArgs);

		/// <summary>This method implements the Start phase of the skill.</summary>
		protected abstract TriggerResult On_Start(SkillSequenceArgs skillSeqArgs);

		/// <summary>This method implements the \"stroke\" of the skill, that means some important moment \"in the middle\".</summary>
		protected abstract TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs);

		/// <summary>This method implements the failing of the skill. </summary>
		protected abstract void On_Fail(SkillSequenceArgs skillSeqArgs);

		/// <summary>This method implements the succes of the skill, a.e. skillgaiin and the success effect.</summary>
		protected abstract void On_Success(SkillSequenceArgs skillSeqArgs);

		/// <summary>This method implements the aborting of the skill. Unlike Fail, this happens before the regular end of the script delay, if there's any... </summary>
		protected abstract void On_Abort(SkillSequenceArgs skillSeqArgs);
		#endregion Triggers
	}

	[SaveableClass]
	public class SkillSequenceArgs {
		private Character self; //set when calling @Select
		private SkillDef skillDef; //set when calling @Select
		private IPoint3D target1, target2; //set in @Select or before it
		private object param1, param2; //set in @Select or before it
		private Item tool;
		private TimeSpan delay; //set in @Start
		private bool success;

		public readonly ScriptArgs scriptArgs;

		[LoadingInitializer]
		public SkillSequenceArgs() {
			this.scriptArgs = new ScriptArgs(this);
		}

		public static SkillSequenceArgs Acquire(Character self, SkillDef skillDef) {
			SkillSequenceArgs args = new SkillSequenceArgs();
			args.self = self;
			args.skillDef = skillDef;
			args.target1 = null;
			args.target2 = null;
			args.param1 = null;
			args.param2 = null;
			args.tool = null;
			args.success = false;
			args.delay = Timer.negativeOneSecond;
			return args;
		}

		public static SkillSequenceArgs Acquire(Character self, SkillDef skillDef, IPoint4D target1, IPoint4D target2, Item tool, object param1, object param2) {
			SkillSequenceArgs args = new SkillSequenceArgs();
			args.self = self;
			args.skillDef = skillDef;
			args.target1 = target1;
			args.target2 = target2;
			args.param1 = param1;
			args.param2 = param2;
			args.tool = tool;
			args.success = false;
			args.delay = Timer.negativeOneSecond;
			return args;
		}

		public static SkillSequenceArgs Acquire(Character self, SkillName skillName) {
			return Acquire(self, (SkillDef) AbstractSkillDef.GetById((int) skillName));
		}

		public static SkillSequenceArgs Acquire(Character self, SkillName skillName, Item tool) {
			return Acquire(self, (SkillDef) AbstractSkillDef.GetById((int) skillName), null, null, tool, null, null);
		}

		public static SkillSequenceArgs Acquire(Character self, SkillDef skillDef, Item tool) {
			return Acquire(self, skillDef, null, null, tool, null, null);
		}

		public static SkillSequenceArgs Acquire(Character self, SkillName skillName, Item tool, object param1) {
			return Acquire(self, (SkillDef) AbstractSkillDef.GetById((int) skillName), null, null, tool, param1, null);
		}

		public static SkillSequenceArgs Acquire(Character self, SkillName skillName, IPoint4D target1, IPoint4D target2, Item tool, object param1, object param2) {
			return Acquire(self, (SkillDef) AbstractSkillDef.GetById((int) skillName), target1, target2, tool, param1, param2);
		}

		public Character Self {
			get {
				return this.self;
			}
		}

		public SkillDef SkillDef {
			get {
				return this.skillDef;
			}
		}

		[Save]
		public void Save(SaveStream output) {
			if (this.self != null) {
				output.WriteValue("self", this.self);
			}
			if (this.skillDef != null) {
				output.WriteValue("skillDef", this.skillDef);
			}
		}

		[LoadLine]
		public void LoadLine(string filename, int line, string valueName, string valueString) {
			switch (valueName) {
				case "self":
					ObjectSaver.Load(valueString, delegate(object loaded, string f, int l) {
						this.self = (Character) loaded;
					}, filename, line);
					break;
				case "skilldef":
					this.skillDef = (SkillDef) ObjectSaver.OptimizedLoad_Script(valueName);
					break;
			}
		}

		[SaveableData]
		public IPoint3D Target1 {
			get {
				return this.target1;
			}
			set {
				this.target1 = value;
			}
		}

		[SaveableData]
		public IPoint3D Target2 {
			get {
				return this.target2;
			}
			set {
				this.target2 = value;
			}
		}

		[SaveableData]
		public object Param1 {
			get {
				return this.param1;
			}
			set {
				this.param1 = value;
			}
		}

		[SaveableData]
		public object Param2 {
			get {
				return this.param2;
			}
			set {
				this.param2 = value;
			}
		}

		[SaveableData]
		public Item Tool {
			get {
				return this.tool;
			}
			set {
				this.tool = value;
			}
		}

		[SaveableData]
		public TimeSpan DelaySpan {
			get {
				return this.delay;
			}
			set {
				this.delay = value;
			}
		}

		public double DelayInSeconds {
			get {
				return this.delay.TotalSeconds;
			}
			set {
				this.delay = TimeSpan.FromSeconds(value);
			}
		}

		[SaveableData]
		public bool Success {
			get {
				return this.success;
			}
			set {
				this.success = value;
			}
		}

		public void PhaseSelect() {
			if (TriggerResult.Cancel != this.skillDef.Trigger_Select(this)) {
				this.PhaseStart();
			}
		}

		public void PhaseStart() {
			this.DelayInSeconds = this.skillDef.GetDelayForChar(this.self);

			if (TriggerResult.Cancel != this.skillDef.Trigger_Start(this)) {
				AbortSkill(this.self);
				this.DelayStroke();
			}
		}

		public void DelayStroke() {
			if (this.delay < TimeSpan.Zero) {
				this.PhaseStroke();
			} else {
				this.self.AddTimer(skillTimerKey, new SkillStrokeTimer(this)).DueInSpan = this.delay;
			}
		}

		/// <summary>
		/// This method fires the @skillStroke triggers. 
		/// Gets usually called by the SkillTimer.
		/// </summary>
		public void PhaseStroke() {
			if (this.self.IsAliveAndValid) {
				if (TriggerResult.Cancel != this.skillDef.Trigger_Stroke(this)) {
					if (this.success) {
						this.PhaseSuccess();
					} else {
						this.PhaseFail();
					}
				}
			}
		}

		/// <summary>This method fires the @skillFail triggers. Gets usually called from the Stroke phase</summary>
		/// <remarks>Failing is something else than being forced to abort</remarks>
		public void PhaseFail() {
			this.skillDef.Trigger_Fail(this);
		}

		/// <summary>This method fires the @Success triggers. Gets usually called from the Stroke phase</summary>
		public void PhaseSuccess() {
			this.skillDef.Trigger_Success(this);
		}

		/// <summary>
		/// This method fires the @skillAbort triggers. 
		/// Gets usually called when the skill is interrupted \"from outside\" - no skillgain, etc.
		/// </summary>
		public void PhaseAbort() {
			this.skillDef.Trigger_Abort(this);
			//this.Dispose();
		}

		public static void AbortSkill(Character self) {
			SkillStrokeTimer timer = (SkillStrokeTimer) self.RemoveTimer(skillTimerKey);
			if (timer != null) {
				timer.skillSeqArgs.PhaseAbort();
				timer.skillSeqArgs = null;
				timer.Delete();
			}
		}

		public static SkillSequenceArgs GetSkillSequenceArgs(Character self) {
			SkillStrokeTimer timer = (SkillStrokeTimer) self.GetTimer(skillTimerKey);
			if (timer != null) {
				return timer.skillSeqArgs;
			}
			return null;
		}


		public static SkillStrokeTimer GetSkillSequenceTimer(Character self) {
			return (SkillStrokeTimer) self.GetTimer(skillTimerKey);
		}

		private static TimerKey skillTimerKey = TimerKey.Acquire("_skillTimer_");

		[SaveableClass]
		[DeepCopyableClass]
		public class SkillStrokeTimer : BoundTimer {

			[CopyableData]
			[SaveableData]
			public SkillSequenceArgs skillSeqArgs;

			[LoadingInitializer]
			[DeepCopyImplementation]
			public SkillStrokeTimer() {
			}

			public SkillStrokeTimer(SkillSequenceArgs skillSeqArgs) {
				Sanity.IfTrueThrow(skillSeqArgs == null, "skillSeqArgs == null");
				this.skillSeqArgs = skillSeqArgs;
			}

			protected sealed override void OnTimeout(TagHolder cont) {
				Logger.WriteDebug("SkillStrokeTimer OnTimeout on " + cont);
				this.skillSeqArgs.PhaseStroke();
			}
		}
	}
}