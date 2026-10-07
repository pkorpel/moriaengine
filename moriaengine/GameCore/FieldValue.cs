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
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using EQATEC.Profiler;
using SteamEngine.Common;
using SteamEngine.CompiledScripts;
using SteamEngine.LScript;

namespace SteamEngine {
	enum FieldValueType : byte {
		Model, Typed, Typeless, ThingDefType
	}

	public interface IFieldValueParser {
		Type HandledType { get; }
		[SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate")]
		bool TryParse(string input, out object retVal);
	}

	public sealed class FieldValue : IUnloadable {
		private static Dictionary<Type, IFieldValueParser> parsers = new Dictionary<Type, IFieldValueParser>();

		private string name;
		private FieldValueType fvType;
		private Type type;
		private bool isChangedManually;
		private bool isSetFromScripts;
		private bool unloaded;

		private FieldValueImpl currentValue;
		private FieldValueImpl defaultValue;

		internal FieldValue(string name, FieldValueType fvType, Type type, string filename, int line, string value) {
			this.name = name;
			this.fvType = fvType;
			this.type = type;

			this.SetFromScripts(filename, line, value);
		}

		internal FieldValue(string name, FieldValueType fvType, Type type, object value) {
			this.name = name;
			this.fvType = fvType;
			this.type = type;

			this.SetFromCode(value);
		}

		public static void Bootstrap() {
			ClassManager.RegisterSupplySubclassInstances<IFieldValueParser>(RegisterParser, false, false);
		}

		public static void RegisterParser(IFieldValueParser parser) {
			Type t = parser.HandledType;
			foreach (Type knownType in parsers.Keys) {
				if (t.IsAssignableFrom(knownType) || knownType.IsAssignableFrom(t)) {
					throw new SEException(parser + " is incompatible with " + parsers[knownType] + " as FieldValue parser");
				}
			}
			parsers[t] = parser;
		}

		public static IFieldValueParser GetParserFor(Type type) {
			IFieldValueParser outVal;
			if (parsers.TryGetValue(type, out outVal)) {
				return outVal;
			}
			return null;
		}

		internal static void ForgetScripts() {
			parsers.Clear();
		}

		public void Unload() {
			if (this.isChangedManually) {
				this.unloaded = true;
			}
		}

		public bool IsUnloaded {
			get { return this.unloaded; }
		}

		private void ThrowIfUnloaded() {
			if (this.unloaded) {
				throw new UnloadedException("The " + Tools.TypeToString(this.GetType()) + " '" + LogStr.Ident(this.name) + "' is unloaded.");
			}
		}

		public string Name {
			get {
				return this.name;
			}
		}

		internal void ResolveTemporaryState() {
			if (this.defaultValue is TemporaryValueImpl) {
				FieldValueImpl wasCurrent = this.currentValue;
				FieldValueImpl wasDefault = this.defaultValue;
				bool success = false;
				try {
					//first, resolve the default value using lscript
					TemporaryValueImpl tempVI = (TemporaryValueImpl) this.defaultValue;

					try {
						string value = tempVI.valueString;
						object retVal = null;
						if (value != null) {
							if (value.Length > 0) {
								if ((this.fvType == FieldValueType.Typed) && (this.type.IsArray)) {

									if (this.type.GetArrayRank() > 1) {
										throw new SEException("Can't use a multirank array in a FieldValue");
									}
									string[] sourceArray = Utility.SplitSphereString(value, false); //
									Type elemType = this.type.GetElementType();
									int n = sourceArray.Length;
									Array resultArray = Array.CreateInstance(elemType, n);

									for (int i = 0; i < n; i++) {
										resultArray.SetValue(ConvertTools.ConvertTo(elemType, this.ResolveSingleValue(tempVI, sourceArray[i], null)),
											i);
									}

									retVal = resultArray;
								} else {
									retVal = this.ResolveSingleValue(tempVI, value, retVal);
								}
							} else {
								retVal = "";
							}
						}

						this.defaultValue = this.GetFittingValueImpl();
						this.defaultValue.Value = retVal;
					} catch (SEException sex) {
						sex.TryAddFileLineInfo(tempVI.filename, tempVI.line);
						throw;
					} catch (Exception e) {
						throw new SEException(tempVI.filename, tempVI.line, e);
					}


					if (!this.isChangedManually) {//we were already resynced...the loaded value should not change
						this.currentValue = this.defaultValue.Clone();
					}

					success = true;
				} finally {
					if (!success) {
						this.currentValue = wasCurrent;
						this.defaultValue = wasDefault;
					}
				}
			}
		}

