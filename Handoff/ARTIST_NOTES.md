# Notas para la devolución

Hito 2.1 · Ataque básico, pasiva y Q (efectos, animaciones y sonidos).

- Herramientas y versiones utilizadas: Unity 6000.6.5f1, URP 17.6.0. Python 3 + Pillow
  para `Tools/Import-IreliaVfx.py` (lee los `.bin`, `.tex` y `.scb` originales sin otras
  dependencias). Sonidos con wwiser v20260808 y vgmstream r2117, igual que
  `Tools/Import-IreliaAudio.py`. Sin paquetes nuevos de Unity.

- Cambios de cámara, encuadre y escala: ninguno. Ajustes nuevos en
  `ScenePresentation.json`: `"official_vfx": false` vuelve a la presentación anterior
  (incluido el rombo de la marca); `vfx_scale` (0,5–2) escala los efectos alrededor de su
  punto de anclaje. Con 1 conservan la proporción del campeón agrandado
  (`ChampionVisualHeight / 228` unidades por unidad LoL).

- Modelos, pivotes, rig y prefabs modificados: ninguno. Los efectos se anclan a los huesos
  que ya existen en los prefabs (`C_Buffbone_Glb_*`, `Buffbone_Glb_Ground_Loc`,
  `Sword1..4_Skn` y el pecho del objetivo). Si un dummy se muestra como minion, se usan los
  huesos del modelo visible.

- Animaciones modificadas/nuevas y su mapeo a acciones: no se agregaron clips. Se usan los
  clips oficiales que ya tenía el proyecto (Attack = `irelia_attack_01`, Attack2 =
  `irelia_attack_02`, QDash/QToIdle/QToRun/QChain) y ahora los efectos siguen sus eventos de
  partículas originales (frame y hueso), escalados con la velocidad de ataque.

- Partículas, shaders, materiales e iluminación:
  - Los efectos se generan a partir de las definiciones originales de Riot
    (`VfxSystemDefinitionData` de los `.bin` de la skin base), no se dibujaron a mano.
    `Tools/Import-IreliaVfx.py` exporta 27 sistemas (104 emisores) a
    `Assets/Resources/Vfx/Irelia/IreliaVfx.json`, 40 texturas a `.../Textures/*.png` y 10
    mallas `.scb` (dentro del JSON), además de los eventos de partículas del grafo de
    animación original. Los GUID se derivan del nombre: reimportar no rompe referencias.
  - Runtime: `RiotVfxData.cs`, `RiotVfxSimulation.cs`, `RiotVfxLibrary.cs`,
    `RiotVfxPlayer.cs` y el shader `Resources/Shaders/RiotParticle.shader`. Reproduce
    emisión por tasa o partícula única, retardos, vida, curvas de color/escala/rotación,
    tablas de probabilidad, velocidad, arrastre y aceleración, billboards, quads, mallas,
    estelas, silueta del personaje (malla adjunta), flipbooks, desplazamiento de UV,
    erosión alfa, textura multiplicada y los modos de fusión de Riot.
  - Ataque básico: barridos `BA_Far_swipe_01/02` en el frame indicado por los clips; si la
    preparación se cancela antes del impacto, el barrido se corta. Impacto `BA_Hit`,
    `BA_Hit_02` o `BA_Hit_Crit` (crítico) en el pecho del objetivo.
  - Pasiva: brillo en una cuchilla flotante por carga (`P_Blades_Stacks_1_2_3`) y en las
    cuatro al máximo (`P_Blades_Stacks_4`); activación a 4 cargas (`P_buf`: destello sobre
    la silueta de Irelia y aura); golpes potenciados `P_Hit_02/04` en el pecho del objetivo
    junto con el impacto del ataque, y barridos `P_Far_swipe_01/02` superpuestos;
    `P_Hit_4stack_left` cuando la Q completa las cargas.
  - Cuchillas flotantes con 4 cargas: el material oficial de las cuchillas
    (`Irelia_Blades_Passive_inst`) cambia su textura por la versión crema/bronce
    (`irelia_base_blades_passive_4_texture`) mientras `IreliaPassiveStacks` vale 4; ahora se
    aplica igual (`BladesPassive4.png`, exportada por `Tools/Import-IreliaVfx.py`, con un
    MaterialPropertyBlock para no tocar `Material2`, que comparten las cuchillas de E/R).
  - Q: `Q_Dash` (borde de avance, estela, onda de choque y silueta brillante), `Q_tar` en el
    objetivo, `Q_heal` en Irelia, `Q_Proc` cuando la Q se reinicia (marca o muerte) y la
    marca oficial `Q_Mark_SelfOnly` alrededor del torso del objetivo, que reemplaza el
    rombo cian.
  - Ajustes tras comparar con el video de LoL: colores en espacio gamma como LoL (las
    texturas de efectos se importan sin sRGB; el shader convierte a lineal solo las mezclas
    alfa), marca de Q escalada con la altura del objetivo, estela de la Q plana sobre el
    camino, borde suavizado en billboards/quads para que el aura de la pasiva no muestre un
    cuadrado (`_EdgeFade`), y los impactos sobre un objetivo se dibujan acercados a la
    cámara según su radio (`_DepthPull`; la posición en pantalla no cambia) para que su
    cuerpo no tape el destello.
  - Convenciones del conversor, por si hay que tocarlo: ejes LoL → Unity con X espejado
    como el importador de personajes (rotaciones: se invierten Y y Z); los quads de Riot
    miden ±1 × escala; los huesos raíz de buff (`C_Buffbone_Glb_Layout_Loc`, etc.) están
    girados 180° en `irelia_base.skl`, y los barridos lo compensan con su propia rotación.

