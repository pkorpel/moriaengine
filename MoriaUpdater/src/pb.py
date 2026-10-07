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


def fileprogressbar(filesize):
    import progressbar
    pbwidgets = [progressbar.Percentage(),' done at ', progressbar.FileTransferSpeed(), 
                 ', ', progressbar.ETA(), ' <<<', progressbar.Bar(), '>>> ']
    pb = progressbar.ProgressBar(widgets = pbwidgets, maxval=filesize)
    pb.start()
    return pb

if __name__=='__main__':
    pbar = fileprogressbar(10000000)
    # maybe do something
    pbar.start()
    for i in range(2000000):
        # do something
        pbar.update(5*i+1)
    pbar.finish()
    print