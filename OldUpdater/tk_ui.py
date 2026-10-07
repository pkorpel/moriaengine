from Tkinter import *
from ScrolledText import ScrolledText
from twisted.internet import tksupport,reactor
from tkSimpleDialog import askstring
import sys, os
from PIL import Image, ImageTk
import PIL.PngImagePlugin


def getImageFromFile(filename):
	if os.path.exists(filename):
		return ImageTk.PhotoImage(Image.open(filename))
	else:
		return ImageTk.PhotoImage(Image.open(os.path.join("bin", filename)))

class Mainframe(Frame):
	def __init__(self, *args, **params):
		## Standard heading: initialization
		Frame.__init__(self, *args, **params)
		self._parent = None
		if len(args) != 0: self._parent = args[0]
		## Widget creation
		
		filenameprefix = ""
		if os.path.exists("bin"):
			filenameprefix = "bin"
		self.backgroundphoto =  getImageFromFile("background.png")
		self.buttonphoto = getImageFromFile("OKbutton.png")
		
		#self._labelandtext = Frame(self)
		#self._labelandtext.pack(side = "top", expand = 1, fill = "both")
		
		
		self._label = Label(self, image=self.backgroundphoto)
		self._label.pack()
		
		self._text = ScrolledText(self, state=DISABLED) #, background="translucen) #, name='text#1', font='-*-MS Sans Serif-Medium-R-Normal-*-*-80-*-*-*-*-*-*') #, height=20, width=80)
		self._text.place(x=10, y=90, width=370, height=180)

		self._button_allowrun = Button(self, text="Ok", command=self.on_button_allowrun_press, 
			image=self.buttonphoto, borderwidth=0, highlightthickness=0, state="active")
		self._button_allowrun.place(x=382, y=255)

		self._progressBar = ProgressBar(self, width=280, height=15)
		self._progressBar.place(x=50, y=273)
		#self._progressBar.pack(side = "bottom", expand = 0, fill = "x")
		
		root.bind('<Key-Return>', self.on_button_allowrun_press)
		

		
		#self._progressBar.grid(column=1, row=2)
		
		## Resize behavior(s)
		#self.grid_rowconfigure(1, weight=2)
		#self.grid_columnconfigure(1, weight=2)
		
		self.bind("<Destroy>", self.on_destroy)
		
	def on_button_allowrun_press(self, arg=None):
		global allowRunClient
		allowRunClient = True
		reactor.stop()

	def on_destroy(self,event):
		reactor.stop()
		#sys.exit(0)
			
	def write(self,data): #stream method
		self._text.configure(state = NORMAL)
		self._text.insert(END,data)
		self._text.see(END) 
		self._text.configure(state=DISABLED)
		
	def flush(self): #stream method
		pass

class ProgressBar(Canvas):
	activitypositions = 8
	
	def __init__(self, master, height=25, fillColor="blue", **kw):
		Canvas.__init__(self, master, kw, height = height)
		self.master = master
		
		self.scale = self.create_rectangle(0, 0, 0, height, fill=fillColor)
		self.text = self.create_text(0, 0, anchor = CENTER)
	
	def start(self):
		pass
	
	def update(self, complete): #complete is a decimal number
		width, height = self.winfo_width(), self.winfo_height()
		self.coords(self.scale, 0, 0,
			complete * width, height)
		self.coords(self.text, width/2, height/2)
		percent = int(round(complete*100))
		self.itemconfig(self.text, text="%d %%" % percent)
		root.title("["+str(percent)+"%] "+mainName)
		self.update_idletasks()
		
	def stop(self):
		self._makeblank()
		
	def _makeblank(self):
		self.coords(self.scale, 0, 0, 0, 0)
		self.itemconfig(self.text, text="")
		root.title(mainName)
		
	def startactivity(self):
		self.activitystate = -1
		self.active = True
		reactor.callFromThread(self._displayactivity)
		#self._displayactivity()
	
	def _displayactivity(self):
		if (self.active):
			self.activitystate += 1
			opmax = ((self.activitypositions*2) - 2)
			case = self.activitystate % opmax
			if (case >  (self.activitypositions - 1)): case = opmax - case
			#
			width, height = self.winfo_width(), self.winfo_height()
			widthunit = width / self.activitypositions
			self.coords(self.scale, case * widthunit, 0,
				(case + 1) * widthunit, height)
			
			self.update_idletasks()
        	
			global root
			case = (self.activitystate/2) % 4
			if (case == 0):
				root.title('[ \\ ] '+mainName)
			elif (case == 1):
				root.title('[ | ] '+mainName)
			elif (case == 2):
				root.title('[ / ] '+mainName)
			else:
				root.title('[ - ] '+mainName)
			
			reactor.callLater(1, self._displayactivity)
	
	def stopactivity(self):
		reactor.callFromThread(self._setactivefalse)
		
	def _setactivefalse(self):
		self.active = False
		self._makeblank()


def init(version):
	global text, progressBar, mainName, root, allowRunClient
	root = Tk()
	root.resizable(False, False)
	tksupport.install(root)
	mainframe = Mainframe(root)
	mainframe.pack(side=TOP, fill=BOTH, expand=1)
	
	allowRunClient = False
	text = mainframe
	progressBar = mainframe._progressBar
	mainName = "MORIA updater v. "+version
	root.title(mainName)
	#sys.stdout = mainframe
	sys.stderr = mainframe

def destroy():
	root.destroy()