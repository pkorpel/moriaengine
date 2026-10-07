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

using System.Diagnostics.CodeAnalysis;
using PerCederberg.Grammatica.Parser;

namespace SteamEngine.LScript {
	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_ConcatOperator : OpNode_Lazy_BinOperator, ITriable {
		//gets created from OpNode
		internal OpNode_ConcatOperator(IOpNodeHolder parent, Node code)
			: base(parent, code) {
		}

		internal override object Run(ScriptVars vars) {
			return string.Concat(this.left.Run(vars), this.right.Run(vars));
		}

		public object TryRun(ScriptVars vars, object[] results) {
			return string.Concat(results[0], results[1]);
		}
	}
}