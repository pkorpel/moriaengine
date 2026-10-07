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

#include <stdio.h>
#include <windows.h>
#include "fastdll.hpp"

typedef void (__stdcall *OnDoneCompressing)(int outstart, int outlen, int inAvail);


unsigned char * inBuffer;
unsigned char * outBuffer;
void EXPORT InitBuffers(unsigned char *unpacked, unsigned char *retval) {
	inBuffer=unpacked;
	outBuffer=retval;
}

//If we want to get into one of these with a debugger, we add '__asm int 3' (omit quotes) as the first line to
//the function we want to debug. We then load SE and attach a debugger, and then we do whatever we need to
//trigger the function (normally, running the TestCompression command). The debugger will stop on that asm command,
//which is really a breakpoint instruction. Handy, eh? Of course, if you DON'T have a debugger attached, then
//SE will crash, heh heh. Which is one reason you don't see breakpoint instructions anywhere in
//here - I'm done debugging this and want SE to actually run these! :P

int EXPORT Crushify1(unsigned char *unpacked, unsigned char *retval, int retstart, int len) {
	#define _CRUSHIFY
	#define _ONE
	#include "fastdll.dupe"
	#undef _ONE
	#undef _CRUSHIFY
}

int EXPORT Crushify2(unsigned char *unpacked, unsigned char *retval, int retstart, int len) {
	#define _CRUSHIFY
	#define _TWO
	#include "fastdll.dupe"
	#undef _TWO
	#undef _CRUSHIFY
}

int EXPORT Crushify3(unsigned char *unpacked, unsigned char *retval, int retstart, int len) {
	#define _CRUSHIFY
	#define _THREE
	#include "fastdll.dupe"
	#undef _THREE
	#undef _CRUSHIFY
}

int EXPORT Crushify4(unsigned char *unpacked, unsigned char *retval, int retstart, int len) {
	#define _CRUSHIFY
	#define _THREE
	#define _FOUR
	#include "fastdll.dupe"
	#undef _FOUR
	#undef _THREE
	#undef _CRUSHIFY
}

int EXPORT Crushify5(unsigned char *unpacked, unsigned char *retval, int retstart, int len) {
	#define _CRUSHIFY
	#define _THREE
	#define _FOUR
	#define _FIVE
	#include "fastdll.dupe"
	#undef _FIVE
	#undef _FOUR
	#undef _THREE
	#undef _CRUSHIFY
}


int EXPORT Crushify6(unsigned char *unpacked, unsigned char *retval, int retstart, int len) {
	#define _CRUSHIFY
	#define _THREE
	#define _FOUR
	#define _SIX
	#include "fastdll.dupe"
	#undef _SIX
	#undef _FOUR
	#undef _THREE
	#undef _CRUSHIFY
}

int EXPORT Crushify7(unsigned char *unpacked, unsigned char *retval, int retstart, int len) {
	#define _CRUSHIFY
	#define _THREE
	#define _FOUR
	#define _SEVEN
	#include "fastdll.dupe"
	#undef _SEVEN
	#undef _FOUR
	#undef _THREE
	#undef _CRUSHIFY
}

int EXPORT Crushify8(int retstart, int len) {
	#define _CRUSHIFY
	#define _THREE
	#define _FOUR
	#define unpacked inBuffer
	#define retval outBuffer
	#include "fastdll.dupe"
	#undef unpacked
	#undef retval
	#undef _FOUR
	#undef _THREE
	#undef _CRUSHIFY
}

int EXPORT Crushify8S(int instart, int retstart, int len) {
	#define _CRUSHIFY
	//#define _THREE
	#define _FOUR
	#define _NINE
	#define unpacked inBuffer
	#define retval outBuffer
	#include "fastdll.dupe"
	#undef unpacked
	#undef retval
	#undef _NINE
	#undef _FOUR
	//#undef _THREE
	#undef _CRUSHIFY
}

