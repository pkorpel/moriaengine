using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.Communication {

	public class Buffer : Poolable {
		public const int bufferLen = 50 * 1024;

		public readonly byte[] bytes;

		public Buffer() {
			this.bytes = new byte[bufferLen];
		}

		public Buffer(int len) {
			this.bytes = new byte[len];
		}

		protected override void On_DisposeManagedResources() {
			if (this.bytes.Length == bufferLen) {
				base.On_DisposeManagedResources();
			}
		}
	}

	public class ListBuffer<T> : Poolable {
		public readonly List<T> list = new List<T>();

		protected override void On_Reset() {
			this.list.Clear();

			base.On_Reset();
		}
	}
}
