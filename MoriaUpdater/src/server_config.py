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

# THIS IS Moriaupdaterserver CONFIGURATION FILE
# YOU CAN PUT THERE SOME GLOBAL VALUE
# Do not touch until you know what you're doing.
# you're warned :)

# where your project will head for your data (for instance, images and ui files)
# by default, this is ../data, relative your trunk layout

import os

__moriaupdaterserver_data_directory__ = '../data/'

options = None #optparse parsed object


def bequiet():
    if (options):
        return options.quiet
    return False

def beverbose():
    if (options):
        return options.verbose
    return False


class project_path_not_found(Exception):
    pass

def parsecmdline():
    import optparse
    parser = optparse.OptionParser(usage = "%prog [options] ftproot", version="%prog %ver", add_help_option=True)
    parser.add_option("-v", "--verbose", action="store_true", help="Show debug messages") #dest="verbose", 
    parser.add_option("-q", "--quiet", action="store_true", help="Show only error messages")
    parser.set_description("ftproot - path to the root of the folder that's visible from the outside world for clients (as ftp or http)")
    global options
    (options, args) = parser.parse_args()
    return (options, args, parser)

def getdatapath():
    """Retrieve moriaupdaterserver data path

    This path is by default <moriaupdaterserver_lib_path>/../data/ in trunk
    and /usr/share/moriaupdaterserver in an installed version but this path
    is specified at installation time.
    """

    # get pathname absolute or relative
    if __moriaupdaterserver_data_directory__.startswith('/'):
        pathname = __moriaupdaterserver_data_directory__
    else:
        pathname = os.path.dirname(__file__) + '/' + __moriaupdaterserver_data_directory__

    abs_data_path = os.path.abspath(pathname)
    if os.path.exists(abs_data_path):
        return abs_data_path
    else:
        raise project_path_not_found


if __name__ == "__main__":
    #write out command line options
    (options, args, parser) = parsecmdline()
    global _parser
    print "usage string: ", parser.get_usage()
    print "parsed options: ", options
    print "remaining args: ", args
    
