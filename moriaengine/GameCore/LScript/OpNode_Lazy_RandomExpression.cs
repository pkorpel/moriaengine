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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using PerCederberg.Grammatica.Parser;
using SteamEngine.Common;

namespace SteamEngine.LScript {
	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_Lazy_RandomExpression : OpNode, IOpNodeHolder {
		bool isSimple;//if true, this is just a random number from a range, i.e. {a b}
		//otherwise, it is a set of odds-value pairs {ao av bo bv ... }

		OpNode[] odds;
		OpNode[] values;

		protected OpNode_Lazy_RandomExpression(IOpNodeHolder parent, string filename, int line, int column, Node origNode)
			: base(parent, filename, line, column, origNode) {

			this.ParentScriptHolder.containsRandom = true;
		}

		internal static OpNode Construct(IOpNodeHolder parent, Node code) {
			int line = code.GetStartLine() + LScriptMain.startLine;
			int column = code.GetStartColumn();
			OpNode_Lazy_RandomExpression constructed = new OpNode_Lazy_RandomExpression(
				parent, LScriptMain.GetParentScriptHolder(parent).filename, line, column, code);

			//LScript.DisplayTree(code);

			int expressions = (code.GetChildCount() - 1) / 2;
			if ((expressions % 2) != 0) {
				throw new SEException("Number of subexpressions in RandomExpressions not odd. This should not happen.");
				//grammar should not let such thing in
			}
			if (expressions == 2) {
				constructed.isSimple = true;
				constructed.values = new OpNode[2];
				constructed.values[0] = LScriptMain.CompileNode(constructed, code.GetChildAt(1));
				constructed.values[1] = LScriptMain.CompileNode(constructed, code.GetChildAt(3));
			} else { // {value odds value odds value odds ... }
				expressions /= 2;
				constructed.odds = new OpNode[expressions];
				constructed.values = new OpNode[expressions];
				for (int i = 0; i < expressions; i++) {
					constructed.values[i] = LScriptMain.CompileNode(constructed, code.GetChildAt(1 + i * 4));
					constructed.odds[i] = LScriptMain.CompileNode(constructed, code.GetChildAt(3 + i * 4));
				}
				constructed.isSimple = false;
			}

			return constructed;
		}

		public void Replace(OpNode oldNode, OpNode newNode) {
			int index = Array.IndexOf(this.values, oldNode);
			if (index >= 0) {
				this.values[index] = newNode;
				return;
			}
			if (this.odds != null) {
				index = Array.IndexOf(this.odds, oldNode);
				if (index >= 0) {
					this.odds[index] = newNode;
					return;
				}
			}
			throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
		}

		internal override object Run(ScriptVars vars) {
			try {
				if (this.isSimple) {
					int lVal = Convert.ToInt32(this.values[0].Run(vars), CultureInfo.InvariantCulture);
					int rVal = Convert.ToInt32(this.values[1].Run(vars), CultureInfo.InvariantCulture);
					int min = lVal;
					int max = rVal;
					if (rVal < lVal) {
						min = rVal;
						max = lVal;
					}
					if ((this.values[0] is OpNode_Object) && (this.values[1] is OpNode_Object)) {
						if (rVal == lVal) { //no randomness at all... we create an OpNode_Object
							this.ReplaceSelf(this.values[0]);
							return rVal;
						}
						OpNode newNode = new OpNode_Final_RandomExpression_Simple_Constant(this.parent, this.filename,
							this.line, this.column, this.OrigNode, min, max + 1);
						this.ReplaceSelf(newNode);
						return newNode.Run(vars);
					} else {
						OpNode newNode = new OpNode_Final_RandomExpression_Simple_Variable(this.parent, this.filename,
							this.line, this.column, this.OrigNode, this.values[0], this.values[1]);
						this.ReplaceSelf(newNode);
						return Globals.dice.Next(min, max + 1);
					}
				}
				int pairCount = this.odds.Length;
				ValueOddsPair[] pairs = new ValueOddsPair[pairCount];
				bool areConstant = true;
				int totalOdds = 0;
				for (int i = 0; i < pairCount; i++) {
					int o = Convert.ToInt32(this.odds[i].Run(vars), CultureInfo.InvariantCulture);
					totalOdds += o;
					pairs[i] = new ValueOddsPair(this.values[i], totalOdds);
					if (!(this.odds[i] is OpNode_Object)) {
						areConstant = false;
					}
				}
				if (areConstant) {
					OpNode newNode = new OpNode_Final_RandomExpression_Constant(this.parent, this.filename,
						this.line, this.column, this.OrigNode, pairs, totalOdds);
					this.ReplaceSelf(newNode);
					return newNode.Run(vars);
				} else {
					OpNode newNode = new OpNode_Final_RandomExpression_Variable(this.parent, this.filename,
						this.line, this.column, this.OrigNode, pairs, this.odds);
					this.ReplaceSelf(newNode);
					return ((OpNode) GetRandomValue(pairs, totalOdds)).Run(vars);
					//no TryRun or such... too lazy I am :)
				}
			} catch (InterpreterException) {
				throw;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating random expression",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public override string ToString() {
			if (this.odds == null) {
				return "{ " + this.values[0] + " " + this.values[1] + " }";
			}
			StringBuilder str = new StringBuilder("{");
			for (int i = 0, n = this.odds.Length; i < n; i++) {
				str.Append(this.values[i]).Append(", ").Append(this.odds[i]).Append(", ");
			}
			str.Length -= 2;
			return str.Append("}").ToString();
		}

		public static object GetRandomValue(ValueOddsPair[] pairs, int totalOdds) {
			int num = Globals.dice.Next(0, totalOdds);
			foreach (ValueOddsPair pair in pairs) {
				if (pair.RolledSuccess(num)) {
					return pair.Value;
				}
			}
			throw new SanityCheckException("Error in the logic for picking a value. We rolled " + num + " but didn't find a result (There are " + totalOdds + " total odds, and " + pairs.Length + " elements. This should not happen.");
		}

	}

	public class ValueOddsPair {
		private object val;
		private int odds;

		public object Value {
			get {
				return this.val;
			}
			set {
				this.val = value;
			}
		}
		public int Odds {
			get {
				return this.odds;
			}
			set {
				this.odds = value;
			}
		}

		public ValueOddsPair(object value, int odds) {
			this.val = value;
			this.odds = odds;
		}

		public int AdjustOdds(int previousOdds) {
			this.odds += previousOdds;
			return this.odds;
		}

		public bool RolledSuccess(int oddsToCompare) {
			return oddsToCompare < this.odds;
		}

		public override string ToString()
		{
			if (this.val != null) {
				return (this.val + " " + this.odds);
			}
			return "null";
		}
		public LogStr ToLogStr()
		{
			if (this.val != null) {
				return LogStr.Raw(this.val) + " " + LogStr.Number(this.odds);
			}
			return (LogStr) "null";
		}
	}
}