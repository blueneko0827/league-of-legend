# Integración visual · Hito 1

Entrega revisada: `DemoMidlanePractice-hito-1/DemoMidlanePractice-hito-1`.
Base del simulador completo: commit `9dc871c`. La comparación con
`Handoff/Baseline.json` identifica 24 diferencias de la entrega; no se copian
cachés, adaptadores exclusivos de la demo ni versiones antiguas del gameplay.

## Presentación incorporada

- Dummies a 0,86 de la altura visual del campeón, inclinados dentro de un
  contenedor `Geometry`; sus radios, velocidad, impactos y marcas no cambian.
- Torres a 2,7 de la altura visual del campeón; minions conservan 0,6.
- Iluminación y ambiente plano configurables; sombras auxiliares que comparten
  malla y huesos de los modelos originales, sin colliders ni materiales nuevos
  sobre las superficies visibles. Reaplicar la creación no duplica las sombras.
- URP con sombras suaves, atlas 4096 y distancia 75. `unit_shadows` del JSON
  permite desactivar los renderers auxiliares al iniciar la partida. La calidad
  de este atlas aumenta el coste gráfico frente a la versión anterior.
- Rocas facetadas y textura de piedra/musgo generadas por código.
- Barras con nivel, segmentos de vida, maná, escudo blanco y pasiva de Irelia;
  los rectángulos de selección coinciden con los de presentación.
- Dummies con último golpe, DPS y total. Se mantiene el reinicio previo de
  estadísticas y la recuperación de la marca de Práctica a los 5 segundos.
- F8: ajustes en vivo de cámara, inclinación y luz. Los cambios de escala se
  aplican reiniciando el modo actual. «Guardar JSON» está disponible en el
  Editor; no guarda automáticamente los ensayos de sliders.
- F9: superposición de las tres referencias incluidas en `References`;
  opacidad y selección de imagen se controlan desde F8.

## Geometría aprobada

El usuario aprobó incorporar también la reducción del mapa. `TowerToTowerRules`
usa `SummonersRiftGeometry.MidOuterTowerDistance`: 37,5963788 unidades Unity,
equivalentes a 3759,63788 unidades de LoL, entre centros de torres. Se conserva
el ancho de 16 Unity (1600 unidades LoL). Las torres quedan en X ±18,7981894;
terreno, límites, puntos de aparición, arcos defensivos y navegación usan ese
mismo tamaño. Este cambio acorta efectivamente recorridos y tiempos de llegada.

Los alcances, radios, velocidades y daños no se reescalan. El dummy móvil de
Práctica conserva su área de 800 × 800 y velocidad de 365. La posición guardada
del dummy de PvP conserva su desplazamiento relativo a la torre propia, se
refleja al cambiar de equipo y se valida contra los nuevos límites.

## Funcionalidad preservada

La fusión manual de `Combatant`, `LaneWorld`, `TrainingDummy` y `PracticeHud`
incorpora solo sus cambios de presentación. Se conservan:

- Práctica, PvP y Normalplay, selección/ocultación de rival y preferencias.
- Runas, summoners, tienda, compras y ventas en cualquier lugar.
- Zona de recuperación eliminada y recall deshabilitado.
- Regeneración intrínseca y restauración de HP/MP/CD tras kills según las
  reglas actuales de PvP y Normalplay.
- Tablas de niveles/oro/XP, progresión al 50% XP y poción inicial de Normalplay.
- IA reciente de Mel/Sett y navegación/separación de minions sin oscilación.
- Cooldowns de entrenamiento y temporizadores actuales de los dummies.

No se importa la eliminación de un archivo fuente original de Irelia que falta
en la devolución: no es necesaria para la mejora visual. Se conservan modelos,
clips, sonidos, voces y partículas actuales. La marca de Irelia sigue siendo el
rombo previo; el Hito 1 no contiene su reemplazo oficial.

## Validación

Las verificaciones se ejecutan en la copia aislada
`TestResults/PlayerValidationProject`, sin cerrar el editor del usuario. Los
informes `visual-hito1-*.xml`, las capturas `visual-hito1-scene-*.png` y el resumen
de hashes quedan en `TestResults` (excluido de Git). Las pruebas históricas de
geometría y escala se ajustan a las nuevas medidas aprobadas; los contratos de
combate conservan sus comprobaciones anteriores.

Resultado final: **271 pruebas distintas aprobadas**, incluidas las capturas
Direct3D de Práctica y ambos lados de Normalplay. Los 122 archivos de runtime
y configuración verificados coinciden por SHA-256 con la copia validada. Resumen:
`TestResults/visual-hito1-summary.json`. Se conserva sin cambios el código reciente
de IA, separación/rutas de minions y economía.
