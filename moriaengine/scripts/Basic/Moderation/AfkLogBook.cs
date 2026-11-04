using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1107 - what Erebor's AFK scripts sent the GMs (eufegenie/kermel delayedmessage, accmsg): each line with its
	/// time, player and account. A line where a player went to the camp keeps the skill they were at, for the AFK log
	/// dialog's punishment. Saved; the oldest lines go past MaxLines.</summary>
	[HasSavedMembers]
	public static class AfkLogBook {
		internal const int MaxLines = 2000;

		[SavedMember]
		private static List<AfkLogEntry> lines = new List<AfkLogEntry>();

		public static IList<AfkLogEntry> Lines {
			get { return lines; }
		}

		/// <summary>`action`, when given, is the skill the player was at when sent to the camp.</summary>
		public static void Add(Character who, string text, string action) {
			AfkLogEntry entry = new AfkLogEntry();
			entry.time = DateTime.Now;
			entry.who = who;
			entry.name = who.Name;
			entry.account = (who.Account != null) ? who.Account.Name : "";
			entry.text = text;
			entry.action = action;
			lines.Add(entry);
			if (lines.Count > MaxLines) {
				lines.RemoveRange(0, lines.Count - MaxLines);
			}
			Logger.WriteInfo(true, "AFK: " + entry);
		}

		internal static void Remove(AfkLogEntry entry) {
			lines.Remove(entry);
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class AfkLogEntry {
		[SaveableData, CopyableData]
		public DateTime time;

		[SaveableData, CopyableData]
		public Character who;

		[SaveableData, CopyableData]
		public string name, account, text, action;

		[LoadingInitializer, DeepCopyImplementation]
		public AfkLogEntry() {
		}

		public override string ToString() {
			return this.time.ToString("yyyy/MM/dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) + " " + this.name + " (" + this.account + "): " + this.text;
		}
	}
}
