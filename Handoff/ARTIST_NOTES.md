# Notas para la devolución

## Hito 2.1 · Ataque básico, pasiva y Q (efectos, animaciones y sonidos)

- **Herramientas y versiones utilizadas:** Unity 6000.6.5f1, URP 17.6.0. Python 3 + Pillow
  para `Tools/Import-IreliaVfx.py` (lee los `.bin`, `.tex` y `.scb` originales sin otras
  dependencias). Sonidos con wwiser v20260808 y vgmstream r2117, igual que
  `Tools/Import-IreliaAudio.py`. Sin paquetes nuevos de Unity.

- **Efectos oficiales (nuevo):** los efectos se generan a partir de las definiciones
  originales de Riot (`VfxSystemDefinitionData` de los `.bin` de la skin base), no se
  dibujaron a mano. `Tools/Import-IreliaVfx.py` exporta 27 sistemas (104 emisores) a
  `Assets/Resources/Vfx/Irelia/IreliaVfx.json`, 40 texturas a `.../Textures/*.png` y 10
  mallas `.scb` (dentro del JSON), además de los eventos de partículas del grafo de
  animación original (qué clip lanza qué efecto, en qué frame y desde qué hueso).
  Los GUID se derivan del nombre: reimportar no rompe referencias.
  - Runtime: `RiotVfxData.cs`, `RiotVfxSimulation.cs`, `RiotVfxLibrary.cs`,
    `RiotVfxPlayer.cs` y el shader `Resources/Shaders/RiotParticle.shader`. Reproduce
    emisión por tasa o partícula única, retardos, vida, curvas de color/escala/rotación,
    tablas de probabilidad, velocidad, arrastre y aceleración, billboards, quads, mallas,
    estelas, silueta del personaje (malla adjunta), flipbooks, desplazamiento de UV,
    erosión alfa, textura multiplicada y los modos de fusión de Riot.
  - **Ataque básico:** barridos `BA_Far_swipe_01/02` en el frame indicado por los clips
    oficiales Attack/Attack2, escalados con la velocidad de ataque; si la preparación se
    cancela antes del impacto, el barrido se corta. Impacto `BA_Hit`, `BA_Hit_02` o
    `BA_Hit_Crit` (crítico) en el hueso del pecho del objetivo.
  - **Pasiva:** brillo en una cuchilla flotante por carga (`P_Blades_Stacks_1_2_3`) y en
    las cuatro al máximo (`P_Blades_Stacks_4`); activación a 4 cargas (`P_buf`: destello
    sobre la silueta de Irelia y aura); golpes potenciados `P_Hit_02/04` en el pecho del
    objetivo, junto con el impacto del ataque, y barridos `P_Far_swipe_01/02` superpuestos;
    `P_Hit_4stack_left` cuando la Q completa las cargas.
  - **Q:** `Q_Dash` (borde de avance, estela, onda de choque y silueta brillante),
    `Q_tar` en el objetivo, `Q_heal` en Irelia, `Q_Proc` cuando la Q se reinicia (marca o
    muerte) y la marca oficial `Q_Mark_SelfOnly`, que reemplaza el rombo cian.
  - Ajustes en `ScenePresentation.json`: `"official_vfx": false` vuelve a la presentación
    anterior (incluido el rombo); `vfx_scale` (0,5–2) escala los efectos alrededor de su
    punto de anclaje. Con 1 conservan la proporción del campeón agrandado
    (`ChampionVisualHeight / 228` unidades por unidad LoL).
  - Convenciones del conversor, por si hay que tocarlo: ejes LoL → Unity con X espejado
    como el importador de personajes (rotaciones: se invierten Y y Z); los quads de Riot
    miden ±1 × escala; los huesos raíz de buff (`C_Buffbone_Glb_Layout_Loc`, etc.) están
    girados 180° en `irelia_base.skl`, y los barridos lo compensan con su propia rotación.

- **Modelos, pivotes, rig y prefabs modificados:** ninguno. Los efectos se anclan a los
  huesos que ya existen en los prefabs (`C_Buffbone_Glb_*`, `Buffbone_Glb_Ground_Loc`,
  `Sword1..4_Skn` y el pecho del objetivo).

