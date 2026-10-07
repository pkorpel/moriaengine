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

namespace SteamEngine.CompiledScripts {

	/// <summary>Numbers of important hues</summary>
	public enum Hues
	{
		Red = 33,
		Blue = 99,
		Green = 68,
		Info = 642, //shit color :) (dark yellow-green-brown undefinable) :-/ its hard to choose
		//text colors
		PageRepliedColor = 1740, //some other orange shit (used for labeling replied pages)
		WriteColor = 2300,
		PlayerColor = 2301,//color for players name in Admin dialog (until the coloring players is solved)
		WriteColor2 = 0481,//tmave tyrkysova
		ReadColor = 2303,
		NAColor = 2305,

		OnlineColor = 1152,//color for highlighting online accounts/players
		OfflineColor = 2301,//color for highlighting offline accounts/players

		LabelColor = 1675, //nadpisy (sloupecku atd) - svetlounce bezova
		HeadlineColor = 2000,//1152, //titulek (obvykle celeho dialogu atd)

		SettingsTitleColor = 2413, //the settings category color
		SettingsNormalColor = 2300, //normal settings items
		SettingsCorrectColor = 95,//setting items that were correctly set
		SettingsFailedColor = 35 //setting items that weren't possible to be set
	}

	#region Dialog enums
	/// <summary>Various sorting criteria used in various dialogs</summary>
	public enum SortingCriteria
	{
		NameAsc,
		NameDesc,
		DefnameAsc,
		DefnameDesc,
		AccountAsc,
		AccountDesc,
		LocationAsc,
		LocationDesc,
		TimeAsc,
		TimeDesc,
		IPAsc,
		IPDesc,
		UnreadAsc,
		UnreadDesc,

		//useful for abilities list
		RunningAsc,
		RunningDesc,

		//useful for account notes
		RefCharAsc,
		RefCharDesc,
		AFKAsc,
		AFKDesc,
		IssuerAsc,
		IssuerDesc
	}

	public enum DialogAlignment
	{
		Align_Left,
		Align_Right,
		Align_Center,

		Valign_Top,
		Valign_Bottom,
		Valign_Center
	}

	/// <summary>Various types of GUTA Leaf Components</summary>
	public enum LeafComponentTypes
	{
		//Buttons
		/// <summary>Button with the big X inside</summary>
		ButtonCross,
		/// <summary>Button with the OK inside</summary>
		ButtonOK,
		/// <summary>Button with the tick inside</summary>
		ButtonTick,
		/// <summary>Button with the reversed tick (arrow back) inside</summary>
		ButtonBack,
		/// <summary>Button with the sheet of paper inside</summary>
		ButtonPaper,
		/// <summary>Button with flying paper</summary>
		ButtonSend,
		/// <summary>Button with the crossed circle</summary>
		ButtonNoOperation,
		/// <summary>Button for sorting (small up arrow)</summary>
		ButtonSortUp,
		/// <summary>Button for sorting (small down arrow)</summary>
		ButtonSortDown,
		/// <summary>Medium UP arrow</summary>
		ButtonPrev,
		/// <summary>Medium DOWN arrow</summary>
		ButtonNext,
		/// <summary>Button with people</summary>
		ButtonPeople,
		/// <summary>Triangular button</summary>
		ButtonTriangle,

		CheckBox,
		RadioButton,
		//Inputs
		InputText,
		InputNumber
	}

	public enum SettingsEnums
	{
		All, //Zobrazit vsechny kategorie v jednom dialogu
		Single, //Zobrazit jen jednu vybranou kategorii

		//Result of setting a single field in the info or settings dialogs
		NotChanged,//field has not changed at all
		ChangedOK, //field has changed successfully
		ChangedError //field has not changed due to some fault
	}
	#endregion Dialog enums

	/// <summary>specification of various localities where to look for resources</summary>
	[Flags]
	public enum ResourcesLocality
	{
		NonSpecified = 0x000, //not specified where to look for resources (used for resources of type: abilities,skills,triggergroups etc)

		//following usages are usually for resource of type "itemdef" - where should we search for the items
		WearableLayers = 0x001, //search among worn items (gloves, helm, rings etc)
		Backpack = 0x002, //look to the backpack only
		Bank = 0x004,	 //look to the bank only
		BackpackAndLayers = Backpack | WearableLayers, //loook to the backpack and worn items
		BackpackAndBank = Backpack | Bank, //look to both main containers
		Everywhere = WearableLayers | Backpack | Bank //search every place on the character (wearables, backapck, bank etc.)
	}

