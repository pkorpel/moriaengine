using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1070 - Erebor's small icons at the top left (charge.scp's d_charge, splitshot.scp's d_splitShot,
	/// crushingArrow.scp's d_crushingArrow), side by side 50 apart. Erebor counted open icons in tag d_count; here
	/// each open one keeps its slot, and a new one takes the first free.</summary>
	public static class AbilityIcons {
		internal const int Fire = 1, Mind = 2, Energy = 3, RoundAttack = 10, KillingSpree = 20;
		private const int SlotWidth = 50;
		private static readonly TagKey chargeSlotTK = TagKey.Acquire("_iconSlotCharge_");
		private static readonly TagKey splitShotSlotTK = TagKey.Acquire("_iconSlotSplitShot_");
		private static readonly TagKey crushingSlotTK = TagKey.Acquire("_iconSlotCrushing_");

		/// <summary>def_charge_icon: the gump of each charge.</summary>
		internal static int ChargeGump(int kind) {
			switch (kind) {
				case Fire:
					return 0x8d1;
				case Mind:
					return 0x8de;
				case Energy:
					return 0x8e9;
				case RoundAttack:
					return 0x520c;
				default:
					return 0x5008;
			}
		}

		/// <summary>def_charge_size_icon: the small thing shown once per charge.</summary>
		internal static int ChargeTile(int kind) {
			switch (kind) {
				case Fire:
					return 0x186e;
				case Mind:
					return 0x186d;
				case KillingSpree:
					return 0x1ae4;
				default:
					return 0x1870;
			}
		}

		internal static int KindOf(MagicChargeSchool school) {
			switch (school) {
				case MagicChargeSchool.Fire:
					return Fire;
				case MagicChargeSchool.Energy:
					return Energy;
				default:
					return Mind;
			}
		}

		/// <summary>f_charge_increase/f_charge_decrease's dialogclose(d_charge,1): shown anew with the count, or closed at 0.</summary>
		internal static void ShowCharge(Character ch, int kind, int count) {
			if (count <= 0) {
				HideCharge(ch);
			} else {
				Show(ch, SingletonScript<D_ChargeIcon>.Instance, chargeSlotTK, kind, count);
			}
		}

		internal static void HideCharge(Character ch) {
			Hide(ch, SingletonScript<D_ChargeIcon>.Instance, chargeSlotTK);
		}

		internal static void ShowSplitShot(Character ch) {
			Show(ch, SingletonScript<D_SplitShotIcon>.Instance, splitShotSlotTK, 0, 0);
		}

		internal static void HideSplitShot(Character ch) {
			Hide(ch, SingletonScript<D_SplitShotIcon>.Instance, splitShotSlotTK);
		}

		internal static void ShowCrushingArrow(Character ch) {
			Show(ch, SingletonScript<D_CrushingArrowIcon>.Instance, crushingSlotTK, 0, 0);
		}

		internal static void HideCrushingArrow(Character ch) {
			Hide(ch, SingletonScript<D_CrushingArrowIcon>.Instance, crushingSlotTK);
		}

		private static void Show(Character ch, CompiledGumpDef def, TagKey slotTK, int kind, int count) {
			if (!(ch is Player player) || (player.GameState == null)) {
				return;
			}
			int slot = (player.GetTag(slotTK) is int own) ? own : FreeSlot(player);
			player.DialogForget(def);
			player.SetTag(slotTK, slot);
			player.Dialog(player, def, new DialogArgs(slot * SlotWidth, kind, count));
		}

		private static void Hide(Character ch, CompiledGumpDef def, TagKey slotTK) {
			if (ch is Player player) {
				player.RemoveTag(slotTK);
				player.DialogForget(def);
			}
		}

		private static int FreeSlot(Player player) {
			for (int slot = 0; ; slot++) {
				if (!Equals(player.GetTag(chargeSlotTK), slot) && !Equals(player.GetTag(splitShotSlotTK), slot) && !Equals(player.GetTag(crushingSlotTK), slot)) {
					return slot;
				}
			}
		}
	}

	/// <summary>d_charge: the charge's picture and a small one per charge. args: offset, kind, count.</summary>
	public class D_ChargeIcon : CompiledGumpDef {
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			int offset = (int) args[0], kind = (int) args[1], count = (int) args[2];
			this.SetLocation(10, 24);
			this.NoClose();
			this.NoMove();
			this.AddPage(0);
			this.AddImage(14 + offset, 0, AbilityIcons.ChargeGump(kind));
			for (int i = 0; i < count; i++) {
				this.TilePic((i * 14) + offset, 45, AbilityIcons.ChargeTile(kind));
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
		}
	}

	/// <summary>d_splitShot, while a split shot is readied.</summary>
	public class D_SplitShotIcon : CompiledGumpDef {
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			this.SetLocation(10, 24);
			this.NoClose();
			this.AddPage(0);
			this.AddImage(14 + (int) args[0], 0, 0x5215);
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
		}
	}

	/// <summary>d_crushingArrow, while a crushing arrow is readied.</summary>
	public class D_CrushingArrowIcon : CompiledGumpDef {
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			this.SetLocation(10, 24);
			this.NoClose();
			this.AddPage(0);
			this.AddImage(14 + (int) args[0], 0, 0x5209);
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
		}
	}
}
