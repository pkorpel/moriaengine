using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	/// <summary>#931 - the Chat page of .settings.</summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class ChatSettings : SettingsMetaCategory {

		[SavedMember]
		public static ChatSettings instance = new ChatSettings();

		[LoadingInitializer]
		public ChatSettings() {
		}

		/// <summary>Between two messages of a player. On Erebor the rule was only written down.</summary>
		[SaveableData]
		public double secondsBetweenMessages = 3;

		/// <summary>Longer ones are cut: what fits a line on an old client.</summary>
		[SaveableData]
		public int maxLength = 73;

		[SaveableData]
		public int gondorHue = 0x480;

		[SaveableData]
		public int mordorHue = 0x26;

		[SaveableData]
		public int allHue = 0x44;

		[SaveableData]
		public int gmHue = 0x21F;

		[SaveableData]
		public int guildHue = 0x44;
	}
}
