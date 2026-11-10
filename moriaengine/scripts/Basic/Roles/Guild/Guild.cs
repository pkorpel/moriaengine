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
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Guilds (enhancements #14). GetGuild/AreInOneGuild/AreAllied/AreInWar were all hardcoded
	/// stubs (GetGuild always null, the rest always false) - meaning every guild-aware check
	/// elsewhere in the codebase (Notoriety.GetHighlightColor/GetCharRelation, written in Phase
	/// 15) has been silently dead code this whole time, not just this feature. Fixed by routing
	/// through RolesManagement/RoleKey, the exact same generic role-membership framework Party
	/// already uses successfully (Party.GetParty mirrors this precisely) - Guild just needed to
	/// actually use it instead of stubbing it out.
	///
	/// War/alliance relationships are stored as guild-name lists (resolved against `allGuilds` at
	/// query time) rather than direct Guild object references, since Role/Disposable isn't a
	/// TagHolder (no GetTag/SetTag available) and Guild needs its own hand-rolled Save/LoadLine
	/// override anyway (matching RankSystem's existing pattern) - name-based cross-references
	/// avoid needing a UID-based object-reference resolution mechanism for this pass.
	///
	/// #884 - a guild is run from its guildstone, as on Erebor (systems/guilds.scp): see GuildStone
	/// and D_GuildMenu. A candidate is a member with GuildMembership.isCandidate, like Erebor's
	/// i_memory_guild, which both have. The 0xD4 guild button is still not handled.
	/// </summary>
	[ViewableClass]
	[SaveableClass]
	public partial class Guild : RankSystem {
		public const int RankPublic = 0, RankCandidate = 1, RankMember = 2, RankMaster = 3;

		public static readonly RoleKey guildRK = RoleKey.Acquire("_guild_");

		private static readonly List<Guild> allGuilds = new List<Guild>();

		public static IEnumerable<Guild> AllGuilds {
			get { return allGuilds; }
		}

		private string abbreviation;
		private List<string> alliedGuildNames;
		private List<string> warGuildNames;

		public bool IsLeader(Character ch) {
			return ch == this.Master;
		}

		/// <summary>tag.master of the stone.</summary>
		public Character Master {
			get;
			internal set;
		}

		/// <summary>#884 - the guild lives on this stone, as on Erebor; null for one from before.</summary>
		public GuildStone Stone {
			get;
			internal set;
		}

		public static Guild ForStone(GuildStone stone) {
			foreach (Guild guild in allGuilds) {
				if (guild.Stone == stone) {
					return guild;
				}
			}
			return null;
		}

		private GuildMembership MembershipOf(Character ch) {
			return RolesManagement.HasRole(ch, this) ? (GuildMembership) this.GetMembership(ch) : null;
		}

		private List<Character> SortedMembers(bool candidates) {
			List<GuildMembership> found = new List<GuildMembership>();
			foreach (Character member in this.Members) {
				GuildMembership membership = this.MembershipOf(member);
				if (membership.isCandidate == candidates) {
					found.Add(membership);
				}
			}
			found.Sort((a, b) => a.seniority.CompareTo(b.seniority));
			return found.ConvertAll(membership => membership.Member);
		}

		/// <summary>Coowners of the stone, the eldest first.</summary>
		public List<Character> FullMembers {
			get { return this.SortedMembers(false); }
		}

		/// <summary>Friends of the stone.</summary>
		public List<Character> Candidates {
			get { return this.SortedMembers(true); }
		}

		public bool IsFullMember(Character ch) {
			GuildMembership membership = this.MembershipOf(ch);
			return (membership != null) && !membership.isCandidate;
		}

		public bool IsCandidate(Character ch) {
			GuildMembership membership = this.MembershipOf(ch);
			return (membership != null) && membership.isCandidate;
		}

		/// <summary>gfindrank; a GM is everyone's master.</summary>
		public int GetRank(Character ch) {
			if (ch.IsGM || (ch == this.Master)) {
				return RankMaster;
			}
			if (this.IsFullMember(ch)) {
				return RankMember;
			}
			return this.IsCandidate(ch) ? RankCandidate : RankPublic;
		}

		private GuildMembership Join(Character ch, bool asCandidate) {
			RolesManagement.Assign(ch, this);
			GuildMembership joined = this.MembershipOf(ch);
			int seniority = 0;
			foreach (Character member in this.Members) {
				seniority = Math.Max(seniority, this.MembershipOf(member).seniority + 1);
			}
			joined.seniority = seniority;
			joined.isCandidate = asCandidate;
			return joined;
		}

		/// <summary>SrcApplyToJoin.</summary>
		public void AddCandidate(Character ch) {
			this.Join(ch, true);
			ch.Say(ch.Name + " je nyni kandidatem.");
		}

		/// <summary>SrcJoinAsMember: a candidate stays in line, a newcomer joins at the end.</summary>
		public void AddMember(Character ch) {
			GuildMembership membership = this.MembershipOf(ch);
			if (membership != null) {
				membership.isCandidate = false;
			} else {
				this.Join(ch, false);
			}
			ch.Say(ch.Name + " je nyni clenem.");
		}

		/// <summary>SrcResign and refreshmaster. Erebor's votes are dead code (the voting button's
		/// line is commented out), which leaves what they did: a master who left is replaced by
		/// the eldest member.</summary>
		public void Resign(Character ch) {
			RolesManagement.UnAssign(ch, this);
			if (ch == this.Master) {
				List<Character> members = this.FullMembers;
				this.Master = (members.Count > 0) ? members[0] : null;
			}
		}

		/// <summary>The name and the stone's name together, and in the other guilds' lists.</summary>
		public void Rename(string name) {
			foreach (Guild other in allGuilds) {
				ReplaceInList(other.alliedGuildNames, this.Name, name);
				ReplaceInList(other.warGuildNames, this.Name, name);
			}
			this.Name = name;
			if (this.Stone != null) {
				this.Stone.Name = name;
			}
		}

		private static void ReplaceInList(List<string> list, string oldName, string newName) {
			int index = (list != null) ? list.IndexOf(oldName) : -1;
			if (index >= 0) {
				list[index] = newName;
			}
		}

		/// <summary>The character's name, with " [abbrev]" if they show it (showname_old).</summary>
		public static string NameWithAbbreviation(Character ch) {
			Guild guild = MysticHypnosis.IsPossessing(ch) ? null : GetGuild(ch); //#1070 - showname: a possessing mystic shows no guild
			if ((guild != null) && guild.MembershipOf(ch).canShowAbbrev) {
				return ch.Name + " [" + guild.Abbreviation + "]";
			}
			return ch.Name;
		}

		public bool ShowsAbbreviation(Character ch) {
			GuildMembership membership = this.MembershipOf(ch);
			return (membership != null) && membership.canShowAbbrev;
		}

		public void ToggleAbbreviation(Character ch) {
			GuildMembership membership = this.MembershipOf(ch);
			if (membership != null) {
				membership.canShowAbbrev = !membership.canShowAbbrev;
			}
		}

		/// <summary>guildMsg, at login and logout.</summary>
		public static void AnnounceConnection(Character ch, bool connected) {
			Guild guild = GetGuild(ch);
			if (guild != null) {
				string verb = connected ? (ch.IsFemale ? "pripojila" : "pripojil") : (ch.IsFemale ? "odpojila" : "odpojil");
				ChatSystem.GuildNotice(guild, "--*" + ch.Name + " se " + verb + "*--");
			}
		}

		public Guild(GuildDef def, RoleKey key)
			: base(def, key) {

			allGuilds.Add(this);
		}

		public string Abbreviation {
			get {
				return this.abbreviation;
			}
			set {
				this.abbreviation = value;
			}
		}

		public static Guild FindByName(string name) {
			foreach (Guild g in allGuilds) {
				if (string.Equals(g.Name, name, System.StringComparison.OrdinalIgnoreCase)) {
					return g;
				}
			}
			return null;
		}

		public static bool AreInOneGuild(Character a, Character b) {
			Guild ga = GetGuild(a);
			if (ga != null) {
				return ga == GetGuild(b);
			}
			return false;
		}

		public static Guild GetGuild(Character self) {
			if (self == null) {
				return null;
			}
			return (Guild) RolesManagement.GetRole(self, guildRK);
		}

		public bool IsAllyWith(Guild other) {
			return (other != null) && (this.alliedGuildNames != null) && this.alliedGuildNames.Contains(other.Name);
		}

		public bool IsAtWarWith(Guild other) {
			return (other != null) && (this.warGuildNames != null) && this.warGuildNames.Contains(other.Name);
		}

		public static bool AreAllied(Guild a, Guild b) {
			if ((a == null) || (b == null) || (a == b)) {
				return false;
			}
			return a.IsAllyWith(b) || b.IsAllyWith(a);
		}

		public static bool AreInWar(Guild a, Guild b) {
			if ((a == null) || (b == null) || (a == b)) {
				return false;
			}
			return a.IsAtWarWith(b) || b.IsAtWarWith(a);
		}

		public void DeclareWarOn(Guild other) {
			if (other == this) {
				return;
			}
			this.RemoveAllyName(other.Name);
			other.RemoveAllyName(this.Name);
			this.warGuildNames = AddUnique(this.warGuildNames, other.Name);
			other.warGuildNames = AddUnique(other.warGuildNames, this.Name);
		}

		public void MakePeaceWith(Guild other) {
			RemoveFromList(this.warGuildNames, other.Name);
			RemoveFromList(other.warGuildNames, this.Name);
		}

		public void DeclareAllianceWith(Guild other) {
			if (other == this) {
				return;
			}
			this.RemoveWarName(other.Name);
			other.RemoveWarName(this.Name);
			this.alliedGuildNames = AddUnique(this.alliedGuildNames, other.Name);
			other.alliedGuildNames = AddUnique(other.alliedGuildNames, this.Name);
		}

		public void BreakAllianceWith(Guild other) {
			RemoveFromList(this.alliedGuildNames, other.Name);
			RemoveFromList(other.alliedGuildNames, this.Name);
		}

		private void RemoveAllyName(string name) {
			RemoveFromList(this.alliedGuildNames, name);
		}

		private void RemoveWarName(string name) {
			RemoveFromList(this.warGuildNames, name);
		}

		private static List<string> AddUnique(List<string> list, string name) {
			if (list == null) {
				list = new List<string>();
			}
			if (!list.Contains(name)) {
				list.Add(name);
			}
			return list;
		}

		private static void RemoveFromList(List<string> list, string name) {
			if (list != null) {
				list.Remove(name);
			}
		}

		protected override IRoleMembership CreateMembershipObject(Character member) {
			return new GuildMembership(member, this);
		}

		protected override void On_DisposeManagedResources() {
			allGuilds.Remove(this);

			base.On_DisposeManagedResources();
		}

		#region persistence
		[Save]
		public override void Save(SaveStream output) {
			base.Save(output);
			if (this.abbreviation != null) {
				output.WriteValue("abbreviation", this.abbreviation);
			}
			if (this.Master != null) {
				output.WriteValue("founder", this.Master);
			}
			if (this.Stone != null) {
				output.WriteValue("stone", this.Stone);
			}
			if (this.alliedGuildNames != null) {
				output.WriteValue("alliedGuildNames", this.alliedGuildNames);
			}
			if (this.warGuildNames != null) {
				output.WriteValue("warGuildNames", this.warGuildNames);
			}
		}

		protected override void LoadLine(string filename, int line, string valueName, string valueString) {
			switch (valueName) {
				case "abbreviation":
					this.abbreviation = (string) ObjectSaver.OptimizedLoad_String(valueString);
					return;
				case "founder":
					ObjectSaver.Load(valueString, this.Load_Founder, filename, line);
					return;
				case "stone":
					ObjectSaver.Load(valueString, this.Load_Stone, filename, line);
					return;
				case "alliedguildnames":
					ObjectSaver.Load(valueString, this.Load_AlliedNames, filename, line);
					return;
				case "warguildnames":
					ObjectSaver.Load(valueString, this.Load_WarNames, filename, line);
					return;
			}
			base.LoadLine(filename, line, valueName, valueString);
		}

		private void Load_Founder(object resolvedObject, string filename, int line) {
			this.Master = (Character) resolvedObject;
		}

		private void Load_Stone(object resolvedObject, string filename, int line) {
			this.Stone = (GuildStone) resolvedObject;
		}

		private void Load_AlliedNames(object resolvedObject, string filename, int line) {
			this.alliedGuildNames = (List<string>) resolvedObject;
		}

		private void Load_WarNames(object resolvedObject, string filename, int line) {
			this.warGuildNames = (List<string>) resolvedObject;
		}
		#endregion persistence
	}

	public class GuildDef : RankSystemDef {
		public static readonly GuildDef instance = (GuildDef)
			new GuildDef("r_guild", "C# scripts", -1).Register();

		public new static void Bootstrap() { //ensure calling the static initialiser (#1369: ClassManager runs it before this)
		}

		public GuildDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override Role CreateImpl(RoleKey key) {
			return new Guild(this, key);
		}

		/// <summary>A guild for a stone without one, named after it, no members yet.</summary>
		public static Guild NewGuild(GuildStone stone) {
			Guild g = (Guild) instance.Create(Guild.guildRK);
			g.Name = stone.Name;
			g.Abbreviation = stone.Name;
			g.Stone = stone;
			return g;
		}
	}
}
