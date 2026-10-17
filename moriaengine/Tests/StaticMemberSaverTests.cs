using System.Collections;
using System.Reflection;
using SteamEngine.CompiledScripts;
using Xunit;

namespace SteamEngine.Tests {
	/// <summary>
	/// Issue #358 (split off #64 item 4): StaticMemberSaver.TryRegisterMember guarded
	/// properties for read/write-ability before registering them, but had no equivalent
	/// guard for fields. A readonly static field marked [SavedMember]
	/// (CraftmenuContents.mainCategories) got registered, saved fine (FieldInfo.GetValue
	/// works on a readonly field), then threw FieldAccessException on every subsequent
	/// boot's load (FieldInfo.SetValue on a readonly field) - logged every boot as
	/// "Error while loading static members". Pure reflection logic, no engine boot needed.
	/// </summary>
	public class StaticMemberSaverTests {
		private static bool TryRegisterAndCheckRegistered(MemberInfo mi) {
			FieldInfo registeredMembersField = typeof(StaticMemberSaver)
				.GetField("registeredMembers", BindingFlags.NonPublic | BindingFlags.Static);
			var registeredMembers = (IList) registeredMembersField.GetValue(null);
			registeredMembers.Clear();

			MethodInfo tryRegisterMember = typeof(StaticMemberSaver)
				.GetMethod("TryRegisterMember", BindingFlags.NonPublic | BindingFlags.Static);
			tryRegisterMember.Invoke(null, new object[] { mi });

			return registeredMembers.Count > 0;
		}

		[Fact]
		public void ReadonlyFieldMarkedSavedMemberIsNotRegistered() {
			FieldInfo field = typeof(CraftmenuContents)
				.GetField("mainCategories", BindingFlags.NonPublic | BindingFlags.Static);
			Assert.True(field.IsInitOnly); // sanity: still readonly in production code

			Assert.False(TryRegisterAndCheckRegistered(field));
		}
	}
}
