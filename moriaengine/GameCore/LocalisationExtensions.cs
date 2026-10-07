using System;
using System.Globalization;
using SteamEngine.Common;

namespace SteamEngine {
	public static class LocalisationExtensions {

		/// <summary>
		/// Shortuct method for sending a server-side-lcoalised message
		/// </summary>
		/// <typeparam name="TLoc">Loc object type</typeparam>
		/// <param name="src">The src object.</param>
		/// <param name="msgSelector">The message selector, typically just a lambda returning the desired field values.</param>
		public static void WriteLineLoc<TLoc>(this ISrc src, Func<TLoc, string> msgSelector) where TLoc : CompiledLocStringCollection<TLoc> {

			var loc = Loc<TLoc>.Get(src.Language);

			string msg = msgSelector(loc);

			src.WriteLine(msg);
		}

		/// <summary>
		/// Shortuct method for sending a server-side-lcoalised message
		/// </summary>
		/// <typeparam name="TLoc">Loc object type</typeparam>
		/// <param name="src">The src object.</param>
		/// <param name="msgSelector">The message selector, typically just a lambda returning the desired field values.</param>
		/// <param name="formatArgs">The format args.</param>
		public static void WriteLineLoc<TLoc>(this ISrc src, Func<TLoc, string> msgSelector, params object[] formatArgs) where TLoc : CompiledLocStringCollection<TLoc> {

			var loc = Loc<TLoc>.Get(src.Language);

			string msg = msgSelector(loc);

			msg = string.Format(CultureInfo.InvariantCulture,
				msg,
				formatArgs);

			src.WriteLine(msg);
		}
	}
}
