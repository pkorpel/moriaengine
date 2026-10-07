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
using System.Collections.Generic;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	/// <summary>Static class for operating with clients delayed messages.</summary>
	public static class MsgsBoard {
		/// <summary>A unique tag name for holding a list of client delayed messages</summary>
		internal static TagKey tkDelayedMsgs = TagKey.Acquire("_delayed_msgs_");

		/// <summary>Default senders name (if the sender was not specified)</summary>
		public const string NO_SENDER = "System";

		/// <summary>Various comparators</summary>
		private static MsgsSenderComparator senderComparator = new MsgsSenderComparator();
		private static MsgsTimeComparator timeComparator = new MsgsTimeComparator();
		private static MsgsUnreadComparator unreadComparator = new MsgsUnreadComparator();


		/// <summary>Returns a copy of the list of clients delayed messages (for sorting e.g.)</summary>
		public static List<DelayedMsg> GetClientsMessages(Character whose) {
			//get the client messages (empty list if no messages present)
			List<DelayedMsg> list = GetMessages(whose);
			//if the list is empty, return it now, else make a copy of the messages list
			if (list.Count == 0) {
				return list;
			}
			return new List<DelayedMsg>(list);
		}

		/// <summary>Simple 'add' method.</summary>
		public static void AddNewMessage(Character whom, DelayedMsg newMsg) {
			GetMessages(whom).Add(newMsg);
		}

		/// <summary>Simple 'remove' method.</summary>
		public static void DeleteMessage(Character whose, DelayedMsg newMsg) {
			GetMessages(whose).Remove(newMsg);
		}

		/// <summary>Private utility method returning the characters list of messages</summary>
		private static List<DelayedMsg> GetMessages(Character whose) {
			List<DelayedMsg> retList = (List<DelayedMsg>) whose.GetTag(tkDelayedMsgs);
			if (retList == null) { // no messages previously posted
				retList = new List<DelayedMsg>();
				whose.SetTag(tkDelayedMsgs, retList);
			}
			return retList;
		}

		/// <summary>Sorting method used for Character: Sorting parameters available are senders name, time and read/unread messages first</summary>
		public static List<DelayedMsg> GetSortedBy(Character whom, SortingCriteria criterion) {
			//get a new copy of the list and sort it using another version of this method			
			return GetSortedBy(GetClientsMessages(whom), criterion);
		}

		/// <summary>Sorting method when the list is obtained first: Sorting parameters available are senders name, time and read/unread messages first</summary>
		public static List<DelayedMsg> GetSortedBy(List<DelayedMsg> messages, SortingCriteria criterion) {
			//get a new copy of the list			
			switch (criterion) {
				case SortingCriteria.NameAsc: //order by sender alphabetically
					messages.Sort(senderComparator);
					break;
				case SortingCriteria.NameDesc: //order by sender alphabetically
					messages.Sort(senderComparator);
					messages.Reverse();
					break;
				case SortingCriteria.TimeAsc: //order by time
					messages.Sort(timeComparator);
					break;
				case SortingCriteria.TimeDesc: //order by time
					messages.Sort(timeComparator);
					messages.Reverse();
					break;
				case SortingCriteria.UnreadAsc: //order unread messages first
					messages.Sort(unreadComparator);
					break;
				case SortingCriteria.UnreadDesc: //order unread messages first
					messages.Sort(unreadComparator);
					messages.Reverse();
					break;
				default: //defaultly sort by time
					messages.Sort(timeComparator);
					break;
			}
			return messages;
		}

		/// <summary>
		/// Return the number of unread messages (those the recipient has not opened by 
		/// 'read/display detail' button
		/// </summary>
		public static int CountUnread(List<DelayedMsg> msgs) {
			int counter = 0;
			foreach (DelayedMsg msg in msgs) {
				if (!msg.read) {
					counter++;
				}
			}
			return counter;
		}

		/// <summary>Return the number of unread messages for the specified player</summary>
		public static int CountUnread(Character whose) {
			return CountUnread(GetMessages(whose));
		}
	}

	/// <summary>
	/// The object of the clients delayed message. Holds the info about the sender, sending time and 
	/// the message body
	/// </summary>
	[SaveableClass]
	public class DelayedMsg {
		[LoadingInitializer]
		public DelayedMsg() {
		}

		[SaveableData]
		public AbstractCharacter sender; //player/GM who has sent the page or null if the sender was the system
		[SaveableData]
		public string text; //the text of the message
		[SaveableData]
		public DateTime time; //time when the message was posted
		[SaveableData]
		public bool read; //was the message read by recipient or not ?
		[SaveableData]
		public Hues color; //messages color


		/// <summary>No sender - the message was created by server probably.</summary>
		public DelayedMsg(string text) {
			this.sender = null;
			this.text = text;
			this.time = DateTime.Now;
			this.color = Hues.WriteColor; //default
			this.read = false;
		}

		public DelayedMsg(AbstractCharacter sender, string text)
			: this(text) {
			this.sender = sender;
		}

		/// <summary>Allows us to specify if the message should be displayed as red</summary>
		public DelayedMsg(string text, bool redMessage)
			: this(text) {
			this.color = Hues.Red;
		}

		public DelayedMsg(AbstractCharacter sender, string text, bool redMessage)
			: this(text, redMessage) {
			this.sender = sender;
		}

		/// <summary>Allows uas to specify the messages color also...</summary>
		public DelayedMsg(AbstractCharacter sender, string text, Hues hue)
			: this(sender, text) {
			this.color = hue;
		}
	}

	/// <summary>Comparator serving for sorting the list of messages by sender</summary>
	class MsgsSenderComparator : IComparer<DelayedMsg> {
		public int Compare(DelayedMsg a, DelayedMsg b) {
			//if sender was not specified, consider the message as from "System"
			string name1 = (a.sender == null) ? MsgsBoard.NO_SENDER : a.sender.Name;
			string name2 = (b.sender == null) ? MsgsBoard.NO_SENDER : b.sender.Name;

			return name1.CompareTo(name2);
		}
	}

	/// <summary>Comparator serving for sorting the list of messages by their creation time</summary>
	class MsgsTimeComparator : IComparer<DelayedMsg> {
		public int Compare(DelayedMsg a, DelayedMsg b) {
			return a.time.CompareTo(b.time);
		}
	}

	/// <summary>Comparator serving for sorting the list of messages by their unread/status</summary>
	class MsgsUnreadComparator : IComparer<DelayedMsg> {
		public int Compare(DelayedMsg a, DelayedMsg b) {
			return a.read.CompareTo(b.read);
		}
	}
}