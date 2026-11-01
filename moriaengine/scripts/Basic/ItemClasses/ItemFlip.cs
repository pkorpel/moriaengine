using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {

	/// <summary>#938 - Sphere's FLIP on an item, by what the item is. EreborNext's CItem.Flip and its checks against
	/// the real 0.99z8 are the reference. A corpse's part is in Corpse.</summary>
	public partial class Item {
		private const int LightPatternCount = 56;
		private const int DoorOffsetMax = 0xF, DoorOpened = 0x1, DoorRightLeft = 0x2, DoorInOut = 0x4, DoorAlign = 0x8;

		private static readonly TagKey lightPatternTK = TagKey.Acquire("_light_pattern_");

		public bool IsTypeLit {
			get {
				switch (this.TypeName) {
					case "t_spell":
					case "t_fire":
					case "t_light_lit":
					case "t_campfire":
					case "t_lava":
					case "t_window":
						return true;
				}
				return false;
			}
		}

		public bool IsDoor {
			get {
				switch (this.TypeName) {
					case "t_door":
					case "t_door_locked":
					case "t_door_open":
						return true;
				}
				return false;
			}
		}

		/// <summary>What the OpenDoor macro looks for: an open door's type stays t_door, only t_door_open is left out.</summary>
		public bool IsMacroDoor {
			get {
				string type = this.TypeName;
				return (type == "t_door") || (type == "t_door_locked");
			}
		}

		private string TypeName {
			get {
				return (this.Type == null) ? null : this.Type.Defname.ToLowerInvariant();
			}
		}

		/// <summary>Which of the client's light shapes it casts: the def's, until a flip gives it its own.</summary>
		public int LightPattern {
			get {
				object pattern = this.GetTag(lightPatternTK);
				return (pattern == null) ? (int) this.TypeDef.Light : (int) pattern;
			}
			set {
				ItemSyncQueue.AboutToChange(this);
				this.SetTag(lightPatternTK, value);
			}
		}

		public override bool IsMovableType {
			get {
				return base.IsMovableType && (this.HasAttribute(ItemAttributes.attr_move_always) || !this.HasAttribute(ItemAttributes.attr_invis));
			}
		}

		/// <summary>A lit thing changes its light, a door turns by its offset from the base model (ALIGN, HAND and SWING
		/// flip one bit of it), anything else takes the next model of its flip group.</summary>
		public override void Flip(string command) {
			if (this.IsTypeLit) {
				int pattern = this.LightPattern + 1;
				this.LightPattern = (pattern >= LightPatternCount) ? 0 : pattern;
			} else if (this.IsDoor) {
				this.Model = this.TypeDef.Model + NextDoorOffset(this.DoorOffset, command);
			} else {
				base.Flip(command);
			}
		}

		private static int NextDoorOffset(int offset, string command) {
			switch ((command ?? "").ToUpperInvariant()) {
				case "ALIGN":
					return offset ^ DoorAlign;
				case "HAND":
					return offset ^ DoorRightLeft;
				case "SWING":
					return offset ^ DoorInOut;
				default:
					return ((offset & ~DoorOpened) + 2) % 16;
			}
		}
	}
}
