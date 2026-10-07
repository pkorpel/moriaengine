#from stat import S_IWRITE
import gnosis.xml.pickle, sys, os, getopt, urllib, time
from twisted.internet import reactor, threads, defer
from twisted.web import http
from twisted.web.client import HTTPDownloader, HTTPPageDownloader
import _winreg as wreg
#import these because py2exe doesnt recognize them automatically
import twisted.web.resource

from common import *

CONFIGFILE = "config.xml"
LOGFILE = "log.txt"
VERSION = "0.2.11"

#def makeNonReadonly(path):
#	if os.path.exists(path):
#		os.chmod(path, S_IWRITE)
#		if os.path.isdir(path):
#			for name in os.listdir(path):
#				subpath = os.path.join(path, name)
#				makeNonReadonly(subpath)

def writeOutput(text):
	ui.text.write(text)
	logfile.write(text)
	

def makeremove(path):
	if os.path.exists(path):
		#os.remove(backuppath)
		commandline("rm -fr \""+path+"\"")
	
def makerename(origpath, moveto):
	if os.path.exists(origpath):
		#makeremove(moveto)
		#os.rename(origpath, moveto)
		commandline("mv -f \""+origpath+"\" \""+moveto+"\"")

def backupfile(relpath, abspath):#absolute path from inside UO
	backuppath = os.path.join(config.backupdir, relpath)
	makeremove(backuppath)
	backupsubdir = os.path.dirname(backuppath)
	if not os.path.exists(backupsubdir):
		os.makedirs(backupsubdir)
	makerename(abspath, backuppath)
	
def commandline(command):
	#print "commandline:", command
	reactor.callFromThread(ui.progressBar.startactivity)
	inpipe, outpipe = os.popen4(command)
	buffer = ""
	for line in outpipe.readlines():
		#print line
		buffer = buffer+line
	inpipe.close()
	retvalue = outpipe.close()
	#retvalue = os.system(command)
	reactor.callFromThread(ui.progressBar.stopactivity)
	if retvalue:
		raise Exception(buffer)
	return 0
	
def uncompressandremove(filename):
	archivename = filename+ARCHIVEEXTENSION
	pathtoput = os.path.dirname(filename)
	reactor.callFromThread(writeOutput, "\trozbaluji...\n") # "+archivename+"\n")
	line = "7z x \""+archivename+"\" -o\""+pathtoput+"\" -trar -y -bd"
	#print line
	commandline(line)
	makeremove(archivename)

def remove_noid_from_register():
	try:
		#HKEY_LOCAL_MACHINE\\SOFTWARE\\Origin Worlds Online\\Ultima Online\\1.0\\noid="..."
		key = wreg.OpenKey(wreg.HKEY_LOCAL_MACHINE, "SOFTWARE\\Origin Worlds Online\\Ultima Online\\1.0", 0, wreg.KEY_SET_VALUE)
		wreg.DeleteValue(key, "noid")
		#wreg.SetValue(verskey, "nouid", wreg.REG_SZ, "normalstring")
		key.Close()
	except Exception, e:
		print "Exception while remove_noid_from_register", e
		#pass

def getuodir():
	path = ""
	try:
		#HKEY_LOCAL_MACHINE\SOFTWARE\Origin Worlds Online\Ultima Online\1.0
		key = wreg.OpenKey(wreg.HKEY_LOCAL_MACHINE, "SOFTWARE\\Origin Worlds Online\\Ultima Online\\1.0")
		path = wreg.QueryValueEx(key, "InstCDPath")[0]
		key.Close()
	except:
		pass
		
	
	#if (path != ""):
	#	print "Je toto spravna cesta k vasi instalaci UO? (a/n)"
	#	path = os.path.normpath(path)
	#	if (raw_input(path+" ?").strip().lower() == "a"):
	#		return path
	#print "Napiste prosim cestu do vaseho adresare UO a stisknete Enter"
	#s = raw_input().strip()
	path = ui.askstring("Lokalizace instalace UO", "Vlozte spravnou cestu k adresari vasi instalace UO", initialvalue = path)
	if path == None: #user hit Cancel
		sys.exit(0)
	return path

