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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using SteamEngine.Common;
using SteamEngine.Regions;

//using SteamEngine.PScript;

namespace SteamEngine {

	public interface IThingFactory {
		Thing Create(int x, int y, int z, byte m);
		Thing Create(IPoint4D point);
		Thing Create(Thing cont);
	}

	public abstract class ThingDef : AbstractDefTriggerGroupHolder, IThingFactory {
		internal FieldValue name;
		internal FieldValue model;
		private FieldValue weight;
		internal FieldValue height;

		private FieldValue color;

		internal MultiData multiData;

		private TriggerGroup defaultTriggerGroup;

		private static Dictionary<Type, Type> thingDefTypesByThingType = new Dictionary<Type, Type>();
		private static Dictionary<Type, Type> thingTypesByThingDefType = new Dictionary<Type, Type>();

		//private static Dictionary<string, Type> thingDefTypesByName = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
		private static Dictionary<Type, ConstructorInfo> thingDefCtors = new Dictionary<Type, ConstructorInfo>();


		//Highest itemdef model #: 21384	(0x5388)	<-- That's a multi. The last real item is 0x3fff.
		//Highest chardef model #: 987 (0x03db)

		//In case someone adds more on the end, we've set these higher.
		//public const int MaxItemModels = 0x6000;
		//public const int MaxCharModels = 0xf000;
		//private static AbstractItemDef[] itemModelDefs = new AbstractItemDef[MaxItemModels];
		//private static AbstractCharacterDef[] charModelDefs = new AbstractCharacterDef[MaxCharModels];

		private static ConcurrentDictionary<int, AbstractItemDef> itemModelDefs = new ConcurrentDictionary<int, AbstractItemDef>();
		private static ConcurrentDictionary<int, AbstractCharacterDef> charModelDefs = new ConcurrentDictionary<int, AbstractCharacterDef>();
		private static int highestItemModel;
		private static int highestCharModel;

		internal ThingDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.name = this.InitTypedField("name", "", typeof(string));
			this.color = this.InitTypedField("color", 0, typeof(int));

			this.model = this.InitModelField("model", 0);
			this.weight = this.InitTypedField("weight", 0, typeof(float));
			this.height = this.InitTypedField("height", 0, typeof(int));
			int modelNum;
			if (ConvertTools.TryParseInt32(defname.Substring(2), out modelNum)) {
				this.model.SetFromScripts(filename, headerLine, modelNum.ToString(CultureInfo.InvariantCulture));
			} else if (this is AbstractItemDef) {
				this.model.SetFromScripts(filename, headerLine, Globals.DefaultItemModel.ToString(CultureInfo.InvariantCulture));
			} else if (this is AbstractCharacterDef) {
				this.model.SetFromScripts(filename, headerLine, Globals.DefaultCharModel.ToString(CultureInfo.InvariantCulture));
			} else {
				throw new ScriptException("Char or item? This should NOT happen!");
			}
		}

		#region Accessors
		/**
			This does NOT check Constants (and must not, or it would trigger infinite recursion from Constant.EvaluateToDef(string)).
		*/
		public new static ThingDef GetByDefname(string defname) {
			return AbstractScript.GetByDefname(defname) as ThingDef;
		}

		/**
			This does NOT check Constants.
			
			Searches for and returns a ThingDef for the specified model #. It is recommended that you use
			FindItemDef or FindCharDef instead of using this method.
			
			This checks chardefs first, unless the defnumber is too high to be a chardef.
			It checks itemdefs if we didn't find a chardef (or if we didn't look, if the defnumber was too high).
			It will return null if the defnumber doesn't correspond to a chardef or itemdef.
			This calls FindCharDef and FindItemDef to do the real work.
			
			@param defnumber The ID# of the def, which must be bigger or equal to 0 and smaller than MaxItemModels.
		*/
		public static ThingDef GetByModel(int defnumber) {
			ThingDef tdone = null;
			tdone = FindCharDef(defnumber);
			if (tdone == null) {
				return FindItemDef(defnumber);
			}
			return tdone;
		}

		public virtual string Name {
			get {
				return (string) this.name.CurrentValue;
			}
			set {
				this.name.CurrentValue = value;
			}
		}

		public int Model {
			get {
				return (int) this.model.CurrentValue;
			}
			set {
				this.model.CurrentValue = value;
			}
		}

		public int Color {
			get {
				return (int) this.color.CurrentValue;
			}
			set {
				this.color.CurrentValue = value;
			}
		}

