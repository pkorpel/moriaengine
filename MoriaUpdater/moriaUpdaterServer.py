#!/usr/bin/python
# -*- coding: utf-8 -*-
### BEGIN LICENSE
# Copyright (C) 2009 Jan Slavetinsky jslavetinsky@seznam.cz
#This program is free software: you can redistribute it and/or modify it 
#under the terms of the GNU General Public License version 3, as published 
#by the Free Software Foundation.
#
#This program is distributed in the hope that it will be useful, but 
#WITHOUT ANY WARRANTY; without even the implied warranties of 
#MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR 
#PURPOSE.  See the GNU General Public License for more details.
#
#You should have received a copy of the GNU General Public License along 
#with this program.  If not, see <http://www.gnu.org/licenses/>.
### END LICENSE
import datetime, sys, os, logging, optparse

# Check if we are working in the source tree or from the installed 
# package and mangle the python path accordingly
if os.path.dirname(sys.argv[0]) != ".":
    if sys.argv[0][0] == "/":
        fullPath = os.path.dirname(sys.argv[0])
    else:
        fullPath = os.getcwd() + "/" + os.path.dirname(sys.argv[0])
else:
    fullPath = os.getcwd()
sys.path.insert(0, os.path.dirname(fullPath))


import src.server_config as config


if __name__ == "__main__":
    #support for command line options
    (options, args, _parser) = config.parsecmdline()    
    
    #set the logging level to show debug messages
    import logging
    if options.verbose:
        logging.basicConfig(level=logging.DEBUG)
        logging.debug('logging enabled - set to debug')
    elif options.quiet:
        logging.basicConfig(level=logging.WARNING)
        logging.debug('logging enabled - set to warning')       
    else:
        logging.basicConfig(level=logging.INFO)
        logging.debug('logging enabled - set to info')
    
    #run the application
    from src import server
    if (len(args) == 0):
        server.main(".")
    elif (len(args) == 1):
        server.main(args[0])
    else:
        parser.error("incorrect number of arguments")

