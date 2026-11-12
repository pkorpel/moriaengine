using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1478 - present while its character is hidden or invisible by magic, added and removed by those flags' setters
	/// (Character.Trigger_VisibilityChange), so every way of hiding has it. Its @stepTaken is Sphere's CheckRevealOnMove. The
	/// other reveals (a skill's start, a blow, a class command) stay calls: Sphere reveals per skill, and a blow's reveal has
	/// its place in the swing (a backstab needs the thief still hidden when its bonus is worked out).</summary>
	[Dialogs.ViewableClass]
	public partial class HiddenPlugin {
		internal static readonly PluginKey Key = PluginKey.Acquire("_hidden_");

		internal static HiddenPlugin Of(Character ch) {
			return (ch == null) ? null : ch.GetPlugin(Key) as HiddenPlugin;
		}

		/// <summary>On whenever ch is hidden or invisible by magic, off otherwise.</summary>
		internal static void Sync(Character ch) {
			bool unseen = ch.Flag_Hidden || ch.Flag_InvisByMagic;
			HiddenPlugin plugin = Of(ch);
			if (unseen && (plugin == null)) {
				ch.AddPlugin(Key, HiddenPluginDef.instance.Create());
			} else if (!unseen && (plugin != null)) {
				plugin.Delete();
			}
		}

		/// <summary>Sphere's CheckRevealOnMove.</summary>
		public void On_StepTaken(bool running) {
			if (this.Cont is Character self) {
				HidingSkillDef.OnStep(self, running);
			}
		}
	}

	public partial class HiddenPluginDef {
		public static readonly HiddenPluginDef instance =
			(HiddenPluginDef) new HiddenPluginDef("p_hidden", "C# scripts", -1).Register();
	}
}
