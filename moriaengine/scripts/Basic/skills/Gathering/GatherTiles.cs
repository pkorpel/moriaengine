namespace SteamEngine.CompiledScripts {

	/// <summary>#958 - what's a tree or a rock to gather from. As in Sphere a static counts by its item type, and a DUPEITEM has its
	/// master's: most cave floor is a dupe of the one def saying t_rock, so the flags or the land under it can't tell.</summary>
	public static class GatherTiles {
		public static bool IsOfType(AbstractInternalItem target, string typeDefname) {
			AbstractItemDef def = ThingDef.FindItemDef(target.Id);
			while (def != null) {
				ItemDef itemDef = def as ItemDef;
				if ((itemDef != null) && (itemDef.Type != null) && (itemDef.Type.Defname == typeDefname)) {
					return true;
				}
				AbstractItemDef master = def.DupeItem;
				def = (master == def) ? null : master;
			}
			return false;
		}
	}
}
