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
using System.Reflection;
using System.Text;
using SteamEngine.Common;

namespace SteamEngine.LScript {

	internal enum SpecialType {
		Normal,
		String,	//method with one argument of type string. is special because of calls without "" 
		//(like "somemethod(This is a sentence which is allowed because of spherescript's bad design)") 
		Params	//method with params argument on the end. has to create the array at some point...
	}

	//internal SecurityTypes {
	//	Callable, //function, method, field, constructor
	//	Variable, //Globals.Tag
	//	Tag, //using the tag keyword
	//	Timer //using AddTimer "keyword"
	//}

	internal class MemberDescriptor {
		internal MemberInfo info;
		internal SpecialType specialType;
		private Type[] parameterTypes;

		internal MemberDescriptor(MemberInfo info, SpecialType specialType) {
			this.info = info;
			this.specialType = specialType;
		}

		public override string ToString() {
			return this.info.ToString();
		}

		protected Type[] GetParameterTypes() {
			if (this.parameterTypes == null) {
				MethodBase methOrCtor = this.info as MethodBase;
				if (methOrCtor != null) {
					ParameterInfo[] pars = methOrCtor.GetParameters();
					this.parameterTypes = new Type[pars.Length];
					for (int i = 0, n = pars.Length; i < n; i++) {
						this.parameterTypes[i] = pars[i].ParameterType;
					}
					return this.parameterTypes;
				}
				FieldInfo field = this.info as FieldInfo;
				if (field != null) {
					this.parameterTypes = new Type[1] { field.FieldType };
					return this.parameterTypes;
				}
				throw new SEException(this.info + " of type " + this.info.GetType() + " and MemberType " + this.info.MemberType + " is not a field, nor method, nor constructor... wtf? This should not happen!");
			}
			return this.parameterTypes;
		}

		internal bool ParamTypesMatch(object[] results) {
			this.GetParameterTypes();
			int i, n;
			switch (this.specialType) {
				case SpecialType.Normal:
					for (i = 0, n = results.Length; i < n; i++) {
						if (!MemberResolver.IsCompatibleType(this.parameterTypes[i], results[i])) {
							return false;
						}
					}
					break;
				case SpecialType.Params:
					int nonArrayLength = this.parameterTypes.Length - 1;
					i = 0;
					for (; i < nonArrayLength; i++) {
						if (!MemberResolver.IsCompatibleType(this.parameterTypes[i], results[i])) {
							//Console.WriteLine("ParamTypesMatch returning false: current parameterType "+parameterTypes[i]
							//	+", current result is "+results[i]);
							return false;
						}
					}
					Type arrayType = this.parameterTypes[nonArrayLength].GetElementType();
					for (n = results.Length; i < n; i++) {
						if (!MemberResolver.IsCompatibleType(arrayType, results[i])) {
							//Console.WriteLine("ParamTypesMatch returning false: arrayType is "+arrayType
							//	+", current result is "+results[i]);
							return false;
						}
					}
					break;
			}//for SpecialType.String is it always true
			//Console.WriteLine("ParamTypesMatch returning true for: "+info+", having parametertypes "
			//	+Tools.ObjToString(parameterTypes)+" and results "+Tools.ObjToString(results));
			return true;
		}
	}


	//a "one-use" class that gets instantiated basically just for convenience (it could be entirely static)
	//designed to resolve use of particular method/field/constuctors (and their "params" and "string" versions
	//out of a relatively vague signature that is provided by lscript.
	//currently designed to be used by lazy_expression and addtimer
	internal class MemberResolver {
		//public static bool safeMode = false; //is set to on when LScript is used as commandline parser, and then calls TriggerKey.command on Globals.src

		private static Stack instances = new Stack();

		private IOpNodeHolder parent;
		private string name;
		private OpNode[] args;
		internal object[] results;
		private int line;
		private int column;
		private string filename;
		private ScriptVars vars;
		private bool inStack;

