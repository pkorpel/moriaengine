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


namespace SteamEngine.CompiledScripts.Dialogs {
	/// <summary>Extend this class in order to obtain a new Category to the settings dialog</summary>
	public abstract class SettingsMetaCategory {
	}

	/// <summary>Viewable class representing the Settings Categories</summary>
	[ViewableClass("Settings Categories")]
	public class SettingsCategories : SettingsMetaCategory {

		/// <summary>
		/// Display a settings dialog. Function accessible from the game.
		/// The function is designed to be triggered using .x settings, but it will be 
		/// mainly used from the SettingsCategories dialog on a various buttons
		/// </summary>
		[SteamFunction]
		public static void Settings(object self, ScriptArgs args) {
			if (args.Argv == null || args.Argv.Length == 0) {
				//call the default settings dialog
				Globals.SrcCharacter.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(instance));
			} else {
				//get the arguments to be sent to the dialog (especialy the first one which is the 
				//desired object for infoizing)
				Globals.SrcCharacter.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(args.Argv[0]));
			}
		}


		/*The class or its subclasses can contain properties/fields which will be 
		 *like the normal values for setting, but it can also contain references to other
		 *SettingsCategories which will there serve as a subcategory*/

		[NoShow]//dont show this :)
		public static SettingsCategories instance = new SettingsCategories();

		//Setting categories: - Database Settings
		[InfoField("Database Settings")]
		public DbConfig DbConfig {
			get {
				return DbManager.Config;
			}
		}

		//Setting categories: - Combat Settings
		[InfoField("Combat Settings")]
		public CombatSettings CombatSettings {
			get {
				return CombatSettings.instance;
			}
		}

		[InfoField("Magery Settings")]
		public MagerySettings MagerySettings {
			get {
				return MagerySettings.instance;
			}
		}

		[InfoField("Ranger Settings")]
		public RangerSettings RangerSettings {
			get {
				return RangerSettings.instance;
			}
		}

		[InfoField("Poisons Settings")]
		public PoisoningSettings PoisoningSettings {
			get {
				return PoisoningSettings.instance;
			}
		}

		[InfoField("Abilities")]
		public AllAbilitiesMassSetting allAbilities = new AllAbilitiesMassSetting();

		[InfoField("Monster Abilities")]
		public MonsterAbilitiesSettings MonsterAbilitiesSettings {
			get {
				return MonsterAbilitiesSettings.instance;
			}
		}

		[InfoField("Level System")]
		public LevelingSettings LevelingSettings {
			get {
				return LevelingSettings.instance;
			}
		}

		[InfoField("Profession System")]
		public ProfessionSettings ProfessionSettings {
			get {
				return ProfessionSettings.instance;
			}
		}

		[InfoField("Item Decay")]
		public DecaySettings DecaySettings {
			get {
				return DecaySettings.instance;
			}
		}

		[InfoField("Notoriety")]
		public NotorietySettings NotorietySettings {
			get {
				return NotorietySettings.instance;
			}
		}

		[InfoField("NPC Aggro")]
		public NpcAggroSettings NpcAggroSettings {
			get {
				return NpcAggroSettings.instance;
			}
		}

		[InfoField("Loot")]
		public LootSettings LootSettings {
			get {
				return LootSettings.instance;
			}
		}

		[InfoField("Elite Monsters")]
		public EliteSettings EliteSettings {
			get {
				return EliteSettings.instance;
			}
		}

		[InfoField("Movement")]
		public CharCollisionSettings CharCollisionSettings {
			get {
				return CharCollisionSettings.instance;
			}
		}

		[InfoField("Logout")]
		public LogoutSettings LogoutSettings {
			get {
				return LogoutSettings.instance;
			}
		}

		[InfoField("Skill Advancement")]
		public SkillAdvancementSettings SkillAdvancementSettings {
			get {
				return SkillAdvancementSettings.instance;
			}
		}

		[InfoField("Mining")]
		public MiningSettings MiningSettings {
			get {
				return MiningSettings.instance;
			}
		}

		[InfoField("Lumberjacking")]
		public LumberjackingSettings LumberjackingSettings {
			get {
				return LumberjackingSettings.instance;
			}
		}

		[InfoField("Fishing")]
		public FishingSettings FishingSettings {
			get {
				return FishingSettings.instance;
			}
		}
	}

	public class AllAbilitiesMassSetting : MassSettings_ByClass_List<AbilityDef> {

		public override string Name {
			get { return "Seznam v�ech abilit"; }
		}
	}
}