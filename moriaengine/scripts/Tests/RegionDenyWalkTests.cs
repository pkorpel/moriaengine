using System;
using System.Collections.Generic;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1579 - the regions' deny walks on a pickup and a ground drop ask every region up to the world region, and a
	/// drop nothing refuses gets the default reach check. Protocol side: Tests/Protocol/RegionDenyWalkTests.cs.</summary>
	public static class RegionDenyWalkTests {
		private static void Check(string name, bool ok) {
			Console.WriteLine("REGIONWALK_CHECK: " + name + "=" + ok);
		}

		/// <summary>#1579 - a pickup from the ground and a ground drop inside a stall (a_stanek_13, in Edoras): both regions are
		/// asked, the parent's deny refuses the drop, and a drop out of reach is refused by the default reach check.</summary>
		[SteamFunction]
		public static void TestRegionDenyWalk(Character self) {
			self.Account.PLevel = 1;
			self.Go(StaticRegion.GetByDefname("a_stanek_13"));
			Region here = self.GetMap().GetRegionFor(self.X, self.Y);
			Region parent = here.Parent;
			E_RegionDenyProbe.asked.Clear();
			E_RegionDenyProbe.denyDropAt = null;
			here.AddTriggerGroup(SingletonScript<E_RegionDenyProbe>.Instance);
			parent.AddTriggerGroup(SingletonScript<E_RegionDenyProbe>.Instance);
			Item gold = (Item) ThingDef.GetByDefname("i_gold").Create(self.X, self.Y, self.Z, self.M);
			try {
				DenyResult picked = self.TryPickupItem(gold, gold.Amount);
				string pickup = string.Join(",", E_RegionDenyProbe.Names(here, parent));
				E_RegionDenyProbe.asked.Clear();
				DenyResult dropped = self.TryPutItemOnGround(self.X, self.Y, self.Z);
				string drop = string.Join(",", E_RegionDenyProbe.Names(here, parent));
				E_RegionDenyProbe.asked.Clear();
				Console.WriteLine("REGIONWALK: here=" + here.Defname + " parent=" + ((parent == null) ? "none" : parent.Defname) +
					" picked=" + picked.Allow + " pickup=" + pickup + " dropped=" + dropped.Allow + " drop=" + drop + " |");
				Check("pickupAsksEveryRegion", picked.Allow && (pickup == "pickup:here,pickup:parent"));
				Check("dropAsksEveryRegion", dropped.Allow && (drop == "drop:here,drop:parent"));

				E_RegionDenyProbe.denyDropAt = parent;
				self.TryPickupItem(gold, gold.Amount);
				DenyResult refused = self.TryPutItemOnGround(self.X, self.Y, self.Z);
				E_RegionDenyProbe.denyDropAt = null;
				Check("parentDeniesDrop", !refused.Allow && (gold.Cont == self));
				if (gold.Cont == self) {
					self.TryPutItemOnGround(self.X, self.Y, self.Z);
				}

				self.TryPickupItem(gold, gold.Amount);
				DenyResult far = self.TryPutItemOnGround(self.X + 20, self.Y, self.Z);
				Console.WriteLine("REGIONWALK: farDrop=" + far.Allow + " |");
				Check("farDropRefused", !far.Allow);
				if (gold.Cont == self) {
					self.TryPutItemOnGround(self.X, self.Y, self.Z);
				}
			} finally {
				here.RemoveTriggerGroup(SingletonScript<E_RegionDenyProbe>.Instance);
				parent.RemoveTriggerGroup(SingletonScript<E_RegionDenyProbe>.Instance);
				gold.Delete();
			}
			Console.WriteLine("REGIONWALK_DONE");
		}
	}

	/// <summary>#1579 - on a stall and its town: records each region asked on a pickup and a drop; denies a drop at one.</summary>
	public partial class E_RegionDenyProbe : CompiledTriggerGroup {
		internal static readonly List<KeyValuePair<string, Region>> asked = new List<KeyValuePair<string, Region>>();
		internal static Region denyDropAt;

		internal static List<string> Names(Region here, Region parent) {
			List<string> names = new List<string>();
			foreach (KeyValuePair<string, Region> pair in asked) {
				names.Add(pair.Key + ":" + ((pair.Value == here) ? "here" : (pair.Value == parent) ? "parent" : pair.Value.Defname));
			}
			return names;
		}

		public TriggerResult On_DenyPickupItemFrom(Region self, ScriptArgs args) {
			asked.Add(new KeyValuePair<string, Region>("pickup", self));
			return TriggerResult.Continue;
		}

		public TriggerResult On_DenyPutItemOn(Region self, DenyPutOnGroundArgs args) {
			asked.Add(new KeyValuePair<string, Region>("drop", self));
			if (self == denyDropAt) {
				args.Result = DenyResultMessages.Deny_ThatDoesNotBelongToYou;
				return TriggerResult.Cancel;
			}
			return TriggerResult.Continue;
		}
	}
}
