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

#ifndef FASTDLLHPP
#define FASTDLLHPP

#include <windows.h>
#include <math.h>
#include <shlobj.h>

#if defined(__DMC__)
	//#define EXPORT _export CALLBACK
	#ifndef WINAPI
	#define EXPORT __attribute__((__stdcall__))
	#else
	#define EXPORT _export WINAPI
	#endif
	//#define EXPORT _export CALLBACK __stdcall	//most recent one
	//__cdecl
#elif defined(__MINGW32__) || defined(__GNUWIN32__)
	#define EXPORT __declspec(dllexport) __stdcall
#else
	#define EXPORT CALLBACK
#endif

int **bitTable;
int flatBitTable[514];
unsigned char jumpTable[20];

#endif