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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SteamEngine.Common;
using SteamEngine.LScript;

namespace SteamEngine.CompiledScripts {

	//it's not a typical def, but then again, who cares :P
	public sealed class TemplateDef : AbstractDef, IThingFactory {
		public const bool TemplateTracingOn = false;

		private FieldValue container;
		private LScriptHolder holder;
		private static ItemDef defaultContainer;

		#region Accessors
		public new static TemplateDef GetByDefname(string defname) {
			return AbstractScript.GetByDefname(defname) as TemplateDef;
		}


		public ItemDef Container {
			get {
				return (ItemDef) this.container.CurrentValue;
			}
			set {
				this.container.CurrentValue = value;
			}
		}

		public ItemDef DefaultContainer {
			get {
				if (defaultContainer == null) {
					defaultContainer = (ItemDef) ThingDef.GetByDefname("i_bag");
					if (defaultContainer == null) {
						throw new ScriptException("Missing TemplateDef's default container itemdef (i_bag)");
					}
				}
				return defaultContainer;
			}
		}
		#endregion Accessors

		#region Loading from scripts
		public TemplateDef(string defname, string filename, int headerLine)
			:
				base(defname, filename, headerLine) {
			this.container = this.InitThingDefField("container", null, typeof(ItemDef));
		}

		public new static void Bootstrap() {
			ScriptLoader.RegisterScriptType("TemplateDef",
				LoadFromScripts, true);
		}
		public override void LoadScriptLines(PropsSection ps) {
			ParseText(ps, this); //is static because it was that way before a little overhaul, and there's no reason for rewriting really

			this.LoadFromSaves(ps); //would try to load the lines as fieldvalues... we already did that
		}

		private static void ParseText(PropsSection input, TemplateDef td) {
			TriggerSection trigger = input.GetTrigger(0);
			//try to read container and/or second defname from the start of the text
			bool altdefnamedefined = false;
			bool containerdefined = false;
			StringBuilder newCode = new StringBuilder();
			StringReader reader = new StringReader(trigger.Code.ToString());
			int linenumber = input.HeaderLine;
			while (true) {
				string line = reader.ReadLine();
				linenumber++;
				if (line != null) {
					line = line.Trim();
					if ((line.Length == 0) || (line.StartsWith("//"))) {//it is a comment or a blank line
						newCode.Append(Environment.NewLine);
						continue;
					}

					Match m = LocStringCollection.valueRE.Match(line);
					if (m.Success) {
						GroupCollection gc = m.Groups;
						string name = gc["name"].Value;
						if (StringComparer.OrdinalIgnoreCase.Equals(name, "defname")) {//set altdefname
							if (altdefnamedefined) {
								Logger.WriteWarning(input.Filename, linenumber, "Alternative defname already defined, ignoring.");
							} else {
								string altdefname = gc["value"].Value;
								altdefname = string.Intern(ConvertTools.LoadSimpleQuotedString(altdefname));
								AbstractScript def = AbstractScript.GetByDefname(altdefname);
								TemplateDef t = def as TemplateDef;
								if (t == null) { //is null or isnt TeplateDef
									if (def != null) {//it isnt TemplateDef
										td.Unload();
										td.Unregister();
										throw new OverrideNotAllowedException(
											"TemplateDef " + LogStr.Ident(td.Defname) + " has the same name as " + LogStr.Ident(def) + ". Ignoring.");
									}
									td.Altdefname = altdefname;
									td.Unregister(); //will be reregistred later by AbstractDef
								} else if (t == td) {
									Logger.WriteWarning(input.Filename, linenumber,
										"Defname redundantly specified for TemplateDef " + LogStr.Ident(altdefname) + ".");
								} else {
									td.Unload();
									td.Unregister();
									throw new OverrideNotAllowedException("TemplateDef " + LogStr.Ident(altdefname) + " defined multiple times.");
								}
								altdefnamedefined = true;
							}
						} else if (StringComparer.OrdinalIgnoreCase.Equals(name, "container")) {//set container itemdef
							if (containerdefined) {
								Logger.WriteWarning(input.Filename, linenumber, "Container id already defined, ignoring.");
							} else {
								td.container.SetFromScripts(input.Filename, linenumber, gc["value"].Value);
								containerdefined = true;
							}
						} else if (StringComparer.OrdinalIgnoreCase.Equals(name, "CATEGORY")) {//ignoring axis thingies
						} else if (StringComparer.OrdinalIgnoreCase.Equals(name, "SUBSECTION")) {//ignoring axis thingies
						} else if (StringComparer.OrdinalIgnoreCase.Equals(name, "DESCRIPTION")) {//ignoring axis thingies
						} else {
							newCode.Append(line);
						}
					} else {
						newCode.Append(line);
					}
					newCode.Append(Environment.NewLine);
				} else {//end of section code
					break;
				}
			}

			//so we have read the important fields, now read the rest as lscript function

			trigger.Code = newCode;
			td.holder = new LScriptHolder(trigger);
			if (input.TriggerCount > 1) {
				Logger.WriteWarning(input.Filename, input.HeaderLine, "Triggers in a template are nonsensual (and ignored).");
			}
		}
		#endregion Loading from scripts

		#region factory methods
		public Thing Create(IPoint4D p) {
			return this.Create(p.X, p.Y, p.Z, p.M);
		}

		public Thing Create(int x, int y, int z, byte m) {
			this.ThrowIfUnloaded();
			ItemDef contDef = this.Container;
			if (contDef == null) {
				contDef = this.DefaultContainer;
			}
			Thing cont = contDef.Create(x, y, z, m);
			this.holder.Run(cont, (ScriptArgs) null);
			return cont;
		}

		public Thing Create(Thing cont) {
			this.ThrowIfUnloaded();
			ItemDef contDef = this.Container;
			if (cont.IsChar) {
				cont = ((Character) cont).Backpack;
			}
			if (contDef != null) {
				cont = contDef.Create(cont);
			} //else we put the item(s) in the given container
			this.holder.Run(cont, (ScriptArgs) null);
			return cont;//we return the container even in case we didn't create it
		}
		#endregion factory methods
	}
}
