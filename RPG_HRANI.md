# Havranov – Strážce vesnice

Hratelný prototyp RPG v Unity 6.6. Spusť Play a v hlavní nabídce zvol **Hrát**.
Po stisku **Hrát** zvol **Nová hra** nebo **Načíst hru**. Nová hra otevře výběr vzhledu a začne s čistým postupem, původní výbavou a plným zdravím i výdrží. Muž a žena mají stejné herní vlastnosti (na začátku 100 zdraví a 100 výdrže). Nový začátek přepíše hlavní i záložní uložení; starý postup se načítá jen volbou **Načíst hru**.

## Ovládání

| Klávesa | Akce |
| --- | --- |
| WASD / šipky | Pohyb |
| Shift | Běh, spotřebovává výdrž |
| J | Zahájit tahový souboj s blízkým nepřítelem |
| C | Dovednosti: odemčení za body z úkolů |
| E | Rozhovor / sebrání předmětu v dosahu |
| H | Vypít léčivý lektvar |
| I | Batoh a vybavení |
| Q | Deník úkolu |
| Esc | Pauza / zavření dialogu |
| F5 | Uložení |
| F9 | Načtení poslední uložené hry |

Interakce s postavami vyžaduje přiblížení na 1 herní jednotku; běžné předměty
jsou dosažitelné do 0,75 jednotky. Vstupy do budov a lokací mají vlastní dosah
1,35 jednotky.

## Herní průchod

1. Na náměstí promluv s rychtářem Radovanem a přijmi úkol.
2. U Elišky nakup lektvary nebo vylepši zbroj. Mira léčí. Železný meč koupíš u kováře Mikuláše až od druhého cyklu za 40 zlata; do té doby bojuješ beze zbraně.
3. Truhly se objevují během úkolu na hledání zásob.
4. Poraz pět nájezdníků. Úder nepřítele je předem signalizován červenou barvou.
5. Na severu se objeví velitel Vranek. Poraz ho a vrať se k Radovanovi pro odměnu.
6. Po splnění lze pokračovat v průzkumu. Po smrti tě Mira zachrání za ztrátu až 10 zlata.

Úrovně a XP se nepoužívají. Za dokončený úkol získáš jeden bod, který slouží pouze k jednorázovému odemčení dovednosti. Vybavení lze vylepšit pětkrát.
Dialogy a inventář pozastavují pohyb i nepřátelskou AI.

## Vedlejší úkoly

V rozhovoru s NPC zvol tlačítko **Úkol**. Deník na Q ukazuje postup a podrobnosti.

| Úkol | Zadavatel a cíl | Odměna |
| --- | --- | --- |
| Ztracené zásoby | Radovan: najdi dvě oranžové bedny severně od náměstí a na východní cestě k mostu. | 60 zlata, 60 XP |
| Zásilka pro strážného | Eliška: předej balíček Petrovi u východního kamenného mostu a vrať se. | 40 zlata, 35 XP, lektvar |

Odměnu vždy odevzdává původní zadavatel. Léčivé byliny, záhonek, bylinný úkol i výroba lektvarů byly odstraněny.
Vedlejší úkoly jsou dostupné i v již rozehrané hře a po dokončení hlavního příběhu.

## Ukládání

Soubor `village-rpg-save.json` se ukládá do Unity `Application.persistentDataPath`.
Ukládá se stav postavy, vybavení, postup úkolu, otevřené truhly, a poražení nepřátelé.
Přeživší nepřátelé se po načtení vracejí na svá místa s plným zdravím.
Uložení probíhá po důležitých událostech, každých 30 sekund a při odchodu.
Při přepisu vzniká záložní soubor `.bak`.

## Struktura

- `RpgState.cs`: serializovatelná data postavy, postup a půdorysy hlavních budov.
- `RpgGame.cs`: souboj, interakce, úkol, obchod, kořist, ukládání a vytvoření světa při načtení `SampleScene`.
- `RpgEnemy.cs`: pronásledování, signalizovaný útok, zranění a odměny.
- `RpgInterface.cs`: HUD, dialogy, batoh, deník, pauza a obrazovky smrti/vítězství.
- `PlayerMovement.cs`: ovládání, běh, okraje mapy, překážky a sledování kamery.
- `Resources/HeroStepB.png`: druhý krok chůze z dodané obrácené verze; oba kroky mají stejnou výšku a polohu chodidel.
- `Editor/RpgValidation.cs`: kontroly pravidel a integrační ověření ve spuštěné scéně.

