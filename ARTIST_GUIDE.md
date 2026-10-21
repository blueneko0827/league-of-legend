# Guía de trabajo visual

## Objetivo

Aproximar la presentación al modo Práctica de League of Legends: perspectiva y
encuadre, dimensiones percibidas, proporciones, suelo, iluminación, modelos,
texturas, animaciones, partículas, indicadores, barras y HUD. El punto de partida
conserva el aspecto actual del simulador; esta entrega no cambia sus proporciones.

El profesional puede modificar los recursos y scripts de presentación necesarios.
El resultado se incorporará después a todos los modos del proyecto completo. Mantener
las reglas de daño, alcance, colisión, velocidad de movimiento y tiempos de impacto
separadas de los cambios visuales. Si una corrección visual exige cambiar un contrato
con la simulación, documentar ese cambio para revisarlo durante la integración.

## Inicio y controles

Unity **6000.6.5f1**, URP **17.6.0**. Escena: `Assets/Scenes/MidLane.unity`.
Menú **Visual Workshop → Abrir práctica** y luego Play. Irelia comienza en nivel 6,
con 50.000 de oro y maná ilimitado. Hay tres dummies fijos, uno evasivo y otro que
recorre un rectángulo de 800 × 800 unidades LoL a velocidad 365. Las torres no atacan.
Las marcas de dummies y el reinicio de enfriamientos conservan las reglas actuales.

| Control | Acción |
|---|---|
| Clic derecho | Mover / atacar un objetivo |
| Clic de rueda | Mover / atacar cerca del cursor |
| Q | Dash al objetivo bajo el cursor |
| Mantener y soltar W | Cargar y soltar W; indicador de dirección |
| E, E | Colocar las dos cuchillas y activar su convergencia |
| Clic izquierdo 2 s sobre el icono E | Alternar marcas infinitas y E sin enfriamiento |
| Clic izquierdo 2 s sobre cualquier dummy/minion | Alternar los cinco modelos: guerrero en el centro fijo y magos en los otros cuatro |
| Clic izquierdo corto sobre una unidad | Inspeccionar vida y maná en el panel superior izquierdo |
| Mantener y soltar R | Apuntar y lanzar R; Esc/clic derecho cancela |
| D / F | Hechizos de invocador; sus iconos permiten cambiarlos |
| P | Tienda; comprar en cualquier lugar |
| S | Detenerse |
| Y / Espacio | Liberar o fijar cámara / centrar en Irelia |
| Flechas / bordes | Desplazar cámara libre |
| Esc / F5 | Pausa / reiniciar Práctica |
| Mute / ms | Silenciar / latencia simulada |
| F8 / F9 | Panel de ajustes visuales / superposición de referencias |
| Icono de recarga del HUD | Elegir otra página de runas; reinicia Práctica |

## Arquitectura de la escena

La escena guardada tiene un `LaneWorld`. **El suelo, cámara, luz, torres, Irelia y
dummies se crean al entrar en Play**, dentro de `Arena` y `Round`. La escena vacía
en modo edición es esperable. Cambiar un Transform solo durante Play no guarda el
resultado. Aplicar ajustes persistentes en los prefabs, configuraciones o scripts que
crean esos objetos. No se creó un segundo sistema visual ni una escena estática que
el juego ignore: se trabaja sobre la misma presentación que luego se integrará.

## Mapa de archivos editables

| Área | Archivos activos / puntos de entrada |
|---|---|
| Configuración visual | `Assets/Resources/ScenePresentation.json`, `Assets/Scripts/ScenePresentationConfig.cs` |
| Cámara | `Assets/Scripts/IsometricCamera.cs`; desplazamiento de seguimiento en `Assets/Resources/SandboxSettings.asset` |
| Tamaño, pivote y orientación | `Assets/Scripts/PracticeModels.cs`, `CameraFacingVisualTilt.cs`, `TrainingDummyModel.cs` |
| Irelia: prefab, rig, texturas y materiales | `Assets/Resources/Models/IreliaOfficial/` |
| Reproducción y mezcla de animaciones | `Assets/Scripts/IreliaRigAnimation.cs`, `IreliaModelAnimation.cs`, `IreliaGlbAssets.cs` |
| Dummy: prefab, rig y clips Idle/Hit | `Assets/Resources/Models/TrainingDummy/`, `Assets/Scripts/TrainingDummyModel.cs` |
| Apariencias de minions de práctica | `TrainingDummy.Appearance.cs`, `MinionVisuals.cs`, `MinionModelAnimation.cs`, `Assets/Resources/Models/LaneUnitsOfficial/` |
| Torres de ambos equipos | `Assets/Resources/Models/LaneUnitsOfficial/BlueTower/` y `RedTower/`; `PracticeModels.CreateTower` |
| Cuchillas E y convergencia | `Assets/Scripts/BladeLink.cs`, `IreliaEConvergenceVisual.cs`, `Assets/Resources/Models/IreliaOfficial/E/` |
| R: vuelo, cuchillas y muro | `Assets/Scripts/BladeWave.cs`, `BladeWall.cs`, `IreliaRVisuals.cs`, `Assets/Resources/Models/IreliaOfficial/R/` |
| Indicadores / marca | `IreliaRAim.cs`, `IreliaMarkVisual.cs`, `Assets/Resources/Shaders/Irelia*.shader` |
| Suelo y entorno | `Assets/Scripts/LaneTerrain.cs`, `LaneBoundaryVisuals.cs`, `LaneWorld.BuildArena`, `Assets/Resources/Terrain/` |
| Luz y renderizado | `LaneWorld.BuildArena`, `Assets/Rendering/`, `ProjectSettings/GraphicsSettings.asset`, `QualitySettings.asset` |
| HUD y barras | `Assets/Scripts/PracticeHud*.cs`, `ChampionHudLayout.cs`, `Assets/Resources/HudIcons/` |
| Sonidos y voces activos | `Assets/Resources/Audio/Irelia/`, `IreliaVoice/`, `Flash/` y sus clases de audio |
| Originales de Riot | `CharacterAssets/Irelia/Base/Original/` y `CharacterAssets/TrainingDummy/Base/Original/` |
| Conversores | `Tools/Import-*`, `Tools/Extract-IreliaRVoice.py`; algunos requieren herramientas externas indicadas en sus argumentos |

