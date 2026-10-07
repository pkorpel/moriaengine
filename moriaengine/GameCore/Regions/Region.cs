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
using System.Text.RegularExpressions;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine.Regions {

	public class Region : PluginHolder {
		public static Regex rectRE = new Regex(@"(?<x1>(0x)?\d+)\s*(,|/s+)\s*(?<y1>(0x)?\d+)\s*(,|/s+)\s*(?<x2>(0x)?\d+)\s*(,|/s+)\s*(?<y2>(0x)?\d+)",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		private string defname;
		private Point4D p = new Point4D(0, 0, 0, 0); //spawnpoint

		internal IList<RegionRectangle> rectangles = new List<RegionRectangle>();
		private Region parent;
		private byte mapplane; //protected, we will make use of it in StaticRegion loading part...
		private bool mapplaneIsSet;
		private int hierarchyIndex = -1;
		private TimeSpan createdAt = Globals.TimeAsSpan;


		internal bool inactivated;
		//this will be set to false when the region is going to be edited
		//after succesful editing (changing of P or changing rectnagles) it will be then reset to true
		//without this, the region won't be activated (Error will occur)
		internal bool canBeActivated = true;


		//private readonly static Type[] constructorTypes = new Type[] {typeof(string), typeof(string), typeof(int)};

		public Region Parent {
			get {
				return this.parent;
			}
			protected set {
				this.parent = value;
			}
		}

		public virtual string Defname {
			get {
				return this.defname;
			}
			protected set {
				this.defname = value;
			}
		}

		public TimeSpan CreatedAt {
			get {
				return this.createdAt;
			}
		}

		public IList<ImmutableRectangle> Rectangles {
			get {
				RegionRectangle[] arr = new RegionRectangle[this.rectangles.Count];
				this.rectangles.CopyTo(arr, 0);
				return arr;
			}
		}

		public bool IsWorldRegion {
			get {
				return (this == StaticRegion.WorldRegion);
			}
		}

		public int HierarchyIndex {
			get {
				return this.hierarchyIndex;
			}
			internal set { this.hierarchyIndex = value; }
		}

		public byte Mapplane {
			get {
				if (!this.mapplaneIsSet) {
					this.mapplane = this.P.M;
					this.mapplaneIsSet = true;
				}
				return this.mapplane;
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public virtual Point4D P {
			get {
				return this.p;
			}
			set {
				this.ThrowIfDeleted();
				if (!this.ContainsPoint(value)) {
					throw new SEException("Spawnpoint " + value + " is not contained in the region " + this);
				}
				this.p = value;
			}
		}

		internal void InternalSetP(Point4D value) {
			this.p = value;
		}

		public bool IsChildOf(Region tested) {
			this.ThrowIfDeleted();
			if (this.parent == null) {
				return false;
			}
			if (tested == this.parent) {
				return true;
			}
			return this.parent.IsChildOf(tested);
		}

		private static Region FindCommonParent(Region a, Region b)
		{
			if (b.IsChildOf(a)) {
				return a;
			}
			if (a.parent == b) {
				return b;
			}
			return FindCommonParent(a.parent, b);
		}

		public static bool TryExitAndEnter(Region oldRegion, Region newRegion, AbstractCharacter ch) {
			Region sharedParent = FindCommonParent(oldRegion, newRegion);
			while (oldRegion != sharedParent) {
				if (!oldRegion.TryExit(ch)) {//cancelled
					return false;
				}
				oldRegion = oldRegion.parent;
			}
			if (sharedParent != newRegion) {//otherwise we are not entering anything
				return TryEnterHierarchy(sharedParent, newRegion, ch);
			}
			return true;
		}

		private static bool TryEnterHierarchy(Region parent, Region child, AbstractCharacter ch) {//enters the parent first, and then all the children till the given child
			if (child.parent != parent) {
				if (!TryEnterHierarchy(parent, child.parent, ch)) {
					return false;
				}
			}
			return child.TryEnter(ch);
		}

		public static void ExitAndEnter(Region oldRegion, Region newRegion, AbstractCharacter ch) {//exit and enter all the regions in hierarchy
			Region sharedParent = FindCommonParent(oldRegion, newRegion);
			while (oldRegion != sharedParent) {
				oldRegion.Exit(ch);
				oldRegion = oldRegion.parent;
			}
			if (sharedParent != newRegion) {//otherwise we are not entering anything
				EnterHierarchy(sharedParent, newRegion, ch);
			}
		}

		private static void EnterHierarchy(Region parent, Region child, AbstractCharacter ch) {//enters the parent first, and then all the children till the given child
			if (child.parent != parent) {
				EnterHierarchy(parent, child.parent, ch);
			}
			child.Enter(ch);
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal static void Trigger_ItemEnter(ItemOnGroundArgs args) {
			Region region = args.Region;
			Point4D point = args.Point;
			AbstractItem item = args.ManipulatedItem;

			do {
				region.TryTrigger(TriggerKey.itemEnter, args);
				ReturnItemOnGroundIfNeeded(item, point);
				try {
					region.On_ItemEnter(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				ReturnItemOnGroundIfNeeded(item, point);

				region = region.parent;
			} while (region != null);
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal static void Trigger_ItemLeave(ItemOnGroundArgs args) {
			Region region = args.Region;
			Point4D point = args.Point;
			AbstractItem item = args.ManipulatedItem;

			do {
				region.TryTrigger(TriggerKey.itemLeave, args);
				ReturnItemOnGroundIfNeeded(item, point);
				try {
					region.On_ItemLeave(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				ReturnItemOnGroundIfNeeded(item, point);

				region = region.parent;
			} while (region != null);
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal void Trigger_DenyPickupItemFrom(DenyPickupArgs args) {
			Region region = this;

			do {
				if (TriggerResult.Cancel != region.TryCancellableTrigger(TriggerKey.denyPickupItemFrom, args)) {
					try {
						if (TriggerResult.Cancel != region.On_DenyPickupItemFrom(args)) {
							return;
						}
					} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				} else {
					return;
				}
				region = region.parent;
			} while (region != null);
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual TriggerResult On_DenyPickupItemFrom(DenyPickupArgs args) {
			this.ThrowIfDeleted();
			return TriggerResult.Continue;
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal TriggerResult Trigger_DenyPutItemOn(DenyPutOnGroundArgs args) {
			this.ThrowIfDeleted();
			Region region = this;

			do {
				if (TriggerResult.Cancel != region.TryCancellableTrigger(TriggerKey.denyPutItemOn, args)) {
					try {
						if (TriggerResult.Cancel != region.On_DenyPutItemOn(args)) {
							return TriggerResult.Cancel;
						}
					} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				} else {
					return TriggerResult.Cancel;
				}

				region = region.parent;
			} while (region != null);

			return TriggerResult.Continue;
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual TriggerResult On_DenyPutItemOn(DenyPutOnGroundArgs args) {
			this.ThrowIfDeleted();
			return TriggerResult.Continue;
		}

		private static void ReturnItemOnGroundIfNeeded(AbstractItem item, Point4D point) {
			if ((item.Cont != null) || (!point.Equals(item))) {
				Logger.WriteWarning(item + " has been moved in the implementation of one of the @LeaveGround triggers. Don't do this. Putting back.");
				item.MakeLimbo();
				item.Trigger_EnterRegion(point.X, point.Y, point.Z, point.M);
			}
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_ItemLeave(ItemOnGroundArgs args) {
			this.ThrowIfDeleted();
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_ItemEnter(ItemOnGroundArgs args) {
			this.ThrowIfDeleted();
		}

		public bool ContainsPoint(Point2D point) {
			foreach (ImmutableRectangle rect in this.Rectangles) {
				if (rect.Contains(point)) {
					return true;
				}
			}
			return false;
		}

		public bool ContainsPoint(int x, int y) {
			foreach (ImmutableRectangle rect in this.Rectangles) {
				if (rect.Contains(x, y)) {
					return true;
				}
			}
			return false;
		}

		public override string ToString() {
			return this.GetType().Name + " " + this.defname;
		}

		public string HierarchyName {
			get
			{
				if (this.parent == null) {
					return this.Name;
				}
				return this.Name + " in " + this.parent.HierarchyName;
			}
		}

		public bool TryEnter(AbstractCharacter ch) {
			this.ThrowIfDeleted();
			if (TriggerResult.Cancel != this.TryCancellableTrigger(TriggerKey.enter, new ScriptArgs(ch, 0))) {
				if (TriggerResult.Cancel != this.On_Enter(ch, false)) {
					return true;
				}
			}
			return false;
		}

		public bool TryExit(AbstractCharacter ch) {
			this.ThrowIfDeleted();
			if (TriggerResult.Cancel != this.TryCancellableTrigger(TriggerKey.exit, new ScriptArgs(ch, 0))) {
				if (TriggerResult.Cancel != this.On_Exit(ch, false)) {
					return true;
				}
			}
			return false;
		}

		public void Enter(AbstractCharacter ch) {
			this.ThrowIfDeleted();
			this.TryTrigger(TriggerKey.enter, new ScriptArgs(ch, 1));
			this.On_Enter(ch, true);
		}

		public void Exit(AbstractCharacter ch) {
			this.ThrowIfDeleted();
			this.TryTrigger(TriggerKey.exit, new ScriptArgs(ch, 1));
			this.On_Exit(ch, true);
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member"), SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public virtual TriggerResult On_Enter(AbstractCharacter ch, bool forced) {//if forced is true, the return value is irrelevant
			Logger.WriteDebug(ch + " entered " + this);
			ch.SysMessage("You have just entered " + this);
			return TriggerResult.Continue;
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member"), SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public virtual TriggerResult On_Exit(AbstractCharacter ch, bool forced) {
			Logger.WriteDebug(ch + " left " + this);
			ch.SysMessage("You have just left " + this);
			return TriggerResult.Continue;
		}

		public bool IsInactivated {
			get {
				return this.inactivated;
			}
		}

		public override void LoadLine(string filename, int line, string valueName, string valueString) {
			switch (valueName) {
				case "category":
				case "subsection":
				case "description":
					return;
				case "event":
				case "events":
				case "type":
				case "triggergroup":
				case "resources"://in sphere, resources are the same like events... is it gonna be that way too in SE?
					base.LoadLine(filename, line, "triggergroup", valueString);
					break;
				case "rect":
				case "rectangle": //RECT=2300,3612,3264,4096
					Match m = rectRE.Match(valueString);
					if (m.Success) {
						GroupCollection gc = m.Groups;
						ushort x1 = ConvertTools.ParseUInt16(gc["x1"].Value);
						ushort y1 = ConvertTools.ParseUInt16(gc["y1"].Value);
						ushort x2 = ConvertTools.ParseUInt16(gc["x2"].Value);
						ushort y2 = ConvertTools.ParseUInt16(gc["y2"].Value);
						//Point2D point1 = new Point2D(x1, y1);
						//Point2D point2 = new Point2D(x2, y2);
						RegionRectangle rr = new RegionRectangle(x1, y1, x2, y2, this);
						//RegionRectangle rr = new RegionRectangle(point1, point2, this);//throws sanityExcepton if the points are not the correct corners. Or should we check it here? as in RegionImporter?
						this.rectangles.Add(rr);
					} else {
						throw new SEException("Unrecognized Rectangle format ('" + valueString + "')");
					}
					break;
				case "p":
				case "spawnpoint":
					this.p = (Point4D) ObjectSaver.Load(valueString);
					break;
				case "mapplane":
					this.mapplane = ConvertTools.ParseByte(valueString);
					this.mapplaneIsSet = true;
					break;
				case "parent":
					ObjectSaver.Load(valueString, this.LoadParent_Delayed, filename, line);
					break;

				case "createdat":
					this.createdAt = (TimeSpan) ObjectSaver.OptimizedLoad_SimpleType(valueString, typeof(TimeSpan));
					break;
				default:
					base.LoadLine(filename, line, valueName, valueString);
					break;
			}
		}

		private void LoadParent_Delayed(object resolvedObject, string filename, int line) {
			Region reg = resolvedObject as Region;
			if (reg != null) {
				this.parent = reg;
			} else {
				Logger.WriteWarning(LogStr.FileLine(filename, line) + "'" + LogStr.Ident(resolvedObject) + "' is not a valid Region. Referenced as parent by '" + LogStr.Ident(this.Defname) + "'.");
			}
		}

		public override void Save(SaveStream output) {
			output.WriteValue("p", this.p);
			output.WriteValue("createdat", this.createdAt);
			if (this.mapplane != 0) {
				output.WriteValue("mapplane", this.mapplane);
			}
			if (this.parent != null) {
				output.WriteValue("parent", this.parent);
			}
			//RECT=2300,3612,3264,4096
			foreach (RegionRectangle rect in this.rectangles) {
				output.WriteLine("rect=" + rect.MinX + "," + rect.MinY + "," + rect.MaxX + "," + rect.MaxY);
			}

			base.Save(output);
		}
	}

	internal class RegionRectangle : ImmutableRectangle {
		internal static readonly RegionRectangle[] emptyArray = new RegionRectangle[0];

		internal readonly Region region;
		internal RegionRectangle(int minX, int minY, int maxX, int maxY, Region region)
			: base(minX, minY, maxX, maxY) {
			this.region = region;
		}

		/*internal RegionRectangle(Point2D start, Point2D end, Region region)
			: base(start, end) {
			this.region = region;
		}*/

		internal RegionRectangle(AbstractRectangle rect, Region region)
			: base(rect.MinX, rect.MinY, rect.MaxX, rect.MaxY) {
			this.region = region;
		}

		/// <summary>
		/// Returns a new (moved) instance, with coordinates altered by given x and y
		/// </summary>
		internal RegionRectangle CloneMoved(int xDiff, int yDiff) {
			return new RegionRectangle(this.MinX + xDiff, this.MinY + yDiff,
				this.MaxX + xDiff, this.MaxY + yDiff, this.region);
		}
	}
}
