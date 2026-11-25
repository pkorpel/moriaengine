using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class HasSavedMembersAttribute : Attribute {
		//no params
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class SavedMemberAttribute : Attribute {
		/// <summary>The description will be used in settings dialog</summary>
		private string description;
		private string category;

		public string Description {
			get {
				return this.description;
			}
		}

		public string Category {
			get {
				return this.category;
			}
		}
		//no params constructor as default
		public SavedMemberAttribute() {
		}

		/// <summary>
		/// Allows us to _shortly_ describe the purpose of the member...
		/// this info will be used for displaying it in 'settings' dialog. 
		/// We can also specify the settings category in which this member will be placed.
		/// </summary>
		public SavedMemberAttribute(string desc, string cat) {
			this.description = desc;
			this.category = cat;
		}
	}

	//[HasSavedMembers]
	public static class StaticMemberSaver {
		public class E_StaticMemberSaver_Global : CompiledTriggerGroup {
			public void On_BeforeSave(Globals ignored) {
				SaveMembers();
			}

			public void On_AfterSave(Globals instance) {
				instance.RemoveTag(tkSavedStaticMembersTable);
			}
		}

		//[SavedMember]
		//private static string testField = "teeeeeeeeeeeeeeststriiiiing";

		private static List<MemberInfo> registeredMembers = new List<MemberInfo>();
		private static TagKey tkSavedStaticMembersTable = TagKey.Acquire("SavedStaticMembersTable");

		private static void LoadMembers() {
			Hashtable table = Globals.Instance.GetTag(tkSavedStaticMembersTable) as Hashtable;
			Globals.Instance.RemoveTag(tkSavedStaticMembersTable);

			if (table != null) {
				foreach (DictionaryEntry entry in table) {
					try {
						string[] splitKey = ((string) entry.Key).Split(':');
						Type type = ClassManager.GetType(splitKey[0]);
						if (type != null) {
							MemberInfo[] mis = type.GetMember(splitKey[1], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
							if (mis.Length > 0) {
								MemberInfo mi = mis[0];
								Type miReturnType;
								if (IsPropertyOrField(mi, out miReturnType)) {
									object value;
									if (ConvertTools.TryConvertTo(miReturnType, entry.Value, out value)) {
										SetValueToMember(mi, value);
									} else {
										Logger.WriteWarning("Cannot convert loaded type '" + Tools.TypeToString(entry.Value.GetType()) + "' to type '" +
										miReturnType + "', while loading the static member " + LogStr.Ident(entry.Key));
									}
								} else {
									Logger.WriteWarning("The member '" + LogStr.Ident(entry.Key) + "' is not a field or property, but is being loaded as such...ignoring");
								}
							} else {
								Logger.WriteWarning("Unknown class member '" + LogStr.Ident(splitKey[1]) + "' while trying to load static member '" + LogStr.Ident(entry.Key) + "'");
							}

						} else {
							Logger.WriteWarning("Unknown class '" + LogStr.Ident(splitKey[0]) + "' while trying to load static member '" + LogStr.Ident(entry.Key) + "'");
						}
					} catch (FatalException) {
						throw;
					} catch (Exception e) {
						Logger.WriteError("Error while loading static members", e);
					}
				}
			}
		}

		private static bool IsPropertyOrField(MemberInfo mi, out Type returnType) {
			if (mi.MemberType == MemberTypes.Property) {
				returnType = ((PropertyInfo) mi).PropertyType;
				return true;
			}
			if (mi.MemberType == MemberTypes.Field) {
				returnType = ((FieldInfo) mi).FieldType;
				return true;
			}
			returnType = null;
			return false;
		}

		public static void SetValueToMember(MemberInfo mi, object value) {
			if (mi.MemberType == MemberTypes.Property) {
				((PropertyInfo) mi).SetValue(null, value, null);
			} else if (mi.MemberType == MemberTypes.Field) {
				((FieldInfo) mi).SetValue(null, value);
			}
		}

		private static void SaveMembers() {
			Hashtable table = new Hashtable();
			foreach (MemberInfo mi in registeredMembers) {
				object obj;
				if (mi.MemberType == MemberTypes.Property) {
					obj = ((PropertyInfo) mi).GetValue(null, null);
				} else {
					obj = ((FieldInfo) mi).GetValue(null);
				}
				string key = mi.ReflectedType.Name + ":" + mi.Name;
				table[key] = obj;
			}
			Globals.Instance.SetTag(tkSavedStaticMembersTable, table);
		}

		//called by Classmanager at the end of starting process, i.e. when everything is loaded
		public static void Init() {
			//Console.WriteLine("StaticMemberSaver init");

			LoadMembers();

			registeredMembers.Clear();
			foreach (Type type in ClassManager.AllManagedTypes) {
				if (Attribute.IsDefined(type, typeof(HasSavedMembersAttribute))) {
					foreach (MemberInfo mi in type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)) {
						TryRegisterMember(mi);
					}
				}
			}
		}

		private static void TryRegisterMember(MemberInfo mi) {
			if (mi.IsDefined(typeof(SavedMemberAttribute), false)) {
				if (mi.MemberType == MemberTypes.Property) {
					PropertyInfo pi = (PropertyInfo) mi;
					if (!pi.CanRead || !pi.CanWrite) {
						Logger.WriteWarning(pi.DeclaringType.Name, pi.Name, "The property must be both writable and readable to be saved and loaded.");
						return;
					}
				}
				registeredMembers.Add(mi);
			}
		}

		/// <summary>
		/// Search through the members of the given class and find those marked with 
		/// 'SaveableData' attribute - we will use them as a settings dialog subsection
		/// </summary>
		public static List<MemberInfo> GetSaveableDataFromClass(Type type) {
			List<MemberInfo> retList = new List<MemberInfo>();
			foreach (MemberInfo mi in type.GetMembers()) {
				if (mi.IsDefined(typeof(SaveableDataAttribute), false)) {
					retList.Add(mi);
				}
			}
			return retList;
		}

		/// <summary>
		/// Get the SavedMember resp. SaveableData attribute and fetch the value of its 'description'
		/// field. This will be displayed in the 'settings' dialog.
		/// </summary>
		public static string GetSettingDescFor(MemberInfo mi) {
			string retDesc = "";
			//first try to find the SavedMember attribute
			object[] attrs = mi.GetCustomAttributes(typeof(SavedMemberAttribute), false);
			if (attrs.Length > 0) {
				//we know that there will be exactly one SavedMember attribute
				retDesc = ((SavedMemberAttribute) attrs[0]).Description;
			}
			if (retDesc.Equals("")) {
				//still nothing found, try to find the SaveableData attribute...
				attrs = mi.GetCustomAttributes(typeof(SaveableDataAttribute), false);
				if (attrs.Length > 0) {
					//we know that there will be exactly one SavedMember attribute
					retDesc = ((SaveableDataAttribute) attrs[0]).Description;
				}
			}

			//there will always be something returned because we are calling this method only for those
			//MemberInfos that have been chosen to the 'settings dialog'
			return retDesc;
		}
	}
}