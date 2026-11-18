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
using SteamEngine.Common;

namespace SteamEngine.Persistence {
	public abstract class DecoratedClassesSaveImplementor : ISaveImplementor {
		Type handledType;
		string headerName;

		protected DecoratedClassesSaveImplementor(Type handledType, string headerName) {
			this.handledType = handledType;
			this.headerName = headerName;
		}

		public Type HandledType {
			get {
				return this.handledType;
			}
		}

		public string HeaderName {
			get {
				return this.headerName;
			}
		}

		public abstract object LoadSection(PropsSection input);
		public abstract void Save(object objToSave, SaveStream writer);

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes"), SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "1#")]
		protected void LoadSectionLines(PropsSection ps, object loadedObject) {
			foreach (PropsLine p in ps.PropsLines) {
				try {
					this.LoadLineImpl(loadedObject, ps.Filename, p.Line, p.Name.ToLowerInvariant(), p.Value);
				} catch (FatalException) {
					throw;
				} catch (Exception ex) {
					Logger.WriteWarning(ps.Filename, p.Line, ex);
				}
			}
		}

		[SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "0#")]
		protected virtual void LoadLineImpl(object loadedObject, string filename, int line, string name, string value) {
			throw new SEException("This should not happen.");
		}
	}
}