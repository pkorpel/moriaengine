using System;
using SteamEngine.Regions;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#912 - Erebor's i_nazgulsonly_gate (systems/nazguls.scp): a gate only Nazguls and GMs pass ("Jen pro
	/// vyvolene!" for the rest), leading to its MoreP. The user's call: a GM tool. GMs place it (.add i_nazgulsonly_gate)
	/// and set where it leads with .nazgulgateto; nothing places one by itself, as on Erebor, whose archive left only
	/// the placeholder 1001,1001.</summary>
	public static class NazgulOnlyGate {
		public const string Def = "i_nazgulsonly_gate";

		[ThreadStatic]
		private static bool jumping; //a Go fires the step triggers of where it lands: never from inside a gate's own jump

		//called by ClassManager on every startup/recompile, see ExpertQuest.Init
		public static void Init() {
			QuestNpc.Attach(Def, SingletonScript<E_NazgulOnlyGate>.Instance);
		}

		internal static void Step(Item gate, Character stepping) {
			if (!stepping.IsGM && !NazgulRings.IsNazgul(stepping)) {
				stepping.SysMessage("Jen pro vyvolene!");
				return;
			}
			Point4D to = gate.MoreP;
			if (jumping) {
				return;
			}
			if (to == null) {
				if (stepping.IsGM) {
					stepping.SysMessage("Brana nikam nevede: nastav ji .nazgulgateto");
				}
				return;
			}
			jumping = true;
			try {
				stepping.Go(to.X, to.Y, to.Z, to.M);
			} finally {
				jumping = false;
			}
		}

		/// <summary>Such a gate stands on the spot: a way there would lead straight back into a gate.</summary>
		internal static bool IsGateAt(int x, int y, byte m) {
			ThingDef def = ThingDef.GetByDefname(Def);
			foreach (AbstractItem item in Map.GetMap(m).GetItemsInRange(x, y, 0)) {
				if ((item.X == x) && (item.Y == y) && (item.Def == def)) {
					return true;
				}
			}
			return false;
		}

		/// <summary>A GM's: target the gate, then where it leads.</summary>
		[SteamFunction]
		public static void NazgulGateTo(Character self) {
			if (self is Player gm) {
				gm.SysMessage("Zamer branu jen pro vyvolene.");
				gm.Target(SingletonScript<Targ_NazgulGate>.Instance);
			}
		}
	}

	public class E_NazgulOnlyGate : CompiledTriggerGroup {
		public void On_Step(Item self, Character stepping) {
			NazgulOnlyGate.Step(self, stepping);
		}
	}

	public class Targ_NazgulGate : CompiledTargetDef {
		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			if (targetted.Def != ThingDef.GetByDefname(NazgulOnlyGate.Def)) {
				self.SysMessage("To neni brana jen pro vyvolene.");
				return TargetResult.Done;
			}
			self.SysMessage("Kam brana vede? Zamer misto.");
			self.Target(SingletonScript<Targ_NazgulGatePlace>.Instance, targetted);
			return TargetResult.Done;
		}
	}

	public class Targ_NazgulGatePlace : CompiledTargetDef {
		protected override TargetResult On_TargonPoint(Player self, IPoint3D targetted, object parameter) {
			Item gate = (Item) parameter;
			if (gate.IsDeleted) {
				return TargetResult.Done;
			}
			if (targetted is Thing thing) {
				targetted = thing.TopObj(); //not a container's own coordinates
			}
			IPoint4D at = targetted as IPoint4D;
			byte m = (at != null) ? at.M : self.M;
			if (NazgulOnlyGate.IsGateAt(targetted.X, targetted.Y, m)) {
				self.SysMessage("Tam vede jina brana jen pro vyvolene, vyber jine misto.");
				return TargetResult.RestartTargetting;
			}
			gate.MoreP = new Point4D(targetted.X, targetted.Y, targetted.Z, m);
			gate.SetAttribute(ItemAttributes.attr_move_never, true); //Erebor's @create made it move-never (and so no decay)
			self.SysMessage("Brana vede na " + gate.MoreP + ".");
			return TargetResult.Done;
		}
	}
}
