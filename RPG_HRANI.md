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

## Herní průchod

1. Na náměstí promluv s rychtářem Radovanem a přijmi úkol.
2. U Elišky nakup lektvary nebo vylepši meč a zbroj. Mira léčí a mění dvě byliny na lektvar.
3. Sbírej truhly (zlaté značky) a byliny (zelené značky).
4. Poraz pět nájezdníků. Úder nepřítele je předem signalizován červenou barvou.
5. Na severu se objeví velitel Vranek. Poraz ho a vrať se k Radovanovi pro odměnu.
6. Po splnění lze pokračovat v průzkumu. Po smrti tě Mira zachrání za ztrátu až 10 zlata.

Úrovně a XP se nepoužívají. Za dokončený úkol získáš jeden bod, který slouží pouze k jednorázovému odemčení dovednosti. Vybavení lze vylepšit pětkrát.
Dialogy a inventář pozastavují pohyb i nepřátelskou AI.

## Vedlejší úkoly

V rozhovoru s NPC zvol tlačítko **Úkol**. Deník na Q ukazuje postup a podrobnosti.

| Úkol | Zadavatel a cíl | Odměna |
| --- | --- | --- |
| Byliny pro Miru | Mira: přines 4 byliny. Při odevzdání se spotřebují. | 25 zlata, 45 XP, 2 lektvary |
| Ztracené zásoby | Radovan: najdi dvě oranžové bedny severně od náměstí a na východní cestě k mostu. | 60 zlata, 60 XP |
| Zásilka pro strážného | Eliška: předej balíček Petrovi u východního kamenného mostu a vrať se. | 40 zlata, 35 XP, lektvar |

Odměnu vždy odevzdává původní zadavatel. Miřin záhonek západně od středu náměstí poskytuje 2 byliny každých 45 sekund.
Vedlejší úkoly jsou dostupné i v již rozehrané hře a po dokončení hlavního příběhu.

## Ukládání

Soubor `village-rpg-save.json` se ukládá do Unity `Application.persistentDataPath`.
Ukládá se stav postavy, vybavení, postup úkolu, otevřené truhly, sebrané byliny a poražení nepřátelé.
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

Mapa úrovní: tlačítko MAPA nebo M, také v pauze a po vítězství. Čtyři bojové varianty v sektorech současné vesnice; dokončení znamená převzít odměnu u Radovana. Další úroveň se odemkne postupně. Zelené uzly lze znovu projít (resetuje se pouze boj dané úrovně), stav ostatních úrovní i vedlejší úkoly se zachovají. Modrá tečka ukazuje polohu hráče, zlatá značka aktivní úroveň. Před cestou lze vybrat chůzi zdarma nebo přesun vozem za 10 zlata; Petr po doručení zásilky pomáhá útokem, Mira po bylinkovém úkolu léčí. Volby a postup jsou součástí uložené hry.

NPC mají vlastní pixelové sprity nezávislé na hráči: Radovan vínový kabátec a čapku, Eliška zástěru a brašnu, Mira světlý šátek a košík bylin, Petr helmu, kopí a štít. Doprovod používá stejný vzhled jako příslušné NPC. Nájezdníci mají čtyři varianty kožené výstroje, velitel vlastní zbroj.
Hlavní menu používá dodaný obrázek nočního hradu MainMenuCastle.jpg přes celou plochu se zachováním poměru stran. Tlačítka mají vlastní 16×16 pixelový rámeček, Point filtrování, devítidílné roztažení a okamžité stavy najetí/stisku. Výběr postav zůstává funkční.

Postavy nyní používají schválený atlas HeroOverhead.png: pro každou postavu stoj na obou nohách, levý a pravý krok, s pohledem shora. Kód otáčí snímky podle směru chůze a zastaví střídání při stání či překážce. Při přiblížení k obchodnici, rychtáři nebo bylinkářce se jednou nabídne Vejít/Odmítnout. Interiér zobrazuje postavy a dialog; obchodnice používá dodaný ShopWomanPortrait.png. Odejít/Esc vrací pohled ven. Nové snímky byly vytvořeny vestavěným imagegen po výslovném souhlasu uživatele; zadání: zachovat vzhled, pohled přímo shora, stoj/levý/pravý krok, holka a kluk.

