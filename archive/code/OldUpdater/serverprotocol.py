from twisted.internet import reactor, protocol, threads
import sys, os, string

VERSION = "0.1.2"

USERS = {"uopatchgm" : "48uogm52"}

localpath = os.path.dirname(sys.argv[0])
logo = open(os.path.join(localpath, "logo.txt")).read()



class UpdaterProtocol(protocol.Protocol):
	username = ""
	
	mode = None
	
	def mode_awaitusername(self, data):
		self.username = data
		self.transport.write("password?:")
		self.mode = self.mode_awaitpassword
		
	def mode_awaitpassword(self, data):
		passw = data.lower().strip()
		username = self.username.lower().strip()
		if (USERS.has_key(username)):
			if (USERS[username].lower() == passw):
				self.mode = self.mode_loggedin
				self.write(logo)
				self.write("server version "+VERSION+"\n")
				
				return
		self.transport.write("wrong user/password, go away!")
		self.mode = self.mode_ignore
		reactor.callLater(1,self.transport.loseConnection)
				
	def mode_loggedin(self, data):
		commparser.command(data, self)
		
	def mode_ignore(self, data):
		pass
	
	def dataReceived(self, data):
		if self.mode != None:
			self.mode(data)
		else:
			self.transport.write("error.")
			self.transport.loseConnection()

	def connectionMade(self):
		 self.mode = self.mode_awaitusername
		 self.transport.write("username?:")
		 

	def connectionLost(self, reason = protocol.connectionDone):
		#print "connectionLost 4"
		self.factory.protocols.remove(self)
	
	def write(self, data):
		if self.transport.connected:
			if len(data)<6:
				data += "      "
			self.transport.writeSequence(data+"\n")
		
		
class CommParser:
	identchars = string.ascii_letters + string.digits + '_'
	working = False
	
	def do_restart(self, arg, protocol):
  		"""internally restarts the server, allowing it to reload the main script, effectively upgrading itself on the fly"""
  		protocol.write("stopping server. connection will now terminate.")
  		stop()
  		reactor.callLater(1, protocol.transport.loseConnection)
  	
	def do_unpack(self, arg, protocol):
		"""unpacks the file "release.rar" in the newest Release directory, or in a Release subdirectory given as argument"""
		self.makework(serverscript.unpackrelease, arg)
		
	def do_startscript(self, arg, protocol):
		"""starts the main script that creates the file/directory structure needed for the updater client..."""
		self.makework(serverscript.start, arg)
			
	def makework(self, func, arg):
		if not self.working:
			self.working = True
			d = threads.deferToThread(func, arg)
			d.addCallback(self.workdone)
			d.addErrback(self.workfailed)
		else:
			print "we are already working, wait..."
			
	def workdone(self, arg):
		print "work done"
		self.working = False
		
	def workfailed(self, failure):
		print "work done with errors"
		print failure
		self.working = False
	
	def command(self, data, protocol):
		cmd, arg, line = self.parseline(data)
		if not line:
			return None
		if cmd is None or cmd == "":
			return None
		try:
			func = getattr(self, 'do_' + cmd)
		except AttributeError:
			return self.default(line,protocol)
		return func(arg,protocol)  
  		
	def parseline(self, line):
		line = line.strip()
		if not line:
			return None, None, line
		elif line[0] == '?':
			line = 'help ' + line[1:]
		i, n = 0, len(line)
		while i < n and line[i] in self.identchars: i = i+1
		cmd, arg = line[:i], line[i:].strip()
		return cmd, arg, line
  		
	def do_help(self, arg, protocol):
		"""displays a list of available commands. can help about specific command passed as argument"""
		if arg: # XXX check arg syntax
			try:
				doc = getattr(self, 'do_' + arg).__doc__
				if doc:
					protocol.write(doc)
					return
			except:
				protocol.write("Unknown command "+arg)
			return
		else:
			names = self.get_names()
			prevname = ''
			names.sort()
			# There can be duplicates if routines overridden
			for name in names:
				if name[:3] == 'do_':
					if name == prevname:
						continue
					prevname = name
					protocol.write(name[3:]+": "+str(getattr(self, name).__doc__))
				
	def get_names(self):
		# Inheritance says we have to look in class and
		# base classes; order is not important.
		names = []
		classes = [self.__class__]
		while classes:
			aclass = classes.pop(0)
			if aclass.__bases__:
				classes = classes + list(aclass.__bases__)
			names = names + dir(aclass)
		return names
				
	def default(self, arg, protocol):
		protocol.write("Unknown command "+arg)

class Redirector:
	def __init__(self, outstreams):
		self.outstreams = outstreams
		
	def write(self, data):
		reactor.callFromThread(self.write_unsafe, data)
		
	def write_unsafe(self, data):
		for stream in self.outstreams:
			stream.write(data)


class Factory(protocol.ServerFactory):
	
	def __init__(self):
		self.protocols = []
		sys.stdout = Redirector(self.protocols)
	
	def buildProtocol(self, addr):
		p = UpdaterProtocol()
		p.factory = self
		self.protocols.append(p)
		return p


def start():
	global serverscript, commparser, listener
	serverscript = absoluteimport("serverscript")
	commparser = CommParser()
	listener = reactor.listenTCP(9876, Factory())
	
def stop():
	listener.stopListening()
	reactor.callLater(1, coreRestart)