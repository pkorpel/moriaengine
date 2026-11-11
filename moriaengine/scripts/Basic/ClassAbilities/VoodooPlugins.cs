using System;
using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1450 - Erebor's i_woodooeffect: a voodoo curse (1 to 7) or blessing (11 to 14) on its owner. Erebor made a
	/// new memory for each (newitemsafe), so they stack: a blessing beside a curse, a curse thrown back beside one already
	/// there. Each has a key of its own. A curse comes back every 30 s, "Jsi pod prokletim voodoo.", until its owner is
	/// blessed (the Bless spell) or dead; a blessing goes at its first tick. The curse's events are its own triggers:
	/// e_warcry_effect and e_noport at @denySpellCast, e_nohorse at @stepTaken. .boosts shows each, as Erebor's boosty_menu.</summary>
	[Dialogs.ViewableClass]
	public partial class VoodooEffectPlugin : IEffectDescriber {
		internal const double Period = 30;
		private const int recallSpellId = 32;
		private const int boostsIcon = 0x5001; //boosty_menu's i_woodooeffect line
		private static readonly string[] kindNames = { "", "zraneni", "vycerpani", "krvaceni", "otrava", "sok", "bez cestovani", "bez kone",
			"", "", "", "sila", "inteligence", "obratnost", "ochrana" };

		/// <summary>Every voodoo on ch, oldest key first as the holder keeps them.</summary>
		internal static List<VoodooEffectPlugin> AllOf(Character ch) {
			List<VoodooEffectPlugin> found = new List<VoodooEffectPlugin>();
			foreach (Plugin plugin in ch.GetAllPlugins()) {
				if (plugin is VoodooEffectPlugin effect) {
					found.Add(effect);
				}
			}
			return found;
		}

		/// <summary>A curse (1 to 7) on ch, if any.</summary>
		internal static VoodooEffectPlugin CurseOf(Character ch) {
			return AllOf(ch).Find(effect => effect.kind < 10);
		}

		/// <summary>A blessing (11 to 14) on ch, if any.</summary>
		internal static VoodooEffectPlugin BlessingOf(Character ch) {
			return AllOf(ch).Find(effect => effect.kind > 10);
		}

		/// <summary>findid(i_woodooeffect): a curse or a blessing.</summary>
		internal static bool HasAny(Character ch) {
			return AllOf(ch).Count > 0;
		}

		/// <summary>[Function woodooeffect]: on now, then every 30 s, beside whatever voodoo is there already.</summary>
		internal static void Apply(Character ch, int kind, Character shaman) {
			VoodooEffectPlugin effect = (VoodooEffectPlugin) VoodooEffectPluginDef.instance.Create();
			effect.kind = kind;
			effect.shaman = shaman;
			ch.AddPlugin(PluginKey.Acquire("_woodooEffect_" + Globals.dice.Next()), effect);
			effect.Timer = Period;
			ShamanVoodoo.On(ch, kind, shaman);
		}

		/// <summary>For tests and GMs: every voodoo on ch goes at once, with no off-effect.</summary>
		internal static void RemoveAll(Character ch) {
			foreach (VoodooEffectPlugin effect in AllOf(ch)) {
				effect.Delete();
			}
		}

		public ActiveEffect DescribeEffect(Character holder) {
			string detail = ((this.kind > 0) && (this.kind < kindNames.Length)) ? kindNames[this.kind] : "";
			return new ActiveEffect((this.kind > 10) ? EffectSection.Positive : EffectSection.Negative, "Voodoo") {
				SecondsLeft = this.Timer,
				Detail = detail,
				Icon = boostsIcon
			};
		}

		/// <summary>i_woodooeffect's @timer.</summary>
		public void On_Timer() {
			if (!(this.Cont is Character ch)) {
				return;
			}
			if ((this.kind > 10) || ch.Flag_Dead || IsBlessed(ch)) {
				ch.SysMessage("voodoo pominulo");
				ShamanVoodoo.Off(ch, this.kind);
				this.Delete();
				return;
			}
			this.Timer = Period;
			ch.SysMessage("Jsi pod prokletim voodoo.");
			ShamanVoodoo.On(ch, this.kind, this.shaman);
		}

		/// <summary>e_warcry_effect's and e_noport's @spellcast, after e_allplayers' rules. A scroll's custom spell never
		/// reached them (customspellcast skips @spellcast).</summary>
		public TriggerResult On_DenySpellCast(SkillSequenceArgs mageryArgs) {
			if (!(mageryArgs.Param1 is SpellDef spell) || spell.IsCustom) {
				return TriggerResult.Continue;
			}
			if (this.kind == ShamanVoodoo.Shock) {
				mageryArgs.Self.SysMessage("Jsi v soku, nemuzes kouzlit!");
				return TriggerResult.Cancel;
			}
			if ((this.kind == ShamanVoodoo.NoPort) && (spell.Id == recallSpellId)) {
				mageryArgs.Self.SysMessage("Nelze cestovat kouzlem.");
				return TriggerResult.Cancel;
			}
			return TriggerResult.Continue;
		}

		/// <summary>e_nohorse's @step: a rider (no GM) gets off.</summary>
		public void On_StepTaken(bool running) {
			if ((this.kind == ShamanVoodoo.NoHorse) && (this.Cont is Character ch) && ch.Flag_Riding && !ch.IsGM) {
				ch.Dismount();
				ch.SysMessage(Loc<RegionEventsLoc>.Get(ch.Language).mountRefuses);
			}
		}

		private static bool IsBlessed(Character ch) {
			foreach (Plugin plugin in ch.GetAllPlugins()) {
				if (plugin is BlessSpellEffectPlugin) {
					return true;
				}
			}
			return false;
		}
	}

	public partial class VoodooEffectPluginDef {
		public static readonly VoodooEffectPluginDef instance =
			(VoodooEffectPluginDef) new VoodooEffectPluginDef("p_woodooEffect", "C# scripts", -1).Register();
	}

	/// <summary>#1450 - Erebor's i_mem_woodoo: a voodoo on its way to its owner, 10 s for a curse, 5 for a blessing. Several
	/// may be on their way at once, each under a key of its own.</summary>
	[Dialogs.ViewableClass]
	public partial class VoodooHandlePlugin {
		/// <summary>For tests and GMs: every voodoo on its way to ch goes.</summary>
		internal static void RemoveAll(Character ch) {
			List<VoodooHandlePlugin> found = new List<VoodooHandlePlugin>();
			foreach (Plugin plugin in ch.GetAllPlugins()) {
				if (plugin is VoodooHandlePlugin handle) {
					found.Add(handle);
				}
			}
			foreach (VoodooHandlePlugin handle in found) {
				handle.Delete();
			}
		}

		internal static void Send(Character victim, Player shaman, int kind) {
			VoodooHandlePlugin handle = (VoodooHandlePlugin) VoodooHandlePluginDef.instance.Create();
			handle.shaman = shaman;
			handle.kind = kind;
			victim.AddPlugin(PluginKey.Acquire("_voodooHandle_" + Globals.dice.Next()), handle);
			handle.Timer = (kind > 10) ? 5 : 10;
		}

		/// <summary>i_mem_woodoo's @timer.</summary>
		public void On_Timer() {
			Character victim = this.Cont as Character;
			Player sender = this.shaman;
			int what = this.kind;
			this.Delete();
			if (victim != null) {
				ShamanVoodoo.Arrive(victim, sender, what);
			}
		}
	}

	public partial class VoodooHandlePluginDef {
		public static readonly VoodooHandlePluginDef instance =
			(VoodooHandlePluginDef) new VoodooHandlePluginDef("p_voodooHandle", "C# scripts", -1).Register();
	}
}