		public float Weight {
			get {
				return (float) this.weight.CurrentValue;
			}
			set {
				this.weight.CurrentValue = value;
			}
		}

		public virtual int Height {
			get
			{
				if (this.height.IsDefaultCodedValue) {
					return Map.PersonHeight;
				}
				return (int) this.height.CurrentValue;
			}
			set {
				this.height.CurrentValue = value;
			}
		}

		public override string ToString()
		{
			if (this.model.CurrentValue == null) {
				return this.Name + ": " + this.Defname + "//" + this.Altdefname + " (null model!)";
			}
			return this.Name + ": " + this.Defname + "//" + this.Altdefname + " (" + this.model.CurrentValue + ")";
		}

		public abstract bool IsItemDef { get; }
		public abstract bool IsCharDef { get; }

		#endregion Accessors

		#region Thing factory methods
		internal Thing CreateWhenLoading() {
			this.ThrowIfUnloaded();
			return this.CreateImpl();
		}

		public Thing Create(int x, int y, int z, byte m) {
			this.ThrowIfUnloaded();
			Thing retVal = this.CreateImpl();
			PutOnGround(retVal, x, y, z, m);
			this.Trigger_Create(retVal);
			return retVal;
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public Thing Create(IPoint4D point) {
			point = point.TopPoint;
			return this.Create(point.X, point.Y, point.Z, point.M);
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public Thing Create(Thing cont) {
			this.ThrowIfUnloaded();
			Thing retVal = this.CreateImpl();
			AbstractItem item = retVal as AbstractItem;
			if (item == null) {//we are char
				MutablePoint4D p = cont.TopObj().point4d;
				PutOnGround(retVal, p.x, p.y, p.z, p.m);
			} else {
				AbstractCharacter contAsChar = cont as AbstractCharacter;
				if (contAsChar != null) {
					if (retVal.IsEquippable) {
						PutInCont(item, contAsChar);
					} else {
						PutInCont(item, contAsChar.GetBackpack());
					}
				} else if (cont.IsContainer) {
					PutInCont(item, cont);
				} else {
					MutablePoint4D p = cont.TopObj().point4d;
					PutOnGround(item, p.x, p.y, p.z, p.m);
				}
			}

			this.Trigger_Create(retVal);
			return retVal;
		}

		private static void PutOnGround(Thing t, int x, int y, int z, byte m) {
			//MarkAsLimbo(t);
			AbstractItem item = t as AbstractItem;
			if (item != null) {
				item.Trigger_EnterRegion(x, y, z, m);
			} else {
				t.point4d.SetXYZM(x, y, z, m);
				Map.GetMap(m).Add(t);
			}
		}

		private static void PutInCont(AbstractItem item, Thing cont) {
			AbstractItem contItem = cont as AbstractItem;
			if (contItem == null) {
				//MarkAsLimbo(item);
				int layer = item.Layer;
				if (layer > 0) {
					item.Trigger_EnterChar((AbstractCharacter) cont, layer);
				} else {
					throw new SEException("Item '" + item + "' is equippable, but has Layer not set.");
				}
			} else {
				//MarkAsLimbo(item);
				int x, y;
				contItem.GetRandomXYInside(out x, out y);
				item.Trigger_EnterItem(contItem, x, y);
			}
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member"), SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal void Trigger_Create(Thing createdThing) {
			this.On_Create(createdThing);
			createdThing.TryTrigger(TriggerKey.create, null);
			try {
				createdThing.On_Create();
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		protected virtual void On_Create(Thing t) {

		}

		protected abstract Thing CreateImpl();
		#endregion Thing factory methods

		#region TriggerGroupHolder helper methods
		internal void Trigger(Thing self, TriggerKey td, ScriptArgs sa) {
			if (this.firstTGListNode != null) {
				PluginHolder.TGListNode curNode = this.firstTGListNode;
				do {
					curNode.storedTG.Run(self, td, sa);
					curNode = curNode.nextNode;
				} while (curNode != null);
			}
		}

		internal void TryTrigger(Thing self, TriggerKey td, ScriptArgs sa) {
			if (this.firstTGListNode != null) {
				PluginHolder.TGListNode curNode = this.firstTGListNode;
				do {
					curNode.storedTG.TryRun(self, td, sa);
					curNode = curNode.nextNode;
				} while (curNode != null);
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal TriggerResult CancellableTrigger(Thing self, TriggerKey td, ScriptArgs sa) {
			if (this.firstTGListNode != null) {
				PluginHolder.TGListNode curNode = this.firstTGListNode;
				do {
					object retVal = curNode.storedTG.Run(self, td, sa);
					if (TagMath.Is1(retVal)) {
						return TriggerResult.Cancel;
					}
					curNode = curNode.nextNode;
				} while (curNode != null);
			}
			return TriggerResult.Continue;
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal TriggerResult TryCancellableTrigger(Thing self, TriggerKey td, ScriptArgs sa) {
			if (this.firstTGListNode != null) {
				PluginHolder.TGListNode curNode = this.firstTGListNode;
				do {
					object retVal = curNode.storedTG.TryRun(self, td, sa);
					if (TagMath.Is1(retVal)) {
						return TriggerResult.Cancel;
					}
					curNode = curNode.nextNode;
				} while (curNode != null);
			}
			return TriggerResult.Continue;
		}
		#endregion TriggerGroupHolder helper methods

		#region Loading from scripts

		protected override void LoadScriptLine(string filename, int line, string param, string args) {
			if ("flag".Equals(param)) {
				param = "flags";
			}
			base.LoadScriptLine(filename, line, param, args);//the AbstractDef Loadline
		}

		public static void RegisterThingDef(Type thingDefType, Type thingType) {
			Type t;
			if (thingDefTypesByThingType.TryGetValue(thingDefType, out t)) {
				throw new OverrideNotAllowedException("ThingDef type " + LogStr.Ident(thingDefType.FullName) + " already has it's Thing type -" + t.FullName + ".");
			}
			if (thingTypesByThingDefType.TryGetValue(thingType, out t)) {
				throw new OverrideNotAllowedException("Thing type " + LogStr.Ident(thingType.FullName) + " already has it's ThingDef type -" + t.FullName + ".");
			}

			//ConstructorInfo ci = thingDefType.GetConstructor(thingDefConstructorParamTypes);
			//if (ci == null) {
			//    throw new SEException("Proper constructor not found.");
			//}
			thingDefTypesByThingType[thingType] = thingDefType;
			thingTypesByThingDefType[thingDefType] = thingType;
			//thingDefTypesByName[thingDefType.Name] = thingDefType;
			//thingDefCtors[thingDefType] = MemberWrapper.GetWrapperFor(ci);
		}

		/**
			This does NOT check Constants.
			
			Returns the AbstractItemDef for the specified model # (which is declared [ItemClass 0xnumber], like [AbstractItem 0x3cf]).
			If there is no AbstractItemDef for that model #, this returns null.
			Passing a defnumber which is too high is an error.
			
			@param defnumber The ID# of the def, which must be bigger or equal to 0 and smaller than MaxItemModels.
		*/
		public static AbstractItemDef FindItemDef(int defnumber) {
			AbstractItemDef def;
			itemModelDefs.TryGetValue(defnumber, out def);
			return def;
		}

		/**
			This does NOT check Constants.
			
			Returns the AbstractItemDef for the specified model # (which is declared [ItemClass 0xnumber], like [AbstractItem 0x3cf]).
			If there is no AbstractItemDef for that model #, this returns null.
			Passing a defnumber which is too high is an error.
			
			@param defnumber The ID# of the def, which must be bigger or equal to 0 and smaller than MaxItemModels.
		*/
		public static AbstractCharacterDef FindCharDef(int defnumber) {
			AbstractCharacterDef def;
			charModelDefs.TryGetValue(defnumber, out def);
			return def;
		}

		internal static void StartingLoading() {

		}

		public new static void Bootstrap() {
			//ThingDef script sections are special in that they can have numeric header indicating model
			RegisterDefnameParser<ThingDef>(ParseDefnames);
		}


		private static void ParseDefnames(PropsSection section, out string defname, out string altdefname) {
			string typeName = section.HeaderType.ToLowerInvariant();
			Type thingDefType = GetDefTypeByName(typeName);
			if (thingDefType == null) {
				throw new SEException("Type " + LogStr.Ident(typeName) + " does not exist.");
			}

			int defnum;
			defname = section.HeaderName.ToLowerInvariant();
			if (ConvertTools.TryParseInt32(defname, out defnum)) {
				if (thingDefType.IsSubclassOf(typeof(AbstractItemDef))) {
					defname = "i_0x" + defnum.ToString("x", CultureInfo.InvariantCulture);
				} else if (thingDefType.IsSubclassOf(typeof(AbstractCharacterDef))) {
					defname = "c_0x" + defnum.ToString("x", CultureInfo.InvariantCulture);
				} else {//this probably can not happen, but one is never too sure :)
					throw new SEException("The ThingDef Type " + LogStr.Ident(thingDefType) + " is neither AbstractCharacterDef nor AbstractItemDef subclass. Ignoring.");
				}
			}

			PropsLine defnameLine = section.TryPopPropsLine("defname");
			if (defnameLine != null) {
				altdefname = ConvertTools.LoadSimpleQuotedString(defnameLine.Value);

				if (string.Equals(defname, altdefname, StringComparison.OrdinalIgnoreCase)) {
					Logger.WriteWarning("Defname redundantly specified for " + section.HeaderType + " " + LogStr.Ident(defname) + ".");
					altdefname = null;
				}
			} else {
				altdefname = null;
			}
		}

		public override void LoadScriptLines(PropsSection ps) {
			int defnum;
			string defname = this.Defname;
			if (ConvertTools.TryParseInt32(defname.Substring(2), out defnum)) {
				if (this.IsCharDef && defname.StartsWith("c_")) {
					if (defnum > highestCharModel) {
						highestCharModel = defnum;
					}
					//Sanity.IfTrueThrow(idefnum>MaxCharModels, "defnum "+idefnum+" (0x"+idefnum.ToString("x")+") is higher than MaxCharModels ("+MaxCharModels+").");
					charModelDefs[defnum] = (AbstractCharacterDef) this;
				} else if (this.IsItemDef && defname.StartsWith("i_")) {
					if (defnum > highestItemModel) {
						highestItemModel = defnum;
					}
					//Sanity.IfTrueThrow(idefnum>MaxItemModels, "defnum "+idefnum+" (0x"+idefnum.ToString("x")+") is higher than MaxItemModels ("+MaxItemModels+").");
					itemModelDefs[defnum] = (AbstractItemDef) this;
				}
			}

			this.ClearTriggerGroups();//maybe clear other things too?

			base.LoadScriptLines(ps);

			//now do load the trigger code. 
			if (ps.TriggerCount > 0) {
				ps.HeaderName = "t__" + defname + "__";
				this.defaultTriggerGroup = ScriptedTriggerGroup.Load(ps);
				this.AddTriggerGroup(this.defaultTriggerGroup);
			}
		}

		public override void Unload() {
			if (this.defaultTriggerGroup != null) {
				this.defaultTriggerGroup.Unload();
			}
			base.Unload();
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal new static void LoadingFinished() {
			//dump number of loaded instances?
			Logger.WriteDebug("Highest itemdef model #: " + highestItemModel + " (0x" + highestItemModel.ToString("x", CultureInfo.InvariantCulture) + ")");
			Logger.WriteDebug("Highest chardef model #: " + highestCharModel + " (0x" + highestCharModel.ToString("x", CultureInfo.InvariantCulture) + ")");

			int count = AllScriptsByDefname.Count;
			using (StopWatch.StartAndDisplay("Resolving dupelists and multidata...")) {
				int a = 0;
				int countPerCent = count / 200;
				foreach (AbstractScript td in AllScripts) {
					if ((a % countPerCent) == 0) {
						Logger.SetTitle("Resolving dupelists and multidata: " + ((a * 100) / count) + " %");
					}
					AbstractItemDef idef = td as AbstractItemDef;
					if (idef != null) {
						try {
							AbstractItemDef dupeItem = idef.DupeItem;
							if (dupeItem != null) {
								dupeItem.AddToDupeList(idef);
							}
						} catch (FatalException) {
							throw;
						} catch (Exception e) {
							Logger.WriteWarning(e);
						}

						try {
							idef.multiData = MultiData.GetByModel(idef.Model);
						} catch (FatalException) {
							throw;
						} catch (Exception e) {
							Logger.WriteWarning(e);
						}

					}
					a++;
				}
			}
			Logger.SetTitle("");
		}

		internal new static void ForgetAll() {
			thingDefTypesByThingType.Clear();//we can assume that inside core there are no non-abstract thingdefs
			thingTypesByThingDefType.Clear();//we can assume that inside core there are no non-abstract thingdefs
			//thingDefTypesByName.Clear();
			thingDefCtors.Clear();

			itemModelDefs.Clear();
			charModelDefs.Clear();
		}
		#endregion Loading from scripts
	}
}