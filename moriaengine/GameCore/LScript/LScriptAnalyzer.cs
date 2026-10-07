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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using PerCederberg.Grammatica.Parser;

namespace SteamEngine.LScript {
	//internal class DebugAnalyzer : StrictAnalyzer {
	//    private string indent = "";

	//    public override Node Exit(Node node) {
	//        indent = indent.Substring(0, indent.Length - 2);
	//        return node;
	//    }

	//    public override void Enter(Node node) {
	//        Console.WriteLine(indent + "entering " + node);
	//        indent += "  ";
	//    }
	//}

	internal class LScriptAnalyzer : StrictAnalyzer {
		private int flag;
		private bool isInQuotes;
		private Stack<bool> quotes = new Stack<bool>();

		public override Node Analyze(Node node) {
			this.flag = 0;
			node = base.Analyze(node);
			this.flag = 1;
			node = base.Analyze(node);
			this.flag = 2;
			return base.Analyze(node);
		}

		public override void Enter(Node node) {
			if (this.flag == 1) {
				base.Enter(node);
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public override Node Exit(Node node) {
			if (this.flag == 0) {
				if (node.GetId() != (int) StrictConstants.ARGS_LIST) {
					if (node.GetChildCount() == 1) {
						return node.GetChildAt(0);
					}
				}
			} else if (this.flag == 1) {
				base.Exit(node);
			} else {
				if (node.GetChildCount() == 1) {
					return node.GetChildAt(0);
				}
			}
			return node;
		}

		public override void EnterQuotedString(Production node) {
			this.quotes.Push(this.isInQuotes);
			this.isInQuotes = true;
		}

		public override Node ExitQuotedString(Production node) {
			this.isInQuotes = this.quotes.Pop();
			return node;
		}

		public override void EnterEvalExpression(Production node) {
			this.quotes.Push(this.isInQuotes);
			this.isInQuotes = false;
		}

		public override Node ExitEvalExpression(Production node) {
			this.isInQuotes = this.quotes.Pop();
			return node;
		}

		public override void EnterStrongEvalExpression(Production node) {
			this.quotes.Push(this.isInQuotes);
			this.isInQuotes = false;
		}

		public override Node ExitStrongEvalExpression(Production node) {
			this.isInQuotes = this.quotes.Pop();
			return node;
		}

		public override Node ExitArg(Token node) {
			return node;
		}



		//a function call that looks like "funcname arg1 arg2 arg3" is parsed as "funcname(arg1(arg2(arg3)))"
		//this method makes it "back" ti the desired form "funcname(arg1, arg2, arg3)"
		//of course, it`s not perfect :) But people should anyway write their functions properly :)
		public override Node ExitArgsList(Production argsList) {
			//Console.WriteLine("ExitArgsList "+LScript.GetString(argsList)+" "+argsList.GetChildCount());
			if (argsList.GetChildCount() >= 1) {
				Node subnode = argsList.GetChildAt(argsList.GetChildCount() - 1);
				if (subnode.GetChildCount() >= 2) {
					//2 because it is some string and whitespaceassigner
					if (HasArgsListAsLast(subnode)) {

					} else if (HasArgsListAsLast(subnode.GetChildAt(subnode.GetChildCount() - 1))) {
						subnode = subnode.GetChildAt(subnode.GetChildCount() - 1);
					} else {
						return argsList;
					}

					Production assigner = (Production) ((Production) subnode).PopChildAt(subnode.GetChildCount() - 1); //is no more needed
					argsList.children.AddRange(assigner.children);
					int lastIndex = argsList.children.Count - 1;
					Node lastNode = argsList.GetChildAt(lastIndex);
					if (lastNode.GetId() == (int) StrictConstants.ARGS_LIST) {
						Production subArgsList = (Production) lastNode;
						argsList.children.RemoveAt(lastIndex);
						argsList.children.AddRange(subArgsList.children);
					}
				}
			}
			return argsList;
		}

		private static bool HasArgsListAsLast(Node node) {
			Node lastnode = node.GetChildAt(node.GetChildCount() - 1);
			if ((lastnode != null)
					&& (lastnode.GetId() == (int) StrictConstants.WHITE_SPACE_ASSIGNER)) {
				//Console.WriteLine("I am "+node);
				return true;
			}
			return false;
		}
	}
}