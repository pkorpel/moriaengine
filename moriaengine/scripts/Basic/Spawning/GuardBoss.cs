using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1234 - Erebor's guard boss system (systems/guardBossSystem.scp, .guardBoss): a spawn can be guarded by other
	/// spawns. While a monster of any guard spawn lives, the guarded spawn's monsters take no damage (one check in
	/// DamageManager, so swings, spells, ticks and abilities alike; Erebor set their resists to 1000, which Irresistable
	/// damage would pass). They can still be fought, and on 1 hit in 5 say one of Erebor's lines. A guard spawn can itself
	/// be guarded, so a dungeon opens layer by layer; each boss looks at its own guards only, one level, as Erebor's.
	/// Fixed from Erebor: a loop (A guards B, B guards A, or longer) is refused, Erebor only warned in its help text.
	/// The links live on the boss's gem, set from its .spawn dialog ("Guarded by").</summary>
	public static class GuardBoss {
		private static readonly TagKey countTK = TagKey.Acquire("_guardBoss_count_");
		private static readonly TagKey spawnGemTK = TagKey.Acquire("spawnGem");
		private static readonly TagKey spawnedCountTK = TagKey.Acquire("spawnedCount");
		private static readonly TagKey spawnInfoTK = TagKey.Acquire("spawnInfo");
		private static readonly string[] lines = { "Muj strazce je na zivu!", "Nemuzes mne zranit!", "Kouzla ani zbrane mi neublizi!", "Muhaha!" };

		private static TagKey GuardTK(int i) {
			return TagKey.Acquire("_guardBoss_" + i.ToString(CultureInfo.InvariantCulture) + "_");
		}

		public static bool IsGem(Thing thing) {
			return (thing is Item) && !thing.IsDeleted && (thing.GetTag(spawnInfoTK) != null);
		}

		/// <summary>The gems guarding this one, as linked (a deleted one stays listed as null until removed).</summary>
		public static List<Item> GuardsOf(Thing bossGem) {
			int count = (bossGem == null) ? 0 : Convert.ToInt32(bossGem.GetTag(countTK) ?? 0, CultureInfo.InvariantCulture);
			List<Item> guards = new List<Item>(count);
			for (int i = 0; i < count; i++) {
				Item gem = bossGem.GetTag(GuardTK(i)) as Item;
				guards.Add(((gem == null) || gem.IsDeleted) ? null : gem);
			}
			return guards;
		}

		private static void Write(Thing bossGem, List<Item> guards) {
			int old = Convert.ToInt32(bossGem.GetTag(countTK) ?? 0, CultureInfo.InvariantCulture);
			for (int i = guards.Count; i < old; i++) {
				bossGem.RemoveTag(GuardTK(i));
			}
			for (int i = 0; i < guards.Count; i++) {
				bossGem.SetTag(GuardTK(i), guards[i]);
			}
			if (guards.Count > 0) {
				bossGem.SetTag(countTK, guards.Count);
			} else {
				bossGem.RemoveTag(countTK);
			}
		}

		/// <summary>How many monsters of this gem live (spawn.scp's own count: a death deletes the NPC and @destroy lowers
		/// it at once, taming lowers it through @leaveSpawn).</summary>
		public static int Alive(Item gem) {
			return ((gem == null) || gem.IsDeleted) ? 0 : Convert.ToInt32(gem.GetTag(spawnedCountTK) ?? 0, CultureInfo.InvariantCulture);
		}

		/// <summary>f_guardBossSystem_hasGuard: a guard gem of this NPC's own gem still has a monster alive.</summary>
		public static bool IsGuarded(Character ch) {
			if (!(ch is NPC) || !(ch.GetTag(spawnGemTK) is Item bossGem) || (bossGem.GetTag(countTK) == null)) {
				return false;
			}
			foreach (Item guard in GuardsOf(bossGem)) {
				if (Alive(guard) > 0) {
					return true;
				}
			}
			return false;
		}

		/// <summary>DamageManager.CauseDamage: true loses the damage.</summary>
		internal static bool Refuses(Character defender) {
			if (!IsGuarded(defender)) {
				return false;
			}
			EffectFactory.StationaryEffect(defender, 0x37B9, 10, 5);
			if (Globals.dice.Next(5) == 0) {
				defender.Say(lines[Globals.dice.Next(lines.Length)]);
			}
			return true;
		}

		/// <summary>Links `guard` to guard `boss`; null, or why not (each its own message).</summary>
		public static string Link(Item boss, Thing guard) {
			if (!IsGem(guard)) {
				return "That is not a spawn gem.";
			}
			if (guard == boss) {
				return "A spawn can't guard itself.";
			}
			List<Item> guards = GuardsOf(boss);
			if (guards.Contains((Item) guard)) {
				return "That spawn guards it already.";
			}
			List<Item> chain = PathOfGuards((Item) guard, boss);
			if (chain != null) {
				return "A loop: " + Describe((Item) guard) + " is already guarded by " + Describe(boss) +
					((chain.Count > 2) ? (" through " + string.Join(", ", chain.GetRange(1, chain.Count - 2).ConvertAll(Describe))) : "") + ".";
			}
			guards.Add((Item) guard);
			Write(boss, guards);
			return null;
		}

		public static void Unlink(Item boss, int index) {
			List<Item> guards = GuardsOf(boss);
			if ((index >= 0) && (index < guards.Count)) {
				guards.RemoveAt(index);
				Write(boss, guards);
			}
		}

		/// <summary>The chain from `from` down its guards to `target` (both included), or null when `target` doesn't guard
		/// `from` at any depth. Linking `from` to guard `target` would then close a loop.</summary>
		private static List<Item> PathOfGuards(Item from, Item target) {
			HashSet<Item> visited = new HashSet<Item>();
			List<Item> path = new List<Item>();
			return Search(from, target, visited, path) ? path : null;
		}

		private static bool Search(Item at, Item target, HashSet<Item> visited, List<Item> path) {
			if ((at == null) || !visited.Add(at)) {
				return false;
			}
			path.Add(at);
			if (at == target) {
				return true;
			}
			foreach (Item next in GuardsOf(at)) {
				if (Search(next, target, visited, path)) {
					return true;
				}
			}
			path.RemoveAt(path.Count - 1);
			return false;
		}

		/// <summary>"x,y (first monster)", for messages and the dialog.</summary>
		public static string Describe(Item gem) {
			if ((gem == null) || gem.IsDeleted) {
				return "(deleted spawn)";
			}
			string what = "";
			if ((gem.GetTag(spawnInfoTK) is SpawnInfo info) && (info.EntryCount > 0)) {
				what = " " + info.GetEntry(0).SpawnDefDisplayName;
			}
			return gem.X + "," + gem.Y + what;
		}

		/// <summary>From d_spawn's "Guarded by" button.</summary>
		public static void ShowDialog(Character gm, Item bossGem) {
			if ((gm is Player player) && IsGem(bossGem)) {
				player.DialogForget(SingletonScript<D_GuardBoss>.Instance);
				player.Dialog(player, SingletonScript<D_GuardBoss>.Instance, new DialogArgs(bossGem));
			}
		}
	}

	/// <summary>#1234 - d_guardBossSystem: a row per guard gem (where, what, alive, Go, remove), then "+ Guarded by".</summary>
	public class D_GuardBoss : CompiledGumpDef {
		internal const int Add = 2, GoBase = 100, RemoveBase = 200;
		private const int Width = 520, Row = 22, LabelHue = 2301, BadHue = 33;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Item boss = (Item) args[0];
			List<Item> guards = GuardBoss.GuardsOf(boss);
			int height = 110 + (Math.Max(guards.Count, 1) * Row);
			this.SetLocation(60, 80);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, height, 2620);
			this.AddLabel(14, 12, LabelHue, "Guarded by - spawn " + GuardBoss.Describe(boss));
			int y = 40;
			if (guards.Count == 0) {
				this.AddLabel(14, y, LabelHue, "No guards: its monsters can be hurt.");
			}
			for (int i = 0; i < guards.Count; i++) {
				Item guard = guards[i];
				if (guard != null) {
					this.AddButton(14, y, 4005, 4007, D_GuardBoss.GoBase + i, GumpButtonType.Reply, 0);
				}
				this.AddButton(48, y, 4017, 4019, D_GuardBoss.RemoveBase + i, GumpButtonType.Reply, 0);
				this.AddLabelCropped(84, y, Width - 100, Row, (guard == null) ? BadHue : LabelHue,
					GuardBoss.Describe(guard) + ((guard == null) ? "" : (", alive " + GuardBoss.Alive(guard))));
				y += Row;
			}
			this.AddButton(14, height - 34, 4005, 4007, Add, GumpButtonType.Reply, 0);
			this.AddLabel(50, height - 32, LabelHue, "+ Guarded by");
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if (!(gi.Cont is Player gm) || !gm.IsGM || !(args[0] is Item boss) || boss.IsDeleted) {
				return;
			}
			int button = gr.PressedButton;
			List<Item> guards = GuardBoss.GuardsOf(boss);
			if (button == Add) {
				gm.SysMessage("Which spawn guards this one?");
				gm.Target(SingletonScript<Targ_GuardBoss>.Instance, boss);
				return;
			}
			if ((button >= GoBase) && (button - GoBase < guards.Count) && (guards[button - GoBase] != null)) {
				Item guard = guards[button - GoBase];
				gm.Go(guard.X, guard.Y, guard.Z, guard.M);
			} else if ((button >= RemoveBase) && (button - RemoveBase < guards.Count)) {
				GuardBoss.Unlink(boss, button - RemoveBase);
			} else {
				return;
			}
			GuardBoss.ShowDialog(gm, boss);
		}
	}

	public class Targ_GuardBoss : CompiledTargetDef {
		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			string refusal = GuardBoss.Link((Item) parameter, targetted);
			if (refusal != null) {
				self.RedMessage(refusal);
			} else {
				self.SysMessage("Spawn " + GuardBoss.Describe(targetted) + " now guards it.");
			}
			GuardBoss.ShowDialog(self, (Item) parameter);
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			self.RedMessage("That is not a spawn gem.");
			return TargetResult.RestartTargetting;
		}
	}
}
