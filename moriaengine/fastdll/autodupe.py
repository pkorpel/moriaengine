from __future__ import division
import os, sys, os.path, string
import cPickle
import getpass
import telnetlib
import time
import socket
import threading
from threading import Thread, Event, Lock

basefolder = '.'

def main():
	curOutFile=open('thirteen.dupe','wt')
	curOutFile.write("//Don't panic, I didn't type all this myself - I made a python script to write it, of course. :P\n")
	for a in range(1,251):
		curOutFile.write('#define TWO two'+str(a)+'\n#define SKIP skip'+str(a)+'\n#define WHILE while'+str(a)+'\n#define ONEBYTE onebyte'+str(a)+'\n#define TWOBYTES twobytes'+str(a)+'\n#define NOTONEBYTE notonebyte'+str(a)+'\n#include "unrolledloop.dupe"\n#undef SKIP\n#undef WHILE\n#undef ONEBYTE\n#undef TWOBYTES\n#undef NOTONEBYTE\n#undef TWO\n')
	
	curOutFile.close()


if __name__ == '__main__': main()