## Kontroly v editoru

`Tools → RPG → Run Gameplay Checks` ověřuje pravidla postupu, transakce, serializaci a překážky.
`Tools → RPG → Run Live Gameplay Checks` ve spuštěném `SampleScene` ověřuje průchod bojem, odměnami, obchodem, léčením, smrtí a dialogy; původní stav obnoví.
Výsledky jsou v konzoli Unity a v `Temp/RpgValidation.txt` a `Temp/RpgLiveValidation.txt`.

## Rozsah prototypu

Obsahuje jednu vesnici, hlavní úkol, tři vedlejší úkoly, šest nájezdníků, velitele a čtyři NPC.
NPC a nepřátelé zatím používají barevné varianty stejného spritu; zvuk zatím chybí.
Chůze je povolená na vyznačené síti cest, náměstí a kamenném mostu. Budovy, stánky, voda mimo most a plochy mimo cesty jsou blokované. Kolize kontroluje celou dráhu i při úhybu, aby nešlo přeskočit střechu. Starší uložená pozice mimo cestu se přesune na nejbližší průchozí místo.
Nepřátelé mají lokální pronásledování, nikoli navigační síť pro cestu přes celou vesnici.

Hrdinové používají přesně dodané postavy: Image (2)/(3) pro holku, Image (4)/imagine5 pro kluka. Odstraněné je pouze pozadí, proporce zůstávají. Kód střídá oba kroky podle skutečně ušlé vzdálenosti a otáčí celý obrázek podle směru včetně diagonál. Výběr pohlaví zůstává v menu a v uložené hře. Kamera snímá mapu shora; perspektiva dodaných obrázků se nepřekresluje.

Mapa lokalit: M označí cestu ke dveřím. Obchod, sklep, knihovna, tajný archiv, zřícenina kostela, les a Temný les tvoří propojený svět. Přechod se potvrdí klávesou E před vstupem. Les se otevře od druhého cyklu. Staré bojové oblasti nejsou součástí hlavního příběhu.

NPC mají vlastní pixelové sprity nezávislé na hráči: Radovan vínový kabátec a čapku, Eliška zástěru a brašnu, Mira světlý šátek a košík bylin, Petr helmu, kopí a štít. Doprovod používá stejný vzhled jako příslušné NPC. Nájezdníci mají čtyři varianty kožené výstroje, velitel vlastní zbroj.
Hlavní menu používá dodaný obrázek nočního hradu MainMenuCastle.jpg přes celou plochu se zachováním poměru stran. Tlačítka mají vlastní 16×16 pixelový rámeček, Point filtrování, devítidílné roztažení a okamžité stavy najetí/stisku. Výběr postav zůstává funkční.

Postavy nyní používají schválený atlas HeroOverhead.png: pro každou postavu stoj na obou nohách, levý a pravý krok, s pohledem shora. Kód otáčí snímky podle směru chůze a zastaví střídání při stání či překážce. Při přiblížení k obchodnici, rychtáři nebo bylinkářce se jednou nabídne Vejít/Odmítnout. Interiér zobrazuje postavy a dialog; obchodnice používá dodaný ShopWomanPortrait.png. Odejít/Esc vrací pohled ven. Nové snímky byly vytvořeny vestavěným imagegen po výslovném souhlasu uživatele; zadání: zachovat vzhled, pohled přímo shora, stoj/levý/pravý krok, holka a kluk.

Obchod má samostatné pixelové pozadí ShopInterior.png. Na obrazovce jsou do pasu Arabella v červeném (vlevo) a Obchodnice v modrém (vpravo), průhledné PNG bez šachovnice. Dole je široký dialog se jménem aktuální mluvčí, střídáním replik a volbami Pokračovat / Zboží / Úkol / Odejít. Kontrolní náhled během hry: Tools > RPG > Preview Shop Dialogue (Ctrl+Shift+F7).

Vstup do místností se nabízí až do vzdálenosti 0,55 od označených dveří. Odmítnutí se znovu nabídne až po vzdálení nad 1,2. Po potvrzení hráč 0,7 sekundy dochází ke dveřím a 0,3 sekundy mizí, teprve pak se zobrazí interiér. Při odchodu se objeví zpět před dveřmi. Během vstupu je boj i ruční ovládání zablokované.

