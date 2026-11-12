using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1480 - Erebor's convict (lagr.scp's tag.trestanec), from the camp's sentence (Camp.Send) to the release
	/// (Camp.Release). Its fields are the tags beside trestanec: the release coins owed in hundredths (lagrlogu), the
	/// sentence's Lumberjacking a GM set (lagrskill), and the murders, name and Lumberjacking given back at the release
	/// (lagrkills, lagrname, lumberpits). Camp reads and writes them.</summary>
	[Dialogs.ViewableClass]
	public partial class ConvictPlugin {
		internal static readonly PluginKey Key = PluginKey.Acquire("_convict_");

		internal static ConvictPlugin Of(Character ch) {
			return (ch == null) ? null : ch.GetPlugin(Key) as ConvictPlugin;
		}

		/// <summary>fixresource's convict part (miningfix.scp), each successful chop: too much to carry ends it with a camp
		/// log taken (skill(-1): no failure, no gain), and one chop in a hundred brings a peasant to nag.</summary>
		public TriggerResult On_SkillSuccess(SkillSequenceArgs skillSeqArgs) {
			if (!(skillSeqArgs.SkillDef is LumberjackingSkillDef) || !(this.Cont is Character self)) {
				return TriggerResult.Continue;
			}
			float carried = MovementStamina.CarriedWeight(self);
			if ((carried > 4 * self.Str) || (carried > 250)) { //Erebor's 40*str and 2500, in tenths of a stone
				self.SysMessage(Loc<CampLoc>.Get(self.Language).CantCarryMore);
				Camp.Consume(self, "i_log_lager", 1);
				return TriggerResult.Cancel;
			}
			if (Globals.dice.Next(100) == 98) {
				CampPeasant.Come(self);
			}
			return TriggerResult.Continue;
		}
	}

	public partial class ConvictPluginDef {
		public static readonly ConvictPluginDef instance =
			(ConvictPluginDef) new ConvictPluginDef("p_convict", "C# scripts", -1).Register();
	}
}
