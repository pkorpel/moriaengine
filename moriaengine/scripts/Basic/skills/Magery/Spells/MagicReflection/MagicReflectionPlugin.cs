/*
    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
    Or visit http://www.gnu.org/copyleft/gpl.html
*/

using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	public partial class MagicReflectionPlugin {

		public void On_Assign() {
			Player self = this.Cont as Player;
			if (self != null) {
				self.WriteLine(Loc<MagicReflectionLoc>.Get(self.Language).NextEvilSpellReflects);
			}
		}

		public virtual TriggerResult On_SpellEffect(SpellEffectArgs effectArgs) {
			SpellDef spell = effectArgs.SpellDef;
			if ((spell.Flags & SpellFlag.IsHarmful) == SpellFlag.IsHarmful) {
				this.Delete(); //delete the reflection plugin, it's work is done

				Character origTarget = (Character) effectArgs.CurrentTarget;
				Character origCaster = effectArgs.Caster;

				MagicReflectionPlugin castersReflection = MagicReflectionPluginDef.GetMagicReflectionPlugin(origCaster);

				if (castersReflection == null) { //caster doesn't have reflection active himself
					//this.EffectPower = given by Effect setting of s_magic_reflection and reflection caster's magery
					effectArgs.SpellPower = (int) (effectArgs.SpellPower * this.EffectPower);
					effectArgs.CurrentTarget = origCaster;
					//effectArgs.Caster = origTarget; //should we enable this?

					spell.Trigger_EffectChar(origCaster, effectArgs); //new spelleffect, using the original effectargs

					ReflectionEyeCandy(origTarget);

					return TriggerResult.Cancel; //cancel the harmful spelleffect
				}
				//caster has reflection too. We optimize here and let the spell go on, just weaker
				effectArgs.SpellPower = (int) (effectArgs.SpellPower * this.EffectPower * this.EffectPower);
				castersReflection.Delete();

				ReflectionEyeCandy(origTarget);
				ReflectionEyeCandy(origCaster);
			}

			return TriggerResult.Continue; //nonharmful spell - do nothing			
		}

		private static void ReflectionEyeCandy(Character target) {
			EffectFactory.StationaryEffect(target, 0x37B9, 10, 5); //reflection effect
		}

		//protected override void EffectEndedMessage(Character cont) {
		//}
	}

	[ViewableClass]
	public partial class MagicReflectionPluginDef {
		public static readonly MagicReflectionPluginDef instance = (MagicReflectionPluginDef)
			new MagicReflectionPluginDef("p_magic_reflection", "C# scripts", -1).Register();

		private static DurableCharEffectSpellDef s_magic_reflection;
		public static DurableCharEffectSpellDef MagicReflectionSpellDef {
			get {
				if (s_magic_reflection == null) {
					s_magic_reflection = (DurableCharEffectSpellDef) SpellDef.GetByDefname("s_magic_reflection");
				}
				return s_magic_reflection;
			}
		}

		[SteamFunction]
		public static bool HasMagicReflection(Character self) {
			return GetMagicReflectionPlugin(self) != null;
		}

		[SteamFunction]
		public static MagicReflectionPlugin GetMagicReflectionPlugin(Character self) {
			PluginKey spellKey = MagicReflectionSpellDef.EffectPluginKey_Spell;
			MagicReflectionPlugin plugin = self.GetPlugin(spellKey) as MagicReflectionPlugin;
			if (plugin != null) {
				return plugin;
			}
			PluginKey potionKey = MagicReflectionSpellDef.EffectPluginKey_Potion;
			if (potionKey != spellKey) {
				return self.GetPlugin(potionKey) as MagicReflectionPlugin;
			}
			return null;
		}
	}

	public class MagicReflectionLoc : CompiledLocStringCollection<MagicReflectionLoc> {
		public string NextEvilSpellReflects = "Pøíští nepøátelské kouzlo se od tebe odrazí";
	}
}