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
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public abstract class CraftingSkillDef : SkillDef {
		//#799 - who crafted an item (Erebor's tag.craftedby)
		private static readonly TagKey crafterTK = TagKey.Acquire("_craftedBy_");
		private static readonly TagKey qualityTK = TagKey.Acquire("_quality_");

		private const int QualityBands = 7;

		//Sphere's Calc_GetRandVal(50): a failed attempt loses this much of each resource, at most
		private const int FailLossPercent = 50;

		public static void SetCrafter(Item item, Character crafter) {
			if ((item != null) && !item.IsDeleted) {
				item.SetTag(crafterTK, crafter);
			}
		}

		/// <summary>The character who crafted this item, or null.</summary>
		public static Character GetCrafter(Item item) {
			return item.GetTag(crafterTK) as Character;
		}

		/// <summary>#962 - Sphere's QUALITY, 0-200, 100 about average; made items only.</summary>
		public static int GetQuality(Item item) {
			object quality = item.GetTag(qualityTK);
			return (quality == null) ? 0 : (int) quality;
		}

		/// <summary>#962 - Sphere's quality bands: skill picks the band, the roll moves it up or down by up to 2.
		/// 0 shoddy, 3 average, 6 superior.</summary>
		public static int RollQualityBand(int skillValue, Func<int, int> random) {
			int modifier = 2 - (int) Math.Truncate(Math.Log10(random(250) + 1));
			if (random(2) == 0) {
				modifier = -modifier;
			}
			int scaled = (skillValue * 2) / 10;
			int band = (scaled < 25) ? 0 : (scaled < 50) ? 1 : (scaled < 75) ? 2 : (scaled < 125) ? 3 : (scaled < 150) ? 4 : (scaled < 175) ? 5 : 6;
			return Math.Clamp(band + modifier, 0, QualityBands - 1);
		}

		public static int RollQuality(int band, Func<int, int> random) {
			switch (band) {
				case 0: return random(25) + 1;
				case 1: return random(25) + 26;
				case 2: return random(25) + 51;
				case 3: return random(50) + 76;
				case 4: return random(25) + 126;
				case 5: return random(25) + 151;
				default: return random(25) + 176;
			}
		}

		private void SetQualityOf(Item newItem, Character self) {
			int band = RollQualityBand(this.SkillValueOfChar(self), Globals.dice.Next);
			newItem.SetTag(qualityTK, RollQuality(band, Globals.dice.Next));
			CraftSkillsLoc texts = Loc<CraftSkillsLoc>.Get(self.Language);
			string[] messages = { texts.QualityShoddy, texts.QualityPoor, texts.QualityBelowAverage, null, texts.QualityAboveAverage, texts.QualityExcellent, texts.QualitySuperior };
			if (messages[band] != null) {
				self.SysMessage(messages[band]);
			}
		}


		public CraftingSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		/// <summary>
		/// This trigger is used only when clicked the skill-list blue radio button for some crafting skill.
		/// opens the craftmenu for the given skill
		/// </summary>
		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			//#962 - no button for a crafting skill, as Sphere's Event_Skill_Use: the tools open the menus
			if (skillSeqArgs.Param1 == null) {
				self.ClilocSysMessage(500014); //That skill cannot be used directly.
				return TriggerResult.Cancel;
			}
			//todo: paralyzed state etc.
			//some special requirements will be also checked (if present)
			if (!this.CheckPrerequisities(skillSeqArgs) || !this.DoCheckSpecials(skillSeqArgs)) {
				CraftingProcessPlugin.UnInstallCraftingPlugin(self);
				return TriggerResult.Cancel; //something wrong, finish now
			}
			return TriggerResult.Continue; //do not stop, we have some item here, we can start crafting now
		}

		/// <summary>#962 - what a tool or a material does when used: Erebor's craftmenu().</summary>
		public static void OpenMenu(Character self, SkillName skill) {
			CraftingSkillDef def = (CraftingSkillDef) SkillDef.GetBySkillName(skill);
			if (self.CheckAliveWithMessage() && def.DoCheckMenu(self)) {
				D_Craftmenu.Craftmenu(self, skill);
			}
		}

		/// <summary>The type's name of a thing, null with no type.</summary>
		public static bool IsOfType(Item item, params string[] typeDefnames) {
			return (item != null) && (item.Type != null) && (Array.IndexOf(typeDefnames, item.Type.Defname) >= 0);
		}

		/// <summary>
		/// This trigger is called when OK button is clicked from the craftmenu. One item from the queue is picked and its making process 
		/// begins here (i.e. the making success and the number of strokes is pre-computed here)
		/// </summary>
		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			//todo: paralyzed state etc.
			//some special requirements will be also checked (if present)
			if (!this.CheckPrerequisities(skillSeqArgs) || !this.DoCheckSpecials(skillSeqArgs)) {
				CraftingProcessPlugin.UnInstallCraftingPlugin(self);
				return TriggerResult.Cancel; //something wrong, finish now
			}

			ItemDef iDefToMake = (ItemDef) skillSeqArgs.Param1;//the on_start trigger runs only if there is something here...
			if (!self.IsGM && !this.CanStartMaking(iDefToMake, self)) {
				CraftingProcessPlugin.MakeImpossible(skillSeqArgs);
				return TriggerResult.Cancel;
			}
			//#962 - decided now, shown after all the strokes: a failure takes as long as a success
			int difficulty = MakeDifficulty(iDefToMake);
			skillSeqArgs.Success = (difficulty <= 0) || this.CheckSuccess(self, difficulty);
			skillSeqArgs.Param2 = this.RollStrokes(iDefToMake);
			this.DoEveryStage(skillSeqArgs);
			return TriggerResult.Continue; //continue to delay, then @stroke
		}

		/// <summary>
		/// This trigger is run a pre-computed number of times before the item is either created or failed.
		/// The item-making animations and sounds are run from here
		/// </summary>
		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			//todo: paralyzed state etc.
			//some special requirements will be also checked (if present)
			if (!this.CheckPrerequisities(skillSeqArgs) || !this.DoCheckSpecials(skillSeqArgs)) {
				CraftingProcessPlugin.UnInstallCraftingPlugin(skillSeqArgs.Self);
				return TriggerResult.Cancel; //something wrong, finish now
			}

			this.DoEveryStage(skillSeqArgs);
			this.DoStroke(skillSeqArgs);
			if (!this.DoStrokeWork(skillSeqArgs)) {
				skillSeqArgs.Success = false;
				return TriggerResult.Continue;
			}
			int strokesCnt = Convert.ToInt32(skillSeqArgs.Param2);
			if (strokesCnt > 1) {
				//the last stroke only sounds, as Sphere's has no count left to animate
				this.DoStrokeAnim(skillSeqArgs);
				skillSeqArgs.Param2 = strokesCnt - 1;
				skillSeqArgs.DelayStroke();
				return TriggerResult.Cancel; //stop here for now (we are waiting for the next stroke round...)
			}
			return TriggerResult.Continue; //continue to @success or @fail (the result is already prepared from the "@start" phase
		}

		/// <summary>
		/// This trigger is run in case the item making succeeds. It consumes the desired number of resources, 
		/// creates the item instance and checks if there are any other items to be created in the queue. If so, it 
		/// re-runs the @start trigger
		/// </summary>
		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
			Player self = (Player) skillSeqArgs.Self;
			//todo: paralyzed state etc.
			//some special requirements will be also checked (if present)
			if (!this.CheckPrerequisities(skillSeqArgs) || !this.DoCheckSpecials(skillSeqArgs)) {
				CraftingProcessPlugin.UnInstallCraftingPlugin(self);
				return;//something wrong, finish now
			}

			this.DoEveryStage(skillSeqArgs);
			ItemDef iDefToMake = (ItemDef) skillSeqArgs.Param1;

			IResourceListEntry missingResource;
			bool canMake = true;
			//first check the necessary resources to have (GM needn't have anything) (if any resources are needed)
			if (!self.IsGM && iDefToMake.SkillMake != null && !iDefToMake.SkillMake.HasResourcesPresent(self, ResourcesLocality.BackpackAndLayers, out missingResource)) {
				self.SysMessage(missingResource.GetResourceMissingMessage(self.Language));
				canMake = false;
			}

			if (canMake) {//if still OK try to consume the consumable resources (if any) otherwise step over this block
				if (!self.IsGM && !this.ConsumeMade(iDefToMake, self, out missingResource)) {
					self.SysMessage(missingResource.GetResourceMissingMessage(self.Language));
					canMake = false;
				} else {//resources consumed or we are GM or no resources needed, create the item and place it to the pre-defined (or default) location
					Item newItem = (Item) iDefToMake.Create(self.ReceivingContainer);
					SetCrafter(newItem, self); //#799 - e_craft_monster's "weapon you made yourself" critical
					string madeType = (newItem.Type == null) ? null : newItem.Type.Defname;
					if (madeType == "t_potion") {
						self.Emote(Loc<CraftSkillsLoc>.Get(self.Language).PourPotion);
						self.Sound(0x240);
					} else if (madeType != "t_scroll") {
						this.SetQualityOf(newItem, self);
					}
					//self.ClilocSysMessage(500638);//You create the item and put it in your backpack.
					self.SysMessage(string.Format(
								Loc<CraftSkillsLoc>.Get(self.Language).ItemMadeAndPutInRecCont,
								iDefToMake.Name, self.ReceivingContainer.Name));
					this.DoSuccess(skillSeqArgs, newItem);
					MagicItemShop.CraftPath(self, (SkillName) this.Id, newItem); //#1017 - the craftsman's paths
					ItemBundle.UnpackMade(newItem, self); //as Erebor's @SkillMakeItem firing the bundle's @dropon_obj
					SkillGainSystem.TryGainSkill(self, (SkillName) this.Id, MakeDifficulty(iDefToMake)); //#328

					CraftingProcessPlugin.MakeFinished(skillSeqArgs);
				}
			}

			//deal with failure - not enough resources 
			if (!canMake) {
				CraftingProcessPlugin.MakeImpossible(skillSeqArgs);
				//the rest will be decided by the CraftingProcessPlugin
			}
		}

		/// <summary>
		/// This trigger is run in case the item making fails. It consumes some of the desired number of resources (i.e. these are wasted), 
		/// and checks if there are any other items to be created in the queue. If so, it 
		/// re-runs the @start trigger
		/// </summary>
		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			if (!self.CheckAliveWithMessage() || !this.DoCheckSpecials(skillSeqArgs)) {
				CraftingProcessPlugin.UnInstallCraftingPlugin(self);
				return;//no message needed, it's been already sent in the called method
			}

			this.DoEveryStage(skillSeqArgs);
			ItemDef iDefToMake = (ItemDef) skillSeqArgs.Param1;

			//check if we can even make it (i.e. we have necessary resources) otherwise we can attempt to make something without resources and in case 
			//we failed the next attempt will begin afterwards (which makes no sense since we do not even have the resources...)
			IResourceListEntry missingResource;
			bool canMake = true;
			//first check the necessary resources to have (GM needn't have anything) (if any resources are needed)
			if (!self.IsGM && iDefToMake.SkillMake != null && !iDefToMake.SkillMake.HasResourcesPresent(self, ResourcesLocality.BackpackAndLayers, out missingResource)) {
				canMake = false;
			}

			bool hasMaterial = true;
			//then check if the necessary material is present
			if (!self.IsGM && iDefToMake.Resources != null && !iDefToMake.Resources.HasResourcesPresent(self, ResourcesLocality.Backpack, out missingResource)) {
				hasMaterial = false;
			}

			if (canMake && hasMaterial) {
				//lost some resources. if some of resources/material was missing then nothing happened...
				this.ConsumeFailed(iDefToMake, self);
				self.SysMessage(this.FailMessage(self));
				//#328 - real Erebor gains on a genuine failed attempt too (not just success), but
				//not on a "couldn't even try" case (missing resources/material - canMake/hasMaterial
				//false above skips this block entirely).
				SkillGainSystem.TryGainSkill(self, (SkillName) this.Id, -MakeDifficulty(iDefToMake));
			}
			CraftingProcessPlugin.MakeFinished(skillSeqArgs);
			//the rest will be decided by the CraftingProcessPlugin
		}

		//Sphere's Skill_Fail: leaving a job for another skill costs what a failure does
		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			ItemDef iDefToMake = skillSeqArgs.Param1 as ItemDef;
			if ((iDefToMake != null) && !self.IsGM) {
				this.ConsumeFailed(iDefToMake, self);
			}
			CraftingProcessPlugin.Stop(self, false);
			CraftingProcessPlugin.UnInstallCraftingPlugin(self);
		}

		/// <summary>Erebor's @Fail line for the skill.</summary>
		protected virtual string FailMessage(Character self) {
			return Loc<CraftSkillsLoc>.Get(self.Language).FailedMaking;
		}

		protected static string SpoiltProduct(Character self) {
			CraftSkillsLoc texts = Loc<CraftSkillsLoc>.Get(self.Language);
			return self.IsFemale ? texts.SpoiltProductFemale : texts.SpoiltProductMale;
		}

		/// <summary>How many strokes one item takes. One is Sphere's for all but the Blacksmith, Carpenter and Alchemist: the first delay, then done.</summary>
		public virtual int RollStrokes(ItemDef iDef) {
			return 1;
		}

		/// <summary>A stroke's own work beyond its sound. False stops the making as failed.</summary>
		protected virtual bool DoStrokeWork(SkillSequenceArgs skillSeqArgs) {
			return true;
		}

		/// <summary>Sphere's check of a new attempt: the requirements, and the materials.</summary>
		protected virtual bool CanStartMaking(ItemDef iDef, Character self) {
			IResourceListEntry missing;
			return ((iDef.SkillMake == null) || iDef.SkillMake.HasResourcesPresent(self, ResourcesLocality.BackpackAndLayers, out missing))
				&& ((iDef.Resources == null) || iDef.Resources.HasResourcesPresent(self, ResourcesLocality.Backpack, out missing));
		}

		/// <summary>What a made item costs when it's done.</summary>
		protected virtual bool ConsumeMade(ItemDef iDef, Character self, out IResourceListEntry missing) {
			missing = null;
			return (iDef.Resources == null) || iDef.Resources.ConsumeResourcesOnce(self, ResourcesLocality.Backpack, out missing);
		}

		/// <summary>What a failed attempt costs.</summary>
		protected virtual void ConsumeFailed(ItemDef iDef, Character self) {
			if (iDef.Resources != null) {
				iDef.Resources.ConsumeSomeResources(self, ResourcesLocality.Backpack, Globals.dice.Next(FailLossPercent));
			}
		}

		/// <summary>What Sphere's skill stage does whatever the stage: @start, each @stroke, @success, @fail.</summary>
		protected virtual void DoEveryStage(SkillSequenceArgs skillSeqArgs) {
		}

		/// <summary>
		/// This method is intended to be overriden by particular CraftingSkillDef classes. 
		/// Its purpose is to make something related to the stroke of the particular skill (such as playing some sound...)
		/// </summary>
		protected virtual void DoStroke(SkillSequenceArgs skillSeqArgs) {
		}

		/// <summary>The animation of a stroke with more to follow.</summary>
		protected virtual void DoStrokeAnim(SkillSequenceArgs skillSeqArgs) {
		}

		/// <summary>
		/// This method is intended to be overriden by particular CraftingSkillDef classes. 
		/// Its purpose is to make something related to the success of the particular skill (such as playing sound,
		/// computing some special item characteristics etc.)
		/// </summary>
		protected virtual void DoSuccess(SkillSequenceArgs skillSeqArgs, Item newItem) {
		}

		/// <summary>
		/// This method is intended to be overriden by particular CraftingSkillDef classes. 
		/// Its purpose is to check any additional pre-requisities for succesfull skill usage 
		/// (e.g. for BS check if the anvil or forge is near etc...).
		/// Return false if the trigger above should be cancelled or true if we can continue
		/// </summary>
		protected virtual bool DoCheckSpecials(SkillSequenceArgs skillSeqArgs) {
			return true;
		}

		/// <summary>What must be true for the menu to open at all, beyond DoCheckSpecials.</summary>
		protected virtual bool DoCheckMenu(Character self) {
			return true;
		}

		/// <summary>#962 - Sphere's FindItemTypeNearby: a thing or a static of the type, the nearest within range.</summary>
		public static bool FindNearby(Character self, string typeDefname, int range, out Point2D found) {
			found = default(Point2D);
			int best = int.MaxValue;
			Map map = self.GetMap();
			foreach (Thing thing in map.GetThingsInRectangle(new ImmutableRectangle(self.X - range, self.Y - range, self.X + range, self.Y + range))) {
				Item item = thing as Item;
				if ((item != null) && (item.Type != null) && (item.Type.Defname == typeDefname)) {
					Pick(self, item.X, item.Y, ref best, ref found);
				}
			}
			foreach (AbstractInternalItem tile in map.GetStaticsAndMultiComponentsInRange(self.X, self.Y, range)) {
				if (GatherTiles.IsOfType(tile, typeDefname)) {
					Pick(self, tile.X, tile.Y, ref best, ref found);
				}
			}
			return best <= range;
		}

		private static void Pick(Character self, int x, int y, ref int best, ref Point2D found) {
			int distance = Point2D.GetSimpleDistance(self.X, self.Y, x, y);
			if (distance < best) {
				best = distance;
				found = new Point2D((ushort) x, (ushort) y);
			}
		}

		/// <summary>
		/// Check if we are alive, have enough stats etc. Return false if the trigger above
		/// should be cancelled or true if we can continue
		/// </summary>
		private bool CheckPrerequisities(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			if (!self.CheckAliveWithMessage()) {
				return false;//no message needed, it's been already sent in the called method
			}
			return true;
		}

		/// <summary>#962 - the first skill of an item's SKILLMAKE, as in Sphere's Skill_MakeItem.</summary>
		public static int MakeDifficulty(ItemDef iDef) {
			ResourcesList requirements = iDef.SkillMake;
			if (requirements != null) {
				foreach (IResourceListEntry entry in requirements.Entries) {
					if (entry is SkillResource) {
						return (int) entry.DesiredCount;
					}
				}
			}
			return 0;
		}

		/// <summary>Check the resources and skillmake if the given character can craft this item</summary>
		public static bool CanBeMade(ItemDef iDef, Character chr) {
			if (chr.IsGM) {//GM can everything
				return true;
			}
			if (!AlchemySkillDef.ClassBrews(iDef, chr) || !InscriptionSkillDef.CanScribe(iDef, chr)) {
				return false;
			}
			//skillmake (skills, tools etc.)
			ResourcesList requir = iDef.SkillMake;
			if (requir != null) {
				IResourceListEntry missingItem;
				if (!requir.HasResourcesPresent(chr, ResourcesLocality.BackpackAndLayers, out missingItem)) {
					return false;
				}
			}

			//resources (necessary items)
			ResourcesList reslist = iDef.Resources;
			if (reslist != null) {
				IResourceListEntry missingItem;
				if (!reslist.HasResourcesPresent(chr, ResourcesLocality.Backpack, out missingItem)) {
					return false;
				}
			}
			return true;
		}
	}

	internal class CraftSkillsLoc : CompiledLocStringCollection<CraftSkillsLoc> {
		public string FailedMaking = "Vyroba se nezdarila.";
		public string TossMixture = "toss the failed mixture from the mortar";
		public string StartGrinding = "start grinding some {0} in the mortar";
		public string ContinueGrinding = "add {0} and continue grinding";
		public string LackForPotion = "Hmmm, you lack {0} for this potion.";
		public string SpoiltProductMale = "Zkazil jsi vyrobek.";
		public string SpoiltProductFemale = "Zkazila jsi vyrobek.";
		public string SpoiltScrollMale = "Zkazil jsi svitek.";
		public string SpoiltScrollFemale = "Zkazila jsi svitek.";
		public string FailedPotion = "Lektvar se nezdaril.";
		public string FailedFood = "Jidlo neni pozivatelne.";
		public string PourPotion = "pour the completed potion into a bottle";
		public string QualityShoddy = "Due to your poor skill, the item is of shoddy quality";
		public string QualityPoor = "You were barely able to make this item.  It is of poor quality";
		public string QualityBelowAverage = "You make the item, but it is of below average quality";
		public string QualityAboveAverage = "The item is of above average quality";
		public string QualityExcellent = "The item is of excellent quality";
		public string QualitySuperior = "Due to your exceptional skill, the item is of superior quality";
		public string FinishCrafting = "Vyroba ukoncena.";
		public string MakingStopped = "Vyroba zrusena";
		public string MakingCutShort = "Vyroba predcasne zrusena";
		public string MakingTheseThings = "Vyrabim tyto veci:";
		public string FailedTryAgain = "Vyrobek se nepovedl - dalsi pokus.";
		public string NotEnoughResources = "Nemas dost zdroju";
		public string CantMakeThat = "nelze vyrobit {0}.";
		public string CookOnFire = "You must cook food on some sort of fire";
		public string SmellsGood = "Mmm, smells good";
		public string NotNearForge = "You must be near a forge to smith";
		public string NeedHammer = "You need a hammer in your hand";
		//500638 - You create the item and put it in your backpack.
		public string ItemMadeAndPutInRecCont = "You create the {0} and put it in your {1}";
	}
}