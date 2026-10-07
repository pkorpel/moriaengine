import os 
import shutil

import urllib
from urlparse import urlparse, urljoin
from tempfile import mkdtemp
import logging
from zipfile import ZipFile, ZIP_DEFLATED

import gnosis.xml.pickle

import update_info as ui
import utils
from client_gui import facade
import client_config as config

usingpybspatchimpl = False
try:
	from bspatch import patch
except ImportError:
	usingpybspatchimpl = True
	from pybspatch import patch

if (config.use_proxy):	
	schema = urlparse(config.proxy)
	proxies = {schema: config.proxy}
	urllib._urlopener = urllib.FancyURLopener(proxies)
	

def main(arg=None):	
	log_level = logging.getLevelName(config.log_level)
	logging.basicConfig(level=log_level, filename="log.txt")	
	facade.start_gui(work)	
	
#fractions of download progress
PERCENTAGE_DOWNLOAD_WITHOUT_CHECKSUM=0.9
PERCENTAGE_UNPACK_WITHOUT_CHECKSUM=0.8

#first phase - getting updateinfo file
TOTALPERCENTAGE_UI = 0.01 * facade.PBMAX
TOTALPERCENTAGE_UICHECKSUM_DOWNLOAD = 0.1 * TOTALPERCENTAGE_UI
TOTALPERCENTAGE_UIFILE_DOWNLOAD = 0.5 * TOTALPERCENTAGE_UI
TOTALPERCENTAGE_UIFILE_UNPACK = 0.3	* TOTALPERCENTAGE_UI
TOTALPERCENTAGE_UIFILE_READ = 0.1 * TOTALPERCENTAGE_UI

TOTALPERCENTAGE_TODOLIST = 0.01 * facade.PBMAX

TOTALPERCENTAGE_CHECKSUMALL = 0.20 * facade.PBMAX

