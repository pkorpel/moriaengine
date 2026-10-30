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
using SteamEngine.Networking;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public partial class PlayerDef {
	}

	[ViewableClass]
	public partial class Player : Character {

		public int Level {
			get {
				return this.level;
			}
			set {
				this.level = (short) value;
			}
		}

		//Erebor's statslevel (level capped at 60 for a few stat formulas) - NOT a level cap: levels
		//are uncapped, on the same (level+1)*330 curve as Erebor's. Currently unused.
		public int EffectiveLevel {
			get {
				return Math.Min((int) this.level, 60);
			}
		}

		/// <summary>Direct set - does not run the level-up check. Use LevelingSystem.AddExperience to grant experience through the normal gameplay path (monster kills, etc).</summary>
		public int Experience {
			get {
				return this.experience;
			}
			set {
				this.experience = value;
			}
		}

		/// <summary>How much total experience is needed to reach the next level - see LevelingSystem.AddExperience for the formula this mirrors.</summary>
		public int NextLevelExperienceThreshold {
			get {
				return (this.Level + 1) * LevelingSettings.instance.nextLevelCoefficient;
			}
		}

		/// <summary>Unspent skill points granted on level-up (+5/level) - spend via LevelingSystem.TrySpendSkillPoints.</summary>
		public int UnspentSkillPoints {
			get {
				return this.unspentSkillPoints;
			}
			set {
				this.unspentSkillPoints = value;
			}
		}

		/// <summary>Unspent ability points granted on level-up (+2/level) - spend via LevelingSystem.TrySpendAbilityPoints.</summary>
		public int UnspentAbilityPoints {
			get {
				return this.unspentAbilityPoints;
			}
			set {
				this.unspentAbilityPoints = value;
			}
		}

		public int StrPointsAllocated {
			get { return this.strPointsAllocated; }
			set { this.strPointsAllocated = value; }
		}

		public int DexPointsAllocated {
			get { return this.dexPointsAllocated; }
			set { this.dexPointsAllocated = value; }
		}

		public int IntPointsAllocated {
			get { return this.intPointsAllocated; }
			set { this.intPointsAllocated = value; }
		}

		public int VitPointsAllocated {
			get { return this.vitPointsAllocated; }
			set { this.vitPointsAllocated = value; }
		}

		/// <summary>Remaining attribute points available to spend across STR/DEX/INT/VIT - total
		/// budget is 300 (flat base) + LevelingSystem.LevelStatBonus(Level), minus whatever's
		/// already been allocated. Computed from Level on every access rather than stored,
		/// since the budget formula is a function of the player's current level, not a simple
		/// per-level increment (unlike skill/ability points).</summary>
		public int AttributePointBudget {
			get {
				int totalGranted = 300 + LevelingSystem.LevelStatBonus(this.Level);
				int totalAllocated = this.strPointsAllocated + this.dexPointsAllocated +
					this.intPointsAllocated + this.vitPointsAllocated;
				return totalGranted - totalAllocated;
			}
		}

		public short Vit {
			get {
				return this.vitality;
			}
			set {
				if (value != this.vitality) {
					CharSyncQueue.AboutToChangeHitpoints(this);
					CharSyncQueue.AboutToChangeStamina(this);
					this.vitality = value;

					//regeneration...
					RegenerationPlugin.TryInstallPlugin(this, this.Hits, this.vitality, this.HitsRegenSpeed);
					RegenerationPlugin.TryInstallPlugin(this, this.Stam, this.vitality, this.StamRegenSpeed);
				}
			}
		}

		public override short MaxHits {
			get {
				return this.vitality;
			}
			set {
				this.Vit = value; //or should we throw exception?
			}
		}

		public override short MaxMana {
			get {
				return this.Int;
			}
			set {
				this.Int = value; //or should we throw exception?
			}
		}

		public override short MaxStam {
			get {
				return this.vitality;
			}
			set {
				this.Vit = value; //or should we throw exception?
			}
		}

		public override short Int {
			get {
				return base.Int;
			}
			set {
				CharSyncQueue.AboutToChangeMana(this);

				base.Int = value;

				//regeneration...
				RegenerationPlugin.TryInstallPlugin(this, this.Mana, this.MaxMana, this.ManaRegenSpeed);

				//meditation finish
				if (this.Mana >= this.MaxMana) {
					this.DeletePlugin(MeditationPlugin.meditationPluginKey);
				}
			}
		}

		public void Where() {
			this.Message("You are at " + this.P());
			this.Message("You are in " + this.Region.HierarchyName);
		}

		public void Password(string newpass) {
			AbstractAccount acc = this.Account;
			if (acc != null) {
				acc.Password(newpass);
			}
		}

		[Button("Skills")]
		public void ShowSkills() {
			GameState state = Globals.SrcGameState;
			if (state != null) {
				this.ShowSkillsTo(state.Conn, state);
			}
		}

		public void AllShow() {
			GameState state = this.GameState;
			if (state != null) {
				if (state.AllShow) {
					state.AllShow = false;
					Globals.SrcWriteLine("AllShow off.");
				} else {
					state.AllShow = true;
					Globals.SrcWriteLine("AllShow on.");
				}
			}
		}

		public void AllShow(bool value) {
			GameState state = this.GameState;
			if (state != null) {
				state.AllShow = value;
				Globals.SrcWriteLine("AllShow " + (value ? "on" : "off"));
			}
		}

		/// <summary>Display (or remove) a quest arrow pointing towards the specified coordinates</summary>
		public void QuestArrow(bool active, ushort xPos, ushort yPos) {
			GameState state = this.GameState;
			if (state != null) {
				QuestArrowOutPacket qaop = Pool<QuestArrowOutPacket>.Acquire();
				qaop.Prepare(active, xPos, yPos);
				state.Conn.SendSinglePacket(qaop);
			}
		}

		/// <summary>Display (or remove) a quest arrow pointing towards the specified location</summary>
		public void QuestArrow(bool active, IPoint2D position) {
			GameState state = this.GameState;
			if (state != null) {
				QuestArrowOutPacket qaop = Pool<QuestArrowOutPacket>.Acquire();
				qaop.Prepare(active, position);
				state.Conn.SendSinglePacket(qaop);
			}
		}

		public void WebLink(string url) {
			GameState state = this.GameState;
			if (state != null) {
				OpenWebBrowserOutPacket qaop = Pool<OpenWebBrowserOutPacket>.Acquire();
				qaop.Prepare(url);
				state.Conn.SendSinglePacket(qaop);
			}
		}

		/// <summary>"Use whatever I'm holding" shortcut - double-clicks whatever's equipped
		/// in the primary hand, falling back to the off-hand if the primary is empty. Reuses
		/// the real double-click trigger (same as a client double-clicking the item directly)
		/// rather than reimplementing equip/use logic here.</summary>
		public void UseHand() {
			AbstractItem item = this.FindLayer(LayerNames.Hand1) ?? this.FindLayer(LayerNames.Hand2);
			if (item != null) {
				item.Trigger_DClick(this);
			}
		}

		#region Profession
		public ProfessionPlugin ProfessionPlugin {
			get {
				return ProfessionPlugin.GetInstalledPlugin(this);
			}
		}

		public ProfessionDef Profession {
			get {
				return ProfessionDef.GetProfessionOfChar(this);
			}
			set {
				ProfessionDef.SetProfessionOfChar(this, value);
			}
		}

		/// <summary>Plain display name for .explevel (#134/#186) - ProfessionDef's own
		/// ToString() returns a debug-style "[name Type]" form, not suitable for a
		/// player-facing dialog. ProfessionDef.Name itself is stored all-lowercase ("mag",
		/// "necromancer", ...) - this capitalizes the first letter for display, since every
		/// profession name is a single word (UpperCamelCase reduces to that here).</summary>
		public string ProfessionName {
			get {
				ProfessionDef profession = this.Profession;
				if (profession == null) {
					return "None";
				}
				string name = profession.Name;
				if (string.IsNullOrEmpty(name)) {
					return name;
				}
				return char.ToUpperInvariant(name[0]) + name.Substring(1);
			}
		}

		/// <summary>Recomputes Character.Title from the current SkillRank+Profession+Level+gender (#165, #844)
		/// - call whenever one of those changes (ProfessionPlugin.On_Assign, LevelingSystem.
		/// AddExperience's level-up loop, the SkillRank setter), and at login, for a title saved by an older
		/// version. Erebor's title had the rank too (experiences.scp rankname). Does nothing if no profession is assigned (Title is
		/// left as whatever it already was, same "incomplete content stays permissive" pattern
		/// used elsewhere - a title-less character isn't an error).</summary>
		public void RefreshTitle() {
			ProfessionDef profession = this.Profession;
			if (profession != null) {
				string title = profession.GetTitle(this.Level, this.IsFemale);
				this.Title = string.IsNullOrEmpty(title) ? title : this.SkillRank + " " + title;
			}
		}
		#endregion Profession

		#region Race
		/// <summary>Which of the 6 playable races (#161/#162) this character chose - backed by
		/// RaceNationPlugin (#410, reworked from a tag to fix the #407 enum-persistence bug
		/// class generically for this property, and per the user's explicit request to move
		/// Race/Nation to the Plugin architecture Profession already uses). Defaults to
		/// Race.None (not yet chosen, e.g. a freshly-created character that hasn't used the
		/// race-selection stone yet, or the plugin was never installed at all).</summary>
		public Race Race {
			get {
				RaceNationPlugin plugin = RaceNationPlugin.GetInstalledPlugin(this);
				return (plugin != null) ? plugin.Race : Race.None;
			}
			set {
				RaceNationPlugin.GetOrInstallPlugin(this).Race = value;
			}
		}

		/// <summary>Which nation within the chosen race (#161/#162) - backed by
		/// RaceNationPlugin (#410, same reasoning as Race above). Setting this also derives
		/// and sets Realm (Character.Realm, its own separate RealmPlugin - per the user's
		/// explicit design call, Realm may need to change independently later, e.g. a
		/// renegade mechanic) and Home from NationData, since every nation implies exactly
		/// one of each. Defaults to Nation.None.</summary>
		public Nation Nation {
			get {
				RaceNationPlugin plugin = RaceNationPlugin.GetInstalledPlugin(this);
				return (plugin != null) ? plugin.Nation : Nation.None;
			}
			set {
				RaceNationPlugin.GetOrInstallPlugin(this).Nation = value;
				if (value != Nation.None) {
					this.Realm = NationData.GetRealm(value);
					this.Home = NationData.GetHome(value);
				}
			}
		}

		private static readonly TagKey homeTK = TagKey.Acquire("_home_");

		/// <summary>Static hometown location (#164) - set once via Nation's setter above, NOT a
		/// recall mechanism (deliberately excluded from #164's scope per the user - "home is
		/// basic home location which doesn't change for recall... lets leave recall away from
		/// this"). Settable directly too, for test-harness convenience (same pattern as
		/// LevelingTests.cs's TestSetProfession bypassing the real assignment flow).</summary>
		public Point4D Home {
			get {
				return this.GetTag(homeTK) as Point4D;
			}
			set {
				this.SetTag(homeTK, value);
			}
		}
		#endregion Race

		/// <summary>Every step is monitored by the ScriptSector system</summary>
		public override TriggerResult On_Step(Direction direction, bool running) {
			ScriptSector.AddTrackingStep(this, direction);
			return base.On_Step(direction, running);
		}

		/// <summary>Classic UO-style logout delay (#261) - the character stays fully standing/
		/// visible/interactable for LogoutSettings.logoutDelaySeconds (default 60s) after
		/// disconnect, then actually disappears (CharLingeringTimer.OnTimeout -> Disconnect() ->
		/// Flag_Disconnected, the same removal-from-view mechanism Map.Disconnected already
		/// provides elsewhere - nothing destroyed/deleted, a reconnect within the window cancels
		/// this cleanly via On_LogIn below). The effect plays HERE, at disconnect/timer-start,
		/// not at the delayed removal - signals to nearby players that this character is in the
		/// process of logging out, not a random disappearance.
		///
		/// #416 - GMs (plevel >= Globals.PlevelOfGM) skip the lingering delay - the timer is
		/// still used (DueInSeconds = 0, firing on the very next tick) rather than calling
		/// Disconnect() directly here: On_LogOut runs while this.GameState is still set (the
		/// connection's own close sequence hasn't nulled it out yet at this point), so
		/// IsLingering is still false and Disconnect() would take its "still connected" branch
		/// and call Conn.Close() a second time, re-entering the very close handler that's
		/// already running this trigger. CharLingeringTimer.OnTimeout's own IsLingering check
		/// only sees the correct, already-lingering state because it runs on a LATER tick, once
		/// that transition has actually completed.</summary>
		private static TimerKey charLingeringTimerTK = TimerKey.Acquire("_charLingeringTimer_");
		public override void On_LogOut() {
			//TODO: In safe/nonsafe areas, settings, etc.

			if (LogoutSettings.instance.showLogoutEffect) {
				EffectFactory.StationaryEffect(this, 0x376A, 10, 16);
			}

			//#810 - Sphere's client detach: whatever is in hand goes back to the pack (it used to stay
			//"dragged" on the lingering body), and every open trade is cancelled
			this.TryGetRidOfDraggedItem();
			SecureTrade.CancelAllFor(this, "logged out");

			AbstractAccount acc = this.Account;
			double delay = ((acc != null) && (acc.PLevel >= Globals.PlevelOfGM)) ? 0 : LogoutSettings.instance.logoutDelaySeconds;
			this.AddTimer(charLingeringTimerTK, new CharLingeringTimer()).DueInSeconds = delay;

			//if (DbManager.Config.useDb) {
			//    DbMethods.loginLogs.GameLogout(this.Conn);
			//    Logger.WriteDebug(ScriptUtil.GetLogString(this.Conn, "Logged out"));
			//} else {
			//    Console.WriteLine(ScriptUtil.GetLogString(this.Conn, "Logged out"));
			//}

			base.On_LogOut();
		}

		public override TriggerResult On_LogIn() {
			this.DeleteTimer(charLingeringTimerTK); //cancel any pending logout-delay removal - reconnecting resumes this body in place
			var result = base.On_LogIn();
			if (result != TriggerResult.Cancel) {
				SecureTrade.ReturnOrphanedBoxes(this); //#810 - a trade box left over from a server restart mid-trade
				this.RefreshTitle();
				//if (DbManager.Config.useDb) {
				//    DbMethods.loginLogs.GameLogin(this.Conn);
				//    Logger.WriteDebug(ScriptUtil.GetLogString(this.Conn, "Logged in"));
				//} else {
				//    Console.WriteLine(ScriptUtil.GetLogString(this.Conn, "Logged in"));
				//}
				GameState state = this.GameState;
				state.SyncUpdateRange();
				state.SendPersonalLightLevel(this.personalLightLevel);
				state.SendGlobalLightLevel(LightAndWeather.GetLightAt(this));
				state.SendSeasonalInformation(LightAndWeather.CurrentSeason, CursorType.Normal);
			}
			return result;
		}


		/// <summary>@Death trigger - check if none of the skills goes above the maximal limit, and
		/// apply the leveling system's death-experience-loss/pay-the-killer mechanic (#128).</summary>
		public override void On_Death(Character killedBy) {
			this.CheckSkillMaxima();
			LevelingSystem.ApplyDeathExperienceLoss(this, killedBy);
			base.On_Death(killedBy);
		}

		/// <summary>
		/// Return the modifier of the skill's maximum value (it is normally determined by 
		/// selected profession, but can be altered e.g. by magic items etc...)
		/// </summary>
		internal short GetSkillMaxModifier(SkillName name) {
			if (this.maxSkillModifier != null) {
				short outVal = 0;
				if (this.maxSkillModifier.TryGetValue(name, out outVal)) {
					return outVal;//skill is modified somehow
				}
				return 0; //skill is not modified
			}
			return 0;
		}

		/// <summary>
		/// Set the special modifier to the given skill's maximum value (the player will be 
		/// allowed to go over his normal profession's maximum for this particular skill
		/// </summary>
		internal void SetSkillMaxModifier(SkillName name, short value) {
			if (this.maxSkillModifier == null) {
				this.maxSkillModifier = new Dictionary<SkillName, short>();
			}
			if (value == 0) {//remove the value from the dictionary
				this.maxSkillModifier.Remove(name);
				if (this.maxSkillModifier.Keys.Count == 0) {
					this.maxSkillModifier = null; //no modifiers left, release the reference
				}
			} else {
				this.maxSkillModifier[name] = value;
			}
		}

		private void CheckSkillMaxima() {
			//check all skills and fix any possible overlaps
			//short skillMaxValue;
			//int basicMaxSkill;
			//foreach (ISkill skl in this.Skills) {
			//    basicMaxSkill = (profession == null) ? 1000 : profession.ProfessionDef.MaxSkill(skl.Id); //profession can be missing (GM's etc.)
			//    skillMaxValue = (short) (basicMaxSkill + GetSkillMaxModifier(((Skill) skl).Name));
			//    if (skl.RealValue > skillMaxValue) {
			//        skl.RealValue = (ushort) Math.Min((ushort) 0, skillMaxValue); //don't allow to go over maximum or under 0
			//    }
			//}
		}


		[SaveableClass]
		public class CharLingeringTimer : BoundTimer {
			[LoadingInitializer]
			public CharLingeringTimer() {
			}

			protected sealed override void OnTimeout(TagHolder cont) {
				Logger.WriteDebug("CharLingeringTimer OnTimeout on " + cont);
				Character self = cont as Character;
				if (self != null) {
					if (self.IsLingering) {
						self.Disconnect();
					}
				}
			}
		}

		public void Target(AbstractTargetDef def) {
			def.Assign(this, null);
		}

		public void Target(AbstractTargetDef def, object parameter) {
			def.Assign(this, parameter);
		}

		public void Menu(AbstractMenuDef def) {
			def.Assign(this, null);
		}

		public void Menu(AbstractMenuDef def, object parameter) {
			def.Assign(this, parameter);
		}

		#region Messaging
		public void DelayedMessage(string text) {
			this.DelayedMessage(null, text);
		}

		/// <summary>
		/// Send a delayed message to the player. Specify the sender of the message (null means 
		/// that the message comes from the system)
		/// </summary>
		public void DelayedMessage(AbstractCharacter sender, string text) {
			if (sender == null) {
				//add a new message without the sender
				MsgsBoard.AddNewMessage(this, new DelayedMsg(text));
				//send the message also to the client
				this.SysMessage("System: " + text);
			} else {
				//add a new message with the sender
				MsgsBoard.AddNewMessage(this, new DelayedMsg(sender, text));
				//send the message also to the client
				this.SysMessage(sender.Name + ": " + text);
			}
			this.InfoMessage("Nova zprava, celkem neprectenych: " + MsgsBoard.CountUnread(this));
		}

		/// <summary>
		/// Send a delayed message to the player. Specify the sender of the message (null means 
		/// that the message comes from the system), we can also specify a message custom color here
		/// </summary>
		public void DelayedMessage(AbstractCharacter sender, string text, Hues hue) {
			if (sender == null) {
				//add a new message without the sender
				MsgsBoard.AddNewMessage(this, new DelayedMsg(null, text, hue));
			} else {
				//add a new message with the sender
				MsgsBoard.AddNewMessage(this, new DelayedMsg(sender, text, hue));
			}
			//send the message also to the client
			this.SysMessage(text, (int) hue);
			this.InfoMessage("Nova zprava, celkem neprectenych: " + MsgsBoard.CountUnread(this));
		}

		public void DelayedRedMessage(string text) {
			this.DelayedRedMessage(null, text);
		}

		/// <summary>
		/// Send a delayed red message to the player. Specify the sender of the message (null means 
		/// that the message comes from the system)
		/// </summary>
		public void DelayedRedMessage(AbstractCharacter sender, string text) {
			if (sender == null) {
				//add a new red message without the sender
				MsgsBoard.AddNewMessage(this, new DelayedMsg(text, true));
			} else {
				//add a new red message with the sender
				MsgsBoard.AddNewMessage(this, new DelayedMsg(sender, text, true));
			}
			//send the message also to the client
			this.RedMessage(text);
			this.InfoMessage("Nova vyznamna zprava, celkem neprectenych: " + MsgsBoard.CountUnread(this));
		}
		#endregion Messaging

		#region Add
		public void Add(int model) {
			this.Add(ThingDef.GetByModel(model), 1);
		}

		public void Add(IThingFactory addedDef) {
			this.Add(addedDef, 1);
		}

		public void Add(int model, int amount) {
			this.Add(ThingDef.GetByModel(model), amount);
		}

		public void Add(IThingFactory addedDef, int amount) {
			this.AddInternal(addedDef, amount, false);
		}

		/// <summary>#697 - the ".static" GM command: identical to Add in every respect (same
		/// targeting-cursor placement flow) except the created item gets attr_move_never set
		/// once placed. Confirmed against the real Sphere 0.56b engine source (CV_ADD/CV_STATIC
		/// both funnel through the one shared Cmd_CreateItem(id, fStatic)/OnTarg_Item_Add path -
		/// fStatic's only effect is one extra SetAttr(ATTR_MOVE_NEVER) call after creation, no
		/// separate placement logic) - mirrored here the same way, as one extra bool through the
		/// existing Add plumbing rather than a parallel implementation.</summary>
		public void Static(int model) {
			this.Static(ThingDef.GetByModel(model), 1);
		}

		public void Static(IThingFactory addedDef) {
			this.Static(addedDef, 1);
		}

		public void Static(int model, int amount) {
			this.Static(ThingDef.GetByModel(model), amount);
		}

		public void Static(IThingFactory addedDef, int amount) {
			this.AddInternal(addedDef, amount, true);
		}

		private void AddInternal(IThingFactory addedDef, int amount, bool fStatic) {
			GameState state = this.GameState;
			if (state != null) {
				if (addedDef != null) {
					string name = addedDef is ThingDef ? ((ThingDef) addedDef).Name : addedDef.ToString();
					this.SysMessage("Kam chce� um�stit '" + name + "' ?");

					ItemDef idef = addedDef as ItemDef;
					AddHelper addedH = new AddHelper(addedDef, amount, fStatic);
					if ((idef != null) && (idef.MultiData != null)) {
						state.TargetForMultis(idef.Model, this.Add_OnTargon, null, addedH);
					} else {
						state.Target(true, this.Add_OnTargon, null, addedH);
					}
				} else {
					this.SysMessage("Nenalezen odpovidajici ItemDef/TemplateDef.");
				}
			}
		}

		private class AddHelper {
			internal IThingFactory def;
			internal int amount;
			internal bool fStatic;
			public AddHelper(IThingFactory def, int amount, bool fStatic) {
				this.def = def;
				this.amount = amount;
				this.fStatic = fStatic;
			}
		}

		private void Add_OnTargon(GameState state, IPoint3D getback, object parameter) {

			AddHelper addedH = (AddHelper) parameter;
			Item targettedItem = getback as Item;
			if (targettedItem != null) {
				getback = targettedItem.TopObj();
			}
			Thing t = addedH.def.Create(getback.X, getback.Y, getback.Z, this.M);
			Item i = t as Item;
			if (i != null) {
				i.Amount = addedH.amount;
				if (addedH.fStatic) {
					i.SetAttribute(ItemAttributes.attr_move_never, true);
				}
			}
		}
		#endregion Add
		/// <summary>The crafting main method. Tries to create the given Item(Def) in a requested quantity</summary>
		public void Make(ItemDef what, int howMuch) {
			SimpleQueue<CraftingSelection> selectionQueue = new SimpleQueue<CraftingSelection>();
			selectionQueue.Enqueue(new CraftingSelection(what, howMuch));
			//to bychom meli, ted skill
			ResourcesList skillMake = what.SkillMake;
			double highestSkillVal = 0;
			CraftingSkillDef highestCsd = (CraftingSkillDef) AbstractSkillDef.GetByKey("Tinkering"); //default skill (neco mit vybrano musime, i v pripade ze skillmake == null)
			if (skillMake != null) {
				foreach (IResourceListEntry_Simple itm in skillMake.NonMultiplicablesSublist) {
					SkillResource sklr = itm as SkillResource;
					if (sklr != null) {
						CraftingSkillDef neededCraftingSkill = sklr.SkillDef as CraftingSkillDef;
						if (neededCraftingSkill != null) {//je to skutecne crafting skill?
							if (highestSkillVal < sklr.DesiredCount) {//je nejvyssi?
								highestSkillVal = sklr.DesiredCount;
								highestCsd = neededCraftingSkill;
							}
						}
					}
				}
			}
			//a muze se zacit vyrabet v pozadovanem mnozstvi
			CraftingProcessPlugin.StartCrafting(this, new CraftingOrder(highestCsd, selectionQueue));
		}

		public Container ReceivingContainer {
			get {
				if (this.receivingContainer == null) {//set the default
				} else if (this.receivingContainer.IsDeleted) {
					this.receivingContainer = null;
				} else if (this.CanPutItemsInContainer(this.receivingContainer).Allow) {
					return this.receivingContainer;
				}
				return this.Backpack;
			}

			set {
				this.receivingContainer = value;
			}
		}

		public override int VisionRange {
			get {
				return this.visionRange;
			}
			set {
				this.visionRange = (byte) value;

				GameState state = this.GameState;
				if (state != null) {
					state.SyncUpdateRange();
				}
			}
		}

		public int PersonalLightLevel {
			get {
				return this.personalLightLevel;
			}
			set {
				this.personalLightLevel = (byte) value;
				GameState state = this.GameState;
				if (state != null) {
					state.SendPersonalLightLevel(value);
				}
			}
		}

		public void SendGlobalLightLevel(int globalLight) {
			GameState state = this.GameState;
			if (state != null) {
				state.SendGlobalLightLevel(globalLight);
			}
		}

		public void SendSeasonalInformation(Season season, CursorType cursor) {
			GameState state = this.GameState;
			if (state != null) {
				state.SendSeasonalInformation(season, cursor);
			}
		}

		/// <summary>
		/// Tries to pay priceTotal gold, drawing from backpack/bank per Globals.PaymentSource
		/// (see GoldUtils.TryPay). Previously a stub that always failed for real players and
		/// always "succeeded" for GMs regardless of price - no gold was ever actually checked or
		/// deducted either way (enhancements Phase 14.5, issue #36).
		/// </summary>
		public bool Pay(decimal priceTotal) {
			if (priceTotal < 0) {
				return false;
			}
			if (priceTotal > uint.MaxValue) {
				return false; //not a representable gold amount
			}

			//round up - a fractional price should never let someone pay less than the real cost
			uint amount = (uint) System.Math.Ceiling(priceTotal);
			return GoldUtils.TryPay(this, amount);
		}
	}
}