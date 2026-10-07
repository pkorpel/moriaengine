from sys import argv
import os

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
		patch_file.read(8) #target file size
	
		#(compressed_control_len, compressed_diff_len, _) = struct.unpack("qqq", raw_data[8:16] + raw_data[16:24] + raw_data[24:32])
	
		compressed_control_block = patch_file.read(compressed_control_len)
		compressed_diff_block    = patch_file.read(compressed_diff_len)
		compressed_extra_block   = patch_file.read()
	
	#print t.elapsed, "s"

	control_stream = StringIO.StringIO(bz2.decompress(compressed_control_block))
	diff_stream    = StringIO.StringIO(bz2.decompress(compressed_diff_block))
	extra_stream   = StringIO.StringIO(bz2.decompress(compressed_extra_block))

	with open(old_file_name, "rb") as old_file:
		#old_file_size = len(old_file.read())
		#old_file.seek(0)
	
		#control_block_size = len(control_stream.read())
		#control_stream.seek(0)
	
		#block_size = 8
		with open(new_file_name, "wb") as new_file:	
			while True:
				# read control data
				# 1. add x bytes from old to x bytes of diff file
				# 2. copy y bytes from extra
				# 4. seek forward z bytes in the old file
				r = control_stream.read(8)
				if not r:
					break				
				x = offtin(r)
				y = offtin(control_stream.read(8))
				z = offtin(control_stream.read(8))
				#print x, y, z
				#(x, y, z) = struct.unpack("qqq", a+b+c)
				diff = diff_stream.read(x)
				old = old_file.read(x)
				arr = array('c', old)
				for i in range(len(diff)):
					arr[i] = chr((ord(diff[i]) + ord(arr[i])) % 256)
				arr.tofile(new_file)
				#print "y", y
				for i in extra_stream.read(y):
					new_file.write(i)

				old_file.seek(z, os.SEEK_CUR)
		
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
		print "pybspatch: usage: python pybsdiff.py oldfile newfile patchfile"
	else:
		old_file_name   = argv[1]
		new_file_name   = argv[2]
		patch_file_name = argv[3]
		patch(old_file_name, new_file_name, patch_file_name)
	
if __name__ == '__main__': 
	main()