Obchod má samostatné pixelové pozadí ShopInterior.png. Na obrazovce jsou do pasu Arabella v červeném (vlevo) a Obchodnice v modrém (vpravo), průhledné PNG bez šachovnice. Dole je široký dialog se jménem aktuální mluvčí, střídáním replik a volbami Pokračovat / Zboží / Úkol / Odejít. Kontrolní náhled během hry: Tools > RPG > Preview Shop Dialogue (Ctrl+Shift+F7).

Vstup do místností se nabízí až do vzdálenosti 0,55 od označených dveří. Odmítnutí se znovu nabídne až po vzdálení nad 1,2. Po potvrzení hráč 0,7 sekundy dochází ke dveřím a 0,3 sekundy mizí, teprve pak se zobrazí interiér. Při odchodu se objeví zpět před dveřmi. Během vstupu je boj i ruční ovládání zablokované.

Oprava vstupů: jediný otevřený interiér je obchod v malém domě na severní straně náměstí (v původním pozadí). Vchod má pevné souřadnice, není navázán na NPC; žádné nové dveřní sprity se nekreslí. Radovan, Mira a Petr chodí po cestách a zastavují při práci či rozhovoru. Obchodnice je uvnitř a zadává Zásilku pro strážného.

Vylepšení hlavní postavy: tlačítko POSTAVA / C nebo přes batoh I. Za každou novou úroveň je 1 bod: vitalita +15 maximálního zdraví, síla +3 útoku, hbitost +4 % rychlosti, nejvýše 5 bodů do každé vlastnosti. Vlastnosti se ukládají s příběhem; starší uložené hry získají body podle dosažené úrovně. Hlavní menu má dole vpravo pixelové ozubené kolečko Nastavení: celková hlasitost, hudba, efekty, ztlumení, celá obrazovka, kvalita, V-Sync, obnova výchozího nastavení. Volby se ukládají do PlayerPrefs. Je zapojená tichá procedurální melodie a zvuky UI/útoku.

Hrdinové jsou opět o cca 12 % menší (měřítko 0,60). Malá mapa vpravo ukazuje modře hráče, červeně živé nepřátele a zlatě sledovaný úkol; kliknutím otevře velkou mapu. Směrová šipka a vzdálenost vedou k cíli vzdušnou čarou (nenahrazují navigaci po cestách). V Q lze sledovat hlavní nebo přijatý vedlejší úkol; volba se ukládá. Cíl reaguje na postup: doručení Petrovi přepne ukazatel zpět k obchodu, nasbírané byliny k Miře. Deník shrnuje odměny a po zvýšení úrovně se připomene nový bod vylepšení C.

Průchodnost celé vesnice: opravené rozpojené západní a severní cesty, doplněné severovýchodní a jižní spojnice a cesta kolem mlýna. Kolize menší postavy má půdorys 3 px a budovy 2 px rezervu; střechy zůstávají blokované, stejně tak tráva a voda mimo most. Automatická kontrola prochází po 4px vzorkovací síti všechny cesty a vyžaduje jejich spojení s náměstím.

Hlavní menu > Lokace > Testovací verze: samostatný čerstvý test nad mapou vesnice. Čtyři NPC, léčivý lektvar, byliny, záhonek, truhla, obě zásilky, meče a zbroje +0 až +5, čtyři vzhledy nájezdníků a velitel. E zkouší objekty, T/Esc otevře testovací nástroje (obnovit obsah, zapnout/vypnout souboj, návrat do hlavního menu). Hráč je nezranitelný, začíná na úrovni 10 se zlatem a lektvary; C zkouší vylepšení. Ukládání i načítání hlavního příběhu jsou v testovacím režimu vypnuté, F9 obnovuje test. Normální Hrát vždy spouští příběh.

## Svíčky a první rituál
Průvodce Petr → děda Matěj → obchodnice Eliška → sklep. Ve sklepě jsou tři volby: **utéct** (bez svíček, lze se vrátit), **rychle skočit pro svíčky** (3 svíčky, ztráta až 20 HP; zůstane nejméně 1 HP), nebo **opatrně dojít** (3 svíčky bez zranění). Svíčky nelze získat opakovaně. V batohu i příběhovém deníku je jejich počet.

