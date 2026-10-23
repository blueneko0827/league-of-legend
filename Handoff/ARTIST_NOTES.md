# Notas para la devolución

## Hito 2.3 · R (Filo de la Vanguardia), sonidos y voces

- **Ajustes tras la segunda revisión del cliente** (comparados cuadro a cuadro con
  `videoRealGameplay_100` y `videoRealGameplay_0`):
  - Forma detrás de las cuchillas de la R en vuelo: los quads del misil (`R_Mis`: `Glow`,
    `Glow1`, `Center_light`, `shadow`) se dibujan un cuarto de vuelta respecto de los quads
    comunes, como ya pasaba con los quads de suelo (`MissileQuadTurn` en
    `RiotVfxSimulation.cs`). El chevrón violeta ahora queda detrás de las cuchillas con la
    punta hacia donde vuela la R («<» cuando la R vuela a la izquierda, según la revisión del
    cliente con su grabación), en la dirección de la R; antes se abría de costado y se veía
    siempre como «^». La textura se espeja a lo largo del vuelo (no se gira 180°) y `R_Mis/Glow`
    lleva un desplazamiento en V (`birthUvOffset` 0,6, ajuste del importador): el chevrón queda
    detrás de las cuchillas con un espacio corto, un tercio del original (0,5), según la revisión
    del cliente.
    `Center_light` pasa a ser la barra fina atravesada al vuelo que se ve en el juego.
  - Salto de la R: `irelia_spell4` mueve la raíz de Irelia unas 230 unidades hacia arriba y
    270 hacia adelante antes de caer (`spell4_to_idle`). El rig fijaba la raíz en X/Z
    (necesario en la Q y los ataques, que se desplazan por la simulación); durante la R ahora
    se respeta todo el movimiento de la raíz (`IreliaRigAnimation.TickR`), así que se ve el
    salto hacia adelante y la caída rápida.
  - W → R: una habilidad pedida durante el bloqueo de 0,25 s de la liberación de la W queda en
    cola y se lanza al terminar (antes, si faltaban más de 0,2 s, se perdía). Así la R
    inmediatamente después de la W sale con su salto, como en LoL (`BladeDuelist.Command`).
  - Brillo rojo oscuro y violeta alrededor de Irelia después de la R: en el juego, cuando la
    R golpea con la pasiva ya en 4 cargas, el efecto de cargas máximas (`P_buf`: aura roja
    oscura, resplandor violeta y estallido naranja) se vuelve a activar. Ahora cualquier golpe
    de habilidad que renueva las cargas al máximo lo reactiva (`PassiveRefreshes` en
    `BladeDuelist`, `ObservePassive` en `IreliaVfx`).
    Es el efecto de la pasiva, así que sigue la regla de LoL: aparece al llegar a 4 cargas o
    cuando la R (u otra habilidad) golpea con la pasiva ya llena. Una R que no golpea a nadie, o
    con menos cargas, no lo muestra (en la grabación del juego Irelia ya tenía las 4 cargas).
  - Golpes consecutivos con la Q sobre objetivos marcados: el estallido de la marca
    (`Q_Proc`) salía pequeño sobre la cabeza; está hecho igual que la marca, así que ahora
    estalla en el mismo hueso del pecho y con su mismo tamaño, sobre el cuerpo del objetivo,
    como en el juego.
  - Pruebas nuevas: `RRightAfterWIsQueuedAndHops` (la R pedida justo después de la W se lanza y
    la raíz sube y avanza) y `RHitAtFourStacksFlaresThePassiveAgain` (con 4 cargas, el golpe de
    la R vuelve a activar `P_buf`).

- Herramientas y versiones utilizadas: las mismas del Hito 2.2 (Unity 6000.6.5f1, URP 17.6.0,
  Python 3 + Pillow, wwiser v20260808, vgmstream r2117). Sin paquetes nuevos de Unity.

- Cambios de cámara, encuadre y escala: ninguno.