		private object ResolveSingleValue(TemporaryValueImpl tempVI, string value, object retVal) {
			if (!this.ResolveStringWithoutLScript(value, ref retVal)) {//this is a dirty shortcut to make resolving faster, without it would it last forever
				string statement = string.Concat("return(", value, ")");
				retVal = LScriptMain.RunSnippet(
					tempVI.filename, tempVI.line, Globals.Instance, statement);
			}
			return retVal;
		}

		private static readonly Regex simpleStringRE = new Regex(@"^""(?<value>[^\<\>]*)""\s*$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes"), SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		private bool ResolveStringWithoutLScript(string value, ref object retVal) {
			switch (this.fvType) {
				case FieldValueType.Typeless:
					if (TryResolveAsString(value, ref retVal)) {
						return true;
					}
					if (TryResolveAsScript(value, ref retVal)) {
						return true;
					}
					if (ConvertTools.TryParseAnyNumber(value, out retVal)) {
						return true;
					}
					if (TryResolveWithExternalParser(null, value, ref retVal)) {
						return true;
					}
					break;
				case FieldValueType.Typed:
					TypeCode code = Type.GetTypeCode(this.type);
					switch (code) {
						case TypeCode.Boolean:
							bool b;
							if (ConvertTools.TryParseBoolean(value, out b)) {
								retVal = b;
								return true;
							}
							break;
						case TypeCode.Empty:
						case TypeCode.DateTime:
						case TypeCode.Object:
							if (typeof(AbstractScript).IsAssignableFrom(this.type)) {
								if (TryResolveAsScript(value, ref retVal)) {
									return true;
								}
							}
							if (TryResolveWithExternalParser(this.type, value, ref retVal)) {
								return true;
							}

							break;
						case TypeCode.String:
							string str = value.Trim().Trim('"');
							if (!str.Contains("<") && !str.Contains(">")) {
								retVal = str;
								return true;
							}
							break;
						default: //it's a number
							if (this.type.IsEnum) {
								try {
									string enumStr = value.Replace(this.type.Name + ".", "") // "FieldValueType.Typed | FieldValueType.Typeless" -> "Typed , Typeles"
										.Replace("|", ",").Replace("+", ","); //hopefully the actual value won't change by this optimisation ;)
									retVal = Enum.Parse(this.type, enumStr, true);
									return true;
								} catch { }
							}
							if (ConvertTools.TryParseSpecificNumber(code, value, out retVal)) {
								return true;
							}
							break;
					}
					break;
				case FieldValueType.Model:
					int s;
					if (ConvertTools.TryParseInt32(value, out s)) {
						retVal = s;
						return true;
					}
					if (TryResolveAsScript(value, ref retVal)) {
						if (retVal is ThingDef) {
							return true;
						}
					}
					break;
				case FieldValueType.ThingDefType:
					int id;
					if (ConvertTools.TryParseInt32(value, out id)) {
						ThingDef def;
						if (typeof(AbstractItemDef).IsAssignableFrom(this.type)) {
							def = ThingDef.FindItemDef(id);
						} else {
							def = ThingDef.FindCharDef(id);
						}
						if (def != null) {
							retVal = def;
							return true;
						}
					}
					if (TryResolveAsScript(value, ref retVal)) {
						if (retVal is ThingDef) {
							return true;
						}
					}
					break;
			}

			retVal = null;
			return false;
		}

		private static bool TryResolveWithExternalParser(Type returnType, string value, ref object retVal) {
			if (returnType != null) {
				IFieldValueParser parser;
				if (parsers.TryGetValue(returnType, out parser)) {
					return parser.TryParse(value, out retVal);
				}
			}
			foreach (KeyValuePair<Type, IFieldValueParser> pair in parsers) {
				if ((returnType == null) || (returnType.IsAssignableFrom(pair.Key))) {
					if (pair.Value.TryParse(value, out retVal)) {
						return true;
					}
				}
			}
			return false;
		}

		internal static bool TryResolveAsString(string value, ref object retVal) {
			Match m = simpleStringRE.Match(value);
			if (m.Success) {
				retVal = m.Groups["value"].Value;
				return true;
			}
			return false;
		}

		internal static bool TryResolveAsScript(string value, ref object retVal) {
			value = value.Trim().TrimStart('#');
			AbstractScript script = AbstractScript.GetByDefname(value);
			if (script != null) {
				retVal = script;
				return true;
			}
			return false;
		}

		[SkipInstrumentation]
		private FieldValueImpl GetFittingValueImpl() {
			switch (this.fvType) {
				case FieldValueType.Typeless:
					return new TypelessValueImpl();
				case FieldValueType.Typed:
					return new TypedValueImpl(this.type);
				case FieldValueType.ThingDefType:
					return new ThingDefValueImpl(this.type);
				case FieldValueType.Model:
					return new ModelValueImpl();
			}
			throw new SEException("this.fvType out of range. This should not happen.");
		}