Oprava vstupů: jediný otevřený interiér je obchod v malém domě na severní straně náměstí (v původním pozadí). Vchod má pevné souřadnice, není navázán na NPC; žádné nové dveřní sprity se nekreslí. Radovan, Mira a Petr chodí po cestách a zastavují při práci či rozhovoru. Obchodnice je uvnitř a zadává Zásilku pro strážného.

Vylepšení hlavní postavy: C otevře osm jednorázových dovedností na dvou stránkách. Body pocházejí z úkolů (limity 1 / 3 / 4 za cyklus); zůstávají přes cykly stejně jako odemčené dovednosti. Nastavení obsahuje hlasitost, efekty, zobrazení HUD a ovládání. Kingdom se přehrává jednou po výběru postavy nebo při načtení příběhu; zdroj je Assets/Sounds/841635__tomentum__kingdom.wav a herní kopie Assets/Resources/Sounds/Kingdom.wav.

Hrdinové jsou opět o cca 12 % menší (měřítko 0,60). Malá mapa vpravo ukazuje modře hráče, červeně živé nepřátele a zlatě sledovaný úkol; kliknutím otevře velkou mapu. Směrová šipka a vzdálenost vedou k cíli vzdušnou čarou (nenahrazují navigaci po cestách). V Q lze sledovat hlavní nebo přijatý vedlejší úkol; volba se ukládá. Cíl reaguje na postup: doručení Petrovi přepne ukazatel zpět k obchodu, nasbírané byliny k Miře. Deník shrnuje odměny a po zvýšení úrovně se připomene nový bod vylepšení C.

Průchodnost celé vesnice: opravené rozpojené západní a severní cesty, doplněné severovýchodní a jižní spojnice a cesta kolem mlýna. Kolize menší postavy má půdorys 3 px a budovy 2 px rezervu; střechy zůstávají blokované, stejně tak tráva a voda mimo most. Automatická kontrola prochází po 4px vzorkovací síti všechny cesty a vyžaduje jejich spojení s náměstím.

Hlavní menu > Lokace > Testovací verze: samostatný test NPC, předmětů, mečů a zbrojí +0 až +5, vlků a netopýrů. Hráč má vlastní zlato, výbavu a osm bodů dovedností. E zkouší objekty, T/Esc otevírá nástroje. Test nečte ani nepřepisuje hlavní uložený příběh.

## Svíčky a první rituál
Průvodce Petr → děda Matěj → obchodnice Eliška → sklep. Ve sklepě jsou tři volby: **utéct** (bez svíček, lze se vrátit), **rychle skočit pro svíčky** (3 svíčky, ztráta až 20 HP; zůstane nejméně 1 HP), nebo **opatrně dojít** (3 svíčky bez zranění). Svíčky nelze získat opakovaně. V batohu i příběhovém deníku je jejich počet.

Vrať se k Petrovi a zvol **Mám svíčky • přijmout radu**. Průvodce tě pošle do **zříceniny kostela**; ukazatel cíle označí cestu. U trojúhelníkového obrazce stiskni **E** a potvrď rituál. Tři svíčky se postupně rozsvítí, obrazec zrudne, hrdina zemře a probudí se do **druhého cyklu**. Herní stav a předměty se obnoví na výchozí hodnoty; zůstane vzhled, počet úmrtí a vzpomínka na selhání. Druhý cyklus se automaticky uloží i do zálohy. V dalších cyklech se znovu hledají svíčky pro nový tvar rituálu.

Svíčky, rada průvodce i cyklus se ukládají. Starší uložení s nalezeným vzkazem navážou rozhovorem o svíčkách. Původní lístek a odměna 45 zlata / 60 XP jsou z této větve odstraněny. Kontroly: **Tools → RPG → Check Candle Ritual Story**.

Havranov je pokojná vesnice: obyvatelé nezačínají boj sami, ale hráč je může vyzvat k tahovému souboji v rozhovoru. Šest řemeslníků a sousedů chodí po cestách, zastavuje se u práce a nabízí rozhovor [E]. Do nových lokalit lze přejít přes zlaté značky [E]; hlavní větev vede do zříceniny kostela a nevyžaduje boj s vesničany. Nepřátelé patří do dalších výprav; v testu se boj zapíná výslovně.

