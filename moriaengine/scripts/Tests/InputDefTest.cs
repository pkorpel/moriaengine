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
//using SteamEngine.Common;

//namespace SteamEngine.CompiledScripts.Dialogs {

//    /// <summary>
//    /// Priklad C# skriptovane verze input dialogu. Jedine co je potreba udelat je overridnout metodu Response 
//    /// K dispozici jsou ve fieldu InputParams vstupni parametry dialoguse kterymi lze pracovat pri responsu
//    /// </summary>
//    public class InputTestCompiled : CompiledInputDef {
//        public override string Label {
//            get {
//                return "Testovaci input dialog";
//            }
//        }

//        public override string DefaultInput {
//            get {
//                return "defaultni hodnota";
//            }
//        }

//        public override void Response(Character src, Thing focus, string filledText) {
//            src.SysMessage("Vlozeno: " + filledText);
//            if (InputParams.Argv.Length > 0) {
//                string nextParams = "";
//                foreach (object obj in InputParams.Argv) {
//                    nextParams += obj.ToString();
//                }
//                src.SysMessage("Vstupni parametry: " + nextParams);
//            }
//        }
//    }
//}