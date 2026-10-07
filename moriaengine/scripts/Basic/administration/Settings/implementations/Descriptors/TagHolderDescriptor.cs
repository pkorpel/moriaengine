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

using System.Collections.Generic;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts.Dialogs {
	[ViewDescriptor(typeof(TagHolder), "Tag Holder")]
	public static class TagHolderDescriptor {
		/* We will add here a few methods for displaying tags, timers, clearing tags and timers
		 * and also two fields holding information about the tags and timers count*/

		[GetMethod("TagsCount", typeof(int))]
		public static object GetTagsCount(object target) {
			int counter = 0;
			foreach (KeyValuePair<TagKey, object> kvp in ((TagHolder) target).GetAllTags()) {
				counter++;
			}
			return counter;
		}

		[GetMethod("TimersCount", typeof(int))]
		public static object GetTimersCount(object target) {
			int counter = 0;
			foreach (KeyValuePair<TimerKey, BoundTimer> kvp in ((TagHolder) target).GetAllTimers()) {
				counter++;
			}
			return counter;
		}

		[Button("Taglist")]
		public static void TagList(object target) {
			D_TagList.TagList((TagHolder) target, null);
		}

		[Button("Timerlist")]
		public static void TimerList(object target) {
			D_TimerList.TimerList((TagHolder) target, null);
		}

		[Button("Cleartags")]
		public static void ClearTags(object target) {
			((TagHolder) target).ClearTags();
		}

		[Button("Cleartimers")]
		public static void ClearTimers(object target) {
			((TagHolder) target).DeleteTimers();
		}
	}
}