## Jednotný světlý pixelový vzhled
Mapa, postavy, obchod a portréty sdílejí barevnou pixelovou paletu; zeleň je výraznější, kámen teplejší a voda tyrkysová. Rozložení mapy a kolize zůstávají stejné. Herní panely, dialogy, menu, archiv i nastavení používají společné rámečky. Zdraví ukazuje také deset srdíček; meč a lektvar mají vlastní sloty. Předměty ve světě mají ikony meče, zbroje, lektvaru, bylin nebo truhly. Úprava se aplikuje při spuštění, původní obrázky se nepřepisují.

## Detailní fantasy mapa a dodané směrové snímky
Aktuální mapa `Assets/Resources/FantasyVillage.png` nahrazuje pouze kresbu pozadí: zachovává světové rozměry 40 × 26,6667, cesty, domy i původní kolize. Vykresluje se v nativním rozlišení bez předchozího barevného a pixelizačního filtru. Hrdinka používá přesné dodané JPG pásy v `Assets/Resources/HeroDirectional/`: zepředu, zezadu a z boku; opačný bok se zrcadlí. Čtyři fáze chůze a klidová póza mají společnou velikost a nepřepisují nastavení pohybu. Čísla pod snímky se oříznou, okolní bílé pozadí odstraní kód. Kluk si zachovává své původní obrázky.

Petr Kříž je nyní občasný portrétní rádce. Při vstupu do hlavní hry se jeho uvítací okno vysune a text se postupně vypíše. Nechodí za hráčem a rada nevytváří fyzický doprovod; starší volba Petra jako doprovodu se ignoruje. Další rady přicházejí u důležitých kroků úkolu. Boční chůze hrdinky střídá otevřené a zavřené kroky ze čtyř dodaných snímků, druhý bok se zrcadlí. Fantasy mapa má jemnější chladnější stíny při zachování plných detailů a kolizí.

## Prostornější Havranov
Aktuální pozadí `SpaciousVillage.png` má šest velkých budov: obchod, kostel, knihovnu, hostinec a domy. Široké dlážděné cesty propojují volné náměstí a kamenný most. Kolize jsou obkreslené podle nového obrázku; starší pozice na střechách se přesunou na nejbližší cestu. Do obchodu postava nejprve viditelně dojde ke dveřím a teprve poté zmizí uvnitř. Kostel a knihovna zatím slouží jako venkovní orientační body. Kamera ukazuje více okolí nad postavou, aby byly čitelné průčelí a dveře.

## Rozšířené středověké město
Nyní se načítá `MedievalTownExpanded.png`: přibližně trojnásobná plocha, další hrázděné, kamenné i doškové domy, hradní brána, kovárna, tržní čtvrť, vodní mlýn a přístaviště. Nově je historicky přepracovaná i původní knihovna, kostel, obchod a hostinec uprostřed. Cesty kombinují dlažbu a udusané klikaté vozové uličky. Celá průchozí síť je propojená a noví obyvatelé pracují v jednotlivých čtvrtích. Staré pozice hlavních NPC a úkolů se při rozšíření zachovávají.

Text je celkově zvětšený na 1,25× původní velikosti: rozhovory 28, názvy 34 a tlačítka až 24. HUD má společně zvětšené písmo i rámečky; větší text používají také nabídky a nastavení. Dialog má větší rám a dvě tlačítka v řádku; dlouhý text lze posouvat. První rituál vyžaduje tři svíčky, po jedné v každém rohu trojúhelníku.

## Mobil a viditelnost rozhraní
**I** otevře mobil hlavní postavy v aplikaci Inventář, **Q** v aplikaci Úkoly. V horní části mobilu lze aplikace přepínat, delší obsah posouvat a klávesou Esc mobil odložit. Příběhový deník se otevírá uvnitř mobilu.

**F2** skryje / znovu zobrazí spodní nápovědu, panel aktuálního úkolu a tlačítka Mapa/Postava. Jednotlivé části lze přepnout v **Esc → Nastavení → Rozhraní hry** (také přes nastavení hlavní nabídky). Volby se uchovají mezi spuštěními. Klávesy I, Q, M a C zůstávají funkční i se skrytými panely.

