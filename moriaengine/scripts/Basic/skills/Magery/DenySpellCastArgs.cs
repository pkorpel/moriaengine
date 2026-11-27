namespace SteamEngine.CompiledScripts {

	/// <summary>#1558 - @denySpellCast's args: argv[0] the result, argv[1] Magery's SkillSequenceArgs, argv[2] the spell, argv[3] its
	/// target. A handler refuses by cancelling or setting Result, after sending its own message; it doesn't act. Fizzle says
	/// whether Magery plays the fizzle for the refusal (the casting rules, Character.On_DenySpellCast, set it).</summary>
	public class DenySpellCastArgs : DenyTriggerArgs {
		public readonly SkillSequenceArgs mageryArgs;

		public DenySpellCastArgs(SkillSequenceArgs mageryArgs)
			: base(DenyResultMessages.Allow, mageryArgs, mageryArgs.Param1, mageryArgs.Target1) {
			this.mageryArgs = mageryArgs;
		}

		public Character Caster {
			get {
				return this.mageryArgs.Self;
			}
		}

		public SpellDef Spell {
			get {
				return (SpellDef) this.mageryArgs.Param1;
			}
		}

		public IPoint3D Target {
			get {
				return this.mageryArgs.Target1;
			}
		}

		public bool Fizzle { get; set; }
	}
}
