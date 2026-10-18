# Historic medieval expansion

Asset: `Assets/Resources/MedievalTownExpanded.png`, generated using the built-in ImageGen tool.

First prompt: zoom out the supplied village and build a larger connected historic medieval town around the existing centre. Preserve the front-wall camera angle and recognizable central shop, library, church and inn. Add a fortified gate, smithy/workshop alley, market quarter, thatched farming hamlet, mill and riverside wharves. Use rough 13th–15th-century lime plaster, crooked oak timber, stone, terracotta and thatch, with cobblestones and worn earth cart lanes rather than suburban roads. No UI, characters, lettering or watermarks.

Second edit prompt: retain the complete expanded map layout and all doors, but redesign the original six central houses as modest historically medieval architecture too. Give the library a book emblem and simple scriptorium facade, the church a restrained stone Gothic parish appearance, the shop a timber merchant frontage and the tavern an ordinary ale sign. Remove oversized fantasy gables, gold trim and glowing ornamental window outlines. Leave streets and outer districts intact.

The source image remains intact in the generated-images directory; a project copy is imported at native resolution without compression. The playable world is about 69×46 units, approximately three times the area of the original 40×26.67 village. The revised paving/roof mask in `RpgTownExpansion.cs` traces the actual generated layout. Every navigable sample connects to the central square, including the gate, smithy, market, mill, wharf, church and library.

Legacy quest and NPC coordinates are transformed into the expanded artwork so current saves and objectives retain their place. The six new district residents use local daily routines. Only the merchant shop has the existing enterable interior; other named buildings remain outdoor landmarks.
