# HP a stamina

V aktuální příběhové hře vykresluje HUD komponenta `RpgInterface` a bere
hodnoty ze stavu `RpgGame`. Při inicializaci hra deaktivuje scénový objekt
**Player HUD** a vytvoří své herní rozhraní. Proto nepřipojuj `PlayerHud`
k herní scéně jako zdroj aktuálních statistik.

`PlayerHud` zůstává samostatnou komponentou pro jiné scény nebo prototypy.
Pokud ji používáš mimo příběhovou hru, její veřejné metody jsou:

```csharp
[SerializeField] private PlayerHud hud;

hud.SetHealth(75f, 100f);
hud.SetStamina(40f, 100f);
```

Obě hodnoty se omezují na rozsah od nuly do zadaného maxima. V příběhové hře
se zdraví, stamina, poškození i regenerace aktualizují přes herní stav.
