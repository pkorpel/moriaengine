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
using SteamEngine.Common;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.LScript {
	public class LScriptTest {
		public static LScriptTesterObject testObj = new LScriptTesterObject();

		//temporary for direct launching while writing the tests
		//public void def__RunLScriptTests(TagHolder ignored) {
		//	RunLScriptTests();
		//}

		[RegisterWithRunTests]
		public static void RunLScriptTests() {

			//Logger.Show("TestSuite", "Running ");


			//calling methods
			TestSnippet(1, "TestMethod_NoParams", "calling zero param method without caller ()");
			TestSnippet(1, "TestMethod_NoParams()", "calling zero param method with ()");
			TestSnippet(0, "TestMethod_NoParams=", typeof(SEException), "calling zero param method with =");
			TestSnippet(0, "TestMethod_NoParams 186",
				"Class member (method/property/field/constructor) 'TestMethod_NoParams' is getting wrong arguments",
				"calling zero param method with an argument after whitespace");
			TestSnippet(0, "TestMethod_NoParams(786)",
				"Class member (method/property/field/constructor) 'TestMethod_NoParams' is getting wrong arguments",
				"calling zero param method with an argument in ()");
			TestSnippet(0, "TestMethod_NoParams =74123",
				"Class member (method/property/field/constructor) 'TestMethod_NoParams' is getting wrong arguments",
				"calling zero param method with an argument after =");

			TestSnippet(3789, "TestMethod_OneParam 3789", "calling one param method without parens");
			TestSnippet(7893, "TestMethod_OneParam(7893)", "calling one param method with parens, syntax 1");
			TestSnippet(7893, "TestMethod_OneParam (7893)", "calling one param method with parens, syntax 2");
			TestSnippet(7893, "TestMethod_OneParam ( 7893 )", "calling one param method with parens, syntax 3");
			TestSnippet(7893, "TestMethod_OneParam( 7893)", "calling one param method with parens, syntax 4");
			TestSnippet(7893, "TestMethod_OneParam(7893 )", "calling one param method with parens, syntax 5");
			TestSnippet(8492, "TestMethod_OneParam=8492", "calling one param method with =, syntax 1");
			TestSnippet(8492, "TestMethod_OneParam= 8492", "calling one param method with =, syntax 2");
			TestSnippet(8492, "TestMethod_OneParam =8492", "calling one param method with =, syntax 3");
			TestSnippet(8492, "TestMethod_OneParam = 8492", "calling one param method with =, syntax 4");
			TestSnippet(0x2195, "TestMethod_OneParam 02195", "calling one param method without parens, hexa without xX");
			TestSnippet(0x2195, "TestMethod_OneParam 0x2195", "calling one param method without parens, hexa without x");
			TestSnippet(1.2195, "TestMethod_OneParam 1.2195", "calling one param method without parens, decimal number with leading number");
			TestSnippet(0.2195, "TestMethod_OneParam .2195", "calling one param method without parens, decimal number without leading number");

			TestSnippet(0, "TestMethod_OneParam",
				"Class member (method/property/field/constructor) 'TestMethod_OneParam' is getting wrong arguments",
				"calling one param method without parens and without any parameter");
			TestSnippet(0, "TestMethod_OneParam()",
				"Class member (method/property/field/constructor) 'TestMethod_OneParam' is getting wrong arguments",
				"calling one param method with parens without any argument");

			TestSnippet(1, "TestMethod_OneStringParam gfdyushgd",
				"calling one string param method without parens");
			TestSnippet(1, "TestMethod_OneStringParam(gfdyushgd)",
				"calling one string param method with parens");
			TestSnippet(1, "TestMethod_OneStringParam=gfdyushgd",
				"calling one string param method with =");

			TestSnippet(1, "TestMethod_OneStringParam gf gfds hgf456 fd",
				"calling one string param method without parens with multiple params");
			TestSnippet(1, "TestMethod_OneStringParam(gf gfds 7hgf456 fd)",
				"calling one string param method with parens with multiple params");
			TestSnippet(1, "TestMethod_OneStringParam=gf gfds 7hgf456 fd",
				"calling one string param method with = with multiple params");

			//returning value
			Sanity.IfTrueThrow(5 != Convert.ToInt32(LScriptMain.RunSnippet(testObj, "return 5")), "Error while return integer");
			Sanity.IfTrueThrow(!Equals(LScriptMain.RunSnippet(testObj, "return \"some test string 7hgf456\""), "some test string 7hgf456"),
				"Error while returning quoted string");
			Sanity.IfTrueThrow(!Equals(LScriptMain.RunSnippet(testObj, "return \" \\\" \""), " \" "),
				"Error while returning quoted string with escaped character");

			Sanity.IfTrueThrow(!Equals(LScriptMain.RunSnippet(testObj, "return some test string 7hgf456"), "some test string 7hgf456"),
				"Error while returning nonquoted string");

			Sanity.IfTrueThrow(564 != Convert.ToInt32(LScriptMain.RunSnippet(testObj, "return GetInteger")),
				"Error while returning integer from a method");
			Sanity.IfTrueThrow(564 != Convert.ToInt32(LScriptMain.RunSnippet(testObj, "return <GetInteger>")),
				"Error while returning integer from a <method>");
			Sanity.IfTrueThrow(564 != Convert.ToInt32(LScriptMain.RunSnippet(testObj, "return <GetInteger()>")),
				"Error while returning integer from a <method()>");
			Sanity.IfTrueThrow(564 != Convert.ToInt32(LScriptMain.RunSnippet(testObj, "return <?GetInteger?>")),
				"Error while returning integer from a <method>");

			TestSnippet(2, "TestMethod_NoParams;TestMethod_NoParams", "calling two expressions in a row, syntax 1");
			TestSnippet(2, "TestMethod_NoParams; TestMethod_NoParams", "calling two expressions in a row, syntax 2");
			TestSnippet(2, "TestMethod_NoParams ;TestMethod_NoParams", "calling two expressions in a row, syntax 3");
			TestSnippet(2, "TestMethod_NoParams ; TestMethod_NoParams", "calling two expressions in a row, syntax 4");


			//these will be a bit difficult... :\
			//TestSnippet(+1, "TestMethod_OneParam +1", "integer unary '+' without parens");
			//TestSnippet(-1, "TestMethod_OneParam -1", "integer unary '-' without parens");
			//TestSnippet(564, "TestMethod_OneParam +GetInteger()", "general expression unary '+' without parens");
			//TestSnippet(-564, "TestMethod_OneParam -GetInteger()", "general expression unary '-' without parens");				
			//Sanity.IfTrueThrow(!SteamEngine.Timers.TimerKey.Get("testtimerkey").Equals(LScript.RunSnippet(testObj, "return %testtimerkey")), "timerkey");
			//Sanity.IfTrueThrow(!SteamEngine.Timers.TriggerKey.Get("testtriggerkey").Equals(LScript.RunSnippet(testObj, "return @testtriggerkey ")), "triggerkey");

			TestSnippet(1, "TestMethod_OneParam(+1)", "integer unary '+' with parens");
			TestSnippet(1, "TestMethod_OneParam=+1", "integer unary '+' with =");
			TestSnippet(-1, "TestMethod_OneParam(-1)", "integer unary '-' with parens");
			TestSnippet(-1, "TestMethod_OneParam=-1", "integer unary '-' with =");
			TestSnippet(564, "TestMethod_OneParam(+GetInteger())", "general expression unary '+' with parens");
			TestSnippet(564, "TestMethod_OneParam=+GetInteger()", "general expression unary '+' with =");
			TestSnippet(-564, "TestMethod_OneParam(-GetInteger())", "general expression unary '-' with parens");
			TestSnippet(-564, "TestMethod_OneParam=-GetInteger()", "general expression unary '-' with =");

			TestSnippet(56 + 7, "TestMethod_OneParam 56+7", "integer binary '+' without parens");
			TestSnippet(56 + 7, "TestMethod_OneParam(56+7)", "integer binary '+' with parens");
			TestSnippet(56 + 7, "TestMethod_OneParam=56+7", "integer binary '+' with =");
			TestSnippet(56 - 7, "TestMethod_OneParam 56-7", "integer binary '-' without parens");
			TestSnippet(56 - 7, "TestMethod_OneParam(56-7)", "integer binary '-' with parens");
			TestSnippet(56 - 7, "TestMethod_OneParam=56-7", "integer binary '-' with =");
			TestSnippet(56 * 7, "TestMethod_OneParam 56*7", "integer binary '*' without parens");
			TestSnippet(56 * 7, "TestMethod_OneParam(56*7)", "integer binary '*' with parens");
			TestSnippet(56 * 7, "TestMethod_OneParam=56*7", "integer binary '*' with =");
			TestSnippet(56 / 7, "TestMethod_OneParam 56/7", "integer binary '/' without parens");
			TestSnippet(56 / 7, "TestMethod_OneParam(56/7)", "integer binary '/' with parens");
			TestSnippet(56 / 7, "TestMethod_OneParam=56/7", "integer binary '/' with =");

			TestSnippet(double.PositiveInfinity, "TestMethod_OneParam(56/0)", "division by zero");


			TestSnippet(Globals.Port, "return Globals.port", "dotted expression");
			Sanity.IfTrueThrow(!Equals(LScriptMain.TryRunSnippet(testObj, "return Tools.ObjToString(System.Collections.ArrayList())"), "[]"),
				"dotted expression: method and constructor witn no params"); //this could outcome false if someone changed the ObjToString method...
			Sanity.IfTrueThrow(!Equals(LScriptMain.TryRunSnippet(testObj, "return Tools.ObjToString(System.Collections.ArrayList(0))"), "[]"),
				"dotted expression: method and constructor witn one param with parens"); //this could outcome false if someone changed the ObjToString method...
			Sanity.IfTrueThrow(!Equals(LScriptMain.TryRunSnippet(testObj, "return Tools.ObjToString(System.Collections.ArrayList=0)"), "[]"),
				"dotted expression: method and constructor witn one param with with ="); //this could outcome false if someone changed the ObjToString method...	
			//this doesn't work (it should never have I think...)
			//Sanity.IfTrueThrow(!string.Equals(LScript.TryRunSnippet(testObj, "return Tools.ObjToString(System.Collections.ArrayList 0)"), "[]"), 
			//	"dotted expression: method and constructor with one param without parens"); //this could outcome false if someone changed the ObjToString method...

			TestSnippet(1, "arg testlocal 1; TestMethod_OneParam arg testlocal", "ARG/LOCAL, syntax 1");
			TestSnippet(2, "arg testlocal = 2; TestMethod_OneParam arg testlocal", "ARG/LOCAL, syntax 2");
			TestSnippet(3, "arg.testlocal 3; TestMethod_OneParam arg.testlocal", "ARG/LOCAL, syntax 3");
			TestSnippet(4, "arg.testlocal = 4; TestMethod_OneParam arg.testlocal", "ARG/LOCAL, syntax 4");
			TestSnippet(5, "arg(testlocal,5); TestMethod_OneParam arg(testlocal)", "ARG/LOCAL, syntax 5");
			TestSnippet(6, "arg(testlocal, 6); TestMethod_OneParam arg(testlocal)", "ARG/LOCAL, syntax 6");
			TestSnippet(1, "local testlocal 1; TestMethod_OneParam local testlocal", "ARG/LOCAL, syntax 7");
			TestSnippet(2, "local testlocal = 2; TestMethod_OneParam local testlocal", "ARG/LOCAL, syntax 8");
			TestSnippet(3, "local.testlocal 3; TestMethod_OneParam local.testlocal", "ARG/LOCAL, syntax 9");
			TestSnippet(4, "local.testlocal = 4; TestMethod_OneParam local.testlocal", "ARG/LOCAL, syntax 10");
			TestSnippet(5, "local(testlocal,5); TestMethod_OneParam local(testlocal)", "ARG/LOCAL, syntax 11");
			TestSnippet(6, "local(testlocal, 6); TestMethod_OneParam local(testlocal)", "ARG/LOCAL, syntax 12");

			TestSnippet(1, "var testvariable 1; TestMethod_OneParam var testvariable", "VAR, syntax 1");
			TestSnippet(2, "var testvariable = 2; TestMethod_OneParam var testvariable", "VAR, syntax 2");
			TestSnippet(3, "var.testvariable 3; TestMethod_OneParam var.testvariable", "VAR, syntax 3");
			TestSnippet(4, "var.testvariable = 4; TestMethod_OneParam var.testvariable", "VAR, syntax 4");
			TestSnippet(5, "var(testvariable,5); TestMethod_OneParam var(testvariable)", "VAR, syntax 5");
			TestSnippet(6, "var(testvariable, 6); TestMethod_OneParam var(testvariable)", "VAR, syntax 6");

			TestSnippet(1, "tag testtag 1; TestMethod_OneParam tag testtag", "TAG, syntax 1");
			TestSnippet(2, "tag testtag = 2; TestMethod_OneParam tag testtag", "TAG, syntax 2");
			TestSnippet(3, "tag.testtag 3; TestMethod_OneParam tag.testtag", "TAG, syntax 3");
			TestSnippet(4, "tag.testtag = 4; TestMethod_OneParam tag.testtag", "TAG, syntax 4");
			TestSnippet(5, "tag(testtag,5); TestMethod_OneParam tag(testtag)", "TAG, syntax 5");
			TestSnippet(6, "tag(testtag, 6); TestMethod_OneParam tag(testtag)", "TAG, syntax 6");

			//ambiguity test
			TestSnippet(5, "TestMethod_IPointParam(Point4D(1,2,3,4))", "ambiguity test 1");
			TestSnippet(6, "TestMethod_IPointParam(Point3D(1,2,3))", "ambiguity test 2");
			TestSnippet(7, "TestMethod_IPointParam(Point2D(1,2))", "ambiguity test 3");

			TestSnippet(2, "TestMethod_PointAndIpointParam(Point4D(1,2,3,4))", "ambiguity test 4");
			TestSnippet(3, "TestMethod_PointAndIpointParam(Point3D(1,2,3))", "ambiguity test 5");
			TestSnippet(4, "TestMethod_PointAndIpointParam(Point2D(1,2))", "ambiguity test 6");

			TestSnippet(5, "TestMethod_IPointParam(LScriptTesterIPoint4D())", "ambiguity test 7");
			TestSnippet(5, "TestMethod_PointAndIpointParam(LScriptTesterIPoint4D())", "ambiguity test 8");

			Sanity.IfTrueThrow(!TimerKey.Acquire("testtimerkey").Equals(LScriptMain.RunSnippet(testObj, "return(%testtimerkey)")), "timerkey");
			Sanity.IfTrueThrow(!TriggerKey.Acquire("testtriggerkey").Equals(LScriptMain.RunSnippet(testObj, "return(@testtriggerkey)")), "triggerkey");

			LScriptMain.RunSnippet(testObj, "return {0 1}");
			LScriptMain.RunSnippet(testObj, "return { 0 1}");
			LScriptMain.RunSnippet(testObj, "return {0 1 }");
			LScriptMain.RunSnippet(testObj, "return { 0 1 }");
			LScriptMain.RunSnippet(testObj, "return {0,1}");
			LScriptMain.RunSnippet(testObj, "return { 0,1}");
			LScriptMain.RunSnippet(testObj, "return {0,1 }");
			LScriptMain.RunSnippet(testObj, "return { 0,1 }");
			LScriptMain.RunSnippet(testObj, "return({0 1})");
			LScriptMain.RunSnippet(testObj, "return({ 0 1})");
			LScriptMain.RunSnippet(testObj, "return({0 1 })");
			LScriptMain.RunSnippet(testObj, "return({ 0 1 })");
			LScriptMain.RunSnippet(testObj, "return({0,1})");
			LScriptMain.RunSnippet(testObj, "return({ 0,1})");
			LScriptMain.RunSnippet(testObj, "return({0,1 })");
			LScriptMain.RunSnippet(testObj, "return({ 0,1 })");

			Console.WriteLine("LScript tests complete.");
		}

		private static void TestSnippet(double difference, string script, Type expectedException, string errormessage) {
			try {
				LScriptMain.RunSnippet(testObj, script);
			} catch (Exception e) {
				if ((expectedException != null)) {
					while (e.InnerException != null) {
						e = e.InnerException;
					}
					if (expectedException.IsInstanceOfType(e)) {
						testObj.CheckCounter(difference, errormessage);
						return;
					}
				}
				throw new SanityCheckException("Error while " + errormessage + ": " + e);
			}
			throw new SanityCheckException("Error while " + errormessage + ": Expected exception was not thrown.");
		}

		private static void TestSnippet(double difference, string script, string expectedExcString, string errormessage) {
			if ((expectedExcString != null) && (expectedExcString.Length == 0)) {
				expectedExcString = null;
			}
			bool wasException = false;
			try {
				LScriptMain.RunSnippet(testObj, script);
			} catch (Exception e) {
				if ((expectedExcString != null) &&
						(expectedExcString == e.Message)) {
					wasException = true;
				} else {
					throw new SanityCheckException("Error while " + errormessage + ": " + e);
				}
			}
			if ((expectedExcString != null) && (!wasException)) {
				throw new SanityCheckException("Error while " + errormessage + ": Expected exception was not thrown.");
			}
			testObj.CheckCounter(difference, errormessage);
		}

		private static void TestSnippet(double difference, string script, string errormessage) {
			try {
				LScriptMain.RunSnippet(testObj, script);
			} catch (Exception e) {
				throw new SanityCheckException("Error while " + errormessage + ": " + e);
			}
			testObj.CheckCounter(difference, errormessage);
		}
	}


	public class LScriptTesterIPoint4D : IPoint4D {
		public int X { get { return 0; } }
		public int Y { get { return 0; } }
		public int Z { get { return 0; } }
		public byte M { get { return 0; } }

		public IEnumerable ThingsInRange() { return null; }
		public IEnumerable ItemsInRange() { return null; }
		public IEnumerable CharsInRange() { return null; }
		public IEnumerable PlayersInRange() { return null; }
		public IEnumerable StaticsInRange() { return null; }
		public IEnumerable DisconnectsInRange() { return null; }
		public IEnumerable MultiComponentsInRange() { return null; }

		public IEnumerable ThingsInRange(ushort range) { return null; }
		public IEnumerable ItemsInRange(ushort range) { return null; }
		public IEnumerable CharsInRange(ushort range) { return null; }
		public IEnumerable PlayersInRange(ushort range) { return null; }
		public IEnumerable StaticsInRange(ushort range) { return null; }
		public IEnumerable DisconnectsInRange(ushort range) { return null; }

		public Map GetMap() { return null; }

		public IPoint4D TopPoint {
			get { return this; }
		}

		IPoint3D IPoint3D.TopPoint {
			get { return this; }
		}

		IPoint2D IPoint2D.TopPoint {
			get { return this; }
		}
	}


	public class LScriptTesterObject : PluginHolder {
		public double currentCounter;
		public double lastCounter;

		private void Incr() {
			this.currentCounter++;
		}

		private void Incr(double difference) {
			this.currentCounter += difference;
		}

		internal void CheckCounter(double difference, string failureMessage) {
			double shouldbe = this.lastCounter + difference;
			Sanity.IfTrueThrow(shouldbe != this.currentCounter, "Error while " + failureMessage);
			this.lastCounter = this.currentCounter;
		}

		public void TestMethod_NoParams() {
			this.Incr();
		}

		public void TestMethod_OneParam(double difference) {
			//Console.WriteLine("TestMethod_OneParam("+difference+")");
			this.Incr(difference);
		}

		public void TestMethod_OneParam(int difference) {
			//Console.WriteLine("TestMethod_OneParam("+difference+")");
			this.Incr(difference);
		}



		public void TestMethod_PointAndIpointParam(Point4D point) {
			Console.WriteLine("TestMethod_PointAndIpointParam(Point4D)");
			this.Incr(2);
		}

		public void TestMethod_PointAndIpointParam(Point3D point) {
			Console.WriteLine("TestMethod_PointAndIpointParam(Point3D)");
			this.Incr(3);
		}

		public void TestMethod_PointAndIpointParam(Point2D point) {
			Console.WriteLine("TestMethod_PointAndIpointParam(Point2D)");
			this.Incr(4);
		}

		public void TestMethod_PointAndIpointParam(IPoint4D point) {
			Console.WriteLine("TestMethod_OneParam(IPoint4D)");
			this.Incr(5);
		}

		public void TestMethod_PointAndIpointParam(IPoint3D point) {
			Console.WriteLine("TestMethod_OneParam(IPoint3D)");
			this.Incr(6);
		}

		public void TestMethod_PointAndIpointParam(IPoint2D point) {
			Console.WriteLine("TestMethod_OneParam(IPoint2D)");
			this.Incr(7);
		}

		public void TestMethod_IPointParam(IPoint4D point) {
			Console.WriteLine("TestMethod_OneParam(IPoint4D)");
			this.Incr(5);
		}

		public void TestMethod_IPointParam(IPoint3D point) {
			Console.WriteLine("TestMethod_OneParam(IPoint3D)");
			this.Incr(6);
		}

		public void TestMethod_IPointParam(IPoint2D point) {
			Console.WriteLine("TestMethod_OneParam(IPoint2D)");
			this.Incr(7);
		}

		public void TestMethod_OneStringParam(string str) {
			this.Incr();
		}

		public int GetInteger() {
			return 564;
		}

		public string GetString() {
			return "test string";
		}

		public object GetSelf() {
			return this;
		}
	}
}



//briefly what must be tested, from the grammar file...

//done tests

//Number: int, hex, float
//SimpleExpression: Caller Indexer OperatorAssigner WhiteSpaceAssigner
//VarExpression (TAG VAR ARG/LOCAL)
//TriggerKey
//TimerKey
//QuotedString, ESCAPEDCHAR
//EvalExpression StrongEvalExpression
//DottedExpressionChain


//partly done

//BinaryOperator: + - * / & % | && || 
//ComparOperators: == != < > <= >=
//UnaryOperator: ! ~ + -



//tests to do yet

//Script
//IfBlock ElseIfBlock ElseBlock
//ForeachBlock
//WhileBlock
//SwitchBlock
//CaseBlock
//AddTimerExpression
//Argument


//not even implemented yet ;)

//RandomExpression
//CROSSHASH


