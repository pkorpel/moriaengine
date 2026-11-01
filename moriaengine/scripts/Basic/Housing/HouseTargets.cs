namespace SteamEngine.CompiledScripts {

	/// <summary>#941 - i_target_house: who becomes a friend, co-owner or the owner. Whoever has a place in the house
	/// loses it first.</summary>
	public class Targ_HouseMember : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			object[] args = (object[]) parameter;
			House house = (House) args[0];
			HouseRank made = (HouseRank) args[1];
			HouseRank current = house.RankOf(targetted);
			if (!(targetted is Player)) {
				self.SysMessage("Toto neni hrac.");
			} else if ((current != HouseRank.Public) && (current < HouseRank.GM)) {
				self.SysMessage("Nejprve bys mu musel odebrat domovni funkci.");
			} else if (made == HouseRank.Owner) {
				house.Owner = targetted;
				self.SysMessage("Vlastnictvi predano.");
				targetted.SysMessage("Stal jsi se vlastnikem domu " + house.Title + ".");
			} else {
				house.AddMember(targetted, made);
				string name = (made == HouseRank.CoOwner) ? "spoluvlastnik" : "pritel";
				self.SysMessage(targetted.Name + " uspesne pridan jako " + name);
				targetted.SysMessage("Stal jsi se " + name + "em domu " + house.Title + ".");
			}
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			return TargetResult.Done;
		}
	}
}