#downloading, unpacking, patching - vyjadreno jako pomer
TIMEPERCENTAGE_DOWNLOADING = 5
TIMEPERCENTAGE_UNPACKING = 3
TIMEPERCENTAGE_PATCHING = 4

	
def work():
	if usingpybspatchimpl:
		logging.debug("using pybspatch")
	
	tempdir = mkdtemp()
	
	try:
		totaldone = 0 
		
		logging.info("UO instalace: '"+config.uo_path+"'")
		
		
		logging.info("Stazeni metainfo souboru...")
		#download ui archive checksum file
		uiarchivechecksum_filename = utils.FILENAME_UPDATEINFO + utils.EXTENSION_ARCHIVE + utils.EXTENSION_CHECKSUM
		totaldone = download(tempdir, uiarchivechecksum_filename, TOTALPERCENTAGE_UICHECKSUM_DOWNLOAD, totaldone)
		with open(os.path.join(tempdir, uiarchivechecksum_filename), "r") as f:
			uiarchivechecksum = f.read()
		
		totaldone = download(tempdir, utils.FILENAME_UPDATEINFO + utils.EXTENSION_ARCHIVE, TOTALPERCENTAGE_UIFILE_DOWNLOAD, totaldone, uiarchivechecksum)
		totaldone = unpack(tempdir, utils.FILENAME_UPDATEINFO, TOTALPERCENTAGE_UIFILE_UNPACK, totaldone)
		
		with open(os.path.join(tempdir, utils.FILENAME_UPDATEINFO), "r") as f:
			uiobj = gnosis.xml.pickle.load(f)
		totaldone += TOTALPERCENTAGE_UIFILE_READ
		facade.set_progress_overall(totaldone)
		
		logging.info("Vytvoreni seznamu souboru ke zpracovani...")
		#list files that need to be processed
		todo = [] #list of (filename, checksum, fileinfo)
		totalsize = 0
		for filename in utils.listfiles_recursive(config.uo_path):
			fi = ui.ui_popfilebyname(uiobj, filename)
			if not (fi is None): 
				path_in_uo = os.path.join(config.uo_path, filename)
				size = os.path.getsize(path_in_uo)		
				totalsize += size
				todo.append((path_in_uo, float(size), fi))
		
		for fi in uiobj.files.values():
			if fi.forced and not fi.todelete:
				path_in_uo = os.path.join(config.uo_path, fi.versions_sorted[-1].filename)
				todo.append((path_in_uo, None, fi))
								
		totaldone += TOTALPERCENTAGE_TODOLIST
		facade.set_progress_overall(totaldone)
		
		logging.info("Souboru ke zpracovani: "+str(len(todo)) + ", celk. velikost: " + str(totalsize / (1024*1024)) + "MB")
		
		logging.info("Kalkulace kontrolnich souctu souboru...")
		
		todownload = [] #(path_in_uo, filesize, fi)
		downloadsize = 0
		topatch = [] #(path_in_uo, filesize, fi, version)
		patchfilessize = 0
		todelete = [] #path_in_uo
		for (path_in_uo, filesize, fi) in todo:
			if fi.todelete:
				todelete.append(path_in_uo)
			else:
				if filesize:
					sizefraction = filesize/totalsize
					TOTALPERCENTAGE_CHECKSUMALL
					def progress_callback(done):
						fraction = (done/filesize)
						facade.set_progress_current(facade.PBMAX * fraction)
						facade.set_progress_overall(totaldone + (sizefraction * fraction * TOTALPERCENTAGE_CHECKSUMALL))
					
					checksum = utils.calculate_checksum(path_in_uo, progress_callback) 
					totaldone += sizefraction * TOTALPERCENTAGE_CHECKSUMALL
				elif os.path.exists(path_in_uo): #size 0 but it does exist
					checksum = utils.calculate_checksum(path_in_uo, None) 
				else:
					checksum = 0
				
				latestversion = fi.versions_sorted[-1]
				if (checksum != latestversion.checksum): #nemame spravnou verzi, musime resit
					vQueue = []
					encountered = False
					size = 0
					for v in fi.versions_sorted: #successive patches
						if not encountered:
							if (v.checksum == checksum):
								encountered = True
						if encountered:
							vQueue.append(v)
							patchfilessize += v.patchsize
							size += v.patchsize
							
					if not encountered: #not found as successive, patch right from the original
						for v in fi.versions_byname.values():
							if v.isoriginal and checksum == v.checksum:
								vQueue.append(v)
								vQueue.append(latestversion)
								patchfilessize += v.patchsize
								size += v.patchsize
								break
												
					if vQueue:
						topatch.append((path_in_uo, size, fi, vQueue))
					else:
						todownload.append((path_in_uo, filesize, fi))
						downloadsize += latestversion.archivesize
		
		if todownload or topatch or todelete:
			megabytestodownload = downloadsize/1024.0/1024.0
			template = "Souboru ke stazeni: {0}, k patchnuti:{1}, ke smazani:{2}. Celkovy download: {3:.1f} MB"
			logging.info(template.format(len(todownload), len(topatch), len(todelete), megabytestodownload))			
		else:
			logging.info("Vsechny soubory v aktualni verzi.")
			facade.set_progress_overall(facade.PBMAX)
		
		
		totalwork = TIMEPERCENTAGE_DOWNLOADING * (downloadsize + patchfilessize) + \
			TIMEPERCENTAGE_UNPACKING * downloadsize + TIMEPERCENTAGE_PATCHING * patchfilessize
		if totalwork > 0:
			fraction_per_byte = (facade.PBMAX - totaldone) / totalwork
		else:
			fraction_per_byte = 0
		
		
		tomove = [] #(path_downloaded, path_in_uo, filesize)
			
		#download
		for (path_in_uo, filesize, fi) in todownload:
			latestversion = fi.versions_sorted[-1]
			
			#download archive
			remote_path = latestversion.versionname + '/' + latestversion.filename + utils.EXTENSION_ARCHIVE
			fraction = latestversion.archivesize * fraction_per_byte * TIMEPERCENTAGE_DOWNLOADING
			totaldone = download(tempdir, remote_path, fraction, totaldone, latestversion.archivechecksum)
			
			#archive downloaded, now unpack
			path_downloaded = os.path.join(latestversion.versionname, latestversion.filename)
			fraction = latestversion.archivesize * fraction_per_byte * TIMEPERCENTAGE_UNPACKING
			totaldone = unpack(tempdir, path_downloaded, fraction, totaldone, latestversion.checksum)
			
			tomove.append((os.path.join(tempdir, path_downloaded), path_in_uo, filesize))
			
		#patch
		for (path_in_uo, filesize, fi, vQueue) in topatch:
			#latestversion = fi.versions_sorted[-1]
			#download patch file
			previous_version_result = path_in_uo
			for i in range(len(vQueue) - 1):
				prev_version = vQueue[i]
				next_version = vQueue[i + 1]
				
				patchname = utils.get_patchname(prev_version, next_version)
				remote_path = utils.DIRNAME_PATCHES + '/' + patchname
				local = os.path.join(tempdir, patchname)
				if not os.path.exists(local):
					os.makedirs(local)
				
				fraction = prev_version.patchsize * fraction_per_byte * TIMEPERCENTAGE_DOWNLOADING
				totaldone = download(local, remote_path, fraction, totaldone, prev_version.patchchecksum)
				path_downloaded = os.path.join(local, utils.DIRNAME_PATCHES, patchname)
				newPath = os.path.join(local, "patched")
				
				logging.debug("patching file '" + previous_version_result + "' to '" + newPath + "' using '" + path_downloaded + "'")
				patch(previous_version_result, newPath, path_downloaded)
				previous_version_result = newPath
				fraction = prev_version.patchsize * fraction_per_byte * TIMEPERCENTAGE_PATCHING				
				totaldone += fraction
				facade.set_progress_overall(totaldone)
			
			tomove.append((previous_version_result, path_in_uo, os.path.getsize(previous_version_result)))
			
		if todelete:
			logging.info("Smazani nadbytecnych souboru v UO slozce...")
			for f in todelete:
				if os.path.isdir(f):
					logging.debug("deleting directory '"+f+"'")
					shutil.rmtree(f)
				else:
					logging.debug("deleting file '"+f+"'")
					os.remove(f)
			
		if tomove:
			logging.info("Presunuti souboru z temporary lokace do UO slozky...")
			for (source, target, size) in tomove:
				dirname = os.path.dirname(target)
				if not os.path.exists(dirname):
					os.makedirs(dirname)
				shutil.move(source, target)
			
	finally:
		shutil.rmtree(tempdir)
		#pass
		
		
	
