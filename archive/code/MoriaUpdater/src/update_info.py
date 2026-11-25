

class UpdateInfo: #this is the class that gets serialized in the meta file (updateinfo.xml)
	def __init__(self, name):
		self.name = name
		self.files = {}
		self.lastversion = ""
		self.librarychecksum = ""
			
#functions, not methods, because of imperfect xml pickle
def ui_addfile(uiobj, fileinfo, filename):
	uiobj.files[filename.lower()] = fileinfo
	
def ui_getfilebyname(uiobj, filename):
	filename = filename.lower()
	if filename in uiobj.files:
		return uiobj.files[filename]
	else:
		return None
	
def ui_popfilebyname(uiobj, filename):
	filename = filename.lower()
	if filename in uiobj.files:
		return uiobj.files.pop(filename)
	else:
		return None

class FileInfo:
	def __init__(self, filename):
		#self.filename = filename
		self.forced = True
		self.todelete = False
		self.versions_sorted = [] #release versions, sorted
		self.versions_byname = {}
		self.versions_bychecksum = {}
		
def fi_addversion(fi, versioninfo):
	fi.versions_byname[versioninfo.versionname] = versioninfo
	fi.versions_bychecksum[versioninfo.checksum] = versioninfo
	
	if not versioninfo.isoriginal:
		fi.versions_sorted.append(versioninfo)
		fi.versions_sorted = sorted(fi.versions_sorted, key=lambda vi: vi.versionname)
		
def fi_getversionscount(fi):
	return len(fi.versions_byname)
	
def fi_getversionbyname(fi, name):
	if name in fi.versions_byname:
		return fi.versions_byname[name]
	else:
		return None
	
def fi_getversionbychecksum(fi, checksum):
	if checksum in fi.versions_bychecksum:
		return fi.versions_bychecksum[checksum]
	else:
		return None

		
class FileVersionInfo:
	def __init__(self, filename, versionname, checksum):
		self.filename = filename
		self.versionname = versionname #Usually something like "Releases\\20041212", i.e. including the directory above
		self.checksum = checksum 	#checksum of the file
		self.archivechecksum = "" #checksum of the archive (valid for newest version)
		self.archivesize = 0 
		self.patchchecksum = ""		#patch from the previous version to the latest version. 
			#if this is original version, then it's patch to the latest one
		self.patchsize = 0
		self.isoriginal = False		#should be backed up by the client