	//specificy how many items should we try to find at the specified location
	public enum ItemFindQuantity
	{
		FindAll = 1, //searches the whole location and returns the list of all items that correspond to the desired one
		FindFirst = 2 //searches until the first corresponding item is found
	}

	/// <summary>Urcuje, jakej rezist se ma aplikovat na dotycny damage.</summary>
	[Flags]
	public enum DamageType
	{
		/// <summary>Damage neredukovano</summary>
		Irresistable = 0x000000,
		/// <summary>Damage redukovano magickym rezistem (neplest s obranou mysli), i kdyz mozna nic takovyho neexistuje ;)</summary>
		Magic = 0x000001,
		Fire = 0x000004,
		MagicFire = Magic | Fire,
		Poison = 0x000040,
		MagicPoison = Magic | Poison,
		Electric = 0x000008,
		MagicElectric = Magic | Electric,
		Acid = 0x000010,
		MagicAcid = Magic | Acid,
		Cold = 0x000020,
		MagicCold = Magic | Cold,
		/// <summary>Mystikuv utok</summary>
		Mystical = 0x000080,
		MagicMystical = Magic | Mystical,
		/// <summary>Damage redukovano fyzickym rezistem (neplest s armorem)</summary>
		Physical = 0x000002,
		/// <summary>Secne zbrane (mece, sekery) </summary>
		Slashing = 0x000100,
		PhysicalSlashing = Physical | Slashing,
		/// <summary>Bodne zbrane (mece, dyky, vidle) (drive piercing, prejmenovano aby se to nepletlo s prubojnosti)</summary>
		Stabbing = 0x000200,
		PhysicalStabbing = Physical | Stabbing,
		/// <summary>Secne bodne zbrane (mece)</summary>
		Sharp = Slashing | Stabbing,
		PhysicalSharp = Physical | Sharp,
		/// <summary>Tupe zbrane (hole, palcaty)</summary>
		Blunt = 0x000400,
		PhysicalBlunt = Physical | Blunt,
		/// <summary>Palne zbrane (luky, kuse)</summary>
		Archery = 0x000800,
		PhysicalArchery = Physical | Archery,
		Bleed = 0x001000,
		PhysicalBleed = Physical | Bleed,

		Summon = 0x002000,
		Dragon = 0x004000
	}

	#region Weapons enums
	//Jednorucni (Broad Sword, Cutlass, Dagger, Katana, Kryss, Long Sword, Scimitar, Short Spear, Spear, Viking Sword, War Fork) - Fencing
	//Obourucni (Axe, Bardiche, Battle Axe, Battle Large Axe, Executioner"s Axe, Double Axe, Halberd, Spear, Two Handed Axe, War Axe)- Swordsmanship
	//Tupe (Mace, War Mace, Hammer Pick, War Hammer, Maul)- Macefighting
	//Strelne (Bow, Crossbow, Heavy crossbow)- Archery
	//Hole (Gnarled staff, Quarter staff, Black staff) - Magery
	public enum WeaponType : byte {
		BareHands,//prazdne ruce
		OneHandBlunt,//jednorucni tupe - mace
		TwoHandBlunt,//dvourucni tupe - kladiva, hole
		OneHandSpike,//jednorucni bodne - noze
		TwoHandSpike,//dvourucni bodne - vidle, ostepy
		OneHandSword,//jednorucni bodne/secne - mece
		TwoHandSword,//dvourucni bodne/secne - obourucni mece
		OneHandAxe,//jednorucni secne - sekery
		TwoHandAxe,//dvourucni secne - sekery
		Bow,//luky - run archery
		XBow,//kuse - stand archery
		Undefined//NPC s undefined weapontypou na defu budou brat weapontype ze sve skutecne zbrane
	}

	public enum WeaponAnimType : byte {
		BareHands,//prazdne ruce
		HeldInLeftHand,
		HeldInRightHand,
		Bow,
		XBow,
		Undefined
	}

	[Flags]
	public enum ProjectileType : byte {
		None = 0x00,
		Bolt = 0x01, JaggedBolt = Jagged | Bolt,
		Arrow = 0x02, JaggedArrow = Jagged | Arrow,
		Throwable = 0x04, //?
		Jagged = 0x08
		//?
	}
	#endregion Weapons enums

	public enum WearableType : byte {
		Clothing = 0,
		Leather = 1,
		Studded = 2,
		Bone = 3,
		Chain = 4,
		Ring = 5,
		Plate = 6
	}

