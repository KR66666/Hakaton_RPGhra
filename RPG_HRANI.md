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
Hráčka střídá dva kroky podle skutečně ušlé vzdálenosti. Při zastavení, v dialogu a při nárazu do překážky se animace zastaví; při běhu se zrychlí. Všechny směry používají stejný pohled přímo shora (viditelný vršek hlavy a ramen), včetně pohybu vlevo a vpravo. Při zastavení zachová poslední směr. Atlas obsahuje šest snímků: dva kroky na jih, sever a východ; západ zrcadlí východ. Velikost se kalibruje podle delší osy půdorysu, takže otočení na východ postavu nenatáhne.
NPC a nepřátelé zatím používají barevné varianty stejného spritu; zvuk zatím chybí.
Chůze je povolená na vyznačené síti cest, náměstí a kamenném mostu. Budovy, stánky, voda mimo most a plochy mimo cesty jsou blokované. Kolize kontroluje celou dráhu i při úhybu, aby nešlo přeskočit střechu. Starší uložená pozice mimo cestu se přesune na nejbližší průchozí místo.
Nepřátelé mají lokální pronásledování, nikoli navigační síť pro cestu přes celou vesnici.
Postava má 12 snímků shora: čtyři směry, v každém stoj na obou nohách, levá a pravá noha. Cyklus: levá–stoj–pravá–stoj. Hráč je menší o 15 %.