#	facade.set_progress_current(50)
#	facade.set_progress_overall(20)

def download(localroot, remote_filepath, totalfraction, totaldone, checksum = None):
	logging.debug("downloading file '" + remote_filepath + "'")

	if (checksum):
		percentage_download = PERCENTAGE_DOWNLOAD_WITHOUT_CHECKSUM
	else:
		percentage_download = 1.0
		
	def reporthook(blocknum, blocksize, totalsize):
		#print "Block number: %d, Block size: %d, Total size: %d" % (blocknum, blocksize, totalsize)
		totalsize = float(totalsize)
		fraction = ((blocknum * blocksize)/totalsize)
		if (fraction > 1):
			fraction = 1.0

		fraction *= percentage_download		
		facade.set_progress_current(facade.PBMAX * fraction)
		facade.set_progress_overall(totaldone + (totalfraction * fraction))
	
	remote_filepath.replace('\\', '/')
	url = urljoin(config.server_url, remote_filepath)
	local_filepath = os.path.join(localroot, remote_filepath)
	
	d = os.path.dirname(local_filepath)
	if not os.path.exists(d):
		os.makedirs(d)
	
	(downloaded_filepath, _) = urllib.urlretrieve(url, local_filepath, reporthook)
	if downloaded_filepath != local_filepath:
		raise Exception("File downloaded to unexpected location")
		
	if not (checksum is None):
		filesize = float(os.path.getsize(local_filepath))
		totaldone_sofar = totaldone + (totalfraction * percentage_download)
		currentdone = facade.PBMAX * percentage_download
		
		def progress_callback(done):
			fraction = (done/filesize) * (1 - percentage_download)
			facade.set_progress_current(currentdone + (facade.PBMAX * fraction))
			facade.set_progress_overall(totaldone_sofar + (totalfraction * fraction))
			
		calculated = utils.calculate_checksum(local_filepath, progress_callback)
		raise_if_incorect_checksum(checksum, calculated)
	
	return totalfraction + totaldone


def unpack(localroot, filepath, totalfraction, totaldone, checksum = None):
	if (checksum):
		percentage_unpack = PERCENTAGE_UNPACK_WITHOUT_CHECKSUM
	else:
		percentage_unpack = 1.0
	
	targetpath = os.path.join(localroot, filepath)
	targetfilename = os.path.basename(targetpath)
	archivepath = targetpath + utils.EXTENSION_ARCHIVE

	block_size = 1024 * 1024

	z = ZipFile(archivepath)
	try:
		entry_info = z.getinfo(targetfilename)
		size = float(entry_info.file_size)
	
		i = z.open(targetfilename)
		with open(targetpath, 'wb') as o:
			try:
				offset = 0
				while True:
					b = i.read(block_size)
					l = len(b)
					if l == 0:
						break
					offset += l 
					fraction = (offset/size)
					facade.set_progress_current(fraction * facade.PBMAX)
					facade.set_progress_overall(totaldone + (fraction * totalfraction))
					o.write(b)
			finally:	
				i.close()
	finally:
		z.close()
			
	if not (checksum is None):
		filesize = float(os.path.getsize(targetpath))
		totaldone_sofar = totaldone + (totalfraction * percentage_unpack)
		currentdone = facade.PBMAX * percentage_unpack
		
		def progress_callback(done):
			fraction = (done/filesize) * (1 - percentage_unpack)
			facade.set_progress_current(currentdone + (facade.PBMAX * fraction))
			facade.set_progress_overall(totaldone_sofar + (totalfraction * fraction))
			
		calculated = utils.calculate_checksum(targetpath, progress_callback)
		
		raise_if_incorect_checksum(checksum, calculated)
			
	return totalfraction + totaldone

def raise_if_incorect_checksum(checksum, calculated):
	if not checksum == calculated:
		raise Exception("Incorrect checksum. Run the updater again.")
	
if __name__ == '__main__': 
	main()