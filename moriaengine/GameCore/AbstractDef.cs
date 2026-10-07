
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using SteamEngine.Common;
using SteamEngine.CompiledScripts;
using SteamEngine.Persistence;

namespace SteamEngine {
	public abstract class AbstractDef : AbstractScript, ITagHolder {
		static int uids;
		private int uid;

		private readonly Hashtable fieldValues = new Hashtable(StringComparer.OrdinalIgnoreCase); //not dictionary because keys are both strings and tagkeys
		private readonly string filename;
		private readonly int headerLine;
		private string altdefname;
		private bool alreadySaved;

		#region Accessors
		public string Filename {
			get {
				return this.filename;
			}
		}

		public int HeaderLine {
			get {
				return this.headerLine;
			}
		}

		public override string ToString() {
			//zobrazovat budeme radsi napred defname a pak udaj o typu (v dialozich se to totiz casto nevejde)
			if (string.IsNullOrEmpty(this.altdefname) || string.IsNullOrEmpty(this.Defname)) {
				return string.Concat("[", this.PrettyDefname, " : ", Tools.TypeToString(this.GetType()), "]");
				//return string.Concat("[", Tools.TypeToString(this.GetType()), " ", this.PrettyDefname, "]");
			}
			return string.Concat("[", this.Defname, "/", this.altdefname, " : ", Tools.TypeToString(this.GetType()), "]");
			//return string.Concat("[", Tools.TypeToString(this.GetType()), " ", this.Defname, "/", this.altdefname, "]");
		}

		public string Filepos {
			get {
				return "line " + this.headerLine + " of " + this.filename;
			}
		}

		public string Altdefname {
			get {
				return this.altdefname;
			}
			protected set {
				this.Unregister();
				this.altdefname = value;
				this.Register();
			}
		}

		public override string PrettyDefname {
			get {
				//from the two available defnames (like c_0x190 and c_man) it returns the more understandable (c_man)
				if (this.Defname.IndexOf("_0x") > 0) {
					if (this.altdefname != null) {
						return this.altdefname;
					}
				}
				return this.Defname;
			}
		}

		public override bool Equals(object obj) {
			return obj == this;
		}

		public override int GetHashCode() {
			return this.uid;
		}

		public new static AbstractDef GetByDefname(string name) {
			return AbstractScript.GetByDefname(name) as AbstractDef;
		}
		#endregion Accessors

		#region Loading from saves
		internal static void LoadSectionFromSaves(PropsSection input) {
			//todo: a way to load new defs (or just regions)

			string typeName = input.HeaderType;
			string defname = input.HeaderName;

			AbstractDef def = GetByDefname(defname);
			if (def == null) {
				Logger.WriteError(input.Filename, input.HeaderLine, LogStr.Ident(typeName + " " + defname)
					+ " is in the world being loaded, but it was not defined in the scripts. Skipping.");
				return;
			}
			if (!StringComparer.OrdinalIgnoreCase.Equals(def.GetType().Name, typeName)) {
				Logger.WriteWarning(input.Filename, input.HeaderLine,
					LogStr.Ident(typeName + " " + defname) + " declared wrong class. It is in fact " + LogStr.Ident(Tools.TypeToString(def.GetType())) + ".");
			}

			def.LoadFromSaves(input);
		}

		public virtual void LoadFromSaves(PropsSection input) {
			foreach (PropsLine pl in input.PropsLines) {
				ObjectSaver.Load(pl.Value, this.LoadField_Delayed, this.filename, pl.Line,
					pl.Name);
			}
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "filename"), SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		private void LoadField_Delayed(object resolvedObject, string filename, int line, object args) {
			string fieldName = (string) args;
			FieldValue fv = (FieldValue) this.fieldValues[fieldName];

			if (fv == null) {//that means it's not in scripts
				Match m = TagHolder.tagRE.Match(fieldName);
				if (m.Success) {	//If the name begins with 'tag.'
					string tagName = m.Groups["name"].Value;
					TagKey tk = TagKey.Acquire(tagName);
					tagName = "tag." + tagName;
					fv = new FieldValue(tagName, FieldValueType.Typeless, null, "", -1, "");
					this.fieldValues[tk] = fv;
					this.fieldValues[tagName] = fv;
				} else {
					Logger.WriteWarning("Unknown saved FieldValue line: " + fieldName + " = '" + resolvedObject + "'");
					return;
				}
			}

			if (fv != null) {
				try {
					fv.CurrentValue = resolvedObject;
				} catch (FatalException) {
					throw;
				} catch (Exception e) {
					Logger.WriteWarning(filename, line, e);
				}
			}
		}
		#endregion Loading from saves

