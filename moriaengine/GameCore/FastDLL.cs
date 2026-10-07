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

/*	(SL)

The .NET documentation says that if you pin something, you should unpin it ASAP or you'll reduce performance.
However, in this file, we pin our two packet data arrays. Their size never changes, and they are
64 KB in size each, and by pinning them, we ensure that their addresses won't ever change, so we can pass those addresses
to FastDLL once, and avoid the cost of passing them every time we call the compression method in it. And the GC
shouldn't have any real reason to move 64 KB arrays around in memory either.

In this way, we counterintuitively gain performance by pinning these objects.

(Unfortunately, pinning integers doesn't work as well!)

-SL
*/

#if USEFASTDLL
using System.Runtime.InteropServices;
//Test fix. #warning FastDLL isn't working right now, you should probably run without it until I get it fixed (You should delete the fastdll files in bin and then run distrib/compile.bat to rebuild everything (it won't rebuild fastdll, don't worry)).
#endif

namespace SteamEngine {
	internal static class FastDLL {
		internal static void ShutDownFastDLL() {
#if USEFASTDLL
			inBufferHandle.Free();
			outBufferHandle.Free();
#endif
		}

#if USEFASTDLL
			private static GCHandle outBufferHandle;
			private static GCHandle inBufferHandle;
			private static void PrepareToInitBuffers() {
				//outBufferHandle = GCHandle.Alloc(Server._out.outpacket, GCHandleType.Pinned);
				//inBufferHandle = GCHandle.Alloc(Server._out.opacket, GCHandleType.Pinned);
				outBufferHandle = GCHandle.Alloc(PacketSender.cBuffer, GCHandleType.Pinned);
				inBufferHandle = GCHandle.Alloc(PacketSender.ucBuffer, GCHandleType.Pinned);
				
				
			}
			
#if DEBUG
				private const string FastDLLPath = "bin/Debug_fastdll.dll";
#else
				private const string FastDLLPath = "bin/fastdll.dll";
#endif
			
			[DllImport(FastDLLPath)]
			public static extern void InitBuffers(IntPtr unpacked, IntPtr retval);
			
			[DllImport(FastDLLPath)]
			public static extern void SetupTables();
			
			[DllImport(FastDLLPath)]
			public static extern int Crushify1(byte[] unpacked, byte[] retval, int retstart, int len);
			
			[DllImport(FastDLLPath)]
			public static extern int Crushify2(byte[] unpacked, byte[] retval, int retstart, int len);
			
			[DllImport(FastDLLPath)]
			public static extern int Crushify3(byte[] unpacked, byte[] retval, int retstart, int len);
			
			[DllImport(FastDLLPath)]
			public static extern int Crushify4(byte[] unpacked, byte[] retval, int retstart, int len);
			
			[DllImport(FastDLLPath)]
			public static extern int Crushify5(byte[] unpacked, byte[] retval, int retstart, int len);
			
			[DllImport(FastDLLPath)]
			public static extern int Crushify6(byte[] unpacked, byte[] retval, int retstart, int len);
			
			[DllImport(FastDLLPath)]
			public static extern int Crushify7(byte[] unpacked, byte[] retval, int retstart, int len);
			
			[DllImport(FastDLLPath)]
			public static extern int Crushify8(int retstart, int len);
			
			//For the added ability to start reading the uncompressed packet data at a specified position.
			[DllImport(FastDLLPath)]
			public static extern int Crushify8S(int instart, int retstart, int len);
			
			[DllImport(FastDLLPath)]
			public static extern int Crushify10(int retstart, int len);
			[DllImport(FastDLLPath)]
			public static extern int Crushify10no3(int retstart, int len);
			
			[DllImport(FastDLLPath)]
			public static extern int Crushify11(int retstart, int len);
			[DllImport(FastDLLPath)]
			public static extern int Crushify12(int retstart, int len);
			[DllImport(FastDLLPath)]
			public static extern int Crushify13(int retstart, int len);
			[DllImport(FastDLLPath)]
			public static extern int Crushify13no10(int retstart, int len);
			[DllImport(FastDLLPath)]
			public static extern int Crushify14(int retstart, int len);
			
#endif

	}
}
