""" Deliver logger messages to logger window """
# $Id: LoggerToWindow.py,v 1.3 2004/04/06 03:48:14 prof Exp $
import logging
import facade


def attach(fmt='%(asctime)s %(message)s', datefmt='%H:%M:%S'):
    """ Attach handler to the logging system """
    h.setFormatter(logging.Formatter(fmt, datefmt))
    logging.getLogger().addHandler(h)


class LogToWindowHandler(logging.Handler):
    """ Provide a logging handler """
    
    def emit(self, record):
        """ Process a log message """
        try:
            facade.write_to_ui(self.format(record))
        except:
            self.handleError(record)


""" Create handler with formatter"""
h = LogToWindowHandler()



