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
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	public sealed class ArrayListSaver : ISaveImplementor, IDeepCopyImplementor {
		public string HeaderName {
			get {
				return "ArrayList";
			}
		}

		public Type HandledType {
			get {
				return typeof(ArrayList);
			}
		}

		public void Save(object objToSave, SaveStream writer) {
			ArrayList list = (ArrayList) objToSave;
			int count = list.Count;
			writer.WriteValue("count", count);
			for (int i = 0; i < count; i++) {
				writer.WriteValue(i.ToString(), list[i]);
			}
		}

		public object LoadSection(PropsSection input) {
			int currentLineNumber = input.HeaderLine;
			try {
				PropsLine countLine = input.PopPropsLine("count");
				currentLineNumber = countLine.Line;
				int count = int.Parse(countLine.Value);
				ArrayList list = new ArrayList(count);
				for (int i = 0; i < count; i++) {
					list.Add(null);
					PropsLine valueLine = input.PopPropsLine(i.ToString());
					currentLineNumber = valueLine.Line;
					ArrayListIndexPair alip = new ArrayListIndexPair(list, i);
					ObjectSaver.Load(valueLine.Value, this.DelayedLoad_Index, input.Filename, valueLine.Line, alip);
				}
				return list;
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
			ArrayListIndexPair alip = (ArrayListIndexPair) param;
			alip.list[alip.index] = loadedObj;
		}

		public void DelayedCopy_Index(object loadedObj, object param) {
			ArrayListIndexPair alip = (ArrayListIndexPair) param;
			alip.list[alip.index] = loadedObj;
		}

		private class ArrayListIndexPair {
			internal ArrayList list;
			internal int index;
			internal ArrayListIndexPair(ArrayList list, int index) {
				this.index = index;
				this.list = list;
			}
		}

		public object DeepCopy(object copyFrom) {
			ArrayList copyFromList = (ArrayList) copyFrom;
			int n = copyFromList.Count;
			ArrayList newList = new ArrayList(n);
			for (int i = 0; i < n; i++) {
				newList.Add(null);
				ArrayListIndexPair alip = new ArrayListIndexPair(newList, i);
				DeepCopyFactory.GetCopyDelayed(copyFromList[i], this.DelayedCopy_Index, alip);
			}
			return newList;
		}
	}
}