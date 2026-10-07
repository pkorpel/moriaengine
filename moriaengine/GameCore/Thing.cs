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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using SteamEngine.Common;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;
using SteamEngine.Networking;
using SteamEngine.Persistence;
using SteamEngine.Regions;

namespace SteamEngine {

	[SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
	public abstract partial class Thing : PluginHolder, IPoint4D, IEnumerable<AbstractItem> {
		private TimeSpan createdAt = Globals.TimeAsSpan;//Server time of creation
		private ushort color;
		private ushort model;
		internal ThingDef def; //tis is changed even from outside the constructor in case of dupeitems...

		internal MutablePoint4D point4d; //made this internal because SetPosImpl is now abstract... -tar

		private int uid = -2;
		//internal Region region;
		//internal NetState netState;//No one is to touch this but the NetState class itself!

		internal object contOrTLL; //parent TLL or parent Thing (in fact it's a Thing only if it's a char we're being dragged by)
		//internal because of ThingLinkedList implementation

		private static int savedCharacters;
		private static int savedItems;
		private static List<bool> alreadySaved = new List<bool>();

		private static int loadedCharacters;
		private static int loadedItems;

		internal Thing nextInList;
		internal Thing prevInList;

		private static List<TriggerGroup> registeredTGs = new List<TriggerGroup>();

		private static UIDArray things = new UIDArray();
		private static int uidBeingLoaded = -1;

		[SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
		internal sealed class ThingSaveCoordinator : IBaseClassSaveCoordinator {
			public static readonly Regex thingUidRE = new Regex(@"^\s*#(?<value>(0x)?[\da-f]+)\s*$",
				RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

			public string FileNameToSave {
				get { return "things"; }
			}

			public void StartingLoading() {
				loadedCharacters = 0;
				loadedItems = 0;
			}

			public void SaveAll(SaveStream output) {
				Logger.WriteDebug("Saving Things.");
				output.WriteComment("Things");
				output.WriteLine();
				savedCharacters = 0;
				savedItems = 0;
				alreadySaved = new List<bool>(things.HighestUsedIndex);
				foreach (Thing t in things) {
					SaveThis(output, t.TopObj());//each thing should recursively save it's contained items
				}
				Logger.WriteDebug(string.Format(CultureInfo.InvariantCulture,
												"Saved {0} things: {1} items and {2} characters.",
												savedCharacters + savedItems, savedItems, savedCharacters));

				alreadySaved = null;
			}

			[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
			public void LoadingFinished() {
				//this means real end of file(s)
				uidBeingLoaded = -1;
				things.LoadingFinished();
				Logger.WriteDebug(string.Format(CultureInfo.InvariantCulture,
												"Loaded {0} things: {1} items and {2} characters.",
												loadedItems + loadedCharacters, loadedItems, loadedCharacters));
				foreach (Thing t in things) {
					try {
						t.On_AfterLoad();
					} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				}
			}

			public Type BaseType {
				get { return typeof(Thing); }
			}

			public string GetReferenceLine(object value) {
				return "#" + ((Thing) value).uid;
			}

			public Regex ReferenceLineRecognizer {
				get { return thingUidRE; }
			}

			[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
			public object Load(Match m) {
				int uid = int.Parse(m.Groups["value"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture);
				Thing thing = UidGetThing(uid);
				if (thing != null) {
					return thing;
				}
				throw new NonExistingObjectException("There is no thing with uid " + LogStr.Number(uid) + " to load.");
			}
		}

		//still needs to be put into the world
		protected Thing(ThingDef myDef) {
			this.def = myDef;
			this.model = (ushort) myDef.Model;
			this.color = (ushort) myDef.Color;
			this.point4d.SetXYZM(0xffff, 0xffff, 0, 0);
			if (uidBeingLoaded == -1) {
				things.Add(this);//sets uid
				this.Resend();
			} else {
				this.uid = uidBeingLoaded;
			}
			Globals.LastNew = this;
		}

		protected Thing(Thing copyFrom)
			: base(copyFrom) { //copying constuctor

			this.Resend();
			things.Add(this);//sets uid
			this.point4d.SetXYZM(copyFrom.point4d);
			this.def = copyFrom.def;
			this.color = copyFrom.color;
			this.model = copyFrom.model;
			//SetSectorPoint4D();
			Globals.LastNew = this;
		}


		#region static UidGet methods
		public static int UidClearFlags(int uid) {
			return (int) (((uint) uid) & ~0xc0000000);			//0x4*, 0x8*, * meaning zeroes padded to 8 digits total
		}

		[CLSCompliant(false)]
		public static int UidClearFlags(uint uid) {
			return (int) (uid & ~0xc0000000);			//0x4*, 0x8*, * meaning zeroes padded to 8 digits total
		}

		public static AbstractCharacter UidGetCharacter(int uid) {
			return things.Get(UidClearFlags(uid)) as AbstractCharacter;
		}

		[CLSCompliant(false)]
		public static AbstractCharacter UidGetCharacter(uint uid) {
			return things.Get(UidClearFlags(uid)) as AbstractCharacter;
		}

		public static AbstractItem UidGetContainer(int uid) {
			AbstractItem i = things.Get(UidClearFlags(uid)) as AbstractItem;
			if ((i != null) && (i.IsContainer)) {
				return i;
			}
			return null;
		}

		public static AbstractItem UidGetItem(int uid) {
			return things.Get(UidClearFlags(uid)) as AbstractItem;
		}

		[CLSCompliant(false)]
		public static AbstractItem UidGetItem(uint uid) {
			return things.Get(UidClearFlags(uid)) as AbstractItem;
		}

		public static Thing UidGetThing(int uid) {
			return things.Get(UidClearFlags(uid));
		}

		[CLSCompliant(false)]
		public static Thing UidGetThing(uint uid) {
			return things.Get(UidClearFlags(uid));
		}
		#endregion static UidGet methods

		public static void RegisterTriggerGroup(TriggerGroup tg) {
			if (!registeredTGs.Contains(tg)) {
				registeredTGs.Add(tg);
			}
		}

		internal void CheckPositionValidityAfterLoad() {
			if (this.IsLimbo || !this.GetMap().IsValidPos((IPoint2D) this)) {
				if (this.IsPlayer) {
					Logger.WriteWarning("Player " + LogStr.Ident(this) + " outside the world after load - P line missing. Putting to 1, 1, 0, 0.");
					this.P(1, 1, 0, 0);
				} else {
					Logger.WriteWarning("Thing " + LogStr.Ident(this) + " outside the world after load - P or CONT line missing. Deleting.");
					this.Delete();
				}
			}
		}

		//Property: Enumerable
		//An IEnumerable for the Things represented by this class
		public static IEnumerable<Thing> AllThings {
			get {
				return things;
			}
		}

		//Property: uid
		//An identification number that no other Thing (AbstractItem, AbstractCharacter, etc) has.
		public int Uid {
			get {
				return this.uid;
			}
		}

		internal void InternalSetUid(int uid) {
			this.uid = uid;
		}

		[CLSCompliant(false)]
		public virtual uint FlaggedUid {
			get {
				return (uint) this.Uid;
			}
		}

		public abstract byte FlagsToSend { get; }
		//Normal = 0x00,
		//Unknown = 0x01, (also warmode?)
		//CanAlterPaperdoll = 0x02,
		//Poisoned = 0x04,
		//GoldenHealth = 0x08,
		//Unknown2 = 0x10,
		//Unknown3 = 0x20,
		//WarMode = 0x40, - used as such in scripts
		//Hidden = 0x80


		#region Position accessors
		//Property: x
		//The x coordinate of this Thing. To change something's position, it's generally recommended that you set P
		//or use Go instead.
		public int X {
			get {
				this.ThrowIfDeleted();
				return this.point4d.x;
			}
			set {
				this.P(value, this.Y, this.Z, this.M);	//maybe the compiler will optimize this for us... I hope so! -SL
			}
		}

		public int Y {
			get {
				this.ThrowIfDeleted();
				return this.point4d.y;
			}
			set {
				this.P(this.X, value, this.Z, this.M);	//maybe the compiler will optimize this for us... I hope so! -SL
			}
		}

		public int Z {
			get {
				this.ThrowIfDeleted();
				return this.point4d.z;
			}
			set {
				this.P(this.X, this.Y, value, this.M);	//maybe the compiler will optimize this for us... I hope so! -SL
			}
		}

		//public byte mapplane { get { return m; } set { m = value; } }
		public byte M {
			get {
				this.ThrowIfDeleted();
				return this.point4d.m;
			}
			set {
				this.P(this.X, this.Y, this.Z, value);	//maybe the compiler will optimize this for us... I hope so! -SL
			}
		}

		public Point4D P() {
			return new Point4D(this.point4d);
		}


		public IPoint4D TopPoint {
			get {
				return this.TopObj();
			}
		}

		IPoint3D IPoint3D.TopPoint {
			get {
				return this.TopObj();
			}
		}

		IPoint2D IPoint2D.TopPoint {
			get {
				return this.TopObj();
			}
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual bool Flag_Disconnected {
			get {
				return false;
			}
		}

		public void P(int x, int y) {
			this.SetPosImpl(x, y, this.Z, this.M);
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public void P(Point2D point) {
			this.SetPosImpl(point.X, point.Y, this.Z, this.M);
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public void P(IPoint2D point) {
			this.SetPosImpl(point.X, point.Y, this.Z, this.M);
		}

		public void P(int x, int y, int z) {
			this.SetPosImpl(x, y, z, this.M);
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public void P(Point3D point) {
			this.SetPosImpl(point.X, point.Y, point.Z, this.M);
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public void P(IPoint3D point) {
			this.SetPosImpl(point.X, point.Y, point.Z, this.M);
		}

		public void P(int x, int y, int z, byte m) {
			this.SetPosImpl(x, y, z, m);
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public void P(Point4D point) {
			this.SetPosImpl(point.X, point.Y, point.Z, point.M);
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public void P(IPoint4D point) {
			this.SetPosImpl(point.X, point.Y, point.Z, point.M);
		}

		public void NudgeUp() {
			this.NudgeUp(1);
		}
		public void NudgeDown() {
			this.NudgeDown(1);
		}

		public void NudgeUp(int amt) {
			sbyte tmpZ = this.point4d.z;
			try {
				tmpZ = checked((sbyte) (tmpZ + amt));
				this.Z = tmpZ;
			} catch (OverflowException) {
				//OverheadMessage("This cannot be nudged that much (It would make its z coordinate too high).");
				this.Z = sbyte.MaxValue;
			}
		}

		public void NudgeDown(short amt) {
			sbyte tmpZ = this.point4d.z;
			try {
				tmpZ = checked((sbyte) (tmpZ - amt));
				this.Z = tmpZ;
			} catch (OverflowException) {
				//OverheadMessage("This cannot be nudged that much (It would make its z coordinate too low).");
				this.Z = sbyte.MinValue;
			}
		}
		#endregion P

		public virtual Thing Cont {
			get {
				return this;
			}
			set {
				throw new SanityCheckException("You can't give a Character a Cont");
			}
		}

		public Map GetMap() {
			return Map.GetMap(this.point4d.m);
		}

		public void Update() {
			this.Resend();
		}
		public void UpdateX() {
			this.RemoveFromView();
			this.Resend();
		}

		public int Color {
			get {
				return this.color;
			}
			set {
				if (value >= 0 && (value & ~0xc000) <= 0xbb6) {
					if (this.IsItem) {
						ItemSyncQueue.AboutToChange((AbstractItem) this);
					} else {
						CharSyncQueue.AboutToChangeBaseProps((AbstractCharacter) this);
					}
					this.color = (ushort) value;
				}
			}
		}

		[CLSCompliant(false)]
		public ushort ShortColor {
			get {
				return this.color;
			}
		}

		public int Model {
			get {
				return this.model;
			}
			set {
				if (this.IsItem) {
					ItemSyncQueue.AboutToChange((AbstractItem) this);
				} else {
					CharSyncQueue.AboutToChangeBaseProps((AbstractCharacter) this);
				}
				this.model = (ushort) value;
			}
		}

		[CLSCompliant(false)]
		public ushort ShortModel {
			get {
				return this.model;
			}
		}

		public TimeSpan CreatedAt {
			get {
				return this.createdAt;
			}
		}

		public ThingDef Def { get { return this.def; } }

		public virtual bool IsPlayer { get { return false; } }

		public int Height {
			get {
				return this.def.Height;
			}
		}

		public override bool IsDeleted {
			get {
				return (this.uid == -1);
			}
		}

		public override void Trigger(TriggerKey tk, ScriptArgs sa) {
			this.ThrowIfDeleted();
			for (int i = 0, n = registeredTGs.Count; i < n; i++) {
				TriggerGroup tg = registeredTGs[i];
				tg.Run(this, tk, sa);
			}
			base.Trigger(tk, sa);
			this.def.Trigger(this, tk, sa);
		}

		public override void TryTrigger(TriggerKey tk, ScriptArgs sa) {
			this.ThrowIfDeleted();
			for (int i = 0, n = registeredTGs.Count; i < n; i++) {
				TriggerGroup tg = registeredTGs[i];
				tg.TryRun(this, tk, sa);
			}
			base.TryTrigger(tk, sa);
			this.def.TryTrigger(this, tk, sa);
		}

		public override TriggerResult CancellableTrigger(TriggerKey tk, ScriptArgs sa) {
			this.ThrowIfDeleted();
			for (int i = 0, n = registeredTGs.Count; i < n; i++) {
				TriggerGroup tg = registeredTGs[i];
				if (TagMath.Is1(tg.Run(this, tk, sa))) {
					return TriggerResult.Cancel;
				}
			}
			if (TriggerResult.Cancel == this.CancellableTrigger(tk, sa)) {
				return TriggerResult.Cancel;
			}
			return this.def.CancellableTrigger(this, tk, sa);
		}

		public override TriggerResult TryCancellableTrigger(TriggerKey tk, ScriptArgs sa) {
			this.ThrowIfDeleted();
			for (int i = 0, n = registeredTGs.Count; i < n; i++) {
				TriggerGroup tg = registeredTGs[i];
				if (TagMath.Is1(tg.TryRun(this, tk, sa))) {
					return TriggerResult.Cancel;
				}
			}
			if (TriggerResult.Cancel == base.TryCancellableTrigger(tk, sa)) {
				return TriggerResult.Cancel;
			}
			return this.def.TryCancellableTrigger(this, tk, sa);
		}

		//------------------------
		//Public methods

		public abstract override string Name { get; set; }

		public virtual bool IsEquipped {
			get {
				return false;
			}
		}

		public virtual bool IsOnGround {
			get {
				return true;
			}
		}

		public virtual bool IsInContainer {
			get {
				return false;
			}
		}

		public abstract bool IsNotVisible { get; }

		public virtual Region Region {
			get {
				return this.GetMap().GetRegionFor(this.point4d);
			}
		}

		public abstract AbstractItem FindCont(int index);

		IEnumerator IEnumerable.GetEnumerator() {
			return this.GetEnumerator();
		}

		public abstract IEnumerator<AbstractItem> GetEnumerator();

		//------------------------
		//Private & internal stuff

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes"), LoadSection]
		public static Thing Load(PropsSection input) {
			//an Exception propagated away from this method is usually considered critical - load failed

			//is called for each separate section

			//Console.WriteLine("["+input.headerType+" "+input.headerName+"]");
			//no exception, ignore case.

			//we need defname and p(x,y, z, m)  to construct the thing.

			ThingDef thingDef = ThingDef.GetByDefname(input.HeaderName);

			if (thingDef == null) {
				Logger.WriteError(input.Filename, input.HeaderLine, "Defname '" + LogStr.Ident(input.HeaderName) + "' not found. Thing loading interrupted.");
				return null;
			}

			int _uid;
			PropsLine prop = input.TryPopPropsLine("uid");
			if (prop == null) {
				prop = input.TryPopPropsLine("serial");
			}
			if (prop != null) {
				if (!ConvertTools.TryParseInt32(prop.Value, out _uid)) {
					Logger.WriteError(input.Filename, prop.Line, "Unrecognized UID property format. Thing loading interrupted.");
					return null;
				}
			} else {
				Logger.WriteError(input.Filename, input.HeaderLine, "UID property not found. Thing loading interrupted.");
				return null;
			}
			_uid = UidClearFlags(_uid);

			uidBeingLoaded = _uid;//the constructor should set this as Uid
			Thing constructed = thingDef.CreateWhenLoading();//let's hope the P gets loaded properly later ;)

			things.AddLoaded(constructed, _uid);

			//now load the rest of the properties
			try {
				constructed.On_Load(input);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }

			constructed.LoadSectionLines(input);
			if (constructed.IsChar) loadedCharacters++;
			if (constructed.IsItem) loadedItems++;

			return constructed;
		}


		/// <summary>
		/// Called after this object was loaded.
		/// </summary>
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_AfterLoad() {
		}

		/// <summary>
		/// Called when this object is being loaded, before the LoadLine calls. 
		/// It exists because of ClassTemplate and it's autogenerating of the Save method itself. 
		/// With this, it's possible to implement custom save code...
		/// </summary>
		/// <param name="output">The output.</param>
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_Load(PropsSection output) {
		}

		public override void LoadLine(string filename, int line, string valueName, string valueString) {
			this.ThrowIfDeleted();
			switch (valueName) {
				case "p":
					object o = ObjectSaver.OptimizedLoad_SimpleType(valueString, typeof(Point4D));
					Point4D asPoint = o as Point4D;
					if (asPoint != null) {
						this.point4d.SetXYZM(asPoint);
					} else {
						this.point4d.SetParsedP((string) o);
					}
					//it will be put in world later by map or Cont
					break;
				case "color":
					this.color = ConvertTools.ParseUInt16(valueString);
					break;
				case "dispid":
				case "model":
				case "body":
					this.model = ConvertTools.ParseUInt16(valueString);
					break;
				case "createdat":
					this.createdAt = (TimeSpan) ObjectSaver.OptimizedLoad_SimpleType(valueString, typeof(TimeSpan));
					break;
				default:
					base.LoadLine(filename, line, valueName, valueString);
					break;
			}
		}

		/// <summary>Sets all uids to lowest possible value. Always save and restart after doing this.</summary>
		public static void ResetAllUids() {
			things.ReIndexAll();
		}

		[SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate"), CLSCompliant(false)]
		public static uint GetFakeUid() {
			return (uint) things.GetFakeUid();
		}

		[SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate"), CLSCompliant(false)]
		public static uint GetFakeItemUid() {
			return (uint) things.GetFakeUid() | 0x40000000;
		}

		public static void DisposeFakeUid(int uid) {
			things.DisposeFakeUid(UidClearFlags(uid));
		}

		[CLSCompliant(false)]
		public static void DisposeFakeUid(uint uid) {
			things.DisposeFakeUid(UidClearFlags(uid));
		}

		internal static void SaveThis(SaveStream output, Thing t) {
			if (t.IsDeleted) {
				Logger.WriteError("Thing " + LogStr.Ident(t) + " is already deleted.");
				return;
			}
			t.SaveWithHeader(output);
		}

		[Save]
		public void SaveWithHeader(SaveStream output) {
			while (alreadySaved.Count <= this.uid) {
				alreadySaved.Add(false);
			}
			if (!alreadySaved[this.uid]) {
				alreadySaved[this.uid] = true;
				if (this.IsChar) {
					savedCharacters++;
				} else {
					savedItems++;
				}

				output.WriteSection(this.GetType().Name, this.Def.PrettyDefname);
				this.Save(output);
				output.WriteLine();
				ObjectSaver.FlushCache(output);
				if (this.CanContain) {
					foreach (AbstractItem i in this) {
						SaveThis(output, i);
					}
				}
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public override void Save(SaveStream output) {
			this.ThrowIfDeleted();
			output.WriteValue("uid", this.uid);

			output.WriteValue("p", this.P());

			if (this.color != 0) {
				output.WriteValue("color", this.color);
			}

			if (this.model != this.def.Model) {
				output.WriteValue("model", this.model);
			}

			output.WriteValue("createdat", this.createdAt);
			try {
				this.On_Save(output);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			base.Save(output);//tagholder save
		}

		/// <summary>
		/// This is called when this object is being saved. 
		/// It exists because of ClassTemplate and it's autogenerating of the Save method itself. 
		/// With this, it's possible to implement custom save code...
		/// </summary>
		/// <param name="output">The output.</param>
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_Save(SaveStream output) {

		}

		public virtual Thing TopObj() {
			return this;
		}


		///*
		//    Method: Dupe

		//        Duplicates this character, possibly more than once.

		//    Parameters:
		//        ntimes - The number of times to duplicate this thing.
		// */
		//public void Dupe(int ntimes) {
		//    ThrowIfDeleted();
		//    //precondition sanity checks
		//    Sanity.IfTrueThrow(ntimes <= 0, "Dupe called with " + ntimes + " times - Only values above 0 are valid.");

		//    while (ntimes > 0) {
		//        this.Dupe();
		//        ntimes--;
		//    }
		//}

		/*
			Member: weight
			The weight of this thing. If you're going to change the weight on an item (instead of changing
			the weight on the itemdef/chardef to change the weight for all items with that def), always do it by
			setting this property. This ensures that the stuff this thing is in (containers, characters, etc)
			updates weight properly.
			
			well obviously I disable dsetting the weight. I may re-enable it once I need it, but probably not at this level -tar
		 */

		public abstract float Weight { get; }
		//	get {
		//		CheckDeleted();
		//		if (HasTag(weightTag)) {
		//			double wtag = GetTag0(weightTag);
		//			return (float) wtag;
		//		}
		//		return def.Weight;
		//	} set {
		//		CheckDeleted();
		//		float oldWeight = Weight;
		//		SetTag(weightTag,value);
		//		if (Cont!=null) {
		//			float weightChange = oldWeight - value;
		//			Cont.DecreaseWeightBy(weightChange);
		//		}
		//	}
		//}


		public abstract void FixWeight();

		protected internal abstract void AdjustWeight(float adjust);

		//internal virtual void DecreaseWeightBy(float adjust) {
		//}
		//internal virtual void IncreaseWeightBy(float adjust) {
		//}


		public sealed override void Delete() {
			if (this.IsDeleted) {
				return;
			}
			if (this.IsPlayer) {
				Globals.SrcWriteLine("Unable to remove player characters with Delete command. Use DeletePlayer().");
				return;
			}
			this.InternalDelete();
		}

		public void DeletePlayer() {
			this.InternalDelete();
		}

		internal void InternalDelete() {
			if (this.IsDeleted) {
				return;
			}
			if (!this.IsLimbo) {
				this.RemoveFromView();
			}
			this.InternalDeleteNoRFV();
		}

		internal void InternalDeleteNoRFV() {
			if (this.IsDeleted) {
				return;
			}
			base.Delete();
			this.Trigger_Destroy();

			things.RemoveAt(this.uid);
			this.uid = -1;
		}

		//fires the @destroy trigger on this Thing, after that removes the item from world. 
		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal virtual void Trigger_Destroy() {
			this.TryTrigger(TriggerKey.destroy, null);
			try {
				this.On_Destroy();
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		//method:On_Destroy
		//Thing`s implementation of trigger @Destroy,
		//does actually nothing, because finalizing of core Thing subclasses is done elsewhere
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_Destroy() {
			//this does nothing by default, because core classes use BeingDeleted (for safety against evil scripts)
		}

		public void Click() {
			this.Trigger_Click(Globals.SrcCharacter);
		}

		//method: Trigger_Click
		//fires the @itemClick / @charclick and @click triggers where this is the Thing being clicked on.
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member"), SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public void Trigger_AosClick(AbstractCharacter clicker) {
			this.ThrowIfDeleted();
			if (clicker == null)
				return;

			GameState clickerState = clicker.GameState;
			if (clickerState != null) {
				TcpConnection<GameState> clickerConn = clickerState.Conn;
				if (!clicker.CanSeeForUpdate(this).Allow) {
					PacketSequences.SendRemoveFromView(clickerConn, this.FlaggedUid);
				} else {
					ScriptArgs sa = new ScriptArgs(clicker, clickerState, clickerConn, this);
					if (TriggerResult.Cancel != this.TryCancellableTrigger(TriggerKey.aosClick, sa)) {
						//@aosClick on thing did not return 1
						try {
							this.On_AosClick(clicker, clickerState, clickerConn);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					}
				}
			}
		}

		//method: Trigger_Click
		//fires the @itemClick / @charclick and @click triggers where this is the Thing being clicked on.
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member"), SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public void Trigger_Click(AbstractCharacter clicker) {
			this.ThrowIfDeleted();
			if (clicker == null)
				return;

			GameState clickerState = clicker.GameState;
			if (clickerState != null) {
				TcpConnection<GameState> clickerConn = clickerState.Conn;
				if (!clicker.CanSeeForUpdate(this).Allow) {
					PacketSequences.SendRemoveFromView(clickerConn, this.FlaggedUid);
				} else {
					ScriptArgs sa = new ScriptArgs(clicker, this);
					var result = this.Trigger_SpecificClick(clicker, sa);
					if (result != TriggerResult.Cancel) {
						//@itemclick or @charclick on src did not return 1
						result = this.TryCancellableTrigger(TriggerKey.click, sa);
						if (result != TriggerResult.Cancel) {
							//@click on item did not return 1
							try {
								this.On_Click(clicker, clickerState, clickerConn);
							} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
						}
					}
				}
			}
		}

		internal abstract TriggerResult Trigger_SpecificClick(AbstractCharacter clickingChar, ScriptArgs sa);

		//method: On_Click
		//Thing`s implementation of trigger @Click,
		//sends the name (DecoratedName) of this Thing as plain overheadmessage
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_Click(AbstractCharacter clicker, GameState clickerState, TcpConnection<GameState> clickerConn) {
			PacketSequences.SendNameFrom(clicker.GameState.Conn, this,
				this.Name, 0);
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_AosClick(AbstractCharacter clicker, GameState clickerState, TcpConnection<GameState> clickerConn) {
			//aos client basically only clicks on incoming characters and corpses
			AosToolTips toolTips = this.GetAosToolTips(clicker.Language);
			PacketSequences.SendClilocNameFrom(clicker.GameState.Conn, this,
				toolTips.FirstId, 0, toolTips.FirstArgument);
		}

		public void DClick() {
			this.Trigger_DClick(Globals.SrcCharacter);
		}

		public void Use() {
			this.Trigger_DClick(Globals.SrcCharacter);
		}

		//method: Trigger_DClick
		//fires the @itemDClick / @charDClick and @dclick triggers where this is the Thing being doubleclicked.
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member"), SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public void Trigger_DClick(AbstractCharacter dclicker) {
			this.ThrowIfDeleted();
			if (dclicker == null)
				return;

			//deny triggers first
			bool isItem = this.IsItem;
			DenyResult result = this.Trigger_DenyDClick(dclicker, isItem);

			if (!result.Allow) {
				result.SendDenyMessage(dclicker);

			} else {//action triggers
				ScriptArgs sa = new ScriptArgs(dclicker, this);

				if (isItem) {
					dclicker.TryTrigger(TriggerKey.itemDClick, sa);
					try {
						dclicker.On_ItemDClick((AbstractItem) this);
					} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				} else {
					dclicker.TryTrigger(TriggerKey.charDClick, sa);
					try {
						dclicker.On_CharDClick((AbstractCharacter) this);
					} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				}

				this.TryTrigger(TriggerKey.dClick, sa);
				try {
					this.On_DClick(dclicker);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			}
		}

		private DenyResult Trigger_DenyDClick(AbstractCharacter dclicker, bool isItem) {
			DenyClickArgs denyArgs = new DenyClickArgs(dclicker, this);
			TriggerResult result;

			if (isItem) {
				result = dclicker.TryCancellableTrigger(TriggerKey.denyItemDClick, denyArgs);
				if (result != TriggerResult.Cancel) {
					try {
						result = dclicker.On_DenyItemDClick(denyArgs);
					} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				}
			} else {
				result = dclicker.TryCancellableTrigger(TriggerKey.denyItemDClick, denyArgs);
				if (result != TriggerResult.Cancel) {
					try {
						result = dclicker.On_DenyCharDClick(denyArgs);
					} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				}
			}

			if (result != TriggerResult.Cancel) {
				result = this.TryCancellableTrigger(TriggerKey.denyDClick, denyArgs);
				if (result != TriggerResult.Cancel) {
					try {
						this.On_DenyDClick(denyArgs);
					} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				}
			}
			return denyArgs.Result;
		}

		//method:On_DClick
		//Thing`s implementation of trigger @DClick,
		//does nothing.
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_DClick(AbstractCharacter dclicker) {
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_DenyDClick(DenyClickArgs args) {
		}

		//method:On_Create
		//Thing`s implementation of trigger @create,
		//does nothing.
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_Create() {

		}

		public override string ToString()
		{
			if (this.def != null) {
				return this.Name + " (0x" + this.uid.ToString("x", CultureInfo.InvariantCulture) + ")";
			}
			return "incomplete Thing (0x" + this.uid.ToString("x", CultureInfo.InvariantCulture) + ")";
		}

		public override int GetHashCode() {
			return this.uid;
		}

		public override bool Equals(object obj) {
			return ReferenceEquals(this, obj);
		}

		public virtual bool IsItem {
			get {
				return false;
			}
		}

		public virtual bool IsContainer {
			get {
				return false;
			}
		}

		//public virtual bool CanPutItemsOn {
		//    get {
		//        return this.CanContain;
		//    }
		//}

		public bool IsMulti {
			get {
				return this.Def.multiData != null;
			}
		}

		public virtual bool CanContain {
			get {
				//i.e. if this is either char or container
				return this.IsContainer;
			}
		}

		public virtual bool IsEquippable {
			get {
				return false;
			}
		}

		public virtual bool IsChar {
			get {
				return false;
			}
		}

		public AbstractItem NewItem(IThingFactory factory) {
			return this.NewItem(factory, 1);
		}

		public abstract AbstractItem NewItem(IThingFactory factory, int amount);

		public void Move(string dir) {
			this.Move(dir, 1);
		}

		public static void ClearAll() {
			foreach (Thing t in things) {
				t.uid = -1;
			}
			things.Clear();
		}

		public void RemoveFromView() {
			DeleteObjectOutPacket p = Pool<DeleteObjectOutPacket>.Acquire();
			p.Prepare(this);
			GameServer.SendToClientsWhoCanSee(this, p);
		}

		public abstract void Resend();

		[SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public AosToolTips GetAosToolTips(Language language) {
			AosToolTips toolTips = AosToolTips.GetFromCache(this, language);
			if (toolTips != null) {
				return toolTips;
			}

			toolTips = Pool<AosToolTips>.Acquire();
			toolTips.Language = language;

			int id;
			string argument;
			this.GetNameCliloc(out id, out argument);
			toolTips.AddLine(id, argument);

			this.TryTrigger(TriggerKey.buildAosToolTips, new ScriptArgs(toolTips, language));
			try {
				this.On_BuildAosToolTips(toolTips, language);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }

			toolTips.InitDone(this);

			return toolTips;//new or changed
		}

		[SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#"), SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "0#")]
		public virtual void GetNameCliloc(out int id, out string argument) {
			id = 1042971;
			argument = this.Name;
		}

		public virtual void On_BuildAosToolTips(AosToolTips opc, Language language) {
		}

		public virtual void InvalidateAosToolTips() {
			AosToolTips.RemoveFromCache(this);
		}

		//public Gump Dialog(string gumpName) {
		//	GumpDef GumpDef = GumpDef.Get(gumpName);
		//	if (GumpDef == null) {
		//		throw new SEException("There is no gump named "+LogStr.Ident(gumpName));
		//	} else {
		//		return Globals.src.Character.SendGump(this, GumpDef, null);
		//	}
		//}

		public Gump Dialog(GumpDef gump) {
			return this.Dialog(Globals.SrcCharacter, gump);
		}

		//volani primo s parametry (jak se s parametry nalozi, to zavisi na tom kterem dialogu)
		public Gump Dialog(GumpDef gump, params object[] paramArr) {
			return this.Dialog(Globals.SrcCharacter, gump, new DialogArgs(paramArr));
		}

		public Gump Dialog(GumpDef gump, DialogArgs args) {
			return this.Dialog(Globals.SrcCharacter, gump, args);
		}

		//jen presmerujeme volani jinam (bez argumentu ovsem)
		public Gump Dialog(AbstractCharacter sendTo, GumpDef gump) {
			return this.Dialog(sendTo, gump, new DialogArgs());
		}

		//volani primo s parametry (jak se s parametry nalozi, to zavisi na tom kterem dialogu)
		public Gump Dialog(AbstractCharacter sendTo, GumpDef gump, params object[] paramArr) {
			return this.Dialog(sendTo, gump, new DialogArgs(paramArr));
		}

		public Gump Dialog(AbstractCharacter sendTo, GumpDef gump, DialogArgs args) {
			if (sendTo != null) {
				return sendTo.SendGump(this, gump, args);
			}
			return null;
		}

		public void Message(string arg) {
			this.Message(arg, 0);
		}

		public void Message(string arg, int color) {
			this.ThrowIfDeleted();
			PacketSequences.SendOverheadMessageFrom(Globals.SrcTcpConnection, this, arg, color);
		}

		public void ClilocMessage(int msg, string args) {
			this.ThrowIfDeleted();
			PacketSequences.SendClilocMessageFrom(Globals.SrcTcpConnection, this, msg, 0, args);
		}

		public void ClilocMessage(int msg, params string[] args) {
			this.ThrowIfDeleted();
			PacketSequences.SendClilocMessageFrom(Globals.SrcTcpConnection, this, msg, 0, args);
		}

		public void ClilocMessage(int msg, int color, string args) {
			this.ThrowIfDeleted();
			PacketSequences.SendClilocMessageFrom(Globals.SrcTcpConnection, this, msg, color, args);
		}

		public void ClilocMessage(int msg, int color, params string[] args) {
			this.ThrowIfDeleted();
			PacketSequences.SendClilocMessageFrom(Globals.SrcTcpConnection, this, msg, color, args);
		}

		//public void SoundTo(ushort soundId, GameState toClient) {
		//    if (soundId != 0xffff) {
		//        if (toClient != null) {
		//            PacketSender.PrepareSound(this, soundId);
		//            PacketSender.SendTo(toClient, true);
		//        }
		//    }
		//}

		public void SoundTo(int soundId, AbstractCharacter toPlayer) {
			if (soundId != 0xffff) {
				if (toPlayer != null) {
					GameState state = toPlayer.GameState;
					if (state != null) {
						PlaySoundEffectOutPacket p = Pool<PlaySoundEffectOutPacket>.Acquire();
						p.Prepare(this, soundId);
						state.Conn.SendSinglePacket(p);
					}
				}
			}
		}

		public void Sound(int soundId) {
			this.Sound(soundId, Globals.MaxUpdateRange);
		}

		public void Sound(int soundId, int range) {
			if (soundId != -1) {
				PacketSequences.SendSound(this.TopObj(), soundId, range);
			}
		}

		/**
			Makes this thing say something to all clients who can hear it.
			
			@param arg The text to say
		 */
		public void Say(string arg) {
			if (!string.IsNullOrEmpty(arg)) {
				this.Speech(arg, 0, SpeechType.Speech, -1, ClientFont.Unified, null, null);
			}
		}

		/**
			Makes this thing say something to all clients who can hear it.
			
			@param arg The text to say
			@param color The color the text should be
		 */
		public void Say(string arg, int color) {
			if (!string.IsNullOrEmpty(arg)) {
				this.Speech(arg, 0, SpeechType.Speech, color, ClientFont.Unified, null, null);
			}
		}

		/**
			Makes this thing say something to all clients who can hear it.
			
			@param arg The text to say
			@param color The color the text should be
			@param font The font to use for the text (The default is 3)
		 */
		public void Say(string arg, int color, ClientFont font) {
			if (!string.IsNullOrEmpty(arg)) {
				this.Speech(arg, 0, SpeechType.Speech, color, font, null, null);
			}
		}

		/**
			Makes this thing say something to all clients who can hear it.
			
			@param arg The cliloc entry # to say
			@param args Additional args needed for the cliloc entry, if any.
		 */
		public void ClilocSay(int arg, params string[] args) {
			this.Speech(null, arg, SpeechType.Speech, -1, ClientFont.Unified, null, args);
		}

		/**
			Makes this thing say something to all clients who can hear it.
			
			@param arg The cliloc entry # to say
			@param color The color the text should be
			@param args Additional args needed for the cliloc entry, if any.
		 */
		public void ClilocSay(int arg, int color, params string[] args) {
			this.Speech(null, arg, SpeechType.Speech, color, ClientFont.Unified, null, args);
		}

		/**
			Makes this thing say something to all clients who can hear it.
			
			@param arg The cliloc entry # to say
			@param color The color the text should be
			@param font The font to use for the text (The default is 3)
			@param args Additional args needed for the cliloc entry, if any.
		 */
		public void ClilocSay(int arg, int color, ClientFont font, params string[] args) {
			this.Speech(null, arg, SpeechType.Speech, color, font, null, args);
		}

		/**
			Makes this thing yell something to all clients who can hear it.
			
			@param arg The text to yell
			@param color The color the text should be
		 */
		public void Yell(string arg, int color) {
			if (!string.IsNullOrEmpty(arg)) {
				this.Speech(arg, 0, SpeechType.Yell, color, ClientFont.Unified, null, null);
			}
		}

		/**
			Makes this thing yell something to all clients who can hear it.
			
			@param arg The cliloc entry # to yell
			@param color The color the text should be
			@param args Additional args needed for the cliloc entry, if any.
		 */
		public void ClilocYell(int arg, int color, params string[] args) {
			this.Speech(null, arg, SpeechType.Yell, color, ClientFont.Unified, null, args);
		}

		/**
			Makes this thing send a whisper to all clients who can hear it.
			
			@param arg The text to whisper
			@param color  The color the text should be
		 */
		public void Whisper(string arg, int color) {
			if (!string.IsNullOrEmpty(arg)) {
				this.Speech(arg, 0, SpeechType.Whisper, color, ClientFont.Unified, null, null);
			}
		}

		/**
			Makes this thing send a whisper to all clients who can hear it.
			
			@param arg The cliloc entry # to whisper
			@param color The color the text should be
			@param args Additional args needed for the cliloc entry, if any.
		 */
		public void ClilocWhisper(int arg, int color, params string[] args) {
			this.Speech(null, arg, SpeechType.Whisper, color, ClientFont.Unified, null, args);
		}

		/**
			Makes this thing send an emote to all clients in range.
			
			@param arg The text to emote
			@param color  The color the text should be
		 */
		public void Emote(string arg, int color) {
			if (!string.IsNullOrEmpty(arg)) {
				this.Speech(arg, 0, SpeechType.Emote, color, ClientFont.Unified, null, null);
			}
		}

		/**
			Makes this thing send an emote to all clients in range.
			
			@param arg The cliloc entry # to emote
			@param color The color the text should be
			@param args Additional args needed for the cliloc entry, if any.
		 */
		public void ClilocEmote(int arg, int color, params string[] args) {
			this.Speech(null, arg, SpeechType.Emote, color, ClientFont.Unified, null, args);
		}

		/**
			Makes this thing yell something to all clients who can hear it.
			
			@param arg The text to yell
		 */
		public void Yell(string arg) {
			this.Yell(arg, 0);
		}

		/**
			Makes this thing yell something to all clients who can hear it.
			
			@param arg The cliloc entry # to yell
			@param args Additional args needed for the cliloc entry, if any.
		 */
		public void ClilocYell(int arg, params string[] args) {
			this.ClilocYell(arg, 0, args);
		}

		/**
			Makes this thing send a whisper to all clients who can hear it.
			
			@param arg The text to whisper
		 */
		public void Whisper(string arg) {
			this.Whisper(arg, -1);
		}
		/**
			Makes this thing send a whisper to all clients who can hear it.
			
			@param arg The cliloc entry # to whisper
			@param args Additional args needed for the cliloc entry, if any.
		 */
		public void ClilocWhisper(int arg, params string[] args) {
			this.ClilocWhisper(arg, -1, args);
		}

		/**
			Makes this thing send an emote to all clients in range.
			
			@param arg The text to emote
		 */
		public void Emote(string arg) {
			this.Emote(arg, 0x22);
		}
		/**
			Makes this thing send an emote to all clients in range.
			
			@param arg The cliloc entry # to emote
			@param args Additional args needed for the cliloc entry, if any.
		 */
		public void ClilocEmote(int arg, params string[] args) {
			this.ClilocEmote(arg, 0x22, args);
		}

		//TODO needs work
		public void Fix() {
			int oldZ = this.point4d.z;
			int newZ = this.GetMap().GetFixedZ(this);
			if (oldZ != newZ) {
				this.Z = newZ;
			}
		}

		public virtual Season Season {
			get {
				return Season.Spring;
			}
		}

		public virtual CursorType Cursor {
			get {
				return CursorType.Gold;
			}
		}

		public void Move(string dir, int amount) {
			if (this.TopObj() != this) {
				throw new SEException("Can't use the Move command for something not on ground.");
			}

			int x = this.point4d.x;
			int y = this.point4d.y;

			switch (dir) {
				case ("n"):
					y -= amount;
					break;
				case ("ne"):
					y -= amount;
					x += amount;
					break;
				case ("e"):
					x += amount;
					break;
				case ("se"):
					y += amount;
					x += amount;
					break;
				case ("s"):
					y += amount;
					break;
				case ("sw"):
					y += amount;
					x -= amount;
					break;
				case ("w"):
					x -= amount;
					break;
				case ("nw"):
					y -= amount;
					x -= amount;
					break;
				default:
					throw new SEException(dir + " isn't a direction I'll accept. Use n, nw, ne, w, e, s, sw, or se.");

			}

			this.P(x, y, this.point4d.z);	//This won't change our Z coordinate, whereas P(x,y) would.
		}

		/*
			Method: Speech
				Does the work of sending speech (of any type) to everyone within range, calling
				Trigger_Hear on NPCs, etc.
				If the speaker is hidden, they will be unhidden, unless they are actually invisible.
				Speech distance is calculated in here based on the speech type, based on your
				INI settings for speechDistance, whisperDistance, emoteDistance, and yellDistance.
				
				At present, NPCs only hear characters, not items.
				
			Parameters:
				speech - What will be said
				type - The type of the speech. Valid values are Speech, Yell, Emote, and Whisper. If anything
					else is sent, the type is treated as Speech. (The other types are Name and Server, which
					shouldn't be spoken)
				font - The font to speak in. 3 is the default, and is used by everything that doesn't specify
					a font. If this is not 3, then the speech will be sent in ASCII instead of Unicode, because
					Unicode doesn't support special fonts like runic.
		 */
		public void Speech(string msg, int clilocMsg, SpeechType type, int color, ClientFont font, int[] keywords, string[] args) {
			AbstractCharacter self = this as AbstractCharacter;

			string language = "enu";
			if (self != null) {
				GameState state = self.GameState;
				if (state != null) {
					language = state.ClientLanguage;
				}
			}
			this.Speech(msg, clilocMsg, type, color, font, language, keywords, args);
		}

		public void Speech(string msg, int clilocMsg, SpeechType type, int color, ClientFont font, string language, int[] keywords, string[] args) {
			this.ThrowIfDeleted();

			AbstractCharacter speaker = this as AbstractCharacter;
			GameState speakerGameState = null;
			bool runSpeechTriggers = false;
			if (speaker != null) {
				speakerGameState = speaker.GameState;
				runSpeechTriggers = speaker.IsPlayer;
			}

			if (runSpeechTriggers) {
				if (TriggerResult.Cancel == speaker.Trigger_Say(msg, type, keywords)) {
					return;
				}
			}// else item/npc is speaking... no triggers fired

			int dist = 0;
			switch (type) {
				case SpeechType.Speech:
					dist = Globals.SpeechDistance;
					break;
				case SpeechType.Emote:
					dist = Globals.EmoteDistance;
					break;
				case SpeechType.Whisper:
					dist = Globals.WhisperDistance;
					break;
				case SpeechType.Yell:
					dist = Globals.YellDistance;
					break;
				default:
					dist = Globals.SpeechDistance;
					//type = SpeechType.Speech; //or should we? could this cause something bad? -tar
					break;
			}

			Map map = this.GetMap();
			IEnumerable<AbstractCharacter> chars;
			SpeechArgs sa;
			AbstractCharacter listenerToExclude = null;
			if (runSpeechTriggers) {
				chars = map.GetCharsInRange(this.point4d.x, this.point4d.y, dist);
				sa = new SpeechArgs(speaker, msg, clilocMsg, type, color, font, language, keywords, args);

				if (speakerGameState != null) {

					//zkusime jestli se jedna o osloveni jmenem
					if (!string.IsNullOrWhiteSpace(msg)) {
						foreach (AbstractCharacter listener in chars) {
							if (msg.StartsWith(listener.Name + " ")) {
								if (listener.CanSeeLOS(speaker).Allow) {
									listener.Trigger_Hear(sa);
									speakerGameState.LastExclusiveConversationPartner = listener;
									runSpeechTriggers = false;
								}
							}
						}
					}

					if (runSpeechTriggers) {
						//zkusime predchozi exkluzivne konverzujici npc (vendor apod)
						var partner = speakerGameState.LastExclusiveConversationPartner;
						if (partner != null) {
							if ((Point2D.GetSimpleDistance(speaker, partner) <= dist) && partner.CanSeeLOS(speaker).Allow) {
								var result = partner.Trigger_Hear(sa);
								if (result == SpeechResult.ActedUponExclusively) {
									runSpeechTriggers = false;
								} else {
									listenerToExclude = partner; //nebudeme na nem znova spoustet @hear
								}
							}
						}
					}
				}

			} else {
				chars = map.GetPlayersInRange(this.point4d.x, this.point4d.y, dist);
				sa = null;
			}

			PacketGroup pg = null;
			try {
				foreach (AbstractCharacter listener in chars) {
					if ((runSpeechTriggers) && (listener != speaker) && (listener != listenerToExclude) && listener.CanSeeLOS(speaker).Allow) {
						var result = listener.Trigger_Hear(sa);
						if (result == SpeechResult.ActedUponExclusively) {
							if (speakerGameState != null) {
								speakerGameState.LastExclusiveConversationPartner = listener;
							}
							runSpeechTriggers = false;
						}
					}

					var state = listener.GameState;
					if (state != null) {
						if (pg == null) {
							pg = PacketGroup.AcquireMultiUsePG();
							if (msg == null) {
								pg.AcquirePacket<ClilocMessageOutPacket>().Prepare(this, clilocMsg, this.Name, type, font, color,
									args == null ? null : string.Join("\t", args));
							} else {
								pg.AddPacket(PacketSequences.PrepareMessagePacket(
									this, msg, this.Name, type, font, color, language));
							}
						}

						state.Conn.SendPacketGroup(pg);
					}
				}
			} finally {
				if (pg != null) {
					pg.Dispose();
				}
			}
		}


		/*
		   Method: CheckForInvalidCoords
		       This function only exists in the debug build, and is not run in non-debug builds.

		       Checks the coords to see if they are outside the map. Since maps start at 0,0 and coordinates
		       are unsigned, X and Y are only checked against the max X and Y for the map, and not against 0,0.

		       This will definitely trip (Throw a SanityCheckException) if the item never got real coordinates
		       after BeingDroppedFromContainer was called, so this is called as a sanity check by methods which
		       call BeingDroppedFromContainer but rely on other methods to give it a real location
		       (after the other methods have been called).

		       This will not trip if the item is in a container (or equipped), since invalid coordinates ARE
		       used for equipped items (Specifically, X=7000).

		       This does not check Z.
		// */
		//[Conditional("DEBUG")]
		//internal void CheckForInvalidCoords() {
		//    bool throwExcep = false;
		//    if (!IsDeleted && IsOnGround) {
		//        if (X > Server.MaxX(M)) {
		//            throwExcep = true;
		//        } else if (Y > Server.MaxY(M)) {
		//            throwExcep = true;
		//        }
		//        if (throwExcep) {
		//            throw new SanityCheckException("Invalid coordinates detected: (" + X + "," + Y + "," + Z + "," + M + ")");
		//        }
		//    }
		//}
	}

	public class DenyClickArgs : DenyTriggerArgs {
		private readonly AbstractCharacter clickingChar;
		private readonly Thing target;


		public DenyClickArgs(AbstractCharacter clickingChar, Thing target)
			: base(DenyResultMessages.Allow, clickingChar, target) {

			this.clickingChar = clickingChar;
			this.target = target;
		}

		public AbstractCharacter ClickingChar {
			get {
				return this.clickingChar;
			}
		}

		public Thing Target {
			get {
				return this.target;
			}
		}
	}
}