	public enum MaterialType : byte {
		None = 0,
		Metal = 1,
		Ore = 2,
		Wood = 3
	}

	public enum Material : byte {
		None = 0,
		Copper = 1, Spruce = 1,
		Iron = 2, Chestnut = 2,
		Silver = 3,
		Gold = 4,
		Verite = 5, Oak = 5,
		Valorite = 6, Teak = 6,
		Obsidian = 7, Mahagon = 7,
		Adamantinum = 8, Eben = 8,
		Mithril = 9, Elven = 9,
		Sand = 10
	}

	#region Various anims
	//taken from runuo
	[Flags]
	public enum CharAnimType : byte {
		Empty = 0x00,
		Monster = 0x01,
		Animal = 0x02,
		Sea = Animal | 0x04,
		Human = 0x08,
		Equipment = 0x10
	}

	public enum GenericAnim : byte {
		Walk = 0,
		Run = 1,
		StandStill = 2,
		RandomIdleAction = 3,
		IdleAction = 4,
		LookAround = 5,
		AttackSwing = 6,
		AttackStab = 7,
		AttackOverhead = 8,
		AttackShoot = 9,
		GetHit = 10,
		FallBackwards = 11,
		FallForwards = 12,
		Block = 13, Dodge = 13,
		AttackBareHands = 14,
		Bow = 15,
		Salute = 16,
		Drink = 17, Eat = 17,
		Cast = 18
	}

	public enum HumanAnim : byte {
		WalkUnarmed = 0,
		WalkArmed = 1,
		RunUnarmed = 2,
		RunArmed = 3,
		StandStill = 4,
		LookAround = 5,
		LookDown = 6,
		WarMode = 7,
		WarModeWithTwoHandedWeapon = 8,
		RightHandSwing = 9, OneHandSwing = 9,
		RightHandStab = 10, OneHandStab = 10,
		RightHandOverhead = 11, OneHandOverhead = 11,
		LeftHandOverhead = 12, TwoHandOverhead = 12,
		LeftHandSwing = 13, TwoHandSwing = 13,
		LeftHandStab = 14, TwoHandStab = 14,
		WalkWarMode = 15,
		CastForward = 16,
		Cast = 17,
		FireBow = 18,
		FireCrossbow = 19,
		GetHit = 20,
		FallBackwards = 21, Die1 = 21,
		FallForwards = 22, Die2 = 22,
		MountedWalk = 23,
		MountedRun = 24,
		MountedStandStill = 25,
		MountedRightHandAttack = 26, MountedCast = 26,
		MountedFireBow = 27,
		MountedFireCrossbow = 28,
		MountedLeftHandAttack = 29,
		Block = 30, Dodge = 30,
		AttackBareHands = 31,
		Bow = 32,
		Salute = 33,
		Drink = 34, Eat = 34,
		MountedSalute = 47,
		MountedBlock = 48,
		MountedGetHit = 49,	//?
		NumAnims = 50
	}

	public enum MonsterAnim : byte {
		Walk = 0,
		StandStill = 1,
		FallBackwards = 2, Die1 = 2,
		FallForwards = 3, Die2 = 3,
		Attack = 4, Attack1 = 4,
		Attack2 = 5,
		Attack3 = 6,
		Attack4 = 7,	//bow?
		Attack5 = 8,	//xbow?
		Attack6 = 9,	//throw?
		GetHit = 10,
		Cast = 11,
		Summoned = 12, Cast2 = 12, UseBreathWeapon = 13, //For dragons, breath fire
		Cast3 = 13, CastForward = 13,
		Cast4 = 14,
		BlockRight = 15,
		BlockLeft = 16,
		IdleAction = 17,
		LookAround = 18,
		Fly = 19,
		TakeOff = 20,
		GetHitWhileFlying = 21,
		NumAnims = 21
	}

	public enum AnimalAnim : byte {
		Walk = 0,
		Run = 1,
		StandStill = 2,
		Eat = 3,
		Unknown = 4,
		Attack = 5, Attack1 = 5,
		Attack2 = 6,
		GetHit = 7,
		Die = 8,
		IdleAction = 9, IdleAction1 = 9,
		IdleAction2 = 10,
		LieDown = 11, Sleep = 11,
		Die2 = 12,	//This looks identical to 5 (attack) on all the animals I've tested so far...
		NumAnims = 13
	}
	#endregion Various anims

	public enum Gender : byte {
		Undefined = 0,
		Male = 1,
		Female = 2
	}

