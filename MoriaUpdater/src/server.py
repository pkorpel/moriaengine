import os 
import sys
from zipfile import ZipFile, ZIP_DEFLATED

import gnosis.xml.pickle

import update_info as ui
import utils
import server_utils
import server_config as config
import logging
from pb import fileprogressbar
from zipfileprogress import ZipFileProgress

DIRNAME_FTPROOT = "storage"
FILENAME_NOFORCE = "noforce"
FILENAME_TODELETE = "todelete"

FILENAME_PACK = "moriapack"


def main(ftproot):
	ftproot = os.path.abspath(ftproot)
	logging.debug("ftproot: '"+ftproot+"'")
	
	releasesdir = os.path.join(ftproot, utils.DIRNAME_RELEASES)
	patchesdir = os.path.join(ftproot, utils.DIRNAME_PATCHES)
	
	#first, make a list of the files to patch
	uiobj = ui.UpdateInfo("moria update files")
	
	logging.info("creating list of files")
	list_releases(uiobj, releasesdir)
	list_originals(uiobj, ftproot)
	
	logging.info("reading noforce and todelete config files")
	read_additional_config(uiobj, ftproot)
	
	logging.info("creating patches")
	create_patches_successive(uiobj, ftproot, patchesdir) #patches between releases
	create_patches_fromoriginals(uiobj, ftproot, patchesdir) #patches from originals to latest
	
	logging.info("compressing latest files for direct downloading")
	compress_latest(uiobj, releasesdir, ftproot)
	
	logging.info("creating moriapack")
	create_pack(uiobj, ftproot) #basically compresses the same files as compress_latest, 
		#could be somehow optimized, if anyone cared (I don't)
	
	logging.info("writing "+utils.FILENAME_UPDATEINFO)
	uifilename = os.path.join(ftproot, utils.FILENAME_UPDATEINFO)
	with file(uifilename, "w") as f:
		gnosis.xml.pickle.dump(uiobj, f)

	server_utils.compress_and_checksum(uifilename)

	logging.info("done")
	
def list_releases(uiobj, releasesdir):
	dirs_releases = os.listdir(releasesdir)
	uiobj.lastversion = dirs_releases[-1]
	for directory in dirs_releases:
		release_path = os.path.join(releasesdir, directory)
		release_name = os.path.join(utils.DIRNAME_RELEASES, directory)
		for filename in utils.listfiles_recursive(release_path):
			if not server_utils.is_helper_filename(filename): #we are skipping the zipped ones and md5s
				filepath = os.path.join(release_path, filename)
				checksum = server_utils.get_checksum(filepath)
				version = ui.FileVersionInfo(filename, release_name, checksum)
				
				fi = ui.ui_getfilebyname(uiobj, filename)
				if (fi == None):
					fi = ui.FileInfo(filename)
					ui.ui_addfile(uiobj, fi, filename)
				ui.fi_addversion(fi, version)
	
def list_originals(uiobj, ftproot):
	originalsdir = os.path.join(ftproot, utils.DIRNAME_ORIGINALS)
	for directory in os.listdir(originalsdir):
		original_path = os.path.join(originalsdir, directory)
		original_name = os.path.join(utils.DIRNAME_ORIGINALS,directory)
		for filename in utils.listfiles_recursive(original_path):
			filepath = os.path.join(original_path, filename)
			fi = ui.ui_getfilebyname(uiobj, filename)
			if (fi != None):
				checksum = server_utils.get_checksum(filepath)
				version = ui.FileVersionInfo(filename, original_name, checksum)
				version.isoriginal = True
				ui.fi_addversion(fi, version)

def create_patches_successive(uiobj, ftproot, patchesdir):	
	for fi in uiobj.files.values():
		if not fi.todelete:
			if len(fi.versions_sorted) > 1: #at least 2 versions = there's something to patch
				for i in range(0, len(fi.versions_sorted) - 1):
					oldver = fi.versions_sorted[i]
					newver = fi.versions_sorted[i+1]
					patchfilename = server_utils.create_patch_if_needed(ftproot, patchesdir, oldver, newver)				
					oldver.patchchecksum = server_utils.get_checksum(patchfilename)
					oldver.patchsize = os.path.getsize(patchfilename)
					
