# HP a stamina

Otevři `Assets/Scenes/SampleScene.unity` a spusť Play. Objekt **Player HUD**
vytvoří za běhu Canvas s HP a staminou vlevo nahoře. V Inspectoru můžeš
za běhu měnit `Health`, `Max Health`, `Stamina` a `Max Stamina`.

Napojení na herní logiku:

```csharp
[SerializeField] private PlayerHud hud;

// Po zásahu nebo změně staminy:
hud.SetHealth(75f, 100f);
hud.SetStamina(40f, 100f);
```

Do pole `hud` přetáhni objekt **Player HUD** ze scény. Hodnoty se omezují
na rozsah 0 až maximum. UI zatím neobsahuje boj ani spotřebu/regeneraci staminy.
