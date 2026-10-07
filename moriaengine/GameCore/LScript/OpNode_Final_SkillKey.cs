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

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using PerCederberg.Grammatica.Parser;

namespace SteamEngine.LScript {
	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_SkillKey_Set : OpNode, IOpNodeHolder, ITriable {
		private OpNode arg;
		private readonly int skillId;

		internal OpNode_SkillKey_Set(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, int skillId, OpNode arg)
			: base(parent, filename, line, column, origNode) {
			this.arg = arg;
			this.skillId = skillId;
		}

		public virtual void Replace(OpNode oldNode, OpNode newNode) {
			if (this.arg == oldNode) {
				this.arg = newNode;
				return;
			}
			throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
		}

		internal override object Run(ScriptVars vars) {
			object oSelf = vars.self;
			vars.self = vars.defaultObject;
			object result;
			try {
				result = this.arg.Run(vars);
			} finally {
				vars.self = oSelf;
			}

			try {
				AbstractCharacter ch = (AbstractCharacter) oSelf;
				//ch.SkillById(skillId).RealValue = Convert.ToUInt16(result);
				ch.SetRealSkillValue(this.skillId, Convert.ToInt32(result, CultureInfo.InvariantCulture));
				//ch.Skills[skillId].RealValue = Convert.ToUInt16(result);
				return null;
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating SkillKey (skill id " + this.skillId + ") expression",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			AbstractCharacter ch = (AbstractCharacter) vars.self;
			ch.SetRealSkillValue(this.skillId, Convert.ToInt32(results[0], CultureInfo.InvariantCulture));
			//ch.Skills[skillId].RealValue = Convert.ToUInt16(results[0]);
			return null;
		}

		public override string ToString() {
			return string.Concat(AbstractSkillDef.GetById(this.skillId).Key, "=", this.arg);
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_SkillKey_Get : OpNode, ITriable {
		private readonly int skillId;

		internal OpNode_SkillKey_Get(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, int skillId)
			: base(parent, filename, line, column, origNode) {
			this.skillId = skillId;
		}

		internal override object Run(ScriptVars vars) {
			try {
				AbstractCharacter ch = (AbstractCharacter) vars.self;
				return ch.GetSkill(this.skillId);
				//return ch.Skills[skillId].RealValue;
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating SkillKey (skill id " + this.skillId + ") expression",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			AbstractCharacter ch = (AbstractCharacter) vars.self;
			return ch.GetSkill(this.skillId);
			//return ch.Skills[skillId].RealValue;
		}

		public override string ToString() {
			return AbstractSkillDef.GetById(this.skillId).Key;
		}
	}

}