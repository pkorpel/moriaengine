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
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	public sealed class ArraySaver : ISaveImplementor, IDeepCopyImplementor {
		public string HeaderName {
			get {
				return "Array";
			}
		}

		public Type HandledType {
			get {
				return typeof(Array);
			}
		}

		public void Save(object objToSave, SaveStream writer) {
			Array arr = (Array) objToSave;
			Type arrType = arr.GetType();
			if (arrType.GetArrayRank() > 1) {
				throw new SEException("Multi-dimensional array saving not implemented.");
			}
			Type elemType = arrType.GetElementType();
			writer.WriteLine("type=" + GenericListSaver.GetTypeName(elemType));

			int length = arr.Length;
			writer.WriteValue("length", length);
			for (int i = 0; i < length; i++) {
				writer.WriteValue(i.ToString(), arr.GetValue(i));
			}
		}

		public object LoadSection(PropsSection input) {
			int currentLineNumber = input.HeaderLine;
			try {
				PropsLine pl = input.PopPropsLine("type");
				currentLineNumber = pl.Line;
				Type elemType = GenericListSaver.ParseType(pl);

				PropsLine lengthLine = input.PopPropsLine("length");
				currentLineNumber = lengthLine.Line;
				int length = ConvertTools.ParseInt32(lengthLine.Value);

				Array arr = Array.CreateInstance(elemType, length);

				for (int i = 0; i < length; i++) {
					PropsLine valueLine = input.PopPropsLine(i.ToString());
					currentLineNumber = valueLine.Line;
					ArrayLoadHelper alip = new ArrayLoadHelper(arr, i, elemType);
					ObjectSaver.Load(valueLine.Value, this.DelayedLoad_Index, input.Filename, valueLine.Line, alip);
				}
				return arr;
			} catch (FatalException) {
				throw;
			} catch (SEException sex) {
				sex.TryAddFileLineInfo(input.Filename, currentLineNumber);
				throw;
			} catch (Exception e) {
				throw new SEException(input.Filename, currentLineNumber, e);
			}
		}

		public void DelayedLoad_Index(object loadedObj, string filename, int line, object param) {
			ArrayLoadHelper alip = (ArrayLoadHelper) param;
			alip.arr.SetValue(ConvertTools.ConvertTo(alip.elemType, loadedObj), alip.index);
		}

		public void DelayedCopy_Index(object loadedObj, object param) {
			ArrayLoadHelper alip = (ArrayLoadHelper) param;
			alip.arr.SetValue(ConvertTools.ConvertTo(alip.elemType, loadedObj), alip.index);
		}

		private class ArrayLoadHelper {
			internal Array arr;
			internal int index;
			internal Type elemType;

			internal ArrayLoadHelper(Array arr, int index, Type elemType) {
				this.index = index;
				this.arr = arr;
				this.elemType = elemType;
			}
		}

		public object DeepCopy(object copyFrom) {
			Array copyFromArray = (Array) copyFrom;
			int n = copyFromArray.Length;
			Type elemType = copyFromArray.GetType().GetElementType();
			Array newArray = Array.CreateInstance(elemType, n);
			for (int i = 0; i < n; i++) {
				ArrayLoadHelper aip = new ArrayLoadHelper(newArray, i, elemType);
				DeepCopyFactory.GetCopyDelayed(copyFromArray.GetValue(i), this.DelayedCopy_Index, aip);
			}
			return newArray;
		}
	}
}