class Config:
	def __init__(self):
		self.hostname = "http://uopatch.septem.cz"
		self.port = "80"
		self.uodir = getuodir()
		self.tempdir = "temp"
		self.dobackup = True
		self.backupdir = "backup"
		self.runaftersuccess = os.path.join(self.uodir, "Client_3.0.6m.exe")
		self.uiname = "tk"
		self.proxyhost = "http://yourproxyhost.com"
		self.proxyport = "8001"
		self.useproxy = False
		self.deleteverdata = True
		self.allowselfupdate = True

def downloadfile(filename, checksum=None):
	global filenamebeingdownloaded, checksumbeingdownloaded, pendingDownloadDeferred
	filenamebeingdownloaded = filename
	checksumbeingdownloaded = checksum
	localpath = os.path.join(config.tempdir, os.path.basename(filename))
	if (checksum != None) and os.path.exists(localpath): #is it not already ready downloaded from previous session(s)?
		deferToThread(getchecksum, localpath).addCallback(downloadfile_lookedupchecksum)
	else:
		downloadfile_start()
		
	pendingDownloadDeferred = defer.Deferred()
	return pendingDownloadDeferred
	
def downloadfile_lookedupchecksum(checksum):
	if (checksumbeingdownloaded == checksum):
		#print "the file in Temp is already complete"
		reactor.callLater(0, pendingDownloadDeferred.callback, None)
	else:
		downloadfile_start()

def downloadfile_start():
	remotepath = config.hostname+":"+config.port+"/"+urllib.pathname2url(filenamebeingdownloaded)
	localpath = os.path.join(config.tempdir, os.path.basename(filenamebeingdownloaded))
	
	factory = FileDownloader(remotepath, localpath, supportPartial = 1)
	if (config.useproxy):
		reactor.connectTCP(config.proxyhost, int(config.proxyport), factory)
	else:
		reactor.connectTCP(config.hostname, int(config.port), factory)
	
	factory.deferred.addErrback(erroroccured)
	factory.deferred.addCallback(downloadfile_completed)

def downloadfile_completed(arg):
	#print "downloadfile_completed", arg
	reactor.callLater(0, pendingDownloadDeferred.callback, arg)
	
#
#the main work is done here :)

##first thing is to download the metafile

def deferToThread(func, *args):
	d = threads.deferToThread(func, *args)
	d.addCallback(deferToThread_completed)
	d.addErrback(erroroccured)
	global pendingThreadDeferred
	pendingThreadDeferred = defer.Deferred()
	return pendingThreadDeferred
	
def deferToThread_completed(arg):
	reactor.callLater(0, pendingThreadDeferred.callback, arg)

def startUpdateinfoDownload():
	writeOutput("stahuji metainfo soubor ("+UPDATEINFO+")\n")
	updateinfopath = os.path.join(config.tempdir, UPDATEINFO+ARCHIVEEXTENSION)
	makeremove(updateinfopath)
	downloadfile(UPDATEINFO+ARCHIVEEXTENSION).addCallback(updateinfoDownloaded)
	
def updateinfoDownloaded(arg):
	#writeOutput("\tupdateinfo soubor stazen\n")
	updatefile = os.path.join(config.tempdir, UPDATEINFO)
	deferToThread(uncompressandremove, updatefile).addCallback(updateinfoUnpacked)
		
def updateinfoUnpacked(arg):
	#writeOutput("\tupdateinfo soubor rozbalen\n")
	updatefile = os.path.join(config.tempdir, UPDATEINFO)
	lastupdatefile = updatefile+".last"
	if os.path.exists(lastupdatefile) and not forcecheck: #forcecheck means we continue regardless of what happened last time, checking all files
		if not os.path.exists(os.path.join(config.tempdir, "working")): #if yes, it means last patching was unsuccesful, we shouldnt stop too early
			lastcheck = getchecksum(lastupdatefile)
			nowcheck = getchecksum(updatefile)
			if (lastcheck == nowcheck):
				writeOutput("Zadna zmena od minuleho updatu (pokud jste jineho nazoru, spustte pres forceupdate.bat)\n")
				makerename(updatefile, lastupdatefile)
				stopapplication()
				return
		makeremove(lastupdatefile)
	reactor.callLater(0, loadUpdateinfo)
	
