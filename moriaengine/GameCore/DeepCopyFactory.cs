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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using SteamEngine.Common;
using SteamEngine.CompiledScripts;
using SteamEngine.Persistence;
using SteamEngine.Regions;

namespace SteamEngine {

	public delegate void ReturnCopy(object copy);
	public delegate void ReturnCopyParam(object copy, object additionalParameter);

	//public interface IDeepCloneable {
	//    object DeepClone();
	//}

	public interface IDeepCopyImplementor {
		object DeepCopy(object copyFrom);
		Type HandledType { get; }
	}

	public static class DeepCopyFactory {
		private static int recursionLevel;

		private static DelayedCopier firstCopier; //this is the list of the pending jobs :)
		private static DelayedCopier lastCopier;

		private static HashSet<object> toBeCopied = new HashSet<object>();
		private static Dictionary<object, object> copies = new Dictionary<object, object>(new ObjectSaver.ReferenceEqualityComparer());

		private static Dictionary<Type, IDeepCopyImplementor> implementors = new Dictionary<Type, IDeepCopyImplementor>();

		public static void Bootstrap() {
			ClassManager.RegisterSupplySubclassInstances<IDeepCopyImplementor>(RegisterImplementor, true, true);
		}

		//called by ClassManager
		internal static void RegisterImplementor(IDeepCopyImplementor implementor) {
			Type type = implementor.HandledType;
			if (implementors.ContainsKey(type)) {
				throw new OverrideNotAllowedException("There is already a IDeepCopyImplementor (" + implementors[type] + ") registered for handling the type " + type);
			}
			implementors[type] = implementor;
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public static bool IsNotCopied(Type type) {
			return (type.IsEnum
				|| ObjectSaver.IsSimpleSaveableType(type) //numbers, datetime, timespan, etc.
				|| typeof(AbstractScript).IsAssignableFrom(type)
				|| typeof(Region).IsAssignableFrom(type)
				|| typeof(Globals).IsAssignableFrom(type)
				|| typeof(IAbstractKey).IsAssignableFrom(type)
				|| typeof(AbstractAccount).IsAssignableFrom(type));
		}

		public static object GetCopy(object copyFrom) {
			if (copyFrom == null) {
				return null;
			}
			if (toBeCopied.Contains(copyFrom)) {
				throw new SEException("You can't obtain a copy of an object that is already pending to be copied in this copy session. Use the -Delayed method here instead.");
			}
			object copy;
			if (copies.TryGetValue(copyFrom, out copy)) {
				return copy;
			}
			return CopyImplementation(copyFrom);
		}

		public static void GetCopyDelayed(object copyFrom, ReturnCopy callback) {
			if (copyFrom == null) {
				callback(null);
				return;
			}
			//if (copyFrom is Thing) {
			//    callback(copyFrom);
			//}
			object copy;
			if (copies.TryGetValue(copyFrom, out copy)) {
				callback(copy);
				return;
			}
			if ((copyFrom is Thing) || (toBeCopied.Contains(copyFrom))) {
				//it is being copied elsewhere, in some other recursion level, or it's Thing which is special
				PushDelayedCopier(new DelayedCopier_NoParam(copyFrom, callback));
			} else {
				toBeCopied.Add(copyFrom);
				copy = CopyImplementation(copyFrom);
				toBeCopied.Remove(copyFrom);
				callback(copy);
			}
		}

		public static void GetCopyDelayed(object copyFrom, ReturnCopyParam callback, object additionalParameter) {
			if (copyFrom == null) {
				callback(null, additionalParameter);
				return;
			}
			object copy;
			if (copies.TryGetValue(copyFrom, out copy)) {
				callback(copy, additionalParameter);
				return;
			}
			if ((copyFrom is Thing) || (toBeCopied.Contains(copyFrom))) {
				//it is being copied elsewhere, in some other recursion level, or it's Thing which is special
				PushDelayedCopier(new DelayedCopier_Param(copyFrom, callback, additionalParameter));
			} else {
				toBeCopied.Add(copyFrom);
				copy = CopyImplementation(copyFrom);
				toBeCopied.Remove(copyFrom);
				callback(copy, additionalParameter);
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		private static object CopyImplementation(object copyFrom) {
			recursionLevel++;

			bool noException = false;
			try {
				IDeepCopyImplementor implementor;
				Type type = copyFrom.GetType();

				if (!implementors.TryGetValue(type, out implementor)) {
					//simplesaveable = immutable.
					if (IsNotCopied(type)) {
						implementor = new CopyImplementor_NoCopyNeeded();
						implementors[type] = implementor;
					} else {
						throw new SEException("We don't know how to copy the type " + type);
					}
				}

				object copy = implementor.DeepCopy(copyFrom);
				copies[copyFrom] = copy;
				noException = true;
				return copy;
			} finally {
				recursionLevel--;
				if (recursionLevel == 0) {
					if (noException) {
						while (DelayedCopierListIsNotEmpty) {
							DelayedCopier copier = null;
							try {
								copier = PopDelayedCopier();
								copier.Run();
							} catch (FatalException) {
								throw;
							} catch (Exception e) {
								Logger.WriteError("While deep-copying object " + copier.copyFrom, e);
							}
						}
					}

					firstCopier = null;
					lastCopier = null;

					toBeCopied.Clear();
					copies.Clear();
				}
			}
		}

		private class CopyImplementor_NoCopyNeeded : IDeepCopyImplementor {
			public object DeepCopy(object copyFrom) {
				return copyFrom;
			}

			public Type HandledType {
				get { throw new SEException("This should not happen."); }
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible"), SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
		public class CopyImplementor_UseICloneable : IDeepCopyImplementor {
			[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
			public object DeepCopy(object copyFrom) {
				return ((ICloneable) copyFrom).Clone();
			}

			public Type HandledType {
				get { throw new SEException("This should not happen."); }
			}
		}

		private abstract class DelayedCopier {
			internal DelayedCopier next;//instances will be all stored in a linked list
			internal object copyFrom;

			internal DelayedCopier(object copyFrom) {
				this.copyFrom = copyFrom;
			}

			internal abstract void Run();
		}

		private class DelayedCopier_NoParam : DelayedCopier {
			protected ReturnCopy deleg;

			internal DelayedCopier_NoParam(object copyFrom, ReturnCopy deleg)
				: base(copyFrom) {
				this.deleg = deleg;
			}

			internal override void Run() {
				object copy;
				if (copies.TryGetValue(this.copyFrom, out copy)) {
					this.deleg(copy);
					return;
				}
				if (this.copyFrom is Thing) {
					this.deleg(this.copyFrom);
					return;
				}
				throw new NonExistingObjectException("The object '" + this.copyFrom + "' has not been copied. Copy requested by " + this.deleg.Target);
			}
		}

		private class DelayedCopier_Param : DelayedCopier {
			protected ReturnCopyParam deleg;
			protected object param;

			internal DelayedCopier_Param(object copyFrom, ReturnCopyParam deleg, object param)
				: base(copyFrom) {
				this.deleg = deleg;
				this.param = param;
			}

			internal override void Run() {
				object copy;
				if (copies.TryGetValue(this.copyFrom, out copy)) {
					this.deleg(copy, this.param);
					return;
				}
				if (this.copyFrom is Thing) {
					this.deleg(this.copyFrom, this.param);
					return;
				}
				throw new NonExistingObjectException("The object '" + this.copyFrom + "' has not been copied. Copy requested by " + this.deleg.Target);
			}
		}

		private static bool DelayedCopierListIsNotEmpty {
			get {
				return firstCopier != null;
			}
		}

		private static void PushDelayedCopier(DelayedCopier dl) {
			if (firstCopier == null) {
				firstCopier = dl;
			}
			if (lastCopier != null) {
				lastCopier.next = dl;
			}
			lastCopier = dl;
		}

		private static DelayedCopier PopDelayedCopier() {
			DelayedCopier dl = firstCopier;
			firstCopier = firstCopier.next;//throws nullpointerexc...
			return dl;
		}

		internal static void ForgetScripts() {
			Dictionary<Type, IDeepCopyImplementor> copiedList = new Dictionary<Type, IDeepCopyImplementor>(implementors);

			foreach (KeyValuePair<Type, IDeepCopyImplementor> pair in copiedList) {

				IDeepCopyImplementor idci = pair.Value;
				if (!(idci is CopyImplementor_UseICloneable)) {
					implementors.Remove(pair.Key);
				}
			}
		}
	}
}