#pragma warning (push)
#pragma warning (disable:4731)
#pragma warning (disable:4102)
int EXPORT Crushify10(int retstart, int len) {
	#define _CRUSHIFY
	#define _THREE
	#define _FOUR
	#define _TEN
	#define unpacked inBuffer
	#define retval outBuffer
	#include "fastdll.dupe"
	#undef unpacked
	#undef retval
	#undef _TEN
	#undef _FOUR
	#undef _THREE
	#undef _CRUSHIFY
}

int EXPORT Crushify10no3(int retstart, int len) {
	#define _CRUSHIFY
	//#define _THREE
	#define _FOUR
	#define _TEN
	#define unpacked inBuffer
	#define retval outBuffer
	#include "fastdll.dupe"
	#undef unpacked
	#undef retval
	#undef _TEN
	#undef _FOUR
	//#undef _THREE
	#undef _CRUSHIFY
}
int EXPORT Crushify11(int retstart, int len) {
	#define _CRUSHIFY
	#define _THREE
	#define _FOUR
	#define _TEN
	#define _ELEVEN
	#define unpacked inBuffer
	#define retval outBuffer
	#include "fastdll.dupe"
	#undef unpacked
	#undef retval
	#undef _TEN
	#undef _ELEVEN
	#undef _FOUR
	#undef _THREE
	#undef _CRUSHIFY
}
int EXPORT Crushify12(int retstart, int len) {
	#define _CRUSHIFY
	//#define _THREE
	#define _FOUR
	#define _TEN
	//TWELVE actually requires THIRTEEN in order to work.
	#define _TWELVE
	#define _THIRTEEN
	#define unpacked inBuffer
	#define retval outBuffer
	#include "fastdll.dupe"
	#undef unpacked
	#undef retval
	#undef _THIRTEEN
	#undef _TWELVE
	#undef _TEN
	#undef _FOUR
	//#undef _THREE
	#undef _CRUSHIFY
}
int EXPORT Crushify13(int retstart, int len) {
	#define _CRUSHIFY
	//#define _THREE
	#define _FOUR
	#define _TEN
	//#define _TWELVE
	#define _THIRTEEN
	#define unpacked inBuffer
	#define retval outBuffer
	#include "fastdll.dupe"
	#undef unpacked
	#undef retval
	#undef _THIRTEEN
	//#undef _TWELVE
	#undef _TEN
	#undef _FOUR
	//#undef _THREE
	#undef _CRUSHIFY
}
int EXPORT Crushify14(int retstart, int len) {
	//__asm int 3
	#define _CRUSHIFY
	//#define _THREE
	#define _FOUR
	#define _TEN
	////#define _TWELVE
	#define _THIRTEEN
	#define _FOURTEEN
	#define unpacked inBuffer
	#define retval outBuffer
	#include "fastdll.dupe"
	#undef unpacked
	#undef retval
	#undef _FOURTEEN
	#undef _THIRTEEN
	//#undef _TWELVE
	#undef _TEN
	#undef _FOUR
	//#undef _THREE
	#undef _CRUSHIFY
}

int EXPORT Crushify13no10(int retstart, int len) {
	#define _CRUSHIFY
	//#define _THREE
	#define _FOUR
	//#define _TEN
	//#define _TWELVE
	#define _THIRTEEN
	#define unpacked inBuffer
	#define retval outBuffer
	#include "fastdll.dupe"
	#undef unpacked
	#undef retval
	#undef _THIRTEEN
	//#undef _TWELVE
	//#undef _TEN
	#undef _FOUR
	//#undef _THREE
	#undef _CRUSHIFY
}

#pragma warning ( pop )

