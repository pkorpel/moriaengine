using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1447 - Erebor's i_soul_remover: the life pact on its receiver, with the shaman who gave it and when. Its
	/// timer is the soul's end; then the soul goes and the pact with it. It stays through death: the pact is there to rise
	/// from it.</summary>
	[Dialogs.ViewableClass]
	public partial class LifePactPlugin {
		internal static readonly PluginKey Key = PluginKey.Acquire("_lifePact_");

		internal static LifePactPlugin Of(Character ch) {
			return (ch == null) ? null : ch.GetPlugin(Key) as LifePactPlugin;
		}

		/// <summary>i_soul_remover's @timer: the soul and the pact go.</summary>
		public void On_Timer() {
			if (this.Cont is Character self) {
				ShamanPacts.RemoveSoul(self);
			}
			this.Delete();
		}
	}

	public partial class LifePactPluginDef {
		public static readonly LifePactPluginDef instance =
			(LifePactPluginDef) new LifePactPluginDef("p_lifePact", "C# scripts", -1).Register();
	}

	/// <summary>#1447 - the empathy elixir, Erebor's i_getphysresists with e_getphysresists: for 8 s each of its holder's
	/// swings reads a monster's physical resists. It stays through death, as Erebor's memory item did.</summary>
	[Dialogs.ViewableClass]
	public partial class EmpathyPlugin {
		internal static readonly PluginKey Key = PluginKey.Acquire("_empathy_");
		internal const double Seconds = 8;

		internal static EmpathyPlugin Of(Character ch) {
			return (ch == null) ? null : ch.GetPlugin(Key) as EmpathyPlugin;
		}

		/// <summary>e_getphysresists' @AfterSwing, which Erebor ran for a dodged blow and a landed one alike, as
		/// @swingLanding runs before the dodge.</summary>
		public void On_SwingLanding(WeaponSwingArgs swing) {
			if ((this.Cont is Player self) && !swing.defender.IsDeleted) {
				ShamanPacts.ReadPhysical(self, swing.defender);
			}
		}

		public override string EffectName {
			get {
				return "Empatie";
			}
		}

		public override void On_Death() {
		}

		/// <summary>i_getphysresists' @unEquip line.</summary>
		protected override void EffectEndedMessage(Character cont) {
			cont.SysMessage("Nyni jiz nejsi " + (cont.IsFemale ? "schopna" : "schopen") + " rozpoznat rezisty.");
		}
	}

	public partial class EmpathyPluginDef {
		public static readonly EmpathyPluginDef instance =
			(EmpathyPluginDef) new EmpathyPluginDef("p_empathy", "C# scripts", -1).Register();
	}
}
