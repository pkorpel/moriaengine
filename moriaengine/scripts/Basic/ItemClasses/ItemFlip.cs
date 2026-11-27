using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {

	/// <summary>#938 - Sphere's FLIP on an item, by what the item is. EreborNext's CItem.Flip and its checks against
	/// the real 0.99z8 are the reference. A corpse's part is in Corpse.</summary>
	public partial class Item {
		private const int LightPatternCount = 56;

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

		internal string TypeName {
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

		/// <summary>A lit thing changes its light, anything else takes the next model of its flip group. A door turns its own
		/// way (Door).</summary>
		public override void Flip(string command) {
			if (this.IsTypeLit) {
				int pattern = this.LightPattern + 1;
				this.LightPattern = (pattern >= LightPatternCount) ? 0 : pattern;
			} else {
				base.Flip(command);
			}
		}
	}
}