- **Animaciones modificadas/nuevas y su mapeo a acciones:** no se agregaron clips. Se
  usan los clips oficiales que ya tenía el proyecto (Attack = `irelia_attack_01`,
  Attack2 = `irelia_attack_02`, QDash/QToIdle/QToRun/QChain) y ahora los efectos siguen
  sus eventos de partículas originales.

- **Sonidos nuevos:** `Tools/Import-IreliaPassiveAudio.py` agrega, con la misma ganancia
  que los clips de ataque/Q existentes: `CritHit_1..4` (impacto crítico), `PSparkles_1..12`
  (golpe potenciado), `PMaxSparkles_1..7` (golpe que completa las cargas) y `PMax_1..5`
  (activación a 4 cargas). Manifiesto: `Assets/SourceAudio/IreliaOfficial/PassiveSources.json`.
  Los clips existentes no se modificaron.

- **Recursos o paquetes nuevos y sus requisitos:** scripts nuevos (con `.meta`):
  `RiotVfxData.cs`, `RiotVfxSimulation.cs`, `RiotVfxLibrary.cs`, `RiotVfxPlayer.cs`,
  `IreliaVfx.cs`; shader `RiotParticle.shader`; carpeta `Assets/Resources/Vfx/Irelia/`;
  herramientas `Tools/Import-IreliaVfx.py` y `Tools/Import-IreliaPassiveAudio.py`.

- **Cambios que requieren fusionar código o afectan contratos con la simulación:**
  - `BladeDuelist.cs`: eventos de solo lectura `OnHitLanded(objetivo, potenciado, crítico,
    desdeQ)` y `QLanded(objetivo, reinicio)`; `OnBasicAttackHit` recibe dos parámetros
    opcionales. Daño, cargas y tiempos no cambian.
  - `Combatant.cs`: el crítico se guarda en una variable antes de calcular el daño (mismo
    cálculo y mismo orden de `Random.value`) y se pasa al audio y al evento; Irelia agrega
    el componente `IreliaVfx`.
  - `IreliaAudio.cs`: nuevas señales `CritHit`, `PSparkles`, `PMaxSparkles`, `PMax`
    (al final del enum) y `BasicHit(variante, crítico)`.
  - `IreliaMarkVisual.cs`: muestra la marca oficial; el rombo queda como respaldo.
  - `Tests/PlayMode/PracticeVisualHandoffTests.cs`: pruebas nuevas que verifican que los
    sistemas cargan, que el ataque básico y la Q lanzan sus efectos y que el golpe
    potenciado estalla en el pecho del dummy.

- **Capturas de comparación y resolución utilizada:** pendiente, 1280×720 en Game.

