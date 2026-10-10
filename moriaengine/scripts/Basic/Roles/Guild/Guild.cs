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
	/// Deliberately out of scope: the real guild-button/gump client packet (0xD4 + a management
	/// gump) and showing the guild abbreviation over a character's displayed name in the client
	/// (Character.Title turned out to already be completely unused at the packet level - no
	/// name-display code reads it at all, so wiring abbreviation display means fixing that too,
	/// a separate pre-existing gap). This pass is server-side: data model, membership, alliances/
	/// wars, and text-command-driven interaction (GuildCommands) instead of a gump.
	/// </summary>
	[ViewableClass]
	[SaveableClass]
	public partial class Guild : RankSystem {

		public static readonly RoleKey guildRK = RoleKey.Acquire("_guild_");

		private static readonly List<Guild> allGuilds = new List<Guild>();

		public static IEnumerable<Guild> AllGuilds {
			get { return allGuilds; }
		}

		private string abbreviation;
		private List<string> alliedGuildNames;
		private List<string> warGuildNames;

		//pending invites - ephemeral, not persisted (same spirit as Party's candidates list)
		private readonly List<Character> pendingInvites = new List<Character>();

		public bool IsLeader(Character ch) {
			return ch == this.Founder;
		}

		public Character Founder {
			get;
			internal set;
		}

		public bool TryInvite(Character candidate) {
			if ((candidate == null) || this.IsMember(candidate) || this.pendingInvites.Contains(candidate)) {
				return false;
			}
			this.pendingInvites.Add(candidate);
			return true;
		}

		public bool HasPendingInvite(Character candidate) {
			return this.pendingInvites.Contains(candidate);
		}

		public void ClearInvite(Character candidate) {
			this.pendingInvites.Remove(candidate);
		}

		public void SendGuildMessage(Character speaker, string text) {
			foreach (Character member in this.Members) {
				member.SysMessage("[Guild] " + speaker.Name + ": " + text);
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
			if (this.Founder != null) {
				output.WriteValue("founder", this.Founder);
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
			this.Founder = (Character) resolvedObject;
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

		public GuildDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override Role CreateImpl(RoleKey key) {
			return new Guild(this, key);
		}

		public static Guild NewGuild(Character founder, string name) {
			Guild g = (Guild) instance.Create(Guild.guildRK);
			g.Name = name;
			g.Founder = founder;
			RolesManagement.Assign(founder, g);
			return g;
		}
	}
}