def create_patches_fromoriginals(uiobj, ftproot, patchesdir):	
	for fi in uiobj.files.values():
		if not fi.todelete:
			latestver = fi.versions_sorted[-1]
			for origver in fi.versions_byname.values():
				if origver.isoriginal:
					
					#delete possible old patches first
					if len(fi.versions_sorted) > 1: #at least 2 release versions = there could be old patch
						for i in range(0, len(fi.versions_sorted) - 1): #all but the last one
							server_utils.delete_patch(patchesdir, origver, fi.versions_sorted[i])
							
					#create patch to latest release
					patchfilename = server_utils.create_patch_if_needed(ftproot, patchesdir, origver, latestver)				
					origver.patchchecksum = server_utils.get_checksum(patchfilename)
					origver.patchsize = os.path.getsize(patchfilename)

def compress_latest(uiobj, releasesdir, ftproot):
	for fi in uiobj.files.values():
		if not fi.todelete:
			#delete possible unneeded archives first
			if len(fi.versions_sorted) > 1: #at least 2 release versions = there could be old archive
				for i in range(0, len(fi.versions_sorted) - 1): #all but the last one
					ver = fi.versions_sorted[i]
					#print "deleting version archive", ver.filename, ver.versionname 
					filename = os.path.join(releasesdir, ver.versionname, ver.filename) 
					server_utils.delete_archive_of(filename)
			
			latestver = fi.versions_sorted[-1]
			filename = os.path.join(ftproot, latestver.versionname, latestver.filename)
			latestver.archivechecksum = server_utils.compress_and_checksum(filename)
			latestver.archivesize = os.path.getsize(filename)

			
def create_pack(uiobj, ftproot):
	packname = os.path.join(ftproot, FILENAME_PACK+utils.EXTENSION_ARCHIVE)

	quiet = config.bequiet()	
	if (quiet):
		archive = ZipFile(packname, mode="w", compression=ZIP_DEFLATED)
	else:
		archive = ZipFileProgress(packname, mode="w", compression=ZIP_DEFLATED)

	try:				
		for fi in uiobj.files.values():
			if not fi.todelete:
				latestver = fi.versions_sorted[-1]
				completefilename = os.path.join(ftproot, latestver.versionname, latestver.filename)
										
				filesize = os.path.getsize(completefilename)		
				logging.info("	adding file '" +  os.path.join(latestver.versionname, latestver.filename) + "' - " + str(int(round((filesize / 1024))))+"kB")
				
				# save the text as a PKZIP format .zip file
				if (quiet or filesize < 1024*1024):
					archive.write(completefilename, arcname=latestver.filename)
				else:
					#create progressBar
					pb = fileprogressbar(filesize)
					archive.writeprogress(completefilename, arcname=latestver.filename, callback=pb.update, )
					pb.finish()
					print
				
				
	finally:
		archive.close()

def read_additional_config(uiobj, ftproot):
	noforcefile = os.path.join(ftproot, FILENAME_NOFORCE)
	
	with open(noforcefile, 'r') as f:
		for line in f:
			fi = ui.ui_getfilebyname(uiobj, line.strip().lower())
			if fi != None:
				fi.forced = False
	
	
	todelfile = os.path.join(ftproot, FILENAME_TODELETE)
	with open(todelfile, 'r') as f:
		for line in f:
			filename = line.strip()
			fi = ui.ui_getfilebyname(uiobj, filename.lower())
			if fi != None:
				fi.todelete = True
			else:
				fi = ui.FileInfo(filename)
				fi.todelete = True
				ui.ui_addfile(uiobj, fi, filename)

	
	
	
if __name__ == '__main__': 
	main(sys.argv[0])