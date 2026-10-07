/*
	self program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	self program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with self program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using SteamEngine.Common;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {
	public static class EffectFactory {
		//More detailed effects.
		public static void LightningEffectAt(IPoint4D point) {
			GraphicalEffectOutPacket p = Pool<GraphicalEffectOutPacket>.Acquire();
			p.Prepare(point, point, 1, 0, 0, 0, 0, false, false, 0, 0);
			GameServer.SendToClientsInRange(point, Globals.MaxUpdateRange, p);
		}

		[SteamFunction]
		public static void LightningEffect(Thing self) {
			GraphicalEffectOutPacket p = Pool<GraphicalEffectOutPacket>.Acquire();
			p.Prepare(self, self, 1, 0, 0, 0, 0, false, false, 0, 0);
			GameServer.SendToClientsWhoCanSee(self, p);
		}

		[SteamFunction]
		public static void StationaryEffect(Thing self, int effect, byte speed, byte duration, bool fixedDirection, bool explodes, int hue, RenderModes renderMode) {
			GraphicalEffectOutPacket p = Pool<GraphicalEffectOutPacket>.Acquire();
			p.Prepare(self, self, 3, effect, speed, duration, 0, fixedDirection, explodes, hue, renderMode);
			GameServer.SendToClientsWhoCanSee(self, p);
		}

		public static void StationaryEffect(Thing self, int effect, byte speed, byte duration) {
			GraphicalEffectOutPacket p = Pool<GraphicalEffectOutPacket>.Acquire();
			p.Prepare(self, self, 3, effect, speed, duration, 0, true, false, 0, 0);
			GameServer.SendToClientsWhoCanSee(self, p);
		}

		public static void StationaryEffectAt(IPoint4D point, int effect, byte speed, byte duration) {
			GraphicalEffectOutPacket p = Pool<GraphicalEffectOutPacket>.Acquire();
			p.Prepare(point, point, 2, effect, speed, duration, 0, true, false, 0, 0);
			GameServer.SendToClientsInRange(point, Globals.MaxUpdateRange, p);
		}

		public static void StationaryEffectAt(IPoint4D point, int effect, byte speed, byte duration, bool fixedDirection, bool explodes, int hue, RenderModes renderMode) {
			GraphicalEffectOutPacket p = Pool<GraphicalEffectOutPacket>.Acquire();
			p.Prepare(point, point, 2, effect, speed, duration, 0, fixedDirection, explodes, hue, renderMode);
			GameServer.SendToClientsInRange(point, Globals.MaxUpdateRange, p);
		}

		[SteamFunction]
		public static void EffectFromTo(IPoint4D source, IPoint4D target, int effect, byte speed, byte duration, bool fixedDirection, bool explodes, int hue, RenderModes renderMode) {
			GraphicalEffectOutPacket p = Pool<GraphicalEffectOutPacket>.Acquire();
			p.Prepare(source, target, 0, effect, speed, duration, 0, fixedDirection, explodes, hue, renderMode);
			GameServer.SendToClientsInRange(source, Globals.MaxUpdateRange, p);
		}

		public static void EffectFromTo(IPoint4D source, IPoint4D target, int effect, byte speed, byte duration, bool fixedDirection, bool explodes) {
			EffectFromTo(source, target, effect, speed, duration, fixedDirection, explodes, 0, RenderModes.Opaque);
		}
	}
}