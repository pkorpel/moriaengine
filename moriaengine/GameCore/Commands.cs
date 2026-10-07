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
using System.Reflection;
using System.Text.RegularExpressions;
using SteamEngine.Common;
using SteamEngine.LScript;
using SteamEngine.Networking;

namespace SteamEngine {
	public static class Commands {
		//method: PlayerCommand
		//this is invoked when a player types a command in game

		internal const string commandAuthorisationFailed = "No permission to run that command";

		internal static bool commandRunning;

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public static void PlayerCommand(GameState state, string command) {
			string lower = command.ToLowerInvariant();
			string noprefix;

			AbstractCharacter commandSrc = state.CharacterNotNull;

			if ((lower.StartsWith("x ") || lower.StartsWith("x."))) {
				noprefix = command.Substring(2);
			} else if ((lower.StartsWith("set ") || lower.StartsWith("set."))) {
				noprefix = command.Substring(4);
			} else {
#if DEBUG
				long ticksBefore = HighPerformanceTimer.TickCount;
#endif
				InvokeCommand(commandSrc, commandSrc, command);
#if DEBUG
				Logger.WriteDebug("Command took (in ms): " + HighPerformanceTimer.TicksToMilliseconds(HighPerformanceTimer.TickCount - ticksBefore));
#endif
				return;
			}

			if (AuthorizeCommand(commandSrc, "x")) {
				state.WriteLine(Loc<CommandLoc>.Get(state.Language).XCommandPrompt);
				state.Target(false, xCommand_Targon, xCommand_Cancel,
					new XCommandParameter(commandSrc, noprefix));
				LogCommand(commandSrc, command, true, null);
			} else {
				LogCommand(commandSrc, command, false, "No permission");
			}
		}

		//        public static void PlayerCommand(GameConn c, string command) {
		//            string lower = command.ToLower();
		//            string noprefix;

		//            AbstractCharacter commandSrc = c.CurCharacter;

		//            if ((lower.StartsWith("x ") || lower.StartsWith("x."))) {
		//                noprefix = command.Substring(2);
		//            } else if ((lower.StartsWith("set ") || lower.StartsWith("set."))) {
		//                noprefix = command.Substring(4);
		//            } else {
		//#if DEBUG
		//                long ticksBefore = HighPerformanceTimer.TickCount;
		//#endif
		//                InvokeCommand(commandSrc, commandSrc, command);
		//#if DEBUG
		//                Logger.WriteDebug("Command took (in ms): "+ HighPerformanceTimer.TicksToMilliseconds(HighPerformanceTimer.TickCount - ticksBefore));
		//#endif
		//                return;
		//            }

		//            if (AuthorizeCommand(commandSrc, "x")) {
		//                c.WriteLine("Command who or what?");
		//                c.Target(false, Commands.xCommand_Targon, Commands.xCommand_Cancel, 
		//                    new XCommandParameter(commandSrc, noprefix));
		//                LogCommand(commandSrc, command, true, null);
		//            } else {
		//                LogCommand(commandSrc, command, false, commandAuthorisationFailed);
		//            }
		//        }

		private class XCommandParameter {
			internal readonly ISrc commandSrc;
			internal readonly string commandWithoutPrefix;

			internal XCommandParameter(ISrc commandSrc, string commandWithoutPrefix) {
				this.commandSrc = commandSrc;
				this.commandWithoutPrefix = commandWithoutPrefix;
			}
		}