	/// <summary>Recognized types of characters. Can be used e.g. in skills (tracking etc.)</summary>
	public enum CharacterTypes : byte {
		All = 0,
		Animals = 1,
		NPCs = 2,
		Players = 3,
		Monsters = 4
	}

	//[Flags]
	//public enum BodyAnimType : byte {
	//    Nothing =	0x00,
	//    Monster =	0x01,
	//    Animal =	0x02,
	//    Human =		0x04,
	//    Equipment =	0x08,
	//    SeaAnimal =	0x10|Animal,
	//    Male =		0x20,
	//    Female =	0x40,
	//    Ghost =		0x80
	//}	

	/// <summary>Category of characters to track, tracking phases</summary>
	public enum TrackingEnums : byte {
		//tracking phases
		Phase_Characters_Seek = 1, //after the category is selected - in this phase the surroundings will be checked for all trackable chars
		Phase_Character_Track = 2 //particular character checked to tbe tracked (displaying his footsteps)
	}

	public enum Realm {
		MordorOutcast = -2,
		Mordor = -1,
		Neutral = 0, Unknown = 0, None = 0,
		Gondor = 1,
		GondorOutcast = 2
	}

	public enum CharRelation {
		AlwaysHostile = -2, //Another realm, evil NPC vs player
		TempHostile = -1, //aggressor, guild in war, criminal?
		Neutral = 0, Unknown = 0, None = 0,
		Allied = 1, //same realm
		Friendly = 2 //same guild/allied guild, party
	}

	[Flags]
	public enum EffectFlag {
		Unknown = 0x00, None = 0x00, Zero = 0x00,
		FromSpellBook = 0x01, FromBook = 0x01,
		FromSpellScroll = 0x02, FromScroll = 0x02,
		FromPotion = 0x04,
		FromTrap = 0x08, //?
		FromAbility = 0x10,

		BeneficialEffect = 0x4000,
		HarmfulEffect = 0x8000
	}

