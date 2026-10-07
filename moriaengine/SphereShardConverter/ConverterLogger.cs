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
using System.IO;
using SteamEngine.Common;

namespace SteamEngine.Converter {
	public class ConverterLogger : Logger {
		public static void Init() {
			new ConverterLogger();

			OpenFile();
		}

		protected override string GetFilepath() {
			//DateTime.Now.GetDateTimeFormats()[4]
			DateTime dtnow = DateTime.Now;
			string filename = "Converting.log";
			//string.Format("converting.{0:00}-{1:00}-{2:00} {3:00}.{4:00}.{5:00}.log", 
			//dtnow.Year, dtnow.Month, dtnow.Day, 
			//dtnow.Hour, dtnow.Minute, dtnow.Second);
			return Path.Combine("logs", filename);
		}
	}
}