# Havranov – Strážce vesnice

Hratelný prototyp RPG v Unity 6.6. Spusť Play a v hlavní nabídce zvol **Hrát**.
Předchozí příběh se automaticky načte. Nový příběh lze začít v nabídce Esc.

## Ovládání

| Klávesa | Akce |
| --- | --- |
| WASD / šipky | Pohyb |
| Shift | Běh, spotřebovává výdrž |
| J / levé tlačítko myši | Útok mečem na blízké nepřátele |
| Mezerník | Krátký úhyb s nezranitelností, stojí 25 výdrže |
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

XP zvyšují úroveň, maximální zdraví a útok. Vybavení lze vylepšit pětkrát.
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

## Tajemství sklepa
U východního mostu promluv s Petrem Křížem [E] a vyraz s ním. Děda Matěj na náměstí tě pošle za Eliškou pro svíčky. V obchodě se rozhodni, zda sestoupíš do sklepa. Po setkání s krysami zvol ústup, u schodů vezmi a otevři lístek, nebo pokračuj bez něj. Pro lístek se lze vrátit. Vzkaz najdeš v deníku [Q] → Tajemství sklepa. Ukaž ho Petrovi pro jednorázovou odměnu. Průběh se ukládá. V testovací lokaci lze ukázku otevřít přes Tools → RPG → Preview Cellar Story (Ctrl+Shift+F10).

Havranov je pokojná vesnice: místní obyvatelé neútočí a hráč jim neubližuje. Šest řemeslníků a sousedů chodí po cestách, zastavuje se u práce a nabízí rozhovor [E]. Dokončením Tajemství sklepa se odemkne další lokace [M], takže postup nevyžaduje boj s vesničany. Nepřátelé patří do dalších výprav; v testu se boj zapíná výslovně.

## Jednotný světlý pixelový vzhled
Mapa, postavy, obchod a portréty sdílejí barevnou pixelovou paletu; zeleň je výraznější, kámen teplejší a voda tyrkysová. Rozložení mapy a kolize zůstávají stejné. Herní panely, dialogy, menu, archiv i nastavení používají společné rámečky. Zdraví ukazuje také deset srdíček; meč a lektvar mají vlastní sloty. Předměty ve světě mají ikony meče, zbroje, lektvaru, bylin nebo truhly. Úprava se aplikuje při spuštění, původní obrázky se nepřepisují.

## Detailní fantasy mapa a dodané směrové snímky
Aktuální mapa `Assets/Resources/FantasyVillage.png` nahrazuje pouze kresbu pozadí: zachovává světové rozměry 40 × 26,6667, cesty, domy i původní kolize. Vykresluje se v nativním rozlišení bez předchozího barevného a pixelizačního filtru. Hrdinka používá přesné dodané JPG pásy v `Assets/Resources/HeroDirectional/`: zepředu, zezadu a z boku; opačný bok se zrcadlí. Čtyři fáze chůze a klidová póza mají společnou velikost a nepřepisují nastavení pohybu. Čísla pod snímky se oříznou, okolní bílé pozadí odstraní kód. Kluk si zachovává své původní obrázky.

Petr Kříž je nyní občasný portrétní rádce. Při vstupu do hlavní hry se jeho uvítací okno vysune a text se postupně vypíše. Nechodí za hráčem a rada nevytváří fyzický doprovod; starší volba Petra jako doprovodu se ignoruje. Další rady přicházejí u důležitých kroků úkolu. Boční chůze hrdinky střídá otevřené a zavřené kroky ze čtyř dodaných snímků, druhý bok se zrcadlí. Fantasy mapa má jemnější chladnější stíny při zachování plných detailů a kolizí.

## Prostornější Havranov
Aktuální pozadí `SpaciousVillage.png` má šest velkých budov: obchod, kostel, knihovnu, hostinec a domy. Široké dlážděné cesty propojují volné náměstí a kamenný most. Kolize jsou obkreslené podle nového obrázku; starší pozice na střechách se přesunou na nejbližší cestu. Do obchodu postava nejprve viditelně dojde ke dveřím a teprve poté zmizí uvnitř. Kostel a knihovna zatím slouží jako venkovní orientační body. Kamera ukazuje více okolí nad postavou, aby byly čitelné průčelí a dveře.

## Rozšířené středověké město
Nyní se načítá `MedievalTownExpanded.png`: přibližně trojnásobná plocha, další hrázděné, kamenné i doškové domy, hradní brána, kovárna, tržní čtvrť, vodní mlýn a přístaviště. Nově je historicky přepracovaná i původní knihovna, kostel, obchod a hostinec uprostřed. Cesty kombinují dlažbu a udusané klikaté vozové uličky. Celá průchozí síť je propojená a noví obyvatelé pracují v jednotlivých čtvrtích. Staré pozice hlavních NPC a úkolů se při rozšíření zachovávají.