		private void SetFromCode(object value) {
			Sanity.IfTrueThrow((this.isChangedManually || this.unloaded), "SetFromCode after change/unload? This should never happen.");

			this.defaultValue = this.GetFittingValueImpl();
			this.defaultValue.Value = value;
			this.currentValue = this.defaultValue.Clone();
			this.unloaded = false;
		}

		public void SetFromScripts(string filename, int line, string value) {
			if (this.isChangedManually) {
				this.defaultValue = new TemporaryValueImpl(filename, line, this, value);
			} else {
				this.currentValue = new TemporaryValueImpl(filename, line, this, value);
				this.defaultValue = new TemporaryValueImpl(filename, line, this, value);
			}

			this.isSetFromScripts = true;
			this.unloaded = false;
		}

		internal bool ShouldBeSaved() {
			if (this.unloaded) {
				return false;
			}
			if (this.isChangedManually) {//it was loaded/changed , so it should be also saved :)
				return !CurrentAndDefaultEquals(this.CurrentValue, this.DefaultValue);
			}
			if ((this.currentValue is TemporaryValueImpl) && (this.defaultValue is TemporaryValueImpl)) {
				return false;//unresolved, no need of touching
			}
			return !CurrentAndDefaultEquals(this.CurrentValue, this.DefaultValue);
		}

		private static bool CurrentAndDefaultEquals(object a, object b) {
			if ((a is Array) && (b is Array)) { //or should we equal Collection? Arrays should be the typical collection type here tho
				Array arrA = (Array) a;
				Array arrB = (Array) b;
				int n = arrA.Length;
				if ((n == arrB.Length) &&
					(arrA.GetType().GetElementType() == arrB.GetType().GetElementType())) {
					for (int i = 0; i < n; i++) {
						if (!CurrentAndDefaultEquals(arrA.GetValue(i), arrB.GetValue(i))) {
							return false;
						}
					}
					return true;
				}
				return false;
			}

			return Equals(a, b);
		}

		/// <summary>If true, it has not been set from scripts nor from saves nor manually</summary>
		public bool IsDefaultCodedValue {
			get {
				if (this.isSetFromScripts || this.isChangedManually) {
					return false;
				}
				if ((this.currentValue is TemporaryValueImpl) && (this.defaultValue is TemporaryValueImpl)) {
					return false; //unresolved, no need of touching
				}
				return true;
			}
		}

		public object CurrentValue {
			get {
				this.ThrowIfUnloaded();
				return this.currentValue.Value;
			}
			set {
				this.currentValue.Value = value;
				this.unloaded = false;
				this.isChangedManually = true;
			}
		}

		public object DefaultValue {
			get {
				this.ThrowIfUnloaded();
				return this.defaultValue.Value;
			}
			//set {
			//	defaultValue.Value = value;
			//	changedValue = true;
			//}
		}

		[SkipInstrumentation]
		private abstract class FieldValueImpl {
			internal abstract object Value { get; set; }
			internal abstract FieldValueImpl Clone();
		}

		private class TemporaryValueImpl : FieldValueImpl {
			internal string filename;
			internal int line;
			internal string valueString;
			FieldValue holder;

			internal TemporaryValueImpl(string filename, int line, FieldValue holder, string value) {
				this.filename = filename;
				this.line = line;
				this.holder = holder;
				this.valueString = value;
			}

			internal override FieldValueImpl Clone() {
				throw new SEException("this is not supposed to be cloned");
			}

			internal override object Value {
				get {
					this.holder.ResolveTemporaryState();
					if (this.holder.currentValue == this) {
						return this.holder.CurrentValue;
					}
					return this.holder.DefaultValue;
				}
				set {
					if (this.holder.currentValue == this) {
						this.holder.ResolveTemporaryState();
						this.holder.CurrentValue = value;
						return;
					}
					throw new SEException("Invalid TemporaryValueImpl instance, it's holder is not holding it, or something is setting defaultvalue. This should not happen.");
				}
			}
		}

		private sealed class ModelValueImpl : FieldValueImpl {
			ThingDef thingDef;
			int model;

			//resolving constructor
			[SkipInstrumentation]
			internal ModelValueImpl() {
			}

			[SkipInstrumentation]
			private ModelValueImpl(ModelValueImpl copyFrom) {
				this.thingDef = copyFrom.thingDef;
				this.model = copyFrom.model;
			}

			[SkipInstrumentation]
			internal override FieldValueImpl Clone() {
				return new ModelValueImpl(this);
			}

