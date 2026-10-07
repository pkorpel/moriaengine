//namespace SteamEngine.Scripts {
//	using System;
//	using SteamEngine;
//	
//	//If something looks difficult, make suggestions for ways to make it easier!
//	
//	public class TextColorTest : Script {
//		private TagKey timerTag=Tag("textColorTest_timer");
//		private TagKey colorTag=Tag("textColorTest_color");
//		
//		public TextColorTest() : base("e_text_color_test") {
//		}
//		
//		//runs when someone types .textColorTest
//		public void func_textColor_Test(TagHolder self) {
//			bool didntHave=self.AddTriggerGroup(this.triggerGroup);	//will return false if we already have it
//			if (didntHave) {
//				//start the timer
//				Globals.srcConn.WriteLine("Starting timer.");
//				Timer timer=self.AddTimer(timerTag, "text_color_test", 1, null);	//will return null if the timer is already on them
//				if (self.GetTag(colorTag)==null) {	//tag does not exist
//					self.SetTag(colorTag,0);
//				}
//			} else {
//				Globals.srcConn.WriteLine("Stopping timer.");
//				self.RemoveTimer(timerTag);
//				self.RemoveTriggerGroup(this.triggerGroup);
//			}
//		}
//		
//		public int on_text_color_test(TagHolder self) {
//			if (self is Thing) {
//				Thing st = (Thing) self;
//				ushort col=(ushort) self.GetTag(typeof(ushort),colorTag);
//				st.Say("(0x"+col.ToString("x")+") Color test",col);
//				if (col<0xffff) {
//					self.SetTag(colorTag,col+1);
//					return 2;
//				} else {
//					self.RemoveTimer(timerTag);
//					self.RemoveTag(colorTag);
//				}
//			}
//			return -1;	//stop this timer
//		}
//	}
//}