def loadUpdateinfo():
	updatefile = os.path.join(config.tempdir, UPDATEINFO)
	lastupdatefile = updatefile+".last"
	global updateinfo
	updateinfo = gnosis.xml.pickle.load(file(updatefile))
	writeOutput("\tinformace precteny\n")
	makerename(updatefile, lastupdatefile)
	open(os.path.join(config.tempdir, "working"), "w").close() #touch
	reactor.callLater(0, checkVersion)
	
def checkVersion():
	#check if there isnt a new version of itself
	if (config.allowselfupdate):
		if os.path.exists("library.zip"): #are we running out of the library (i.e. using py2exe), because only then are we updateable
			if (updateinfo.librarychecksum != ""): #is there anyting to download at all?
				libchecksum = getchecksum("library.zip");
				if (libchecksum != updateinfo.librarychecksum):
					writeOutput("Nova verze updateru! Stahuji...\n")
					if not pretend:
						downloadfile(UPDATER+"/library.zip", updateinfo.librarychecksum).addCallback(newVersionDownloaded)
						return
	reactor.callLater(0, startWalkingFiles)
						
def newVersionDownloaded(ignoredarg):
	donework.append("Stazena nova verze updateru")
	templibrarypath = os.path.join(config.tempdir, "library.zip")
	if (assertChecksum(templibrarypath, updateinfo.librarychecksum)):
		makerename(templibrarypath, "library.zip")
		pathtobat = os.path.abspath(os.path.join("..", "update.bat"))
		writeOutput("restartuji se:\n")
		os.chdir("..")
		os.startfile(pathtobat)
		reactor.stop()
		#stopapplication()
	
def startWalkingFiles():
	global index, filelist
	filelist = updateinfo.files.values()
	index = 0
	reactor.callLater(0, processWalkedFile)
	writeOutput(updateinfo.name+":\n")
	
def processWalkedFile():
	global index
	if len(filelist) <= index:
		reactor.callLater(0, stopapplication)
	else:
		fi = filelist[index]
		index += 1
		fileinuo = os.path.join(config.uodir, fi.filename) #name of the file in uo directory
		if fi.todelete:
			#writeOutput("fi.todelete\n") 
			if os.path.exists(fileinuo):
				if (fi.filename.lower() == "verdata.mul") and not config.deleteverdata:
					writeOutput("soubor '"+fi.filename+"' ponechan na zaklade nastaveni.\n")
				elif config.dobackup:
					writeOutput("soubor '"+fi.filename+"' budiz odstranen (presunut do backup adresare)\n")
					if not pretend:
						backupfile(fi.filename, fileinuo)
				else:
					writeOutput("soubor '"+fi.filename+"' budiz odstranen\n")
					if not pretend:
						makeremove(fileinuo)
			reactor.callLater(0, processWalkedFile)
		elif fi.forced:
			reactor.callLater(0, downloadOrPatch, fi)
		elif os.path.exists(fileinuo):
			reactor.callLater(0, downloadOrPatch, fi)
		else:
			reactor.callLater(0, processWalkedFile)
		


def downloadOrPatch(fi): #argument is fileinfo instance. eventually must call processWalkedFile()
	global currentFileInfo
	currentFileInfo = fi
	#let's first check if the file is not complete already
	#os.path.exists(fileinuo):
	#	deferToThread(getchecksum, fileinuo).addCallback(downloadOrPatch_gotChecksum)
	
	writeOutput("soubor '"+fi.filename+"' ...") 
	fileinuo = os.path.join(config.uodir, currentFileInfo.filename) #name of the file in uo directory
	if os.path.exists(fileinuo):
		deferToThread(getchecksum, fileinuo).addCallback(downloadOrPatch_gotChecksum)
	else:
		writeOutput("v adresari UO neni, ale ma byt. Stahuji...\n")
		if not pretend:
			donework.append(currentFileInfo.filename+": stahovan novy")
			downloadAndMove()
		else:
			reactor.callLater(0, processWalkedFile)
		
