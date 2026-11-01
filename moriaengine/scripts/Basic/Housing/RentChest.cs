using SteamEngine.Common;
using SteamEngine.Communication.TCP;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	public partial class RentChestDef {
	}

	/// <summary>#941 - i_najem_chest: anyone opens it and puts money in, which buys time when someone clicks it or the sign, as on
	/// Erebor. It takes nothing else, as nothing else pays, and only a GM takes anything out, for what an eviction left in it.
	/// Erebor kept it locked (#958).</summary>
	[ViewableClass]
	public partial class RentChest {
		private static readonly TagKey houseTK = TagKey.Acquire("_rent_chest_house_");

		public House House {
			get {
				House house = this.GetTag(houseTK) as House;
				return ((house == null) || house.IsDeleted) ? null : house;
			}
			set {
				this.SetTag(houseTK, value);
			}
		}

		private void Tell(AbstractCharacter clicker) {
			Character ch = clicker as Character;
			House house = this.House;
			if ((ch != null) && (house != null)) {
				house.CheckRent(ch);
				this.ClickMessage(ch, "Mesicni najem domu cislo " + house.Number + " cini " + house.Rent + " gp.");
				this.ClickMessage(ch, "Najem je zaplacen na " + house.PaidDays + " dni dopredu.");
			}
		}

		public override void On_Click(AbstractCharacter clicker, GameState clickerState, TcpConnection<GameState> clickerConn) {
			base.On_Click(clicker, clickerState, clickerConn);
			this.Tell(clicker);
		}

		public override void On_AosClick(AbstractCharacter clicker, GameState clickerState, TcpConnection<GameState> clickerConn) {
			base.On_AosClick(clicker, clickerState, clickerConn);
			this.Tell(clicker);
		}

		public override void On_DClick(AbstractCharacter from) {
			this.Tell(from);
			base.On_DClick(from);
		}

		public override TriggerResult On_DenyPutItemIn(DenyPutInItemArgs args) {
			Character ch = args.PickingChar as Character;
			Item item = args.ManipulatedItem as Item;
			if ((ch != null) && !ch.IsGM && (item != null) && !House.PaysRent(item)) {
				args.Result = new DenyResult_StringSysMessage("Do truhly patri jen penize.");
				return TriggerResult.Cancel;
			}
			return base.On_DenyPutItemIn(args);
		}

		public override void On_DenyPickupItemFrom(DenyPickupArgs args) {
			Character ch = args.PickingChar as Character;
			if ((ch != null) && !ch.IsGM) {
				args.Result = new DenyResult_StringSysMessage("Co je v truhle, uz patri k najmu.");
				return;
			}
			base.On_DenyPickupItemFrom(args);
		}
	}
}
