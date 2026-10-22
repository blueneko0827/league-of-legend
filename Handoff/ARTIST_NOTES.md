# Notas para la devolución

## Hito 2.2 · W (Danza Desafiante) y E (Dueto Impecable): efectos y sonidos

- Herramientas y versiones utilizadas: las mismas del Hito 2.1 (Unity 6000.6.5f1, URP 17.6.0,
  Python 3 + Pillow, wwiser v20260808, vgmstream r2117). Sin paquetes nuevos de Unity.

- Cambios de cámara, encuadre y escala: ninguno. Los efectos usan la misma escala que el
  Hito 2.1 (`vfx_scale`, `official_vfx` en `ScenePresentation.json`).

- Modelos, pivotes, rig y prefabs modificados: ninguno.

- Animaciones modificadas/nuevas y su mapeo a acciones: no se agregaron clips. Se siguen los
  eventos del grafo oficial: `irelia_spell3` lanza `E_cas` y `irelia_spell3_b` lanza
  `E_cas_02` desde `Buffbone_Glb_Ground_Loc`. El clip oficial de carga de la W ya guarda las
  cuchillas flotantes, como en LoL.

- Partículas, shaders, materiales e iluminación:
  - `Tools/Import-IreliaVfx.py` ahora importa también los sistemas `W_*` y `E_*` (53 sistemas,
    200 emisores). El runtime suma lo que estos usan: haz entre dos puntos (`VfxPrimitiveBeam`,
    la línea entre las dos cuchillas de la E), rayos orientados (`VfxPrimitiveRay`, la columna de
    luz sobre la cuchilla), partículas hijas (`childParticleSetDefinition`: estelas y brillos que
    siguen a cada partícula), velocidad orbital y paleta de color (`paletteDefinition`: el corte
    dorado de la W). La velocidad orbital también aplica a las chispas de `Q_heal` y `Q_Proc`
    (antes salían rectas; ahora giran alrededor de Irelia como en LoL).
  - **W:** `W_DR` (escudo alrededor de Irelia mientras carga), `W_FullPower` (destello y anillo
    dorado al completar la carga, hasta soltar), `W_Swipe` / `W_Swipe_Empowered` (corte al
    soltar; el dorado con carga completa) en la dirección apuntada, `W_Hit` en cada objetivo
    alcanzado y `W_Block` cuando la W absorbe un golpe.
  - **E:** `E_cas` / `E_cas_02` (giro al lanzar), `E_Mis_01` (vuelo de cada cuchilla desde
    Irelia hasta su punto), `E_Blades` (cuchilla suspendida con su columna de luz) y
    `E_Team_Indicator` (anillo en el suelo; versión roja para una Irelia enemiga), `E_Beam`
    (franja translúcida entre los dos puntos, con bordes azules y fragmentos en punta que
    corren por ella; se desvanece en los extremos, debajo de las cuchillas) y
    `E_Beam_Warning_Sparks` (rayos sobre la franja), `E_Mis_02` (las cuchillas dejan su punto
    y vuelan una hacia la otra, tendidas a lo largo de la línea: se cruzan en el centro en el
    momento del aturdimiento y se apagan en el extremo opuesto, unos 0,14 s en total, medido en
    el juego), y al impactar `E_Hit` / `E_Hit_minion` en cada unidad y `E_hit_Avatar` (silueta)
    en el campeón aturdido. El haz termina solo después del impacto: su corte rojo y la línea
    oscura que queda en el camino salen como en LoL.
  - **Tiempo de la E:** entre la llegada de la segunda cuchilla y el impacto ahora pasan 0,35 s
    (antes 0,25 s), como en el juego (medido en las grabaciones: unos 0,33–0,37 s).
  - Ajustes generales tras comparar con LoL: los efectos del suelo (anillos, discos, franjas) se
    dibujan antes que el resto, como en LoL (antes el disco oscuro de `W_FullPower` tapaba el
    escudo); la paleta de color (corte dorado de la W) se lee sin mipmaps, para que el color no
    se mezcle con las filas vecinas al alejar la cámara; cada emisor continuo suelta su primera
    partícula al empezar (antes, una fracción de segundo después: el escudo de la W tardaba en
    aparecer).
  - **Escudo de la W:** sus esferas (`W_DR/Ripple`, varias capas a la vez, de distinto tamaño)
    usan el tamaño de Irelia (`FlexShape`: su altura), así que ahora la rodean como en LoL,
    con varias capas a la vez (una interior marcada y otra exterior más tenue); antes eran
    del tamaño de un objeto genérico y quedaban casi ocultas dentro del modelo. Ajustado a ojo
    contra el juego (en `Tools/Import-IreliaVfx.py`, `ARTIST_TWEAKS`): la burbuja conserva su
    relleno violeta y su borde la mayor parte de su vida, y las imágenes de cuchillas que giran
    alrededor (`top2`) quedan tenues, como en LoL.
  - **Indicador de la W** (`IreliaWIndicator.cs`, nuevo), como en el juego: círculo alrededor de
    Irelia (brillo verde azulado con borde blanco fino), las dos líneas punteadas del área de
    golpe, el cuerpo de la flecha (ancho parejo), la mira (siempre mirando a la cámara, como en
    LoL) y la punta en forma de hoja. Las texturas de indicadores de LoL no vienen en los
    archivos del personaje: las proporciones se midieron sobre una vista cenital de la
    grabación del juego (el círculo vuelve a ser redondo), la punta se calcó de esa misma
    grabación (`Tools/Trace-IreliaWArrowhead.py`) y el resto se dibuja con
    `Tools/Make-IreliaWIndicator.py`. La R mantiene su indicador.
  - **Tamaño del indicador de la W:** los campeones se dibujan 1,65 veces más grandes que las
    distancias del juego, así que un indicador exactamente del alcance de la W se ve corto al
    lado de Irelia (su punta queda a 3 alturas de Irelia; en LoL, a 3,9). Por eso se dibuja
    1,3 veces más grande (`"w_indicator_scale": 1.3` en `ScenePresentation.json`), con la
    proporción de LoL. Con `1` muestra exactamente el alcance y el ancho de golpe de la W; el
    área de golpe no cambia en ningún caso.
  - **Pasiva con 4 cargas** (al completar las cargas con Q, E u otros golpes), comparada con el
    juego: en LoL las cuchillas brillan naranja dorado, el cuerpo de Irelia toma un brillo
    dorado y salen rayos dorados en todas direcciones.
    - Cuchillas y cuerpo: el material de las cuchillas de LoL (`Irelia_Blades_Passive_inst`)
      lleva su resplandor (bloom) de azul a naranja dorado (1, 0,62, 0) con 4 cargas. El proyecto
      no tiene bloom, así que `IreliaPassiveGlow.cs` (nuevo, shader
      `IreliaPassiveGlow.shader`) suma ese dorado sobre las cuchillas (relleno y borde) y un borde
      dorado sobre el cuerpo; aparece y se va en 0,2 s. Se mantiene la textura de cuchillas de 4
      cargas que ya cambiaba en el Hito 2.1.
    - Rayos: los destellos de `P_buf` (`Sparks1` / `Sparks2`) salían todos en la misma dirección;
      su forma de emisión (`VfxShapeSphere`, `flags` 1) indica una dirección al azar para cada
      uno, y ahora salen en todas direcciones, como en el juego. El importador lee `flags`
      (`shapeFlags`).
    - Color: el destello del cuerpo (`Avatar_Burst`) y los rayos (`Sparks1` / `Sparks2`) son
      dorados como en el juego (en los datos son blanco y celeste/violeta, que LoL lleva a dorado
      con su resplandor); ajuste en `ARTIST_TWEAKS` del importador.
  - Con `"official_vfx": true` ya no se muestran los marcadores anteriores: el disco y las seis
    cuchillas que giraban durante la carga de la W, la barra del corte de la W, y las cuchillas
    de malla y el haz propio de la E. Con `false` vuelven tal como estaban.

