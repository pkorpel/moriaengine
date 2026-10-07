import sys, os, imp, string, md5
import gnosis.xml.pickle
from twisted.internet import reactor, protocol, threads



def absoluteimport(modulename):
	descriptor = imp.find_module(modulename, [os.path.dirname(sys.argv[0])])
	filepath = descriptor[0].name
	f = open(filepath)
	try:
		return imp.load_module(modulename, f, descriptor[1], descriptor[2])
	finally:
		f.close()
		
def coreRestart():
	m = absoluteimport("serverprotocol")
	m.absoluteimport = absoluteimport #importing the import function :)
	m.coreRestart = coreRestart #this function
	m.start()


coreRestart()
reactor.run()
	




















