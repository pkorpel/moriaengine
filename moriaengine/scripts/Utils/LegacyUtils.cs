using System;
using System.Text;
using SteamEngine.Common;
using SteamEngine.Networking;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>Methods for simulating of sphereserver API in some cases</summary>
	public static class LegacyUtils {

		//the same as currentskill, only backward compatible with sphere

		[SteamFunction]
		public static int Action(Character self, ScriptArgs sa) {
			if ((sa != null) && (sa.Argv.Length > 0)) {
				int value = Convert.ToInt32(sa.Argv[0]);
				if ((value != (int) self.CurrentSkillName) || (value < 0) || (value >= AbstractSkillDef.SkillsCount)) {
					self.AbortSkill();
				}
				return 0;
			}
			if (self.CurrentSkill == null) {
				return -1;
			}
			return (int) self.CurrentSkillName;
		}

		[SteamFunction]
		public static void Go(Character self, string s) {
			StaticRegion reg = StaticRegion.GetByNameOrDefname(s);
			if (reg != null) {
				self.P(reg.P);
				return;
			}

			//translate s to coordinates
			bool parse = true;
			string constant = null;
			while (parse) {
				parse = false;
				string[] args = Utility.SplitSphereString(s, true);
				switch (args.Length) {
					case 1: {
							if (constant == null) {
								object o = Constant.GetValue(s);
								if (o is string) {
									Logger.WriteDebug("Resolved constant '" + s + "' to " + o);
									constant = s;
									s = (string) o;
									parse = true;
								} else {
									throw new SanityCheckException("We found a constant named '" + s + "', but it was a " + o.GetType() + " -- we expected a string.");
								}
							} else {
								throw new SanityCheckException("We found a constant named '" + s + "', but it didn't resolve to anything meaningful.");
							}
							break;
						}
					case 2: {
							self.Go(ConvertTools.ParseUInt16(args[0]), ConvertTools.ParseUInt16(args[1]));
							break;
						}
					case 3: {
							self.Go(ConvertTools.ParseUInt16(args[0]), ConvertTools.ParseUInt16(args[1]), ConvertTools.ParseSByte(args[3]), ConvertTools.ParseByte(args[4]));
							break;
						}
					case 4: {
							self.Go(ConvertTools.ParseUInt16(args[0]), ConvertTools.ParseUInt16(args[1]), ConvertTools.ParseSByte(args[3]));
							return;
						}
					default: {
							if (args.Length > 4) {
								throw new SanityCheckException("Too many args (" + args.Length + ") to Go(\"" + s + "\"), expected no more than 4.");
							} //if (args.Length<2) {
						throw new SanityCheckException("Too few args (" + args.Length + ") to Go(\"" + s + "\"), expected at least 2.");
					}
				}
			}
			//Update();
		}

		[SteamFunction]
		public static void AddEvent(ITriggerGroupHolder self, TriggerGroup tg) {
			self.AddTriggerGroup(tg);
		}

		[SteamFunction]
		public static void RemoveEvent(ITriggerGroupHolder self, TriggerGroup tg) {
			self.RemoveTriggerGroup(tg);
		}

		[SteamFunction]
		public static bool HasEvent(ITriggerGroupHolder self, TriggerGroup tg) {
			return self.HasTriggerGroup(tg);
		}

		[SteamFunction]
		public static void Effect(Thing self, byte type, int effect, byte speed, byte duration, bool fixedDirection) {
			switch (type) {
				case 0:
					EffectFactory.EffectFromTo(Globals.SrcCharacter, self,
						effect, speed, duration, fixedDirection, false, 0, 0);
					break;
				case 1:
					EffectFactory.LightningEffect(self);
					break;
				case 2:
					EffectFactory.StationaryEffectAt(self, effect, speed, duration, fixedDirection, false, 0, 0);
					break;
				case 3:
					EffectFactory.StationaryEffect(self, effect, speed, duration, fixedDirection, false, 0, 0);
					break;
				default:
					Logger.WriteWarning("Unknown effect type '" + type + "'. Sending it anyways.");
					GraphicalEffectOutPacket p = Pool<GraphicalEffectOutPacket>.Acquire();
					p.Prepare(Globals.SrcCharacter,
						self, type, effect, speed, duration, 0, fixedDirection, false, 0, 0);
					GameServer.SendToClientsWhoCanSee(self, p);
					break;
			}
		}

		[SteamFunction]
		public static void Events(ITriggerGroupHolder self, ScriptArgs sa) {
			if (sa != null) {
				object[] argv = sa.Argv;
				if (argv.Length > 0) {
					object firstArg = argv[0];
					TriggerGroup tg = firstArg as TriggerGroup;
					if (tg != null) {
						Events(self, tg);
						return;
					}
					TGRemover tgr = firstArg as TGRemover;
					if (tgr != null) {
						Events(self, tgr);
						return;
					}
					int i = Convert.ToInt32(firstArg);
					Events(self, i);
					return;
				}
			}
			Events(self);
		}

		public static void Events(ITriggerGroupHolder self, TriggerGroup tg) {//applies to spherescript-like "events(+e_blah)"
			self.AddTriggerGroup(tg);
		}


		public static void Events(ITriggerGroupHolder self, TGRemover remover) {
			self.RemoveTriggerGroup(remover.TG);
		}

		public static void Events(ITriggerGroupHolder self, int i) {
			if (i == 0) {
				self.ClearTriggerGroups();
			}
		}

		public static string Events(ITriggerGroupHolder self) {
			StringBuilder toreturn = new StringBuilder();
			foreach (TriggerGroup tg in self.GetAllTriggerGroups()) {
				toreturn.Append(tg).Append(", ");

			}
			if (toreturn.Length > 2) {
				toreturn.Length -= 2;
			}
			return toreturn.ToString();
		}
	}
}