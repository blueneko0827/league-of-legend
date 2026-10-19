# Irelia — skin base

## Originales de Riot disponibles localmente

Dentro de `Original` se conserva la estructura de las rutas originales:

| Carpeta | Contenido |
|---|---|
| `assets/characters/irelia/skins/base` | Modelo `.skn`, esqueleto `.skl`, texturas `.tex`, pantalla de carga, **68 animaciones `.anm`** y **130 archivos de partículas** (mallas y texturas). |
| `assets/characters/ireliablades/skins/base` | Modelo y esqueleto independientes de las cuchillas, con **4 animaciones**. |
| `assets/characters/irelia/hud` | Retrato e iconos disponibles de las habilidades de la skin base. |
| `assets/sounds/wwise2016/sfx/characters/irelia/skins/base` | Bancos completos `irelia_base_sfx_audio.bnk` y `irelia_base_sfx_events.bnk`. |
| `assets/sounds/wwise2016/vo/en_us/characters/irelia/skins/base` | Bancos completos de voces en inglés: audio `.bnk`, contenedor `.wpk` y eventos `.bnk`. Se conserva todo el banco disponible, además de las frases de R ya extraídas. |
| `assets/shared`, `assets/particles` | Recursos compartidos disponibles en la extracción, conservados como posibles dependencias. Algunos nombres mencionan otros campeones. |
| `data/characters` | Definiciones `.bin` de Irelia y sus cuchillas para base/skin0, y definiciones compartidas que incluyen skin0. |
| `AdditionalBanks` | Banco de voz suelto encontrado junto a las carpetas fuente, conservado por separado. |
| `Unresolved` | 23 archivos extraídos con nombres hash: su skin no está identificada. Se conservan por si resuelven dependencias. |
| `LongNames` | Definiciones compartidas cuyos nombres originales producen rutas demasiado largas en Windows. El inventario conserva sus nombres y rutas originales exactos. |

Las dos copias de la carpeta base (`base` e `IreliaMore`) coinciden byte por byte:
se guardó una sola copia y ambas procedencias figuran en el inventario. Las otras
skins y el personaje `jade_irelia` se excluyeron; sus rutas están enumeradas en
`excluded_other_skins_or_modes` del inventario.

Las animaciones incluyen ataques y variantes, caminar/correr, Q/W/E/R y sus
transiciones, idle, muerte, reaparición, regreso a base, homeguard y gestos. Los
archivos de partículas son recursos originales; su inclusión no implica que todos
sus efectos ya estén reproducidos por el motor de Unity.

## Copia de los recursos convertidos

`Unity/Assets` conserva los archivos ya preparados por el proyecto:

- `SourceModels/IreliaOfficial`: GLB convertido, fuentes usadas por los importadores y efectos E/R.
- `SourceAudio/IreliaOfficial`: bancos usados para convertir sonidos.
- `Resources/Models/IreliaOfficial`: prefab, materiales, mallas, texturas y **21 clips de animación Unity**.
- `Resources/Audio/Irelia`: **122 WAV** y manifiestos de extracción de sonidos.
- `Resources/Audio/IreliaVoice`: **8 WAV de R** y su manifiesto. El resto de voces permanece en los bancos originales completos.
- `Resources/HudIcons` y `Resources/Shaders`: iconos y shaders de Irelia utilizados por el simulador.

La copia incluye `.meta` para conservar la información de importación. Depende del
proyecto y de sus paquetes (incluido URP); no es un proyecto Unity independiente.

El prefab activo sigue en
[`Assets/Resources/Models/IreliaOfficial/Irelia.prefab`](../../../Assets/Resources/Models/IreliaOfficial/Irelia.prefab).
Los importadores se encuentran en [`Tools`](../../../Tools), incluyendo
`Import-OfficialIrelia.ps1`, `Import-IreliaAudio.py`, `Extract-IreliaRVoice.py` y
los importadores de E, R e indicadores. Los formatos `.anm`, `.tex`, `.scb`,
`.bnk` y `.wpk` requieren sus herramientas de conversión; Unity no los reproduce
directamente.

**Inventario:** 792 archivos, 202.600.534 bytes, sin contar este README y el propio
`manifest.json`. Cada archivo está verificado con SHA-256.