		#region FieldValue methods
		protected FieldValue InitTypedField(string name, object value, Type type) {
			if (typeof(ThingDef).IsAssignableFrom(type)) {
				return this.InitThingDefField(name, value, type);
			}
			FieldValue fieldValue = (FieldValue) this.fieldValues[name];
			if (fieldValue == null) {
				fieldValue = new FieldValue(name, FieldValueType.Typed, type, value);
				this.fieldValues[name] = fieldValue;
			} else {
				throw new SanityCheckException("InitField_Typed called , when a fieldvalue called " + LogStr.Ident(name) + " already exists.");
			}
			return fieldValue;
		}

		protected FieldValue InitThingDefField(string name, object value, Type type) {
			FieldValue fieldValue = (FieldValue) this.fieldValues[name];
			if (fieldValue == null) {
				fieldValue = new FieldValue(name, FieldValueType.ThingDefType, type, value);
				this.fieldValues[name] = fieldValue;
			} else {
				throw new SanityCheckException("InitField_Typed called , when a fieldvalue called " + LogStr.Ident(name) + " already exists.");
			}
			return fieldValue;
		}

		protected FieldValue InitModelField(string name, object value) {
			FieldValue fieldValue = (FieldValue) this.fieldValues[name];
			if (fieldValue == null) {
				fieldValue = new FieldValue(name, FieldValueType.Model, null, value);
				this.fieldValues[name] = fieldValue;
			} else {
				throw new SanityCheckException("InitField_Model called , when a fieldvalue called " + LogStr.Ident(name) + " already exists.");
			}
			return fieldValue;
		}

		protected FieldValue InitTypelessField(string name, object value) {
			FieldValue fieldValue = (FieldValue) this.fieldValues[name];
			if (fieldValue == null) {
				fieldValue = new FieldValue(name, FieldValueType.Typeless, null, value);
				this.fieldValues[name] = fieldValue;
			} else {
				throw new SanityCheckException("InitField_Typeless called , when a fieldvalue called " + LogStr.Ident(name) + " already exists.");
			}
			return fieldValue;
		}

		protected bool HasFieldValue(string name) {
			return this.fieldValues.ContainsKey(name);
		}

		//public void SetDefaultFieldValue(string name, object value) {
		//    ((FieldValue) this.fieldValues[name]).DefaultValue = value;
		//}

		protected virtual void SetCurrentFieldValue(string name, object value) {
			((FieldValue) this.fieldValues[name]).CurrentValue = value;
		}

		protected object GetDefaultFieldValue(string name) {
			FieldValue fv = (FieldValue) this.fieldValues[name];
			if (fv != null) {
				return fv.DefaultValue;
			}
			return null;
		}

		protected object GetCurrentFieldValue(string name) {
			FieldValue fv = (FieldValue) this.fieldValues[name];
			if (fv != null) {
				return fv.CurrentValue;
			}
			return null;
		}
		#endregion FieldValue methods