Los originales incluyen **68 animaciones corporales, 4 de las cuchillas**, recursos
de partículas y bancos completos de audio/voces disponibles localmente. El runtime
usa actualmente **21 clips Unity**. Se pueden retocar o reemplazar y convertir otros
clips originales. No todos los originales están conectados al runtime; revisar el
mapeo de carga en `IreliaGlbAssets` al incorporar animaciones nuevas.

Los efectos actuales combinan mallas/texturas originales con lógica visual propia.
No son todos prefabs de Particle System: algunos se construyen por código. Se pueden
reemplazar por sistemas de partículas, VFX o prefabs conservando los eventos de la
simulación que los disparan. Toda dependencia nueva debe viajar dentro del proyecto.

## Escala y cámara: detalles importantes

La simulación usa aproximadamente **100 unidades LoL = 1 unidad Unity**. El radio
lógico de Irelia y dummy es 0,65. La escala del GameObject que contiene `Combatant`
y sus colliders debe conservarse; los ajustes visuales van bajo `Model`,
`ChampionBody`, `Geometry` o dentro del prefab.

`PracticeModels.FitHeight` normaliza el tamaño del modelo antes de aplicar el
multiplicador visual. Por eso cambiar solo el tamaño global del prefab puede ser
compensado por esa normalización. Las torres también se normalizan por su altura
visible, preservando la parte de su base enterrada. Revisar esos puntos al ajustar
dimensiones y pivotes. Los clips pueden sobrescribir la escala de huesos, por lo
que la escala visual suele aplicarse en un padre externo al rig.

Valores iniciales: FOV 32,5°, pitch Unity +55°, yaw 45°, multiplicador 1,65,
inclinación 15°, proporción de torre 2,7. La configuración **limita** algunos valores
en `ScenePresentationConfig.Load`. Además, `camera_distance` se recalcula como
`CameraMaxDistance * camera_zoom` e `IsometricCamera` usa ese valor derivado: usar
`camera_zoom` para ajustar el encuadre; editar solo el campo JSON de distancia no
lo cambia. Si se necesita otro comportamiento,
modificar y documentar esos puntos. Tampoco se presupone que dichos valores sean
una extracción exacta de la cámara de Riot: son el ajuste actual del simulador.

## Guardar y devolver

Editar los archivos activos en `Assets`; preservar sus `.meta` y los GUID. Evitar
regenerar todos los recursos con un importador después de retocarlos: podría
sobrescribir cambios de animación, material o prefab. Mantener los archivos de
autoría adicionales en el proyecto y anotar herramienta/versión.

Comparar desde Game con la misma resolución, cámara y posición, incluyendo ambos
lados del mapa. `References/imagelol*.jpg` contiene capturas de LoL aportadas por el
usuario. `imageSimulator*.png` son capturas antiguas, no la línea base de esta entrega.
La captura validada de esta copia está en `Handoff/Validation/practice-preview.png`.
Es una referencia de la escena 3D; no incluye la capa OnGUI del HUD.

Antes de devolver, guardar fuera de Play los archivos editados, añadir capturas de
antes/después y completar `Handoff/ARTIST_NOTES.md`. Luego ejecutar:

```powershell
python Tools/Export-VisualChanges.py
```

Se crea `Deliverables/VisualChanges.zip` con los archivos cambiados, sus `.meta` y un
informe para integración. El script necesita Python 3, no librerías adicionales.
También se puede devolver el proyecto completo limpio, sin cachés. **No ejecutar
`--finalize-baseline` después de empezar a editar**: la entrega ya tiene su línea base.
