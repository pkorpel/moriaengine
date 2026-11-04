using System;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1172 - server side of Tests/Protocol/SkillRestartTests.cs: a skill under way when the world is saved goes on
	/// after the restart, as Sphere's does. A smith making two daggers, and a monster in the middle of a swing at a sheep.</summary>
	public static class SkillRestartTests {
		private static readonly TagKey holderTK = TagKey.Acquire("_skillRestartHolder_");
		private const double StrokeAfterRestartSeconds = 5; //due while the smith is still offline after the restart

		[SteamFunction]
		public static void TestSkillRestart(Character self, ScriptArgs sa) {
			string what = sa.Args.Trim().Trim('"');
			if (what == "setup") {
				//the smith: copper daggers, two of them, with a forge at hand and a light
				self.SetTag(holderTK, "smith");
				self.SetRealSkillValue((int) SkillName.Blacksmith, 1000);
				self.SetRealSkillValue((int) SkillName.Tinkering, 1000);
				Item ingots = (Item) ((ItemDef) ThingDef.GetByDefname("i_ingot_copper")).Create(self.Backpack);
				ingots.Amount = 40;
				self.TryEquip((Item) ((ItemDef) ThingDef.GetByDefname("i_hammer_smith")).Create(self.Backpack));
				Item forge = (Item) ((ItemDef) ThingDef.GetByDefname("i_forge")).Create(self.Backpack);
				forge.P((ushort) (self.X + 1), self.Y, self.Z, self.M);
				Item candle = (Item) ((ItemDef) ThingDef.GetByDefname("i_candle_lit")).Create(self.Backpack);
				candle.P(self.X, self.Y, self.Z, self.M);
				SimpleQueue<CraftingSelection> queue = new SimpleQueue<CraftingSelection>();
				queue.Enqueue(new CraftingSelection((ItemDef) ThingDef.GetByDefname("i_dagger_copper"), 2));
				CraftingProcessPlugin.StartCrafting(self, new CraftingOrder((CraftingSkillDef) SkillDef.GetBySkillName(SkillName.Blacksmith), queue));

				//the monster: held still, swinging at a sheep that can take it
				NPC sheep = (NPC) ThingDef.GetByDefname("c_sheep_woolly").Create(self.X, self.Y + 2, self.Z, self.M);
				sheep.SetTag(holderTK, "sheep");
				NPCAiTimer.EnsureInstalled(sheep);
				NPCAiTimer.GetFor(sheep).isHeld = true;
				sheep.MaxHits = 30000;
				sheep.Hits = 30000;
				NPC orc = (NPC) ThingDef.GetByDefname("c_orc_lord").Create(self.X + 1, self.Y + 2, self.Z, self.M);
				orc.SetTag(holderTK, "orc");
				NPCAiTimer.EnsureInstalled(orc);
				NPCAiTimer.GetFor(orc).isHeld = true; //no thinking: only the saved swing may hit
				WeaponSkillTargetQueuePlugin.AddTarget(orc, sheep);

				//both strokes due a little after the save, so the save catches them under way and, after the restart, the
				//smith's comes due while they're offline (it waits for the login) and the orc's simply lands
				SkillSequenceArgs.SkillStrokeTimer smithTimer = ((Character) self).CurrentSkillTimer;
				SkillSequenceArgs.SkillStrokeTimer orcTimer = orc.CurrentSkillTimer;
				if (smithTimer != null) {
					smithTimer.DueInSeconds = StrokeAfterRestartSeconds;
				}
				if (orcTimer != null) {
					orcTimer.DueInSeconds = StrokeAfterRestartSeconds;
				}
				Console.WriteLine("SKILL_RESTART: setup smithSkill=" + SkillOf(self) + " orcSkill=" + SkillOf(orc) +
					" daggers=" + Count(self, "i_dagger_copper") + " sheepHits=" + sheep.Hits + " |");
				return;
			}

			Character smith = null, orcAgain = null, sheepAgain = null;
			foreach (Thing thing in Thing.AllThings) {
				switch (thing.GetTag(holderTK) as string) {
					case "smith":
						smith = (Character) thing;
						break;
					case "orc":
						orcAgain = (Character) thing;
						break;
					case "sheep":
						sheepAgain = (Character) thing;
						break;
				}
			}
			string line = "found=" + (smith != null) + "," + (orcAgain != null) + "," + (sheepAgain != null);
			if ((smith != null) && (orcAgain != null) && (sheepAgain != null)) {
				//"loaded": what came back from the save; "after": once the strokes have had time to fire
				line += " smithSkill=" + SkillOf(smith) + " orcSkill=" + SkillOf(orcAgain) +
					" daggers=" + Count(smith, "i_dagger_copper") + " sheepHits=" + sheepAgain.Hits;
			}
			Console.WriteLine("SKILL_RESTART: " + what + " " + line + " |");
		}

		/// <summary>The skill under way: None, its name, or SkillLost when a sequence came back without its skill (#1172).</summary>
		private static string SkillOf(Character ch) {
			SkillSequenceArgs ssa = ch.CurrentSkillArgs;
			if (ssa == null) {
				return "None";
			}
			return (ssa.SkillDef == null) ? "SkillLost" : ((SkillName) ssa.SkillDef.Id).ToString();
		}

		private static int Count(Character ch, string defname) {
			ThingDef def = ThingDef.GetByDefname(defname);
			int n = 0;
			foreach (AbstractItem item in ch.Backpack) {
				if (item.Def == def) {
					n += item.Amount;
				}
			}
			return n;
		}
	}
}