- Modelos, pivotes, rig y prefabs modificados: ninguno. Nuevo: el muro de la R de LoL es una
  unidad propia del juego (`IreliaBlades`: 26 cuchillas, cada una en su hueso).
  `Tools/Import-IreliaRBlades.py` (nuevo) la convierte a
  `Assets/Resources/Vfx/Irelia/IreliaRBlades.json` (mallas, huesos y clips) e
  `IreliaRBlades.cs` (nuevo) la arma al formarse el muro; no hay prefab.

- Animaciones modificadas/nuevas y su mapeo a acciones: no se agregaron clips de Irelia. Su
  `irelia_spell4` se sigue reproduciendo a lo largo de los 0,4 s del lanzamiento, que coincide
  con la grabación del juego (giro y salida de las cuchillas a los 0,4 s). Su evento `R_cas`
  (cuadro 0, `Buffbone_Glb_Ground_Loc`) ahora lanza el giro de la R. Clips de `IreliaBlades`, a
  30 cuadros por segundo: `cast` (33 cuadros: las cuchillas salen disparadas del impacto y caen
  sobre el muro), `cycle` (51, en bucle mientras dura el muro) y `death` (20, se hunden en el
  suelo al terminar). Eventos de su grafo: `R_Hit_Aoe` en el cuadro 0 y `R_Wall_Bladeglow` en
  cada cuchilla desde el cuadro 9. Como en la grabación del juego, esta unidad aparece dos
  cuadros (0,067 s) después de la explosión (`RWallUnitDelay` en `IreliaVfx.cs`).

- Partículas, shaders, materiales e iluminación:
  - `Tools/Import-IreliaVfx.py` ahora importa también los sistemas `R_*` (65 sistemas,
    279 emisores, 19 eventos de animación).
  - **R, como en LoL:** `R_cas` (giro alrededor de Irelia al lanzar), `R_Mis` (las cuchillas
    vuelan en abanico, de punta, y al salir atraviesan un anillo vertical, de frente al vuelo),
    y donde estalla sobre un campeón: `R_Mis_Tar_Break` (explosión con rayos sobre él) y el
    muro: las cuchillas de `IreliaBlades`, `R_groundlines` (líneas en el suelo y la valla
    translúcida, que siguen exactamente el contorno del muro de daño), `R_Hit_Aoe` (el triángulo
    grande de la marca en el suelo, apuntando hacia donde voló la R; los rayos eléctricos que
    recorren el muro desde el centro hacia afuera; el resplandor y el abanico de estelas) y
    `R_Wall_Bladeglow` (brillo en cada cuchilla). Cuando el muro golpea a un enemigo, este
    recibe `R_Wall_Hit` (estallido y luego un brillo dorado que pasa a violeta). Las versiones
    `_Enemy` se usan para una Irelia enemiga.
  - **Revisión con la grabación del juego** (cuadro a cuadro, `videoRealGameplay_100` y
    `videoRealGameplay_0`): se corrigió lo que difería.
    - El anillo de la salida del misil estaba acostado en el suelo alrededor de Irelia; en LoL
      está de pie, de frente al vuelo.
    - El triángulo de la marca en el suelo (`R_Hit_Aoe`) salía a la mitad del tamaño, detrás del
      objetivo y girado de costado. Ahora mide unas 900 unidades, centrado 100 unidades por
      delante del impacto y con una punta hacia donde voló la R, como en el juego (tamaño del
      objeto de referencia 220, `RHitAoeBoundSize`).
    - Los rayos eléctricos de `R_Hit_Aoe` (`pulse`) no se veían (en su lugar quedaba una lámina
      azul). Ahora son líneas finas que recorren el muro hacia afuera.
    - Los emisores de una sola tanda de la R lanzaban una partícula en lugar de toda la tanda:
      los rayos de la explosión, el abanico de estelas, y las puntas y esquirlas del golpe del
      muro.
    - `R_Wall_Disarm` (icono de espada naranja sobre el enemigo) ya no se usa: la R actual de
      LoL no desarma y la grabación no lo muestra.
  - Runtime (`RiotVfxSimulation.cs`, `RiotVfxLibrary.cs`, `RiotParticle.shader`):
    - LoL dibuja las partículas de un misil en un marco cuyo eje Y es la dirección de vuelo
      (`MissileFrame`). Con ese marco las cuchillas de `R_Mis` vuelan de punta y sus anillos de
      salida quedan de pie, como en la grabación.
    - `burst` (nuevo, lo marca el importador en los sistemas `R_*`): un emisor de una sola
      partícula lanza toda su tasa de una vez.
    - Erosión: si el mapa de erosión es la misma textura del emisor, se desplaza y se repite con
      ella; un `erosionSliceWidth` angosto (menos de 0,25) dibuja solo una franja fina sobre el
      umbral. Solo lo usa el `pulse` de `R_Hit_Aoe` (los rayos).
    - Un `texDiv` fraccionario en una malla erosionada por su propia textura la repite (solo el
      `pulse`: 0,5 = dos veces alrededor del muro).
    - Los demás efectos (ataques, pasiva, Q, W, E) no cambian: ninguno usa estos casos.
  - Material de las cuchillas del muro: `IreliaRWallBlade.shader` (nuevo) con la textura de las
    cuchillas de Irelia y el brillo de borde y resplandor de su material del juego
    (`Irelia_bladeglow_inst`).
  - Escala: el contorno del muro, sus líneas en el suelo y el pulso van a escala de juego, sobre
    el muro de daño; la altura y el tamaño de las cuchillas y de las partículas, a la escala de
    los campeones, como los demás efectos.
  - Con `"official_vfx": true` ya no se muestran las cuchillas propias de la R (proyectil y muro
    de `IreliaRVisuals`). Con `false` vuelven tal como estaban.