- **Limitaciones pendientes:**
  - El renderizador de partículas de Riot no es público: la interpretación de algunos
    campos (giro de billboards, tamaño base de los quads, `uvMode`) se dedujo de los datos.
    Se ajusta con `vfx_scale` o en `RiotVfxSimulation.cs`.
  - Omitidos: 2 emisores de distorsión (refracción de pantalla, requiere otra pasada de
    render) y 1 emisor de polvo de la Q cuya textura (de Kai'Sa) no viene en los archivos.
  - `P_Hit_4stack_right` no está en los archivos entregados: se usa la versión izquierda.
  - El simulador decide el crítico al impactar, no al iniciar el ataque; por eso se usa el
    impacto y el sonido críticos, pero no la animación/sonido de lanzamiento crítico.
  - El sonido `Q_Hit_reset` no está en el banco base (igual que antes).
  - LoL mezcla en espacio gamma y el proyecto es lineal: las texturas de efectos se importan
    sin sRGB y el shader convierte a lineal solo las mezclas alfa; los aditivos usan su valor
    original, que sobre el suelo se acerca al brillo de LoL (fondos muy claros: algo más).
- **Ajustes tras comparar con LoL (video del cliente):** la marca de Q se dibuja alrededor del
  torso del objetivo y escala con su altura (antes era pequeña sobre la cabeza); la estela de la
  Q queda plana sobre el camino, visible desde la cámara; los efectos aditivos ya no se ven
  apagados por la conversión lineal.
- **Bordes suaves:** el aura de la pasiva (textura desplazada y multiplicada) mostraba el
  contorno cuadrado del quad. Los billboards y quads tienen ahora un borde suavizado y los de
  textura desplazada/multiplicada un desvanecimiento elíptico (`_EdgeFade` en el shader).
- **Pasiva sobre los dummies:** los golpes potenciados (`P_Hit_02/04`, `P_Hit_4stack_left`)
  se anclaban a los pies del objetivo, donde quedaban tapados por el dummy y su panel de daño
  (y la variante `_04`, girada 180°, quedaba bajo el suelo). Ahora estallan en el pecho del
  objetivo, como el impacto del ataque y como en LoL. Los impactos sobre un objetivo se
  dibujan acercados a la cámara según el radio del objetivo (`_DepthPull`; la posición en
  pantalla no cambia), para que su cuerpo no tape la mitad del destello. Prueba nueva:
  `EmpoweredHitBurstsAtTheDummyChest`.

## Hito 1 · Escala, cámara, iluminación, sombras, suelo, rocas y barras de vida

- **Herramientas y versiones utilizadas:** Unity 6000.6.5f1, URP 17.6.0. Todo el
  trabajo se hizo en scripts y configuración (sin paquetes ni dependencias nuevas).
  Las rocas y su textura se generan por código; no hay archivos de autoría externos.

- **Cambios de cámara, encuadre y escala:**
  - **Dummy:** antes se ajustaba a una altura fija de 1,8 unidades y no tenía la
    inclinación hacia la cámara que sí tiene Irelia. En pantalla quedaba a ~0,5 de la
    altura de Irelia; en las capturas de LoL (`References/imagelol3.jpg`) es ~0,86.
    Ahora su altura es `altura visual del campeón × dummy_height_ratio` (0,86) y usa la
    misma inclinación (`CameraFacingVisualTilt`) dentro de un contenedor `Geometry`,
    igual que los campeones. El collider y el radio de juego del dummy no cambian.
  - **Torres:** `tower_height_ratio` pasa de 3,0 a 2,7, la proporción de las mallas
    oficiales (torre ≈ 610–630 unidades de alto sobre el suelo, Irelia ≈ 228).
    Irelia mantiene su tamaño.
  - **Cámara:** mismos valores iniciales (FOV 32,5°, inclinación 55°, giro 45°). Se
    agregó `camera_zoom` (multiplicador de distancia, 1 = encuadre actual) y ahora la
    cámara puede reaplicarse en vivo.
  - Nuevos campos en `ScenePresentation.json`: `camera_zoom`, `dummy_height_ratio`,
    `sun_pitch_degrees`, `sun_yaw_degrees`, `sun_intensity`, `sun_color`,
    `shadow_strength`, `ambient_color`, `unit_shadows`. Los rangos aceptados están en
    `ScenePresentationConfig.Clamp` (FOV ampliado a 25–45°, inclinación a 45–70°).

- **Modelos, pivotes, rig y prefabs modificados:** ningún prefab, malla, rig ni clip se
  modificó. El dummy se instancia ahora bajo `Model/Geometry/Training dummy model`.

- **Animaciones modificadas/nuevas y su mapeo a acciones:** ninguna en este hito.

- **Partículas, shaders, materiales e iluminación:**
  - **Sombras de unidades:** los materiales oficiales de Irelia, dummy y torres usan
    `URP/Unlit`, que no proyecta sombras. `UnitShadows` agrega a cada renderer una copia
    "solo sombras" que comparte malla y huesos; el material y los colores originales no
    se tocan. Las copias siguen el estado visible/oculto de su original. Se puede
    desactivar con `"unit_shadows": false`.
  - **Luz:** el sol y el ambiente se configuran desde el JSON (`LaneWorld.ApplyLighting`).
    La escena estaba en ambiente "Skybox", que ignoraba el color de ambiente del código;
    ahora es ambiente plano. Sombras dirigidas hacia la derecha de la pantalla, como en
    las capturas de LoL. El suelo queda un poco más oscuro y frío.
  - `Assets/Rendering/MidLaneURP.asset`: sombras suaves activadas, mapa de sombras de
    2048 a 4096 y distancia de sombras de 50 a 75 (para que no se corten con el zoom).
    En equipos de pocos recursos se puede volver a 2048.
  - Los personajes son Unlit, así que la luz no cambia sus colores: solo afecta al suelo
    y las rocas.

- **HUD/barras y otros cambios:**
  - **Barras sobre las unidades** al estilo de la herramienta de práctica: recuadro de
    nivel, vida segmentada cada 100 PV (marca más gruesa cada 1000), franja de maná,
    escudo en blanco a continuación de la vida, y los cuatro puntos de la pasiva de
    Irelia bajo su barra. Verde para Irelia, rojo para enemigos.
  - **Dummies:** nombre ("Dummy fijo", "Dummy evasivo", "Dummy aleatorio", "Dummy Q")
    sobre la barra y, bajo los pies, "Último golpe / DPS / Total". `TrainingDummy`
    registra el último golpe y el inicio de la secuencia solo para mostrarlos.
  - El área de clic sobre la barra (`TargetPicker.TryHealthBar`) usa el mismo rectángulo
    que se dibuja (`WorldBarLayout.Frame`).
  - **Rocas del borde:** cuatro formas facetadas con textura pintada por código (piedra
    más oscura en la base y musgo en la parte superior), agrupadas con piedras pequeñas.
    Siguen generándose con semilla fija.
  - **Panel de ajuste (F8):** cámara, inclinación de modelos, luz y sombras en vivo; la escala se aplica con "Aplicar escala", que reinicia Práctica. En el
    Editor, "Guardar JSON" escribe `Assets/Resources/ScenePresentation.json`.
    **F9** superpone las capturas `References/imagelol*` con opacidad regulable para
    comparar encuadre y proporciones. Ambas son herramientas de esta copia de trabajo.

- **Recursos o paquetes nuevos y sus requisitos:** ninguno. Scripts nuevos (con `.meta`):
  `UnitShadows.cs`, `LaneWorld.Presentation.cs`, `PracticeHud.WorldBars.cs`,
  `VisualTuningPanel.cs`.

- **Archivos de autoría incluidos:** no aplica (todo es código).

- **Cambios que requieren fusionar código o afectan contratos con la simulación:**
  - `Core/TowerToTowerRules.cs`: `LengthUnits` pasa de 5150 a la distancia oficial entre
    las torres externas de mid (`SummonersRiftGeometry.MidOuterTowerDistance`, ~3760). Las
    torres quedan a ±18,8 unidades del centro; límites del mapa, puntos de recuperación,
    terreno y cámara se recalculan solos. Dummies e Irelia siguen dentro de la línea.
  - `Combatant.cs`: `VisualHeight` del dummy usa `DummyVisualHeight` y
    `VisualTiltRotation` ya no excluye al dummy. Afecta solo anclajes de HUD y el área
    de selección con el mouse, no colisiones ni alcances.
  - `TrainingDummy.cs`: solo agrega `LastHitAmount`, `SequenceStartedAt` y
    `DamagePerSecond` (lectura para el HUD); el cálculo de `TotalDamage` no cambia.
  - `LaneWorld.cs`: la luz pasa a `ApplyLighting()` y se agrega `VisualTuningPanel`.
    En el proyecto completo se puede omitir el panel quitando esa línea.
  - `PracticeHud.cs`: `DrawWorldBars` y `DrawDummyDamage` se movieron a
    `PracticeHud.WorldBars.cs` (con soporte para torres, súbditos, Sett y escudos de
    otros modos) y `BlocksPointer` consulta el panel F8.

- **Capturas de comparación y resolución utilizada:** pendiente. Tomar en la vista Game
  a 1280×720 con la cámara fija en Irelia al iniciar Práctica, antes y después.

- **Limitaciones pendientes:**
  - El rombo cian sobre los dummies es la marca de Irelia (lógica de práctica); su
    aspecto se trabajará con los efectos en el hito 2.
  - Los valores de cámara no son una extracción de la cámara de Riot. Con F9 se puede
    afinar el encuadre contra las capturas y guardarlo.
