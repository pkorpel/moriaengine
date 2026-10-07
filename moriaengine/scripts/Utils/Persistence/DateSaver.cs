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
using System.Globalization;
using System.Text.RegularExpressions;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	public sealed class DateTimeImplementor : ISimpleSaveImplementor {
		private static DateTimeImplementor instance;
		public static DateTimeImplementor Instance {
			get {
				return instance;
			}
		}

		public DateTimeImplementor() {
			instance = this;
		}

		//public static Regex dateTimeRE= new Regex(@"^\::(?<value>\d+)\s*$",                     
		//changed to match date/time in format like dd.MM.yyyy HH.mm.ss.FFFFFFF
		//the whole time part is voluntary 
		//the seconds part is voluntary
		//and the decimal part too
		//I know it is not perfect, but it should be enough for our purposes.
		public static Regex re = new Regex(@"^\::(?<value>([0-3]?\d?\.[01]?\d?\.\d\d\d\d)\s*([012]?\d?:[0-5]\d(:[0-5]\d(\.\d{1,7})?)?)?)\s*$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		public Type HandledType {
			get {
				return typeof(DateTime);
			}
		}

		public Regex LineRecognizer {
			get {
				return re;
			}
		}

		public string Save(object objToSave) {
			//return "::"+((DateTime) value).Ticks;
			//we will return the date in some more acceptable form
			//the FFF... notation cuts off any possible zeros, it works only if decimal part of the seconds is null				
			string dateString = ((DateTime) objToSave).ToString("dd.MM.yyyy HH:mm:ss.FFFFFFF");
			//cut off the last zeros, we dot need them, hour was not specified
			if (dateString.EndsWith("00:00:00")) {//no hours at all?					
				dateString = dateString.Substring(0, dateString.Length - 8).Trim();
			} else if (dateString.EndsWith(":00")) {//or no seconds?
				dateString = dateString.Substring(0, dateString.Length - 3).Trim();
			}
			return "::" + dateString;
		}

		public object Load(Match match) {
			GroupCollection gc = match.Groups;

			//prepare formatter for parsing and parse the date from the string
			IFormatProvider culture = new CultureInfo("cs-CZ", true);
			return DateTime.Parse(gc["value"].Value, culture);
		}

		public string Prefix {
			get {
				return "::";
			}
		}
	}

	public sealed class TimeSpanImplementor : ISimpleSaveImplementor {
		private static TimeSpanImplementor instance;
		public static TimeSpanImplementor Instance {
			get {
				return instance;
			}
		}

		public TimeSpanImplementor() {
			instance = this;
		}

		//public static Regex timeSpanRE = new Regex(@"^\:(?<value>\d+)\s*$",
		//changed to match timespan in format like [-]d.hh:mm:ss.ff
		public static Regex re = new Regex(@"^\:(?<value>-?(\d*.)?([012]?\d?:[0-5]\d(:[0-5]\d(\.\d{1,7})?)?)?)\s*$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		public Type HandledType {
			get {
				return typeof(TimeSpan);
			}
		}

		public Regex LineRecognizer {
			get {
				return re;
			}
		}

		public string Save(object objToSave) {
			//return ":"+((TimeSpan) value).Ticks;
			//the TimeSpan has it own way how to transform to string, no need to rewrite this
			return ":" + ((TimeSpan) objToSave);
		}

		public object Load(Match match) {
			return TimeSpan.Parse(match.Groups["value"].Value);
		}

		public string Prefix {
			get {
				return ":";
			}
		}
	}
}