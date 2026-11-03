using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1017 - c_vetesnik (Gondor) and c_vyberci (Mordor), chars/vendors.scp and heck.scp's d_magic_item_exchanger:
	/// they identify a magic item for a price, and take ten properties' worth of identified ones for a new bag.</summary>
	public static class MagicItemShop {
		private const int ReachDistance = 3, IdentifyDistance = 6;

		internal static bool IsExchanger(NPC npc) {
			string defname = npc.Def.PrettyDefname;
			return (defname == "c_vetesnik") || (defname == "c_vyberci");
		}

		internal static bool IsMordor(NPC npc) {
			return npc.Def.PrettyDefname == "c_vyberci";
		}

		internal static bool TryOpen(NPC npc, Character from) {
			if ((npc == null) || !(from is Player player) || !IsExchanger(npc)) {
				return false;
			}
			if (Point2D.GetSimpleDistance(npc, player) > ReachDistance) {
				player.SysMessage("Na takovou vzdalenost s tebou jednat nebude.");
			} else {
				player.Dialog(player, SingletonScript<D_MagicItemExchanger>.Instance, new DialogArgs(npc));
			}
			return true;
		}

		/// <summary>identification_i_mi_magic: a target for 15 s.</summary>
		internal static void StartIdentify(NPC npc, Player player) {
			npc.Say("Tak co chces identifikovat?");
			player.SysMessage("Zamer neidentifikovany predmet.");
			player.Target(SingletonScript<Targ_IdentifyMagic>.Instance, npc);
		}

		internal static void Identify(NPC npc, Player player, Item target) {
			if (Point2D.GetSimpleDistance(npc, player) > IdentifyDistance) {
				player.SysMessage("Na takovou vzdalenost ti to neidentifikuje.");
				return;
			}
			if (target.TopObj() != player) {
				player.SysMessage("Vec musis mit u sebe.");
				return;
			}
			List<Item> items = new List<Item>();
			if (target is Container box) {
				foreach (AbstractItem inside in box) {
					if (inside is Item real) {
						items.Add(real);
					}
				}
			} else if (MagicItems.IsUnidentified(target)) {
				items.Add(target);
			} else {
				player.SysMessage("Zameruj neidentifikovane magicke predmety.");
				return;
			}
			int done = 0;
			foreach (Item item in items) {
				if (!MagicItems.IsUnidentified(item)) {
					continue;
				}
				if (!GoldUtils.Pay(player, LootSettings.instance.identifyPrice, PaySource.Usual, "Nemas dostatek zlataku pro identifikaci.")) {
					break;
				}
				MagicItems.Finish(item);
				MagicItemGmTools.CountIdentified(player, LootSettings.instance.identifyPrice); //#1070 - payedgold
				Logger.WriteInfo(true, player.Name + " identifikoval predmet " + item.Name + " (" + item.Uid + ")");
				done++;
			}
			string who = player.IsFemale ? "Identifikovala" : "Identifikoval";
			if (done == 1) {
				player.SysMessage(who + " jsi magicky predmet.");
			} else if (done > 1) {
				player.SysMessage(who + " jsi " + done + " magickych predmetu.");
			}
		}

		/// <summary>isrealmagicitem: identified magic that nobody made.</summary>
		internal static bool IsRealMagicItem(Item item) {
			return MagicItems.IsMagic(item) && (CraftingSkillDef.GetCrafter(item) == null);
		}

		/// <summary>consumeXMagicals: the pack's loose real magic items, in order, until they hold ten properties.</summary>
		internal static bool Exchange(NPC npc, Player player) {
			int needed = LootSettings.instance.exchangeAttributes;
			List<Item> marked = new List<Item>();
			int attributes = 0;
			foreach (AbstractItem loose in player.Backpack) {
				if (attributes >= needed) {
					break;
				}
				if ((loose is Item item) && IsRealMagicItem(item)) {
					attributes += MagicItems.Properties(item).Count;
					marked.Add(item);
				}
			}
			bool mordor = IsMordor(npc);
			if (attributes < needed) {
				npc.Say(mordor ? "Neopovazuj se mne podvest! Nemas dost predmetu!" : "Je mi lito, nemas dost predmetu k vymene.");
				return false;
			}
			foreach (Item item in marked) {
				Logger.WriteInfo(true, player.Name + " vymenil " + item.Name + " (" + item.Uid + ") u " + npc.Name);
				item.Delete();
			}
			MagicItemGmTools.CountExchanged(marked.Count); //#1070 - collected_items
			((ItemDef) ThingDef.GetByDefname("i_mi_bag")).Create(player.Backpack);
			if (mordor) {
				npc.Say("Hmm, nas pan nebude moc potesen, ale svoji povinnost jsi splnil.");
				npc.Say("A zde je neco pro tebe.");
			} else {
				npc.Say("Dekuji ti, zajimave predmety. Snad o ne nekdo bude mit zajem.");
				npc.Say("A zde je neco pro tebe, snad ti to bude k uzitku.");
			}
			return true;
		}

		/// <summary>cm_system.scp's e_makestring @SkillMakeItem: a weapon of quality over 170 by a smith, bowyer or carpenter
		/// at 100.0 or more, level in 200 times, gets the crafter's path as its property.</summary>
		internal static void CraftPath(Character crafter, SkillName skill, Item made) {
			if (!(crafter is Player player) || !(made is Weapon) ||
					((skill != SkillName.Blacksmith) && (skill != SkillName.Fletching) && (skill != SkillName.Carpentry)) ||
					(CraftingSkillDef.GetQuality(made) <= 170) || (crafter.GetSkill(skill) <= 999) ||
					(player.Level < Globals.dice.Next(1, 201))) {
				return;
			}
			if (!ApplyPath(player, made)) {
				return;
			}
			MagicItems.Finish(made);
			made.Name = made.Name + " vyroben od " + crafter.Name;
		}

		/// <summary>craftenhanceditem. Swords and bows are finished even without the path, as on Erebor; maces and
		/// staves without it aren't.</summary>
		internal static bool ApplyPath(Player crafter, Item made) {
			string type = (made.Type == null) ? "" : made.Type.Defname.ToLowerInvariant();
			ThingDef plain = ThingDef.GetByModel(made.Model);
			string graphic = (plain == null) ? "" : plain.Defname.ToLowerInvariant();
			if ((graphic == "i_bow") || (graphic == "i_crossbow") || (graphic == "i_crossbow_heavy")) {
				MagicItems.SetValue(made, 31, ClassAbilities.Points(crafter, "archery"));
				return true;
			}
			if (((type == "t_weapon_fence") && (graphic != "i_dagger") && (graphic != "i_knife_skinning")) ||
					((type == "t_weapon_sword") && (graphic != "i_hatchet"))) {
				if ((made.Layer == (int) LayerNames.Hand1) || (graphic == "i_war_fork")) {
					MagicItems.SetValue(made, 42, ClassAbilities.Points(crafter, "sharps"));
				} else {
					MagicItems.SetValue(made, 40, ClassAbilities.Points(crafter, "twohand"));
				}
				return true;
			}
			int crushing = ClassAbilities.Points(crafter, "crushing");
			if (type == "t_weapon_mace_smith") {
				if (crushing <= 0) {
					return false;
				}
				MagicItems.SetValue(made, 41, crushing);
				return true;
			}
			if ((type == "t_weapon_mace_staff") || (type == "t_weapon_mace_crook")) {
				if (crushing <= 0) {
					return false;
				}
				MagicItems.SetValue(made, 46, crushing);
				if (crushing > 3) {
					MagicItems.SetValue(made, 25, crushing / 4);
				}
				return true;
			}
			return false;
		}
	}

	public class Targ_IdentifyMagic : CompiledTargetDef {
		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			if (parameter is NPC npc) {
				MagicItemShop.Identify(npc, self, targetted);
			}
			return TargetResult.Done;
		}
	}

	/// <summary>magic_item_exchanger_gondor and _mordor.</summary>
	public sealed class MagicItemExchangerSpeech : AbstractSpeech {
		private static readonly string[] greetings = { "dobry", "dobre", "zdravim", "ahoj", "zdar", "cus" };

		public new static void Bootstrap() {
			new MagicItemExchangerSpeech("magic_item_exchanger_gondor").Register();
			new MagicItemExchangerSpeech("magic_item_exchanger_mordor").Register();
		}

		private MagicItemExchangerSpeech(string defname)
			: base(defname) {
		}

		protected override SpeechResult Handle(AbstractCharacter listener, SpeechArgs speechArgs) {
			return this.TryHandle(listener, speechArgs);
		}

		protected override SpeechResult TryHandle(AbstractCharacter listener, SpeechArgs speechArgs) {
			NPC npc = listener as NPC;
			Player speaker = speechArgs.Speaker as Player;
			string speech = (speechArgs.Speech ?? "").ToLowerInvariant();
			if ((npc == null) || (speaker == null)) {
				return SpeechResult.IgnoredOrActedUpon;
			}
			if (speech.Contains("vymena") || speech.Contains("menit")) {
				npc.Say(MagicItemShop.IsMordor(npc) ? "Dost bylo okouneni, ukaz, co mas!" : "Dobra, podivame se, co pro mne mas.");
				MagicItemShop.Exchange(npc, speaker);
				return SpeechResult.ActedUponExclusively;
			}
			if (speech.Contains("identifikace") || speech.Contains("identifikuj")) {
				MagicItemShop.StartIdentify(npc, speaker);
				return SpeechResult.ActedUponExclusively;
			}
			foreach (string word in greetings) {
				if (speech.Contains(word)) {
					MagicItemShop.TryOpen(npc, speaker);
					return SpeechResult.ActedUponExclusively;
				}
			}
			return SpeechResult.IgnoredOrActedUpon;
		}
	}
}

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>d_magic_item_exchanger: five pages, identify and exchange.</summary>
	public class D_MagicItemExchanger : CompiledGumpDef {
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			NPC npc = (NPC) args[0];
			bool mordor = MagicItemShop.IsMordor(npc);
			int needed = LootSettings.instance.exchangeAttributes;
			this.SetLocation(5, 5);
			this.AddPage(0);
			this.AddBackground(20, 200, 640, 350, 2620);
			this.AddImage(30, 210, 0x3182);
			this.AddImage(40, 215, mordor ? 0x3024 : 0x3025);
			this.AddLabel(220, 210, 52, mordor ? "Vyberci" : "Vetesnik");
			this.AddButton(160, 330, 0x988, 0x988, 0, GumpButtonType.Page, 2);
			this.AddLabel(210, 332, 1152, "Kdo jsi?");
			this.AddButton(325, 330, 0x988, 0x988, 0, GumpButtonType.Page, 3);
			this.AddLabel(375, 332, 1152, "Prohlednout");
			this.AddButton(160, 355, 0x988, 0x988, 0, GumpButtonType.Page, 4);
			this.AddLabel(210, 357, 175, "Identifikovat");
			this.AddButton(325, 355, 0x988, 0x988, 0, GumpButtonType.Page, 5);
			this.AddLabel(375, 357, 175, "Vymenit");

			this.AddPage(1);
			this.AddHtml(170, 230, 440, 100, mordor ? "Nazdar. Co chces?" : "Bud zdrav hrdino.", false, true);
			this.AddPage(2);
			this.AddHtml(170, 230, 440, 100, mordor ?
				"Co te po jmenu! Me jmeno neni dulezite, rikej mi vyberci. Dej mi magicke predmety libovolne kvality, s alespon " + needed + " magickymi vlastnostmi v souctu. Ty mas narok na jeden novy ode mne vymenou." :
				"Me jmeno neni dulezite, ale rikaji mi vetesnik. Byl jsem poveren sberem magickych predmetu pro vyzkumne ucely. Bohuzel v tuto chvili mam nedostatek financnich prostredku a proto ti navrhnu jiny zpusob. Ty mi das magicke predmety, ktere budou mit v souctu alespon " + needed + " magickych vlastnosti. Predmety mohou byt jakekoliv kvality a materialu, zajima mne pouze, kolik vlastnosti celkem budou mit. Ja ti obratem daruji jeden novy neidentifikovany.",
				false, true);
			this.AddPage(3);
			this.AddHtml(170, 230, 440, 100, mordor ? "Vidis vyskou krasnou zenu." : "Vidis vysokeho muze s bradkou.", false, true);
			this.AddPage(4);
			this.AddHtml(170, 230, 440, 100, "Moji dalsi schopnosti je rozpoznani magickych vlastnosti neznameho predmetu. Mas li predmet, o nemz nevis jake ma vlastnosti, mohu ti ho identifikovat. Za tuto sluzbu si vsak vezmu " + LootSettings.instance.identifyPrice + " zlatych", false, true);
			this.AddButton(25, 395, 0xfa5, 0xfa7, 1, GumpButtonType.Reply, 0);
			this.AddLabel(60, 395, 152, "Identifikovat");
			this.AddButton(25, 415, 0xfa5, 0xfa7, 0, GumpButtonType.Reply, 0);
			this.AddLabel(60, 415, 152, "Nemam zajem");
			this.AddPage(5);
			this.AddHtml(170, 230, 440, 100, "Zvaz dobre, co u sebe mas! Prejes si teda vymenit nekolik tvych identifikovanych magickych predmetu za 1 novy neidentifikovany? Vezmu si prvni co u sebe neleznes, celkem si ale vezmu predmety s alespon " + needed + " magickymi vlastnostmi v souctu.", false, true);
			this.AddButton(25, 395, 0xfa5, 0xfa7, 2, GumpButtonType.Reply, 0);
			this.AddLabel(60, 395, 152, "Ano");
			this.AddButton(25, 415, 0xfa5, 0xfa7, 0, GumpButtonType.Reply, 0);
			this.AddLabel(60, 415, 152, "Ne");
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = (Player) gi.Cont;
			NPC npc = (NPC) args[0];
			if (gr.PressedButton == 1) {
				MagicItemShop.StartIdentify(npc, player);
			} else if (gr.PressedButton == 2) {
				npc.Say("Dobra, podivame se, co pro mne mas.");
				MagicItemShop.Exchange(npc, player);
			}
		}
	}
}
