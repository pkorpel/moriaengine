import os, sys
from common import *
import gnosis.xml.pickle

FTPROOT = "storage"
NOFORCE = "noforce"
TODELETE = "todelete"
WINRARPATH = os.path.join("WinRAR","WinRAR.exe")
RELEASEARCHIVE = "release.rar"

localdir = os.path.dirname(sys.argv[0])

WINRARPATH = os.path.join(localdir, WINRARPATH)
FTPROOT = os.path.join(localdir, FTPROOT)

uifilename = os.path.join(FTPROOT, UPDATEINFO)
originalsdir = os.path.join(FTPROOT, ORIGINALS)
releasesdir = os.path.join(FTPROOT, RELEASES)
patchesdir = os.path.join(FTPROOT, PATCHES)
noforcefile = os.path.join(FTPROOT, NOFORCE)
todelfile = os.path.join(FTPROOT, TODELETE)
updaterfile = os.path.join(FTPROOT, UPDATER, "library.zip")


def commandline(command):
	#print "commandline:", command
	print command
	inpipe, outpipe = os.popen4(command)
	for line in outpipe.readlines():
		sys.stdout.write(line)
	inpipe.close()
	return outpipe.close()

def compressfile(filename):
	archivename = filename+ARCHIVEEXTENSION
	if os.path.exists(archivename):
		os.remove(archivename)
	#commandline("7za a "+archivename+" "+filename+" -mx=9 -ms=on")
	if commandline(WINRARPATH+" u -m5 -ep1 -ed -y "+archivename+" "+filename):
		print "Error while compressing "+filename+"!"

def unpackrelease(release = ""): #if None, unpack the newest
	if (len(release) < 1):
		dirs = os.listdir(releasesdir)
		dirs.sort()
		dirs.reverse()
		archivepath = os.path.join(releasesdir, dirs[0], RELEASEARCHIVE)
	else:
		archivepath = os.path.join(releasesdir, release, RELEASEARCHIVE)
		
	#print archivepath, os.path.dirname(archivepath)
	
	if os.path.exists(archivepath):
		if not commandline(WINRARPATH+" x -y "+archivepath+" "+os.path.dirname(archivepath)): #not means errorlevel 0
			os.remove(archivepath)
			print "succesfully unpacked, archive removed"
		else:
			print "some error occured while unpacking"
	else:
		print "There is no archive at "+archivepath

def makefilelist(basedir, currentdir, filelist): #makes a recursive file list, with paths starting from currentdir
	for name in os.listdir(basedir):
		filepath = os.path.join(basedir, name)
		relativepath = os.path.join(currentdir, name)
		if os.path.isdir(filepath):
			makefilelist(filepath, relativepath, filelist)
		elif os.path.isfile(filepath):
			#print "appending", filepath, "to", filelist
			filelist.append(relativepath)
	return filelist
	
def makepatchifneeded(filename, origversion, newversion): #returns it's checksum
	patchpath = os.path.join(patchesdir, getpatchname(filename, origversion.name, newversion.name))
	patchdir = os.path.dirname(patchpath)
	if not os.path.exists(patchdir):
		os.makedirs(patchdir)
	zippatchpath = patchpath+ARCHIVEEXTENSION
	if (os.path.exists(patchpath)):
		pass #the patch already exists, we do nothing
	else:
		if os.path.exists(zippatchpath):
			os.remove(zippatchpath)
		origpath = os.path.join(FTPROOT, origversion.name, filename)
		newpath = os.path.join(FTPROOT, newversion.name, filename)
		if commandline(os.path.join(localdir, "jdiff")+" "+origpath+" "+newpath+" "+patchpath+" -bv"):
			print "Error while diffing "+newpath+" against "+origpath+"!"
	origversion.patchchecksum = getchecksum(patchpath)
	if not os.path.exists(zippatchpath):
		compressfile(patchpath)
	origversion.patcharchivechecksum = getchecksum(zippatchpath)


