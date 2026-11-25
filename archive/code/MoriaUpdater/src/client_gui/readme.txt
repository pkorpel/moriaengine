ui.tcl je ve skutecnosti neco jako projektovej soubor 
pro "visual tcl" resp. "page", kterej se otevre a na zaklade toho 
se vygeneruje python, kterej se pak uklada do ui.py


pouze se tam musi pridat tenhle kousek kodu, bo samotnej TK neumi png 
(a pridavame vyhledavani obrazku podle toho v jaky vyvojovy fazi je program)



from PIL import ImageTk 

def PhotoImage(filename):
    import os
    if os.path.exists(filename):
        return ImageTk.PhotoImage(filename)
    else:
        p = os.path.join(os.path.join("bin", filename))
        if os.path.exists(p):
            return ImageTk.PhotoImage(p)
        else:
            p = os.path.join(os.path.join("../..", filename))
            return ImageTk.PhotoImage(p)