		internal static MemberResolver GetInstance(ScriptVars vars, IOpNodeHolder parent,
				string name, OpNode[] args, int line, int column, string filename) {

			MemberResolver instance;
			if (instances.Count > 0) {
				instance = (MemberResolver) instances.Pop();
			} else {
				instance = new MemberResolver();
			}
			instance.inStack = false;

			instance.vars = vars;
			instance.parent = parent;
			instance.name = name;
			instance.args = args;
			instance.results = null;
			instance.line = line;
			instance.column = column;
			instance.filename = filename;
			return instance;
		}

		//or should we call it Dispose? well, who cares :)
		internal static void ReturnInstance(MemberResolver instance) {
			instances.Push(instance);
			instance.inStack = true;
		}

		private MemberResolver() {
		}

		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		internal OpNode[] Args {
			get {
				return this.args;
			}
		}

		private void TryMakeMemberDescriptor(MemberInfo info, SpecialType specType,
				ref MemberDescriptor desc, ref List<MemberDescriptor> ambiguities) {
			this.RunArgs();
			MemberDescriptor newDesc = new MemberDescriptor(info, specType);
			if (newDesc.ParamTypesMatch(this.results)) {
				if (desc != null) {
					if (ambiguities == null) {
						ambiguities = new List<MemberDescriptor>();
						ambiguities.Add(desc);
					}
					ambiguities.Add(newDesc);
				} else {
					desc = newDesc;
				}
			}
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		internal bool Resolve(Type type, BindingFlags flags,
				MemberTypes memberTypes, out MemberDescriptor desc) {
			//resolve as any member (method or property(as getter/setter method) or constructor or field)

			Sanity.IfTrueThrow(this.inStack, "called Resolve on a disposed MemberResolver");

			bool nameMatches = false;
			desc = null;
			List<MemberDescriptor> ambiguities = null;
			int argsLength = this.args.Length;
			List<MemberInfo> namedWell = null;

			if (IsMethod(memberTypes)) {
				MethodInfo[] mis = type.GetMethods(BindingFlags.Public | flags);
				foreach (MethodInfo mi in mis) {//methods
					if (StringComparer.OrdinalIgnoreCase.Equals(this.name, mi.Name)) {
						if (namedWell == null) {
							namedWell = new List<MemberInfo>(1);
						}
						namedWell.Add(mi);
						nameMatches = true;
					}
				}
				if (namedWell != null) {
					foreach (MethodInfo mi in namedWell) {
						if (mi.GetParameters().Length == argsLength) {
							this.TryMakeMemberDescriptor(mi, SpecialType.Normal, ref desc, ref ambiguities);
						}
					}
					if (desc == null) {
						ParameterInfo[] pars;
						int parsLength;
						foreach (MethodInfo mi in namedWell) {
							pars = mi.GetParameters();
							parsLength = pars.Length;
							if ((argsLength >= (parsLength - 1)) && (parsLength > 0) && //(..., params sometype[] foo)
									(pars[parsLength - 1].GetCustomAttributes(typeof(ParamArrayAttribute), true).Length > 0)) {
								this.TryMakeMemberDescriptor(mi, SpecialType.Params, ref desc, ref ambiguities);
							}
						}
						if (desc == null) {
							foreach (MethodInfo mi in namedWell) {
								pars = mi.GetParameters();
								parsLength = pars.Length;
								if ((argsLength > 0) && (parsLength == 1) && (pars[0].ParameterType == typeof(string))) {
									this.TryMakeMemberDescriptor(mi, SpecialType.String, ref desc, ref ambiguities);
								}
							}
						}
					}
				}
			}

			if (IsProperty(memberTypes)) {
				if (desc == null) {//properties
					if (namedWell != null) {
						namedWell.Clear();
					}

					foreach (PropertyInfo pi in type.GetProperties(BindingFlags.Public | flags)) {
						if (StringComparer.OrdinalIgnoreCase.Equals(this.name, pi.Name)) {
							if (namedWell == null) {
								namedWell = new List<MemberInfo>(1);
							}
							namedWell.Add(pi);
							nameMatches = true;
						}
					}

					if ((namedWell != null) && (namedWell.Count > 0)) {
						foreach (PropertyInfo pi in namedWell) {
							MethodInfo setter = pi.GetSetMethod(false); //false for public only
							if (setter != null) {
								if (argsLength == 1) {
									this.TryMakeMemberDescriptor(setter, SpecialType.Normal, ref desc, ref ambiguities);
								}
							}
						}
						if (desc == null) {
							foreach (PropertyInfo pi in namedWell) {
								MethodInfo setter = pi.GetSetMethod(false); //false for public only
								if (setter != null) {
									if ((argsLength > 0) && (pi.PropertyType == typeof(string))) {
										this.TryMakeMemberDescriptor(setter, SpecialType.String, ref desc, ref ambiguities);
									}
								}
							}
							if (desc == null) {
								foreach (PropertyInfo pi in namedWell) {
									if (argsLength == 0) {
										MethodInfo getter = pi.GetGetMethod(false); //false for public only
										if (getter != null) {
											this.TryMakeMemberDescriptor(getter, SpecialType.Normal, ref desc, ref ambiguities);
										}
									}
								}
							}
						}
					}
				}
			}

			if (IsConstructor(memberTypes)) {
				if ((desc == null) && (StringComparer.OrdinalIgnoreCase.Equals(this.name, type.Name))) { //constructors
					nameMatches = true;
					ConstructorInfo[] cis = type.GetConstructors();
					foreach (ConstructorInfo ci in cis) {
						if (ci.GetParameters().Length == argsLength) {
							this.TryMakeMemberDescriptor(ci, SpecialType.Normal, ref desc, ref ambiguities);
						}
					}
					if (desc == null) {
						foreach (ConstructorInfo ci in cis) {
							ParameterInfo[] pars = ci.GetParameters();
							int parsLength = pars.Length;
							if ((argsLength >= (parsLength - 1)) && (parsLength > 0)) {//(..., params sometype[] foo)
								if (pars[pars.Length - 1].GetCustomAttributes(typeof(ParamArrayAttribute), true).Length > 0) {
									this.TryMakeMemberDescriptor(ci, SpecialType.Params, ref desc, ref ambiguities);
								}
							}
						}
						if (desc == null) {
							foreach (ConstructorInfo ci in cis) {
								ParameterInfo[] pars = ci.GetParameters();
								int parsLength = pars.Length;
								if ((parsLength == 1) && (pars[0].ParameterType == typeof(string))) {
									this.TryMakeMemberDescriptor(ci, SpecialType.String, ref desc, ref ambiguities);
								}
							}
						}
					}
				}
			}

			if (IsField(memberTypes)) {
				if (desc == null) {//fields
					if (namedWell != null) {
						namedWell.Clear();
					}
					foreach (FieldInfo fi in type.GetFields(BindingFlags.Public | flags)) {
						if (StringComparer.OrdinalIgnoreCase.Equals(this.name, fi.Name)) {
							if (namedWell == null) {
								namedWell = new List<MemberInfo>(1);
							}
							namedWell.Add(fi);
							nameMatches = true;
						}
					}
					if ((namedWell != null) && (namedWell.Count > 0)) {
						foreach (FieldInfo fi in namedWell) {
							if (argsLength < 2) {
								this.TryMakeMemberDescriptor(fi, SpecialType.Normal, ref desc, ref ambiguities);
							}
						}
						if (desc == null) {
							foreach (FieldInfo fi in namedWell) {
								if ((argsLength > 0) && (fi.FieldType == typeof(string))) {
									this.TryMakeMemberDescriptor(fi, SpecialType.String, ref desc, ref ambiguities);
								}
							}
						}
					}
				}
			}

			if (ambiguities != null) {
				MemberDescriptor[] resolvedAmbiguities;
				if (TryResolveAmbiguity(ambiguities, this.results, out resolvedAmbiguities)) {
					desc = resolvedAmbiguities[0];
					return true;
				}

				StringBuilder sb = new StringBuilder("Ambiguity detected when resolving expression as object method/property. There were following possibilities:");
				foreach (MemberDescriptor md in resolvedAmbiguities) {
					sb.Append(Environment.NewLine).Append(md);
				}
				throw new InterpreterException(sb.ToString(),
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName());
			}

			//Console.WriteLine("nameMatches: "+nameMatches+" of "+name+", flags:"+flags+", memberTypes:"+memberTypes+", type:"+type);

			return nameMatches;
		}

		//this could not at all be here, but it is quite convenient
		//internal void CheckCommandSecurity() {
		//	ScriptArgs sa = new ScriptArgs(vars.self, name, );
		//	if (Globals.srcConn.TryCancellableTrigger(Globals.src, TriggerKey.command, sa)) {
		//		throw new InterpreterException("You are not allowed to execute that.", 
		//			this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName());
		//	}
		//}

		internal void RunArgs() {
			if (this.results == null) {
				object oSelf = this.vars.self;
				this.results = new object[this.args.Length];
				this.vars.self = this.vars.defaultObject;
				try {
					for (int i = 0, n = this.args.Length; i < n; i++) {
						this.results[i] = this.args[i].Run(this.vars);
					}
				} finally {
					this.vars.self = oSelf;
				}
			}
		}

		internal LScriptHolder ParentScriptHolder {
			get {//"topobj" of the parent node
				LScriptHolder parentAsHolder = this.parent as LScriptHolder;
				if (parentAsHolder != null) {
					return parentAsHolder;
				}
				OpNode parentAsOpNode = this.parent as OpNode;
				if (parentAsOpNode != null) {
					return parentAsOpNode.ParentScriptHolder;
				}
				throw new SEException("The parent is nor OpNode nor LScriptHolder... this can not happen?!");
			}
		}

		internal static bool ReturnsString(OpNode node) {
			IKnownRetType nodeAsKnownType = node as IKnownRetType;
			if (nodeAsKnownType != null) {
				Type returnType = nodeAsKnownType.ReturnType;
				if ((returnType == typeof(string)) || returnType.IsSubclassOf(typeof(string))) {
					//we are sure that the nodeAsKnownType is always string (or null)
					return true;
				}
			}
			return false;
		}

		internal static bool IsCompatibleType(Type toType, object from) {
			if (from == null) {//in fact it may not be the case, but well, we dont know that...
				return true;
			}
			//Console.WriteLine("is {0} subtype of {1}?", from.GetType(), toType);
			if (toType.IsInstanceOfType(from)) {
				return true;
			}
			if ((ConvertTools.IsNumberType(toType)) && (ConvertTools.IsNumberType(from.GetType()))) {
				return true;
			}
			//Console.WriteLine("MemberResolver.IsCompatibleType false: from {0}({1}) to type {2}", from, from.GetType(), toType);
			return false;
		}

		internal static bool IsMethod(MemberInfo info) {
			return IsMethod(info.MemberType);
		}

		internal static bool IsMethod(MemberTypes memberType) {
			return (memberType & MemberTypes.Method) == MemberTypes.Method;
		}

		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		internal static bool IsProperty(MemberInfo info) {
			return IsProperty(info.MemberType);
		}

		internal static bool IsProperty(MemberTypes memberType) {
			return (memberType & MemberTypes.Property) == MemberTypes.Property;
		}

		internal static bool IsConstructor(MemberInfo info) {
			return IsConstructor(info.MemberType);
		}

		internal static bool IsConstructor(MemberTypes memberType) {
			return (memberType & MemberTypes.Constructor) == MemberTypes.Constructor;
		}

		internal static bool IsField(MemberInfo info) {
			return IsField(info.MemberType);
		}

		internal static bool IsField(MemberTypes memberType) {
			return (memberType & MemberTypes.Field) == MemberTypes.Field;
		}

		//ambiguity stuff starts here

		internal static bool TryResolveAmbiguity(List<MemberDescriptor> descriptors, object[] results, out MemberDescriptor[] bestMatches) {
			int n = descriptors.Count;
			AmbiguityResolver[] resolvers = new AmbiguityResolver[n];
			for (int i = 0; i < n; i++) {
				resolvers[i] = new AmbiguityResolver(descriptors[i]);
			}

			IList<AmbiguityResolver> bestGroup = CompareAmbiguities(resolvers, results);

			n = bestGroup.Count;
			bestMatches = new MemberDescriptor[n];
			for (int i = 0; i < n; i++) {
				bestMatches[i] = (MemberDescriptor) bestGroup[i].createdFrom;
			}
			return n == 1;
		}

		internal static bool TryResolveAmbiguity(List<MethodInfo> mis, object[] results, out MethodInfo[] bestMatches) {
			int n = mis.Count;
			AmbiguityResolver[] resolvers = new AmbiguityResolver[n];
			for (int i = 0; i < n; i++) {
				resolvers[i] = new AmbiguityResolver(mis[i]);
			}

			IList<AmbiguityResolver> bestGroup = CompareAmbiguities(resolvers, results);

			n = bestGroup.Count;
			bestMatches = new MethodInfo[n];
			for (int i = 0; i < n; i++) {
				bestMatches[i] = (MethodInfo) bestGroup[i].createdFrom;
			}
			return n == 1;
		}

		private static IList<AmbiguityResolver> CompareAmbiguities(AmbiguityResolver[] resolvers, object[] results) {
			List<List<AmbiguityResolver>> equalityGroups = new List<List<AmbiguityResolver>>();

			foreach (AmbiguityResolver current in resolvers) {
				current.EvalAmbiguity(results);
				bool addedIntoGroup = false;
				foreach (List<AmbiguityResolver> equalityGroup in equalityGroups) {
					if (HaveEqualDistances(current, equalityGroup[0])) {
						equalityGroup.Add(current);
						addedIntoGroup = true;
						break;
					}
				}
				if (!addedIntoGroup) {//create new group
					List<AmbiguityResolver> newGroup = new List<AmbiguityResolver>();
					newGroup.Add(current);
					equalityGroups.Add(newGroup);
				}
			}
			equalityGroups.Sort(CompareEqalityGroups);
			List<AmbiguityResolver> bestGroup = equalityGroups[equalityGroups.Count - 1];
			equalityGroups.RemoveAt(equalityGroups.Count - 1);

			foreach (List<AmbiguityResolver> equalityGroup in equalityGroups) {
				if (CompareEqalityGroups(bestGroup, equalityGroup) < 1) {
					return resolvers;//we failed, we return the whole set :\
				}
			}
			return bestGroup;
		}

		private static bool HaveEqualDistances(AmbiguityResolver a, AmbiguityResolver b) {
			int[] distancesA = a.distances;
			int[] distancesB = b.distances;
			for (int i = 0, n = distancesA.Length; i < n; i++) {
				if (distancesA[i] != distancesB[i]) {
					return false;
				}
			}
			return true;
		}

		private static int CompareEqalityGroups(List<AmbiguityResolver> groupA, List<AmbiguityResolver> groupB) {
			int[] distancesA = groupA[0].distances;
			int[] distancesB = groupB[0].distances;
			int retVal = 0;
			for (int i = 0, n = distancesA.Length; i < n; i++) {
				int distA = distancesA[i];
				int distB = distancesB[i];
				if (distA < distB) {
					if (retVal == 0) {
						retVal = 1;
					} else if (retVal < 0) {
						return 0;//considered equal
					}
				} else if (distB < distA) {
					if (retVal == 0) {
						retVal = -1;
					} else if (retVal > 0) {
						return 0;//considered equal
					}
				}
			}
			return retVal;
		}


		//internal static bool TryResolveAmbiguity(Type[,] paramTypes, Type[,] 

		private class AmbiguityResolver : MemberDescriptor {
			internal int[] distances;
			internal object createdFrom;

			internal AmbiguityResolver(MethodInfo mi)
				: base(mi, SpecialType.Normal) {
				this.createdFrom = mi;
			}

			internal AmbiguityResolver(MemberDescriptor descriptor)
				: base(descriptor.info, SpecialType.Normal) {
				this.createdFrom = descriptor;
			}

			internal void EvalAmbiguity(object[] results) {
				this.distances = new int[results.Length];
				Type[] paramTypes = this.GetParameterTypes();

				for (int i = 0, n = results.Length; i < n; i++) {
					this.distances[i] = GetHierarchyDistance(paramTypes[i], results[i]);
				}
			}

			private static int GetHierarchyDistance(Type toType, object from) {
				if (from == null) {
					return int.MaxValue;
				}
				Type fromType = from.GetType();
				int dist = GetSubclassDistance(toType, fromType);
				if (dist == 0) {
					return dist;//types are equal, wow! :)
				}
				//now let's look at numbers
				if (ConvertTools.IsNumberType(toType) && ConvertTools.IsNumberType(fromType)) {
					if ((ConvertTools.IsFloatType(toType) && ConvertTools.IsFloatType(fromType)) ||
						(ConvertTools.IsSignedIntegerType(toType) && ConvertTools.IsSignedIntegerType(fromType)) ||
						(ConvertTools.IsUnsignedIntegerType(toType) && ConvertTools.IsUnsignedIntegerType(fromType))) {
						return 1;
					}
					if (ConvertTools.IsIntegerType(toType) && ConvertTools.IsIntegerType(fromType)) {
						return 2;
					}
					if (ConvertTools.IsFloatType(toType) && ConvertTools.IsIntegerType(fromType)) { //casting from int to double etc.
						return 3;
					}
					return 4;
				}

				int ifaceDist = GetInterfaceDistance(toType, fromType);

				return 1000 + Math.Min(dist, ifaceDist);
				//1000 + because the number distances are primary
			}

			private static int GetSubclassDistance(Type toType, Type fromType) {
				int distance = 0;
				do {
					if (toType.Equals(fromType))
						return distance;
					fromType = fromType.BaseType;
					distance++;
				} while (fromType != null);
				return int.MaxValue;
			}

			private static int GetInterfaceDistance(Type toType, Type fromType) {
				if (!toType.IsInterface) {
					return int.MaxValue;//totype is no interface
				}
				List<Type> fromTypeBaseTypes = new List<Type>();

				do {
					fromTypeBaseTypes.Add(fromType);
					fromType = fromType.BaseType;
				} while (fromType != null);

				for (int i = fromTypeBaseTypes.Count - 1; i >= 0; i--) {
					Type fromTypeBase = fromTypeBaseTypes[i];
					Type[] ifaces = fromTypeBase.GetInterfaces();
					if (Array.IndexOf(ifaces, toType) > -1) { //it contains the iface
						return i + GetHighestIfaceDistanceRecursive(ifaces, toType);
					}
				}
				return int.MaxValue;
			}

			private static int GetHighestIfaceDistanceRecursive(Type[] ifaces, Type toType) {
				int highestDist = 0;
				foreach (Type iface in ifaces) {
					int newDist = GetHighestIfaceDistanceRecursive(iface.GetInterfaces(), toType);
					if (newDist > highestDist) {
						highestDist = newDist;
					}
				}
				if (Array.IndexOf(ifaces, toType) > -1) {
					highestDist++;
				}
				return highestDist;
			}

			////Console.WriteLine("is {0} subtype of {1}?", from.GetType(), toType);
			//if (toType.IsInstanceOfType(from)) {
			//    return true;
			//} else if ((TagMath.IsNumberType(toType))&&(TagMath.IsNumberType(from.GetType()))) {
			//    return true;
			//}
			////Console.WriteLine("MemberResolver.IsCompatibleType false: from {0}({1}) to type {2}", from, from.GetType(), toType);
			//return false;

		}


	}
}