- Sonidos:
  - R: sus eventos oficiales ya estaban conectados (lanzamiento, salida del misil, impacto en un
    campeón, que ya incluye la capa de activación del muro `R2_OnBuffActivate`, impacto en
    súbditos y golpe del muro). Nuevo: el sonido de salida del misil se apaga en 1 s cuando el
    misil termina, como el evento oficial `Stop_sfx_Irelia_IreliaR_onmissilelaunch`; antes
    sonaba completo. Revisado contra los datos: la R no tiene otros sonidos (de sus partículas,
    solo `R_Wall_Hit` tiene uno, el golpe del muro).
  - **Voces (nuevo):** `Tools/Import-IreliaVoice.py` importa las voces oficiales en inglés de
    Irelia (172 líneas en 21 grupos) de `irelia_base_vo_events.bnk` y
    `irelia_base_vo_audio.wpk`. El banco decide qué líneas pertenecen a cada evento y a cada
    valor del interruptor de la pasiva (`Irelia_P_Stacks`: de 0 a 3 cargas, o 4). El nivel de
    cada línea sale del volumen y la ganancia del banco, relativo a las voces de la R (que no
    cambian). Manifiesto: `Assets/SourceAudio/IreliaOfficial/VoiceSources.json`.
  - Reglas (`IreliaAudio.cs`):
    - Una sola línea a la vez, como el mezclador de voces de LoL: mientras suena una, la nueva
      de igual prioridad se descarta. La de muerte tiene prioridad mayor y la reemplaza.
    - Habilidades, en cada lanzamiento (como en las grabaciones del juego):
      - Q: esfuerzos. Con 4 cargas, una secuencia: las 4 líneas del conjunto actual y luego
        esfuerzos al azar. Al perder las 4 cargas cambia el conjunto (los eventos
        `Spell3DPEnd_01/02/03`).
      - W al cargar y al soltar: el banco solo tiene líneas para 4 cargas. Al soltar se corta
        la línea de la carga, como en la grabación del juego.
      - E: en la primera y la segunda cuchilla.
      - R: las 8 líneas que ya estaban, con su 30 % de probabilidad (cuando no está sonando otra
        voz), que no cambia. Ahora no se repiten seguidas, como el contenedor del banco.
    - Órdenes del jugador y esfuerzos del ataque básico:
      - Primera orden de movimiento de la partida: `Move2DFirst` (se intenta una sola vez).
      - Al salir de la fuente con un clic a más de 900 unidades: `Move2DLong`, una vez por
        salida.
      - Movimiento (`Move2DStandard`), ataque a un campeón enemigo (`Attack2DGeneral`) y
        esfuerzos del ataque básico: después de 5 s sin voces, la siguiente orden o ataque tiene
        50 % de probabilidad; si no suena, se esperan otros 5 s (`ChatterQuietSeconds`,
        `ChatterChance`). Con clics seguidos sale más o menos una línea cada 15 s. LoL no
        documenta su frecuencia, y en las grabaciones del juego no suenan mientras Irelia sigue
        lanzando habilidades.
    - Compras: B. F. Sword, Trinity Force y Blade of the Ruined King tienen su línea. Titanic
      Hydra también, pero la práctica todavía no permite comprarla.
    - Muerte, reaparición y eliminación de un campeón: sus líneas. No ocurren en la práctica;
      quedan listas para los modos con rival.
    - Cada grupo suena mezclado, sin repetir sus últimas N líneas, según su contenedor del banco.

