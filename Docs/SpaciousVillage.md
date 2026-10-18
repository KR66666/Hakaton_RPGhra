# Spacious village map

Asset: `Assets/Resources/SpaciousVillage.png`.
Generated with built-in ImageGen using the supplied fantasy village screenshot as style reference.

Prompt specification: a complete 1536×1024 steep three-quarter overhead fantasy pixel-art village, six large distinct buildings instead of a dense town, broad connected stone boulevards, an empty central square, a book-emblem library, church, tavern, shop, cottages, an east river and broad bridge. Warm windows, slate roofs, flowers and trees outside paved lanes, readable front walls and doors, no characters, UI, words or watermark. The initial coordinate plan was included in the prompt; the final road network and building blockers were traced to the generated map rather than assuming those coordinates were exact.

Geometry is in normalized 1536×1024 coordinates in `RpgWorldGeometry`. The world still spans 40×26.6667 units. Road widths, door coordinates and old-save relocation use the same geometry. The shop entrance walks the hero to the visible door for 1.15 seconds, then fades inside for 0.45 seconds. Church, library and bridge are map landmarks; existing shop interior and story remain available.

Camera framing is wider and shifted upward to show front walls and entrances. The avatar is scaled to match the larger buildings. Movement speed, keys and quest rules remain unchanged.