Aktivní záložka mobilu je tmavší. Vedlejší příběhy, jejich popisy a tlačítka se v aplikaci Úkoly zobrazí až po rozhovoru o daném úkolu s jeho zadavatelem. Objevení se ukládá i bez přijetí úkolu; dříve přijaté nebo dokončené úkoly zůstávají známé. Nová hra i druhý cyklus začínají bez objevených vedlejších úkolů. Prázdné položky vedlejších úkolů se před objevením nezobrazují ani v inventáři.

## Tahové souboje a odemykání dovedností
Úrovně postavy a XP jsou nahrazené body vylepšení. Za odevzdání každého vedlejšího úkolu, dokončení hlavního úkolu oblasti a doručení svíček průvodci získáš jeden bod. Stejnou odměnu nelze v daném cyklu získat opakovaně. Nepřátelé dávají pouze zlato. C otevře dvoustránkovou nabídku osmi dovedností za jeden bod; každou lze odemknout pouze jednou:
- Vitalita: +20 maximálního HP.
- Silný úder: +6 poškození.
- Úhyb: 15% šance vyhnout se nepřátelskému zásahu.
- Rozhodující úder: 10% šance okamžitě porazit běžného nepřítele při útoku.
- Vytrvalec: +20 maximální výdrže.
- Pevný kryt: úplný blok má šanci 50 % místo 35 %.
- Únikový plán: útěk má šanci 75 % místo 50 %.
- Ostré oko: +6 poškození lukem.

Souboj začne při přiblížení k aktivnímu nepříteli nebo klávesou J v jeho dosahu. Hráč volí útok, krytí (poloviční příští zásah), lektvar nebo útěk (50 %). Po platné akci následuje právě jeden tah nepřítele. Zabitý nepřítel už neútočí; neplatné použití lektvaru nespotřebuje tah. Čas, WASD, myš ani mezerník souboj neposouvají. Nelze jej obejít zavřením okna nebo načtením během boje. Ukládá se před začátkem a po dokončení; při ukončení aplikace během boje zůstává poslední stav před soubojem. Po útěku se zranění a použité lektvary uchovají; nepřítel se doplní a počká, než hráč odejde z jeho dosahu.

Odemčené dovednosti zůstávají po celou hru přes všechny cykly. Nevyužité body zůstávají do dalších cyklů. Pouze nová hra odstraní také odemčené dovednosti. Starší uložené zvýšení atributů se převede na odpovídající dovednosti a dokončené úkoly dostanou body pouze jednou. Testovací lokace má osm samostatných bodů pro vyzkoušení všech dovedností. Kontroly: Tools → RPG → Check Turn Combat and Skills.

F2 a přepínač spodní nápovědy nyní skrývají také oba panely Souboj / Lektvar.

## Souboj s NPC přes rozhovor
U obyvatel, průvodce, dědy Matěje, Miry, Radovana i obchodnice je volba **Vyzvat k souboji**. Obchodnici lze vyzvat v rozhovoru, ve sklepní větvi i při prohlížení zboží. Výzva je dostupná u skutečně přítomného NPC (automatické rady průvodce na dálku ji nenabízejí). Následuje běžný tahový souboj; průvodce má 72 HP, ostatní NPC 48 HP.

Vítězství odstraní NPC z lokace a uloží jeho porážku samostatně od nepřátel kampaně. Nevydává body za úkoly, zlato ani postup v počítadle nájezdníků. Po útěku či prohře zůstává NPC k dispozici. Načtení a cestování poražená NPC neobnoví; nový příběh nebo nový cyklus ano. Služby a odevzdávání vedlejších úkolů poraženého zadavatele nejsou dostupné.

Hlavní větev se svíčkami má náhradní stopy: poražený Matěj a Eliška zanechají cestu do obchodu/sklepa, obchod zůstává přístupný. Petra lze vyzvat až po předání tří svíček a získání pokynů k rituálu (dokončený Petrův úkol). Za jeho porážku získáš přesně jednu svíčku navíc, nikdy opakovaně. Poznámky po něm zůstanou v mobilu Q. První rituál stále používá jen tři svíčky. Čtvrtá platí pouze v aktuálním cyklu; při probuzení do dalšího cyklu zmizí také.

