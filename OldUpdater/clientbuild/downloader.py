import os, urllib

from twisted.internet import reactor, threads, defer
from twisted.protocols import http
from twisted.web.client import HTTPDownloader, HTTPPageDownloader


def downloadfile(filename, c, checksum=None):
	global filenamebeingdownloaded, checksumbeingdownloaded, pendingDownloadDeferred, config
	config = c
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

