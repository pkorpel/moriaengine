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
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	public static class ScriptedCommands {
		[SteamFunction]
		public static void Remove(Player self) {
			self.SysMessage("Zamer objekt pro odstraneni");
			self.Target(SingletonScript<Targ_Remove>.Instance);
		}

		public class Targ_Remove : CompiledTargetDef {
			protected override TargetResult On_TargonThing(Player self, Thing targetted, object parameter) {
				targetted.Delete();
				return TargetResult.Done;
			}
		}

		[SteamFunction]
		public static void Tele(Player self) {
			//teleports to a targetted location
			self.SysMessage("Kam se chces portovat?");
			self.Target(SingletonScript<Targ_Tele>.Instance);
		}

		public class Targ_Tele : CompiledTargetDef {
			protected override TargetResult On_TargonPoint(Player self, IPoint3D targetted, object parameter) {
				self.Go(targetted);
				return TargetResult.Done;
			}
		}

		/// <summary>Toggles the plevel of the account between 1 (player) and the account's max plevel, and writes back a message about the resulting state.</summary>
		/// <remarks>Has no effect on players.</remarks>
		[SteamFunction]
		public static void GM(Character self) {
			AbstractAccount acc = self.Account;
			if (acc != null) {
				if (acc.PLevel < acc.MaxPLevel) {
					acc.PLevel = acc.MaxPLevel;
					Globals.SrcWriteLine(string.Format(
						Loc<GmCommandsLoc>.Get(Globals.SrcLanguage).GMModeOn,
						acc.PLevel));
				} else {
					acc.PLevel = 1;
					Globals.SrcWriteLine(Loc<GmCommandsLoc>.Get(Globals.SrcLanguage).GMModeOff);
				}
			}
		}

		/// <summary>Toggles the Flag_Insubst of a gm, and writes back a message about the resulting state.</summary>
		[SteamFunction]
		public static void Invis(Character self) {
			if (self.Flag_Insubst) {
				self.Flag_Insubst = false;
				Globals.SrcWriteLine(Loc<GmCommandsLoc>.Get(Globals.SrcLanguage).InsubstOff);
			} else {
				self.Flag_Insubst = true;
				Globals.SrcWriteLine(Loc<GmCommandsLoc>.Get(Globals.SrcLanguage).InsubstOn);
			}
		}

		/// <summary>GM-only toggle: while on, nothing can damage this character at all (see
		/// DamageManager.CauseDamage) - a would-be attacker's hit plays a "no effect" animation
		/// instead of landing.</summary>
		[SteamFunction]
		public static void Invul(Character self) {
			if (self.Flag_Invulnerable) {
				self.Flag_Invulnerable = false;
				Globals.SrcWriteLine(Loc<GmCommandsLoc>.Get(Globals.SrcLanguage).InvulOff);
			} else {
				self.Flag_Invulnerable = true;
				Globals.SrcWriteLine(Loc<GmCommandsLoc>.Get(Globals.SrcLanguage).InvulOn);
			}
		}

		/// <summary>Views or changes where purchases draw gold from, live, no restart needed -
		/// see Globals.PaymentSource's own remarks for why this is tag-backed rather than .ini.</summary>
		[SteamFunction]
		public static void PaymentSource(Character self, ScriptArgs sa) {
			object[] argv = sa.Argv;
			if (argv.Length == 0 || argv[0] == null) {
				self.SysMessage("Current payment source: " + Globals.PaymentSource +
					". Usage: PaymentSource <BackpackFirst|BankOnly>");
				return;
			}

			Globals.PaymentSourceMode mode;
			if (Enum.TryParse(Convert.ToString(argv[0]), true, out mode)) {
				Globals.PaymentSource = mode;
				self.SysMessage("Payment source set to " + mode + ".");
			} else {
				self.SysMessage("Unknown payment source '" + argv[0] + "'. Usage: PaymentSource <BackpackFirst|BankOnly>");
			}
		}

		/// <summary>GM-only minimum-viable profession-assignment command (#135) - confirmed via
		/// grep that nothing anywhere ever calls SetProfessionOfChar/sets player.Profession, so
		/// without this the entire enforcement layer (skill caps, spell restriction, and now
		/// weapon/armor restriction, #138) has nothing feeding it. A real player-facing
		/// selection flow (stone/dialog) is a stretch goal per #134, not required here. Accepts
		/// either the profession's Name ("mag") or Defname ("profession_mage"), same
		/// dual-lookup pattern ProfessionDef.LoadScriptLine itself already uses for abilities/
		/// skills.</summary>
		[SteamFunction]
		public static void Profession(Player self, ScriptArgs sa) {
			object[] argv = sa.Argv;
			if (argv.Length == 0 || argv[0] == null) {
				string current = (self.Profession != null) ? self.Profession.Name : "none";
				self.SysMessage("Your current profession: " + current + ". Usage: Profession <name>, then target a character.");
				return;
			}

			string name = Convert.ToString(argv[0]);
			ProfessionDef profession = ProfessionDef.GetByDefname(name) ?? ProfessionDef.GetByName(name);
			if (profession == null) {
				self.SysMessage("Unknown profession '" + name + "'.");
				return;
			}

			self.SysMessage("Target the character to assign profession '" + profession.Name + "' to.");
			self.Target(SingletonScript<Targ_Profession>.Instance, profession);
		}

		public class Targ_Profession : CompiledTargetDef {
			protected override TargetResult On_TargonThing(Player self, Thing targetted, object parameter) {
				Player targetPlayer = targetted as Player;
				if (targetPlayer == null) {
					self.SysMessage("That is not a player character.");
					return TargetResult.Done;
				}

				ProfessionDef profession = (ProfessionDef) parameter;
				targetPlayer.Profession = profession;
				self.SysMessage("Assigned profession '" + profession.Name + "' to " + targetPlayer.Name + ".");
				return TargetResult.Done;
			}
		}

		/// <summary>GM-only instant kill via target cursor (#192, matching Erebor's GM ".kill"):
		/// asks who to kill, then kills the targeted character outright with a lightning visual
		/// effect and sound - reuses Character.InstaKill() (scripts/Basic/CharClasses/
		/// Character.cs), which already had a "//TODO effect?" placeholder for exactly this.
		/// Real finding while building this: naming this command method "Kill" (matching
		/// Character's own pre-existing "Kill" instance method 1:1) made ".kill" silently resolve
		/// to self.Kill() directly - a GM typing ".kill" killed THEMSELVES with no target cursor
		/// and no error of any kind, because GM-plevel commands are dispatched as LScript source
		/// (Commands.InvokeCommand), whose member resolution on self takes priority over the
		/// SteamFunction global-function table. Renamed Character's method to InstaKill to free
		/// up the "Kill" command name rather than picking a different command name.</summary>
		[SteamFunction]
		public static void Kill(Player self) {
			self.SysMessage("Target the character to kill.");
			self.Target(SingletonScript<Targ_Kill>.Instance);
		}

		public class Targ_Kill : CompiledTargetDef {
			protected override TargetResult On_TargonThing(Player self, Thing targetted, object parameter) {
				Character targetChar = targetted as Character;
				if (targetChar == null) {
					self.SysMessage("That is not a character.");
					return TargetResult.Done;
				}

				EffectFactory.LightningEffect(targetChar);
				targetChar.Sound((int) SoundNames.lightningstrike);
				targetChar.InstaKill();
				self.SysMessage("Killed " + targetChar.Name + ".");
				return TargetResult.Done;
			}
		}

		/// <summary>GM ".flip" by target cursor (#193): what Sphere's FLIP does to the target (#938), see Item.Flip.
		/// A door takes ALIGN, HAND or SWING.</summary>
		[SteamFunction]
		public static void Flip(Player self, ScriptArgs sa) {
			self.SysMessage("Target the item to flip.");
			self.Target(SingletonScript<Targ_Flip>.Instance, sa.Args.Trim().Trim('"'));
		}

		public class Targ_Flip : CompiledTargetDef {
			protected override TargetResult On_TargonThing(Player self, Thing targetted, object parameter) {
				AbstractCharacter ch = targetted as AbstractCharacter;
				if (ch != null) {
					ch.Direction = (Direction) (((int) ch.Direction + 1) & (int) Direction.Mask); //clockwise, FLIP ignores its text
					return TargetResult.Done;
				}

				Item item = (Item) targetted;
				int modelBeforeFlip = item.Model;
				byte directionBeforeFlip = item.DirectionByte;
				item.Flip(parameter as string);
				if ((item.Model == modelBeforeFlip) && (item.DirectionByte == directionBeforeFlip)) {
					// #206 - DUPEITEM group is empty/missing for this item, Flip() was a silent
					// no-op. Give the GM a clear signal instead of leaving them guessing.
					self.SysMessage("This item has no flip variants.");
				}
				return TargetResult.Done;
			}
		}

		/// <summary>GM-only diagnostic (#254): reports a targeted NPC's live `AggroMode` (an
		/// `NPCDef`-level property `.info` never surfaces at all, since `.info`'s reflection
		/// only walks the NPC instance's own type, not its Def) plus `Realm` and the notoriety
		/// color the issuing GM would actually see for it - lets a GM directly confirm in-game
		/// whether a spawned monster really got the AggroMode/color its .def content claims,
		/// without needing a code change/rebuild each time this comes up.</summary>
		[SteamFunction]
		public static void CheckAggro(Player self) {
			self.SysMessage("Target the NPC to check.");
			self.Target(SingletonScript<Targ_CheckAggro>.Instance);
		}

		public class Targ_CheckAggro : CompiledTargetDef {
			protected override TargetResult On_TargonThing(Player self, Thing targetted, object parameter) {
				NPC npc = targetted as NPC;
				if (npc == null) {
					self.SysMessage("That is not an NPC.");
					return TargetResult.Done;
				}

				HighlightColor color = Notoriety.GetHighlightColor(self, npc);
				self.SysMessage(npc.Name + "#" + npc.Uid + ": AggroMode=" + npc.AggroMode +
					" (species default=" + ((NPCDef) npc.Def).AggroMode + ")" +
					" Realm=" + npc.Realm +
					" ExperienceReward=" + npc.ExperienceReward +
					" WildMonstersRenderOrange=" + NotorietySettings.instance.wildMonstersRenderOrange +
					" HighlightColorAsSeenByYou=" + color);
				return TargetResult.Done;
			}
		}

		/// <summary>GM-only manual world save (#258): synchronously calls `Globals.Save()`, the
		/// same mechanism shutdown/recompile already use internally - there was previously no
		/// way to force a save on demand (world state only otherwise persists on shutdown or
		/// script recompile, confirmed via grep - no periodic auto-save exists either). No
		/// target needed. Deliberately unlisted in `CommandsAuthorization.cs`, so it's GM-only
		/// (plevel >= 4) by the existing default-deny fallback - same precedent as .kill/.flip/
		/// .profession, and matches the user's explicit "plevel 4 and above only" request.</summary>
		[SteamFunction]
		public static void Save(Player self) {
			self.SysMessage("Saving world...");
			if (Globals.TrySave()) {
				self.SysMessage("Save complete.");
			} else {
				self.RedMessage("The save failed, see the server log."); //#1173
			}
		}

		/// <summary>#1173 - shuts the server down, saving the world first (a failed save only warns). A GM's .exit used to
		/// read as a bare word and do nothing: commands resolve on the GM, and only the console's resolve on Globals.
		/// Admins only (CommandsAuthorization), as the setups.</summary>
		[SteamFunction]
		public static void Exit(Player self) {
			self.SysMessage("Saving the world and shutting down...");
			Globals.Exit();
		}

		/// <summary>#1173 - shuts the server down WITHOUT saving: everything since the last save is lost.</summary>
		[SteamFunction]
		public static void ExitNoSave(Player self) {
			self.SysMessage("Shutting down without saving...");
			Globals.ExitNoSave();
		}

		//public static void DragonTileTargon(GameConn c, IPoint3D point, object targData) {
		//    AbstractCharacter cre = c.CurCharacter;
		//    Map map = cre.GetMap();
		//    ushort[,] tiles = new ushort[3, 3];
		//    //Go through each tile in the 3x3 area including this one and surrounding tiles, and record them
		//    for (int xt=point.X-1, a=0; xt<point.X+2; xt++, a++) {
		//        for (int yt=point.Y-1, b=0; yt<point.Y+2; yt++, b++) {
		//            tiles[a, b]=map.GetTileId((ushort) xt, (ushort) yt);
		//        }
		//    }

		//    //UO averages the z values for that and the surrounding tiles, and sends the average,
		//    //when we click a map tile itself... So we check the map data for the actual z.
		//    int z = map.GetTileZ(point.X, point.Y);

		//    //record the tileID of tile A, B will be set when we see one (If we see one).
		//    MapTileType tileTypeA = map.GetMapTileType(tiles[1, 1]);

		//    MapTileType tileTypeB = (MapTileType) 0xff;
		//    cre.Message("(That's a '"+tileTypeA+"' at "+point.X+","+point.Y+","+z+").");

		//    //s is the string we'll return.
		//    string s = "";

		//    //We start at (0,0), the NW corner.
		//    int xp=0;
		//    int yp=0;
		//    //We continue until cont is false, and dir determines which direction we're travelling in - we
		//    //change direction 3 times, since we are proceeding clockwise in a 3x3 square (never touching the middle square).
		//    //(That's how dragon wants the ABABABAB stuff in the betweentrans script files (including statbetweentrans).
		//    bool cont=true;
		//    int dir=0;
		//    while (cont) {
		//        MapTileType curTileType = map.GetMapTileType(tiles[xp, yp]);
		//        if (curTileType==tileTypeA) {
		//            s+="A";
		//        } else if (tileTypeB==((MapTileType) 0xff) || curTileType==tileTypeB) {
		//            tileTypeB=curTileType;
		//            s+="B";
		//        } else {
		//            s+="C";	//Dragon doesn't support more than two tileTypes (which is annoying when you have corners), but we write C if we find one. Maybe it'll support them someday.
		//        }
		//        //0,0 is the northwest tile, 1,1 is the middle, and 2,2 is the southeast tile.
		//        switch (dir) {
		//            case 0:	//travelling east, on the north side of the square
		//                xp++;
		//                if (xp==2) dir=1;
		//                break;
		//            case 1:	//travelling south, on the east side of the square
		//                yp++;
		//                if (yp==2) dir=2;
		//                break;
		//            case 2:	//travelling west, on the south side of the square
		//                xp--;
		//                if (xp==0) dir=3;
		//                break;
		//            case 3:	//travelling north, on the west side of the square.
		//                yp--;
		//                if (yp==0) cont=false;
		//                break;
		//        }
		//    }

		//    cre.SysMessage("OK, if that tile type ('"+tileTypeA+"') is tile A, and type B is '"+tileTypeB+"', then for betweentrans, it would be: "+s+" : (NW is first, then proceed clockwise) If you see C in there then we found a third tile type, or if B said it was 255, then we only found one.");
		//}

		//public void def_self(AbstractCharacter self) {
		//	Thing thing = (self as Thing);
		//	((GameConn)Globals.srcConn).HandleTarget(0, (int)thing.FlaggedUid, thing.X, thing.Y, thing.Z, 0);
		//	Packets.Prepared.SendCancelTargettingCursor((GameConn)Globals.srcConn);
		//}

		//public void def_dragonTile(TagHolder self) {
		//	((Character)self).SysMessage("What map tile do you want info about for dragon?");
		//	((GameConn) Globals.srcConn).Target(true, null, dragonTileTargon, null);
		//}

		//[SteamFunction]
		//public static void OldInfo(TagHolder self) {
		//    Character ch = self as Character;
		//    if (self==Globals.Src) {
		//        ch.SysMessage("Show info on what item, character, static, or map tile?");
		//        ch.Conn.Target(true, infoTargon, null, null);
		//    } else {
		//        //They typed xinfo instead of info.
		//        if (self is Character) {
		//            InfoTargon(Globals.SrcGameConn, self as AbstractCharacter, null);
		//        } else if (self is AbstractItem) {
		//            InfoTargon(Globals.SrcGameConn, self as AbstractItem, null);
		//            //Statics aren't TagHolders, so this can't happen, unfortunately. But maybe that'll change eventually:
		//            //} else if (self is Static) {
		//            //	Static sta = self as Static;
		//            //	InfoTargonStatic((GameConn)Globals.srcConn, sta.id, sta.X, sta.Y, sta.Z, null);
		//        }
		//    }
		//}

		//TODO: Show a dialog, once dialogs exist.
		//public static void InfoTargon(GameConn c, IPoint3D getback, object targData) {
		//    if (getback is AbstractItem) {
		//        Item itm = getback as Item;
		//        itm.Message("I am an item named '"+itm.Name+"'. My model # is 0x"+itm.Model.ToString("x")+", and my itemdef is "+itm.Def+", and I have a height of "+itm.Height+", and a weight of "+itm.Weight+".");
		//    } else if (getback is Character) {
		//        Character cre = getback as Character;
		//        cre.Message("I am a character named '"+cre.Name+"' at "+cre.X+","+cre.Y+","+cre.Z+" on mapplane "+cre.M+". My model # is 0x"+cre.Model.ToString("x")+", and my chardef is "+cre.Def+", and I have a height of "+cre.Height+", and a weight of "+cre.Weight+".");
		//        string ownride = "";
		//        if (cre.Owner!=null) {
		//            ownride="My owner is "+cre.Owner;
		//            if (cre.Rider!=null) {
		//                ownride+=", and I am being ridden by "+cre.Rider+".";
		//            } else if (cre.Mount!=null) {
		//                ownride+=", and I am riding "+cre.Mount+".";
		//            } else {
		//                ownride+=", but I am not being ridden right now.";
		//            }
		//        } else if (cre.IsPlayer) {
		//            ownride="I am a player, my account name is "+cre.Account.Name;
		//            if (cre.Rider!=null) {
		//                ownride+=", and I am (WTF?) being ridden by "+cre.Rider+".";
		//            } else if (cre.Mount!=null) {
		//                ownride+=", and I am riding "+cre.Mount+".";
		//            } else {
		//                ownride+=", but I am not riding anything right now.";
		//            }
		//        } else {
		//            bool freenpc = Globals.dice.NextDouble()<0.05;
		//            if (freenpc) {
		//                ownride="I am a free NPC";
		//            } else {
		//                ownride="I am an NPC with no owner";
		//            }
		//            if (cre.Rider!=null) {
		//                ownride+=", but nonetheless I am being ridden by "+cre.Rider+".";
		//            } else if (cre.Mount!=null) {
		//                ownride+=", and I am riding "+cre.Mount+".";
		//            } else {
		//                if (freenpc) {
		//                    ownride+="! Dratted PCs, always buying all the animals and going off an getting them etten by ettins...";
		//                } else {
		//                    ownride=".";
		//                }
		//            }
		//        }
		//        cre.Message(ownride);
		//    } else if (getback is Static) {
		//        AbstractCharacter cre = c.CurCharacter;
		//        Map map = cre.GetMap();
		//        Static sta = getback as Static;
		//        if (sta!=null) {
		//            sta.OverheadMessage("I am a static named '"+sta.Name+"' at "+sta.X+","+sta.Y+","+sta.Z+" on mapplane "+sta.M+". I have a staticID of 0x"+sta.Id.ToString("x")+", and a height of "+sta.Height+".");
		//        } else {
		//            cre.Message("Your client said that there was a static there (staticID 0x"+sta.Id.ToString("x")+", at "+sta.X+","+sta.Y+","+sta.Z+"), but I don't see that static there.");
		//        }
		//        cre.Message("That map tile ("+getback.X+","+getback.Y+") has a tile ID of "+map.GetTileId(getback.X, getback.Y)+" and is at "+map.GetTileZ(getback.X, getback.Y)+" z. The map tile type is "+map.GetMapTileType(getback.X, getback.Y)+".");
		//    } else {
		//        AbstractCharacter cre = c.CurCharacter;
		//        Map map = cre.GetMap();
		//        cre.Message("That map tile ("+getback.X+","+getback.Y+") has a tile ID of "+map.GetTileId(getback.X, getback.Y)+" and is at "+map.GetTileZ(getback.X, getback.Y)+" z. The map tile type is "+map.GetMapTileType(getback.X, getback.Y)+".");
		//    }
		//}

		//public void def_setskill(TagHolder self, ScriptArgs argo) {
		//	if ( argo.Argv.Length > 0 )
		//	{
		//		args=argo.Argv;
		//		//SkillName skill;
		//		try {
		//			Enum.Parse( typeof( SkillName ), argo.Argv[0].ToString(), true );
		//		} catch {
		//			((AbstractCharacter)self).SysMessage( 1005631, 0x22, "" );
		//			return;
		//		}
		//		((AbstractCharacter)self).SysMessage( "Wem wollt ihr den Skill "+argo.Argv[0]+" auf "+argo.Argv[1]+" erhoehen?" );
		//		((GameConn) Globals.srcConn).Target(false, null, skillsetTargon, null);
		//	}
		//}
		//
		//public void SkillSet(GameConn c, IPoint3D getback, object targData) {
		//	if ( getback is Character ) {
		//		Character cre = (Character)getback;
		//		ushort newvalue = ushort.Parse( args[1].ToString() );
		//		
		//		SkillName skillname;
		//		skillname = (SkillName)Enum.Parse( typeof( SkillName ), this.args[0].ToString(), true );
		//		Skill skill = (Skill)cre.Skills[(int)skillname];
		//		
		//		if ( skill == null )
		//			return;
		//		else if ( skill != null )
		//			skill.RealValue = newvalue;
		//		
		//		cre.SysMessage( args[0]+": Value="+newvalue );
		//	}
		//}
	}

	public class GmCommandsLoc : CompiledLocStringCollection<GmCommandsLoc> {
		public string GMModeOn = "GM mode on (Plevel {0}).";
		public string GMModeOff = "GM mode off (Plevel 1).";
		public string InsubstOn = "Flag_Insubst on (you're invisible and players can not interact with you)";
		public string InsubstOff = "Flag_Insubst off (you're visible)";
		public string InvulOn = "Flag_Invulnerable on (nothing can damage you)";
		public string InvulOff = "Flag_Invulnerable off (you can be damaged normally)";
	}
}