def downloadOrPatch_gotChecksum(checksum):
	version = FileInfogetversionbychecksum(currentFileInfo, checksum)
	fileinuo = os.path.join(config.uodir, currentFileInfo.filename)
	if (version == None): #unknown version
		if config.dobackup:
			writeOutput("je nezname verze, stahuji cely aktualni soubor a zalohuji...\n")
			if not pretend:
				donework.append(currentFileInfo.filename+": zalohovan a stazen novy")
				backupfile(currentFileInfo.filename, fileinuo)
		else:
			writeOutput("je nezname verze, stahuji cely aktualni soubor...\n")
			if not pretend:
				donework.append(currentFileInfo.filename+": stazen novy")
				makeremove(fileinuo)
		if not pretend:
			downloadAndMove()
		else:
			reactor.callLater(0, processWalkedFile)
		
	elif (checksum == currentFileInfo.latestversion.checksum):
		writeOutput("je v aktualni verzi, netreba resit\n")
		reactor.callLater(0, processWalkedFile)
	else:
		writeOutput("je zastarala verze '"+os.path.basename(version.name)+"'. Stahuji patch...\n")
		if not pretend:
			donework.append(currentFileInfo.filename+": aplikovan patch")
			global currentOrigVersion
			currentOrigVersion = version
			reactor.callLater(0, downloadAndPatch)
		else:
			reactor.callLater(0, processWalkedFile)

def assertChecksum(filepath, checksum):
	realchecksum = getchecksum(filepath)
	if (realchecksum != checksum):
		problemstring = "soubor "+os.path.basename(filepath)+" neodpovida ocekavane checksum ("+realchecksum+" != "+checksum+"), preskakuji. Zkuste opakovat download pozdeji a/nebo kontaktovat GM team"
		reactor.callFromThread(writeOutput, problemstring+"\n")
		problems.append(problemstring)
		return False
	else:
		return True

def downloadAndMove():#just download, unpack, move to uodir. eventually must call processWalkedFile()
	downloadfile(currentFileInfo.latestversion.name+"/"+currentFileInfo.filename+ARCHIVEEXTENSION, currentFileInfo.latestversion.archivechecksum).addCallback(downloadAndMove_downloaded)
		
def downloadAndMove_downloaded(ignoredarg):
	filename, version = currentFileInfo.filename, currentFileInfo.latestversion
	temppath = os.path.join(config.tempdir, os.path.basename(filename))
	deferToThread(uncompressandremove, temppath).addCallback(downloadAndMove_uncompressed)

def downloadAndMove_uncompressed(ignoredarg):
	temppath = os.path.join(config.tempdir, os.path.basename(currentFileInfo.filename))
	deferToThread(assertChecksum, temppath, currentFileInfo.latestversion.checksum).addCallback(downloadAndMove_checksumChecked)
		
def downloadAndMove_checksumChecked(checkresult):
	if checkresult:
		writeOutput("\tsoubor uspesne stazen a rozbalen\n")
		fileinuo = os.path.join(config.uodir, currentFileInfo.filename)
		dirinuo = os.path.dirname(fileinuo)
		temppath = os.path.join(config.tempdir, os.path.basename(currentFileInfo.filename))
		if not os.path.exists(dirinuo):
			os.makedirs(dirinuo)
		makerename(temppath, fileinuo)
		reactor.callLater(0, processWalkedFile)
	#else ?
		

def downloadAndPatch():#download, unpack, apply to the file in uodir
	#filename, origversion, newversion = currentFileInfo.filename, currentOrigVersion, currentFileInfo.latestversion
	patchname = getpatchname(currentFileInfo.filename, currentOrigVersion.name, currentFileInfo.latestversion.name)
	downloadfile(PATCHES+"/"+patchname+ARCHIVEEXTENSION, currentOrigVersion.patcharchivechecksum).addCallback(downloadAndPatch_downloaded)
	
def downloadAndPatch_downloaded(ignoredarg):
	patchname = getpatchname(currentFileInfo.filename, currentOrigVersion.name, currentFileInfo.latestversion.name)
	temppatchpath = os.path.join(config.tempdir, os.path.basename(patchname))
	deferToThread(uncompressandremove, temppatchpath).addCallback(downloadAndPatch_uncompressed)
	
