#!/usr/bin/python
# -*- coding: utf-8 -*-
### BEGIN LICENSE
# Copyright (C) 2009 Jan Slavetinsky jslavetinsky@seznam.cz
#This program is free software: you can redistribute it and/or modify it 
#under the terms of the GNU General Public License version 3, as published 
#by the Free Software Foundation.
#
#This program is distributed in the hope that it will be useful, but 
#WITHOUT ANY WARRANTY; without even the implied warranties of 
#MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR 
#PURPOSE.  See the GNU General Public License for more details.
#
#You should have received a copy of the GNU General Public License along 
#with this program.  If not, see <http://www.gnu.org/licenses/>.
### END LICENSE

#this and pybspatch are wrappers for the bsdiff module, aiming to use the same diff files the bsdiff executables themselves create and consume
#the implementation is based on the bsdiff source and the following format definition:

#from bspatch.c:
#File format:
#    0      8    "BSDIFF40"
#    8      8    X
#    16     8    Y
#    24     8    sizeof(newfile)
#    32     X    bzip2(control block)
#    32+X   Y    bzip2(diff block)
#    32+X+Y ???  bzip2(extra block)
#with control block a set of triples (x,y,z) meaning "add x bytes
#from oldfile to x bytes from the diff block; copy y bytes from the
#extra block; seek forwards in oldfile by z bytes".

import logging
import bz2
import cStringIO as StringIO
import bsdiff
import server_utils
import os

#implementation of bspatch using the binary bspatch module
def diff(old_file_name, new_file_name, patch_file_name):    
    old_filesize = os.path.getsize(old_file_name)
    logging.debug("opening old file '" + old_file_name + \
                 "' (checksum '" + server_utils.get_checksum(old_file_name) + \
                 "', size " + server_utils.filesize_to_str(old_filesize) + ")")
    with open(old_file_name, "rb") as old_file:
        old_data = old_file.read()
    
    new_filesize = os.path.getsize(old_file_name)
    logging.debug("opening new file '" + new_file_name + \
                 "' (checksum '" + server_utils.get_checksum(new_file_name) + \
                 "', size " + server_utils.filesize_to_str(new_filesize) + ")")    
    with open(new_file_name, "rb") as new_file:   
        new_data = new_file.read()

    logging.debug("invoking the bsdiff.Diff function (progress bar unavailable :( )")
    (control_tuples_list, diff_string, extra_string) = bsdiff.Diff(old_data, new_data)
 
    control_stream = StringIO.StringIO()    
    logging.debug("writing the control tuples")    
    for (x, y, z) in control_tuples_list:
        # write out each triplet of control data
        _offtout(x, control_stream)
        _offtout(y, control_stream)
        _offtout(z, control_stream)

    logging.debug("compressing the diff file blocks")
    compressed_control = bz2.compress(control_stream.getvalue())
    compressed_diff = bz2.compress(diff_string)
    compressed_extra = bz2.compress(extra_string)    
    
    logging.info("writing patch file '" + patch_file_name + "'")
    with open(patch_file_name, "wb") as patch_file:
        patch_file.write("BSDIFF40")   
        _offtout(len(compressed_control), patch_file)
        _offtout(len(compressed_diff), patch_file)
        _offtout(len(new_data), patch_file)
        logging.debug("len(compressed_control): " + str(len(compressed_control)) + ", len(compressed_diff): " + str(len(compressed_diff)) + ", len(new_data): " + str(len(new_data)))
        
        patch_file.write(compressed_control)
        patch_file.write(compressed_diff)
        patch_file.write(compressed_extra)


#taken from the c implementation.
def _offtout(x, stream):
    y = abs(x)
        
    stream.write(chr(y & 0xff))
    stream.write(chr((y >> 1 * 8) & 0xff))
    stream.write(chr((y >> 2 * 8) & 0xff))
    stream.write(chr((y >> 3 * 8) & 0xff))
    stream.write(chr((y >> 4 * 8) & 0xff))
    stream.write(chr((y >> 5 * 8) & 0xff))
    stream.write(chr((y >> 6 * 8) & 0xff))
    z = (y >> 7 * 8) & 0xff
    if(x < 0):
        z |= 0x80;
    stream.write(chr(z))

def main():    
        from sys import argv
        logging.basicConfig(level=logging.INFO)         
        if(len(argv) < 4):
            print "pybsdiff: usage: python pybsdiff.py oldfile newfile patchfile"
        else:
            #logging.basicConfig(level=logging.DEBUG)
            diff(argv[1], argv[2], argv[3])  
    #diff("F:/Development/storage/originals/LBR_nopatch/map0.mul", "F:/Development/storage/releases/2010_12_17_sjednoceno/map0.mul", "C:/games/map0.patch")

if __name__ == '__main__': 
    main()
    

