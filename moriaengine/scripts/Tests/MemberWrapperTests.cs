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

using System.Reflection;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	public class MemberWrapperTests {
		//temporary for direct launching while writing the tests

		[RegisterWithRunTests]
		[SteamFunction]
		public static void RunMWTests() {
			ConstructorInfo constructor = MemberWrapper.GetWrapperFor(typeof(MemberWrapperTests).GetConstructors()[0]);
			FieldInfo intFi = MemberWrapper.GetWrapperFor(typeof(MemberWrapperTests).GetField("intField"));
			FieldInfo objFi = MemberWrapper.GetWrapperFor(typeof(MemberWrapperTests).GetField("objField"));
			MethodInfo instanceMi = MemberWrapper.GetWrapperFor(typeof(MemberWrapperTests).GetMethod("TestInstanceMethod"));
			MethodInfo staticMi = MemberWrapper.GetWrapperFor(typeof(MemberWrapperTests).GetMethod("TestStaticMethod"));

			MemberWrapperTests instance = (MemberWrapperTests) constructor.Invoke(
				new object[] { 5, "test" });
			Sanity.IfTrueThrow(instance.intField != 5, "while testing constructor invoking (intField = " + instance.intField + ")");
			Sanity.IfTrueThrow(!"test".Equals(instance.objField), "while testing constructor invoking (objField = " + instance.objField + ")");

			int retVal = (int) instanceMi.Invoke(instance, new object[] { 5 });
			Sanity.IfTrueThrow(retVal != 10, "while testing instance method invoking");

			retVal = (int) instanceMi.Invoke(instance, new object[] { 5 });
			Sanity.IfTrueThrow(retVal != 10, "while testing static method invoking");


			intFi.SetValue(instance, 321);
			retVal = (int) intFi.GetValue(instance);
			Sanity.IfTrueThrow(retVal != 321, "while testing valuetype field invoking");

			objFi.SetValue(instance, "foobar");
			object o = objFi.GetValue(instance);
			Sanity.IfTrueThrow(!"foobar".Equals(o), "while testing referencetype field invoking");
		}

		public int intField;
		public object objField;

		public MemberWrapperTests(int arg, string str) {
			this.intField = arg;
			this.objField = str;
		}

		public static int TestStaticMethod(int i) {
			return 2 * i;
		}
		public int TestInstanceMethod(int i) {
			return 2 * i;
		}
	}
}