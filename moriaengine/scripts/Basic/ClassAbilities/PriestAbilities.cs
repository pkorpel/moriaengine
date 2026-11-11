using System;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.Communication.TCP;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Networking;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	public partial class LifeCandleDef {
	}

	/// <summary>#1017 - i_svice_zivota and i_svice_zivota_lit: a priest lights one, and for a day it can bring
	/// them back from the dead. The lit one is attr_newbie, so it stays in the pack through death.</summary>
	[ViewableClass]
	public partial class LifeCandle {
		internal const string LitDef = "i_svice_zivota_lit";
		internal static readonly TimerKey BurningTK = TimerKey.Acquire("_lifeCandleBurning_");

		private bool IsLit {
			get { return this.Def == ThingDef.GetByDefname(LitDef); }
		}

		public override void On_Create() {
			base.On_Create();
			if (this.IsLit) {
				this.Color = 0x842;
				this.SetAttribute(ItemAttributes.attr_newbie, true);
			}
		}

		public override void On_DClick(AbstractCharacter from) {
			Player player = from as Player;
			if (player == null) {
				return;
			}
			if (this.IsLit) {
				player.SysMessage("Svici nelze uchopit.");
			} else if (!Professions.HasProfession(player, Professions.Priest)) {
				player.RedMessage("Nevladnes moci, aby jsi tuto svici zapalil.");
			} else {
				PriestAbilities.LightCandle(player);
				this.Delete();
			}
		}

		public override void On_Click(AbstractCharacter clicker, GameState clickerState, TcpConnection<GameState> clickerConn) {
			base.On_Click(clicker, clickerState, clickerConn);
			this.Tell(clicker as Character);
		}

		public override void On_AosClick(AbstractCharacter clicker, GameState clickerState, TcpConnection<GameState> clickerConn) {
			base.On_AosClick(clicker, clickerState, clickerConn);
			this.Tell(clicker as Character);
		}

		private void Tell(Character ch) {
			if ((ch != null) && this.IsLit && ch.HasTimer(BurningTK)) {
				int seconds = (int) ch.GetTimer(BurningTK).DueInSeconds;
				this.ClickMessage(ch, "Svice zivota podivne zari, vydrzi jeste " + seconds.ToString(CultureInfo.InvariantCulture) + " sekund");
			}
		}
	}

	/// <summary>[Function resurrection]. Its cooldown is the priest's profession plugin's since #1435.</summary>
	public static class PriestAbilities {
		private static readonly TagKey diedAtTK = TagKey.Acquire("_diedAt_");
		private const double RestlessSeconds = 10, CandleSeconds = 86400;

		internal static void NoteDeath(Character self) {
			self.SetTag(diedAtTK, Globals.TimeInSeconds);
		}

		internal static double DiedAt(Character self) {
			return ClassAbilities.SecondsTag(self, diedAtTK);
		}

		/// <summary>#1070 - e_allplayers' @beforegeteffect: the Resurrection spell on a player who died less than 5 s
		/// ago does nothing, unless the area resurrects at once (region_cf_instantRes, the arenas).</summary>
		internal static bool TooSoonToRise(Character target, SpellDef spell) {
			if (!(target is Player) || !(spell is ResurrectionSpellDef) || (Globals.TimeInSeconds - DiedAt(target) >= 5)) {
				return false;
			}
			FlaggedRegion area = RegionEventSystem.AreaOf(target.Region);
			if ((area != null) && area.HasCustomFlag(FlaggedRegion.InstantRes)) {
				return false;
			}
			target.SysMessage("Na oziveni jeste nejsi " + (target.IsFemale ? "pripravena" : "pripraven") + ".");
			return true;
		}

		internal static Item FindLitCandle(Character self) {
			ItemDef lit = (ItemDef) ThingDef.GetByDefname(LifeCandle.LitDef);
			foreach (AbstractItem item in self.Backpack) {
				if (item.Def == lit) {
					return (Item) item;
				}
			}
			return null;
		}

		internal static void PutOutCandle(Character self) {
			Item lit = FindLitCandle(self);
			if (lit != null) {
				lit.Delete();
			}
			self.DeleteTimer(LifeCandle.BurningTK);
		}

		internal static void LightCandle(Player self) {
			PutOutCandle(self);
			((ItemDef) ThingDef.GetByDefname(LifeCandle.LitDef)).Create(self.Backpack);
			BurningCandleTimer timer = new BurningCandleTimer();
			self.AddTimer(LifeCandle.BurningTK, timer);
			timer.DueInSeconds = CandleSeconds;
			self.SysMessage("Zapalil jsi svou svici zivota.");
		}

		[SteamFunction]
		public static void Resurrection(Player self) {
			ClassAbilitySettings settings = ClassAbilitySettings.instance;
			PriestProfessionPlugin priest = PriestProfessionPlugin.Of(self); //null for a GM of another class
			double next = (priest == null) ? 0 : priest.nextSelfRes;
			if ((priest == null) && !self.IsGM) {
				self.RedMessage("Nemuzes pouzit tuto schopnost.");
			} else if (!self.Flag_Dead) {
				self.RedMessage("Jsi nazivu, nepotrebujes se ozivovat.");
			} else if ((Globals.TimeInSeconds < next) && !self.IsGM) {
				int left = (int) (next - Globals.TimeInSeconds);
				self.SysMessage("Pokusit se o sebeoziveni se muzes za " + (left / 60) + " minut, " + (left % 60) + " sekund.");
			} else if ((Globals.TimeInSeconds - ClassAbilities.SecondsTag(self, diedAtTK)) < RestlessSeconds) {
				self.SysMessage("Tva duse je jeste prilis neklidna");
			} else if ((FindLitCandle(self) == null) && !self.IsGM) {
				self.SysMessage("Tva svice jiz vyhasla.");
			} else if ((ClassAbilities.Points(self, "selfres") * 45) < Globals.dice.Next(1001)) {
				if (priest != null) {
					priest.nextSelfRes = Globals.TimeInSeconds + settings.selfResFailSeconds;
				}
				self.SysMessage("Nepovedlo se ti ozivit se.");
			} else {
				if (priest != null) {
					priest.nextSelfRes = Globals.TimeInSeconds + settings.selfResSuccessSeconds;
				}
				self.Resurrect(null);
				PutOutCandle(self);
				int hits = ClassAbilities.Points(self, "selfres_hp"), mana = ClassAbilities.Points(self, "selfres_mana"), stamina = ClassAbilities.Points(self, "selfres_stamina");
				self.Hits = (short) Restored(hits, self.MaxHits);
				self.Mana = (short) Restored(mana, self.MaxMana);
				self.Stam = (short) ((hits == 0) ? 3 : (stamina * self.MaxStam * 10) / 100); //Erebor's stamina line asks about hits
			}
		}

		internal static int Restored(int points, int maximum) {
			return (points == 0) ? 3 : (points * maximum * 10) / 100;
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class BurningCandleTimer : BoundTimer {
		[DeepCopyImplementation, LoadingInitializer]
		public BurningCandleTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if (cont is Character self) {
				Item lit = PriestAbilities.FindLitCandle(self);
				if (lit != null) {
					lit.Delete();
				}
			}
		}
	}
}
