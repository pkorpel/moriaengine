from sys import argv
import os
from bsdiff import Patch

import bz2
from array import array
import struct
import cStringIO as StringIO

def patch(old_file_name, new_file_name, patch_file_name):
	#import stopwatch
	#t = stopwatch.Timer() # immediately starts the clock

	with open(patch_file_name, "rb") as patch_file:
		patch_file.read(8) #magic number
		
		compressed_control_len = offtin(patch_file.read(8))
		compressed_diff_len = offtin(patch_file.read(8))
		new_file_len = offtin(patch_file.read(8))
	
		compressed_control_block = patch_file.read(compressed_control_len)
		compressed_diff_block    = patch_file.read(compressed_diff_len)
		compressed_extra_block   = patch_file.read()
	
	#print t.elapsed, "s"

	control_stream = StringIO.StringIO(bz2.decompress(compressed_control_block))
	diff_string    = bz2.decompress(compressed_diff_block)
	extra_string   = bz2.decompress(compressed_extra_block)

	with open(old_file_name, "rb") as old_file:
		old_data = old_file.read()
		
	control_tuples_list = []
	while True:
		r = control_stream.read(8)
		if not r:
			break				
		x = offtin(r)
		y = offtin(control_stream.read(8))
		z = offtin(control_stream.read(8))
		control_tuples_list.append((x,y,z))
		
	new_data = Patch(old_data, new_file_len, control_tuples_list, diff_string, extra_string)
		
	with open(new_file_name, "wb") as new_file:
		new_file.write(new_data)
		
	#import utils
	#print utils.calculate_checksum(new_file_name)
	#print t.elapsed, "s"

def offtin(buf):
	s = ord(buf[7])
	
	y=(s&0x7F)<<56;
	y+=ord(buf[6])<<48;
	y+=ord(buf[5])<<40;
	y+=ord(buf[4])<<32;
	y+=ord(buf[3])<<24;
	y+=ord(buf[2])<<16;
	y+=ord(buf[1])<<8;
	y+=ord(buf[0]);

	if (s&0x80):
		y=-y;
		
	return y

def main():
	if(len(argv) < 4):
		print "bspatch: usage: python pybsdiff.py oldfile newfile patchfile"
	else:
		old_file_name   = argv[1]
		new_file_name   = argv[2]
		patch_file_name = argv[3]
		patch(old_file_name, new_file_name, patch_file_name)
	
if __name__ == '__main__': 
	main()
