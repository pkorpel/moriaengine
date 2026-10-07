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
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public class TrackingSkillDef : SkillDef {
		//tag key for the tag with the list of trackers to the tracked character
		internal static TagKey trackedByTK = TagKey.Acquire("_tracked_by_");

		/// <summary>Maximal age [sec] of the footsteps to be tracked at skill 0</summary>
		private FieldValue minFootstepAge;
		/// <summary>Maximal age [sec] of the footsteps to be tracked at skill 100</summary>
		private FieldValue maxFootstepAge;

		/// <summary>Maximum characters to be recognized at skill 0</summary>
		private FieldValue minCharsToTrack;
		/// <summary>Maximum characters to be recognized at skill 100</summary>
		private FieldValue maxCharsToTrack;

		/// <summary>Max steps before tracking chance is recomputed at skill 0</summary>
		private FieldValue minSafeSteps;
		/// <summary>Max steps before tracking chance is recomputed at skill 100</summary>
		private FieldValue maxSafeSteps;

		/// <summary>Similar as Effect but for tracking monsters,animals and NPCs</summary>
		private FieldValue pvmEffect;

		public TrackingSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.minFootstepAge = this.InitTypedField("minFootstepAge", 15, typeof(double));
			this.maxFootstepAge = this.InitTypedField("maxFootstepAge", 120, typeof(double));
			this.minCharsToTrack = this.InitTypedField("minCharsToTrack", 3, typeof(int));
			this.maxCharsToTrack = this.InitTypedField("maxCharsToTrack", 20, typeof(int));
			this.minSafeSteps = this.InitTypedField("minSafeSteps", 1, typeof(int));
			this.maxSafeSteps = this.InitTypedField("maxSafeSteps", 10, typeof(int));
			this.pvmEffect = this.InitTypedField("pvmEffect", new[] { 16.0, 64, 0 }, typeof(double[]));
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			//todo: paralyzed state etc.
			if (!this.CheckPrerequisities(skillSeqArgs)) {
				return TriggerResult.Cancel; //finish now
			}
			Character self = skillSeqArgs.Self;
			self.ClilocSysMessage(1011350);//What do you wish to track?
			self.Dialog(self, SingletonScript<D_Tracking_Categories>.Instance, new DialogArgs(skillSeqArgs));
			return TriggerResult.Cancel; //stop it, other triggers will be run from the tracking dialog
		}

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Continue; //continue to delay, then @stroke
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;

			if (!this.CheckPrerequisities(skillSeqArgs)) {
				return TriggerResult.Cancel;
			}
			switch ((TrackingEnums) skillSeqArgs.Param2) {
				case TrackingEnums.Phase_Characters_Seek:
					skillSeqArgs.Success = this.CheckSuccess(self, Globals.dice.Next(700)); //normally check for success...
					break;
				case TrackingEnums.Phase_Character_Track: //we will try to display the chars path
					skillSeqArgs.Success = true;//select the character to display its footsteps or to navigate to it, no need to check for success
					break;
			}

			return TriggerResult.Continue; //continue to @success or @fail
		}

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			CharacterTypes charType = (CharacterTypes) skillSeqArgs.Param1;
			TimeSpan now = Globals.TimeAsSpan;

			if (charType == CharacterTypes.Players) {//tracking Players
				ImmutableRectangle playerRect = this.GetPlayerTrackingArea(self);
				TimeSpan maxAge = this.GetMaxFootstepsAge(self);
				switch ((TrackingEnums) skillSeqArgs.Param2) {
					case TrackingEnums.Phase_Characters_Seek: //we will look for chars around
						List<AbstractCharacter> charsAround = ScriptSector.GetCharactersInRectangle(playerRect, now, maxAge, self.M);

						//check if tracking is possible (with message)
						//i.e. too many chars or none at all
						if (this.CheckTrackImpossible(skillSeqArgs, charsAround.Count, charType)) {
							return;
						}

						//display a dialog with the found trackable characters
						self.ClilocSysMessage(1018093);//Select the one you would like to track.
						self.Dialog(self, SingletonScript<D_Tracking_Characters>.Instance, new DialogArgs(skillSeqArgs, charsAround));
						break;
					case TrackingEnums.Phase_Character_Track: //we will try to display the chars path
						Character trackedChar = (Character) skillSeqArgs.Target1;

						//and forward the tracking management to the special plugin
						PlayerTrackingPlugin.InstallOnChar(self, trackedChar, playerRect, maxAge, this.GetMaxSafeSteps(self));

						break;
				}
			} else {//tracking animals, monsters or NPCs
				ImmutableRectangle npcRect = this.GetNPCTrackingArea(self);
				switch ((TrackingEnums) skillSeqArgs.Param2) {
					case TrackingEnums.Phase_Characters_Seek: //we will look for chars around
						List<AbstractCharacter> trackables = null;
						switch (charType) {
							case CharacterTypes.Animals:
								trackables = new List<AbstractCharacter>();
								foreach (Character chr in Map.GetMap(self.M).GetNPCsInRectangle(npcRect)) {
									if (chr.IsAnimal) trackables.Add(chr);
								}
								break;
							case CharacterTypes.Monsters:
								trackables = new List<AbstractCharacter>();
								foreach (Character chr in Map.GetMap(self.M).GetNPCsInRectangle(npcRect)) {
									if (chr.IsMonster) trackables.Add(chr);
								}
								break;
							case CharacterTypes.NPCs:
								trackables = new List<AbstractCharacter>();
								foreach (Character chr in Map.GetMap(self.M).GetNPCsInRectangle(npcRect)) {
									if (chr.IsHuman) trackables.Add(chr);
								}
								break;
							case CharacterTypes.All:
								//monsters, animals and human NPCs
								trackables = new List<AbstractCharacter>(Map.GetMap(self.M).GetNPCsInRectangle(npcRect));
								break;
						}

						//check if tracking is possible (with message) - i.e. too many chars or none at all
						if (this.CheckTrackImpossible(skillSeqArgs, trackables.Count, charType)) {
							return;
						}

						//display a dialog with the found trackable characters
						self.ClilocSysMessage(1018093);//Select the one you would like to track.
						self.Dialog(self, SingletonScript<D_Tracking_Characters>.Instance, new DialogArgs(skillSeqArgs, trackables));
						break;
					case TrackingEnums.Phase_Character_Track: //we will try to display the chars path
						Character charToTrack = (Character) skillSeqArgs.Target1;

						NPCTrackingPlugin npctpl = (NPCTrackingPlugin) NPCTrackingPlugin.defInstance.Create();
						npctpl.maxAllowedDist = this.GetNPCMaxRange(self); //maximal distance before the tracked character disappears...						
						npctpl.safeSteps = this.GetMaxSafeSteps(self); //number of safe steps
						npctpl.whoToTrack = charToTrack;
						npctpl.Timer = NPCTrackingPlugin.refreshTimeout;//set the first timer

						self.AddPlugin(NPCTrackingPlugin.npcTrackingPluginKey, npctpl);
						break;
				}
			}
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
			skillSeqArgs.Self.ClilocSysMessage(502989);//Tracking failed.
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			skillSeqArgs.Self.SysMessage("Tracking aborted");
		}

		/// <summary>
		/// Check if we are alive, have enuogh stamina etc.... Return false if the trigger above 
		/// should be cancelled or true if we can continue
		/// </summary>
		private bool CheckPrerequisities(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			if (!self.CheckAliveWithMessage()) {
				return false;//no message needed, it's been already sent in the called method
			}
			if (self.Stam <= self.MaxStam / 10) {
				self.ClilocSysMessage(502988);//You are too weary to make anything from the clues of nature.
				return false; //stop
			}
			return true;
		}

		//check if there isn't too much or none chars to track, return true if impossible
		private bool CheckTrackImpossible(SkillSequenceArgs ssa, int charsAroundCount, CharacterTypes charType) {
			Character self = ssa.Self;
			if (charsAroundCount == 0) {
				switch (charType) {
					case CharacterTypes.Animals:
						self.ClilocSysMessage(502991); //You see no evidence of animals in the area.
						return true;
					case CharacterTypes.Monsters:
						self.ClilocSysMessage(502993); //You see no evidence of creatures in the area.
						return true;
					case CharacterTypes.NPCs:
						self.ClilocSysMessage(502995); //You see no evidence of people in the area. 
						return true;
					case CharacterTypes.Players:
						self.ClilocSysMessage(502995); //You see no evidence of people in the area. 
						return true;
					//1018092	You see no evidence of those in the area.
				}
			}
			if (charsAroundCount > this.GetMaxTrackableChars(self)) {
				switch (charType) {
					case CharacterTypes.Animals:
						self.ClilocSysMessage(502990); //This area is too crowded to track any individual animal.
						return true;
					case CharacterTypes.Monsters:
						self.ClilocSysMessage(502992); //This area is too crowded to track any individual creature.
						return true;
					case CharacterTypes.NPCs:
						self.ClilocSysMessage(502994); //This area is too crowded to track any individual.
						return true;
					case CharacterTypes.Players:
						self.ClilocSysMessage(502994); //This area is too crowded to track any individual.
						return true;
					//1018091	This area is too crowded to track anything.
				}
			}
			return false;//its OK, we can continue with tracking
		}

		//get the area to look for any footsteps in
		private ImmutableRectangle GetPlayerTrackingArea(Character self) {
			return new ImmutableRectangle(self, (ushort) this.GetPlayerMaxRange(self));
		}

		private ImmutableRectangle GetNPCTrackingArea(Character self) {
			return new ImmutableRectangle(self, (ushort) this.GetNPCMaxRange(self));
		}

		public int GetPlayerMaxRange(Character forWho) {
			return (int) this.GetEffectForChar(forWho); //tracking players - use the Effect field
		}

		//get the Tracking range  - either for computing the scanned Rectangle or determining the maximal tracking distance
		public int GetNPCMaxRange(Character self)
		{
			//tracking other types of characters (animals, monsters, NPCs) - use the PVMEffect field
			if (self.IsGM) {
				return (int) ScriptUtil.EvalRangePermille(1000.0, this.PVMEffect);
			}
			return (int) ScriptUtil.EvalRangePermille(this.SkillValueOfChar(self), this.PVMEffect);
		}

		//get the maximum age of the footsteps to be found
		public TimeSpan GetMaxFootstepsAge(Character self) {
			TimeSpan maxAge;
			if (self.IsGM) {
				maxAge = ScriptSector.maxEntityAge; //get the maximal lifetime of the footsteps
			} else {
				maxAge = TimeSpan.FromSeconds(ScriptUtil.EvalRangePermille(this.SkillValueOfChar(self), this.MinFootstepAge, this.MaxFootstepAge));
			}
			return maxAge;
		}

		//get the maximum age of the footsteps to be found
		public int GetMaxTrackableChars(Character self) {
			int maxChars;
			if (self.IsGM) {
				maxChars = int.MaxValue; //unlimited, GM sees everything
			} else {
				maxChars = (int) ScriptUtil.EvalRangePermille(this.SkillValueOfChar(self), this.MinCharsToTrack, this.MaxCharsToTrack);
			}
			return maxChars;
		}

		//get the maximum number of safe steps the tracker will be able to do (before another tracking chance recomputing)
		public int GetMaxSafeSteps(Character self) {
			int maxSteps;
			if (self.IsGM) {
				maxSteps = int.MaxValue; //unlimited, GM can go as far as he wants
			} else {
				maxSteps = (int) ScriptUtil.EvalRangePermille(this.SkillValueOfChar(self), this.MinSafeSteps, this.MaxSafeSteps);
			}
			return maxSteps;
		}

		[InfoField("Min age [sec.0-skill]")]
		public double MinFootstepAge {
			get {
				return (double) this.minFootstepAge.CurrentValue;
			}
			set {
				this.minFootstepAge.CurrentValue = value;
			}
		}

		[InfoField("Max age[sec.100-skill]")]
		public double MaxFootstepAge {
			get {
				return (double) this.maxFootstepAge.CurrentValue;
			}
			set {
				this.maxFootstepAge.CurrentValue = value;
			}
		}

		[InfoField("Trackables [char.0/skill]")]
		public int MinCharsToTrack {
			get {
				return (int) this.minCharsToTrack.CurrentValue;
			}
			set {
				this.minCharsToTrack.CurrentValue = value;
			}
		}

		[InfoField("Trackables [char.100/skill]")]
		public int MaxCharsToTrack {
			get {
				return (int) this.maxCharsToTrack.CurrentValue;
			}
			set {
				this.maxCharsToTrack.CurrentValue = value;
			}
		}

		[InfoField("SafeSteps [char.0/skill]")]
		public int MinSafeSteps {
			get {
				return (int) this.minSafeSteps.CurrentValue;
			}
			set {
				this.minSafeSteps.CurrentValue = value;
			}
		}

		[InfoField("SafeSteps [char.100/skill]")]
		public int MaxSafeSteps {
			get {
				return (int) this.maxSafeSteps.CurrentValue;
			}
			set {
				this.maxSafeSteps.CurrentValue = value;
			}
		}

		[InfoField("PVM Effect")]
		public double[] PVMEffect {
			get {
				return (double[]) this.pvmEffect.CurrentValue;
			}
			set {
				this.pvmEffect.CurrentValue = value;
			}
		}

		[SteamFunction]
		public static void Track(Character self) {
			self.SelectSkill(SkillName.Tracking);
		}

		[SteamFunction]
		public static void StopTrack(Character self) {
			PlayerTrackingPlugin.UninstallPlugin(self);
			self.DeletePlugin(NPCTrackingPlugin.npcTrackingPluginKey);
		}
	}
}