Petr je před dokončením svého úkolu chráněn také na úrovni herního stavu, nejen skrytím tlačítka. Starší uložení s předčasně poraženým Petrem ho obnoví; uložená porážka po dokončeném úkolu dostane jednu bonusovou svíčku. Bonus i jeho jednorázové vyzvednutí se ukládají pouze v aktuálním cyklu a nový cyklus je resetuje.

Každý nový cyklus vytvoří výchozí herní stav: plné HP podle odemčených dovedností a plnou výdrž, původní zlato a výbavu, žádné svíčky, žádné dokončené/objevené úkoly a všechna NPC znovu přítomná. Zůstávají nevyužité body, naučený boj a odemčené dovednosti se všemi jejich účinky, totožnost postavy (vzhled a jméno), číslo cyklu a příběhová paměť. Běžné načtení uložené hry pokračuje uvnitř rozehraného cyklu.

## Zpětná vazba při získání odměny
Získané předměty, zlato a body vylepšení zobrazí malý panel s ikonou a přírůstkem. Panel se krátce zvětší, vysune a během přibližně dvou sekund zmizí. Odměny získané najednou se sloučí do jednoho oznámení, rychle po sobě získané se zobrazí postupně. Oznámení neblokuje ovládání a je viditelné i nad dialogem či mobilem. Body mají vlastní ikonu a mírně delší cinknutí. Zvuk respektuje hlavní hlasitost, hlasitost efektů a ztlumení. Načtení uloženého inventáře, nový příběh a nový cyklus odměnovou animaci ani zvuk nespouštějí.

Léčivý lektvar obnovuje **15 HP**, nejvýše do maxima. Truhly a bedny se zásobami se v hlavní hře objeví jen při aktivním Radovanově úkolu **Ztracené zásoby** a po odevzdání zbývající zmizí. Již vyzvednuté se neobnoví při načtení. Testovací lokace si ponechává všechny ukázkové předměty. Hlavní kamera zůstává na původní velikosti 4,8. Přiblížená je minimapa v pravém horním rohu: ukazuje 2,5× detailnější výřez okolí hráče, sleduje ho a u okrajů mapy se zastaví. Vzdálení nepřátelé mimo výřez se nezobrazují; vzdálený cíl zůstává označen na hraně minimapy.

Kliknutí levým tlačítkem během postupného vypisování průvodcova dialogu ihned zobrazí celý text a dokončí vysunutí panelu. Kliknutí při otevírání dialogu se nepočítá jako přeskočení; kliknutí použité pro přeskočení nepotvrdí současně žádnou volbu.

NPC při chůzi sledují skutečný posun. Pokud déle než dvě sekundy stojí nebo jen přešlapují na místě, obrátí směr trasy a hned se pokusí ustoupit či obejít překážku. Nečekají přitom další pracovní pauzu. Obejítí respektuje mapu, ostatní NPC i hráče. Úmyslné pracovní zastávky a otevřený dialog se do zaseknutí nepočítají; samotná blízkost hráče už NPC nezastaví natrvalo.

## Delší pochůzky a první seznámení
Pohybliví obyvatelé chodí po delších okruzích mezi křižovatkami: běžní vesničané a rychtář obejdou náměstí a jižní ulici, průvodce obchází východní cestu, obyvatelé čtvrtí mají vlastní okruhy kolem brány, dílen, trhu, mlýna, přístaviště a polí. Body i jednotlivé úseky jsou ověřené na průchodnost. Chůze má rychlost 0,9 a pracovní pauzy trvají 1,5–3,5 sekundy. Kontrola zaseknutí po dvou sekundách zůstává aktivní.

Při prvním rozhovoru NPC osloví neznámého příchozího. Hrdina řekne zvolené jméno, vysvětlí, že svět nezná a neví, jak se sem dostal. NPC se představí a poté nabídne běžný rozhovor. Neznají hrdinovu minulost ani povahu časové smyčky. Teprve po dokončení seznámení se jejich skutečné jméno ukazuje nad postavou a v nápovědě. Seznámení se ukládají v rámci cyklu, opakované rozhovory úvod neopakují a nový cyklus je resetuje; hrdina si může pamatovat minulé setkání, NPC ho však znovu vnímají jako cizince. Odemčené dovednosti se tím nemění.

