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
using System.IO;
using System.Text;
using EQATEC.Profiler;
using PerCederberg.Grammatica.Parser;
using SteamEngine.Common;
using SteamEngine.Timers;

namespace SteamEngine.LScript {

	public static class LScriptMain {
		internal static int startLine;

		internal static LScriptHolder snippetRunner = new LScriptHolder();

		public static Exception LastSnippetException {
			get {
				return snippetRunner.lastRunException;
			}
		}

		public static bool LastSnippetSuccess {
			get {
				return snippetRunner.lastRunSuccesful;
			}
		}

		public static object RunSnippet(TagHolder self, string script) {
			return RunSnippet("<snippet>", 0, self, script);
		}

		public static object TryRunSnippet(TagHolder self, string script) {
			return TryRunSnippet("<snippet>", 0, self, script);
		}

		public static LScriptHolder GetNewSnippetRunner(string filename, int line, string script) {
			//Logger.WriteDebug("GetNewSnippetRunner("+script+")");

			script += Environment.NewLine;
			LScriptHolder newSnippetRunner = new LScriptHolder();
			newSnippetRunner.filename = filename;
			newSnippetRunner.line = line;
			try {
				using (StringReader reader = new StringReader(script)) {
					newSnippetRunner.code = Compile(newSnippetRunner, reader, line);
				}
				newSnippetRunner.lastRunSuccesful = true;
				return newSnippetRunner;
			} catch (ParserLogException ple) {
				newSnippetRunner.lastRunException = ple;
				LogStrBuilder lstr = new LogStrBuilder();
				for (int i = 0, n = ple.GetErrorCount(); i < n; i++) {
					ParseException pe = ple.GetError(i);
					int curline = pe.GetLine() + line;
					if (i > 0) {
						lstr.Append(Environment.NewLine);
					}
					lstr.Append(LogStr.FileLine(filename, curline)).Append(pe.GetErrorMessage());
				}
				throw new SEException(lstr.ToLogStr());
			} catch (RecursionTooDeepException rtde) {
				newSnippetRunner.lastRunException = rtde;
				throw rtde; // we really do want to rethrow it, so that its useless stack is lost.
			} catch (Exception e) {
				newSnippetRunner.lastRunException = e;
				throw;
			}
		}

		public static object RunSnippet(string filename, int line, TagHolder self, string script) {
			//Console.WriteLine("running snippet "+script);
			script += Environment.NewLine;
			//snippetRunner = new LScriptHolder();
			//WorldSaver.currentfile = filename;
			snippetRunner.filename = filename;
			snippetRunner.line = line;
			snippetRunner.containsRandom = false;
			//snippetRunner.registerNames.Clear();
			snippetRunner.lastRunSuccesful = false;
			try {
				using (StringReader reader = new StringReader(script)) {
					snippetRunner.code = Compile(snippetRunner, reader, line);
				}
				object retVal = snippetRunner.code.Run(new ScriptVars(null, self, snippetRunner.LocalVarsCount));
				snippetRunner.lastRunSuccesful = true;
				return retVal;
			} catch (ParserLogException ple) {
				snippetRunner.lastRunException = ple;
				LogStr lstr = (LogStr) "";
				for (int i = 0, n = ple.GetErrorCount(); i < n; i++) {
					ParseException pe = ple.GetError(i);
					int curline = pe.GetLine() + line;
					if (i > 0) {
						lstr = lstr + Environment.NewLine;
					}
					lstr = lstr + LogStr.FileLine(filename, curline)
						+ pe.GetErrorMessage();
					//Logger.WriteError(WorldSaver.currentfile, curline, pe.GetErrorMessage());
				}
				throw new SEException(lstr);
			} catch (RecursionTooDeepException rtde) {
				snippetRunner.lastRunException = rtde;
				throw rtde; // we really do want to rethrow it, so that its useless stack is lost.
			} catch (Exception e) {
				snippetRunner.lastRunException = e;
				throw;
			}
		}

		public static object TryRunSnippet(string filename, int line, TagHolder self, string script) {
			try {
				return RunSnippet(filename, line, self, script);
			} catch (FatalException) {
				throw;
			} catch (SEException sex) {
				if (sex.StartsWithFileLine) {
					Logger.WriteError(sex);
				} else {
					Logger.WriteError(filename, line, sex);
				}
			} catch (Exception e) {
				Logger.WriteError(filename, line, e);
			}
			return null;
		}

