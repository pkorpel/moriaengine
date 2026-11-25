from distutils.core import setup
import py2exe


#includes = ["bsdiff"]

setup(
#    version = "0.5.0",
#    description = "py2exe user-access-control sample script",
#    name = "py2exe samples",
    
    options = {"py2exe": {#"compressed": 1,
                          #"optimize": 2,
                          #"ascii": 1,
                          #"bundle_files": 1
						  #"skip_archive":True
						  #"data_files":[('', ['background.png'])],
						  #"includes": includes,
						  }},
    #zipfile = None,
	#console
	windows = [dict(script="moriaUpdaterClient.py",
          icon_resources = [(1, "icon.ico")],
          dest_base="MoriaUpdater"
          ,uac_info="requireAdministrator"
		  )]
    )



