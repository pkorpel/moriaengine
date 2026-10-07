import sys
from twisted.internet import reactor

class ProgressBar:
	def start(self):
		pass #sys.stdout.write("\n")
		
	def update(self, complete):
		bar = '='*int(25 * complete)
		out = '\r%3i%% |%-25.25s|' % (complete*100, bar)
		sys.stdout.write(out)
		
	def stop(self):
		sys.stdout.write("\n")
		
	def startactivity(self):
		self.activitystate = 0
		self.active = True
		self.displayactivity()
		#sys.stdout.write("\n")
		
	def displayactivity(self):
		if (self.active):
			self.activitystate += 1
			case = self.activitystate % 4
			if (case == 0):
				sys.stdout.write('\r\\')
			elif (case == 1):
				sys.stdout.write('\r|')
			elif (case == 2):
				sys.stdout.write('\r/')
			else:
				sys.stdout.write('\r-')
			reactor.callLater(0.1, self.displayactivity)
	
	def stopactivity(self):
		self.active = False
		sys.stdout.write("\n")
	
	
def askstring(caption, prompt, initialvalue = ""):
	if (path != ""):
		print caption
		print "je toto spravne? (a/n)"
		if (raw_input(initialvalue+" ?").strip().lower() == "a"):
			return initialvalue
	print prompt
	s = raw_input().strip()
	return s

def init(version):
	global text, progressBar, allowRunClient
	allowRunClient = True
	print open("logo.txt").read()
	print "version", version
	text = sys.stdout
	progressBar = ProgressBar()
	
def destroy():
	pass