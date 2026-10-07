import os

import _winreg

try:
	from bspatch import patch
except ImportError:
	from pybspatch import patch #pure python - slower



def get_uodir_from_registry():
	key = _winreg.OpenKey(_winreg.HKEY_LOCAL_MACHINE, "SOFTWARE\\Origin Worlds Online\\Ultima Online\\1.0")
	# | _winreg.KEY_WOW64_32KEY
	try:
		return _winreg.QueryValueEx(key, "InstCDPath")[0]
	except:
		pass		
	finally:
		key.Close()
		
		
		
		
		
		
		
		
		
		
		
		
		
if __name__ == '__main__': 
	print "path to UO:", get_uodir_from_registry()
	os.system("pause")