- Recursos o paquetes nuevos y sus requisitos:
  - Texturas de la R en `Assets/Resources/Vfx/Irelia/Textures/`.
  - `IreliaRBlades.json` y el shader `IreliaRWallBlade.shader`.
  - Voces en `Assets/Resources/Audio/IreliaVoice/`: WAV de 16 bits; Unity las comprime a
    Vorbis en memoria.
  - Sin paquetes nuevos.

- Archivos de autoría incluidos: ninguno nuevo. Todo sale de los archivos originales con
  `Tools/Import-IreliaVfx.py`, `Tools/Import-IreliaRBlades.py` y `Tools/Import-IreliaVoice.py`.

- Cambios que requieren fusionar código o afectan contratos con la simulación:
  - **R con lanzamiento rápido, sin flecha:** en el juego la R se lanza al presionarla, sin
    indicador de apuntado. `PlayerCommands.cs` ahora la lanza al presionar R hacia el cursor.
    `ScenePresentation.json` → `"r_aim_indicator": false` (por defecto). Con `true` vuelve el
    modo anterior: mantener R para apuntar con la flecha y soltar para lanzar.
    `IreliaRAim.Begin(punto, mostrar)` toma la dirección sin dibujar la flecha. El texto de
    ayuda de la R lo explica.
  - `BladeWave.cs`: `Launched` (solo lectura); con `official_vfx` también oculta el proyectil de
    la R; avisa a `IreliaAudio` cuando el misil de la R termina (cada misil apaga su propio
    sonido de salida).
  - `BladeWall.cs`: `Blade`, `Impact`, `Direction`, `ExpiresAt` y `OwnerTeam` (solo lectura);
    crea su muro propio solo sin `official_vfx`; avisa cada golpe del muro.
  - `BladeWall.cs` y `BladeTuning` (`rWallArmTime`, nuevo, 0,15 s): como en la grabación, las
    cuchillas caen sobre el muro un momento después de la explosión, y recién entonces golpea.
    Un enemigo parado sobre la línea o que la cruzó mientras caían recibe el golpe al caer y,
    como los golpeados por la explosión, la marca. Los 2,5 s del muro cuentan desde que caen,
    como en LoL (en la grabación se hunden unos 2,7 s después del impacto).
  - `BladeDuelist.cs`: evento de solo lectura `RWallHit(objetivo)`.
  - `RiotVfxSimulation.cs`: `MissileFrame`; `RiotVfxEmitterData.burst` (nuevo).
  - `IreliaAudio.cs`:
    - Voces nuevas: enum `IreliaVoiceCue` y evento `VoicePlayed`.
    - `RMissileEnded`, `MoveOrdered`, `AttackOrdered`, `ItemBought`, `ChampionKilled`, `Died`,
      `Respawned`. `RLaunched` ahora devuelve un identificador del sonido para `RMissileEnded`.
    - La voz de la R usa la misma fuente que las demás voces (`Irelia voice`).
    - Las fuentes de sonido se pausan con el juego también antes de reproducir algo (al
      reanudar, la primera jugada no pisa una voz pausada).
  - `Combatant.cs`: avisa a la voz al comprar, al eliminar a un campeón y al reaparecer. Al
    morir llama a `Died` en lugar de `StopAll`; también detiene los efectos de sonido.
  - `PlayerCommands.cs`: las órdenes de mover y atacar avisan a la voz en el clic. La orden no
    cambia.
  - `Tests/PlayMode/PracticeVisualHandoffTests.cs`: pruebas nuevas `OfficialREffectsPlay` y
    `OfficialVoiceLinesPlay`. `OfficialREffectsPlay` también comprueba que el muro llega después
    de la explosión y que no aparece `R_Wall_Disarm`.