def downloadAndPatch_uncompressed(ignoredarg):
	patchname = getpatchname(currentFileInfo.filename, currentOrigVersion.name, currentFileInfo.latestversion.name)
	temppatchpath = os.path.join(config.tempdir, os.path.basename(patchname))
	deferToThread(assertChecksum, temppatchpath, currentOrigVersion.patchchecksum).addCallback(downloadAndPatch_checksumChecked)
	
def downloadAndPatch_checksumChecked(checkresult):
	if checkresult:
		writeOutput("\taplikuji patch...\n")
		deferToThread(applyPatch).addCallback(downloadAndPatch_completed)
		
def downloadAndPatch_completed(ignoredarg):
	reactor.callLater(0, processWalkedFile)
		
def applyPatch(): #currentFileInfo, currentOrigVersion. runs in thread.
	patchname = getpatchname(currentFileInfo.filename, currentOrigVersion.name, currentFileInfo.latestversion.name)
	temppatchpath = os.path.join(config.tempdir, os.path.basename(patchname))
	tempfilepath = os.path.join(config.tempdir, os.path.basename(currentFileInfo.filename))
	fileinuo = os.path.join(config.uodir, currentFileInfo.filename)
	makerename(fileinuo, tempfilepath) #move the file from uodir to temp as (maybe temporary) backup
	line = "jpatch \""+tempfilepath+"\" \""+temppatchpath+"\" \""+fileinuo+"\""
	#writeOutput("commandline: "+line+"\n")
	commandline(line)
	#writeOutput("after commandline\n")
	if (assertChecksum(fileinuo, currentFileInfo.latestversion.checksum)):
		reactor.callFromThread(writeOutput, "\tpatch uspesne aplikovan\n")
		if (config.dobackup and currentOrigVersion.isoriginal):
			backupfile(os.path.basename(fileinuo), tempfilepath)
		else:
			makeremove(tempfilepath)
	else:
		makerename(tempfilepath, fileinuo) #move back the original file (the failed patched one is removed by now)
	makeremove(temppatchpath)
		
		
def printSummary():
	writeOutput("\n")
	endwithpause = False
	global runclient
	runclient = True
	if len(donework) > 0:
		endwithpause = True
		writeOutput("souhrn:\n")
		for line in donework:
			writeOutput("\t"+line+"\n")
	
	if len(problems) == 0:
		if not pretend:
			workingfilepath = os.path.join(config.tempdir, "working")
			if os.path.exists(workingfilepath):
				os.remove(workingfilepath)
	else:
		runclient = False
		writeOutput("nalezene problemy:\n")
		for line in problems:
			writeOutput(line+"\n")
		writeOutput("\n")
		
	if not os.path.exists(config.runaftersuccess):
		writeOutput("cesta ke klientovi tak jak je nastavena v souboru config.xml neexistuje.\n")
		runclient = False
		writeOutput("\n")
	
	if runclient:
		writeOutput("Zmacknutim Ok spustite klienta...\n")
		
#hacked-in simple proxy support 
class HTTPPageDownloader_Proxy(HTTPPageDownloader):
	def connectionMade(self):
		method = getattr(self.factory, 'method', 'GET')
		
		if (config.useproxy):#this is the only change here, it surprisingly works ;)
			self.sendCommand(method, self.factory.url)
		else:
			self.sendCommand(method, self.factory.path)
		
		self.sendHeader('Host', self.factory.headers.get("host", self.factory.host))
		self.sendHeader('User-Agent', self.factory.agent)
		if self.factory.cookies:
			l=[]
			for cookie, cookval in self.factory.cookies.items():  
				l.append('%s=%s' % (cookie, cookval))
			self.sendHeader('Cookie', '; '.join(l))
		data = getattr(self.factory, 'postdata', None)
		if data is not None:
			self.sendHeader("Content-Length", str(len(data)))
		for (key, value) in self.factory.headers.items():
			if key.lower() != "content-length":
				# we calculated it on our own
				self.sendHeader(key, value)
		self.endHeaders()
		self.headers = {}
		
		if data is not None:
		    self.transport.write(data)
		