		internal static LScriptHolder LoadAsFunction(TriggerSection input) {
			string name = input.TriggerName;
			LScriptHolder sc = ScriptHolder.GetFunction(name) as LScriptHolder;
			if (sc == null) {
				sc = new LScriptHolder(input);
				sc.RegisterAsFunction();
			} else {
				if (sc.unloaded) {
					sc.Compile(input);
				} else {
					throw new ScriptException("Function " + LogStr.Ident(name) + " already exists!");
				}
			}
			return sc;
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal static OpNode TryCompile(LScriptHolder parent, TextReader stream, int startLine) {
			try {
				return Compile(parent, stream, startLine);
			} catch (FatalException) {
				throw;
			} catch (ParserLogException ple) {
				for (int i = 0, n = ple.GetErrorCount(); i < n; i++) {
					ParseException pe = ple.GetError(i);
					int line = pe.GetLine() + startLine;
					Logger.WriteError(parent.filename, line, pe);
				}
			} catch (RecursionTooDeepException) {
				Logger.WriteError(parent.filename, startLine, "Recursion too deep while parsing.");
			} catch (Exception e) {
				Logger.WriteError(parent.filename, startLine, e);
			}
			return null;
		}

		internal static OpNode Compile(LScriptHolder parent, TextReader stream, int startLine) {
			LScriptMain.startLine = startLine;
			Parser parser = new LScriptParser(stream);
			//Parser parser = new LScriptParser(stream, new DebugAnalyzer());

			Node node = null;
			node = parser.Parse();
			indent = "";
			Analyzer analyzer = new LScriptAnalyzer();
			node = analyzer.Analyze(node);
			//DisplayTree(node);
			OpNode finishedNode = CompileNode(parent, node);
			return finishedNode;
		}

		[SkipInstrumentation]
		internal static OpNode CompileNode(IOpNodeHolder parent, Node code, bool mustEval) {
			switch ((StrictConstants) code.GetId()) {
				case StrictConstants.STRONG_EVAL_EXPRESSION:
				case StrictConstants.EVAL_EXPRESSION:
				case StrictConstants.ARGUMENT:
				case StrictConstants.VAR_EXPRESSION:
				case StrictConstants.IF_BLOCK:
				case StrictConstants.WHILE_BLOCK:
				case StrictConstants.SCRIPT:
				case StrictConstants.CODE:
				case StrictConstants.CODE_BODY:
				case StrictConstants.SIMPLE_CODE:
				case StrictConstants.SIMPLE_CODE_BODY:
				case StrictConstants.ARGCHK:
				case StrictConstants.ARGTXT:
				case StrictConstants.ARGNUM:
				case StrictConstants.ARGO:
				case StrictConstants.ARGN:
				case StrictConstants.ARGNN:
				case StrictConstants.ARGVN:
				case StrictConstants.ARGON:
				case StrictConstants.ADD_TIMER_EXPRESSION:
				case StrictConstants.WHITESPACE:
				case StrictConstants.COMEOL:
				case StrictConstants.EVAL_WORD_EXPRESSION:
				case StrictConstants.TIMER_KEY:
				case StrictConstants.TRIGGER_KEY:
				case StrictConstants.FOREACH_BLOCK:
				case StrictConstants.FOR_BLOCK:
				case StrictConstants.SWITCH_BLOCK:
				case StrictConstants.RANDOM_EXPRESSION:
				case StrictConstants.TYPE_OF_EXPRESSION:
					return CompileNode(parent, code);
			}

			if (mustEval) {
				switch ((StrictConstants) code.GetId()) {
					case StrictConstants.STRING:
					case StrictConstants.SIMPLE_EXPRESSION:
						return OpNode_Lazy_Expression.Construct(parent, code, true);

					case StrictConstants.DOTTED_EXPRESSION_CHAIN:
						return OpNode_Lazy_ExpressionChain.Construct(parent, code);

					case StrictConstants.CODE_BODY_PARENS:
					case StrictConstants.SIMPLE_CODE_BODY_PARENS:
						return CompileNode(parent, code.GetChildAt(1), true);

					case StrictConstants.SCRIPT_LINE:
						if (code.GetChildCount() > 1) {
							return CompileNode(parent, code.GetChildAt(0));
						}
						return OpNode_Object.Construct(parent, (object) null);
				}


				throw new InterpreterException("Uncompilable node. If you see this message you have probably used expression '" + LogStr.Number(GetString(code)) + "'(Node type " + LogStr.Ident(code.ToString()) + ") in an invalid way.",
					startLine + code.GetStartLine(), code.GetStartColumn(),
					GetParentScriptHolder(parent).filename, GetParentScriptHolder(parent).GetDecoratedName());
			}
			return CompileNode(parent, code);
		}

		internal static OpNode CompileNode(IOpNodeHolder parent, Node code) {
			//Console.WriteLine("compiling "+GetString(code));
			switch ((StrictConstants) code.GetId()) {
				case StrictConstants.IF_BLOCK:
					return OpNode_If.Construct(parent, code);

				case StrictConstants.WHILE_BLOCK:
					return OpNode_While.Construct(parent, code);

				case StrictConstants.FOREACH_BLOCK:
					return OpNode_Foreach.Construct(parent, code);

				case StrictConstants.FOR_BLOCK:
					return OpNode_For.Construct(parent, code);

				case StrictConstants.SWITCH_BLOCK:
					return OpNode_Switch.Construct(parent, code);

				case StrictConstants.WHITESPACE:
#if DEBUG
					Logger.WriteWarning(GetParentScriptHolder(parent).filename,
						code.GetStartLine() + startLine, "Void code.");
#endif
					return OpNode_Object.Construct(parent, (object) null);

				case StrictConstants.COMEOL:
#if DEBUG
					Logger.WriteWarning(GetParentScriptHolder(parent).filename,
						code.GetStartLine() + startLine, "Void code.");
#endif
					return OpNode_Object.Construct(parent, (object) null);

				case StrictConstants.TIMER_KEY:
					return OpNode_Object.Construct(parent, TimerKey.Acquire(
						((Token) code.GetChildAt(1)).GetImage()));

				case StrictConstants.TRIGGER_KEY:
					return OpNode_Object.Construct(parent, TriggerKey.Acquire(
						((Token) code.GetChildAt(1)).GetImage()));

				case StrictConstants.PLUGIN_KEY:
					return OpNode_Object.Construct(parent, PluginKey.Acquire(
						((Token) code.GetChildAt(2)).GetImage()));

				case StrictConstants.ARGCHK:
				case StrictConstants.ARGTXT:
				case StrictConstants.ARGNUM:
				case StrictConstants.ARGO:
				case StrictConstants.ARGN:
				case StrictConstants.ARGNN:
				case StrictConstants.ARGVN:
				case StrictConstants.ARGON:
				case StrictConstants.ARGUMENT:
					return OpNode_Argument.Construct(parent, code);

				case StrictConstants.SCRIPT:
					return OpNode_Script.Construct(parent, code);

				case StrictConstants.CODE:
					return OpNode_Code.Construct(parent, code);

				case StrictConstants.CODE_BODY:
					return OpNode_Lazy_UnOperator.Construct(parent, code);

				case StrictConstants.CODE_BODY_PARENS:
					return CompileNode(parent, code.GetChildAt(1));

				case StrictConstants.SIMPLE_CODE:
					return OpNode_Code.Construct(parent, code);

				case StrictConstants.SIMPLE_CODE_BODY:
					return OpNode_Lazy_UnOperator.Construct(parent, code);

				case StrictConstants.SIMPLE_CODE_BODY_PARENS:
					return CompileNode(parent, code.GetChildAt(1));

				case StrictConstants.EVAL_WORD_EXPRESSION:
					return CompileNode(parent, code.GetChildAt(1), true);

				case StrictConstants.QUOTED_STRING:
					return OpNode_Lazy_QuotedString.Construct(parent, code);

				case StrictConstants.SCRIPT_LINE:
					if (code.GetChildCount() > 1) {
						return CompileNode(parent, code.GetChildAt(0));
					}
					return OpNode_Object.Construct(parent, (object) null);

				case StrictConstants.STRONG_EVAL_EXPRESSION:
				case StrictConstants.EVAL_EXPRESSION:
					return OpNode_Lazy_EvalExpression.Construct(parent, code);

				case StrictConstants.DOTTED_EXPRESSION_CHAIN:
					return OpNode_Lazy_ExpressionChain.Construct(parent, code);

				case StrictConstants.STRING:
				case StrictConstants.SIMPLE_EXPRESSION:
					return OpNode_Lazy_Expression.Construct(parent, code);

				case StrictConstants.RANDOM_EXPRESSION:
					return OpNode_Lazy_RandomExpression.Construct(parent, code);

				case StrictConstants.VAR_EXPRESSION:
					return OpNode_Lazy_VarExpression.Construct(parent, code);

				case StrictConstants.ADD_TIMER_EXPRESSION:
					return OpNode_Lazy_AddTimer.Construct(parent, code);

				case StrictConstants.TYPE_OF_EXPRESSION:
					return OpNode_Typeof.Construct(parent, code);

				case StrictConstants.INTEGER:
					long i;
					try {
						i = ConvertTools.ParseInt64(((Token) code).GetImage());
					} catch (Exception e) {
						throw new InterpreterException("Exception while parsing integer",
							startLine + code.GetStartLine(), code.GetStartColumn(),
							GetParentScriptHolder(parent).filename, GetParentScriptHolder(parent).GetDecoratedName(), e);
					}
					if ((i <= int.MaxValue) && (i >= int.MinValue)) {
						return OpNode_Object.Construct(parent, (int) i);
					}
					return OpNode_Object.Construct(parent, i);

				case StrictConstants.HEXNUMBER:
					ulong h;
					try {
						h = ConvertTools.ParseUInt64(((Token) code).GetImage().Trim());
					} catch (Exception e) {
						throw new InterpreterException("Exception while parsing hexadecimal integer",
							startLine + code.GetStartLine(), code.GetStartColumn(),
							GetParentScriptHolder(parent).filename, GetParentScriptHolder(parent).GetDecoratedName(), e);
					}
					if ((h <= uint.MaxValue) && (h >= uint.MinValue)) {
						return OpNode_Object.Construct(parent, (uint) h);
					}
					return OpNode_Object.Construct(parent, h);

				case StrictConstants.FLOAT:
					double d;
					try {
						d = ConvertTools.ParseDouble(((Token) code).GetImage());
					} catch (Exception e) {
						throw new InterpreterException("Exception while parsing decimal number",
							startLine + code.GetStartLine(), code.GetStartColumn(),
							GetParentScriptHolder(parent).filename, GetParentScriptHolder(parent).GetDecoratedName(), e);
					}
					return OpNode_Object.Construct(parent, d);
			}

			throw new InterpreterException("Uncompilable node. If you see this message you have probably used expression '" + LogStr.Number(GetString(code)) + "'(Node type " + LogStr.Ident(code.ToString()) + ")  in an invalid way.",
				startLine + code.GetStartLine(), code.GetStartColumn(),
				GetParentScriptHolder(parent).filename, GetParentScriptHolder(parent).GetDecoratedName());
		}

		internal static LScriptHolder GetParentScriptHolder(IOpNodeHolder holder) {
			OpNode parentNode = holder as OpNode;
			if (parentNode != null) {
				return parentNode.ParentScriptHolder;
			}
			return (LScriptHolder) holder;
		}

		private static string indent;

		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		internal static void DisplayTree(Node node) {
			Console.WriteLine(indent + node + " : " + GetString(node));
			//Console.WriteLine(indent+node);
			for (int i = 0, n = node.GetChildCount(); i < n; i++) {
				Node child = node.GetChildAt(i);
				indent += "    ";
				DisplayTree(child);
				indent = indent.Substring(0, indent.Length - 4);
			}
		}

		internal static string GetFirstTokenString(Node node) {
			if (node.GetChildCount() > 0) {
				return GetFirstTokenString(node.GetChildAt(0));
			}
			return GetString(node);
		}

		internal static string GetString(Node node) {
			StringBuilder builder = new StringBuilder();
			BuildToString(node, builder);
			return builder.ToString();
		}

		private static void BuildToString(Node node, StringBuilder builder) {
			Token token = node as Token;
			if (token != null) {
				if (node.GetId() == (int) StrictConstants.ESCAPEDCHAR) {
					builder.Append(token.GetImage()[1]);
				} else {
					builder.Append(token.GetImage());
				}
			}
			if (node != null) {
				for (int i = 0, n = node.GetChildCount(); i < n; i++) {
					Node child = node.GetChildAt(i);
					BuildToString(child, builder);
				}
			}
		}
	}
}
