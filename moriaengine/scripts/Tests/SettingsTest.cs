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

using System.Collections;

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>Simple class for testing the info dialogs</summary>
	[ViewableClass("SimpleClass")]
	public class SimpleClass {
		private AbstractCharacter refChar;

		public string nullSS;
		public int notNullSS = 5;
		public string ROSS {
			get {
				return "getted Value";
			}
		}

		public ArrayList nejakejList = new ArrayList();

		public Hashtable nejakaTabulka = new Hashtable();

		public Hues notNullEnum = Hues.Blue;

		public LeafComponentTypes nullROEnum {
			get {
				return LeafComponentTypes.ButtonCross;
			}
		}

		public AbstractCharacter GetChar {
			get {
				return Globals.SrcCharacter;
			}
		}

		public AbstractCharacter GetSetChar {
			get {
				return this.refChar;
			}
			set {
				this.refChar = value;
			}
		}

		[NoShow]
		public int bar;
		[NoShow]
		public int baz;

		[Button("List doublify")]
		public void SomeMethod() {
			for (int i = 0; i < (this.bar == 0 ? 1 : this.bar) * 2; i++) {
				this.nejakejList.Add(this.nejakejList.Count);
			}
			this.bar = (this.bar == 0 ? 1 : this.bar) * 2;
		}

		[Button("Table doublify")]
		public void SomeOtherMethod() {
			for (int i = 0; i < (this.baz == 0 ? 1 : this.baz) * 2; i++) {
				this.nejakaTabulka.Add(this.nejakaTabulka.Count + ".)", this.nejakaTabulka.Count);
			}
			this.baz = (this.baz == 0 ? 1 : this.baz) * 2;
		}
	}

	/*/// <summary>Class returning pages for the dialog.</summary>
	public sealed class Prototype_GeneratedDataView_SimpleClass : ViewableClassDataView {
		protected override IEnumerable<ButtonDataFieldView> ActionButtonsPage(int firstLineIndex, int maxButtonsOnPage) {
			return new SimpleClassActionButtonsPage(firstLineIndex, maxButtonsOnPage);			
		}

		protected override IEnumerable<IDataFieldView> DataFieldsPage(int firstLineIndex, int maxLinesOnPage) {
			return new SimpleClassDataFieldsPage(firstLineIndex, maxLinesOnPage);
		}		

		public override int LineCount {
			get {
				return 2;
			}
		}

		public override string Name {
			get {
				return "SimpleClass";
			}
		}

		[Summary("This class will be automatically generated - the MoveNext method ensures the correct " +
			" iteration on all the data fields of the ViewableClass. It makes sure the upperBound " +
			"won't be reached")]
		public class SimpleClassDataFieldsPage : AbstractPage<IDataFieldView> {
			public SimpleClassDataFieldsPage(int firstLineIndex, int maxFieldsOnPage) : base(firstLineIndex, maxFieldsOnPage) {
			}
			
			public override bool MoveNext() {
				if(nextIndex < upperBound) {
					switch(nextIndex) {
						case 0:
							current = GeneratedReadWriteDataFieldView_SimpleClass_Foo.instance;
							break;						
						default:
							//this happens when there are not enough lines to fill the whole page
							//or if we are beginning with the index larger then the overall LinesCount 
							//(which results in the empty page and should not happen)
							return false;
					}
					++nextIndex;//prepare the index for the next round of iteration
					return true;
				} else {
					return false;
				}
			}
		}

		/// <summary>And this one ensures correct iteration over the action buttons of the ViewableClass</summary>
		public class SimpleClassActionButtonsPage : AbstractPage<ButtonDataFieldView> {
			public SimpleClassActionButtonsPage(int firstLineIndex, int maxButtonsOnPage) : base(firstLineIndex, maxButtonsOnPage) {
			}

			public override bool MoveNext() {				
				if(nextIndex < upperBound) {
					switch(nextIndex) {						
						case 0:
							current = GeneratedButtonDataFieldView_SimpleClass_SomeMethod.instance;
							break;
						default:
							//this happens when there are not enough lines to fill the whole page
							//or if we are beginning with the index larger then the overall LinesCount 
							//(which results in the empty page and should not happen)
							return false;
					}
					++nextIndex;//prepare the index for the next round of iteration
					return true;
				} else {
					return false;
				}
			}
		}

		/// <summary>Dataview implementation for the member 'foo' of the SimpleClass</summary>
		public class GeneratedReadWriteDataFieldView_SimpleClass_Foo : ReadWriteDataFieldView {
			public static GeneratedReadWriteDataFieldView_SimpleClass_Foo instance = new GeneratedReadWriteDataFieldView_SimpleClass_Foo();

			public override string Name {
				get {
					return "foo";
				}
			}

			public override object GetValue(object target) {
				return ((SimpleClass)target).foo;
			}

			public override string GetStringValue(object target) {
				return ObjectSaver.Save(((SimpleClass)target).foo);
			}

			public override void SetValue(object target, object value) {
				((SimpleClass)target).foo = (string)value;			
			}

			public override void SetStringValue(object target, string value) {
				((SimpleClass)target).foo = (string)ObjectSaver.OptimizedLoad_String(value);			
			}		
		}

		/// <summary>Dataview implementation for the method 'SomeMethod' of the SimpleClass</summary>
		public class GeneratedButtonDataFieldView_SimpleClass_SomeMethod : ButtonDataFieldView {
			public static GeneratedButtonDataFieldView_SimpleClass_SomeMethod instance = new GeneratedButtonDataFieldView_SimpleClass_SomeMethod();

			public override string Name {
				get {
					return "Test Button";
				}
			}

			public override void OnButton(object target) {
				((SimpleClass)target).SomeMethod();
			}		
		}
	  
		public override Type HandledType {
			get {
				return typeof(SimpleClass);
			}
		}
	}	
	*/
}