class FileDownloader(HTTPDownloader):
	downloadedLength = 0
	protocol = HTTPPageDownloader_Proxy
	
	def gotHeaders(self, headers):
		#self.lengthToDownload = int(headers.get("content-length")[0])        
		self.realLength = int(headers.get("content-length")[0])        
		if self.requestedPartial:
			contentRange = headers.get("content-range", None)
			if not contentRange:
				# server doesn't support partial requests, oh well
				self.requestedPartial = 0 
				return
			start, end, self.realLength = http.parseContentRange(contentRange[0])
			if start != self.requestedPartial:
				# server is acting wierdly
				self.requestedPartial = 0
			else:
				#self.lengthToDownload = end - start
				self.downloadedLength = start
			
	def pageStart(self, partialContent):
		ui.progressBar.start()
		HTTPDownloader.pageStart(self, partialContent)

	def pagePart(self, data):
		self.downloadedLength += len(data)
		#print self.downloadedLength, "/", self.realLength
		ui.progressBar.update(self.downloadedLength / float(self.realLength))
		#self.progmeter.update(self.downloadedLength)
		HTTPDownloader.pagePart(self, data)
		
	def pageEnd(self):
		ui.progressBar.stop()
		HTTPDownloader.pageEnd(self)

def erroroccured(failure):
	writeOutput("Chyba: "+str(failure)+"\n")
	problems.append(str(failure))
	stopapplication()
	
def stopapplication():
	printSummary()
	
	writeOutput("Kliknete na [X] v rohu pro ukonceni")

def parsecmdline():
	opts, args = getopt.getopt(sys.argv[1:], "pf", ["pretend", "forcecheck"])
	global pretend, forcecheck
	pretend = False
	forcecheck = False
	for o, a in opts:
		if o in ("-p", "--pretend"):
			pretend = True
		if o in ("-f", "--forcecheck"):
			forcecheck = True
		#if o in ("-h", "-?", "--help"):

def obtainconfig():
	global config, ui
	if os.path.exists(CONFIGFILE):
		config = gnosis.xml.pickle.load(file(CONFIGFILE)) 
		ui = __import__(config.uiname+"_ui")
		ui.init(VERSION)
		
	else:
		import tk_ui as ui
		ui.init(VERSION)
		try:
			config = Config()
		except:
			reactor.stop()
			return
		gnosis.xml.pickle.dump(config, file(CONFIGFILE,"w"))

	config.tempdir = os.path.abspath(config.tempdir)
	config.backupdir = os.path.abspath(config.backupdir)
	config.uodir = os.path.abspath(config.uodir)

	if not hasattr(config, "useproxy"):
		config.useproxy = False
	
	if not hasattr(config, "deleteverdata"):
		config.deleteverdata = True
		
	if not hasattr(config, "allowselfupdate"):
		config.allowselfupdate = True
	
	if not os.path.exists(config.tempdir):
		os.makedirs(config.tempdir)
	
	if os.path.exists("bin"):
		os.chdir("bin")
		
	if config.dobackup:
		if not os.path.exists(config.backupdir):
			os.mkdir(config.backupdir)
			
			
	remove_noid_from_register()
	
	reactor.callLater(0, startUpdateinfoDownload)

commongetchecksum  = getchecksum
def getchecksum(filepath):
	ui.progressBar.startactivity()
	retval = commongetchecksum(filepath)
	ui.progressBar.stopactivity()
	return retval
	

problems = [] #if this has len > 0, the "working" wont be deleted and the app can be normally restarted without being stopped at "nothing new"
donework = []

def main():
	global runclient, logfile
	runclient = False
	
	logfile = file(LOGFILE, "a")
	y,mon,d,h,min, iigg,nnoo,rree,daylight = time.localtime()
	#logfile.write(str(time.localtime()))
	logfile.write("\n\n%0.2d.%0.2d.%0.4d, %0.2d:%0.2d\n" %(d, mon, y, h, min))
	
	parsecmdline()
	
	reactor.callLater(0, obtainconfig)
	
	reactor.run()
	
	ui.destroy()
	
	if runclient and ui.allowRunClient:
		os.startfile("\""+config.runaftersuccess+"\"")
	
	logfile.write("\n")
	logfile.close()
		
if __name__ == "__main__":
	main()