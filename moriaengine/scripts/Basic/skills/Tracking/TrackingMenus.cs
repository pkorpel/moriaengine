using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1004 - Tracking's two menus by the player's style. The picture rows are Sphere's Cmd_Skill_Tracking, 20 names at most.
	/// What a pick does stays ours: in 99z8 the second menu's answer does nothing.</summary>
	internal static class TrackingMenus {
		private const int MostNames = 20;
		private static readonly CharacterTypes[] categories = { CharacterTypes.Animals, CharacterTypes.Monsters, CharacterTypes.NPCs, CharacterTypes.Players, CharacterTypes.All };

		internal static void ShowCategories(SkillSequenceArgs skillSeqArgs) {
			if (!(skillSeqArgs.Self is Player self)) {
				return;
			}
			TrackingMenuLoc texts = Loc<TrackingMenuLoc>.Get(self.Language);
			List<PictureMenuRow> rows = new List<PictureMenuRow> {
				new PictureMenuRow(0x2120, texts.Animals),
				new PictureMenuRow(0x20df, texts.Monsters),
				new PictureMenuRow(0x2106, texts.Humans),
				new PictureMenuRow(0x20cd, texts.Players),
				new PictureMenuRow(0x2100, texts.AnythingThatMoves)
			};
			MenuStyles.Show(self, SingletonScript<D_Tracking_Categories>.Instance, new DialogArgs(skillSeqArgs), texts.Tracking, rows,
				index => {
					skillSeqArgs.Param1 = categories[index];
					skillSeqArgs.Param2 = TrackingEnums.Phase_Characters_Seek;
					skillSeqArgs.PhaseStart();
				},
				skillSeqArgs.PhaseAbort);
		}

		internal static void ShowCharacters(SkillSequenceArgs skillSeqArgs, List<AbstractCharacter> found) {
			if (!(skillSeqArgs.Self is Player self)) {
				return;
			}
			List<PictureMenuRow> rows = new List<PictureMenuRow>();
			for (int i = 0; (i < found.Count) && (i < MostNames); i++) {
				rows.Add(new PictureMenuRow(MenuStyles.IconOf((Character) found[i]), found[i].Name));
			}
			MenuStyles.Show(self, SingletonScript<D_Tracking_Characters>.Instance, new DialogArgs(skillSeqArgs, found), Loc<TrackingMenuLoc>.Get(self.Language).Tracking, rows,
				index => {
					skillSeqArgs.Target1 = (Character) found[index];
					skillSeqArgs.Param2 = TrackingEnums.Phase_Character_Track;
					skillSeqArgs.PhaseStart();
				},
				skillSeqArgs.PhaseAbort);
		}
	}

	internal class TrackingMenuLoc : CompiledLocStringCollection<TrackingMenuLoc> {
		public string WhatToTrack = "What do you wish to track?";
		public string Name = "Name";
		public string Tracking = "Tracking";
		public string Animals = "Animals";
		public string Monsters = "Monsters";
		public string Humans = "Humans";
		public string Players = "Players";
		public string AnythingThatMoves = "Anything that moves";
	}
}
