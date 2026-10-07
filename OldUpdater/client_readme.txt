AUTOUPDATER
-----------

co to je? 
---------------
	Program slouzici k automatizaci a "efektivizaci" stahovani modifikovanych souboru pro Ultimu Online (i kdyz neni v podstate  vubec specificky pro UO - teoreticky se da pouzit na cokoli)

jak to funguje? 
---------------
	Misto stahovani celych souboru pri kazde nove aktualizaci stahuje jen rozdilove soubory ("patche"), ktere jsou daleko mensi, a setri tak cas i objem prenesenych dat.
	Je automatizovan, takze prakticky vse co musite udelat je tu a tam ho spustit ("update.bat") a sledovat co se deje :)
	Po prvnim spusteni se vytvori soubor config.xml, kde muzete v pripade potreby zmenit nekolik nastaveni: dotycne adresare, jestli se soubory zalohuji, jestli se na konci program zapauzuje, apod.
	Prepinacem -p nebo --pretend zapnete "predstiraci" mod, tzn. ze se dovite co je potreba updatovat, ale nic se nestahuje ani nemaze... (tj. spustite program pres soubor "pretend.bat")
	V pripade ze neco provadite se svym UO adresarem a chcete pak (znovu) pouzit autoupdater, v pripade ze mezitim nevysla nova verze dat se nic nestane. Proto bud smazte obsah adresare "temp" v adresari updateru, nebo ho spustte s prepinacem -f nebo --forcecheck (jinymi slovy, pustte "forceupdate.bat")

co kdyz nastane problem?
---------------
	Muze se stat ze se prihodi nejaka chyba, nejspis se pozna tak ze se vypisou jakysi errory na vystupu. Obecny postup pro reseni takovych problemu je smazani obsahu adresare temp. Pokud se to tim nevyresi, a mate pocit ze problem je skutecne v programu, dejte vedet na foru morie (http://moria.ultima.cz/)

je to bezpecne? 
---------------
	Program operuje jen v adresari UO, ve svem, pracovnim ("temp") a zalohovacim ("backup") adresari. Nezname soubory z UO zalohuje, nicmene doporucuji mit cokoli co mate v tomto adresari a je vam drahe, zalohovat zvlast.

jak to funguje "uvnitr"?
---------------
	Program je napsan v Pythonu (www.python.org) a pomoci utility py2exe preveden do podoby ve ktere nepotrebuje k provozu nainstalovany python runtime. Vsechen kod je i s knihovnimi moduly uskladnen v archivu library.zip, coz take navic umoznuje efektivni updatovani sama sebe.
	Pokud netusite o cem tenhle odstavec byl, tak si z toho nic nedelejte, nejspis vas to stejne nezajima.

Licence:
---------------
	V pripade ze by mel nekdo zajem provadet s programem neco jinyho nez ho pouzivat k updatovani morijskych souboru (jako treba obdivovat zdrojaky :), necht mne kontaktuje, jinak je cokoli jinyho zakazano :P
	
	Program pouziva commandlinovy utility 7zip (rozbalovani, GNU LGPL licence), JojoDiff (binarni diff/patch, GNU GPL licence), rm a mv z jakychsi UnxUtilsDist (mazani a presouvani souboru - windowsovy utility furt votravujou s readonly flagem, licence asi taky GNU GPL, ale kdo vi :)

Thanks to:
---------------
	Testers: Blekota, Rem, Donnald, Bubbo
	
Credits:
---------------
	Tartaros - program
	Vypravec - grafika