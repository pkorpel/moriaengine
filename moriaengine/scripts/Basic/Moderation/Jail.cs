using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1107 - Sphere 0.99z8's JAIL and FORGIVE (EreborNext CChar.PrivilegeMethods): the account is jailed, the character
	/// remembers where it stood and goes to the jail area. While jailed, any move elsewhere (a spell, a gate, a telepad, a stone,
	/// a GM's go) lands in jail again, and a login puts it back there. Forgiving clears it and sends the character back.
	/// Sphere also keeps a release time, but its expiry does nothing (an empty branch in 0.99z8's CAccount::OnTick), so
	/// a jailed player stays until forgiven: no time here.</summary>
	public static class Jail {
		private static readonly TagKey jailedTK = TagKey.Acquire("_jailed_");
		private static readonly TagKey releasePointTK = TagKey.Acquire("_jailReleasePoint_");
		private const string JailArea = "a_Jail";

		public static bool IsJailed(Character ch) {
			AbstractAccount account = ch.Account;
			return (account != null) && account.HasTag(jailedTK);
		}

		/// <summary>The jail area's point, Sphere's GetRegionPoint("jail").</summary>
		public static Point4D JailPoint {
			get {
				return (StaticRegion.GetByDefname(JailArea) is StaticRegion jail) ? jail.P : null;
			}
		}

		/// <summary>JAIL: true if it went.</summary>
		public static bool Send(Character who) {
			AbstractAccount account = who.Account;
			if ((account == null) || (JailPoint == null)) {
				return false;
			}
			if (!account.HasTag(jailedTK)) {
				who.SetTag(releasePointTK, new Point4D(who.X, who.Y, who.Z, who.M));
			}
			account.SetTag(jailedTK, true);
			Effect(who);
			return true;
		}

		/// <summary>JailEffect: to the jail, with the message.</summary>
		private static void Effect(Character who) {
			if (!who.HasTag(releasePointTK)) {
				who.SetTag(releasePointTK, new Point4D(who.X, who.Y, who.Z, who.M));
			}
			Point4D jail = JailPoint;
			if (jail != null) {
				who.P(jail.X, jail.Y, jail.Z, jail.M);
				who.Fix();
			}
			who.SysMessage(Loc<JailLoc>.Get(who.Language).Jailed);
		}

		/// <summary>FORGIVE / PARDON. False if they weren't jailed.</summary>
		public static bool Forgive(Character who, Character gm) {
			AbstractAccount account = who.Account;
			if ((account == null) || !account.HasTag(jailedTK)) {
				return false;
			}
			account.RemoveTag(jailedTK);
			JailLoc texts = Loc<JailLoc>.Get(who.Language);
			if (gm != null) {
				gm.SysMessage(string.Format(Loc<JailLoc>.Get(gm.Language).Forgiven, who.Name));
			}
			who.SysMessage(texts.YouAreForgiven);
			if (!(who.GetTag(releasePointTK) is Point4D back)) {
				if (gm != null) {
					gm.SysMessage(Loc<JailLoc>.Get(gm.Language).NoReleasePoint);
				}
				return true;
			}
			who.RemoveTag(releasePointTK);
			who.Go(back);
			return true;
		}

		/// <summary>Spell_Effect_Teleport for a jailed character: back to jail instead. True when it did.</summary>
		internal static bool Redirect(Character who) {
			if (!IsJailed(who) || (JailPoint == null)) {
				return false;
			}
			JailLoc texts = Loc<JailLoc>.Get(who.Language);
			who.SysMessage((Globals.dice.Next(2) == 0) ? texts.Punishing : texts.Ashamed);
			Point4D jail = JailPoint;
			who.P(jail.X, jail.Y, jail.Z, jail.M);
			who.Fix();
			return true;
		}

		/// <summary>Setup_Start: a jailed character logs in to jail.</summary>
		internal static void OnLogin(Character who) {
			if (IsJailed(who)) {
				Effect(who);
			}
		}

		[SteamFunction("Jail")]
		public static void JailCommand(Player self) {
			self.Target(SingletonScript<Targ_Jail>.Instance);
		}

		[SteamFunction]
		public static void Forgive(Player self) {
			self.Target(SingletonScript<Targ_Forgive>.Instance);
		}

		[SteamFunction]
		public static void Pardon(Player self) {
			Forgive(self);
		}
	}

	public class Targ_Jail : CompiledTargetDef {
		protected override void On_Start(Player self, object parameter) {
			self.SysMessage(Loc<JailLoc>.Get(self.Language).WhoToJail);
			base.On_Start(self, parameter);
		}

		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			if (!targetted.IsPlayer || (targetted.Account == null)) {
				self.SysMessage(Loc<JailLoc>.Get(self.Language).OnlyPlayers);
				return TargetResult.RestartTargetting;
			}
			if (Jail.Send(targetted)) {
				self.SysMessage(string.Format(Loc<JailLoc>.Get(self.Language).YouJailed, targetted.Name));
			}
			return TargetResult.Done;
		}
	}

	public class Targ_Forgive : CompiledTargetDef {
		protected override void On_Start(Player self, object parameter) {
			self.SysMessage(Loc<JailLoc>.Get(self.Language).WhoToForgive);
			base.On_Start(self, parameter);
		}

		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			if (!Jail.Forgive(targetted, self)) {
				self.SysMessage(string.Format(Loc<JailLoc>.Get(self.Language).NotJailed, targetted.Name));
			}
			return TargetResult.Done;
		}
	}

	public class JailLoc : CompiledLocStringCollection<JailLoc> {
		public string Jailed = "Byl jsi uveznen.";
		public string YouAreForgiven = "Bylo ti odpusteno.";
		public string Forgiven = "{0} je propusten z vezeni.";
		public string NoReleasePoint = "Neni znamo, odkud prisel - zustava, kde je.";
		public string Punishing = "Citis, ze te svet tresta.";
		public string Ashamed = "Citis, ze by ses mel za sve ciny stydet.";
		public string WhoToJail = "Koho zavrit do vezeni?";
		public string WhoToForgive = "Komu odpustit?";
		public string OnlyPlayers = "Do vezeni jen hrace.";
		public string YouJailed = "{0} je ve vezeni.";
		public string NotJailed = "{0} neni ve vezeni.";
	}
}