## Archiv pouze podle objevů
Bez platné rozehrané/uložené hry je kronika prázdná. Archiv načítá skutečný postup příběhové hry: postavy po prvním rozhovoru nebo souboji (jméno se ukáže až po představení či souboji), nepřátele při setkání zblízka, lokace po skutečném vstupu a předměty po jejich získání. Obchod a sklep jsou samostatná navštívená místa. Výchozí výbava a lektvary se počítají jako získané až při spuštění hry. Prohlížení kategorií nic neodemyká a neukazuje popisy neobjevených záznamů.

Objevy jsou součástí uložené hry, testovací lokace je nepřidává do hlavního příběhu. Nová hra a nový cyklus začínají s čistými záznamy, které se znovu doplňují hraním. U staršího uložení lze bezpečně doplnit pouze jeho aktuální místo, vlastněné předměty, dokončená seznámení a skutečně poražené nepřátele. Nadpis, kategorie a posuvný obsah mají oddělené místo a nepřekrývají se. Kontroly: Tools → RPG → Check Archive Discovery.


Tři příběhové cykly:
- Rituály vyžadují postupně 3 / 4 / 5 svíček: trojúhelník, čtverec, hvězda. První dva rituály končí smrtí a novým cyklem; třetí spustí obranu před osmi nepřáteli.
- Za úkoly lze získat nejvýše 1 / 3 / 4 body v příslušném cyklu. Neutracené body, odemčené buffy a výcvik se přenášejí. Vybavení, svíčky, setkání s NPC a úkoly začínají znovu. Nová hra resetuje vše.
- V prvním cyklu lze v běžném souboji pouze utíkat. Petra lze po dokončení jeho úkolu porazit zvláštní volbou v rozhovoru a získat jednu svíčku. Každý cyklus Petra obnoví a nic si nepamatuje.
- Od druhého cyklu učí Petr zdarma boj s mečem; alternativou je kovář Mikuláš za 30 zlata. Meč stojí dalších 40 zlata. Od třetího učí Petr nebo stávající rybář Oldřich zdarma boj s lukem; Oldřich prodává luk za 40 zlata. Výcvik nepřidává vybavení.
- Od druhého cyklu jsou vedle tří sklepních svíček dostupné další: Petr, les, tajný pokoj za regálem knihovny. Celkem šest, poslední rituál potřebuje pět. Každý zdroj lze získat jen jednou za cyklus.
- Netopýři jsou v lese a Temném lese od třetího cyklu; zasáhne je pouze naučený boj s vlastněným lukem. Blok má 35% šanci plně vykrýt ránu, jinak zranění půlí.
- Závěrečná vlna umožňuje ruční tahy nebo volitelný zrychlený autoboj. Ten používá skutečné zbraně a lektvary, léčí při nejvýše 35 HP a může prohrát. Počet zbývajících nepřátel se ukládá po vítězství; útěk či porážka vlnu nedokončí. Během obrany nelze odejít z kostela ani zavřít nabídku. Před rituálem jsou nutné obě zbraně a výcvik. Po smrti se hráč probudí na náměstí a celá vlna se při dalším pokusu obnoví; uložení a návrat do menu umožní později pokračovat bez obnovení zdraví.
- Po poražení posledního nepřítele se otevře nabídka **Příběh dokončen**: lze pokračovat v průzkumu světa nebo uložit postup a vrátit se do hlavního menu. Při načtení dokončeného příběhu se nabídka zobrazí znovu.

Nepřátelé jsou pouze dva druhy: **vlk** (pozemní, pouze meč) a **netopýr** (létající, pouze luk). Platí pro průzkum, testovací lokaci i závěrečnou vlnu. Nájezdníci ani velitel se již negenerují; jejich staré záznamy se z archivu vyřazují. Starší bojový úkol se splní po pěti poražených tvorech bez dalšího bosse.


Vstupy do minilokalit mají jeden standard: zlatá ikona dveří a nápis `[E] Vstoupit: název`, dosah 1,35 herní jednotky, po stisku E společné potvrzení `Vstoupit / Zůstat zde` a krátké zatmavení. Platí pro obchod, knihovnu, kostel, les, Temný les, návraty do města i tajný pokoj knihovny. Obchod se sám neotevírá při přiblížení. Schody do sklepa používají totéž potvrzení a zatmavení; návrat ze sklepa vede zpět do obchodu. Odmítnutí vstupu ani zavření potvrzení nepřesune hráče a neobjeví cílovou lokaci v archivu.


