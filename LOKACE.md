# Lokace a přechody

Ve hře stiskni **M** a vyber „Označit cestu“. Šipka a zlatý bod na mapce tě dovedou ke vstupu. Přijď ke zlaté značce na vzdálenost přibližně jednoho kroku a stiskni **E**. Přechody jsou obousměrné.

```text
Havranov
├── Obchod → sklep obchodu (původní interiér a příběhová sekvence)
├── Knihovna
├── Zřícenina kostela
└── Les
    └── Temný les
```

| Vstup ve městě | Cíl | Návrat |
| --- | --- | --- |
| Severní brána | Les | Jižní stezka |
| Knihovna v jižní části města | Knihovna s regály a čítárnou | Jižní dveře |
| Cesta u kostela ve východní části města | Zřícenina kostela | Jižní brána |
| Severní stezka v lese | Temný les | Jižní stezka do lesa |

Nové lokace jsou zatím jednoduché pixelové mapy z pohledu shora s překážkami a průchozími cestami. Další příběh, úkoly a nepřátelé v nich nejsou přidáni. Při přechodu se zachová zdraví, výdrž, inventář i rozpracované úkoly. Aktuální lokace a poloha se ukládají; starší soubory bez údaje o lokaci začínají v Havranově.

## Implementace

- `Assets/Scripts/RpgLocations.cs`: stabilní identifikátory lokací, směry přechodů, vstupní body a geometrie překážek.
- `Assets/Scripts/RpgLocationArt.cs`: dočasná grafika nových map, která sdílí půdorysy s kolizemi.
- `Assets/Scripts/RpgLocationTravel.cs`: značky vstupů, přechod se zatmavením, obnovení mapy, sledování cesty a nabídka mapy.
- `Assets/Scripts/RpgState.cs`: pole `place` v uložené hře; stávající `currentArea` dál patří původním bojovým výpravám.
- `Assets/Editor/RpgLocationValidation.cs`: nabídka **Tools → RPG → Check Location Transitions** ověří propojení, průchodnost, kolize, rozměry map a serializaci.

Nové mapy používají současnou herní scénu a kameru. Původní město, testovací lokace, obchod a sklep zůstávají zachované.
