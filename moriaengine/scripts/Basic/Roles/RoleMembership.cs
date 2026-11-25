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
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	public partial class Role {

		[SaveableClass]
		public class RoleMembership : IRoleMembership {
			private Character member;
			private Role cont;

			public RoleMembership(Character member, Role cont) {
				this.member = member;
				this.cont = cont;
			}

			public Character Member {
				get { return this.member; }
			}

			public Role Cont {
				get {
					return this.cont;
				}
			}

			public void Dispose() {
			}

			[LoadSection]
			public RoleMembership(PropsSection input) {
				int currentLineNumber = input.HeaderLine;
				try {
					PropsLine pl = input.PopPropsLine("member");
					currentLineNumber = pl.Line;
					ObjectSaver.Load(pl.Value, this.Load_RoleMember, input.Filename, pl.Line);

					pl = input.PopPropsLine("cont");
					currentLineNumber = pl.Line;
					ObjectSaver.Load(pl.Value, this.Load_RoleCont, input.Filename, pl.Line);


					foreach (PropsLine p in input.PropsLines) {
						try {
							this.LoadLine(input.Filename, p.Line, p.Name.ToLowerInvariant(), p.Value);
						} catch (FatalException) {
							throw;
						} catch (Exception ex) {
							Logger.WriteWarning(input.Filename, p.Line, ex);
						}
					}

				} catch (FatalException) {
					throw;
				} catch (SEException sex) {
					sex.TryAddFileLineInfo(input.Filename, currentLineNumber);
					throw;
				} catch (Exception e) {
					throw new SEException(input.Filename, currentLineNumber, e);
				}
			}

			protected virtual void LoadLine(string filename, int line, string valueName, string valueString) {
				throw new ScriptException("Invalid data '" + LogStr.Ident(valueName) + "' = '" + LogStr.Number(valueString) + "'.");
			}

			[Save]
			public virtual void Save(SaveStream output) {
				output.WriteValue("member", this.member);
				output.WriteValue("cont", this.cont);
			}

			private void Load_RoleMember(object resolvedObject, string filename, int line) {
				this.member = (Character) resolvedObject;
			}

			private void Load_RoleCont(object resolvedObject, string filename, int line) {
				this.cont = (Role) resolvedObject;
				this.cont.InternalLoadMembership(this);
			}
		}
	}
}