		#region Saving
		internal static void SaveAll(SaveStream output) {
			Logger.WriteDebug("Saving defs.");
			output.WriteComment("Defs");

			foreach (AbstractScript script in AllScripts) {
				AbstractDef def = script as AbstractDef;
				if (def != null) {
					def.alreadySaved = false;
				}
			}
			int count = AllScripts.Count;
			int a = 0;
			int countPerCent = count / 200;
			foreach (AbstractScript script in AllScripts) {
				AbstractDef def = script as AbstractDef;
				if (def != null) {
					if ((a % countPerCent) == 0) {
						Logger.SetTitle("Saving defs: " + ((a * 100) / count) + " %");
					}
					if ((!def.IsUnloaded) && (!def.alreadySaved)) {
						def.Save(output);
						def.alreadySaved = true;
					}
					a++;
				}
			}
			Logger.SetTitle("");
			output.WriteLine();
			ObjectSaver.FlushCache(output);
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public void Save(SaveStream output) {
			bool headerWritten = false;
			foreach (DictionaryEntry entry in this.fieldValues) {
				if (entry.Key is TagKey) {//so that tags dont get written twice
					continue;
				}
				try {
					FieldValue fv = (FieldValue) entry.Value;
					if (fv.ShouldBeSaved()) {
						if (!headerWritten) {
							output.WriteLine();
							output.WriteSection(this.GetType().Name, this.PrettyDefname);
							headerWritten = true;
						}
						output.WriteValue(fv.Name, fv.CurrentValue);
					}
				} catch (FatalException) {
					throw;
				} catch (Exception e) {
					Logger.WriteWarning(e);//this should not actually happen, I hope :)
				}
			}
			if (headerWritten) {
				ObjectSaver.FlushCache(output);
			}
		}
		#endregion Saving

		#region Loading from scripts
		private static readonly Type[] ConstructorParamTypes = { typeof(string), typeof(string), typeof(int) };

		private static Dictionary<string, ConstructorInfo> constructorsByTypeName = new Dictionary<string, ConstructorInfo>(StringComparer.OrdinalIgnoreCase);

		public delegate void DefnameParser(PropsSection section, out string defname, out string altdefname);

		private static Dictionary<Type, DefnameParser> defnameParsersByType_Registered = new Dictionary<Type, DefnameParser>();
		private static Dictionary<Type, DefnameParser> defnameParsersByType_Inferred = new Dictionary<Type, DefnameParser>();

		private List<PropsLine> postponedLines;

		public static Type GetDefTypeByName(string name) {
			ConstructorInfo ctor;
			if (constructorsByTypeName.TryGetValue(name, out ctor)) {
				return ctor.DeclaringType;
			}
			return null;
		}

		public static void RegisterDefnameParser<T>(DefnameParser parserMethod) where T : AbstractDef {
			defnameParsersByType_Registered.Add(typeof(T), parserMethod); //throws in case of duplicity
			defnameParsersByType_Inferred.Clear();
		}

		private static DefnameParser GetDefnameParser(Type defType) {
			DefnameParser parserMethod;

			if (defnameParsersByType_Inferred.TryGetValue(defType, out parserMethod)) {
				return parserMethod;
			}

			//lazy initialisation. We're looking for the according parser
			Type type = defType;
			while (type != typeof(AbstractDef)) {
				if (defnameParsersByType_Registered.TryGetValue(type, out parserMethod)) {
					defnameParsersByType_Inferred.Add(defType, parserMethod);
					return parserMethod;
				}
				type = type.BaseType;
			}

			parserMethod = DefaultDefnameParser;
			defnameParsersByType_Inferred.Add(defType, parserMethod);
			return parserMethod;
		}

		public new static void Bootstrap() {
			//all AbstractDef descendants will be registered so that they get loaded by our LoadFromScripts method
			ClassManager.RegisterSupplySubclasses<AbstractDef>(RegisterSubtype);
		}

		public static bool RegisterSubtype(Type defType) {
			if (!defType.IsAbstract) {
				ConstructorInfo ci;
				if (constructorsByTypeName.TryGetValue(defType.Name, out ci)) { //we have already a Def type named like that
					throw new OverrideNotAllowedException("Trying to overwrite class " + LogStr.Ident(ci.DeclaringType) + " in the register of AbstractDef classes.");
				}
				ci = defType.GetConstructor(ConstructorParamTypes);
				if (ci != null) {
					constructorsByTypeName[defType.Name] = MemberWrapper.GetWrapperFor(ci);

					ScriptLoader.RegisterScriptType(defType.Name, LoadFromScripts, false);
				}
			}
			return false;
		}

		//called by WorldSaver Load
		public static bool ExistsDefType(string name) {
			return constructorsByTypeName.ContainsKey(name);
		}

		protected AbstractDef(string defname, string filename, int headerLine)
			: base(defname) {
			this.filename = filename;
			this.headerLine = headerLine;
			this.uid = uids++;
		}

		public static IUnloadable LoadFromScripts(PropsSection input) {
			//it is something like this in the .scp file: [headerType headerName] = [WarcryDef a_warcry] etc.
			string typeName = input.HeaderType;
			ConstructorInfo constructor = constructorsByTypeName[typeName];
			Type type = constructor.DeclaringType;

			string defname, altdefname;
			GetDefnameParser(type)(input, out defname, out altdefname);
			defname = string.Intern(string.Concat(defname));
			altdefname = string.Intern(string.Concat(altdefname));

			AbstractScript def = AbstractScript.GetByDefname(defname);
			if (!string.IsNullOrEmpty(altdefname)) {
				AbstractScript defByAltdefname = AbstractScript.GetByDefname(altdefname);
				if (defByAltdefname != null) {
					if (def == null) {
						def = defByAltdefname;
						defByAltdefname = null;
					} else if (defByAltdefname != def) {
						throw new OverrideNotAllowedException("Header defname '" + defname + "' and alternate defname '" + altdefname + "' identify two different objects: "
							+ "'" + def + "' and '" + defByAltdefname + "'. Ignoring.");
					}
				}
			}

			//check if it isn't another type or duplicity			
			if (def != null)
			{
				if (def.GetType() != type) {
					throw new OverrideNotAllowedException("You can not change the class of a Def while resync. You have to recompile or restart to achieve that. Ignoring.");
				}
				if (!def.IsUnloaded) {
					throw new OverrideNotAllowedException(def + " defined multiple times.");
				}
			}

			//construct new or resync
			AbstractDef constructed = (AbstractDef) def;
			if (def == null) {
				object[] cargs = { defname, input.Filename, input.HeaderLine };
				constructed = (AbstractDef) constructor.Invoke(cargs);
			} else {
				//?call some kind of "OnResync" virtual method?
				constructed.UnUnload();
				constructed.Unregister();
			}

			constructed.InternalSetDefname(defname);
			constructed.altdefname = altdefname;

			constructed.LoadScriptLines(input);

			constructed.Register();

			return constructed;
		}

		private static void DefaultDefnameParser(PropsSection section, out string defname, out string altdefname) {
			string typeName = section.HeaderType.ToLowerInvariant();
			defname = section.HeaderName.ToLowerInvariant();

			bool defnameIsNum;
			defname = DeNumerizeDefname(typeName, defname, out defnameIsNum);

			altdefname = null;
			PropsLine defnameLine = section.TryPopPropsLine("defname");
			if (defnameLine != null) {
				altdefname = defnameLine.Value;
				bool altdefnameIsNum;
				altdefname = DeNumerizeDefname(typeName, altdefname, out altdefnameIsNum);

				if (string.Equals(defname, altdefname, StringComparison.OrdinalIgnoreCase)) {
					Logger.WriteWarning("Defname redundantly specified for " + section.HeaderType + " " + LogStr.Ident(defname) + ".");
					altdefname = null;
				}

				//if header name is a number, we put it as alt
				if (defnameIsNum && !altdefnameIsNum) {
					string t = altdefname;
					altdefname = defname;
					defname = t;
				}
			}
		}

		private static string DeNumerizeDefname(string typeName, string defname, out bool isNumeric) {
			int defnum;
			isNumeric = false;
			if (ConvertTools.TryParseInt32(defname, out defnum)) {
				defname = defnum.ToString("x", CultureInfo.InvariantCulture);
				defname = string.Concat("_", typeName, "_0x", defname, "_");
				isNumeric = true;
			}
			return defname;
		}


		//register with static dictionaries and lists. 
		//Can be called multiple times without harm
		public override AbstractScript Register() {
			try {
				if (!string.IsNullOrEmpty(this.altdefname)) {
					var previous = AllScriptsByDefname.GetOrAdd(this.altdefname, this);
					if (previous != this) {
						throw new SEException("previous != this when registering AbstractScript '" + this.altdefname + "'");
					}
				}
			} finally {
				base.Register();
			}
			return this;
		}

		//unregister from static dictionaries and lists. 
		//Can be called multiple times without harm
		protected override void Unregister() {
			try {
				if (!string.IsNullOrEmpty(this.altdefname)) {

					AbstractScript previous;
					if (AllScriptsByDefname.TryRemove(this.altdefname, out previous)) {
						if (previous != this) {
							if (!AllScriptsByDefname.TryAdd(this.altdefname, previous)) {
								throw new FatalException("Parallel loading fucked up.");
							}
							throw new SEException("previous != this when unregistering AbstractScript '" + this.altdefname + "'");
						}
					}
				}
			} finally {
				base.Unregister();
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public virtual void LoadScriptLines(PropsSection ps) {
			foreach (PropsLine p in ps.PropsLines) {
				try {
					string name = p.Name.ToLowerInvariant();

					if (name.StartsWith("tag.") || (this.fieldValues.ContainsKey(name))) {
						this.LoadScriptLine(ps.Filename, p.Line, name, p.Value);
					} else {
						if (this.postponedLines == null) {
							this.postponedLines = new List<PropsLine>();
						}
						this.postponedLines.Add(p);
					}
				} catch (FatalException) {
					throw;
				} catch (Exception ex) {
					Logger.WriteWarning(ps.Filename, p.Line, ex);
				}
			}
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "filename")]
		protected virtual void LoadScriptLine(string filename, int line, string param, string args) {
			Match m = TagHolder.tagRE.Match(param);
			FieldValue fieldValue;
			if (m.Success) {	//If the name begins with 'tag.'
				string tagName = m.Groups["name"].Value;
				TagKey tk = TagKey.Acquire(tagName);
				fieldValue = (FieldValue) this.fieldValues[tk];
				if (fieldValue == null) {
					tagName = "tag." + tagName;
					fieldValue = new FieldValue(tagName, FieldValueType.Typeless, null, filename, line, args);
					this.fieldValues[tk] = fieldValue;
					this.fieldValues[tagName] = fieldValue;
				} else {
					fieldValue.SetFromScripts(filename, line, args);
				}
				return;
			}

			switch (param) {
				case "category":
				case "subsection":
				case "description":
					return;
				//axis props are ignored. Or shouldnt they? :)
				default:
					fieldValue = (FieldValue) this.fieldValues[param];
					if (fieldValue != null) {
						fieldValue.SetFromScripts(filename, line, args);
						return;
					}
					throw new ScriptException("Invalid data '" + LogStr.Ident(param) + "' = '" + LogStr.Number(args) + "'.");
			}
		}

		internal static void LoadingFinished() {

			//load postponed lines. That are those which are not initialised by the start of the loading.
			//this is so scripts can load dynamically named defnames, where the dynamic names can depend on not-yet-loaded other scripts
			//this way, for example ProfessionDef definition can list skills and abilities
			foreach (AbstractScript script in AllScripts) {
				AbstractDef def = script as AbstractDef;
				if (def != null) {
					def.LoadPostponedScriptLines();
					def.Trigger_AfterLoadFromScripts();
				}
			}
		}

		private void LoadPostponedScriptLines() {
			if (this.postponedLines != null) {
				foreach (PropsLine p in this.postponedLines) {
					try {
						this.LoadScriptLine(this.filename, p.Line, p.Name.ToLowerInvariant(), p.Value);
					} catch (FatalException) {
						throw;
					} catch (Exception ex) {
						Logger.WriteWarning(this.filename, p.Line, ex);
					}
				}
				this.postponedLines = null;
			}
		}

		private void Trigger_AfterLoadFromScripts() {
			try {
				this.On_AfterLoadFromScripts();
			} catch (FatalException) {
				throw;
			} catch (SEException se) {
				se.TryAddFileLineInfo(this.filename, this.headerLine);
				Logger.WriteError(se);
			} catch (Exception e) {
				Logger.WriteError(this.filename, this.headerLine, e);
			}
		}

		protected virtual void On_AfterLoadFromScripts() {
		}

		/// <summary>
		/// Resolves all fieldvalues of all defs
		/// </summary>
		/// <remarks>
		/// This method is called on startup when the resolveEverythingAtStart in steamengine.ini is set to True
		/// </remarks>
		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public static void ResolveAll() {
			int count = AllScripts.Count;
			Logger.WriteDebug("Resolving " + count + " defs");

			DateTime before = DateTime.Now;
			int a = 0;
			int countPerCent = count / 100;
			foreach (AbstractScript script in AllScripts) {
				AbstractDef def = script as AbstractDef;
				if (def != null) {
					if ((a % countPerCent) == 0) {
						Logger.SetTitle("Resolving def field values: " + ((a * 100) / count) + " %");
					}
					if (!def.IsUnloaded) {//those should have already stated what's the problem :)
						foreach (FieldValue fv in def.fieldValues.Values) {
							try {
								fv.ResolveTemporaryState();
							} catch (FatalException) {
								throw;
							} catch (Exception e) {
								Logger.WriteWarning(e);
							}
						}
					}
					a++;
				}
			}
			DateTime after = DateTime.Now;
			Logger.WriteDebug("...took " + (after - before));
			Logger.SetTitle("");
		}

		public override void Unload() {
			foreach (FieldValue fv in this.fieldValues.Values) {
				fv.Unload();
			}

			base.Unload();

			//todo: clear the other various properties...
			//todo: not clear those tags/tgs/timers/whatever that were set dynamically (ie not in scripted defs)
		}

		//unloads instances that come from scripts.
		internal static void ForgetScripts() {
			ForgetAll(); //just to be sure - unregister everything. Should be called by Main anyway

			constructorsByTypeName.Clear();//we assume that inside core there are no non-abstract defs

			//only leave the defnameParsers defined in core (like AbstractSkillDef and ThingDef probably? :)
			defnameParsersByType_Inferred.Clear();

			foreach (Type t in new List<Type>(defnameParsersByType_Registered.Keys)) {
				if (t.Assembly != ClassManager.CoreAssembly) { //not in core
					defnameParsersByType_Registered.Remove(t);
				}
			}
		}
		#endregion Loading from scripts

		#region ITagHolder methods
		public object GetTag(TagKey tk) {
			FieldValue fv = (FieldValue) this.fieldValues[tk];
			if (fv != null) {
				return fv.CurrentValue;
			}
			return null;
		}

		public void SetTag(TagKey tk, object value) {
			FieldValue fv = (FieldValue) this.fieldValues[tk];
			if (fv == null) {
				string tagName = "tag." + tk;
				fv = new FieldValue(tagName, FieldValueType.Typeless, null, "", -1, "");
				this.fieldValues[tk] = fv;
				this.fieldValues[tagName] = fv;
			}
			fv.CurrentValue = value;
		}

		public bool HasTag(TagKey tk) {
			return (this.fieldValues.ContainsKey(tk));
		}

		public void RemoveTag(TagKey tk) {
			FieldValue fv = (FieldValue) this.fieldValues[tk];
			if (fv != null) {
				fv.CurrentValue = null;
			}
		}

		public void ClearTags() {
			foreach (DictionaryEntry entry in this.fieldValues) {
				if (entry.Key is TagKey) {
					FieldValue fv = (FieldValue) entry.Value;
					fv.CurrentValue = null;
				}
			}
		}
		#endregion ITagHolder methods
	}
}