		//method: ConsoleCommand
		//this is invoked directly by consoles
		[SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
		public static void ConsoleCommand(ConsoleDummy consoleDummy, string command) {
			Globals.SetSrc(consoleDummy);
#if DEBUG
			long ticksBefore = HighPerformanceTimer.TickCount;
#endif
			InvokeCommand(consoleDummy, Globals.Instance, command);
#if DEBUG
			Logger.WriteDebug("Command took (in ms): " + HighPerformanceTimer.TicksToMilliseconds(HighPerformanceTimer.TickCount - ticksBefore));
#endif
		}

		private static void LogCommand(ISrc commandSrc, string command, bool success, object err) {
			if (success) {
				Console.WriteLine("'" + commandSrc.Account.Name + "' commands '" + command + "'. OK");
			} else {
				string errText = "";
				Exception e = err as Exception;
				if (e != null) {
					errText = e.Message;
				} else {
					errText = string.Concat(err);
				}
				Console.WriteLine("'" + commandSrc.Account.Name + "' commands '" + command + "'. ERR: " + errText);
				commandSrc.WriteLine(string.Format(CultureInfo.InvariantCulture,
					Loc<CommandLoc>.Get(commandSrc.Language).CommandFailed,
					command, errText));
			}
		}

		//TODO? passing just the argument name might be too primitive, but I think it should be enough
		public static bool AuthorizeCommand(ISrc commandSrc, string name) {
			if (commandRunning) {
				ScriptArgs sa = new ScriptArgs(commandSrc, name);
				if (TriggerResult.Cancel == Globals.Instance.TryCancellableTrigger(TriggerKey.command, sa)) {
					return false;
				}
			}
			return true;
		}

		public static void AuthorizeCommandThrow(ISrc commandSrc, string name) {
			if (commandRunning) {
				ScriptArgs sa = new ScriptArgs(commandSrc, name);
				if (TriggerResult.Cancel == Globals.Instance.TryCancellableTrigger(TriggerKey.command, sa)) {
					throw new SEException(commandAuthorisationFailed);
				}
			}
		}

		static CacheDictionary<string, LScriptHolder> gmCommandsCache = new CacheDictionary<string, LScriptHolder>(1000, false, StringComparer.Ordinal);

		public static void ClearGMCommandsCache() {
			gmCommandsCache.Clear();
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		private static void InvokeCommand(ISrc commandSrc, TagHolder self, string code) {
			Sanity.IfTrueThrow(commandSrc == null, "commandSrc cannot be null in Commands.InvokeCommand");

			Globals.SetSrc(commandSrc);
			try {
				commandRunning = true;
				if (commandSrc.MaxPlevel < Globals.PlevelForLscriptCommands) {
					string errText;
					bool success = SimpleCommandParser.TryRunSnippet(commandSrc, self, code, out errText);
					LogCommand(commandSrc, code, success, errText);
				} else {
					string codeAsKey = string.Concat(self == null ? typeof(void).FullName : self.GetType().FullName, code);
					LScriptHolder scriptHolder;
					if (!gmCommandsCache.TryGetValue(codeAsKey, out scriptHolder)) {
						try {
							scriptHolder = LScriptMain.GetNewSnippetRunner("<command>", 0, code);
						} catch (FatalException) {
							throw;
						} catch (Exception e) {
							LogCommand(commandSrc, code, false, e);
							return;
						}
					}

					scriptHolder.TryRun(self, (ScriptArgs) null);

					//if the command does nothing, consider it an error
					if ((scriptHolder.code is OpNode_Constant) || (scriptHolder.code is OpNode_This) || (scriptHolder.code is OpNode_Object)) {
						string errText = Loc<CommandLoc>.Get(commandSrc.Language).CommandDoesNothing;
						LogCommand(commandSrc, code, false, errText);
						return;
					}

					if (scriptHolder.lastRunSuccesful) {
						gmCommandsCache[codeAsKey] = scriptHolder;
					} else {
						gmCommandsCache.Remove(codeAsKey);
					}
					LogCommand(commandSrc, code, scriptHolder.lastRunSuccesful, scriptHolder.lastRunException);
				}
			} finally {
				commandRunning = false;
			}

			////performance test
			//string errText;
			//int n = 1000;
			//long start = HighPerformanceTimer.TickCount;
			//for (int i = 0; i<n; i++) {
			//	SimpleCommandParser.TryRunSnippet(self, code, out errText);
			//}
			//Console.WriteLine("simpleparser: "+HighPerformanceTimer.TicksToMilliseconds(HighPerformanceTimer.TickCount - start)+" ms");
			//start = HighPerformanceTimer.TickCount;
			//for (int i = 0; i<n; i++) {
			//	LScript.LScript.TryRunSnippet("<command>", 0, self, code);
			//}
			//Console.WriteLine("lscript: "+HighPerformanceTimer.TicksToMilliseconds(HighPerformanceTimer.TickCount - start)+" ms");
		}

		private static OnTargon xCommand_Targon = XCommandTargon;
		private static OnTargonCancel xCommand_Cancel = XCommandCancel;

		public static void XCommandCancel(GameState state, object parameter) {
			//?
		}

		public static void XCommandTargon(GameState state, IPoint3D getback, object parameter) {
			TagHolder self = getback as TagHolder;
			if (self != null) {
				XCommandParameter xcp = (XCommandParameter) parameter;
				InvokeCommand(xcp.commandSrc, self, xcp.commandWithoutPrefix);
			}
		}
	}

	static class SimpleCommandParser {
		static Regex commandRE = new Regex(@"(?<name>\w+)(\s+(?<arg>.+))?",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);


		/// <summary>
		/// Runs a method or function of the object self, given by name, and possible 
		/// one argument of numeric or string type, separated by space from the name
		/// </summary>
		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public static bool TryRunSnippet(ISrc commandSrc, TagHolder self, string code, out string errText) {
			Match m = commandRE.Match(code);
			if (m.Success) {
				string name = m.Groups["name"].Value;
				string arg = m.Groups["arg"].Value;
				bool haveArg = false;
				bool argIsNumber = false;
				object argAsNumber = null;

				if (!Commands.AuthorizeCommand(commandSrc, name)) {
					errText = Commands.commandAuthorisationFailed;
					return false;
				}

				if (arg.Length > 0) {
					haveArg = true;
					argIsNumber = ConvertTools.TryParseAnyNumber(arg, out argAsNumber);
				}

				ScriptHolder func = ScriptHolder.GetFunction(name);
				if (func != null) {
					if (argIsNumber) {
						func.TryRun(self, new ScriptArgs(argAsNumber));
					} else {
						func.TryRun(self, new ScriptArgs(arg));
					}
					if (func.lastRunException != null) {
						errText = func.lastRunException.Message;
					} else {
						errText = "";
					}
					return func.lastRunSuccesful;
				}

				Type argType;
				bool nameMatched;
				MethodInfo mi = FindMethod(self.GetType(), name, haveArg, argIsNumber, out argType, out nameMatched);
				if (mi != null) {
					try {
						if (haveArg) {
							if (argIsNumber) {
								mi.Invoke(self, new[] { Convert.ChangeType(argAsNumber, argType, CultureInfo.InvariantCulture) });
							} else {
								mi.Invoke(self, new object[] { arg });
							}
						} else {
							mi.Invoke(self, null);
						}
						errText = "";
						return true;
					} catch (FatalException) {
						throw;
					} catch (Exception e) {
						Logger.WriteError(e);
						errText = e.Message;
					}
				} else if (nameMatched) {
					errText = Loc<CommandLoc>.Get(commandSrc.Language).WrongCommandArgument;
				} else {
					errText = string.Format(CultureInfo.InvariantCulture,
						Loc<CommandLoc>.Get(commandSrc.Language).UnknownCommand,
						name);
				}
			} else {
				errText = Loc<CommandLoc>.Get(commandSrc.Language).WrongCommandFormat;
			}
			return false;
		}

		private static MethodInfo FindMethod(Type type, string name, bool hasArg, bool argIsNumber, out Type argType, out bool nameMatched) {
			nameMatched = false;
			argType = null;
			foreach (MethodInfo mi in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)) {
				if (StringComparer.OrdinalIgnoreCase.Equals(name, mi.Name)) {
					ParameterInfo[] pis = mi.GetParameters();
					if (hasArg) {
						if (pis.Length == 1) {
							argType = pis[0].ParameterType;
							if (argIsNumber) {
								if (ConvertTools.IsNumberType(argType)) {
									return mi;
								}
							} else if (argType.Equals(typeof(string))) {
								return mi;
							}
						}
					} else if (pis.Length == 0) {
						return mi;
					}
					nameMatched = true;
				}
			}
			return null;
		}
	}

	internal class CommandLoc : CompiledLocStringCollection<CommandLoc> {
		public string XCommandPrompt = "Command who or what?";
		public string CommandFailed = "Command '{0}' failed - {1}";
		public string WrongCommandArgument = "Wrong argument for that method";
		public string UnknownCommand = "Unknown method/function {0}";
		public string CommandDoesNothing = "This code line does nothing";
		public string WrongCommandFormat = "Unrecognized command format";
	}
}