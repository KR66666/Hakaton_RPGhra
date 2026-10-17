# RPG Adventure – Unity

Otevři projekt v Unity **6000.6.6f1** a stiskni **Play**. Editor automaticky
spustí `Assets/Scenes/MainMenu.unity`, i když upravuješ jinou scénu.
Nastavení se obnovuje po importu skriptů; lze ho také zapnout přes
`Tools > RPG > Start Play Mode From Main Menu`.

- **Hrát** načte `SampleScene` s ukazateli HP a staminy.
- **Konec** zavře aplikaci; v editoru ukončí Play Mode.
- **Archiv** otevře scénu `Bestiář`. Ta obsahuje připravená, zatím neaktivní
  tlačítka **Nepřátelé**, **Lokace** a **Předměty** a funkční tlačítko **Zpět**.
- Menu podporuje myš a klávesnici (šipky, Enter).

## Struktura

- `Assets/Scenes/` – hlavní menu a herní scéna.
- `Assets/Scripts/UI/` – `MainMenu` a `PlayerHud`.
- `Assets/Settings/` – nastavení renderování a šablony Unity.
- `Docs/` – návod k napojení HUD na herní logiku.

Scény v Build Settings jsou seřazeny `MainMenu`, `SampleScene`, `Bestiář`.
UI se vytváří při spuštění scény. Herní scéna zatím obsahuje pouze
základní HUD, bez pohybu hráče a soubojů.
