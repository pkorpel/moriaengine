import Queue
import threading
import logging

import gui
import guiLoggingHandler

MESSAGE_LOG = "log"
MESSAGE_PROGRESS_CURRENT = "progress_current"
MESSAGE_PROGRESS_OVERALL = "progress_overall"

messages = Queue.Queue()

PBMAX = 99.99 #progressbar maximum

def start_gui(callback):
    t = threading.Thread(target=_wrap_calc, args=(callback,))
    t.daemon=True
    t.start()    
    
    guiLoggingHandler.attach()
    gui.start()
    

def _wrap_calc(target):
    try:
        target()
        logging.info("Hotovo")
    except:
        logging.exception("Chyba:")


def get_message():
    if messages.empty():   
        return (False, None, None)
    else:
        (msg, param) = messages.get()
        return (True, msg, param)

def write_to_ui(text):
    messages.put((MESSAGE_LOG, text))    

def set_progress_current(value):
    messages.put((MESSAGE_PROGRESS_CURRENT, value))
    
def set_progress_overall(value):
    messages.put((MESSAGE_PROGRESS_OVERALL, value))
    
    
    

def _fake_work():    
    import time
    time.sleep(1)
    
    set_progress_current(50)
    set_progress_overall(20)
    
    logging.info("info line")
    
    time.sleep(1)
    raise Exception("exception text")
    

if __name__ == '__main__': 
    logging.basicConfig(level=logging.INFO)
    
    start_gui(_fake_work)