	//kind of stolen from RunUO :)
	/// <summary>
	/// Enumeration containing all possible light types. These are only applicable to light source items, like lanterns, candles, braziers, etc.
	/// </summary>
	public enum LightType {
		/// <summary>
		/// Window shape, arched, ray shining east.
		/// </summary>
		ArchedWindowEast,
		/// <summary>
		/// Medium circular shape.
		/// </summary>
		Circle225,
		/// <summary>
		/// Small circular shape.
		/// </summary>
		Circle150,
		/// <summary>
		/// Door shape, shining south.
		/// </summary>
		DoorSouth,
		/// <summary>
		/// Door shape, shining east.
		/// </summary>
		DoorEast,
		/// <summary>
		/// Large semicircular shape (180 degrees), north wall.
		/// </summary>
		NorthBig,
		/// <summary>
		/// Large pie shape (90 degrees), north-east corner.
		/// </summary>
		NorthEastBig,
		/// <summary>
		/// Large semicircular shape (180 degrees), east wall.
		/// </summary>
		EastBig,
		/// <summary>
		/// Large semicircular shape (180 degrees), west wall.
		/// </summary>
		WestBig,
		/// <summary>
		/// Large pie shape (90 degrees), south-west corner.
		/// </summary>
		SouthWestBig,
		/// <summary>
		/// Large semicircular shape (180 degrees), south wall.
		/// </summary>
		SouthBig,
		/// <summary>
		/// Medium semicircular shape (180 degrees), north wall.
		/// </summary>
		NorthSmall,
		/// <summary>
		/// Medium pie shape (90 degrees), north-east corner.
		/// </summary>
		NorthEastSmall,
		/// <summary>
		/// Medium semicircular shape (180 degrees), east wall.
		/// </summary>
		EastSmall,
		/// <summary>
		/// Medium semicircular shape (180 degrees), west wall.
		/// </summary>
		WestSmall,
		/// <summary>
		/// Medium semicircular shape (180 degrees), south wall.
		/// </summary>
		SouthSmall,
		/// <summary>
		/// Shaped like a wall decoration, north wall.
		/// </summary>
		DecorationNorth,
		/// <summary>
		/// Shaped like a wall decoration, north-east corner.
		/// </summary>
		DecorationNorthEast,
		/// <summary>
		/// Small semicircular shape (180 degrees), east wall.
		/// </summary>
		EastTiny,
		/// <summary>
		/// Shaped like a wall decoration, west wall.
		/// </summary>
		DecorationWest,
		/// <summary>
		/// Shaped like a wall decoration, south-west corner.
		/// </summary>
		DecorationSouthWest,
		/// <summary>
		/// Small semicircular shape (180 degrees), south wall.
		/// </summary>
		SouthTiny,
		/// <summary>
		/// Window shape, rectangular, no ray, shining south.
		/// </summary>
		RectWindowSouthNoRay,
		/// <summary>
		/// Window shape, rectangular, no ray, shining east.
		/// </summary>
		RectWindowEastNoRay,
		/// <summary>
		/// Window shape, rectangular, ray shining south.
		/// </summary>
		RectWindowSouth,
		/// <summary>
		/// Window shape, rectangular, ray shining east.
		/// </summary>
		RectWindowEast,
		/// <summary>
		/// Window shape, arched, no ray, shining south.
		/// </summary>
		ArchedWindowSouthNoRay,
		/// <summary>
		/// Window shape, arched, no ray, shining east.
		/// </summary>
		ArchedWindowEastNoRay,
		/// <summary>
		/// Window shape, arched, ray shining south.
		/// </summary>
		ArchedWindowSouth,
		/// <summary>
		/// Large circular shape.
		/// </summary>
		Circle300,
		/// <summary>
		/// Large pie shape (90 degrees), north-west corner.
		/// </summary>
		NorthWestBig,
		/// <summary>
		/// Negative light. Medium pie shape (90 degrees), south-east corner.
		/// </summary>
		DarkSouthEast,
		/// <summary>
		/// Negative light. Medium semicircular shape (180 degrees), south wall.
		/// </summary>
		DarkSouth,
		/// <summary>
		/// Negative light. Medium pie shape (90 degrees), north-west corner.
		/// </summary>
		DarkNorthWest,
		/// <summary>
		/// Negative light. Medium pie shape (90 degrees), south-east corner. Equivalent to <c>LightType.SouthEast</c>.
		/// </summary>
		DarkSouthEast2,
		/// <summary>
		/// Negative light. Medium circular shape (180 degrees), east wall.
		/// </summary>
		DarkEast,
		/// <summary>
		/// Negative light. Large circular shape.
		/// </summary>
		DarkCircle300,
		/// <summary>
		/// Opened door shape, shining south.
		/// </summary>
		DoorOpenSouth,
		/// <summary>
		/// Opened door shape, shining east.
		/// </summary>
		DoorOpenEast,
		/// <summary>
		/// Window shape, square, ray shining east.
		/// </summary>
		SquareWindowEast,
		/// <summary>
		/// Window shape, square, no ray, shining east.
		/// </summary>
		SquareWindowEastNoRay,
		/// <summary>
		/// Window shape, square, ray shining south.
		/// </summary>
		SquareWindowSouth,
		/// <summary>
		/// Window shape, square, no ray, shining south.
		/// </summary>
		SquareWindowSouthNoRay,
		/// <summary>
		/// Empty.
		/// </summary>
		Empty,
		/// <summary>
		/// Window shape, skinny, no ray, shining south.
		/// </summary>
		SkinnyWindowSouthNoRay,
		/// <summary>
		/// Window shape, skinny, ray shining east.
		/// </summary>
		SkinnyWindowEast,
		/// <summary>
		/// Window shape, skinny, no ray, shining east.
		/// </summary>
		SkinnyWindowEastNoRay,
		/// <summary>
		/// Shaped like a hole, shining south.
		/// </summary>
		HoleSouth,
		/// <summary>
		/// Shaped like a hole, shining south.
		/// </summary>
		HoleEast,
		/// <summary>
		/// Large circular shape with a moongate graphic embeded.
		/// </summary>
		Moongate,
		/// <summary>
		/// Unknown usage. Many rows of slightly angled lines.
		/// </summary>
		Strips,
		/// <summary>
		/// Shaped like a small hole, shining south.
		/// </summary>
		SmallHoleSouth,
		/// <summary>
		/// Shaped like a small hole, shining east.
		/// </summary>
		SmallHoleEast,
		/// <summary>
		/// Large semicircular shape (180 degrees), north wall. Identical graphic as <c>LightType.NorthBig</c>, but slightly different positioning.
		/// </summary>
		NorthBig2,
		/// <summary>
		/// Large semicircular shape (180 degrees), west wall. Identical graphic as <c>LightType.WestBig</c>, but slightly different positioning.
		/// </summary>
		WestBig2,
		/// <summary>
		/// Large pie shape (90 degrees), north-west corner. Equivalent to <c>LightType.NorthWestBig</c>.
		/// </summary>
		NorthWestBig2
	}
}