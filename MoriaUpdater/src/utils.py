import os
import hashlib
import logging

EXTENSION_ARCHIVE = ".zip"
EXTENSION_CHECKSUM = ".md5"

DIRNAME_ORIGINALS = "originals"
DIRNAME_RELEASES = "releases"
DIRNAME_PATCHES = "patches"

FILENAME_UPDATEINFO = "updateinfo.xml"


BUFFERSIZE=1024*1024

def calculate_checksum(filename, progress_callback = None):
	m = hashlib.md5()
	
	filesize = os.path.getsize(filename)
	logging.debug("computing checksum of file " + filename + " - " + str(int(round((filesize / 1024))))+" kB")
	
	with open(filename, 'rb') as fp:
		done = 0
		while 1:
			data = fp.read(BUFFERSIZE)
			l = len(data)
			if l == 0:
				break
			m.update(data)
			if (progress_callback):
				done += l
				progress_callback(done)
		
	return m.hexdigest()
	
def get_patchname(origversion, newversion):
	origversionname = os.path.basename(origversion.versionname) #the version names include directory, which we dont need
	newversionname = os.path.basename(newversion.versionname)
	#filename = filename.replace("\\", ".-.")
	#filename = filename.replace("/", ".-.")
	return origversion.filename.lower()+"."+origversionname+"."+newversionname+".patch"

def listfiles_recursive(basedir): #makes a recursive file list, with paths starting from basedir
	filelist = []
	listfiles_recursive_impl(basedir, "", filelist)
	return filelist

def listfiles_recursive_impl(basedir, currentdir, filelist): #makes a recursive file list, with paths starting from basedir
	for name in os.listdir(basedir):
		filepath = os.path.join(basedir, name)
		relativepath = os.path.join(currentdir, name)
		if os.path.isdir(filepath):
			listfiles_recursive_impl(filepath, relativepath, filelist)
		elif os.path.isfile(filepath):
			#print "appending", filepath, "to", filelist
			filelist.append(relativepath)
		
		
#	
#def uncompressandremove(filename):
#	archivename = filename+ARCHIVEEXTENSION
#	print "rozbaluji "+archivename
#	archive = ZipFile(archivename)
#	unzipped = open(filename, "wb")
#	unzipped.write(archive.read(os.path.basename(filename)))
#	unzipped.close()
#	archive.close()
#	os.remove(archivename)