- HUD/barras y otros cambios: sonidos nuevos con `Tools/Import-IreliaEAudio.py` (misma
  ganancia que los existentes): `EBladeLand_1` (evento `IreliaE_missile_stop`, cuando cada
  cuchilla llega a su punto; se conserva el impacto y se recorta el zumbido de 7 s que sigue)
  y `ESparkles_1..14` (`IreliaESecondary_sparkles`). Manifiesto:
  `Assets/SourceAudio/IreliaOfficial/ESources.json`. Los sonidos de W y E que ya existían no
  cambian.

- Recursos o paquetes nuevos y sus requisitos: texturas y mallas de W/E en
  `Assets/Resources/Vfx/Irelia/`, audios en `Assets/Resources/Audio/Irelia/`, texturas del
  indicador de la W en `Assets/Resources/Models/IreliaOfficial/WAim/`, shader
  `Assets/Resources/Shaders/IreliaWAim.shader` y script `IreliaWIndicator.cs`; brillo de la
  pasiva: `IreliaPassiveGlow.cs` y `Assets/Resources/Shaders/IreliaPassiveGlow.shader`. Sin
  paquetes nuevos.

- Archivos de autoría incluidos: `Tools/Make-IreliaWIndicator.py` (dibuja el indicador de la
  W) y `Tools/Trace-IreliaWArrowhead.py` (calca la punta de la flecha de un cuadro de la
  grabación del juego); el resto sale de los archivos originales con `Tools/Import-IreliaVfx.py` y
  `Tools/Import-IreliaEAudio.py`.

