namespace SteamEngine.CompiledScripts {

	/// <summary>#1426 - a player's weekly set and the player's events that count for it, Erebor's e_weekly_quest,
	/// e_weekly_itemDclick, e_weekly_itemLoot, e_weekly_stepOnWeb and @playerKill. Put on with the first set, it stays.
	/// The rules are WeeklyQuests'; the teacher, the bandage, the palantir, the poison and bleed ticks and a blow's damage
	/// still call them directly, each with its own number.</summary>
	public partial class WeeklyQuestPlugin {
		public static readonly PluginKey key = PluginKey.Acquire("_weekly_");

		/// <summary>The player's set from now on.</summary>
		internal static void Hold(Player player, WeeklySet set) {
			if (!(player.GetPlugin(key) is WeeklyQuestPlugin plugin)) {
				plugin = (WeeklyQuestPlugin) WeeklyQuestPluginDef.instance.Create();
				player.AddPlugin(key, plugin);
			}
			plugin.set = set;
		}

		public void On_Login() {
			if (this.Cont is Player player) {
				WeeklyQuests.OnLogin(player);
			}
		}

		/// <summary>Before the item's own use (the clicker's @itemDClick comes first).</summary>
		public void On_ItemDClick(AbstractCharacter clicker, AbstractItem item) {
			if (this.Cont is Player player) {
				WeeklyQuests.OnItemDClick(player, item as Item);
			}
		}

		public void On_ItemStep(AbstractCharacter stepping, AbstractItem item, bool repeated) {
			if (!repeated && (this.Cont is Player player) && (item is Item stepped)) {
				WeeklyQuests.OnStepOnItem(player, stepped);
			}
		}

		/// <summary>Lifting something: a monster's loot from its body counts.</summary>
		public void On_PickingUp(AbstractItem item) {
			if ((this.Cont is Player player) && (item.TopObj() is Corpse)) {
				WeeklyQuests.OnLoot(player, item);
			}
		}

		public void On_KillCredit(KillCreditArgs args) {
			if ((this.Cont is Player player) && (args.victim is NPC victim)) {
				WeeklyQuests.OnKillCredit(player, victim, args.share);
			}
		}
	}

	public partial class WeeklyQuestPluginDef {
		public static readonly WeeklyQuestPluginDef instance =
			(WeeklyQuestPluginDef) new WeeklyQuestPluginDef("p_weeklyQuests", "C# scripts", -1).Register();
	}
}