Vrať se k Petrovi a zvol **Mám svíčky • přijmout radu**. Průvodce tě pošle do **zříceniny kostela**; ukazatel cíle označí cestu. U trojúhelníkového obrazce stiskni **E** a potvrď rituál. Tři svíčky se postupně rozsvítí, obrazec zrudne, hrdina zemře a probudí se do **druhého cyklu**. Herní stav a předměty se obnoví na výchozí hodnoty; zůstane vzhled, počet úmrtí a vzpomínka na selhání. Druhý cyklus se automaticky uloží i do zálohy. Jeho další příběh zatím není připraven. První rituál v něm nelze opakovat.

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
Úrovně postavy a XP jsou nahrazené body vylepšení. Za odevzdání každého vedlejšího úkolu, dokončení hlavního úkolu oblasti a doručení svíček průvodci získáš jeden bod. Stejnou odměnu nelze v daném cyklu získat opakovaně. Nepřátelé dávají pouze zlato. C otevře nabídku čtyř dovedností za jeden bod; každou lze odemknout pouze jednou:
- Vitalita: +20 maximálního HP.
- Silný úder: +6 poškození.
- Úhyb: 15% šance vyhnout se nepřátelskému zásahu.
- Rozhodující úder: 10% šance okamžitě porazit běžného nepřítele při útoku, bez účinku na velitele.

Souboj začne při přiblížení k aktivnímu nepříteli nebo klávesou J v jeho dosahu. Hráč volí útok, krytí (poloviční příští zásah), lektvar nebo útěk (50 %). Po platné akci následuje právě jeden tah nepřítele. Zabitý nepřítel už neútočí; neplatné použití lektvaru nespotřebuje tah. Čas, WASD, myš ani mezerník souboj neposouvají. Nelze jej obejít zavřením okna nebo načtením během boje. Ukládá se před začátkem a po dokončení; při ukončení aplikace během boje zůstává poslední stav před soubojem. Po útěku se zranění a použité lektvary uchovají; nepřítel se doplní a počká, než hráč odejde z jeho dosahu.

Body i odemčené dovednosti se ukládají a přežijí příběhový přechod do druhého cyklu. Nová hra začíná bez nich. Starší uložené zvýšení atributů se převede na odpovídající dovednosti a dokončené úkoly dostanou body pouze jednou. Testovací lokace má čtyři samostatné body pro vyzkoušení všech dovedností. Kontroly: Tools → RPG → Check Turn Combat and Skills.

F2 a přepínač spodní nápovědy nyní skrývají také oba panely Souboj / Lektvar.

## Souboj s NPC přes rozhovor
U obyvatel, průvodce, dědy Matěje, Miry, Radovana i obchodnice je volba **Vyzvat k souboji**. Obchodnici lze vyzvat v rozhovoru, ve sklepní větvi i při prohlížení zboží. Výzva je dostupná u skutečně přítomného NPC (automatické rady průvodce na dálku ji nenabízejí). Následuje běžný tahový souboj; průvodce má 72 HP, ostatní NPC 48 HP.

Vítězství odstraní NPC z lokace a uloží jeho porážku samostatně od nepřátel kampaně. Nevydává body za úkoly, zlato ani postup v počítadle nájezdníků. Po útěku či prohře zůstává NPC k dispozici. Načtení a cestování poražená NPC neobnoví; nový příběh nebo nový cyklus ano. Služby a odevzdávání vedlejších úkolů poraženého zadavatele nejsou dostupné.

Hlavní větev se svíčkami má náhradní stopy: poražený Matěj a Eliška zanechají cestu do obchodu/sklepa, obchod zůstává přístupný. Poražený průvodce zanechá **Průvodcovy poznámky** v mobilu Q; po získání tří svíček lze podle nich odemknout rituál. Tato náhradní cesta přidá stejnou jednorázovou odměnu za svíčky a neblokuje druhý cyklus.