void SetupTables() {
	bitTable = new int* [257];
	for (int a=0; a<257; a++) {
		bitTable[a]=new int[2];
	}
	bitTable[0][0] = 0x2;
	bitTable[0][1] = 0x0;
	bitTable[1][0] = 0x5;
	bitTable[1][1] = 0x1f;
	bitTable[2][0] = 0x6;
	bitTable[2][1] = 0x22;
	bitTable[3][0] = 0x7;
	bitTable[3][1] = 0x34;
	bitTable[4][0] = 0x7;
	bitTable[4][1] = 0x75;
	bitTable[5][0] = 0x6;
	bitTable[5][1] = 0x28;
	bitTable[6][0] = 0x6;
	bitTable[6][1] = 0x3b;
	bitTable[7][0] = 0x7;
	bitTable[7][1] = 0x32;
	bitTable[8][0] = 0x8;
	bitTable[8][1] = 0xe0;
	bitTable[9][0] = 0x8;
	bitTable[9][1] = 0x62;
	bitTable[10][0] = 0x7;
	bitTable[10][1] = 0x56;
	bitTable[11][0] = 0x8;
	bitTable[11][1] = 0x79;
	bitTable[12][0] = 0x9;
	bitTable[12][1] = 0x19d;
	bitTable[13][0] = 0x8;
	bitTable[13][1] = 0x97;
	bitTable[14][0] = 0x6;
	bitTable[14][1] = 0x2a;
	bitTable[15][0] = 0x7;
	bitTable[15][1] = 0x57;
	bitTable[16][0] = 0x8;
	bitTable[16][1] = 0x71;
	bitTable[17][0] = 0x8;
	bitTable[17][1] = 0x5b;
	bitTable[18][0] = 0x9;
	bitTable[18][1] = 0x1cc;
	bitTable[19][0] = 0x8;
	bitTable[19][1] = 0xa7;
	bitTable[20][0] = 0x7;
	bitTable[20][1] = 0x25;
	bitTable[21][0] = 0x7;
	bitTable[21][1] = 0x4f;
	bitTable[22][0] = 0x8;
	bitTable[22][1] = 0x66;
	bitTable[23][0] = 0x8;
	bitTable[23][1] = 0x7d;
	bitTable[24][0] = 0x9;
	bitTable[24][1] = 0x191;
	bitTable[25][0] = 0x9;
	bitTable[25][1] = 0x1ce;
	bitTable[26][0] = 0x7;
	bitTable[26][1] = 0x3f;
	bitTable[27][0] = 0x9;
	bitTable[27][1] = 0x90;
	bitTable[28][0] = 0x8;
	bitTable[28][1] = 0x59;
	bitTable[29][0] = 0x8;
	bitTable[29][1] = 0x7b;
	bitTable[30][0] = 0x8;
	bitTable[30][1] = 0x91;
	bitTable[31][0] = 0x8;
	bitTable[31][1] = 0xc6;
	bitTable[32][0] = 0x6;
	bitTable[32][1] = 0x2d;
	bitTable[33][0] = 0x9;
	bitTable[33][1] = 0x186;
	bitTable[34][0] = 0x8;
	bitTable[34][1] = 0x6f;
	bitTable[35][0] = 0x9;
	bitTable[35][1] = 0x93;
	bitTable[36][0] = 0xa;
	bitTable[36][1] = 0x1cc;
	bitTable[37][0] = 0x8;
	bitTable[37][1] = 0x5a;
	bitTable[38][0] = 0xa;
	bitTable[38][1] = 0x1ae;
	bitTable[39][0] = 0xa;
	bitTable[39][1] = 0x1c0;
	bitTable[40][0] = 0x9;
	bitTable[40][1] = 0x148;
	bitTable[41][0] = 0x9;
	bitTable[41][1] = 0x14a;
	bitTable[42][0] = 0x9;
	bitTable[42][1] = 0x82;
	bitTable[43][0] = 0xa;
	bitTable[43][1] = 0x19f;
	bitTable[44][0] = 0x9;
	bitTable[44][1] = 0x171;
	bitTable[45][0] = 0x9;
	bitTable[45][1] = 0x120;
	bitTable[46][0] = 0x9;
	bitTable[46][1] = 0xe7;
	bitTable[47][0] = 0xa;
	bitTable[47][1] = 0x1f3;
	bitTable[48][0] = 0x9;
	bitTable[48][1] = 0x14b;
	bitTable[49][0] = 0x9;
	bitTable[49][1] = 0x100;
	bitTable[50][0] = 0x9;
	bitTable[50][1] = 0x190;
	bitTable[51][0] = 0x6;
	bitTable[51][1] = 0x13;
	bitTable[52][0] = 0x9;
	bitTable[52][1] = 0x161;
	bitTable[53][0] = 0x9;
	bitTable[53][1] = 0x125;
	bitTable[54][0] = 0x9;
	bitTable[54][1] = 0x133;
	bitTable[55][0] = 0x9;
	bitTable[55][1] = 0x195;
	bitTable[56][0] = 0x9;
	bitTable[56][1] = 0x173;
	bitTable[57][0] = 0x9;
	bitTable[57][1] = 0x1ca;
	bitTable[58][0] = 0x9;
	bitTable[58][1] = 0x86;
	bitTable[59][0] = 0x9;
	bitTable[59][1] = 0x1e9;
	bitTable[60][0] = 0x9;
	bitTable[60][1] = 0xdb;
	bitTable[61][0] = 0x9;
	bitTable[61][1] = 0x1ec;
	bitTable[62][0] = 0x9;
	bitTable[62][1] = 0x8b;
	bitTable[63][0] = 0x9;
	bitTable[63][1] = 0x85;
	bitTable[64][0] = 0x5;
	bitTable[64][1] = 0xa;
	bitTable[65][0] = 0x8;
	bitTable[65][1] = 0x96;
	bitTable[66][0] = 0x8;
	bitTable[66][1] = 0x9c;
	bitTable[67][0] = 0x9;
	bitTable[67][1] = 0x1c3;
	bitTable[68][0] = 0x9;
	bitTable[68][1] = 0x19c;
	bitTable[69][0] = 0x9;
	bitTable[69][1] = 0x8f;
	bitTable[70][0] = 0x9;
	bitTable[70][1] = 0x18f;
	bitTable[71][0] = 0x9;
	bitTable[71][1] = 0x91;
	bitTable[72][0] = 0x9;
	bitTable[72][1] = 0x87;
	bitTable[73][0] = 0x9;
	bitTable[73][1] = 0xc6;
	bitTable[74][0] = 0x9;
	bitTable[74][1] = 0x177;
	bitTable[75][0] = 0x9;
	bitTable[75][1] = 0x89;
	bitTable[76][0] = 0x9;
	bitTable[76][1] = 0xd6;
	bitTable[77][0] = 0x9;
	bitTable[77][1] = 0x8c;
	bitTable[78][0] = 0x9;
	bitTable[78][1] = 0x1ee;
	bitTable[79][0] = 0x9;
	bitTable[79][1] = 0x1eb;
	bitTable[80][0] = 0x9;
	bitTable[80][1] = 0x84;
	bitTable[81][0] = 0x9;
	bitTable[81][1] = 0x164;
	bitTable[82][0] = 0x9;
	bitTable[82][1] = 0x175;
	bitTable[83][0] = 0x9;
	bitTable[83][1] = 0x1cd;
	bitTable[84][0] = 0x8;
	bitTable[84][1] = 0x5e;
	bitTable[85][0] = 0x9;
	bitTable[85][1] = 0x88;
	bitTable[86][0] = 0x9;
	bitTable[86][1] = 0x12b;
	bitTable[87][0] = 0x9;
	bitTable[87][1] = 0x172;
	bitTable[88][0] = 0x9;
	bitTable[88][1] = 0x10a;
	bitTable[89][0] = 0x9;
	bitTable[89][1] = 0x8d;
	bitTable[90][0] = 0x9;
	bitTable[90][1] = 0x13a;
	bitTable[91][0] = 0x9;
	bitTable[91][1] = 0x11c;
	bitTable[92][0] = 0xa;
	bitTable[92][1] = 0x1e1;
	bitTable[93][0] = 0xa;
	bitTable[93][1] = 0x1e0;
	bitTable[94][0] = 0x9;
	bitTable[94][1] = 0x187;
	bitTable[95][0] = 0xa;
	bitTable[95][1] = 0x1dc;
	bitTable[96][0] = 0xa;
	bitTable[96][1] = 0x1df;
	bitTable[97][0] = 0x7;
	bitTable[97][1] = 0x74;
	bitTable[98][0] = 0x9;
	bitTable[98][1] = 0x19f;
	bitTable[99][0] = 0x8;
	bitTable[99][1] = 0x8d;
	bitTable[100][0] = 0x8;
	bitTable[100][1] = 0xe4;
	bitTable[101][0] = 0x7;
	bitTable[101][1] = 0x79;
	bitTable[102][0] = 0x9;
	bitTable[102][1] = 0xea;
	bitTable[103][0] = 0x9;
	bitTable[103][1] = 0xe1;
	bitTable[104][0] = 0x8;
	bitTable[104][1] = 0x40;
	bitTable[105][0] = 0x7;
	bitTable[105][1] = 0x41;
	bitTable[106][0] = 0x9;
	bitTable[106][1] = 0x10b;
	bitTable[107][0] = 0x9;
	bitTable[107][1] = 0xb0;
	bitTable[108][0] = 0x8;
	bitTable[108][1] = 0x6a;
	bitTable[109][0] = 0x8;
	bitTable[109][1] = 0xc1;
	bitTable[110][0] = 0x7;
	bitTable[110][1] = 0x71;
	bitTable[111][0] = 0x7;
	bitTable[111][1] = 0x78;
	bitTable[112][0] = 0x8;
	bitTable[112][1] = 0xb1;
	bitTable[113][0] = 0x9;
	bitTable[113][1] = 0x14c;
	bitTable[114][0] = 0x7;
	bitTable[114][1] = 0x43;
	bitTable[115][0] = 0x8;
	bitTable[115][1] = 0x76;
	bitTable[116][0] = 0x7;
	bitTable[116][1] = 0x66;
	bitTable[117][0] = 0x7;
	bitTable[117][1] = 0x4d;
	bitTable[118][0] = 0x9;
	bitTable[118][1] = 0x8a;
	bitTable[119][0] = 0x6;
	bitTable[119][1] = 0x2f;
	bitTable[120][0] = 0x8;
	bitTable[120][1] = 0xc9;
	bitTable[121][0] = 0x9;
	bitTable[121][1] = 0xce;
	bitTable[122][0] = 0x9;
	bitTable[122][1] = 0x149;
	bitTable[123][0] = 0x9;
	bitTable[123][1] = 0x160;
	bitTable[124][0] = 0xa;
	bitTable[124][1] = 0x1ba;
	bitTable[125][0] = 0xa;
	bitTable[125][1] = 0x19e;
	bitTable[126][0] = 0xa;
	bitTable[126][1] = 0x39f;
	bitTable[127][0] = 0x9;
	bitTable[127][1] = 0xe5;
	bitTable[128][0] = 0x9;
	bitTable[128][1] = 0x194;
	bitTable[129][0] = 0x9;
	bitTable[129][1] = 0x184;
	bitTable[130][0] = 0x9;
	bitTable[130][1] = 0x126;
	bitTable[131][0] = 0x7;
	bitTable[131][1] = 0x30;
	bitTable[132][0] = 0x8;
	bitTable[132][1] = 0x6c;
	bitTable[133][0] = 0x9;
	bitTable[133][1] = 0x121;
	bitTable[134][0] = 0x9;
	bitTable[134][1] = 0x1e8;
	bitTable[135][0] = 0xa;
	bitTable[135][1] = 0x1c1;
	bitTable[136][0] = 0xa;
	bitTable[136][1] = 0x11d;
	bitTable[137][0] = 0xa;
	bitTable[137][1] = 0x163;
	bitTable[138][0] = 0xa;
	bitTable[138][1] = 0x385;
	bitTable[139][0] = 0xa;
	bitTable[139][1] = 0x3db;
	bitTable[140][0] = 0xa;
	bitTable[140][1] = 0x17d;
	bitTable[141][0] = 0xa;
	bitTable[141][1] = 0x106;
	bitTable[142][0] = 0xa;
	bitTable[142][1] = 0x397;
	bitTable[143][0] = 0xa;
	bitTable[143][1] = 0x24e;
	bitTable[144][0] = 0x7;
	bitTable[144][1] = 0x2e;
	bitTable[145][0] = 0x8;
	bitTable[145][1] = 0x98;
	bitTable[146][0] = 0xa;
	bitTable[146][1] = 0x33c;
	bitTable[147][0] = 0xa;
	bitTable[147][1] = 0x32e;
	bitTable[148][0] = 0xa;
	bitTable[148][1] = 0x1e9;
	bitTable[149][0] = 0x9;
	bitTable[149][1] = 0xbf;
	bitTable[150][0] = 0xa;
	bitTable[150][1] = 0x3df;
	bitTable[151][0] = 0xa;
	bitTable[151][1] = 0x1dd;
	bitTable[152][0] = 0xa;
	bitTable[152][1] = 0x32d;
	bitTable[153][0] = 0xa;
	bitTable[153][1] = 0x2ed;
	bitTable[154][0] = 0xa;
	bitTable[154][1] = 0x30b;
	bitTable[155][0] = 0xa;
	bitTable[155][1] = 0x107;
	bitTable[156][0] = 0xa;
	bitTable[156][1] = 0x2e8;
	bitTable[157][0] = 0xa;
	bitTable[157][1] = 0x3de;
	bitTable[158][0] = 0xa;
	bitTable[158][1] = 0x125;
	bitTable[159][0] = 0xa;
	bitTable[159][1] = 0x1e8;
	bitTable[160][0] = 0x9;
	bitTable[160][1] = 0xe9;
	bitTable[161][0] = 0xa;
	bitTable[161][1] = 0x1cd;
	bitTable[162][0] = 0xa;
	bitTable[162][1] = 0x1b5;
	bitTable[163][0] = 0x9;
	bitTable[163][1] = 0x165;
	bitTable[164][0] = 0xa;
	bitTable[164][1] = 0x232;
	bitTable[165][0] = 0xa;
	bitTable[165][1] = 0x2e1;
	bitTable[166][0] = 0xb;
	bitTable[166][1] = 0x3ae;
	bitTable[167][0] = 0xb;
	bitTable[167][1] = 0x3c6;
	bitTable[168][0] = 0xb;
	bitTable[168][1] = 0x3e2;
	bitTable[169][0] = 0xa;
	bitTable[169][1] = 0x205;
	bitTable[170][0] = 0xa;
	bitTable[170][1] = 0x29a;
	bitTable[171][0] = 0xa;
	bitTable[171][1] = 0x248;
	bitTable[172][0] = 0xa;
	bitTable[172][1] = 0x2cd;
	bitTable[173][0] = 0xa;
	bitTable[173][1] = 0x23b;
	bitTable[174][0] = 0xb;
	bitTable[174][1] = 0x3c5;
	bitTable[175][0] = 0xa;
	bitTable[175][1] = 0x251;
	bitTable[176][0] = 0xa;
	bitTable[176][1] = 0x2e9;
	bitTable[177][0] = 0xa;
	bitTable[177][1] = 0x252;
	bitTable[178][0] = 0x9;
	bitTable[178][1] = 0x1ea;
	bitTable[179][0] = 0xb;
	bitTable[179][1] = 0x3a0;
	bitTable[180][0] = 0xb;
	bitTable[180][1] = 0x391;
	bitTable[181][0] = 0xa;
	bitTable[181][1] = 0x23c;
	bitTable[182][0] = 0xb;
	bitTable[182][1] = 0x392;
	bitTable[183][0] = 0xb;
	bitTable[183][1] = 0x3d5;
	bitTable[184][0] = 0xa;
	bitTable[184][1] = 0x233;
	bitTable[185][0] = 0xa;
	bitTable[185][1] = 0x2cc;
	bitTable[186][0] = 0xb;
	bitTable[186][1] = 0x390;
	bitTable[187][0] = 0xa;
	bitTable[187][1] = 0x1bb;
	bitTable[188][0] = 0xb;
	bitTable[188][1] = 0x3a1;
	bitTable[189][0] = 0xb;
	bitTable[189][1] = 0x3c4;
	bitTable[190][0] = 0xa;
	bitTable[190][1] = 0x211;
	bitTable[191][0] = 0xa;
	bitTable[191][1] = 0x203;
	bitTable[192][0] = 0x9;
	bitTable[192][1] = 0x12a;
	bitTable[193][0] = 0xa;
	bitTable[193][1] = 0x231;
	bitTable[194][0] = 0xb;
	bitTable[194][1] = 0x3e0;
	bitTable[195][0] = 0xa;
	bitTable[195][1] = 0x29b;
	bitTable[196][0] = 0xb;
	bitTable[196][1] = 0x3d7;
	bitTable[197][0] = 0xa;
	bitTable[197][1] = 0x202;
	bitTable[198][0] = 0xb;
	bitTable[198][1] = 0x3ad;
	bitTable[199][0] = 0xa;
	bitTable[199][1] = 0x213;
	bitTable[200][0] = 0xa;
	bitTable[200][1] = 0x253;
	bitTable[201][0] = 0xa;
	bitTable[201][1] = 0x32c;
	bitTable[202][0] = 0xa;
	bitTable[202][1] = 0x23d;
	bitTable[203][0] = 0xa;
	bitTable[203][1] = 0x23f;
	bitTable[204][0] = 0xa;
	bitTable[204][1] = 0x32f;
	bitTable[205][0] = 0xa;
	bitTable[205][1] = 0x11c;
	bitTable[206][0] = 0xa;
	bitTable[206][1] = 0x384;
	bitTable[207][0] = 0xa;
	bitTable[207][1] = 0x31c;
	bitTable[208][0] = 0xa;
	bitTable[208][1] = 0x17c;
	bitTable[209][0] = 0xa;
	bitTable[209][1] = 0x30a;
	bitTable[210][0] = 0xa;
	bitTable[210][1] = 0x2e0;
	bitTable[211][0] = 0xa;
	bitTable[211][1] = 0x276;
	bitTable[212][0] = 0xa;
	bitTable[212][1] = 0x250;
	bitTable[213][0] = 0xb;
	bitTable[213][1] = 0x3e3;
	bitTable[214][0] = 0xa;
	bitTable[214][1] = 0x396;
	bitTable[215][0] = 0xa;
	bitTable[215][1] = 0x18f;
	bitTable[216][0] = 0xa;
	bitTable[216][1] = 0x204;
	bitTable[217][0] = 0xa;
	bitTable[217][1] = 0x206;
	bitTable[218][0] = 0xa;
	bitTable[218][1] = 0x230;
	bitTable[219][0] = 0xa;
	bitTable[219][1] = 0x265;
	bitTable[220][0] = 0xa;
	bitTable[220][1] = 0x212;
	bitTable[221][0] = 0xa;
	bitTable[221][1] = 0x23e;
	bitTable[222][0] = 0xb;
	bitTable[222][1] = 0x3ac;
	bitTable[223][0] = 0xb;
	bitTable[223][1] = 0x393;
	bitTable[224][0] = 0xb;
	bitTable[224][1] = 0x3e1;
	bitTable[225][0] = 0xa;
	bitTable[225][1] = 0x1de;
	bitTable[226][0] = 0xb;
	bitTable[226][1] = 0x3d6;
	bitTable[227][0] = 0xa;
	bitTable[227][1] = 0x31d;
	bitTable[228][0] = 0xb;
	bitTable[228][1] = 0x3e5;
	bitTable[229][0] = 0xb;
	bitTable[229][1] = 0x3e4;
	bitTable[230][0] = 0xa;
	bitTable[230][1] = 0x207;
	bitTable[231][0] = 0xb;
	bitTable[231][1] = 0x3c7;
	bitTable[232][0] = 0xa;
	bitTable[232][1] = 0x277;
	bitTable[233][0] = 0xb;
	bitTable[233][1] = 0x3d4;
	bitTable[234][0] = 0x8;
	bitTable[234][1] = 0xc0;
	bitTable[235][0] = 0xa;
	bitTable[235][1] = 0x162;
	bitTable[236][0] = 0xa;
	bitTable[236][1] = 0x3da;
	bitTable[237][0] = 0xa;
	bitTable[237][1] = 0x124;
	bitTable[238][0] = 0xa;
	bitTable[238][1] = 0x1b4;
	bitTable[239][0] = 0xa;
	bitTable[239][1] = 0x264;
	bitTable[240][0] = 0xa;
	bitTable[240][1] = 0x33d;
	bitTable[241][0] = 0xa;
	bitTable[241][1] = 0x1d1;
	bitTable[242][0] = 0xa;
	bitTable[242][1] = 0x1af;
	bitTable[243][0] = 0xa;
	bitTable[243][1] = 0x39e;
	bitTable[244][0] = 0xa;
	bitTable[244][1] = 0x24f;
	bitTable[245][0] = 0xb;
	bitTable[245][1] = 0x373;
	bitTable[246][0] = 0xa;
	bitTable[246][1] = 0x249;
	bitTable[247][0] = 0xb;
	bitTable[247][1] = 0x372;
	bitTable[248][0] = 0x9;
	bitTable[248][1] = 0x167;
	bitTable[249][0] = 0xa;
	bitTable[249][1] = 0x210;
	bitTable[250][0] = 0xa;
	bitTable[250][1] = 0x23a;
	bitTable[251][0] = 0xa;
	bitTable[251][1] = 0x1b8;
	bitTable[252][0] = 0xb;
	bitTable[252][1] = 0x3af;
	bitTable[253][0] = 0xa;
	bitTable[253][1] = 0x18e;
	bitTable[254][0] = 0xa;
	bitTable[254][1] = 0x2ec;
	bitTable[255][0] = 0x7;
	bitTable[255][1] = 0x62;
	bitTable[256][0] = 0x4;
	bitTable[256][1] = 0xd;
	
	//flatBitTable = new int [514];
	for (int a=0; a<257; a++) {
		flatBitTable[a<<1]=bitTable[a][0];
		flatBitTable[(a<<1)|1]=bitTable[a][1];
	}
	for (int a=0; a<8; a++) {
		jumpTable[a]=25;
	}
	for (int a=8; a<16; a++) {
		jumpTable[a]=15;
	}
	for (int a=16; a<20; a++) {
		jumpTable[a]=0;
	}
	/*
	int* count = new int[16];
	for (int a=0; a<16; a++) {
		count[a]=0;
	}
	
	for (int a=0; a<257; a++) {
		count[bitTable[a][0]]++;
	}
	for (int a=0; a<16; a++) {
		printf("%i of the table entries are %i bits long.\n",count[a],a);
	}
	for (int a=0; a<7; a++) {
		jumpTable[a]=23;
	}
	for (int a=8; a<15; a++) {
		jumpTable[a]=13;
	}
	for (int a=16; a<20; a++) {
		jumpTable[a]=0;
	}*/
	/*
	0 of the table entries are 0 bits long.
	0 of the table entries are 1 bits long.
	1 of the table entries are 2 bits long.
	0 of the table entries are 3 bits long.
	1 of the table entries are 4 bits long.
	2 of the table entries are 5 bits long.
	7 of the table entries are 6 bits long.
	19 of the table entries are 7 bits long.
	29 of the table entries are 8 bits long.
	75 of the table entries are 9 bits long.
	97 of the table entries are 10 bits long.
	26 of the table entries are 11 bits long.
	0 of the table entries are 12 bits long.
	0 of the table entries are 13 bits long.
	0 of the table entries are 14 bits long.
	0 of the table entries are 15 bits long.
	*/
}