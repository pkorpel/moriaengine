using System;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1116 - Erebor's war lieutenants (dobyvani/namestek_valky_funkce.scp, namestek_valky_npc.scp): Norbert
	/// (c_namestek_valky_g) sells Gondor's players the right to declare war on Mordor, Urdak (c_namestek_valky_m) Mordor's on
	/// Gondor, for 100 000 gold. A double click within 3 tiles or *ahoj*/*hi* from a player of the realm opens the dialog;
	/// anyone else hears "Tahni!". Buying ("Vyhlasit utok") wants skill rank 3 and level 30, no captain, the right realm, a
	/// keep left to take, and no right bought already; then GoldUtils.Pay.
	/// Fixed from Erebor: the double click's distance is to the lieutenant (Erebor measured the player's last target), and a
	/// player of neither realm can't buy either right (Erebor only refused the other realm).</summary>
	public static class WarLieutenant {
		public const string GondorDef = "c_namestek_valky_g", MordorDef = "c_namestek_valky_m";
		public const int Price = 100000, MinLevel = 30;
		private const int Reach = 3;

		/// <summary>The realm whose lieutenant this is, None for any other NPC.</summary>
		public static Realm RealmOf(Character npc) {
			string defname = npc.Def.PrettyDefname;
			if (string.Equals(defname, GondorDef, StringComparison.OrdinalIgnoreCase)) {
				return Realm.Gondor;
			}
			if (string.Equals(defname, MordorDef, StringComparison.OrdinalIgnoreCase)) {
				return Realm.Mordor;
			}
			return Realm.None;
		}

		/// <summary>f_namestek_valky_g/_m, from Character.On_DClick. True when this was a lieutenant.</summary>
		internal static bool TryDClick(NPC npc, Character from) {
			Realm realm = RealmOf(npc);
			if ((realm == Realm.None) || !(from is Player player)) {
				return false;
			}
			if (Point2D.GetSimpleDistance(npc, player) > Reach) {
				player.SysMessage("Na takovou vzdalenost s tebou jednat nebude.");
			} else if (player.Realm == realm) {
				npc.Say("Chces zautocit...?");
				Open(npc, player);
			} else {
				npc.Say((realm == Realm.Mordor) ? "Tahni..." : "Tahni!");
			}
			return true;
		}

		internal static void Open(NPC npc, Player player) {
			if (RealmOf(npc) == Realm.Gondor) {
				player.Dialog(player, SingletonScript<D_WarLieutenantGondor>.Instance, new DialogArgs(npc));
			} else {
				player.Dialog(player, SingletonScript<D_WarLieutenantMordor>.Instance, new DialogArgs(npc));
			}
		}

		/// <summary>war_will_pay, in Erebor's order. What the lieutenant says when he refuses, or null when the right is bought.</summary>
		public static string Buy(NPC npc, Player player) {
			Realm realm = RealmOf(npc);
			if (((int) player.SkillRank < (int) SkillRank.Grandmaster) || (player.Level < MinLevel)) {
				return "Je mi lito, ale na nejaky realny uspech ve valce nemas dostatek zkusenosti.";
			}
			if (KeepCaptains.IsCaptain(player)) {
				return "Ty uz kapitanem jsi. Starej se o svou pevnost a vedeni dalsi valky prenech jinemu.";
			}
			if (player.Realm != realm) {
				return (realm == Realm.Gondor) ? "Vypadni prasivej mordore!" : "Vypadni prasivej gondore!";
			}
			WarFront front = Conquest.Front(realm);
			if ((realm == Realm.Gondor) ? (front.conquered <= -2) : (front.conquered >= 2)) {
				return "Vzdyt uz jsi tem " + ((realm == Realm.Gondor) ? "Mordoranum" : "Gondoranum") + " vsechno " + Conquest.Word(player, "dobyl", "dobyla") + "!";
			}
			if (Conquest.HasRight(player)) {
				return "Je zbytecne platit mi za dalsi vyhlasovani valky, kdyz uz jsi to jednou " + Conquest.Word(player, "udelal", "udelala") + ".";
			}
			if (GoldUtils.Pay(player, Price)) {
				Conquest.GiveRight(player);
				Logger.WriteInfo(true, player.Name + " paid " + Price + " gp for the right to declare war on " + Conquest.RealmName(Conquest.Enemy(realm)));
			}
			return null;
		}
	}

	/// <summary>spk_namestek_valky_g/_m: *ahoj*, *hi*.</summary>
	public sealed class WarLieutenantSpeech : AbstractSpeech {
		public new static void Bootstrap() {
			new WarLieutenantSpeech("spk_namestek_valky_g").Register();
			new WarLieutenantSpeech("spk_namestek_valky_m").Register();
		}

		private WarLieutenantSpeech(string defname)
			: base(defname) {
		}

		protected override SpeechResult Handle(AbstractCharacter listener, SpeechArgs speechArgs) {
			return this.TryHandle(listener, speechArgs);
		}

		protected override SpeechResult TryHandle(AbstractCharacter listener, SpeechArgs speechArgs) {
			NPC npc = listener as NPC;
			Player player = speechArgs.Speaker as Player;
			string speech = speechArgs.Speech;
			if ((npc == null) || (player == null) || (speech == null)) {
				return SpeechResult.IgnoredOrActedUpon;
			}
			if ((speech.IndexOf("ahoj", StringComparison.OrdinalIgnoreCase) < 0) && (speech.IndexOf("hi", StringComparison.OrdinalIgnoreCase) < 0)) {
				return SpeechResult.IgnoredOrActedUpon;
			}
			Realm realm = WarLieutenant.RealmOf(npc);
			if (player.Realm == realm) {
				npc.Say((realm == Realm.Mordor) ? "Chces zautocit...?" : "Hmmm?");
				WarLieutenant.Open(npc, player);
			} else {
				npc.Say("Tahni!");
			}
			return SpeechResult.IgnoredOrActedUpon;
		}
	}

	/// <summary>d_namestek_valky_g and _m: Erebor's texts, page for page.</summary>
	public abstract class D_WarLieutenant : QuestDialogDef {
		protected abstract string AttackText(Player player);

		protected override void Build(QuestDialogContent d, Character npc, Player player) {
			d.BaseButton(1, 51, "Vyhlasit utok");
			d.BaseButton(2, 51, "Nejake drby ?");
			d.BaseButton(3, 51, "Obchodovat ?");
			d.BaseButton(4, 51, "Hledam praci");
			d.BaseButton(5, 51, "Nesu ti ...");
			d.BaseButton(6, 51, "Prohlednout");
			d.Page("Pozdrav");
			d.SetText("Hmm...");
			d.Html();
			d.Page("Vyhlasit utok");
			d.SetText(this.AttackText(player));
			d.Html();
			d.SetText("Ano, to mi vyhovuje. Zaplatim 100 000 GP.");
			d.Response(false, true, "war_will_pay");
			d.SetText("Sto tisic je pro me mnoho penez, rad bych neco jineho..");
			d.Response(false, true, "nowar");
			d.Page("Nejake drby ?");
			d.SetText("Hehe.. Mno drby tak stim ti nepomohu ja je neposloucham ani neposilam dal..");
			d.Html();
			d.Page("Obchodovat ?");
			d.SetText("Myslis si ze jsem zde  od toho abych tu obchodoval ? Ja nejsem obchodnik jasny ?");
			d.Html();
			d.Page("Hledam praci");
			d.SetText("Jedine do Armady.. nic jineho nema budoucnost.. Ta te nakrmi pobavi i zahubi.. vse v jednom baleni.. Stalo by zato navtivit but kapitana mestske straze  nebo narukovat k Armade.. Inu to vis..Tohle je stred sveta.. Minas Tirith");
			d.Html();
			d.Page("Nesu ti ...");
			d.SetText("Ano?");
			d.Html();
			d.Page("Prohlednout");
			d.SetText("Tento mlady muz rozhodne nevypada nato ze svou dosavadni sluzbu prozil prave v knihovne..  Na obliceji i na rukach");
			d.Html();
		}

		protected override void OnButton(string button, Character npc, Player player) {
			if (button == "war_will_pay") {
				string refused = WarLieutenant.Buy((NPC) npc, player);
				if (refused != null) {
					npc.Say(refused);
				}
			} else if (button == "nowar") {
				npc.Say("Tak s tim se neda nic delat, musis si nasetrit.");
			}
		}
	}

	public sealed class D_WarLieutenantGondor : D_WarLieutenant {
		protected override int Face {
			get { return 0x3029; }
		}

		protected override string AttackText(Player player) {
			return "Jejda " + Conquest.Word(player, "mladej panacek", "slecna") + " porada utok na nove pevnosti ?  No ale ber na vedomi ze te to bude neco stat. Uredni schvaleni, glejd, zalohy na leky pro ranene z fronty. No rekneme ze celkem mi sem slozis 100 tisic GP jasny ?";
		}
	}

	public sealed class D_WarLieutenantMordor : D_WarLieutenant {
		protected override int Face {
			get { return 0x3047; }
		}

		protected override string AttackText(Player player) {
			return "Konecne rozumna rec " + Conquest.Word(player, "chlape", "zencka") + " poradne jim to nandame. Boli me kazde nadechnuti kdyz vim ze ti smrduti Gondori stale nejsou v nesem pevnem sevreni. A nebo jeste lip, mrtvej Gondor, dobrej Gondor!  Bouzel mam ale pro tebe jednu smutnou zpravu, bude te to neco stat. Posveceni Marghulem, a zalohy na alkohol pro oslavu vitezstvi. No rekneme ze celkem mi sem slozis 100 tisic GP, a bude to v cajku. Jasny ?";
		}
	}
}