- Cambios que requieren fusionar código o afectan contratos con la simulación:
  - `BladeDuelist.cs`: eventos de solo lectura `WLanded(objetivo)` y `ELanded(objetivo,
    aturdido)`; el disco de carga solo se crea sin `official_vfx`; el indicador de la W es
    `IreliaWIndicator` (misma forma de uso que `IreliaDirectionIndicator`, que sigue en la R).
  - `BladeWave.cs`: avisa cada impacto de la W (no de la R); oculta su barra con `official_vfx`.
  - `BladeLink.cs`: expone su línea de tiempo (orígenes, lanzamientos, convergencia, impacto)
    para los efectos; crea las cuchillas de malla y su haz solo sin `official_vfx`; reproduce el
    sonido de llegada de cada cuchilla. Daño, aturdimiento y marca no cambian.
  - `BladeTuning.cs` / `BladeTuning.asset`: `eConvergeTime` 0,25 → 0,35 s (el impacto de la E
    llega 0,1 s más tarde, como en LoL).
  - `IreliaModelAnimation.cs`: las cuchillas de guardia de la W solo sin `official_vfx`.
  - `ScenePresentationConfig.cs` / `ScenePresentation.json`: ajuste nuevo `w_indicator_scale`
    (solo presentación).
  - `IreliaAudio.cs`: señales `EBladeLand` y `ESparkles` (al final del enum).
  - `RiotVfxSimulation.cs` / `RiotVfxData.cs`: `shapeFlags` (dirección al azar en esfera).
  - `IreliaVfx.cs`: con 4 cargas suma un material de brillo a los `SkinnedMeshRenderer` del
    modelo de Irelia (se quita al perder las cargas, al morir o al reiniciar).
  - `Tests/PlayMode/PracticeVisualHandoffTests.cs`: prueba nueva `OfficialWAndEEffectsPlay`.

- Capturas de comparación y resolución utilizada: pendientes, 1280×720 en Game.

- Limitaciones pendientes:
  - Texturas de otros campeones o skins que no vienen en los archivos: tres se reemplazan por
    la textura de Irelia más parecida para que el emisor se vea (comparado con la grabación del
    juego; `SUBSTITUTE_TEXTURES` en el importador): el destello dorado que viaja hasta el
    objetivo al soltar la W cargada (`W_Swipe_Empowered/Center`, brillo suave), las chispas de
    `W_Hit` y el círculo violeta que sube de la cuchilla de la E (`E_Blades/Dark_circle`). El
    polvo de `W_DR`, de `W_Swipe_Empowered` y del dash de la Q sigue sin mostrarse (Irelia no
    tiene una textura de humo; la de ruido mostraba parches cuadrados). La malla
    `W_Swipe_Empowered/mesh_dustup` es de otra skin y tampoco está.
  - Varios campos de los emisores de W/E no tienen nombre conocido (p. ej. el modo de los haces);
    se dejaron en su valor por defecto.
  - La orientación de las cuchillas que se cruzan (`E_Mis_02`), su recorrido completo y los
    ajustes del escudo de la W salen de comparar con las grabaciones del juego, no de un campo
    de los datos.
  - El zumbido largo de la cuchilla en espera no se reproduce en bucle (se recorta, ver arriba).
  - El evento de sonido `FUTURE_IreliaE_foley` de la animación no está en el banco base.

## Hito 2.1 · Ataque básico, pasiva y Q (efectos, animaciones y sonidos)

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
