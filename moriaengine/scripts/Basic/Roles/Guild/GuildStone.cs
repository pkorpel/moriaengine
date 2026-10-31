using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	public partial class GuildStoneDef {
	}

	/// <summary>#884 - Erebor's i_guildstone (systems/guilds.scp): the guild is this stone. The
	/// first to double click a stone nobody belongs to becomes its master.</summary>
	[ViewableClass]
	public partial class GuildStone {
		public override void On_DClick(AbstractCharacter from) {
			Player player = from as Player;
			if (player == null) {
				return;
			}
			Guild guild = Guild.ForStone(this) ?? GuildDef.NewGuild(this);
			if ((guild.Members.Count == 0) && (Guild.GetGuild(player) == null)) {
				guild.AddMember(player);
				guild.Master = player;
			}
			player.Dialog(player, SingletonScript<D_GuildMenu>.Instance, new DialogArgs(guild));
		}

		public override void On_Destroy() {
			Guild guild = Guild.ForStone(this);
			if (guild != null) {
				guild.Dispose();
			}
			base.On_Destroy();
		}

		/// <summary>Nothing else leads the save to a Role, so without this a guild doesn't outlive
		/// a restart. Not a .ct field: a dupe of the stone must not share or deep copy the guild.</summary>
		public override void Save(SaveStream output) {
			base.Save(output);
			Guild guild = Guild.ForStone(this);
			if (guild != null) {
				ObjectSaver.Save(guild);
			}
		}
	}

	public class Targ_PlaceGuildstone : CompiledTargetDef {
		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			Item deed = (Item) parameter;
			if (!deed.IsDeleted) {
				Item stone = (Item) ThingDef.GetByDefname("i_guildstone").Create(targetted.X, targetted.Y, targetted.Z, self.M);
				stone.SetAttribute(ItemAttributes.attr_move_never, true);
				deed.Delete();
			}
			return TargetResult.Done;
		}
	}

	/// <summary>"Pridat kandidata na clenstvi" (applytojoin).</summary>
	public class Targ_GuildCandidate : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			Guild guild = (Guild) parameter;
			if (!(targetted is Player)) {
				self.SysMessage(targetted.Name + " neni hrac.");
			} else if (Guild.GetGuild(targetted) != null) {
				self.SysMessage(targetted.Name + " je clenem jine guildy.");
			} else if (guild.GetRank(self) >= Guild.RankMember) {
				guild.AddCandidate(targetted);
			}
			return TargetResult.Done;
		}
	}

	/// <summary>"Predat vedeni" (i_guild_target).</summary>
	public class Targ_GuildMaster : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			Guild guild = (Guild) parameter;
			if (guild.IsFullMember(targetted)) {
				guild.Master = targetted;
				self.SysMessage("Guildmasterovstvi predano");
			} else {
				self.SysMessage("Toto neni clen guildy");
			}
			return TargetResult.Done;
		}
	}
}
