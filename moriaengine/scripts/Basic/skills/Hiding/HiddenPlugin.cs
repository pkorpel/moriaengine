using System;
using SteamEngine.Generators.Abstractions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1478 - present while its character is hidden or invisible by magic, added and removed by those flags' setters
	/// (Character.Trigger_VisibilityChange), so every way of hiding has it. Its @step (after the move) is Sphere's CheckRevealOnMove. The
	/// other reveals (a skill's start, a blow, a class command) stay calls: Sphere reveals per skill, and a blow's reveal has
	/// its place in the swing (a backstab needs the thief still hidden when its bonus is worked out).</summary>
	[Dialogs.ViewableClass]
	[PluginTemplate]
	public partial class HiddenPlugin : Plugin {
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

		/// <summary>Sphere's CheckRevealOnMove. #1558 - @step after the move (was @stepTaken); not for a GM (CanMoveEverywhere),
		/// as @stepTaken wasn't raised for one.</summary>
		public void On_Step(Direction direction, bool running) {
			if ((this.Cont is Character self) && !self.CanMoveEverywhere) {
				HidingSkillDef.OnStep(self, running);
			}
		}
	}

	[PluginDefTemplate]
	public partial class HiddenPluginDef : PluginDef {
		public static readonly HiddenPluginDef instance =
			(HiddenPluginDef) new HiddenPluginDef("p_hidden", "C# scripts", -1).Register();
	}
}
