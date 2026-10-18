# RPG Adventure – Unity

Otevři projekt v Unity **6000.6.6f1** a spusť Play. Editor automaticky
otevře hlavní menu (`Assets/Scenes/MainMenu.unity`).

- **Hrát** spustí příběhovou hru v `SampleScene`; existující uložený příběh se
  načte automaticky. Pro nový příběh použij nabídku hry.
- **Lokace → Testovací verze** otevře oddělenou testovací arénu, která nemění
  uložený příběh.
- **Archiv** otevře scénu `Bestiář`.
- Hlavní menu a herní rozhraní se vytvářejí za běhu. Herní scéna obsahuje
  pohyb, průzkum, úkoly, boj, vybavení, progresi a ukládání.

## Ovládání

WASD / šipky – pohyb; Shift – běh; J / levé tlačítko myši – útok;
E – interakce; Esc – pauza nebo zavření dialogu. Další klávesy a postup hry
jsou popsány v [RPG_HRANI.md](./RPG_HRANI.md).

## Struktura

- `Assets/Scenes/` – menu, herní scéna a archiv.
- `Assets/Scripts/` – herní logika, RPG rozhraní, pohyb a UI.
- `Assets/Resources/` – mapy, postavy, portréty a další zdroje.
- `Assets/Editor/` – automatické kontroly a editorové nástroje.
- `Docs/` – doplňující technická dokumentace.

Build Settings obsahují scény `MainMenu`, `SampleScene` a `Bestiář`.
Aktuální herní HUD spravuje `RpgInterface`; samostatná komponenta `PlayerHud`
se během spuštění příběhové hry vypíná.
