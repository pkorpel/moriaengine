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
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
//using System.Windows.Forms;
#if !MONO
#endif

namespace SteamEngine.Common {
	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	public sealed class RegisterWithRunTestsAttribute : Attribute {
	}

	//[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	//public class ExpectExceptionAttribute : Attribute {
	//	Type type;
	//	public ExpectExceptionAttribute(Type type) {
	//		this.type = type;
	//	}
	//}

	internal class TestMethod {
		private MethodInfo mi;
		//Type exceptionType = null;

		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		internal MethodInfo Method {
			get {
				return this.mi;
			}
		}

		internal TestMethod(MethodInfo mi) {
			this.mi = mi;
			//ExpectExceptionAttribute[] attrs = (ExpectExceptionAttribute[])
			//	mi.GetCustomAttributes(typeof(ExpectExceptionAttribute), true);
			//	
			//if (attrs.Length > 0) {
			//	exceptionType
			//}
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal bool Invoke() {
			Console.WriteLine("TestSuite : Running test '" + this.mi + "'.");
			try {
				this.mi.Invoke(null, null);
				Console.WriteLine("TestSuite : Test passed.");
				return true;
			} catch (Exception e) {
				Logger.WriteCritical("TestSuite : Test failed!", e);
				return false;
			}
		}
	}

	/**
		TestDelegates (like PacketSender.EncodingTests) can just throw SanityCheckException (with a message)
		if a test fails. They should do Logger.Show("TestSuite", "Testing <whatever whatever>") before each test,
		and if one failed, "The test of <whatever whatever> failed. <error message, reason, more information, etc>".
		
	*/
	public static class TestSuite {
		//public static TestSuite This { get { return instance; } }
		private static ArrayList tests = new ArrayList();	//It's an ArrayList<TestMethod>

		public static void AddTest(MethodInfo mi) {
			tests.Add(new TestMethod(mi));
		}

		//when recompiling, we need to get rid of non-core methods. in fact we can only leave the scripted ones, because the core hasnt changed...
		public static void ForgetAll() {
			tests.Clear();
		}

		//throws an SEBugException if testing fails.
		public static void RunAllTests() {
			bool failure = false;
			Console.WriteLine("TestSuite : Starting running tests.");
			//Logger.Show("TestSuite","Running Tests.");
			foreach (TestMethod test in tests) {
				if (!test.Invoke()) {
					failure = true;
				}
			}
			if (failure) {
				//Logger.Show("TestSuite", "Tests failed!");
				Console.WriteLine("TestSuite : One or more tests failed!");
			} else {
				Console.WriteLine("TestSuite : All tests passed.");
				//Logger.Show("TestSuite","All tests passed");
			}
		}
	}
}
