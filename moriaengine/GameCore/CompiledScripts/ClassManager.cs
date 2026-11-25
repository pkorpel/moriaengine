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
using System.Reflection;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	public delegate void RegisterTGDeleg(TriggerGroup tg);

	public delegate bool SupplyType(Type type);

	public delegate bool SupplyDecoratedType<T>(Type type, T attr) where T : Attribute;

	public delegate void SupplyInstance<T>(T instance);

	public static class ClassManager {
		[SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
		private static readonly Dictionary<string, Type> allTypesbyName = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

		private static readonly Dictionary<string, RegisterTGDeleg> registerTGmethods = new Dictionary<string, RegisterTGDeleg>(StringComparer.OrdinalIgnoreCase);

		private static List<SupplyDecoratedTypeBase> supplyDecoratedTypesDelegs = new List<SupplyDecoratedTypeBase>();

		private static List<SupplySubclassInstanceBase> supplySubclassInstanceDelegs = InitDecoratedTypesDelegsList();

		private static List<TypeDelegPair> supplySubclassDelegs = new List<TypeDelegPair>();

		static List<SupplySubclassInstanceBase> InitDecoratedTypesDelegsList() {
			//can't be in GeneratedCodeUtil's Bootstrap, cos that's too late.
			List<SupplySubclassInstanceBase> list = new List<SupplySubclassInstanceBase>();
			list.Add(new SupplySubclassInstanceTuple<ISteamCSCodeGenerator>(GeneratedCodeUtil.RegisterGenerator, true, true)); //ClassManager.RegisterSupplySubclassInstances<ISteamCSCodeGenerator>(GeneratedCodeUtil.RegisterGenerator, true, true);
			return list;			
		}

		public static ICollection<Type> AllManagedTypes {
			get {
				return allTypesbyName.Values;
			}
		} 

		private static Assembly commonAssembly = typeof(LogStr).Assembly;
		public static Assembly CommonAssembly {
			get {
				return commonAssembly;
			}
		}

		private static Assembly coreAssembly = typeof(ClassManager).Assembly;
		public static Assembly CoreAssembly {
			get {
				return coreAssembly;
			}
		}

		public static Assembly ScriptsAssembly {
			get {
				return CompilerInvoker.compiledScripts.assembly;
			}
		}

		public static Assembly GeneratedAssembly {
			get {
				return GeneratedCodeUtil.generatedAssembly;
			}
		}

		//removes all non-core references
		internal static void ForgetScripts() {
			Type[] types = new Type[allTypesbyName.Count];
			allTypesbyName.Values.CopyTo(types, 0);
			allTypesbyName.Clear();
			foreach (Type type in types) {
				if (coreAssembly == type.Assembly) {
					allTypesbyName[type.Name] = type;
				}
			}

			List<SupplyDecoratedTypeBase> tempDecoTypes = new List<SupplyDecoratedTypeBase>(supplyDecoratedTypesDelegs.Count);
			foreach (SupplyDecoratedTypeBase entry in supplyDecoratedTypesDelegs) {
				if (!IsTypeFromScripts(entry.type) && !IsTypeFromScripts(entry.TargetClass)) {
					tempDecoTypes.Add(entry);
				}
			}
			supplyDecoratedTypesDelegs = tempDecoTypes;

			List<SupplySubclassInstanceBase> tempInstances = new List<SupplySubclassInstanceBase>(supplySubclassInstanceDelegs.Count);
			foreach (SupplySubclassInstanceBase entry in supplySubclassInstanceDelegs) {
				if (!IsTypeFromScripts(entry.type) && !IsTypeFromScripts(entry.TargetClass)) {
					tempInstances.Add(entry);
				}
			}
			supplySubclassInstanceDelegs = tempInstances;

			List<TypeDelegPair> tempSubclasses = new List<TypeDelegPair>(supplySubclassDelegs.Count);
			foreach (TypeDelegPair entry in supplySubclassDelegs) {
				if (!IsTypeFromScripts(entry.type)) {
					tempSubclasses.Add(entry);
				}
			}
			supplySubclassDelegs = tempSubclasses;

			//allTypes = new Type[allTypesbyName.Count];
			//allTypesbyName.Values.CopyTo(allTypes, 0);
		}

		private static bool IsTypeFromScripts(Type type) {
			return (type.Assembly == GeneratedAssembly) ||
				(type.Assembly == ScriptsAssembly);
		}

		public static Type GetType(string name) {
			Type type;
			allTypesbyName.TryGetValue(name, out type);
			return type;
		}

		internal static RegisterTGDeleg GetRegisterTGmethod(string name) {
			RegisterTGDeleg mi;
			registerTGmethods.TryGetValue(name, out mi);
			return mi;
		}

		[SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
		public static void RegisterSupplyDecoratedClasses<T>(SupplyDecoratedType<T> deleg, bool inherited) where T : Attribute {
			supplyDecoratedTypesDelegs.Add(new SupplyDecoratedTypeBaseTuple<T>(deleg, inherited));
		}

		[SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
		public static void RegisterSupplySubclasses<T>(SupplyType deleg) {
			supplySubclassDelegs.Add(new TypeDelegPair(typeof(T), deleg));
		}

		[SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
		public static void RegisterSupplySubclassInstances<T>(SupplyInstance<T> deleg, bool sealedOnly, bool throwIfNoCtor) {
			supplySubclassInstanceDelegs.Add(new SupplySubclassInstanceTuple<T>(deleg, sealedOnly, throwIfNoCtor));
		}

		private class TypeDelegPair {
			internal readonly Type type;
			internal readonly SupplyType deleg;

			internal TypeDelegPair(Type type, SupplyType deleg) {
				this.type = type;
				this.deleg = deleg;
			}
		}

		private abstract class SupplySubclassInstanceBase {
			internal readonly bool sealedOnly;
			internal readonly bool throwIfNoCtor;
			internal readonly Type type;

			internal SupplySubclassInstanceBase(bool sealedOnly, bool throwIfNoCtor, Type type) {
				this.sealedOnly = sealedOnly;
				this.throwIfNoCtor = throwIfNoCtor;
				this.type = type;
			}

			internal abstract bool InvokeDeleg(object instance);
			internal abstract Type TargetClass { get; }
		}

		private class SupplySubclassInstanceTuple<T> : SupplySubclassInstanceBase {
			SupplyInstance<T> deleg;

			internal SupplySubclassInstanceTuple(SupplyInstance<T> deleg, bool sealedOnly, bool throwIfNoCtor)
				: base(sealedOnly, throwIfNoCtor, typeof(T)) {
				this.deleg = deleg;
			}

			internal override bool InvokeDeleg(object instance) {
				if (this.deleg != null) {
					this.deleg((T) instance);
					return true;
				}
				return false;
			}

			internal override Type TargetClass {
				get
				{
					if (this.deleg == null) {
						return typeof(void);
					}
					return this.deleg.Method.DeclaringType;
				}
			}
		}

		private abstract class SupplyDecoratedTypeBase {
			internal readonly bool inherited;
			internal readonly Type type;

			internal SupplyDecoratedTypeBase(bool inherited, Type type) {
				this.inherited = inherited;
				this.type = type;
			}

			internal abstract bool InvokeDeleg(Type suppliedType, Attribute attr);
			internal abstract Type TargetClass { get; }
		}

		private class SupplyDecoratedTypeBaseTuple<T> : SupplyDecoratedTypeBase where T : Attribute {
			SupplyDecoratedType<T> deleg;

			internal SupplyDecoratedTypeBaseTuple(SupplyDecoratedType<T> deleg, bool inherited)
				: base(inherited, typeof(T)) {
				this.deleg = deleg;
			}

			internal override bool InvokeDeleg(Type suppliedType, Attribute attr) {
				return this.deleg(suppliedType, (T) attr);
			}

			internal override Type TargetClass {
				get {
					return this.deleg.Method.DeclaringType;
				}
			}
		}

		internal static bool InitClasses(Assembly assembly) {
			Type[] types = assembly.GetTypes();
			if (!InitClasses(types, assembly.GetName().Name)) {
				Logger.WriteCritical("Scripts invalid.");
				return false;
			}
			//allTypes = new Type[allTypesbyName.Count];
			//allTypesbyName.Values.CopyTo(allTypes, 0);
			return true;
		}

		private static bool InitClasses(Type[] types, string assemblyName) {
			bool success = true;

			//first call the Bootstrap methods (if present)
			for (int i = 0; i < types.Length; i++) {
				MethodInfo m = types[i].GetMethod("Bootstrap", BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);
				if (m != null) {
					m.Invoke(null, null);
				}
			}
			//then Initialize the classes as needed
			for (int i = 0; i < types.Length; i++) {
				try {
					InitClass(types[i]);
				} catch (FatalException) {
					throw;
				} catch (Exception e) {
					Logger.WriteError(assemblyName, Tools.TypeToString(types[i]), e);
					success = false;
				}
			}
			return success;
		}

		private static void InitClass(Type type) {
			allTypesbyName[type.Name] = type;

			foreach (MethodInfo meth in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)) {
				if (Attribute.IsDefined(meth, typeof(RegisterWithRunTestsAttribute))) {
					TestSuite.AddTest(meth);
				}
				if (Attribute.IsDefined(meth, typeof(SteamFunctionAttribute))) {
					CompiledScriptHolderGenerator.AddCompiledSHType(meth);
				}
			}

			if (type.IsSubclassOf(typeof(TagHolder))) {
				MethodInfo rtgmi = type.GetMethod("RegisterTriggerGroup",
					BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(TriggerGroup) }, null);
				if (rtgmi != null) {
					RegisterTGDeleg rtgd = (RegisterTGDeleg) Delegate.CreateDelegate(typeof(RegisterTGDeleg), rtgmi);
					registerTGmethods[type.Name] = rtgd;
				}
			}

			//initialise all language classes
			if (type.IsSubclassOf(typeof(CompiledLocStringCollection<>))) {
				MethodInfo locInitMethod = typeof(Loc<>).MakeGenericType(type).GetMethod("Init", BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);
				locInitMethod.Invoke(null, null);
			}

			bool match = false;
			foreach (TypeDelegPair entry in supplySubclassDelegs) {
				if (entry.type.IsAssignableFrom(type)) {
					match = match || entry.deleg(type);
				}
			}

			foreach (SupplyDecoratedTypeBase entry in supplyDecoratedTypesDelegs) {
				object[] attribs = type.GetCustomAttributes(entry.type, entry.inherited);
				if (attribs.Length > 0) {
					match = match || entry.InvokeDeleg(type, (Attribute) attribs[0]);
					if (attribs.Length > 1) {
						Logger.WriteWarning("Class " + type + " has more than one " + entry.type + " Attribute defined. What to do...?");
					}
				}
			}

			if (!match) {
				object instance = null;
				foreach (SupplySubclassInstanceBase entry in supplySubclassInstanceDelegs) {
					if (entry.type.IsAssignableFrom(type)) {
						if ((type.IsAbstract) ||
							((entry.sealedOnly) && (!type.IsSealed))) {
							continue;
						}
						if (instance == null) {
							ConstructorInfo ci = type.GetConstructor(Type.EmptyTypes);
							if (ci != null) {
								instance = ci.Invoke(null);
							} else if (entry.throwIfNoCtor) {
								throw new SEException("No proper constructor.");
							}
						}
						if (instance != null) {
							if (!entry.InvokeDeleg(instance)) {//if there's no loading method, just the constructor, 
								//and if it's an abstractscript, it needs to be registered. Can't be done in AbstractScript constructor itself cos that's too soon.
								//Or it would need some tweaks which I'm not in the mood to do :)
								AbstractScript script = instance as AbstractScript;
								if (script != null) {
									script.Register(); //can't be run inside of the constructor...
								}
							}
						}
					}
				}
			}

			//if (typeof(IImportable).IsAssignableFrom(type)) {
			//    ExportImport.RegisterExportClass(type);
			//}

			//moved to InitClasses method - see above
			/*
			MethodInfo m = type.GetMethod("Bootstrap", BindingFlags.Static|BindingFlags.Public|BindingFlags.DeclaredOnly ); 
			if (m!=null) {
				m.Invoke(null, null);
			}
			*/
		}

		//called by Main on the end of startup/recompile process
		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal static void InitScripts() {
			Type[] types = commonAssembly.GetTypes();
			if (!InitClasses(types, commonAssembly.GetName().Name)) {
				throw new SEException("Common library invalid.");
			}

			Logger.WriteDebug("Initializing Scripts.");
			foreach (Type type in allTypesbyName.Values) {
				Assembly a = type.Assembly;
				if ((coreAssembly != a) && (commonAssembly != a)) {
					MethodInfo m = type.GetMethod("Init", BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);
					if (m != null) {
						try {
							m.Invoke(null, null);
						} catch (FatalException) {
							throw;
						} catch (Exception e) {
							Logger.WriteError(type.Name, m.Name, e);
						}
					}
				}
			}
			Logger.WriteDebug("Initializing Scripts done.");
		}
	}
}
