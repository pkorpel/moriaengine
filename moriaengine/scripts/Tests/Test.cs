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
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	/// <summary>Testovaci trida pro dialog nastaveni, bude mit sadu saved memberu.</summary>
	[HasSavedMembers]
#pragma warning disable 0414 //disable the boring "private field never used warning"
	public static class TESTSettClass {
		[SavedMember("attr1", "Category1")]
		private static object att1 = "pepicek";
		[SavedMember("attr2", "Category1")]
		private static string att2 = "jiricek";
		[SavedMember("attr3", "Category1")]
		private static int att3 = 5;
		[SavedMember("attr4", "Category2")]
		private static DateTime att4 = new DateTime();

		[SavedMember("attr5", "Categotry3")]
		private static TestSavedClass1 att5 = new TestSavedClass1();
		[SavedMember("attr6", "Categotry3")]
		private static string att6 = "frantik";
		[SavedMember("attr7", "Categotry4")]
		private static string att7 = "zmolžík";
		[SavedMember("attr8", "Categotry4")]
		private static TimeSpan att8 = new TimeSpan();
		[SavedMember("attr10", "Categotry5")]
		private static TestSavedClass2 att10 = new TestSavedClass2();
		[SavedMember("attr11", "Categotry6")]
		private static TestSavedClass1 att11 = new TestSavedClass1();
		[SavedMember("attr12", "Categotry5")]
		private static TestSavedClass2 att12 = new TestSavedClass2();
		[SavedMember("attr13", "Categotry6")]
		private static TestSavedClass1 att13 = new TestSavedClass1();
		[SavedMember("attr14", "Categotry7")]
		private static TestSavedClass2 att14 = new TestSavedClass2();
		[SavedMember("attr15", "Categotry7")]
		private static TestSavedClass1 att15 = new TestSavedClass1();
		[SavedMember("attr16", "Categotry8")]
		private static TestSavedClass2 att16 = new TestSavedClass2();
		[SavedMember("attr17", "Categotry8")]
		private static TestSavedClass1 att17 = new TestSavedClass1();
		[SavedMember("attr18", "Categotry9")]
		private static TestSavedClass2 att18 = new TestSavedClass2();
		[SavedMember("attr19", "Categotry9")]
		private static TestSavedClass1 att19 = new TestSavedClass1();
		[SavedMember("attr20", "Categotry7")]
		private static TestSavedClass1 att20 = new TestSavedClass1();
		[SavedMember("attr21", "Categotry8")]
		private static TestSavedClass2 att21 = new TestSavedClass2();
		[SavedMember("attr22", "Categotry8")]
		private static TestSavedClass1 att22 = new TestSavedClass1();
		[SavedMember("attr25", "Categotry9")]
		private static string att25 = "tencobymelbytprvninadasistarnce";
		[SavedMember("attr23", "Categotry9")]
		private static TestSavedClass2 att23 = new TestSavedClass2();
		[SavedMember("attr24", "Categotry9")]
		private static TestSavedClass1 att24 = new TestSavedClass1();
	}

	[SaveableClass]
	public class TestSavedClass1 {
		[LoadingInitializer]
		public TestSavedClass1() {
		}

		[SaveableData("inattr1")]
		public string savedatt1 = "Maruska";
		[SaveableData("inattr2")]
		public bool savedatt2 = false;
		[SaveableData("inattr3")]
		public int savedatt3 = 3;
		[SaveableData("inattr4")]
		public bool savedatt4 = false;
		[SaveableData("inattr5")]
		public TimeSpan savedatt5 = new TimeSpan();
	}

	[SaveableClass]
	public class TestSavedClass2 {
		[LoadingInitializer]
		public TestSavedClass2() {
		}

		[SaveableData("inattr6")]
		public string savedatt6 = "Jenicek a Marenka";
		[SaveableData("inattr7")]
		public bool savedatt7 = true;
		[SaveableData("inattr8")]
		public int savedatt8 = 18;
		[SaveableData("inattr9")]
		public DateTime savedatt9 = new DateTime();
		[SaveableData("inattr10")]
		public TimeSpan savedatt10 = new TimeSpan();
	}

	//If something looks difficult, make suggestions for ways to make it easier!
	public static class TestScript {
		public static void Moo(string asdf) {
			char o;
			for (int a = 0; a < asdf.Length; a++) {
				o = asdf[a];
				Console.Write(o);
			}
		}

		/// <summary>Documentation in scripts</summary>
		/// <remarks>Documentation attributes are allowed in scripts.</remarks>
		[SteamFunction]
		public static TagHolder testInvoking(TagHolder self, string accname) {
			Console.WriteLine("I am called on " + self + " with '" + accname + "'");
			return new TagHolder();
		}
		[SteamFunction]
		public static void TestFuncEmpty(TagHolder self) {
			Moo(self.Name);
		}
		[SteamFunction]
		public static void TestFunc2Args(TagHolder self, string a, string b) {
			Moo(self.Name);
		}
		[SteamFunction]
		public static void TestFunc3Args(TagHolder self, string a, string b, string c) {
			Moo(self.Name);
		}
		[SteamFunction]
		public static void TestFunc4Args(TagHolder self, string a, string b, string c, string d) {
			Moo(self.Name);
		}
		[SteamFunction]
		public static void TestFunc5Args(TagHolder self, string a, string b, string c, string d, string e) {
			Moo(self.Name);
		}
		[SteamFunction]
		public static void TestFuncPArray(TagHolder self, params object[] args) {
			Moo(self.Name);
		}
		[SteamFunction]
		public static void TestException(TagHolder self) {
			throw new SEException("I threw an Test exception");
		}
		[SteamFunction]
		public static void funkce(TagHolder self, ScriptArgs sa) {
			Console.WriteLine("function has been run: " + sa.Args);
		}

		//public void on_startup(TagHolder globals) {
		//	TagKey testTag=Tag("testTag");
		//	
		//	ArrayList list=new ArrayList();
		//	list.Add("first item");
		//	list.Add("second item");
		//	list.Add(Server.dice.Next(5, 100));
		//	ArrayList list2=new ArrayList();
		//	list2.Add(Server.dice.Next(5, 100));
		//	list2.Add(Server.dice.Next(5, 100));
		//	list2.Add(Server.dice.Next(5, 100));
		//	list2.Add("third item");
		//	list2.Add(Thing.UidGetThing(0));
		//	list2.Add(GameAccount.Get(0));
		//	list.Add(list2);
		//	globals.SetTag(testTag,list);
		//	
		//	TagHolder copied = new TagHolder(globals);
		//
		//	Console.WriteLine("original:"+Globals.ObjToString(globals.tags));
		//	Console.WriteLine("copied:"+Globals.ObjToString(copied.tags));
		//	ArrayList copiedlist=(ArrayList)copied.GetTag(testTag);
		//	copiedlist[1]="changeditem";
		//	list.Add("newitem");
		//	list2.Add("newitem in list 2");
		//	Console.WriteLine("original:"+Globals.ObjToString(globals.tags));			
		//	Console.WriteLine("copied:"+Globals.ObjToString(copied.tags));
		//	
		//	Console.WriteLine("list:"+list.Count+"   "+list.Capacity);
		//}
		//
		//public void on_testtrigger(TagHolder globals, object[] args) {
		//	Console.WriteLine("triggered with args: "+Globals.ObjToString(args));
		//	Console.WriteLine("timers is now: "+Globals.ObjToString(Server.globals.timers));
		//}
	}
}