using System.Collections.Generic;
using System.Linq;
using SteamEngine;
using Xunit;

namespace SteamEngine.Tests {
	/// <summary>
	/// Pure logic test for the context-menu entry collector (issue #15) - no engine/map
	/// dependency, so this is fast-tier without needing EngineTestFixture. The protocol wiring
	/// itself (IncomingPackets' 0xBF-0x13/0x15 subcommands, OutgoingPackets'
	/// DisplayPopupMenuOutPacket) is verified by the full-boot-tier tests still passing after
	/// this change, and real in-game look/feel needs a connected client.
	/// </summary>
	public class ContextMenuTests {
		[Fact]
		public void EmptyMenuHasNoEntries() {
			var menu = new ContextMenu();
			Assert.Equal(0, menu.Count);
			Assert.Empty(menu.Entries);
		}

		[Fact]
		public void AddedEntriesGetSequentialOneBasedIndices() {
			var menu = new ContextMenu();
			menu.Add(3000085, null);
			menu.Add(3000090, null);

			List<(ushort index, int cliloc)> entries = menu.Entries.ToList();

			Assert.Equal(2, entries.Count);
			Assert.Equal((ushort) 1, entries[0].index);
			Assert.Equal(3000085, entries[0].cliloc);
			Assert.Equal((ushort) 2, entries[1].index);
			Assert.Equal(3000090, entries[1].cliloc);
		}

		[Fact]
		public void InvokeSelected_CallsOnlyTheMatchingEntrysCallback() {
			var menu = new ContextMenu();
			bool firstCalled = false, secondCalled = false;
			menu.Add(3000085, who => firstCalled = true);
			menu.Add(3000090, who => secondCalled = true);

			menu.InvokeSelected(2, null);

			Assert.False(firstCalled);
			Assert.True(secondCalled);
		}

		[Fact]
		public void InvokeSelected_UnknownIndexCallsNothingAndDoesNotThrow() {
			var menu = new ContextMenu();
			bool called = false;
			menu.Add(3000085, who => called = true);

			menu.InvokeSelected(99, null);

			Assert.False(called);
		}

		[Fact]
		public void InvokeSelected_NullCallbackDoesNotThrow() {
			var menu = new ContextMenu();
			menu.Add(3000085, null);

			menu.InvokeSelected(1, null);
		}
	}
}
