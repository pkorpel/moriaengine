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
	}
}