def start(arg=None):
	#first, make a list of the files to patch
	ui = UpdateInfo("moria update files")
	
	print "creating list of files"
	dirs = os.listdir(releasesdir)
	dirs.sort()
	dirs.reverse()
	ui.lastversion = dirs[0]
	for directory in dirs:
		versiondir = os.path.join(releasesdir, directory)
		versionname = os.path.join(RELEASES, directory)
		for filename in makefilelist(versiondir, "", []):
			if not (filename.endswith(ARCHIVEEXTENSION) or filename.endswith(".zip") or filename.endswith(".rar") or filename.endswith(".7z")): #we are skipping the zipped ones
				filepath = os.path.join(versiondir, filename)
				checksum = getchecksum(filepath) #from common.py
				version = FileVersionInfo(versionname, checksum)
				archivepath = filepath+ARCHIVEEXTENSION
				
				fi = ui.getfilebyname(filename)
				if (fi == None):
					fi = FileInfo(filename)
					
					fi.forced = True
					
					ui.addfile(fi)
					fi.latestversion = version
					if not os.path.exists(archivepath):
						compressfile(filepath)
					version.archivechecksum = getchecksum(archivepath)
				#print "file:", filepath, "fi.getversionscount=", fi.getversionscount
				if fi.getversionscount() == 1:
					fi.lastbutoneversion = version
					if os.path.exists(archivepath):
						os.remove(archivepath)
						
				fi.addversion(version)
			
	for directory in os.listdir(originalsdir):
		versiondir = os.path.join(originalsdir, directory)
		versionname = os.path.join(ORIGINALS,directory)
		for fi in ui.files.values(): #we could also walk the list we have in the ui, but who cares...
			filepath = os.path.join(versiondir, fi.filename)
			if os.path.exists(filepath):
				checksum = getchecksum(filepath) #from common.py
				version = FileVersionInfo(versionname, checksum)
				version.isoriginal = True
				fi.addversion(version)
				
	
	#and now, create the patches and the latest pack
	print "creating needed patchfiles"
	moriapackpath = os.path.abspath(os.path.join(FTPROOT, ARCHIVENAME, ARCHIVENAME+"_"+dirs[0]+ARCHIVEEXTENSION))
	filestopack = None
	if not os.path.exists(moriapackpath):
		filestopack = [] #ZipFile(os.path.join(FTPROOT, ARCHIVENAME+"_"+dirs[0]+ARCHIVEEXTENSION), 'w', ZIP_DEFLATED)
	
	for fi in ui.files.values():
		for version in fi.versionsbyname.values():
			if version != fi.latestversion:
				makepatchifneeded(fi.filename, version, fi.latestversion)
			elif (filestopack != None): #write it to the pack
				filestopack.append((os.path.join(FTPROOT, version.name), fi.filename)) #appends a tuple of versiondir, filename
			if fi.lastbutoneversion != None:#we remove old patches if there are any
				patchpath = os.path.join(patchesdir, getpatchname(fi.filename, version.name, fi.lastbutoneversion.name))
				if os.path.exists(patchpath):
					os.remove(patchpath)
				archivepath = patchpath+ARCHIVEEXTENSION
				if os.path.exists(archivepath):
					os.remove(archivepath)
				
	
	print "creating moriapack"
	if filestopack != None:
		origdir = os.getcwd()
		for filetuple in filestopack:
			os.chdir(filetuple[0])
			cmdline = WINRARPATH+" u -m5 -ed -y "+moriapackpath+" "+filetuple[1]
			#print cmdline
			if commandline(cmdline):
				print "Error while compressing moriapack!"
			os.chdir(origdir)
	
	print "reading additional config files and checking client version"
	try:
		os.remove(os.path.join(FTPROOT, ARCHIVENAME, ARCHIVENAME+"_"+dirs[1]+ARCHIVEEXTENSION)) #remove the last but one pack
	except:
		pass
	
	for line in open(noforcefile):
		fi = ui.getfilebyname(line.strip().lower())
		if fi != None:
			fi.forced = False
	
	for line in open(todelfile):
		filename = line.strip()
		fi = ui.getfilebyname(filename.lower())
		if fi != None:
			fi.todelete = True
		else:
			fi = FileInfo(filename)
			fi.todelete = True
			ui.addfile(fi)
	
	
	if (os.path.exists(updaterfile)):
		ui.librarychecksum = getchecksum(updaterfile)
	
	print "writing "+UPDATEINFO
	gnosis.xml.pickle.dump(ui, file(uifilename, "w"))
	compressfile(uifilename)
	
	#gnosis.xml.pickle.dump(ui, GzipFile("file.xml.gzip", "w", 9, open("file.xml.gzip", "w")))
	
	
	
if __name__ == '__main__': 
	start()