- Capturas de comparación y resolución utilizada: pendientes, 1280×720 en Game.

- Limitaciones pendientes:
  - La comparación con la grabación se hizo con la vista previa del simulador de efectos (fuera
    de Unity, con la misma cámara y la misma escala que la grabación). Conviene revisarla en el
    editor.
  - LoL suma un resplandor (bloom) que el proyecto no tiene; los rayos y el estallido se ven un
    poco menos brillantes que en el juego.
  - El área de la explosión sigue siendo un círculo de 320 unidades alrededor del objetivo.
  - Con el marco del misil también quedan de pie los conos y el remolino de polvo de la salida
    (`ConeBurst`, `ConeBurst1`, `mesh_dustup`); en la vista previa se parecen al juego, pero se
    dibujan en la capa del suelo (debajo de las cuchillas): conviene revisarlo en el editor.
  - Al formarse el muro se revisa el recorrido de cada enemigo desde la explosión: quien estaba
    sobre la línea o la cruzó en esos 0,15 s recibe el golpe y la marca.
  - Voz de la R: en las grabaciones del juego Irelia dice una línea en cada R (3 de 3). Acá
    sigue en 30 %, como estaba definido (`RVoiceChance`).
  - La frecuencia de las voces de órdenes y de los esfuerzos del ataque básico no está en los
    datos; se ajusta con `ChatterQuietSeconds` y `ChatterChance`.
  - El interruptor de las líneas de la Q con 4 cargas no tiene nombre en el banco, y los datos
    no dicen cuál de los tres conjuntos elige LoL al perder las cargas: se elige al azar.
  - Sin conectar: primer encuentro con cada campeón, burlas, bromas, risas, recall, wards y
    pings (la práctica no tiene emotes, recall ni wards).
  - Las voces son las de los archivos entregados, solo en inglés (`en_US`), igual que las de la R.

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
  - **Impacto de la W** (`W_Hit`), comparado con la grabación del juego: allí es un estallido
    blanco intenso con halo azul, más o menos del ancho del objetivo, que brilla sobre todo en
    la primera décima de segundo, con chispas que salen disparadas. Leído tal cual, su estrella
    empezaba invisible y solo se ponía azul, y las chispas eran puntos. Ajustes en
    `ARTIST_TWEAKS`: estrella blanca al inicio que pasa a azul y 1,6 veces más grande; halos
    1,5 veces; chispas como trazos (`irelia_base_z_glow_bright` en lugar de la textura de Zoe
    que no viene) y 2,2 veces más grandes.
  - **Corte de la W al soltar:** la punta de flecha de estelas que vuela hacia el objetivo
    (`W_Swipe` / `W_Swipe_Empowered`: `Glow` y `Glow1`, tendidos en el suelo) apuntaba a un
    costado, a 90° del corte. LoL dibuja la textura de los efectos del suelo girada un cuarto de
    vuelta respecto de los demás (su parte de arriba va a lo largo del primer tamaño,
    `birthScale` X); comparado con la grabación del juego, ahora la punta apunta al objetivo y
    se estira a lo largo de su vuelo, como en LoL. Los demás efectos del suelo (anillo de la E,
    ondas de la Q y de la E) son redondos y se ven igual.
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
      lleva su resplandor (bloom) de azul a naranja dorado con 4 cargas. El proyecto no tiene
      bloom, así que `IreliaPassiveGlow.cs` (nuevo, shader `IreliaPassiveGlow.shader`) suma un
      brillo sobre el modelo, ajustado a la grabación del juego: las cuchillas se ven crema
      dorado pálido (sin contorno naranja) y el cuerpo conserva sus colores (rojo, blanco,
      violeta) con apenas un borde cálido; aparece y se va en 0,2 s. Se mantiene la textura de
      cuchillas de 4 cargas del Hito 2.1. El resplandor alrededor del pecho (`P_buf/glow1`) es
      naranja como en el juego (en los datos, amarillo).
    - Rayos: los destellos de `P_buf` (`Sparks1` / `Sparks2`) salían todos en la misma dirección;
      su forma de emisión (`VfxShapeSphere`, `flags` 1) indica una dirección al azar para cada
      uno, y ahora salen en todas direcciones, como en el juego. El importador lee `flags`
      (`shapeFlags`). Además se dibujaban como rectángulos tendidos en el suelo (su rotación de
      nacimiento los acuesta), y juntos formaban una mancha clara rectangular desde los pies de
      Irelia; ahora se dibujan como trazos a lo largo de su vuelo, mirando a la cámara, y salen
      desde el pecho como los rayos de una rueda, como en el juego. Solo afecta a los emisores
      de dirección al azar (los cortes de los golpes básicos no cambian).
    - Luz alrededor del cuerpo, como la de una vela: el resplandor `P_buf/glow1` es más amplio,
      naranja dorado, suave y tenue, y late despacio (dos partículas por segundo que aparecen y
      se apagan); ya no se ve como un disco ("moneda"). El brillo del modelo (cuchillas y
      borde del cuerpo) también es más suave. El destello del cuerpo al activarse
      (`Avatar_Burst`) es más tenue y los rayos continuos (`Sparks2`) son menos y más suaves.
    - Corona dorada al completar las cargas: `P_buf` ahora usa el tamaño de Irelia
      (`FlexShape`, como el escudo de la W). Su anillo dorado (`RippleActivate`), que se
      deshace en arcos como una corona de laurel alrededor de ella, antes era del tamaño de un
      objeto genérico y quedaba oculto dentro del modelo; en la grabación aparece justo cuando
      el golpe completa las 4 cargas.
    - Rayos de luz (agregados para igualar la grabación; LoL usa parte de esto con su bloom,
      que el proyecto no tiene): emisor nuevo `P_buf/Beams`, creado por el importador con la
      textura `irelia_p_beam_gold` (dibujada por él). Mientras mantiene las 4 cargas, rayos
      suaves dorados, como luz de vela, salen desde el centro del cuerpo en un círculo
      irregular: cada rayo es una partícula (unos nueve por segundo, de medio a un segundo de
      vida) con dirección, largo, brillo y giro al azar, más intensa en el centro y apagándose
      hacia la punta, y siguen a Irelia al moverse.
    - La corona de laurel dorada que se ve bajo Irelia en la grabación no es de Irelia: aparece
      cuando la runa Conquistador llega a 12 cargas (en el HUD de la grabación su contador pasa
      de 10 a 12 y aparece su ícono dorado en ese mismo momento). No se agrega, porque el
      proyecto no tiene runas en los efectos; la corona que se había agregado antes se quitó.
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
  - `RiotVfxSimulation.cs` / `RiotVfxData.cs`: `shapeFlags` (dirección al azar en esfera) y
    orientación de la textura de los efectos del suelo (punta de la W).
  - `IreliaVfx.cs`: con 4 cargas suma un material de brillo a los `SkinnedMeshRenderer` del
    modelo de Irelia (se quita al perder las cargas, al morir o al reiniciar).
  - `Tests/PlayMode/PracticeVisualHandoffTests.cs`: prueba nueva `OfficialWAndEEffectsPlay`.

- Capturas de comparación y resolución utilizada: pendientes, 1280×720 en Game.

- Limitaciones pendientes:
  - Texturas de otros campeones o skins que no vienen en los archivos: dos se reemplazan por
    la textura de Irelia más parecida para que el emisor se vea (comparado con la grabación del
    juego; `SUBSTITUTE_TEXTURES` en el importador): las chispas de `W_Hit` y el círculo violeta
    que sube de la cuchilla de la E (`E_Blades/Dark_circle`). El destello de
    `W_Swipe_Empowered/Center` sigue sin mostrarse: con la textura de reemplazo se veía, durante
    un cuadro al soltar la W, como un destello en punta hacia adelante que no está en el juego.
    El polvo de `W_DR`, de `W_Swipe_Empowered` y del dash de la Q tampoco se muestra (Irelia no
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
