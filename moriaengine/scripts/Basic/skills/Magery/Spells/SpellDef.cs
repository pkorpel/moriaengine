/*
    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
t
    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
    Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Networking;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	[Flags]
	public enum SpellFlag
	{
		None = 0,
		CanTargetStatic = 0x000001,
		CanTargetGround = 0x000002,
		CanTargetItem = 0x000004,
		CanTargetChar = 0x000008,
		CanTargetAnything = CanTargetStatic | CanTargetGround | CanTargetItem | CanTargetChar,
		AlwaysTargetSelf = 0x000010,
		CanEffectDeadChar = 0x000020,
		CanEffectStatic = 0x000040,
		CanEffectGround = 0x000080,
		CanEffectItem = 0x000100,
		CanEffectChar = 0x000200,
		CanEffectAnything = CanEffectStatic | CanEffectGround | CanEffectItem | CanEffectChar,
		EffectNeedsLOS = 0x000400,
		IsAreaSpell = 0x000800,
		TargetCanMove = 0x001000,
		UseMindPower = 0x002000, //otherwise, magery value is used
		IsHarmful = 0x004000,
		IsBeneficial = 0x008000
	}

	[ViewableClass]
	public class SpellDef : AbstractIndexedDef<SpellDef, int> {
		private static Dictionary<string, ConstructorInfo> spellDefCtorsByName = new Dictionary<string, ConstructorInfo>(StringComparer.OrdinalIgnoreCase);

		//private static Dictionary<int, SpellDef> byId = new Dictionary<int, SpellDef>();

		private TriggerGroup scriptedTriggers;

		//private int id;
		#region Accessors
		private FieldValue scrollItem;
		private FieldValue runeItem;
		private FieldValue name;
		private FieldValue castTime;
		private FieldValue flags;
		private FieldValue manaUse;
		private FieldValue requirements;
		private FieldValue resources;
		private FieldValue difficulty;
		private FieldValue effect;
		private FieldValue sound;
		private FieldValue runes;
		private FieldValue effectRange;
		private string runeWords;

		public new static SpellDef GetByDefname(string defname) {
			return AbstractScript.GetByDefname(defname) as SpellDef;
		}

		public static SpellDef GetById(int key) {
			return GetByDefIndex(key);
		}

		public static ICollection<SpellDef> AllSpellDefs {
			get {
				return AllIndexedDefs;
			}
		}

		//public override string ToString() {
		//    return string.Concat(this.PrettyDefname, " (spellId ", this.Id.ToString(), ")");
		//}

		public int Id {
			get {
				return this.DefIndex;
			}
		}

		public string Name {
			get {
				return (string) this.name.CurrentValue;
			}
			set {
				this.name.CurrentValue = value;
			}
		}

		public SpellScrollDef ScrollItem {
			get {
				return (SpellScrollDef) this.scrollItem.CurrentValue;
			}
			set {
				this.scrollItem.CurrentValue = value;
			}
		}

		public ItemDef RuneItem {
			get {
				return (ItemDef) this.runeItem.CurrentValue;
			}
			set {
				this.runeItem.CurrentValue = value;
			}
		}

		public double CastTime {
			get {
				return (double) this.castTime.CurrentValue;
			}
			set {
				this.castTime.CurrentValue = value;
			}
		}

		public SpellFlag Flags {
			get {
				return (SpellFlag) this.flags.CurrentValue;
			}
			set {
				this.flags.CurrentValue = value;
			}
		}

		public int ManaUse {
			get {
				return (int) this.manaUse.CurrentValue;
			}
			set {
				this.manaUse.CurrentValue = value;
			}
		}

		public ResourcesList Requirements {
			get {
				return (ResourcesList) this.requirements.CurrentValue;
			}
			set {
				this.requirements.CurrentValue = value;
			}
		}

		public ResourcesList Resources {
			get {
				return (ResourcesList) this.resources.CurrentValue;
			}
			set {
				this.resources.CurrentValue = value;
			}
		}

		public int Difficulty {
			get {
				object current = this.difficulty.CurrentValue;
				if ((current == null) || Convert.ToInt32(current) < 0) {
					//TODO extract from requirement ResList
					return 0;
				}
				return (int) current;
			}
			set {
				this.difficulty.CurrentValue = value;
			}
		}

		public double[] Effect {
			get {
				return (double[]) this.effect.CurrentValue;
			}
			set {
				this.effect.CurrentValue = value;
			}
		}

		public double GetEffectForValue(double spellpower) {
			return ScriptUtil.EvalRangePermille(spellpower, this.Effect);
		}

		public SoundNames Sound {
			get {
				return (SoundNames) this.sound.CurrentValue;
			}
			set {
				this.sound.CurrentValue = value;
			}
		}

		public string Runes {
			get {
				return (string) this.runes.CurrentValue;
			}
			set {
				this.runes.CurrentValue = value;
				this.runeWords = null;
			}
		}

		public int EffectRange {
			get {
				return (int) this.effectRange.CurrentValue;
			}
			set {
				this.effectRange.CurrentValue = value;
			}
		}

		public string GetRuneWords() {
			if (this.runeWords == null) {
				string runes = this.Runes.ToLowerInvariant();
				int n = runes.Length;
				string[] arr = new string[n];
				for (int i = 0; i < n; i++) {
					arr[i] = this.GetRuneWord(runes[i]);
				}
				this.runeWords = string.Join(" ", arr);
			}
			return this.runeWords;
		}

		public int GetManaUse(bool isFromScroll) {
			if (isFromScroll) {
				return this.ManaUse / 2;
			}
			return this.ManaUse;
		}

		public int GetDifficulty(bool isFromScroll) {
			if (isFromScroll) {
				return this.Difficulty / 2;
			}
			return this.Difficulty;
		}

		private string GetRuneWord(char ch) {
			switch (ch) {
				case 'a': return "Ruth";	//hnev
				case 'b': return "Er";		//jeden,malo
				case 'c': return "Mor";		//temny
				case 'd': return "Curu";	//dovednost
				case 'e': return "Dol";		//hlava
				case 'f': return "Ruin";	//plamen
				case 'g': return "Esgal";	//zástìna
				case 'h': return "Sul";		//vitr
				case 'i': return "Anna";	//dar
				case 'j': return "Del";		//hruza
				case 'k': return "Heru";	//pán
				case 'l': return "Fuin";	//temnota
				case 'm': return "Aina";	//svaty
				case 'n': return "Sereg";	//krev
				case 'o': return "Morgul";	//temnamagie
				case 'p': return "Kel";		//odejit
				case 'q': return "Gor";		//hruza,des
				case 'r': return "Faroth";	//pronasledovat
				case 's': return "Tir";		//bditstrezit
				case 't': return "Barad";	//vez
				case 'u': return "Ril";		//trpit
				case 'v': return "Beleg";	//Mohutny
				case 'w': return "Loth";	//magickýkvìt
				case 'x': return "Val";		//mocnost
				case 'y': return "Kemen";	//zeme
				case 'z': return "Fea";		//duch
			}
			throw new SEException("Wrong spell rune " + ch);
		}
		#endregion Accessors

		#region Loading from scripts
		public SpellDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {

			this.name = this.InitTypedField("name", null, typeof(string));
			this.scrollItem = this.InitThingDefField("scrollItem", null, typeof(SpellScrollDef));
			this.runeItem = this.InitThingDefField("runeItem", null, typeof(ItemDef));
			this.castTime = this.InitTypedField("castTime", null, typeof(double));
			this.flags = this.InitTypedField("flags", null, typeof(SpellFlag));
			this.manaUse = this.InitTypedField("manaUse", 10, typeof(int));
			this.requirements = this.InitTypedField("requirements", null, typeof(ResourcesList));
			this.resources = this.InitTypedField("resources", null, typeof(ResourcesList));
			this.difficulty = this.InitTypedField("difficulty", null, typeof(int));
			this.effect = this.InitTypedField("effect", null, typeof(double[]));
			this.sound = this.InitTypedField("sound", -1, typeof(SoundNames));
			this.runes = this.InitTypedField("runes", null, typeof(string));
			this.effectRange = this.InitTypedField("effectRange", 5, typeof(int));
		}

		public new static void Bootstrap() {
			//SpellDef script sections are special in that they have numeric header indicating spell id in spellbooks
			RegisterDefnameParser<SpellDef>(ParseDefnames);
		}

		private static void ParseDefnames(PropsSection section, out string defname, out string altdefname) {
			ushort spellId;
			if (!ConvertTools.TryParseUInt16(section.HeaderName, out spellId)) {
				throw new ScriptException("Unrecognized format of the id number in the spelldef script header.");
			}
			defname = "spell_" + spellId.ToString(CultureInfo.InvariantCulture);

			PropsLine defnameLine = section.TryPopPropsLine("defname");
			if (defnameLine != null) {
				altdefname = ConvertTools.LoadSimpleQuotedString(defnameLine.Value);

				if (string.Equals(defname, altdefname, StringComparison.OrdinalIgnoreCase)) {
					Logger.WriteWarning("Defname redundantly specified for " + section.HeaderType + " " + LogStr.Ident(defname) + ".");
					altdefname = null;
				}
			} else {
				altdefname = null;
			}
		}

		public override void LoadScriptLines(PropsSection ps) {
			this.DefIndex = ConvertTools.ParseInt32(this.Defname.Substring(6)); //"spell_" = 6chars

			base.LoadScriptLines(ps);

			if (ps.TriggerCount > 0) {
				ps.HeaderName = "t__" + this.Defname + "__";
				this.scriptedTriggers = ScriptedTriggerGroup.Load(ps);
			}
		}

		protected override void On_AfterLoadFromScripts() {
			base.On_AfterLoadFromScripts();

			ResourcesList.ThrowIfNotConsumable(this.Resources);
		}

		public override void Unload() {
			if (this.scriptedTriggers != null) {
				this.scriptedTriggers.Unload();
			}
			base.Unload();
		}

		#endregion Loading from scripts

		#region Trigger methods
		public TriggerResult TryCancellableTrigger(IPoint3D self, TriggerKey td, ScriptArgs sa) {
			//cancellable trigger just for the one triggergroup
			if (this.scriptedTriggers != null) {
				if (TagMath.Is1(this.scriptedTriggers.TryRun(self, td, sa))) {
					return TriggerResult.Cancel;
				}
			}
			return TriggerResult.Continue;
		}

		private static TriggerKey tkSuccess = TriggerKey.Acquire("success");
		private static TriggerKey tkStart = TriggerKey.Acquire("start");
		private static TriggerKey tkCauseSpellEffect = TriggerKey.Acquire("causespelleffect");
		private static TriggerKey tkSpellEffect = TriggerKey.Acquire("spelleffect");
		private static TriggerKey tkEffectChar = TriggerKey.Acquire("effectchar");
		private static TriggerKey tkEffectItem = TriggerKey.Acquire("effectitem");
		private static TriggerKey tkEffectGround = TriggerKey.Acquire("effectground");

		internal void Trigger_Select(SkillSequenceArgs mageryArgs) {
			//Checked so far: death, book on self
			//TODO: Check zones, frozen?, hypnoform?, cooldown?
			Character caster = mageryArgs.Self;
			//ResourcesList req = this.Requirements;
			//if (req != null) {
			//    if (!req.HasResourcesPresent(
			//}
			if (!this.CheckPermissionOutgoing(caster)) {
				return;
			}


			SpellFlag flags = this.Flags;
			if ((flags & SpellFlag.AlwaysTargetSelf) == SpellFlag.AlwaysTargetSelf) {
				if (!this.CheckPermissionIncoming(caster, caster)) {
					return;
				}
				mageryArgs.Target1 = caster;
				mageryArgs.PhaseStart();
				return;
			}
			if ((flags & SpellFlag.CanTargetAnything) != SpellFlag.None) {
				if (mageryArgs.Target1 == null) {//if not null, it already has a target
					Player self = caster as Player;
					if (self != null) {
						self.Target(SingletonScript<SpellTargetDef>.Instance, mageryArgs);
						return;
					}
					caster.AnnounceBug();
					throw new SEException("Only Players can target spells");
				}
				mageryArgs.PhaseStart();
				return;
			}

			throw new SEException("SpellDef.Trigger_Select - unfinished");
		}

		//return false = aborting
		internal TriggerResult Trigger_Start(SkillSequenceArgs mageryArgs) {
			var result = this.TryCancellableTrigger(mageryArgs.Self, tkStart, mageryArgs.scriptArgs);
			if (result != TriggerResult.Cancel) {
				result = this.On_Start(mageryArgs);
			}
			return result;
		}

		public virtual TriggerResult On_Start(SkillSequenceArgs mageryArgs) {
			return TriggerResult.Continue; //don't cancel
		}

		internal void Trigger_Success(SkillSequenceArgs mageryArgs) {
			//target visibility/distance/LOS and self being alive checked in magery stroke
			Character caster = mageryArgs.Self;

			if (!this.CheckPermissionOutgoing(caster)) {
				return;
			}

			var result = this.TryCancellableTrigger(caster, tkSuccess, mageryArgs.scriptArgs);
			if (result != TriggerResult.Cancel) {
				result = this.On_Success(mageryArgs);
				if (result == TriggerResult.Cancel) {
					return;
				}
			} else {
				return;
			}

			SpellFlag flags = this.Flags;
			IPoint3D target = mageryArgs.Target1;

			IPoint3D targetTopPoint = target.TopPoint;
			Point4D targetTop = targetTopPoint as Point4D; //sound is done after the effect, whereas the object could be already deleted. So we use this to preserve the position
			if (targetTop == null) {
				targetTop = new Point4D(targetTopPoint, caster.M);
			}
			bool isArea = (flags & SpellFlag.IsAreaSpell) == SpellFlag.IsAreaSpell;
			SpellEffectArgs sea = null;

			EffectFlag effectFlag;
			if (mageryArgs.Tool is SpellScroll) {
				effectFlag = EffectFlag.FromSpellScroll;
			} else {
				effectFlag = EffectFlag.FromSpellBook; //we assume there is no other possibility (for now?)
			}

			if ((flags & SpellFlag.IsBeneficial) == SpellFlag.IsBeneficial) {
				effectFlag |= EffectFlag.BeneficialEffect;
			} else if ((flags & SpellFlag.IsHarmful) == SpellFlag.IsHarmful) {
				effectFlag |= EffectFlag.HarmfulEffect;
			}

			bool singleEffectDone = false;
			Character targetAsChar = target as Character;
			if (targetAsChar != null) {
				if ((flags & SpellFlag.CanEffectChar) == SpellFlag.CanEffectChar) {
					singleEffectDone = true;
					this.GetSpellPowerAgainstChar(caster, target, targetAsChar, effectFlag, ref sea);
					if (this.CheckSpellPowerWithMessage(sea)) {
						this.Trigger_EffectChar(targetAsChar, sea);
					}
					this.MakeSound(targetTop);
				}
			} else {
				Item targetAsItem = target as Item;
				if (targetAsItem != null) {
					if ((flags & SpellFlag.CanEffectItem) == SpellFlag.CanEffectItem) {
						singleEffectDone = true;
						this.GetSpellPowerAgainstNonChar(caster, target, targetAsItem, effectFlag, ref sea);
						if (this.CheckSpellPowerWithMessage(sea)) {
							this.Trigger_EffectItem(targetAsItem, sea);
						}
						this.MakeSound(targetTop);
					}
				}
			}
			if (!singleEffectDone) {
				if (((flags & SpellFlag.CanEffectGround) == SpellFlag.CanEffectGround) ||
					(((flags & SpellFlag.CanEffectStatic) == SpellFlag.CanEffectStatic) && (target is AbstractInternalItem))) {
					singleEffectDone = true;

					this.GetSpellPowerAgainstNonChar(caster, target, targetTop, effectFlag, ref sea);
					if (this.CheckSpellPowerWithMessage(sea)) {
						this.Trigger_EffectGround(targetTop, sea);
					}
					this.MakeSound(targetTop);
				}
			}

			if (!singleEffectDone && !isArea) {
				throw new SEException(this + ": Invalid target and/or spell flag?!");
			}

			if (isArea) {
				bool canEffectItem = (flags & SpellFlag.CanEffectItem) == SpellFlag.CanEffectItem;
				bool canEffectChar = (flags & SpellFlag.CanEffectChar) == SpellFlag.CanEffectChar;
				if (canEffectItem || canEffectChar) {
					foreach (Thing t in caster.GetMap().GetThingsInRange(targetTop.X, targetTop.Y, this.EffectRange)) {
						if (t == target) { //already done
							continue;
						}
						targetTop = new Point4D(t.TopPoint); //make a sound at least once 
						Character ch = t as Character;
						if (ch != null) {
							if (canEffectChar) {
								this.GetSpellPowerAgainstChar(caster, target, ch, effectFlag, ref sea);
								if (this.CheckSpellPowerWithMessage(sea)) {
									if ((flags & SpellFlag.IsBeneficial) == SpellFlag.IsBeneficial) {
										if (Notoriety.GetCharRelation(caster, ch) < sea.CasterToMainTargetRelation) { //target "more enemy" than main target, we don't wanna benefit him.
											continue;
										}
									} else if ((flags & SpellFlag.IsHarmful) == SpellFlag.IsHarmful) {
										if (Notoriety.GetCharRelation(caster, ch) > sea.CasterToMainTargetRelation) { //target "more friendly" than main target, we don't wanna hurt him.
											continue;
										}
									}

									this.Trigger_EffectChar(ch, sea);
									if (!singleEffectDone) {
										singleEffectDone = true;
										this.MakeSound(targetTop);
									}
								}
							}
						} else if (canEffectItem) {
							Item i = t as Item;
							if (i != null) {
								this.GetSpellPowerAgainstNonChar(caster, target, i, effectFlag, ref sea);
								if (this.CheckSpellPowerWithMessage(sea)) {
									this.Trigger_EffectItem(i, sea);
									if (!singleEffectDone) {
										singleEffectDone = true;
										this.MakeSound(targetTop);
									}
								}
							}
						}
					}
					//TODO? effect ground in the whole area? Or only statics?
				}
			}

			//if (sea != null) {
			//    sea.Dispose();
			//}
		}

		private bool CheckSpellPowerWithMessage(SpellEffectArgs sea) {
			SpellFlag flags = this.Flags;
			if ((flags & SpellFlag.CanEffectDeadChar) != SpellFlag.CanEffectDeadChar) {
				Character targetAsChar = sea.CurrentTarget as Character;
				if ((targetAsChar != null) && (targetAsChar.Flag_Dead)) {
					sea.Caster.ClilocSysMessage(501857); // This spell won't work on that!
					return false;
				}
			}

			if ((flags & SpellFlag.IsHarmful) == SpellFlag.IsHarmful) {
				if (sea.SpellPower < 1) {
					GameState casterState = sea.Caster.GameState;
					if (casterState != null) {
						casterState.WriteLine(Loc<SpellDefLoc>.Get(casterState.Language).TargetResistedSpell);
					}
					Character targetAsChar = sea.CurrentTarget as Character;
					if (targetAsChar != null) {
						targetAsChar.ClilocSysMessage(501783); // You feel yourself resisting magical energy.
					}
					return false;
				}
			}
			return true;
		}

		private void GetSpellPowerAgainstChar(Character caster, IPoint3D mainTarget, Character currentTarget, EffectFlag effectFlag, ref SpellEffectArgs sea) {
			int spellPower;
			SpellFlag flags = this.Flags;
			if ((flags & SpellFlag.UseMindPower) == SpellFlag.UseMindPower) {
				int mindDef;
				if (caster.IsPlayerForCombat) {
					mindDef = currentTarget.MindDefenseVsP;
				} else {
					mindDef = currentTarget.MindDefenseVsM;
				}
				if (currentTarget.IsPlayerForCombat) {
					spellPower = (caster.MindPowerVsP - mindDef) * 10; //*10 because we need to be in thousands, like it is with skills
				} else {
					spellPower = (caster.MindPowerVsM - mindDef) * 10;
				}
			} else {
				spellPower = caster.GetSkill(SkillName.Magery);
			}

			if (sea == null) {
				sea = SpellEffectArgs.Acquire(caster, mainTarget, currentTarget, this, spellPower, effectFlag);
				if (!(mainTarget is Character)) {
					if ((flags & SpellFlag.IsBeneficial) == SpellFlag.IsBeneficial) {
						sea.CasterToMainTargetRelation = CharRelation.Allied;
					} else if ((flags & SpellFlag.IsHarmful) == SpellFlag.IsHarmful) {
						sea.CasterToMainTargetRelation = CharRelation.TempHostile;
					}
				}
			} else {
				sea.CurrentTarget = currentTarget;
				sea.SpellPower = spellPower;
			}
		}

		private void GetSpellPowerAgainstNonChar(Character caster, IPoint3D target, IPoint3D currentTarget, EffectFlag effectFlag, ref SpellEffectArgs sea) {
			int spellPower = caster.GetSkill(SkillName.Magery);
			if (sea == null) {
				sea = SpellEffectArgs.Acquire(caster, target, currentTarget, this, spellPower, effectFlag);
			} else {
				sea.CurrentTarget = currentTarget;
				sea.SpellPower = spellPower;
			}
		}

		public void Trigger_EffectChar(Character target, SpellEffectArgs spellEffectArgs) {
			if (!this.CheckPermissionIncoming(spellEffectArgs.Caster, target)) {
				return;
			}

			Character caster = spellEffectArgs.Caster;
			var result = caster.TryCancellableTrigger(tkCauseSpellEffect, spellEffectArgs.scriptArgs);
			if (result != TriggerResult.Cancel) {
				try {
					result = caster.On_CauseSpellEffect(target, spellEffectArgs);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (result != TriggerResult.Cancel) {
					result = target.TryCancellableTrigger(tkSpellEffect, spellEffectArgs.scriptArgs);
					if (result != TriggerResult.Cancel) {
						try {
							result = target.On_SpellEffect(spellEffectArgs);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
						if (result != TriggerResult.Cancel) {
							result = this.TryCancellableTrigger(target, tkEffectChar, spellEffectArgs.scriptArgs);
							if (result != TriggerResult.Cancel) {
								try {
									this.On_EffectChar(target, spellEffectArgs);
								} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
							}
						}
					}
				}
			}
		}

		public void Trigger_EffectItem(Item target, SpellEffectArgs spellEffectArgs) {
			if (!this.CheckPermissionIncoming(spellEffectArgs.Caster, target)) {
				return;
			}
			Character caster = spellEffectArgs.Caster;
			var result = caster.TryCancellableTrigger(tkCauseSpellEffect, spellEffectArgs.scriptArgs);
			if (result != TriggerResult.Cancel) {
				try {
					result = caster.On_CauseSpellEffect(target, spellEffectArgs);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (result != TriggerResult.Cancel) {
					result = target.TryCancellableTrigger(tkSpellEffect, spellEffectArgs.scriptArgs);
					if (result != TriggerResult.Cancel) {
						try {
							result = target.On_SpellEffect(spellEffectArgs);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
						if (result != TriggerResult.Cancel) {
							result = this.TryCancellableTrigger(target, tkEffectItem, spellEffectArgs.scriptArgs);
							if (result != TriggerResult.Cancel) {
								try {
									this.On_EffectItem(target, spellEffectArgs);
								} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
							}
						}
					}
				}
			}
		}

		public void Trigger_EffectGround(IPoint3D target, SpellEffectArgs spellEffectArgs) {
			if (!this.CheckPermissionIncoming(spellEffectArgs.Caster, target)) {
				return;
			}

			Character caster = spellEffectArgs.Caster;
			var result = caster.TryCancellableTrigger(tkCauseSpellEffect, spellEffectArgs.scriptArgs);
			if (result != TriggerResult.Cancel) {
				try {
					result = caster.On_CauseSpellEffect(target, spellEffectArgs);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (result != TriggerResult.Cancel) {
					result = this.TryCancellableTrigger(target, tkEffectGround, spellEffectArgs.scriptArgs);
					if (result != TriggerResult.Cancel) {
						try {
							this.On_EffectGround(target, spellEffectArgs);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					}
				}
			}
		}

		const RegionFlags anyAntiMagicOut = RegionFlags.NoBeneficialMagicOut | RegionFlags.NoEnemyTeleportingOut |
			RegionFlags.NoHarmfulMagicOut | RegionFlags.NoMagicOut | RegionFlags.NoTeleportingOut;
		const RegionFlags anyAntiMagicIn = RegionFlags.NoBeneficialMagicIn | RegionFlags.NoEnemyTeleportingIn |
			RegionFlags.NoHarmfulMagicIn | RegionFlags.NoMagicIn | RegionFlags.NoTeleportingIn;

		internal bool CheckPermissionOutgoing(Character caster) {
			FlaggedRegion region = caster.Region as FlaggedRegion;
			if (region != null) {
				RegionFlags regionFlags = region.Flags;
				if ((regionFlags & anyAntiMagicOut) != RegionFlags.Zero) { //there are some antimagic flags, let's check them out
					SpellFlag spellFlag = this.Flags;
					if ((regionFlags & RegionFlags.NoMagicOut) == RegionFlags.NoMagicOut) {
						caster.RedMessage(Loc<SpellDefLoc>.Get(caster.Language).ForbiddenMagicOut);
						return false;
					}
					if (((regionFlags & RegionFlags.NoBeneficialMagicOut) == RegionFlags.NoBeneficialMagicOut) &&
							((spellFlag & SpellFlag.IsBeneficial) == SpellFlag.IsBeneficial)) {
						caster.RedMessage(Loc<SpellDefLoc>.Get(caster.Language).ForbiddenBeneficialMagicOut);
						return false;
					}
					if (((regionFlags & RegionFlags.NoHarmfulMagicOut) == RegionFlags.NoHarmfulMagicOut) &&
							((spellFlag & SpellFlag.IsHarmful) == SpellFlag.IsHarmful)) {
						caster.RedMessage(Loc<SpellDefLoc>.Get(caster.Language).ForbiddenHarmfulMagicOut);
						return false;
					}
				}
			}
			return true;
		}

		internal bool CheckPermissionIncoming(Character caster, IPoint3D target) {
			FlaggedRegion targetRegion = caster.GetMap().GetRegionFor(target) as FlaggedRegion;
			if (targetRegion != null) {
				RegionFlags regionFlags = targetRegion.Flags;
				if ((regionFlags & anyAntiMagicIn) != RegionFlags.Zero) { //there are some antimagic flags, let's check them out
					SpellFlag spellFlag = this.Flags;
					if ((regionFlags & RegionFlags.NoMagicIn) == RegionFlags.NoMagicIn) {
						caster.RedMessage(Loc<SpellDefLoc>.Get(caster.Language).ForbiddenMagicIn);
						return false;
					}
					if (((regionFlags & RegionFlags.NoBeneficialMagicIn) == RegionFlags.NoBeneficialMagicIn) &&
							((spellFlag & SpellFlag.IsBeneficial) == SpellFlag.IsBeneficial)) {
						caster.RedMessage(Loc<SpellDefLoc>.Get(caster.Language).ForbiddenBeneficialMagicIn);
						return false;
					}
					if (((regionFlags & RegionFlags.NoHarmfulMagicIn) == RegionFlags.NoHarmfulMagicIn) &&
							((spellFlag & SpellFlag.IsHarmful) == SpellFlag.IsHarmful)) {
						caster.RedMessage(Loc<SpellDefLoc>.Get(caster.Language).ForbiddenHarmfulMagicIn);
						return false;
					}
				}
			}
			return true;
		}

		protected virtual TriggerResult On_Success(SkillSequenceArgs mageryArgs) {
			return TriggerResult.Continue;
		}

		protected virtual void On_EffectGround(IPoint3D target, SpellEffectArgs spellEffectArgs) {
		}

		protected virtual void On_EffectChar(Character target, SpellEffectArgs spellEffectArgs) {
			if ((this.Flags & SpellFlag.IsHarmful) == SpellFlag.IsHarmful) {
				target.Trigger_HostileAction(spellEffectArgs.Caster);
			}
		}

		protected virtual void On_EffectItem(Item target, SpellEffectArgs spellEffectArgs) {
		}

		public void MakeSound(IPoint4D place) {
			int sound = (int) this.Sound;
			if (sound != -1) {
				PacketSequences.SendSound(place, sound, Globals.MaxUpdateRange);
			}
		}
		#endregion Trigger methods

		#region Methods for usage outside normal magery sequence
		public void EffectChar(Character caster, Character target, EffectFlag sourceType) {
			SpellFlag flags = this.Flags;
			if ((flags & SpellFlag.IsBeneficial) == SpellFlag.IsBeneficial) {
				sourceType &= ~EffectFlag.HarmfulEffect;
				sourceType |= EffectFlag.BeneficialEffect;
			} else if ((flags & SpellFlag.IsHarmful) == SpellFlag.IsHarmful) {
				sourceType &= ~EffectFlag.BeneficialEffect;
				sourceType |= EffectFlag.HarmfulEffect;
			}

			SpellEffectArgs sea = null;
			this.GetSpellPowerAgainstChar(caster, target, target, sourceType, ref sea);
			this.MakeSound(target);
			if (this.CheckSpellPowerWithMessage(sea)) {
				this.Trigger_EffectChar(target, sea);
			}
		}

		//TODO:  versions for item and ground
		#endregion Methods for usage outside normal magery sequence
	}

	public class SpellEffectArgs {
		private Character caster;
		private IPoint3D currentTarget;
		private IPoint3D mainTarget;
		private SpellDef spellDef;
		private int spellPower;
		private CharRelation relation;
		private bool relationFoundOut;
		private EffectFlag effectFlag = EffectFlag.FromSpellBook;

		public readonly ScriptArgs scriptArgs;

		public SpellEffectArgs()
		{
			this.scriptArgs = new ScriptArgs(this);
		}

		public static SpellEffectArgs Acquire(Character caster, IPoint3D mainTarget, IPoint3D currentTarget, SpellDef spellDef, int spellPower, EffectFlag effectFlag) {
			SpellEffectArgs retVal = new SpellEffectArgs();
			retVal.caster = caster;
			retVal.mainTarget = mainTarget;
			retVal.currentTarget = currentTarget;
			retVal.spellDef = spellDef;
			retVal.spellPower = spellPower;
			retVal.effectFlag = effectFlag;
			return retVal;
		}

		//protected override void On_Reset() {
		//    this.source = null;
		//    this.target = null;
		//    this.spellDef = null;
		//    this.spellPower = 0;
		//    base.On_Reset();
		//}

		public Character Caster {
			get {
				return this.caster;
			}
		}

		public IPoint3D MainTarget {
			get {
				return this.mainTarget;
			}
		}

		public IPoint3D CurrentTarget {
			get {
				return this.currentTarget;
			}
			set {
				this.currentTarget = value;
			}
		}

		public SpellDef SpellDef {
			get {
				return this.spellDef;
			}
		}

		public int SpellPower {
			get {
				return this.spellPower;
			}
			set {
				this.spellPower = value;
			}
		}

		public CharRelation CasterToMainTargetRelation {
			get {
				if (!this.relationFoundOut) {
					Character targetAsChar = this.mainTarget as Character;
					if (targetAsChar != null) {
						this.relation = Notoriety.GetCharRelation(this.caster, targetAsChar);
					} else {
						this.relation = CharRelation.AlwaysHostile;
					}
					this.relationFoundOut = true;
				}
				return this.relation;
			}
			set { //used when target is no char
				this.relation = value;
				this.relationFoundOut = true;
			}
		}

		public EffectFlag EffectFlag {
			get {
				return this.effectFlag;
			}
		}
	}

	public sealed class SpellTargetDef : CompiledTargetDef {
		//better without message?
		//protected override void On_Start(Player self, object parameter) {
		//    self.SysMessage("Vyber cíl kouzla");
		//    base.On_Start(self, parameter);
		//}

		protected override void On_TargonCancel(Player caster, object parameter) {
			//SkillSequenceArgs mageryArgs = (SkillSequenceArgs) parameter;
			//mageryArgs.Dispose();
		}

		protected override TargetResult On_TargonGround(Player caster, IPoint3D targetted, object parameter) {
			SkillSequenceArgs mageryArgs = (SkillSequenceArgs) parameter;
			SpellDef spell = (SpellDef) mageryArgs.Param1;
			SpellFlag flags = spell.Flags;

			if ((flags & SpellFlag.CanTargetGround) == SpellFlag.CanTargetGround) {
				if (((flags & SpellFlag.TargetCanMove) != SpellFlag.TargetCanMove) && targetted is Thing) {
					//we pretend to have targetted the ground, cos we don't want it to move
					mageryArgs.Target1 = new Point3D(targetted.TopPoint);
				} else {
					mageryArgs.Target1 = targetted;
				}
				if (!spell.CheckPermissionIncoming(caster, mageryArgs.Target1)) {
					return TargetResult.Done;
				}
				mageryArgs.PhaseStart();
			} else {
				return TargetResult.RestartTargetting; //repeat targetting
			}
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonChar(Player caster, Character targetted, object parameter) {
			return this.TargonNonGround(caster, targetted, parameter, SpellFlag.CanTargetChar);
		}

		protected override TargetResult On_TargonItem(Player caster, Item targetted, object parameter) {
			return this.TargonNonGround(caster, targetted, parameter, SpellFlag.CanTargetItem);
		}

		protected override TargetResult On_TargonStatic(Player caster, AbstractInternalItem targetted, object parameter) {
			return this.TargonNonGround(caster, targetted, parameter, SpellFlag.CanTargetStatic);
		}

		private TargetResult TargonNonGround(Player caster, IPoint3D targetted, object parameter, SpellFlag targetSF) {
			SkillSequenceArgs mageryArgs = (SkillSequenceArgs) parameter;
			SpellDef spell = (SpellDef) mageryArgs.Param1;

			if ((spell.Flags & targetSF) == targetSF) {
				if (!spell.CheckPermissionIncoming(caster, targetted)) {
					return TargetResult.Done;
				}
				mageryArgs.Target1 = targetted;
				mageryArgs.PhaseStart();
			} else {
				return this.On_TargonGround(caster, targetted, parameter);
			}
			return TargetResult.Done;
		}
	}

	public class SpellDefLoc : CompiledLocStringCollection<SpellDefLoc> {
		internal string TargetResistedSpell = "Cíl odolal kouzlu!";
		internal string ForbiddenMagicIn = "Zde je zakázáno kouzlit";
		internal string ForbiddenMagicOut = "Odtud je zakázáno kouzlit";
		internal string ForbiddenHarmfulMagicIn = "Zde je zakázáno kouzlit škodlivá kouzla";
		internal string ForbiddenHarmfulMagicOut = "Odtud je zakázáno kouzlit škodlivá kouzla";
		internal string ForbiddenBeneficialMagicIn = "Zde je zakázáno kouzlit pøínosná kouzla";
		internal string ForbiddenBeneficialMagicOut = "Odtud je zakázáno kouzlit pøínosná kouzla";
	}
}

