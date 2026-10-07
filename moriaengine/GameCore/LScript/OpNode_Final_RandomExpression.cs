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

namespace SteamEngine.LScript {
	[SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase"), SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
	internal class OpNode_Final_RandomExpression_Simple_Constant : OpNode {
		int min, max;

		internal OpNode_Final_RandomExpression_Simple_Constant(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, int min, int max)
			: base(parent, filename, line, column, origNode) {
			this.min = min;
			this.max = max;
		}

		internal override object Run(ScriptVars vars) {
			return Globals.dice.Next(this.min, this.max);
		}

		public override string ToString() {
			return "{" + this.min + " " + this.max + "}";
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase"), SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
	internal class OpNode_Final_RandomExpression_Simple_Variable : OpNode, IOpNodeHolder {
		OpNode leftNode, rightNode;

		internal OpNode_Final_RandomExpression_Simple_Variable(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, OpNode leftNode, OpNode rightNode)
			: base(parent, filename, line, column, origNode) {
			this.leftNode = leftNode;
			this.rightNode = rightNode;

			leftNode.parent = this;
			rightNode.parent = this;
		}

		public virtual void Replace(OpNode oldNode, OpNode newNode) {
			if (this.leftNode == oldNode) {
				this.leftNode = newNode;
				return;
			}
			if (this.rightNode == oldNode) {
				this.rightNode = newNode;
				return;
			}
			throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
		}

		internal override object Run(ScriptVars vars) {
			try {
				int lVal = Convert.ToInt32(this.leftNode.Run(vars), CultureInfo.InvariantCulture);
				int rVal = Convert.ToInt32(this.rightNode.Run(vars), CultureInfo.InvariantCulture);
				if (lVal < rVal) {
					return Globals.dice.Next(lVal, rVal + 1);
				}
				if (lVal > rVal) {
					return Globals.dice.Next(rVal, lVal + 1);
				} //lVal == rVal
				return lVal;
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
			return "{ " + this.leftNode + " " + this.rightNode + " }";
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase"), SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
	internal class OpNode_Final_RandomExpression_Constant : OpNode, IOpNodeHolder {
		ValueOddsPair[] pairs;
		int totalOdds;

		internal OpNode_Final_RandomExpression_Constant(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, ValueOddsPair[] pairs, int totalOdds)
			: base(parent, filename, line, column, origNode) {
			this.pairs = pairs;
			this.totalOdds = totalOdds;
			//?TODO sort the pairs for better effectivity

			foreach (ValueOddsPair pair in pairs) {
				((OpNode) pair.Value).parent = this;
			}
		}

		internal override object Run(ScriptVars vars) {
			OpNode chosenNode = (OpNode) OpNode_Lazy_RandomExpression.GetRandomValue(this.pairs, this.totalOdds);
			return chosenNode.Run(vars);
		}

		public virtual void Replace(OpNode oldNode, OpNode newNode) {
			foreach (ValueOddsPair pair in this.pairs) {
				OpNode node = (OpNode) pair.Value;
				if (node == oldNode) {
					pair.Value = newNode;
					return;
				}
			}
			throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
		}

		public override string ToString() {
			StringBuilder str = new StringBuilder("{");
			foreach (ValueOddsPair pair in this.pairs) {
				str.Append(pair.Value).Append(" ").Append(pair.Odds.ToString(CultureInfo.InvariantCulture)).Append(" ");
			}
			return str.Append("}").ToString();
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase"), SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
	internal class OpNode_Final_RandomExpression_Variable : OpNode, IOpNodeHolder {
		ValueOddsPair[] pairs;
		OpNode[] odds;

		internal OpNode_Final_RandomExpression_Variable(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, ValueOddsPair[] pairs, OpNode[] odds)
			: base(parent, filename, line, column, origNode) {
			this.pairs = pairs;
			this.odds = odds;
			foreach (OpNode odd in odds) {
				odd.parent = this;
			}
			foreach (ValueOddsPair pair in pairs) {
				((OpNode) pair.Value).parent = this;
			}
		}

		internal override object Run(ScriptVars vars) {
			object oSelf = vars.self;
			try {
				vars.self = vars.defaultObject;
				int totalOdds = 0;
				for (int i = 0, n = this.pairs.Length; i < n; i++) {
					int o = Convert.ToInt32(this.odds[i].Run(vars), CultureInfo.InvariantCulture);
					totalOdds += o;
					this.pairs[i].Odds = totalOdds;
				}
				OpNode chosenNode = (OpNode) OpNode_Lazy_RandomExpression.GetRandomValue(this.pairs, totalOdds);
				return chosenNode.Run(vars);
			} catch (InterpreterException) {
				throw;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating random expression",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			} finally {
				vars.self = oSelf;
			}
		}

		public virtual void Replace(OpNode oldNode, OpNode newNode) {
			foreach (ValueOddsPair pair in this.pairs) {
				OpNode node = (OpNode) pair.Value;
				if (node == oldNode) {
					pair.Value = newNode;
					return;
				}
			}
			int index = Array.IndexOf(this.odds, oldNode);
			if (index >= 0) {
				this.odds[index] = newNode;
				return;
			}
			throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
		}

		public override string ToString() {
			StringBuilder str = new StringBuilder("{");
			for (int i = 0, n = this.pairs.Length; i < n; i++) {
				str.Append(this.pairs[i].Value).Append(" ").Append(this.odds[i]).Append(" ");
			}
			return str.Append("}").ToString();
		}
	}

}