- HUD/barras y otros cambios: sonidos nuevos con `Tools/Import-IreliaPassiveAudio.py`, con
  la misma ganancia que los clips de ataque/Q existentes: `CritHit_1..4` (impacto crítico),
  `PSparkles_1..12` (golpe potenciado), `PMaxSparkles_1..7` (golpe que completa las cargas)
  y `PMax_1..5` (activación a 4 cargas). Manifiesto:
  `Assets/SourceAudio/IreliaOfficial/PassiveSources.json`. Los clips existentes no se
  modificaron.
  - Cargas bajo la barra de Irelia: como en LoL, cuatro casillas; vacía = punto oscuro,
    carga = segmento inclinado (azul acero con 1–3 cargas, lima con 4).
  - Icono de cargas de la pasiva: ahora como el ícono de buff del cliente de LoL (ícono
    oficial de la pasiva, marco fino, número de cargas abajo a la derecha y barrido horario
    que oscurece el ícono a medida que vence la duración; se detiene mientras se carga la W).
  - Barra sobre los campeones: más alta para no tapar la cara de Irelia (el modelo oficial
    sobresale de la altura de selección). Se ajusta con `champion_bar_lift` en
    `ScenePresentation.json` (fracción de la altura visual del campeón, 0,36 por defecto;
    0 = posición anterior). Los dummies no cambian. El clic sobre la barra usa la misma
    posición.

- Recursos o paquetes nuevos y sus requisitos: scripts nuevos (con `.meta`):
  `RiotVfxData.cs`, `RiotVfxSimulation.cs`, `RiotVfxLibrary.cs`, `RiotVfxPlayer.cs`,
  `IreliaVfx.cs`; shader `RiotParticle.shader`; carpeta `Assets/Resources/Vfx/Irelia/`;
  audios en `Assets/Resources/Audio/Irelia/`. Sin paquetes nuevos.

- Archivos de autoría incluidos: ninguno externo. Todo se reconstruye desde los archivos
  originales con `Tools/Import-IreliaVfx.py` y `Tools/Import-IreliaPassiveAudio.py`.

- Cambios que requieren fusionar código o afectan contratos con la simulación:
  - `BladeDuelist.cs`: eventos de solo lectura `OnHitLanded(objetivo, potenciado, crítico,
    desdeQ)` y `QLanded(objetivo, reinicio)`; `OnBasicAttackHit` recibe dos parámetros
    opcionales. Daño, cargas y tiempos no cambian.
  - `Combatant.cs`: el crítico se guarda en una variable antes de calcular el daño (mismo
    cálculo y mismo orden de `Random.value`) y se pasa al audio y al evento; Irelia agrega
    el componente `IreliaVfx`.
  - `IreliaAudio.cs`: nuevas señales `CritHit`, `PSparkles`, `PMaxSparkles`, `PMax`
    (al final del enum) y `BasicHit(variante, crítico)`.
  - `IreliaMarkVisual.cs`: muestra la marca oficial; el rombo queda como respaldo.
  - `ScenePresentationConfig.cs`: campos `official_vfx`, `vfx_scale` y `champion_bar_lift`.
  - `BladeDuelist.cs`: propiedad de solo lectura `PassiveRemaining` para el temporizador del HUD.
  - `Tests/PlayMode/PracticeVisualHandoffTests.cs`: pruebas nuevas que verifican que los
    sistemas cargan, que el ataque básico y la Q lanzan sus efectos y que el golpe
    potenciado estalla en el pecho del dummy.

- Capturas de comparación y resolución utilizada: pendientes, 1280×720 en Game.

- Limitaciones pendientes:
  - El renderizador de partículas de Riot no es público: la interpretación de algunos
    campos (giro de billboards, tamaño base de los quads, `uvMode`) se dedujo de los datos.
    Se ajusta con `vfx_scale` o en `RiotVfxSimulation.cs`.
  - Omitidos: 2 emisores de distorsión (refracción de pantalla, requiere otra pasada de
    render) y 1 emisor de polvo de la Q cuya textura (de Kai'Sa) no viene en los archivos.
  - `P_Hit_4stack_right` no está en los archivos entregados: se usa la versión izquierda.
  - El simulador decide el crítico al impactar, no al iniciar el ataque; por eso se usa el
    impacto y el sonido críticos, pero no la animación/sonido de lanzamiento crítico.
  - El sonido `Q_Hit_reset` no está en el banco base (igual que antes).
  - Los aditivos usan su valor gamma original, que sobre el suelo se acerca al brillo de
    LoL (sobre fondos muy claros se ven algo más brillantes).
