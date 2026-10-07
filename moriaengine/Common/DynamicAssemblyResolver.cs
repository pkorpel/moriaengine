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
using System.Linq;
using System.Reflection;

namespace SteamEngine.Common {

	/// <summary>
	/// The script-compilation pipeline (CompilerInvoker/GeneratedCodeUtil) loads freshly-compiled
	/// assemblies from a different directory than the running process's own (scripts/Generated get
	/// built to their own bin/ folders, not GameCore's). On .NET Framework, Assembly.LoadFile's
	/// "LoadFrom context" would transparently fall back to an already-loaded assembly of the same
	/// simple name elsewhere in the AppDomain. .NET Core's AssemblyLoadContext model doesn't do this
	/// automatically - each Assembly.LoadFile call gets its own isolated context that only probes its
	/// own directory, so a compiled-scripts assembly referencing SteamEngine.GameCore/Common/etc (all
	/// already loaded in the current process, just from a different folder) fails to resolve them and
	/// GetTypes() throws ReflectionTypeLoadException for every type.
	/// AppDomain.AssemblyResolve is still honored as a process-wide last-resort fallback in .NET Core,
	/// so just reusing whatever's already loaded by simple name restores the old Framework behavior.
	/// </summary>
	public static class DynamicAssemblyResolver {
		private static bool registered;

		public static void Register() {
			if (!registered) {
				registered = true;
				AppDomain.CurrentDomain.AssemblyResolve += ResolveFromAlreadyLoaded;
			}
		}

		private static Assembly ResolveFromAlreadyLoaded(object sender, ResolveEventArgs args) {
			string simpleName = new AssemblyName(args.Name).Name;
			return AppDomain.CurrentDomain.GetAssemblies()
				.FirstOrDefault(a => string.Equals(a.GetName().Name, simpleName, StringComparison.OrdinalIgnoreCase));
		}
	}
}