			internal override object Value {
				get
				{
					if (this.thingDef == null) {
						return this.model;
					}
					return this.thingDef.Model;
				}
				set {
					this.thingDef = value as ThingDef;
					if (this.thingDef == null) {
						this.model = ConvertTools.ToInt32(value);
					} else {
						if ((this.thingDef.model.currentValue == this) || (this.thingDef.model.defaultValue == this)) {
							ThingDef d = this.thingDef;
							this.thingDef = null;
							throw new ScriptException(LogStr.Ident(d) + " specifies its own defname as its model, could lead to infinite loop...!");
						}
					}
				}
			}

		}

		private class TypedValueImpl : FieldValueImpl {
			protected Type type;
			object val;

			//resolving constructor
			[SkipInstrumentation]
			internal TypedValueImpl(Type type) {
				this.type = type;
			}

			[SkipInstrumentation]
			protected TypedValueImpl(TypedValueImpl copyFrom) {
				this.type = copyFrom.type;
				this.val = copyFrom.val;
			}

			[SkipInstrumentation]
			internal override FieldValueImpl Clone() {
				return new TypedValueImpl(this);
			}

			private static object GetInternStringIfPossible(object obj) {
				string asString = obj as String;
				if (asString != null) {
					return String.Intern(asString);
				}
				return obj;
			}

			private static object ConvertSingleValue(Type type, object value) {
				string valueAsString = value as string;
				if (typeof(AbstractScript).IsAssignableFrom(type) && valueAsString != null) {
					valueAsString = valueAsString.Trim();
					valueAsString = valueAsString.TrimStart('#');
					AbstractScript script = AbstractScript.GetByDefname(valueAsString);
					if (script != null) {
						return script;
					}
				}
				return GetInternStringIfPossible(ConvertTools.ConvertTo(type, value)); //ConvertTo will throw exception if impossible
			}

			internal override object Value {
				get {
					return this.val;
				}
				set {
					if (value != null) {
						Type sourceType = value.GetType();
						if ((sourceType != this.type) && (this.type.IsArray)) {
							Array sourceArray;
							Array resultArray;
							Type elemType = this.type.GetElementType();
							if (sourceType.IsArray) {//we must change the element type
								if (sourceType.GetArrayRank() > 1) {
									throw new SEException("Can't use a multirank array in a FieldValue");
								}
								sourceArray = (Array) value;
							} else if (value is string) {
								sourceArray = Utility.SplitSphereString((string) value, false); //
							} else {
								sourceArray = new[] { value }; //just wrap it in a 1-element array, gets converted in the next step
							}

							int n = sourceArray.Length;
							resultArray = Array.CreateInstance(elemType, n);
							for (int i = 0; i < n; i++) {
								resultArray.SetValue(
									ConvertSingleValue(elemType, sourceArray.GetValue(i)), i);
							}

							this.val = ConvertTools.ConvertTo(this.type, resultArray); //this should actually do nothing, just for check
							return;
						}
					}
					this.val = ConvertSingleValue(this.type, value);
				}
			}
		}

		private sealed class TypelessValueImpl : FieldValueImpl {
			object obj;

			//resolving constructor
			[SkipInstrumentation]
			internal TypelessValueImpl() {
			}

			[SkipInstrumentation]
			private TypelessValueImpl(TypelessValueImpl copyFrom) {
				this.obj = copyFrom.obj;
			}

			[SkipInstrumentation]
			internal override FieldValueImpl Clone() {
				return new TypelessValueImpl(this);
			}

			internal override object Value {
				get {
					return this.obj;
				}
				set {
					string asString = value as string;
					if (asString != null) {
						this.obj = string.Intern(asString);
					} else {
						this.obj = value;
					}
				}
			}
		}

		private sealed class ThingDefValueImpl : TypedValueImpl {
			//resolving constructor
			[SkipInstrumentation]
			internal ThingDefValueImpl(Type type)
				: base(type) {
			}

			[SkipInstrumentation]
			private ThingDefValueImpl(ThingDefValueImpl copyFrom)
				: base(copyFrom) {
			}

			[SkipInstrumentation]
			internal override FieldValueImpl Clone() {
				return new ThingDefValueImpl(this);
			}

			internal override object Value {
				get {
					return base.Value;
				}
				set {
					if (value != null) {
						ThingDef td = value as ThingDef;
						if (td == null) {
							if (ConvertTools.IsNumberType(value.GetType())) {
								int id = ConvertTools.ToInt32(value);
								if (typeof(AbstractItemDef).IsAssignableFrom(this.type)) {
									td = ThingDef.FindItemDef(id);
								} else {
									td = ThingDef.FindCharDef(id);
								}
								if (td == null) {
									throw new SEException("There is no Char/ItemDef with model " + id + "(0x" + id.ToString("x") + ")");
								}
								base.Value = td;
								return;
							}
						}
					}
					base.Value = value;
				}
			}
		}
	}
}