using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1107 - Erebor's labour camp (systems/lagr.scp, systems/lagr_afk.scp): a convict chops wood on plane 1 until
	/// the camp's guard has had enough release coins. Seven sentence levels per account, separate for crimes (lagr) and
	/// for failed AFK checks (afklagr), from the converted trest_N/lumber_N and afk_trest_N/afk_lumber_N tables; past the
	/// last level the convict is a recidivist with Lumberjacking 0 who has to page a GM. The levels stay on the account
	/// when they're released; only a GM lowers them.</summary>
	public static class Camp {
		//on the account, as Erebor's account.tag(lagr), account.tag(afklagr), account.tag(vlagru)
		private static readonly TagKey levelTK = TagKey.Acquire("_campLevel_");
		private static readonly TagKey afkLevelTK = TagKey.Acquire("_campAfkLevel_");
		private static readonly TagKey inCampTK = TagKey.Acquire("_inCamp_");
		//#1481 - on a character of an account in the camp that isn't the convict, Erebor's tag.lagrjail
		private static readonly TagKey waitsInJailTK = TagKey.Acquire("_campWaitsInJail_");
		//#1480 - on the character, trestanec and the tags beside it: ConvictPlugin

		internal const string Area = "a_Lagr";
		private const int LandingMinX = 356, LandingMaxX = 505, LandingMinY = 1177, LandingMaxY = 1270, LandingZ = 0;
		private const byte LandingPlane = 1;
		internal const byte Plane = LandingPlane;
		internal const int CoinValue = 100; //lagrlogu is in hundredths of a release coin
		internal const int LogsPerCoin = 100;
		private const int MaxLogsTakenAtRelease = 10000;
		//attributes that keep an item on the convict: newbie, move_never, owned, invis (Erebor's 0b0d4)
		private const ItemAttributes Stays = ItemAttributes.attr_newbie | ItemAttributes.attr_move_never | ItemAttributes.attr_owned | ItemAttributes.attr_invis;
		//eqlayers: what's worn goes to the bank
		private static readonly LayerNames[] wornLayers = { LayerNames.Hand1, LayerNames.Hand2, LayerNames.Shoes, LayerNames.Pants, LayerNames.Shirt, LayerNames.Helmet,
			LayerNames.Gloves, LayerNames.Ring, LayerNames.Collar, LayerNames.HalfApron, LayerNames.Chest, LayerNames.Bracelet, LayerNames.Tunic, LayerNames.Earrings,
			LayerNames.Arms, LayerNames.Cape, LayerNames.Robe, LayerNames.Skirt, LayerNames.Leggins, LayerNames.Mount };

		internal static int Level(AbstractAccount account, bool afk) {
			return (account == null) ? 0 : Convert.ToInt32(account.GetTag(afk ? afkLevelTK : levelTK) ?? 0, CultureInfo.InvariantCulture);
		}

		internal static void SetLevel(AbstractAccount account, bool afk, int level) {
			TagKey key = afk ? afkLevelTK : levelTK;
			if (level <= 0) {
				account.RemoveTag(key);
			} else {
				account.SetTag(key, level);
			}
		}

		public static bool IsInCamp(Character ch) {
			return (ch.Account != null) && ch.Account.HasTag(inCampTK);
		}

		public static bool IsConvict(Character ch) {
			return ConvictPlugin.Of(ch) != null;
		}

		/// <summary>lagrlogu: hundredths of a release coin still owed. A character who isn't a convict owes nothing, and
		/// the setters leave it alone.</summary>
		public static int Sentence(Character ch) {
			return ConvictPlugin.Of(ch)?.sentence ?? 0;
		}

		internal static void SetSentence(Character ch, int hundredths) {
			if (ConvictPlugin.Of(ch) is ConvictPlugin convict) {
				convict.sentence = hundredths;
			}
		}

		/// <summary>lumberpits: the Lumberjacking a convict gets back.</summary>
		internal static int KeptLumberjacking(Character ch) {
			return ConvictPlugin.Of(ch)?.keptLumber ?? 0;
		}

		internal static void SetKeptLumberjacking(Character ch, int value) {
			if (ConvictPlugin.Of(ch) is ConvictPlugin convict) {
				convict.keptLumber = value;
			}
		}

		internal static int SentenceSkill(Character ch) {
			return ConvictPlugin.Of(ch)?.sentenceSkill ?? 0;
		}

		internal static void SetSentenceSkill(Character ch, int skill) {
			if (ConvictPlugin.Of(ch) is ConvictPlugin convict) {
				convict.sentenceSkill = skill;
			}
		}

		/// <summary>The converted tables (trest_N, lumber_N, afk_trest_N, afk_lumber_N, trestu, afk_trestu).</summary>
		internal static int Table(string name) {
			Constant constant = Constant.GetByName(name);
			return (constant == null) ? 0 : Convert.ToInt32(constant.Value, CultureInfo.InvariantCulture);
		}

		private static int Levels(bool afk) {
			return Table(afk ? "afk_trestu" : "trestu");
		}

		/// <summary>The recidivist past the last level, who has to page a GM.</summary>
		internal static bool IsRecidivist(Character ch) {
			return Level(ch.Account, false) > Levels(false);
		}

		/// <summary>lagr / afklagr: to the camp at the next level.</summary>
		public static void Send(Character who, bool afk) {
			AbstractAccount account = who.Account;
			if (account == null) {
				return;
			}
			Jail.Forgive(who, null);
			Notoriety.ClearCriminal(who);
			CampLoc texts = Loc<CampLoc>.Get(who.Language);
			if (!IsConvict(who)) {
				account.SetTag(inCampTK, true);
				ConvictPlugin convict = (ConvictPlugin) ConvictPluginDef.instance.Create();
				convict.keptName = who.Name;
				convict.keptKills = Notoriety.GetMurderCount(who);
				who.AddPlugin(ConvictPlugin.Key, convict);
				Notoriety.SetMurderCount(who, 0);
				who.Karma = 0;
				who.Name = texts.ConvictPrefix + " " + who.Name;
				if (IsInTutorial(who)) {
					StripForTutorial(who);
				} else {
					PutAwayBelongings(who);
				}
				GiveTools(who);
			}
			int level = Level(account, afk) + 1;
			SetLevel(account, afk, level);
			who.Go(Globals.dice.Next(LandingMinX, LandingMaxX + 1), Globals.dice.Next(LandingMinY, LandingMaxY + 1), LandingZ, LandingPlane);
			SetKeptLumberjacking(who, who.GetSkill(SkillName.Lumberjacking));
			string prefix = afk ? "afk_" : "";
			if (level <= Levels(afk)) {
				who.SetRealSkillValue((int) SkillName.Lumberjacking, Table(prefix + "lumber_" + level));
				SetSentence(who, Table(prefix + "trest_" + level));
				who.SysMessage(string.Format(texts.YouMustEarn, Sentence(who) / CoinValue));
				return;
			}
			if (!afk && (Sentence(who) == Table("trest_3")) && (SentenceSkill(who) == Table("lumber_3"))) {
				SetSentence(who, 0); //lagr.scp's own reset of a GM menu's leftover level 3 values
				SetSentenceSkill(who, 0);
			}
			if (!afk && (Sentence(who) > 0) && (SentenceSkill(who) > 0)) {
				who.SetRealSkillValue((int) SkillName.Lumberjacking, SentenceSkill(who));
				who.SysMessage(string.Format(texts.YouMustEarn, Sentence(who) / CoinValue));
				return;
			}
			SetSentence(who, 0);
			SetSentenceSkill(who, 0);
			who.SetRealSkillValue((int) SkillName.Lumberjacking, 0);
			who.SysMessage(afk ? texts.RecidivistAfk : texts.Recidivist);
		}

		private static bool IsInTutorial(Character who) {
			Region region = who.Region;
			return (region != null) && StringComparer.OrdinalIgnoreCase.Equals(region.Name, "tutorial");
		}

		/// <summary>inventobank: a ridden mount is shrunk into the pack first, then the pack, the dragged item and what's
		/// worn go to the bank - all but what stays on (newbie, fixed, owned, invisible).</summary>
		private static void PutAwayBelongings(Character who) {
			if (who.Mount is NPC mount) {
				who.Dismount();
				PetFigurine figurine = (PetFigurine) ThingDef.GetByDefname("i_pet_figurine").Create(who.Backpack);
				figurine.Keep(mount);
				mount.Delete();
			}
			Container bank = who.Bank;
			foreach (AbstractItem item in new List<AbstractItem>(who.Backpack)) {
				ToBank(item, bank);
			}
			ToBank(who.FindLayer(LayerNames.Dragging), bank);
			foreach (LayerNames layer in wornLayers) {
				ToBank(who.FindLayer(layer), bank);
			}
		}

		private static void ToBank(AbstractItem item, Container bank) {
			if ((item is Item it) && !item.IsDeleted && ((ItemAttributes) 0 == (Attributes(it) & Stays))) {
				it.Cont = bank;
			}
		}

		private static ItemAttributes Attributes(Item item) {
			ItemAttributes all = 0;
			foreach (ItemAttributes flag in new[] { ItemAttributes.attr_newbie, ItemAttributes.attr_move_never, ItemAttributes.attr_owned, ItemAttributes.attr_invis }) {
				if (item.HasAttribute(flag)) {
					all |= flag;
				}
			}
			return all;
		}

		/// <summary>In the tutorial: everything goes, and a night sight potion makes sure there's a pack.</summary>
		private static void StripForTutorial(Character who) {
			foreach (AbstractItem item in new List<AbstractItem>(who.Backpack)) {
				item.Delete();
			}
			foreach (LayerNames layer in wornLayers) {
				AbstractItem worn = who.FindLayer(layer);
				if (worn != null) {
					worn.Delete();
				}
			}
			ThingDef.GetByDefname("i_potion_nightsight").Create(who.Backpack);
		}

		/// <summary>tmp_lagrpacket: a hatchet and a skirt.</summary>
		private static void GiveTools(Character who) {
			foreach (string defname in new[] { "i_hatchet", "i_armor_female_skirt" }) {
				try {
					who.NewEquip((IThingFactory) ThingDef.GetByDefname(defname));
				} catch (SEException) {
					ThingDef.GetByDefname(defname).Create(who.Backpack); //that layer is taken by something that stays on
				}
			}
		}

		/// <summary>lagrpustit / afklagrpustit: tools and camp logs taken, name, kills and Lumberjacking back, coins gone,
		/// forgiven, home.</summary>
		public static void Release(Character who) {
			ConsumeOne(who, "i_hatchet");
			ConsumeOne(who, "i_armor_female_skirt");
			Consume(who, "i_log_lager", MaxLogsTakenAtRelease);
			if (who.Account != null) {
				who.Account.RemoveTag(inCampTK);
			}
			if (ConvictPlugin.Of(who) is ConvictPlugin convict) {
				Notoriety.SetMurderCount(who, convict.keptKills);
				if (convict.keptName != null) {
					who.Name = convict.keptName;
				}
				who.SetRealSkillValue((int) SkillName.Lumberjacking, convict.keptLumber);
				convict.Delete();
			}
			Consume(who, "i_gold_lagr", int.MaxValue);
			Jail.Forgive(who, null);
			Notoriety.ClearCriminal(who);
			who.SysMessage(Loc<CampLoc>.Get(who.Language).Released);
			Point4D home = (who as Player)?.Home ?? PlayerCreation.startingPosition;
			who.Go(home);
		}

		/// <summary>#1481 - the player login's lagr part (sphereevents.scp): while the account is in the camp, its other
		/// characters wait in the jail; the first login after the account is out sends such a one home.</summary>
		internal static void OnLogin(Character who) {
			if (IsInCamp(who) && !IsConvict(who)) {
				who.SetTag(waitsInJailTK, true);
				Point4D jail = Jail.JailPoint;
				if (jail != null) {
					who.Go(jail);
				}
			} else if (!IsInCamp(who) && who.HasTag(waitsInJailTK)) {
				who.RemoveTag(waitsInJailTK);
				who.Go((who as Player)?.Home ?? PlayerCreation.startingPosition);
			}
		}

		internal static bool WaitsInJail(Character who) {
			return who.HasTag(waitsInJailTK);
		}

		/// <summary>lagrodvolat / afklagrodvolat: a GM lowers a level; one in the camp is released first.</summary>
		public static void LowerLevel(Character who, bool afk) {
			if (IsInCamp(who)) {
				Release(who);
			}
			AbstractAccount account = who.Account;
			if (account != null) {
				SetLevel(account, afk, Level(account, afk) - 1);
			}
			AfkCheck.ForgiveFailure(who); //nachytanafk, numberAFKsInMonth
		}

		private static void ConsumeOne(Character who, string defname) {
			Consume(who, defname, 1);
		}

		/// <summary>Takes up to `amount` of the def from the pack and what's worn; the number taken.</summary>
		internal static int Consume(Character who, string defname, int amount) {
			ThingDef def = ThingDef.GetByDefname(defname);
			int taken = 0;
			List<AbstractItem> found = new List<AbstractItem>();
			Collect(who.Backpack, def, found);
			foreach (LayerNames layer in wornLayers) {
				AbstractItem worn = who.FindLayer(layer);
				if ((worn != null) && (worn.Def == def)) {
					found.Add(worn);
				}
			}
			foreach (AbstractItem item in found) {
				if (taken >= amount) {
					break;
				}
				int take = Math.Min(item.Amount, amount - taken);
				taken += take;
				if (take >= item.Amount) {
					item.Delete();
				} else {
					item.Amount -= take;
				}
			}
			return taken;
		}

		internal static int Count(Character who, string defname) {
			ThingDef def = ThingDef.GetByDefname(defname);
			List<AbstractItem> found = new List<AbstractItem>();
			Collect(who.Backpack, def, found);
			int count = 0;
			foreach (AbstractItem item in found) {
				count += item.Amount;
			}
			return count;
		}

		private static void Collect(AbstractItem container, ThingDef def, List<AbstractItem> found) {
			foreach (AbstractItem item in container) {
				if (item.Def == def) {
					found.Add(item);
				} else if (item is Container inner) {
					Collect(inner, def, found);
				}
			}
		}

		/// <summary>The guard's "vymena": 100 camp logs for one more release coin. The coins in the pack become one stack.</summary>
		internal static bool Exchange(Character who) {
			if (Count(who, "i_log_lager") < LogsPerCoin) {
				return false;
			}
			Consume(who, "i_log_lager", LogsPerCoin);
			int coins = Consume(who, "i_gold_lagr", int.MaxValue) + 1;
			Item stack = (Item) ThingDef.GetByDefname("i_gold_lagr").Create(who.Backpack);
			stack.Amount = coins;
			stack.SetAttribute(ItemAttributes.attr_newbie, true);
			stack.SetAttribute(ItemAttributes.attr_move_never, true);
			return true;
		}

		/// <summary>e_lagr_nodmg: in the camp, a player's blows and spells don't hurt another player.</summary>
		internal static bool ProtectsFrom(Character attacker, Character target) {
			if ((attacker == null) || (target == null) || (attacker == target) || !attacker.IsPlayer || !target.IsPlayer) {
				return false;
			}
			return IsInCampArea(attacker);
		}

		internal static bool IsInCampArea(Character ch) {
			FlaggedRegion area = RegionEventSystem.AreaOf(ch.Region);
			return (area != null) && StringComparer.OrdinalIgnoreCase.Equals(area.Defname, Area);
		}

		/// <summary>The guard, placed by .SetupWorld at the camp's point.</summary>
		internal static Point4D GuardSpot {
			get {
				return (StaticRegion.GetByDefname(Area) is StaticRegion camp) ? camp.P : null;
			}
		}
	}

	public class CampLoc : CompiledLocStringCollection<CampLoc> {
		public string ConvictPrefix = "Trestanec";
		public string YouMustEarn = "Musis ziskat {0} propoustecich zlataku.";
		public string Recidivist = "Kontatuj GM pres page jsi recidivista dostanes zvlastni trest.";
		public string RecidivistAfk = "Kontatuj GM pres page jsi recidivista a podle toho s tebou bude nalozeno";
		public string Released = "Jsi propusten";
		public string CantCarryMore = "Vic toho nepoberes";
		public string GuardHello = "Musis ziskat {0} propoustecich zlataku. Ziskas je u me vymenou za drevo, kdyz reknes \"vymena\". Pokud nemas svetlo, rekni \"svetlo\", pokud postradas sekeru, rekni \"sekera\". Az budes mit propousteci zlataky, oslov mne \"propustit\". Mas-li hlad, rekni \"jidlo\".";
		public string GuardRecidivist = "Kontaktuj GM pres page, tva postava je recidivista, ktery dostane specialni trest.";
		public string GuardNotEnoughWood = "Nemas dost dreva - pokud mi reknes \"vymena\", vymenim 100 spruce logu za 1 propusteci zlatak.";
		public string GuardHaveAxe = "Sekeru mas, tak sekej.";
		public string GuardNewAxe = "Dal jsem ti novou sekeru.";
		public string GuardOnlyHave = "Mas u sebe jen {0} zlataku. Dej mi je a pokracuj.";
		public string GuardStillNeed = "Potrebujes jeste {0} propoustecich zlataku. Ty ziskas vymenou za normalni drevo. Az budes mit dostatek dreva, oslov me \"vymena\".";
		public string GuardNeed = "Potrebujes {0} propoustecich zlataku, ty ziskas vymenou za normalni drevo, az budes mit dostatek dreva oslov me \"vymena\".";
	}
}
