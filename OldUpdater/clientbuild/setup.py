from distutils.core import setup
import py2exe

#setup(console=["AutoUpdater.py"])

setup( 
    windows = [
        { 
            "script": "AutoUpdater.pyw", 
            "icon_resources": [(1, "icon.ico"), (2, "icon.ico")] 
        } 
    ], 
    console = [
        { 
            "script": "AutoUpdater_text.py", 
            "icon_resources": [(1, "icon.ico"), (2, "icon.ico")] 
        } 
    ], 
)