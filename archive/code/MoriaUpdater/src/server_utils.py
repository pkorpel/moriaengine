import os
#import sys
from zipfile import ZipFile, ZIP_DEFLATED


from pybsdiff import diff

import server_config as config
import utils
import logging
from zipfileprogress import ZipFileProgress
from pb import fileprogressbar

def get_checksum(filename):
	sumfilename = get_checksum_filename(filename)
	if is_newer_file(filename, sumfilename):
		filesize = os.path.getsize(filename)
		if (config.bequiet() or filesize < 1024*1024):
			sum = utils.calculate_checksum(filename)
		else:        
			pb = fileprogressbar(filesize)
			sum = utils.calculate_checksum(filename, pb.update)
			pb.finish()
			print
		
		f = open(sumfilename,"w")
		f.write(sum)
		f.close()
		return sum
	else:
		f = open(sumfilename,"r")
		sum = f.read()
		f.close()
		return sum

def get_checksum_filename(filename):
	return filename + utils.EXTENSION_CHECKSUM

#source = a .mul file or something
#dependant = its compressed version, or .md5 file etc
#if the 'source' is newer than the dependant, the dependant will need to be updated
def is_newer_file(filename_source, filename_dependant):
	if not os.path.exists(filename_dependant):
		return True
	
	srcstat = os.stat(filename_source)
	srctime = max(srcstat.st_mtime, srcstat.st_ctime)
	
	depstat = os.stat(filename_dependant)	
	deptime = max(depstat.st_mtime, depstat.st_ctime)
	
	return srctime > deptime

def is_helper_filename(filename):
	return filename.endswith(utils.EXTENSION_ARCHIVE) or \
		filename.endswith(utils.EXTENSION_CHECKSUM)
					
def create_patch_if_needed(ftproot, patchesdir, origversion, newversion): #returns versionname of the patch file
	patchpath = os.path.join(patchesdir, utils.get_patchname(origversion, newversion))
	
	patchdir = os.path.dirname(patchpath)
	if not os.path.exists(patchdir):
		os.makedirs(patchdir)
	if os.path.exists(patchpath):
		pass #the patch already exists, we do nothing
	else:
		origpath = os.path.join(ftproot, origversion.versionname, origversion.filename)
		newpath = os.path.join(ftproot, newversion.versionname, newversion.filename)
		diff(origpath, newpath, patchpath)

	return patchpath
		
def delete_patch(patchesdir, origversion, newversion):
	patchpath = os.path.join(patchesdir, utils.get_patchname(origversion, newversion))
	if os.path.exists(patchpath):
		os.remove(patchpath)
	checksumfile = get_checksum_filename(patchpath)
	if os.path.exists(checksumfile):
		os.remove(checksumfile)



#def diff(oldfilename, newfilename, patchfilename):
#	_commandline('bsdiff ' + oldfilename + ' ' + newfilename + ' ' + patchfilename)

#def _commandline(command):
#	print "	cmd:", command
#	for line in outpipe.readlines():
#		sys.stdout.write(line)
#	inpipe.close()
#	inpipe, outpipe = os.popen4(command)
#	code = outpipe.close()
#	if (code):
#		raise RuntimeError('Command failed. Return code = '+str(code))
	
def delete_archive_of(filename):
	archivename = filename+utils.EXTENSION_ARCHIVE
	if os.path.exists(archivename):
		os.remove(archivename)
		
	checksumfile = get_checksum_filename(archivename)
	if os.path.exists(checksumfile):
		os.remove(checksumfile)


def filesize_to_str(filesize):
	return str(int(round((filesize / 1024)))) + "kB"

def compress_and_checksum(filename, arcname=None):
	archivename = filename+utils.EXTENSION_ARCHIVE
	
	if is_newer_file(filename, archivename):	
		if not arcname:
			arcname = os.path.basename(filename)
			
		filesize = os.path.getsize(filename)
		logging.info("zipping file '" + filename + "' - " + filesize_to_str(filesize))
		
		try:
			if (config.bequiet() or filesize < 1024*1024):
				archive = ZipFile(archivename, mode="w", compression=ZIP_DEFLATED)
				archive.write(filename, arcname=arcname)				
			else:
				archive = ZipFileProgress(archivename, mode="w", compression=ZIP_DEFLATED)
				#create progressBar
				pb = fileprogressbar(filesize)
				archive.writeprogress(filename, arcname=arcname, callback=pb.update, compress_type=ZIP_DEFLATED)
				pb.finish()	
				print		
		finally:
			archive.close()
			
		return get_checksum(archivename)	

