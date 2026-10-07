import os, md5

ARCHIVEEXTENSION = ".rar"
ORIGINALS = "Originals"
RELEASES = "Releases"
PATCHES = "Patches"
UPDATER = "Updater"
ARCHIVENAME = "moriapack"
UPDATEINFO = "updateinfo.xml"

class UpdateInfo: #this is the class that gets serialized in the meta file (updateinfo.xml)
	def __init__(self, name):
		self.name = name
		self.files = {}
		self.lastversion = ""
		self.librarychecksum = ""
			
	def addfile(self, fileinfo):
		self.files[fileinfo.filename.lower()] = fileinfo
		
	def getfilebyname(self, filename):
		filename = filename.lower()
		if filename in self.files:
			return self.files[filename]
		else:
			return None

class FileInfo:
	def __init__(self, filename):
		self.filename = filename
		self.forced = True
		self.todelete = False
		self.latestversion = None
		self.lastbutoneversion = None #those are being deleted from the server...
		self.versionsbyname = {}
		self.versionsbychecksum = {}
		
	def addversion(self, versioninfo):
		self.versionsbyname[versioninfo.name] = versioninfo
		self.versionsbychecksum[versioninfo.checksum] = versioninfo
			
	def getversionscount(self):
		return len(self.versionsbyname)
		
	def getversionbyname(self, name):
		if name in self.versionsbyname:
			return self.versionsbyname[name]
		else:
			return None
		
	def getversionbychecksum(self, checksum):
		if checksum in self.versionsbychecksum:
			return self.versionsbychecksum[checksum]
		else:
			return None
			
def FileInfogetversionbychecksum(self, checksum): #dirty hack - the deserialized instances have no methods :\
	if checksum in self.versionsbychecksum:
		return self.versionsbychecksum[checksum]
	else:
		return None
		
class FileVersionInfo:
	def __init__(self, name, checksum):
		self.name = name			#name of the version, not the file. Usually something like "Releases\\20041212", i.e. including the directory above
		self.checksum = checksum 	#checksum of the file
		self.archivechecksum = "" #checksum of the archive (valid for newest version)
		self.patcharchivechecksum = "" #checksum of the archived patch (valid for the older versions - could be in one variable with archivechecksum, but this is safer...)
		self.patchchecksum = ""		#patch from this version to the latest version. None for the latest version itself
		self.isoriginal = False		#should be backuped by the client
		
def getchecksum(filename):
	#print "getting checksum for", filename
	m = md5.new()
	fp = open(filename, 'rb')
	try:
		while 1:
			data = fp.read(8096) #such a nice number :)
			if not data:
				break
			m.update(data)
	finally:
		fp.close()
	
	return m.hexdigest()
	
def getpatchname(filename, origversion, newversion):
	origversion = os.path.basename(origversion) #the version names include directory, which we dont need
	newversion = os.path.basename(newversion)
	#filename = filename.replace("\\", ".-.")
	#filename = filename.replace("/", ".-.")
	return filename+"."+origversion+"."+newversion+".patch"

#def compressfile(filename):
#	archive = ZipFile(filename+ARCHIVEEXTENSION, "w", ZIP_DEFLATED)
#	archive.write(filename, os.path.basename(filename))
#	archive.close()
#	#os.remove(filename)
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
