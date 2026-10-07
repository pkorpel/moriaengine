import os
from ConfigParser import ConfigParser, DEFAULTSECT

from client_utils import get_uodir_from_registry 


CONFIG_FILENAME = "MoriaUpdater.conf"




config = ConfigParser({
					'UseProxy': 'False',
					'Proxy':'http://yourproxy.com',
					'UoPath':get_uodir_from_registry(),
					'DeleteVerdata':'True',
					'ServerUrl':'http://tar.dyndns-server.com/~tar/moriaUpdaterStorage/',
					'LogLevel':'INFO'							
					})

config.read(CONFIG_FILENAME)

use_proxy = config.getboolean(DEFAULTSECT, 'UseProxy')
proxy = config.get(DEFAULTSECT, 'Proxy')
uo_path = os.path.abspath(os.path.normpath(config.get(DEFAULTSECT, 'UoPath')))
delete_verdata = config.getboolean(DEFAULTSECT, 'DeleteVerdata')
server_url = config.get(DEFAULTSECT, 'ServerUrl')
log_level = config.get(DEFAULTSECT, 'LogLevel')

with file(CONFIG_FILENAME, mode="w") as f:
	config.write(f)



if __name__ == '__main__':
	print "uo_path:", uo_path
	
	
	os.system("pause")