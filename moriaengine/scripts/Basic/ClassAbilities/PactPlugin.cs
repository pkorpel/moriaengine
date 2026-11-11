using System;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1445 - a character's shaman pacts (blood, mana, stamina): the one it gives (type, bearer), the one it bears
	/// (type, giver, and i_pact's regen every 5 s and end) and the blood sharing's switch, Erebor's tags on both sides. One
	/// plugin a character, whichever sides it holds: a mana pact may be the shaman's own, and a death never deletes another
	/// plugin of its own holder this way (#1387's loop). It goes when it holds neither side.</summary>
	[Dialogs.ViewableClass]
	public partial class PactPlugin {
		internal static readonly PluginKey Key = PluginKey.Acquire("_shamanPact_");
		internal const double TickSeconds = 5;

		internal static PactPlugin Of(Character ch) {
			return (ch == null) ? null : ch.GetPlugin(Key) as PactPlugin;
		}

		internal static PactPlugin Ensure(Character ch) {
			PactPlugin pact = Of(ch);
			if (pact == null) {
				pact = (PactPlugin) PactPluginDef.instance.Create();
				ch.AddPlugin(Key, pact);
			}
			return pact;
		}

		internal bool Gives {
			get {
				return this.givenType != null;
			}
		}

		internal bool Bears {
			get {
				return this.borneType != null;
			}
		}

		/// <summary>The pact on this side Erebor's tag.pacttype named: the given one first, as CancelPact looks.</summary>
		internal string OwnType {
			get {
				return this.givenType ?? this.borneType;
			}
		}

		internal void StopGiving() {
			this.givenType = null;
			this.receiver = null;
			this.DropIfEmpty();
		}

		internal void StopBearing() {
			this.borneType = null;
			this.giver = null;
			BoundTimer timer = this.TimerObject;
			if (timer != null) {
				timer.Delete();
			}
			this.DropIfEmpty();
		}

		private void DropIfEmpty() {
			if (!this.Gives && !this.Bears) {
				if (this.Cont is Character holder) {
					holder.DeletePlugin(Key);
				}
			}
		}

		/// <summary>i_pact and i_pact_bonus, every 5 s: the regen, and the end.</summary>
		public void On_Timer() {
			Character self = this.Cont as Character;
			if ((self == null) || !this.Bears) {
				return;
			}
			if (this.borneType == "blood") {
				if (Globals.TimeInSeconds >= this.endsAt) {
					ShamanPacts.CancelBy(self);
				} else {
					this.Timer = TickSeconds;
				}
				return;
			}
			this.ticksLeft--;
			if (this.ticksLeft < 1) {
				ShamanPacts.CancelBy(self);
				return;
			}
			if (this.borneType == "mana") {
				self.Mana = (short) Math.Min(self.MaxMana, self.Mana + this.regen);
			} else if (this.borneType == "stamina") {
				self.Stam = (short) Math.Min(self.MaxStam, self.Stam + this.regen);
			}
			this.Timer = TickSeconds;
		}

		/// <summary>@death and @Logout: a pact ends with either side gone (ShamanPacts.OnGone).</summary>
		public void On_Death() {
			if (this.Cont is Character self) {
				ShamanPacts.OnGone(self);
			}
		}

		public void On_LogOut() {
			if (this.Cont is Character self) {
				ShamanPacts.OnGone(self);
			}
		}

		/// <summary>e_bloodpact's @gethit, after the damage (@afterGetHit).</summary>
		public void On_AfterGetHit(WeaponSwingArgs swing, int damage) {
			if (this.Cont is Character hit) {
				ShamanPacts.ShareBlood(swing.attacker, hit, damage);
			}
		}
	}

	public partial class PactPluginDef {
		public static readonly PactPluginDef instance =
			(PactPluginDef) new PactPluginDef("p_shamanPact", "C# scripts", -1).Register();
	}
}
