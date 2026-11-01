namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - multiitem_create: a bundle of shafts or arrows made as one item, opened into its loose pieces.</summary>
	public static class ItemBundle {
		private static readonly TagKey bundleItemTK = TagKey.Acquire("bundleItem");
		private static readonly TagKey bundleCountTK = TagKey.Acquire("bundleCount");

		internal static void Unpack(Item bundle, Character opener, int piecesEach) {
			ItemDef pieces = ThingDef.GetByDefname(bundle.Def.GetTag(bundleItemTK) as string) as ItemDef;
			Container pack = opener.Backpack;
			if (pieces != null) {
				Item loose = (Item) pieces.Create(pack);
				loose.Amount = bundle.Amount * piecesEach;
				CraftingSkillDef.SetCrafter(loose, CraftingSkillDef.GetCrafter(bundle));
				bundle.Delete();
				pack.TryStackToAnyInside(loose);
			}
		}

		internal static int CountOf(Item bundle) {
			object count = bundle.Def.GetTag(bundleCountTK);
			return (count == null) ? 1 : System.Convert.ToInt32(count, System.Globalization.CultureInfo.InvariantCulture);
		}
	}

	public class t_shaft_multi : CompiledTriggerGroup {
		public TriggerResult On_Dclick(Item self, Character dclicker) {
			ItemBundle.Unpack(self, dclicker, ItemBundle.CountOf(self));
			return TriggerResult.Cancel;
		}
	}

	public class t_arrow_piercing_multi : CompiledTriggerGroup {
		private const int ArrowsInBundle = 20;

		public TriggerResult On_Dclick(Item self, Character dclicker) {
			ItemBundle.Unpack(self, dclicker, ArrowsInBundle);
			return TriggerResult.Cancel;
		}
	}
}
