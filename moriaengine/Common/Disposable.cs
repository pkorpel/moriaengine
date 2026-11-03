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

namespace SteamEngine.Common {
	//taken from http://www.geocities.com/Jeff_Louie/OOP/oop28.htm

	public abstract class Disposable : IDisposable {
		internal bool disposed;

		// subclass should to implement these two methods
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		protected virtual void On_DisposeManagedResources() {

		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		protected virtual void On_DisposeUnmanagedResources() {

		}

		/// <summary>#100 - after both of the above and after the object counts as disposed: where a pooled object goes back to its
		/// pool, so nothing of the disposal still runs on it once someone else may have it.</summary>
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		protected virtual void On_AfterDispose() {

		}

		[SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
		public virtual void Dispose() {
			this.Dispose(true);
			GC.SuppressFinalize(this); // remove this from gc finalizer list
		}

		protected void ThrowIfDisposed() {
			if (this.disposed) {
				throw new SEException(this + " disposed");
			}
		}

		public bool IsDisposed {
			get {
				return this.disposed;
			}
		}

		//#100 - two threads could both pass the old outer check and dispose (and so pool) one object twice; and a pooled object
		//went back to its pool before its unmanaged part was disposed, so a connection reused meanwhile lost its new socket
		private void Dispose(bool disposing) {
			lock (this) {
				if (this.disposed) {
					return;
				}
				if (disposing) {// called from Dispose
					this.On_DisposeManagedResources();
				}
				this.On_DisposeUnmanagedResources();
				this.disposed = true;
			}
			if (disposing) {
				this.On_AfterDispose();
			}
		}

		~Disposable() // maps to finalize
		{
			this.Dispose(false);
		}
	}
}