Postup lesa podle cyklů: v prvním cyklu jsou Les i Temný les zamčené a vstup zobrazí „Hmmm, já zatím nevím, jak se tam dostat.“ Ve druhém a třetím cyklu se při prvním pokusu o vstup objeví myšlenka o nalezení cesty, oznámení otevření lokace a připomínka výcviku/výbavy. Ve druhém cyklu jsou v obou částech lesa pouze vlci; ve třetím vlci a netopýři. Vlk vyžaduje naučený boj s vlastněným mečem, netopýr naučený boj s vlastněným lukem. Luk nelze použít na vlka. Běžný boj v prvním cyklu není dostupný; zvláštní příběhová volba u Petra po jeho úkolu zůstává.


Lesy používají dodaná pozadí `les.webp` a `temny les.webp` převedená beze změny do PNG pro import Unity (`ForestMap`, `DarkForestMap`). Kolize kopírují viditelné stezky, balvany, klády, ploty a řeku; vodu překračuje pouze most. Vstup z města leží na jihozápadní stezce, přechod do Temného lesa za východním mostem. Lesní svíčka je na východním břehu. Vlci stojí na severních odbočkách; od třetího cyklu se netopýr objevuje na jihovýchodní odbočce.

V knihovně je knihovnice Agáta uprostřed u pultu. Po seznámení popíše cestu ke svíčce bez souboje. Volba „Zapamatovat si bezpečnou cestu“ uloží znalost a v lese zapne navigaci po bezpečných bodech. Znalost zůstává do dalších cyklů, nová hra ji vymaže. Trasa nepřátele nemaže ani neposkytuje imunitu: skutečně vede mimo jejich dosah. Kontrola: Tools → RPG → Check Forest Paths and Safe Route.


Od druhého cyklu se automatické oslovení a rady Petra nespouštějí ani při načtení hry. Po probuzení hrdina řekne „Okay, zkusím to nějak sám.“ (hrdinka „sama“). Hráč může rovnou požádat v obchodě o vstup do známého sklepa bez opakovaného úvodu u Petra a dědy. Po získání tří sklepních svíček si pamatuje postup rituálu a nemusí pro další radu k Petrovi; počet potřebných svíček se dál řídí cyklem. Petra lze dobrovolně oslovit kvůli výcviku a jeho dialogy zůstávají dostupné. První cyklus zachovává původní vedení.


Dveře a kolize: značky vstupů se neposouvají na nejbližší cestu. Obchod, knihovna a kostel mají pevně zaměřené středy dveří na aktuálním obrázku města; návratová pozice je zvlášť na volné ploše před nimi. Knihovna i tajný archiv používají skutečný jižní práh a tajný průchod leží přímo na odkrytých dveřích v regálu. Vstup vyžaduje stát před dveřmi, v jejich šířce a dosahu: nelze ho spustit zezadu či bokem přes zeď. Volné přechody v lese zůstávají na stezkách. Kolizní stopa postav je sjednocena na poloměr 0,18 jednotky; město kontroluje i diagonální okraje stopy a všechny mapy pohyb průběžně procházejí po krátkých krocích. Kontrola: Tools → RPG → Check World Collisions and Door Thresholds.

## Opravy návazností a úvod
Nová hra začíná přeskočitelným úvodem shora: usnutí, proměna partnera a uzamčení ve snu. Úvod se ukládá a při běžném načtení ani novém cyklu se neopakuje. Používá stávající snový obrázek MainMenuCastle a barevné čtverce.

Pokud Oldřich zemře před pořízením luku, od třetího cyklu zůstane u přístaviště truhla: luk stále stojí 40 zlata, střelbu lze nastudovat zdarma. Náhradní luk prodává také živý kovář. Tato cesta funguje i po smrti Petra a kováře. Zásilku nelze nově přijmout pro mrtvého Petra; již přijatou lze vrátit Elišce bez odměny.

Běžná porážka vždy obnoví hráče na náměstí se zachováním stavu současného cyklu. Kontroly oprav: Tools > RPG > Check Audit Regressions.
