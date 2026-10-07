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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;
using SteamEngine.Regions;

namespace SteamEngine.Networking {
	[SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
	public sealed class CharSyncQueue : SyncQueue {
		internal static CharSyncQueue instance = new CharSyncQueue();

		private SimpleQueue<CharState> queue = new SimpleQueue<CharState>();

		private CharSyncQueue() {
		}

		protected override void ProcessQueue() {
			while (this.queue.Count > 0) {
				CharState ch = this.queue.Dequeue();
				if (ch.thing != null) {
					ch.ProcessChar();
				}
				ch.Dispose();
			}
		}

		public static void ProcessChar(AbstractCharacter ch) {
			if (ch.syncState != null) {
				ch.syncState.ProcessChar();
				ch.syncState.thing = null;
				ch.syncState = null;
			}
		}

		/// <summary>Call when a thing is being created</summary>
		public static void Resend(AbstractCharacter thing) {
			if (IsEnabled) {
				Logger.WriteInfo(Globals.NetSyncingTracingOn, "Resend(" + thing + ") called");
				instance.PopAndEnqueueInstance(thing).changeFlags |= CharSyncFlags.Resend;
			}
		}

		public static void PropertiesChanged(AbstractCharacter thing) {
			if (IsEnabled) {
				Logger.WriteInfo(Globals.NetSyncingTracingOn, "AboutToChangeProperty(" + thing + ") called");
				instance.PopAndEnqueueInstance(thing).changeFlags |= CharSyncFlags.Property;
			}
		}

		public static void AboutToChangeSkill(AbstractCharacter thing, int skillId) {
			if (IsEnabled) {
				Logger.WriteInfo(Globals.NetSyncingTracingOn, "AboutToChangeSkill(" + thing + ", " + skillId + ") called");
				instance.PopAndEnqueueInstance(thing).AboutToChangeSkill(skillId);
			}
		}

		/// <summary>Call when name is about to be changed</summary>
		public static void AboutToChangeName(AbstractCharacter thing) {
			if (IsEnabled) {
				Logger.WriteInfo(Globals.NetSyncingTracingOn, "AboutToChangeName(" + thing + ") called");
				instance.PopAndEnqueueInstance(thing).AboutToChangeName();
			}
		}

		/// <summary>Call when base properties (model/color) are about to be changed</summary>
		public static void AboutToChangeBaseProps(AbstractCharacter thing) {
			if (IsEnabled) {
				Logger.WriteInfo(Globals.NetSyncingTracingOn, "AboutToChangeBaseProps(" + thing + ") called");
				instance.PopAndEnqueueInstance(thing).AboutToChangeBaseProps();
			}
		}

		/// <summary>Call when direction is about to be changed</summary>
		public static void AboutToChangeDirection(AbstractCharacter thing, bool requested) {
			if (IsEnabled) {
				Logger.WriteInfo(Globals.NetSyncingTracingOn, "AboutToChangeDirection(" + thing + ") called");
				instance.PopAndEnqueueInstance(thing).AboutToChangeDirection(requested);
			}
		}

		/// <summary>Call when Flags are about to be changed</summary>
		public static void AboutToChangeFlags(AbstractCharacter thing) {
			if (IsEnabled) {
				Logger.WriteInfo(Globals.NetSyncingTracingOn, "AboutToChangeFlags(" + thing + ") called");
				instance.PopAndEnqueueInstance(thing).AboutToChangeFlags();
			}
		}

		/// <summary>Call when visibility is about to be changed</summary>
		public static void AboutToChangeVisibility(AbstractCharacter ch) {
			if (IsEnabled) {
				instance.PopAndEnqueueInstance(ch).AboutToChangeVisibility();
			}
		}

		/// <summary>Call when position is about to be changed</summary>
		public static void AboutToChangePosition(AbstractCharacter thing, MovementType movType) {
			if (IsEnabled) {
				int movTypeInt = (int) movType;
				Sanity.IfTrueThrow((movTypeInt < 1 || movTypeInt > 8), "Incorrect MovementType.");
				Logger.WriteInfo(Globals.NetSyncingTracingOn, "AboutToChangePosition(" + thing + ", " + movType + ") called");
				instance.PopAndEnqueueInstance(thing).AboutToChangePosition(movTypeInt);
			}
		}

		/// <summary>Call when mount is about to be changed</summary>
		public static void AboutToChangeMount(AbstractCharacter thing) {
			if (IsEnabled) {
				Logger.WriteInfo(Globals.NetSyncingTracingOn, "AboutToChangeMount(" + thing + ") called");
				instance.PopAndEnqueueInstance(thing).AboutToChangeMount();
			}
		}

		/// <summary>Call when highlight (notoriety color) is about to be changed</summary>
		public static void AboutToChangeHighlight(AbstractCharacter thing) {
			if (IsEnabled) {
				Logger.WriteInfo(Globals.NetSyncingTracingOn, "AboutToChangeHighlight(" + thing + ") called");
				instance.PopAndEnqueueInstance(thing).changeFlags |= CharSyncFlags.Highlight;
			}
		}

		/// <summary>Call when hitpoints are about to be changed</summary>
		public static void AboutToChangeHitpoints(AbstractCharacter thing) {
			if (IsEnabled) {
				Logger.WriteInfo(Globals.NetSyncingTracingOn, "AboutToChangeHitpoints(" + thing + ") called");
				instance.PopAndEnqueueInstance(thing).AboutToChangeHitpoints();
			}
		}
		/// <summary>Call when mana is about to be changed</summary>
		public static void AboutToChangeMana(AbstractCharacter thing) {
			if (IsEnabled) {
				Logger.WriteInfo(Globals.NetSyncingTracingOn, "AboutToChangeMana(" + thing + ") called");
				instance.PopAndEnqueueInstance(thing).AboutToChangeMana();
			}
		}

		/// <summary>Call when stamina is about to be changed</summary>
		public static void AboutToChangeStamina(AbstractCharacter thing) {
			if (IsEnabled) {
				Logger.WriteInfo(Globals.NetSyncingTracingOn, "AboutToChangeStamina(" + thing + ") called");
				instance.PopAndEnqueueInstance(thing).AboutToChangeStamina();
			}
		}

		/// <summary>
		/// Call when stats are about to be changed
		/// </summary>
		/// <param name="thing">The thing.</param>
		/// <remarks>
		/// The stats are following: Strength, Dexterity, Intelligence, Gender, Gold,
		/// PhysicalResist (armor), Weight, FireResist, ColdResist, PoisonResist,
		/// EnergyResist, Luck, MinDamage, MaxDamage and TithingPoints
		/// </remarks>
		public static void AboutToChangeStats(AbstractCharacter thing) {
			if (IsEnabled) {
				Logger.WriteInfo(Globals.NetSyncingTracingOn, "AboutToChangeStats(" + thing + ") called");
				instance.PopAndEnqueueInstance(thing).AboutToChangeStats();
			}
		}

		[SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
		internal class CharState : Poolable {
			internal AbstractCharacter thing;
			internal CharSyncFlags changeFlags;

			private Point4D point;
			private string name;
			private byte flagsToSend;
			//private ushort flags;
			//private uint flaggedUid;
			private AbstractCharacter mount;
			private int mountUid;

			private int model;
			private int color;
			private Direction direction;

			private short hitpoints;
			private short maxHitpoints;
			private short stamina;
			private short maxStamina;
			private short mana;
			private short maxMana;
			private short str;
			private short intel;
			private short dex;
			private bool isFemale;
			private long gold;

			private short armorClass;
			private short stat1;
			private short stat2;
			private short stat3;
			private short mindDefense;
			private float weight;
			private short stat4;
			private short stat5;
			private short stat6;
			private long tithingPoints;

			private int[] changedSkills = new int[AbstractSkillDef.SkillsCount];
			private int changedSkillsCount;

			protected override void On_Reset() {
				base.On_Reset();

				this.changeFlags = CharSyncFlags.None;
				this.changedSkillsCount = 0;
			}

			private bool IsNewAndPositiveBit(CharSyncFlags flagBeingSet) {
				if ((this.changeFlags & flagBeingSet) != flagBeingSet) {
					this.changeFlags |= flagBeingSet;
					return true;
				}
				return false;
			}

			protected override void On_DisposeManagedResources() {
				if (this.thing != null) {
					lock (MainClass.globalLock) {
						this.thing.syncState = null;
						this.thing = null;
					}
				}
				base.On_DisposeManagedResources();
			}

			internal void AboutToChangeSkill(int skillId) {
				for (int i = 0; i < this.changedSkillsCount; i++) {
					if (this.changedSkills[i] == skillId) {
						return;//we know about this change already
					}
				}
				this.changedSkills[this.changedSkillsCount] = skillId;
				this.changedSkillsCount++;
			}

			internal void AboutToChangeName() {
				if (this.IsNewAndPositiveBit(CharSyncFlags.Name)) {
					this.name = this.thing.Name;
				}
			}
			private bool GetNameChanged() {
				bool retVal = (((this.changeFlags & CharSyncFlags.Name) == CharSyncFlags.Name) && (!string.Equals(this.name, this.thing.Name, StringComparison.Ordinal)));
				Logger.WriteInfo(Globals.NetSyncingTracingOn && retVal, "NameChanged: true");
				return retVal;
			}

			internal void AboutToChangeBaseProps() {
				if (this.IsNewAndPositiveBit(CharSyncFlags.BaseProps)) {
					this.model = this.thing.Model;
					this.color = this.thing.Color;
				}
			}

			private bool GetBasePropsChanged() {
				bool retVal = (((this.changeFlags & CharSyncFlags.BaseProps) == CharSyncFlags.BaseProps)
					&& ((this.model != this.thing.Model) || (this.color != this.thing.Color)));
				Logger.WriteInfo(Globals.NetSyncingTracingOn && retVal, "GetBasePropsChanged (model/color): " + retVal);
				return retVal;
			}

			private bool GetDirectionChanged() {
				bool retVal = (((this.changeFlags & CharSyncFlags.Direction) == CharSyncFlags.Direction)
					&& (this.direction != this.thing.Direction));
				Logger.WriteInfo(Globals.NetSyncingTracingOn && retVal, "GetDirectionChanged: " + retVal);
				return retVal;
			}

			internal void AboutToChangeFlags() {
				if (this.IsNewAndPositiveBit(CharSyncFlags.Flags)) {
					this.flagsToSend = this.thing.FlagsToSend;
				}
			}

			private bool GetChangedFlags(out bool invisChanges, out bool warModeChanges) {
				invisChanges = false;
				warModeChanges = false;
				bool retVal = false;
				if ((this.changeFlags & CharSyncFlags.Flags) == CharSyncFlags.Flags) {
					byte newFlagsToSend = this.thing.FlagsToSend;

					if ((this.flagsToSend & 0x40) != (newFlagsToSend & 0x40)) {
						warModeChanges = true;
					}

					retVal = ((this.flagsToSend & ~0x40) != (newFlagsToSend & ~0x40));

					if ((this.changeFlags & CharSyncFlags.Visibility) == CharSyncFlags.Visibility) {
						invisChanges = true;
					}
					if (!invisChanges) {
						invisChanges = ((this.flagsToSend & 0x80) != (newFlagsToSend & 0x80));
					}
				}

				Logger.WriteInfo(Globals.NetSyncingTracingOn && retVal && retVal, "GetFlagsChanged: " + retVal + ", invisChanges " + invisChanges);
				return retVal;
			}

			internal void AboutToChangeVisibility() {
				this.changeFlags |= CharSyncFlags.Visibility;
				if (this.IsNewAndPositiveBit(CharSyncFlags.Flags)) {
					this.flagsToSend = this.thing.FlagsToSend;
				}
			}

			internal void AboutToChangePosition(int movType) {
				if (this.IsNewAndPositiveBit(CharSyncFlags.Position)) {
					this.point = new Point4D(this.thing);
				}
				this.changeFlags |= (CharSyncFlags) movType; //a dirty change of the enum type....
			}

			private bool GetPositionChanged(out bool teleported, out bool running, out bool requestedStep) {
				teleported = false;
				running = false;
				requestedStep = false;
				bool retVal = false;
				if (((this.changeFlags & CharSyncFlags.Position) == CharSyncFlags.Position) && (!Point4D.Equals(this.point, this.thing))) {
					retVal = true;
					if ((this.changeFlags & CharSyncFlags.Running) == CharSyncFlags.Running) {
						running = true;
					}
					if ((this.changeFlags & CharSyncFlags.Teleport) == CharSyncFlags.Teleport) {
						teleported = true;
					}
				}
				if ((this.changeFlags & CharSyncFlags.RequestedStep) == CharSyncFlags.RequestedStep) {
					requestedStep = true;
				}
				Logger.WriteInfo(Globals.NetSyncingTracingOn && retVal, "GetPositionChanged: " + retVal + ", teleported:"
					+ teleported + ", running:" + running + ", requestedStep:" + requestedStep);
				return retVal;
			}

			internal void AboutToChangeMount() {
				if (this.IsNewAndPositiveBit(CharSyncFlags.Mount)) {
					this.mount = this.thing.Mount;
					if (this.mount != null) {
						this.mountUid = this.mount.Uid;
					}
				}
			}

			private bool GetMountChanged() {
				bool retVal = (((this.changeFlags & CharSyncFlags.Mount) == CharSyncFlags.Mount) && (this.mount != this.thing.Mount));
				Logger.WriteInfo(Globals.NetSyncingTracingOn && retVal, "GetMountChanged: " + retVal);
				return retVal;
			}

			private bool GetHighlightChanged() {
				bool retVal = ((this.changeFlags & CharSyncFlags.Highlight) == CharSyncFlags.Highlight);
				Logger.WriteInfo(Globals.NetSyncingTracingOn && retVal, "GetHighlightChanged: " + retVal);
				return retVal;
			}

			internal void AboutToChangeHitpoints() {
				if (this.IsNewAndPositiveBit(CharSyncFlags.Hits)) {
					this.hitpoints = this.thing.Hits;
					this.maxHitpoints = this.thing.MaxHits;
				}
			}

			private bool GetHitpointsChanged() {
				bool retVal = (((this.changeFlags & CharSyncFlags.Hits) == CharSyncFlags.Hits)
					&& ((this.hitpoints != this.thing.Hits) || (this.maxHitpoints != this.thing.MaxHits)));
				Logger.WriteInfo(Globals.NetSyncingTracingOn && retVal, "GetHitpointsChanged: " + retVal);
				return retVal;
			}

			internal void AboutToChangeMana() {
				if (this.IsNewAndPositiveBit(CharSyncFlags.Mana)) {
					this.mana = this.thing.Mana;
					this.maxMana = this.thing.MaxMana;
				}
			}

			private bool GetManaChanged() {
				bool retVal = (((this.changeFlags & CharSyncFlags.Mana) == CharSyncFlags.Mana)
					&& ((this.mana != this.thing.Mana) || (this.maxMana != this.thing.MaxMana)));
				Logger.WriteInfo(Globals.NetSyncingTracingOn && retVal, "GetManaChanged: " + retVal);
				return retVal;
			}

			internal void AboutToChangeStamina() {
				if (this.IsNewAndPositiveBit(CharSyncFlags.Stam)) {
					this.stamina = this.thing.Stam;
					this.maxStamina = this.thing.MaxStam;
				}
			}
			private bool GetStaminaChanged() {
				bool retVal = (((this.changeFlags & CharSyncFlags.Stam) == CharSyncFlags.Stam)
					&& ((this.stamina != this.thing.Stam) || (this.maxStamina != this.thing.MaxStam)));
				Logger.WriteInfo(Globals.NetSyncingTracingOn && retVal, "GetStaminaChanged: " + retVal);
				return retVal;
			}

			internal void AboutToChangeStats() {
				if (this.IsNewAndPositiveBit(CharSyncFlags.Stats)) {
					this.str = this.thing.Str;
					this.dex = this.thing.Dex;
					this.intel = this.thing.Int;
					this.isFemale = this.thing.IsFemale;
					this.gold = this.thing.Gold;
					this.armorClass = this.thing.StatusArmorClass;
					this.weight = this.thing.Weight;
					this.stat1 = this.thing.ExtendedStatusNum01;
					this.stat2 = this.thing.ExtendedStatusNum02;
					this.stat3 = this.thing.ExtendedStatusNum03;
					this.mindDefense = this.thing.StatusMindDefense;
					this.stat4 = this.thing.ExtendedStatusNum04;
					this.stat5 = this.thing.ExtendedStatusNum05;
					this.stat6 = this.thing.ExtendedStatusNum06;
					this.tithingPoints = this.thing.TithingPoints;
				}
			}

			private bool GetStatsChanged() {
				bool retVal = (((this.changeFlags & CharSyncFlags.Stats) == CharSyncFlags.Stats)
					&& ((this.str != this.thing.Str) || (this.dex != this.thing.Dex) || (this.intel != this.thing.Int) ||
					(this.isFemale != this.thing.IsFemale) || (this.gold != this.thing.Gold) ||
					(this.armorClass != this.thing.StatusArmorClass) ||
					(this.weight != this.thing.Weight) || (this.stat1 != this.thing.ExtendedStatusNum01) ||
					(this.stat2 != this.thing.ExtendedStatusNum02) || (this.stat3 != this.thing.ExtendedStatusNum03) ||
					(this.mindDefense != this.thing.StatusMindDefense) || (this.stat4 != this.thing.ExtendedStatusNum04) ||
					(this.stat5 != this.thing.ExtendedStatusNum05) || (this.stat6 != this.thing.ExtendedStatusNum06) ||
					(this.tithingPoints != this.thing.TithingPoints)));

				Logger.WriteInfo(Globals.NetSyncingTracingOn && retVal, "GetStatsChanged: " + retVal);
				return retVal;
			}

			internal void AboutToChangeDirection(bool requested) {
				if (this.IsNewAndPositiveBit(CharSyncFlags.Direction)) {
					this.direction = this.thing.Direction;
					if (requested) {
						this.changeFlags |= CharSyncFlags.RequestedStep;
					} else {
						this.changeFlags = this.changeFlags & ~CharSyncFlags.RequestedStep;
					}
				}
			}

			internal void ProcessChar() {
				if (!this.thing.IsDeleted) {//deleted items are supposed to be removedfromview by the delete code
					if ((this.changeFlags != CharSyncFlags.None) || (this.changedSkillsCount > 0)) {
						if ((this.changeFlags & CharSyncFlags.Resend) == CharSyncFlags.Resend) {
							ProcessCharResend(this.thing);
						} else {
							this.ProcessCharUpdate(this.thing);
						}
					}
				}
			}

			private static PacketGroup[] charInfoPackets = new PacketGroup[Tools.GetEnumLength<HighlightColor>()]; //0x78

			private static void ProcessCharResend(AbstractCharacter ch) {
				Logger.WriteInfo(Globals.NetSyncingTracingOn, "ProcessCharResend " + ch);

				GameState state = ch.GameState;
				if (state != null) {
					DrawGamePlayerOutPacket packet = Pool<DrawGamePlayerOutPacket>.Acquire();
					packet.Prepare(state, ch);
					state.Conn.SendSinglePacket(packet);
				}

				bool propertiesExist = true;
				AosToolTips[] toolTipsArray = null;

				foreach (AbstractCharacter viewer in ch.GetMap().GetPlayersInRange(ch.X, ch.Y, Globals.MaxUpdateRange)) {
					if (viewer != ch) {
						GameState viewerState = viewer.GameState;
						if (viewerState != null) {
							if (viewer.CanSeeForUpdate(ch).Allow) {
								HighlightColor highlightColor = ch.GetHighlightColorFor(viewer);
								int highlight = (int) highlightColor;
								PacketGroup pg = charInfoPackets[highlight];
								if (pg == null) {
									pg = PacketGroup.AcquireMultiUsePG();
									pg.AcquirePacket<DrawObjectOutPacket>().Prepare(ch, highlightColor); //0x78
									charInfoPackets[highlight] = pg;
								}
								TcpConnection<GameState> viewerConn = viewerState.Conn;
								viewerConn.SendPacketGroup(pg);
								if (Globals.UseAosToolTips && viewerState.Version.AosToolTips) {
									if (propertiesExist) {
										propertiesExist = ProcessCharProperties(ch, ref toolTipsArray, viewerState, viewerConn);
									}
								}
							}
						}
					}
				}

				for (int i = 0, n = charInfoPackets.Length; i < n; i++) {
					PacketGroup pg = charInfoPackets[i];
					if (pg != null) {
						pg.Dispose();
						charInfoPackets[i] = null;
					}
				}
			}

			private static bool ProcessCharProperties(AbstractCharacter target, ref AosToolTips[] toolTipsArray, GameState viewerState, TcpConnection<GameState> viewerConn) {
				if (toolTipsArray == null) {
					toolTipsArray = new AosToolTips[Tools.GetEnumLength<Language>()];
				}
				Language language = viewerState.Language;
				AosToolTips toolTips = toolTipsArray[(int) language];
				if (toolTips == null) {
					toolTips = target.GetAosToolTips(language);
					toolTipsArray[(int) language] = toolTips;
					if (toolTips != null) {
						toolTips.SendIdPacket(viewerState, viewerConn);
						return true;
					}
				} else {
					toolTips.SendIdPacket(viewerState, viewerConn);
					return true;
				}
				return false;
			}

			private static PacketGroup[] myCharInfos = new PacketGroup[Tools.GetEnumLength<HighlightColor>()];
			private static PacketGroup[] myMovings = new PacketGroup[Tools.GetEnumLength<HighlightColor>()];

			[SuppressMessage("Microsoft.Performance", "CA1809:AvoidExcessiveLocals"), SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
			private void ProcessCharUpdate(AbstractCharacter ch) {
				//TODO: party update
				//triggers - @seenewplayer and stuff?

				Logger.WriteInfo(Globals.NetSyncingTracingOn, "ProcessCharUpdate " + ch);
				bool invisChanged, warModeChanges;
				bool flagsChanged = this.GetChangedFlags(out invisChanged, out warModeChanges);
				bool highlightChanged = this.GetHighlightChanged();
				bool hitsChanged = this.GetHitpointsChanged();
				bool nameChanged = this.GetNameChanged();
				bool mountChanged = this.GetMountChanged();
				bool teleported, running, requestedStep;
				bool posChanged = this.GetPositionChanged(out teleported, out running, out requestedStep);
				bool directionChanged = this.GetDirectionChanged();
				bool basePropsChanged = this.GetBasePropsChanged();
				bool propertiesChanged = (this.changeFlags & CharSyncFlags.Property) == CharSyncFlags.Property;
				bool propertiesExist = propertiesChanged;
				AosToolTips[] toolTipsArray = null;
				ICollection<AbstractCharacter> partyMembers = ch.PartyMembers;
				bool hasParty = (partyMembers != null && partyMembers.Count > 1);

				Map chMap = ch.GetMap();
				int chX = ch.X;
				int chY = ch.Y;

				PacketGroup pgRemoveMount = null, pgUpdateMount = null;

				{
					GameState myState = ch.GameState;
					if (myState != null) {
						TcpConnection<GameState> myConn = myState.Conn;
						this.UpdateSkills(myState, myConn);

						if (propertiesChanged) {
							if (Globals.UseAosToolTips && myState.Version.AosToolTips) {
								propertiesExist = ProcessCharProperties(ch, ref toolTipsArray, myState, myConn);
							}
						}
						if (this.GetStatsChanged() || nameChanged) {
							Logger.WriteInfo(Globals.NetSyncingTracingOn, "Sending StatusBar to self");
							StatusBarInfoOutPacket sbiop = Pool<StatusBarInfoOutPacket>.Acquire();
							sbiop.Prepare(ch, StatusBarType.Me); //0x11
							myConn.SendSinglePacket(sbiop);
						} else {
							bool manaChanged = this.GetManaChanged();
							bool staminaChanged = this.GetStaminaChanged();
							if (hitsChanged || manaChanged || staminaChanged) {
								if (hitsChanged && manaChanged && staminaChanged) {//all 3 stats
									Logger.WriteInfo(Globals.NetSyncingTracingOn, "Sending Stats to self");
									PacketGroup statsPG = PacketGroup.AcquireMultiUsePG();
									statsPG.AcquirePacket<MobAttributesOutPacket>().Prepare(ch, true); //0x2d
									myConn.SendPacketGroup(statsPG);
									if (hasParty) {
										SendPGToPartyMembers(ch, partyMembers, statsPG);
									}
									statsPG.Dispose();
								} else {
									if (manaChanged) {
										Logger.WriteInfo(Globals.NetSyncingTracingOn, "Sending Mana to self");
										PacketGroup manaPG = PacketGroup.AcquireMultiUsePG();
										manaPG.AcquirePacket<UpdateCurrentManaOutPacket>().Prepare(ch.FlaggedUid, ch.Mana, ch.MaxMana, true); //0xa2
										myConn.SendPacketGroup(manaPG);
										if (hasParty) {
											SendPGToPartyMembers(ch, partyMembers, manaPG);
										}
										manaPG.Dispose();
									}
									if (staminaChanged) {
										Logger.WriteInfo(Globals.NetSyncingTracingOn, "Sending Stamina to self");
										PacketGroup stamPG = PacketGroup.AcquireMultiUsePG();
										stamPG.AcquirePacket<UpdateCurrentStaminaOutPacket>().Prepare(ch.FlaggedUid, ch.Stam, ch.MaxStam, true); //0xa3
										myConn.SendPacketGroup(stamPG);
										if (hasParty) {
											SendPGToPartyMembers(ch, partyMembers, stamPG);
										}
										stamPG.Dispose();
									}
									if (hitsChanged) {
										Logger.WriteInfo(Globals.NetSyncingTracingOn, "Sending Hitpoints to self");
										PacketGroup hitsPG = PacketGroup.AcquireMultiUsePG();
										hitsPG.AcquirePacket<UpdateCurrentHealthOutPacket>().Prepare(ch.FlaggedUid, ch.Hits, ch.MaxHits, true); //0xa1
										myConn.SendPacketGroup(hitsPG);
										if (hasParty) {
											SendPGToPartyMembers(ch, partyMembers, hitsPG);
										}
										hitsPG.Dispose();
									}
								}
							}
						}
						if (flagsChanged || basePropsChanged || ((directionChanged || posChanged) && (!requestedStep || teleported))) {
							Logger.WriteInfo(Globals.NetSyncingTracingOn, "Sending char info to self");
							DrawGamePlayerOutPacket dgpot = Pool<DrawGamePlayerOutPacket>.Acquire();
							dgpot.Prepare(myState, ch); //0x20
							myConn.SendSinglePacket(dgpot);
						}
						if (highlightChanged) {
							DrawObjectOutPacket doop = Pool<DrawObjectOutPacket>.Acquire();
							doop.Prepare(ch, ch.GetHighlightColorFor(ch)); //0x78							
							myConn.SendSinglePacket(doop);
						}

						if (warModeChanges) {
							PreparedPacketGroups.SendWarMode(myConn, ch.Flag_WarMode);
						}
						if (posChanged) {
							Map oldMap = this.point.GetMap();
							bool mapChanged = oldMap != chMap;
							int updateRange = ch.UpdateRange;

							if (mapChanged) {//other map. We must clear the view, and possibly change client's facet
								int newFacet = chMap.Facet;
								if (oldMap.Facet != newFacet) {
									PreparedPacketGroups.SendFacetChange(myConn, newFacet);
								}
								PacketGroup pg = null;
								foreach (Thing t in oldMap.GetThingsInRange(this.point.X, this.point.Y, updateRange)) {
									Logger.WriteInfo(Globals.NetSyncingTracingOn, "Removing thing (" + t + ") from own view");
									if (pg == null) {
										pg = PacketGroup.AcquireSingleUsePG();
									}
									pg.AcquirePacket<DeleteObjectOutPacket>().Prepare(t);
								}
								if (pg != null) {
									myConn.SendPacketGroup(pg);
								}
							}
							foreach (Thing thingInRange in chMap.GetThingsInRange(chX, chY, updateRange)) {
								if (thingInRange != ch) {//it isn't me
									if (ch.CanSeeForUpdate(thingInRange).Allow && (mapChanged ||
									!ch.CanSeeForUpdateFrom(this.point, thingInRange).Allow)) {//I can see it now, but couldn't see it before
										Logger.WriteInfo(Globals.NetSyncingTracingOn, "Sending thing (" + thingInRange + ") to self");
										AbstractCharacter newChar = thingInRange as AbstractCharacter;
										if (newChar != null) {
											PacketSequences.SendCharInfoWithPropertiesTo(ch, myState, myConn, newChar);
											UpdateCurrentHealthOutPacket uchop = Pool<UpdateCurrentHealthOutPacket>.Acquire();
											uchop.Prepare(newChar.FlaggedUid, newChar.Hits, newChar.MaxHits, false); //0xa1
											myConn.SendSinglePacket(uchop);
										} else {
											AbstractItem newItem = (AbstractItem) thingInRange;
											newItem.GetOnGroundUpdater().SendTo(ch, myState, myConn);

											PacketSequences.TrySendPropertiesTo(myState, myConn, newItem);
										}
									}
								}
							}
						}
						if (mountChanged) {
							this.SendMountChange(myConn, ch, ref pgRemoveMount, ref pgUpdateMount);
						}
					}
				}

				if (posChanged || directionChanged || flagsChanged || warModeChanges || highlightChanged ||
						invisChanged || hitsChanged || nameChanged || mountChanged || basePropsChanged) {
					int range = Globals.MaxUpdateRange;
					if (teleported) {
						this.RemoveFromViewIfNeeded();
					} else if (posChanged) {
						range++;//not teleported, that means only a step, so we update wider range
					}

					bool myCharInfosTouched = false;
					bool myMovingsTouched = false;

					PacketGroup pgDeleteObject = null;
					PacketGroup pgPetStatus = null;
					PacketGroup pgOtherStatus = null;
					PacketGroup pgHitsPacket = null;

					foreach (AbstractCharacter viewer in chMap.GetPlayersInRange(chX, chY, (ushort) range)) {
						if (viewer != ch) {
							GameState viewerState = viewer.GameState;
							if (viewerState != null) {
								TcpConnection<GameState> viewerConn = viewerState.Conn;
								bool viewerCanSeeForUpdateAtPointChecked = false;
								bool viewerCanSeeForUpdateAtPoint = false;
								bool viewerCanSeeForUpdateChecked = false;
								bool viewerCanSeeForUpdate = false;

								if ((!teleported) && (invisChanged || posChanged)) { //if teleported, we're already done
									if (!invisChanged) {
										viewerCanSeeForUpdateAtPoint = viewer.CanSeeForUpdateAt(this.point, ch).Allow;
										viewerCanSeeForUpdateAtPointChecked = true;
									}
									if (invisChanged || viewerCanSeeForUpdateAtPoint) {
										viewerCanSeeForUpdate = viewer.CanSeeForUpdate(ch).Allow;
										viewerCanSeeForUpdateChecked = true;
										if (!viewerCanSeeForUpdate) {//they did see us, but now they dont. RemoveFromView.
											if (pgDeleteObject == null) {
												pgDeleteObject = PacketGroup.AcquireMultiUsePG();
												pgDeleteObject.AcquirePacket<DeleteObjectOutPacket>().Prepare(ch);
											}
											Logger.WriteInfo(Globals.NetSyncingTracingOn, "Removing " + ch + " from view of " + viewerState);
											viewerConn.SendPacketGroup(pgDeleteObject);
										}
									}
								}
								if (!viewerCanSeeForUpdateChecked) {
									viewerCanSeeForUpdate = viewer.CanSeeForUpdate(ch).Allow;
								}
								if (viewerCanSeeForUpdate) {
									bool hitsSent = false;
									bool newCharSent = false;
									if (invisChanged || posChanged) {
										if (!invisChanged) {
											if (!viewerCanSeeForUpdateAtPointChecked) {
												viewerCanSeeForUpdateAtPoint = viewer.CanSeeForUpdateAt(this.point, ch).Allow;
											}
										}
										if (invisChanged || !viewerCanSeeForUpdateAtPoint) {
											//viewer didn't see us, but he does now - we send newchar packet
											myCharInfosTouched = true;
											int highlight = (int) ch.GetHighlightColorFor(viewer);
											PacketGroup myCharInfo = myCharInfos[highlight];
											if (myCharInfo == null) {
												myCharInfo = PacketGroup.AcquireMultiUsePG();
												myCharInfo.AcquirePacket<DrawObjectOutPacket>().Prepare(ch, (HighlightColor) highlight); //0x78
												myCharInfos[highlight] = myCharInfo;
											}
											Logger.WriteInfo(Globals.NetSyncingTracingOn, "Sending new char info to " + viewerState);
											viewerConn.SendPacketGroup(myCharInfo);
											newCharSent = true;
											if (propertiesExist && Globals.UseAosToolTips && viewerState.Version.AosToolTips) {
												propertiesExist = ProcessCharProperties(ch, ref toolTipsArray, viewerState, viewerConn);
											}

											if (!hasParty || !partyMembers.Contains(viewer)) { //new char, send hitpoints
												SendPercentHitsPacket(ch, ref pgHitsPacket, viewerState, viewerConn);
											}
											hitsSent = true;
										}
									}
									if (!newCharSent) {
										if (propertiesChanged && propertiesExist) {
											if (Globals.UseAosToolTips && viewerState.Version.AosToolTips) {
												propertiesExist = ProcessCharProperties(ch, ref toolTipsArray, viewerState, viewerConn);
											}
										}
										if (posChanged || directionChanged || flagsChanged || warModeChanges || highlightChanged || basePropsChanged) {
											myMovingsTouched = true;
											int highlight = (int) ch.GetHighlightColorFor(viewer);
											PacketGroup myMoving = myMovings[highlight];
											if (myMoving == null) {
												myMoving = PacketGroup.AcquireMultiUsePG();
												myMoving.AcquirePacket<UpdatePlayerPacket>().Prepare(ch, running, (HighlightColor) highlight); //0x77
												myMovings[highlight] = myMoving;
											}
											Logger.WriteInfo(Globals.NetSyncingTracingOn, "Sending moving char to " + viewerState);
											viewerConn.SendPacketGroup(myMoving);
										}
										if (mountChanged) {
											this.SendMountChange(viewerConn, ch, ref pgRemoveMount, ref pgUpdateMount);
										}
									}
									if (nameChanged) {
										hitsSent = true;
										if (viewer.CanRename(ch)) {
											if (pgPetStatus == null) {
												pgPetStatus = PacketGroup.AcquireMultiUsePG();
												pgPetStatus.AcquirePacket<StatusBarInfoOutPacket>().Prepare(ch, StatusBarType.Pet);
											}
											Logger.WriteInfo(Globals.NetSyncingTracingOn, "Sending pet status " + viewerState);
											viewerConn.SendPacketGroup(pgPetStatus);
										} else {
											if (pgOtherStatus == null) {
												pgOtherStatus = PacketGroup.AcquireMultiUsePG();
												pgOtherStatus.AcquirePacket<StatusBarInfoOutPacket>().Prepare(ch, StatusBarType.Other);
											}
											Logger.WriteInfo(Globals.NetSyncingTracingOn, "Sending simple status " + viewerState);
											viewerConn.SendPacketGroup(pgOtherStatus);
										}
									}
									if (hitsChanged && !hitsSent) {
										if (!hasParty || !partyMembers.Contains(viewer)) {
											SendPercentHitsPacket(ch, ref pgHitsPacket, viewerState, viewerConn);
										}
									}
								}
							}
						}
					}

					if (pgDeleteObject != null) {
						pgDeleteObject.Dispose();
					}
					if (pgPetStatus != null) {
						pgPetStatus.Dispose();
					}
					if (pgOtherStatus != null) {
						pgOtherStatus.Dispose();
					}
					if (pgHitsPacket != null) {
						pgHitsPacket.Dispose();
					}

					if (myMovingsTouched) {
						for (int i = 0, n = myMovings.Length; i < n; i++) {
							PacketGroup pg = myMovings[i];
							if (pg != null) {
								pg.Dispose();
								myMovings[i] = null;
							}
						}
					}

					if (myCharInfosTouched) {
						for (int i = 0, n = myCharInfos.Length; i < n; i++) {
							PacketGroup pg = myCharInfos[i];
							if (pg != null) {
								pg.Dispose();
								myCharInfos[i] = null;
							}
						}
					}

					ch.Flag_Moving = false;
				}
				if (pgRemoveMount != null) {
					pgRemoveMount.Dispose();
				}
				if (pgUpdateMount != null) {
					pgUpdateMount.Dispose();
				}
			}

			private static void SendPercentHitsPacket(AbstractCharacter ch, ref PacketGroup pgHitsPacket, GameState viewerState, TcpConnection<GameState> viewerConn) {
				if (pgHitsPacket == null) {
					pgHitsPacket = PacketGroup.AcquireMultiUsePG();
					pgHitsPacket.AcquirePacket<UpdateCurrentHealthOutPacket>().Prepare(ch.FlaggedUid, ch.Hits, ch.MaxHits, false);
				}
				Logger.WriteInfo(Globals.NetSyncingTracingOn, "Sending hits packet " + viewerState);
				viewerConn.SendPacketGroup(pgHitsPacket);
			}

			private static void SendPGToPartyMembers(AbstractCharacter self, ICollection<AbstractCharacter> partyMembers, PacketGroup statsPG) {
				foreach (AbstractCharacter partyMember in partyMembers) {
					if (self.CanSeeCoordinates(partyMember)) {
						GameState partyState = partyMember.GameState;
						if (partyState != null) {
							partyState.Conn.SendPacketGroup(statsPG);
						}
					}
				}
			}

			private void SendMountChange(TcpConnection<GameState> viewerConn, AbstractCharacter ch, ref PacketGroup pgRemoveMount, ref PacketGroup pgUpdateMount) {
				AbstractCharacter myMount = ch.Mount;
				if (myMount == null) {
					if (pgRemoveMount == null) {
						pgRemoveMount = PacketGroup.AcquireMultiUsePG();
						pgRemoveMount.AcquirePacket<DeleteObjectOutPacket>().Prepare(this.mountUid | 0x40000000);
					}
					Logger.WriteInfo(Globals.NetSyncingTracingOn, "Removing mount (#" + this.mountUid.ToString("x", CultureInfo.InvariantCulture) + ") for " + viewerConn.State.Character);
					viewerConn.SendPacketGroup(pgRemoveMount);
				} else {
					if (pgUpdateMount == null) {
						pgUpdateMount = PacketGroup.AcquireMultiUsePG();
						pgUpdateMount.AcquirePacket<WornItemOutPacket>().PrepareMount(ch.FlaggedUid, myMount);
					}
					Logger.WriteInfo(Globals.NetSyncingTracingOn, "Sending mount (#" + this.mountUid.ToString("x", CultureInfo.InvariantCulture) + ") to " + viewerConn.State.Character);
					viewerConn.SendPacketGroup(pgUpdateMount);
				}
			}

			private void RemoveFromViewIfNeeded() {
				ImmutableRectangle rect = new ImmutableRectangle(this.point, Globals.MaxUpdateRange);

				PacketGroup pg = null;
				Map map = this.point.GetMap();

				foreach (AbstractCharacter viewer in map.GetPlayersInRectangle(rect)) {
					GameState state = viewer.GameState;
					if (state != null) {
						if ((viewer.CanSeeForUpdateAt(this.point, this.thing).Allow) && (!viewer.CanSeeForUpdate(this.thing).Allow)) {
							if (pg == null) {
								pg = PacketGroup.AcquireMultiUsePG();
								pg.AcquirePacket<DeleteObjectOutPacket>().Prepare(this.thing);
							}
							Logger.WriteInfo(Globals.NetSyncingTracingOn, "Removing thing (" + this.thing + ") from the view of " + viewer);
							state.Conn.SendPacketGroup(pg);
						}
					}
				}
				if (pg != null) {
					pg.Dispose();
				}
			}

			private void UpdateSkills(GameState myState, TcpConnection<GameState> myConn) {
				if (this.changedSkillsCount > 0) {
					PacketGroup pg = PacketGroup.AcquireSingleUsePG();
					for (int i = 0; i < this.changedSkillsCount; i++) {
						int skillId = this.changedSkills[i];
						ISkill skill = this.thing.GetSkillObject(skillId);
						Logger.WriteInfo(Globals.NetSyncingTracingOn, "UpdateSkill id: " + skillId);
						pg.AcquirePacket<SendSkillsOutPacket>().PrepareSingleSkillUpdate((ushort) skillId, skill, myState.Version.DisplaySkillCaps);
					}
					myConn.SendPacketGroup(pg);
				}
			}
		}

		//get an ItemState instance from the pool, or create a new one
		private CharState PopAndEnqueueInstance(AbstractCharacter ch) {
			CharState state = ch.syncState;
			if (state != null) {
				return state; //we assume it's enqueued already and stuff. No one is to touch AbstractCharacter.syncState but this class!
			}
			state = Pool<CharState>.Acquire();
			state.thing = ch;
			ch.syncState = state;
			this.queue.Enqueue(state);
			this.autoResetEvent.Set();
			return state;
		}

		[Flags]
		internal enum CharSyncFlags {
			None = 0x00000000,
			Resend = 0x10000000, //complete update - after creation

			//these are same as in MovementType - do not change the values
			Walking = 0x00000001,
			Running = 0x00000002,
			RequestedStep = 0x00000004,
			Teleport = 0x00000008,

			//char updates
			BaseProps = 0x00000010, //Model, Color
			Direction = 0x00000020,
			Name = 0x00000040,
			Flags = 0x00000080,
			Position = 0x00000100,
			Visibility = 0x00000200, //we can change visibility even without changing flags (for particular people etc.)
			Mount = 0x00000400,
			Highlight = 0x00000800,

			//status
			Hits = 0x00001000,
			Mana = 0x00002000,
			Stam = 0x00004000,
			Stats = 0x00008000, //str, dex, int + extended status props - gender, gold, resists, luck, damage, tithingpoints, weight, etc.

			Property = 0x00010000
		}
	}
}