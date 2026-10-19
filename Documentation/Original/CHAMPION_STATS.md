# Estadísticas de campeones

## Perfil actual de Irelia: bases oficiales sin runas (18/10/2026)

Las cifras de esta sección son bases intrínsecas. Desde la integración de runas del 18/10/2026, fragmentos y efectos de runas se suman como una capa separada; ver `RUNES_IMPLEMENTATION.md`. No se vuelven a incorporar los bonos de la antigua captura como base.

Todos los modos usan `ChampionStatReference.AtLevel`, que ahora resuelve también
Irelia mediante `LeagueChampionStats.For`. La referencia es LoL PC Data Dragon
16.20.1 y el registro del cliente 16.20; el crecimiento de AD de 3,5 procede del
cliente porque Data Dragon publica 0 en ese campo. Se aplican los valores completos
con su precisión interna, sin redondear cada nivel antes de calcular el combate.

| Nivel | Vida base | AD base | Ataques/s sin pasiva |
|---|---:|---:|---:|
| 1 | 630 | 65 | 0,656 |
| 9 | 1405,1 | 88,59 | 0,766536 |
| 18 | 2585 | 124,5 | 0,9348 |

Sin objetos/efectos: movimiento 335, alcance 200, AP 0, AH 0, crítico 0, robo de vida 0,
omnivamp 0 y tenacidad 0. Vida/maná, armadura/RM y regeneraciones crecen con la curva
normal descrita más abajo. Los objetos y la pasiva se aplican después con las mismas
reglas en Normalplay, PvP y Práctica. No se modifican habilidades, sus rangos,
enfriamientos, la economía ni los atributos de los otros campeones.

Normalplay y PvP siguen equipando Doran's Blade: con ese objeto Irelia comienza
con 710 HP y 75 AD. Práctica sigue comenzando en nivel 6, sin starter, con 1084,25 HP,
78,825 AD y 0,72078 ataques/s. BORK en nivel 18 suma 40 AD y 25% AS: 164,5 AD y 1,0988 ataques/s,
antes de otras bonificaciones.

`Assets/Resources/LeagueData/IreliaLiveStats.json` y `IreliaLiveStats.cs` se conservan
como archivo histórico de las 18 capturas del 17/10/2026. Incluyen la página de runas
observada (+65HP,+5,4AD,+13%AS y5% de tenacidad); **ya no alimentan el gameplay**.
No se reescribe esa evidencia para presentarla como una captura sin runas.

La Q conserva su fórmula y el bono a minions `50 + 11 * (nivel - 1)`. Los siguientes
valores intrínsecos son ahora también el perfil activo antes de aplicar el inventario.

Referencia: **LoL PC, Data Dragon 16.20.1**, contrastada con los registros del cliente **16.20**, el 14 de octubre de 2026. Sin objetos, runas ni bonificaciones temporales. La escala común es **100 unidades de LoL = 1 metro de Unity**.

## Nivel 1

| Estadística | Irelia | Sett | Vladimir | Mel |
|---|---:|---:|---:|---:|
| Velocidad de movimiento | 335 | 340 | 330 | 330 |
| Velocidad en Unity (m/s) | 3,35 | 3,40 | 3,30 | 3,30 |
| Vida | 630 | 670 | 600 | 630 |
| Daño de ataque | 65 | 60 | 55 | 54 |
| Armadura | 36 | 33 | 24 | 21 |
| Resistencia mágica | 30 | 28 | 30 | 30 |
| Alcance de ataque | 200 | 125 | 450 | 550 |
| Ataques por segundo | 0,656 | 0,625 | 0,658 | 0,625 |
| Maná | 350 | Sin maná | Sin maná | 480 |
| Regeneración de vida / 5 s | 3,5 | 7 | 7 | 6 |
| Regeneración de maná / 5 s | 8 | 0 | 0 | 9 |

Sett tiene cinco unidades más de velocidad base que Irelia. Vladimir y Mel tienen cinco menos. El valor genérico anterior de los rivales era 550; se eliminó.

Fuentes oficiales: [Irelia](https://ddragon.leagueoflegends.com/cdn/16.20.1/data/en_US/champion/Irelia.json), [Sett](https://ddragon.leagueoflegends.com/cdn/16.20.1/data/en_US/champion/Sett.json), [Vladimir](https://ddragon.leagueoflegends.com/cdn/16.20.1/data/en_US/champion/Vladimir.json) y [Mel](https://ddragon.leagueoflegends.com/cdn/16.20.1/data/en_US/champion/Mel.json).

## Crecimiento por nivel

| Incremento | Irelia | Sett | Vladimir | Mel |
|---|---:|---:|---:|---:|
| Vida | 115 | 114 | 110 | 99 |
| Daño de ataque | 3,5 | 4 | 3 | 3,3 |
| Armadura | 4,7 | 4,7 | 4,5 | 5,2 |
| Resistencia mágica | 2,05 | 2,05 | 1,3 | 1,3 |
| Velocidad de ataque | 2,5% | 1,75% | 2% | 2,5% |
| Maná | 50 | 0 | 0 | 28 |
| Regeneración de vida / 5 s | 0,85 | 0,5 | 0,6 | 0,55 |
| Regeneración de maná / 5 s | 0,8 | 0 | 0 | 0,9 |

Se usa la curva de crecimiento de LoL: `n × (0,7025 + 0,0175 × n)`, con `n = nivel - 1`. La velocidad de movimiento base no sube con el nivel. La regeneración se convierte a valores por segundo para la simulación.

Data Dragon publica cero en crecimiento de daño de ataque para estos cuatro campeones en este parche. Se tomó ese campo de los registros del cliente: [Irelia](https://raw.communitydragon.org/16.20/game/data/characters/irelia/irelia.bin.json), [Sett](https://raw.communitydragon.org/16.20/game/data/characters/sett/sett.bin.json), [Vladimir](https://raw.communitydragon.org/16.20/game/data/characters/vladimir/vladimir.bin.json) y [Mel](https://raw.communitydragon.org/16.20/game/data/characters/mel/mel.bin.json). También se contrastaron proporciones y preparación de ataques básicos.

## Aplicación al juego

`Assets/Scripts/Core/LeagueChampionStats.cs` es la fuente única de estadísticas base y crecimiento para jugador e IA. Se eliminaron los valores genéricos de `SandboxSettings` y las copias duplicadas de estadísticas en `BladeTuning`.

Normalplay y PvP empiezan en1; Práctica empieza en6. Irelia aprende automáticamente la ruta solicitada en todos los modos; PvP y Normalplay conservan sus propias reglas de progresión. La referencia de atributos es idéntica entre modos y la dificultad no multiplica estadísticas.

Sett y Vladimir no reciben una barra artificial de maná. Sett almacena temple para W y dispone de su pasiva de puños y regeneración; Vladimir conserva un kit simplificado.

## Alcance de esta corrección

Se ajustaron las estadísticas **base** y su crecimiento. Las pasivas de puños y regeneración de Sett se completaron en la auditoría del 15 de octubre. Los demás kits, ciertos efectos de objetos y las runas siguen requiriendo revisiones de fidelidad. La curación de la zona de práctica tras las torres, los súbditos, los modelos y las colisiones del escenario siguen siendo simplificaciones. Esto no convierte los kits provisionales de Sett, Vladimir y Mel en réplicas completas.


## Auditoría de Sett — 15 de octubre de 2026

La tabla completa aportada por el usuario coincide con Irelia y Sett en base/crecimiento. Se añaden el daño y velocidad del puño derecho, secuencia garantizada de Q y regeneración por vida perdida que faltaban en el kit. La IA estima sus ataques con esta secuencia y su preparación real. Ver [SETT_DAMAGE_AUDIT.md](SETT_DAMAGE_AUDIT.md) para números, fuentes y límites; esta revisión no convierte todos los kits en réplicas completas.

## Auditoría de Mel — 16 de octubre de 2026

La tabla aportada coincide con Mel en todos sus valores base y crecimiento. Se reemplaza
su kit provisional por pasiva acumulable, ejecución Overwhelm, Q por explosiones, W con
escudo/reflejo, E con núcleo y aura separados y R global sobre marcados. Se conservan
las reglas del modo personalizado. Fórmulas, discrepancias entre versiones y límites
de la reconstrucción: [MEL_ABILITY_AUDIT.md](MEL_ABILITY_AUDIT.md).

Validacion final del perfil API: 80 pruebas PlayMode distintas aprobadas, incluidas 18 comparaciones directas de atributos por nivel, compra/venta de BORK, PvP/Practica y regresiones de objetos/progresion/combate; 161 reglas de combate aprobadas. Resultados en TestResults/irelia-live-stats-validation.json e irelia-live-stats-regressions.xml. Reiniciar Play para recrear Irelia con el perfil nuevo.


17/10/2026 — Referencia de atributos compartida entre modos: `ChampionStatReference.AtLevel(campeon,nivel)` es ahora la unica entrada del calculo de atributos de Combatant. No acepta modo, dificultad ni equipo. Irelia usa las18 capturas de la API, incluidos RM, AH, robo de vida y tenacidad; Sett/Mel/Vladimir usan cada uno su propio perfil intrinseco. Objetos y efectos temporales se aplican despues con las mismas reglas. Se conservan los niveles iniciales y compras automaticas existentes: comparar siempre mismo nivel, inventario y estado de pasivas. Por ejemplo, Doran's Blade aporta10AD/80HP en cualquier modo; su presencia inicial en PvP no es un bono oculto de ese modo. Las bases intrinsecas siguen separadas de los valores efectivos para Spellblade y escalados de objetos. SharedChampionStatsTests contrasta 22 atributos por campeon en niveles1-18 entre Normalplay/PvP, sin y con el mismo inventario, y los tres modos con Irelia desde6 hasta18 (Práctica empieza en6). No se modifican economia, aprendizaje, oleadas, IA ni modos.

Validacion de atributos compartidos: 64 pruebas PlayMode distintas aprobadas y 161 reglas de combate. Incluye igualdad por nivel de los cuatro campeones con/sin objetos, perfil API de Irelia, transiciones y progresion. Resultados: TestResults/shared-champion-stats-validation.json y shared-champion-stats-final.xml. Reiniciar Play para recrear actores con la referencia compartida.


18/10/2026 — Normalplay: vida de minions sustituida por la tabla refinada del usuario en niveles 1–18. Melee: 477, 485, 492, 501, 510, 518, 527, 536, 600, 670, 750, 810, 850, 890, 935, 970, 1020, 1080. Caster: 296, 301, 307, 312, 318, 323, 329, 335, 340, 345, 355, 373, 385, 398, 410, 425, 440, 450. Cannon: 913, 912, 962, 1012, 1062, 1112, 1162, 1212, 1350, 1480, 1650, 1790, 1880, 1960, 2050, 2150, 2300, 2500. Nivel 1 cannon conserva 913 HP al no especificarse valor. Tabla personalizada ligada al nivel, no al minuto. Se conserva AD, XP, oro, radios y frecuencia de oleadas; PvP y practica sin cambios. Los minions existentes conservan su porcentaje de vida al escalar.


18/10/2026 — Normalplay entrega la mitad del oro de la proxima subida al alcanzar 50% de EXP y el resto al subir. Un registro acumulado independiente del saldo/inventario evita pagos duplicados por Level Up, sincronizacion o saltos de varios niveles; ambos campeones reciben lo mismo, incluso con rival oculto o muerto. Stop Level conserva su congelacion. El tooltip del boton muestra solo el oro pendiente. PvP conserva su economia. Irelia aprende automaticamente la ruta QEWQQRQWQWRWWEEREE en todos los modos, sin puntos manuales; Normalplay mantiene EXP y Practica sigue empezando en6 con Q3/W1/E1/R1.


18/10/2026 — Minions priorizan al campeon enemigo que dana a su campeon aliado mientras el atacante este a alcance de ataque; al salir recuperan su seleccion normal. Navegacion local con separacion suave, barrido de circulos y desvio lateral; caminar no desplaza minions. Se conservan radios y desplazamientos por habilidades. Tienda: lista Irelia editable con X al seleccionar dentro de Irelia y check para anadir desde All Items; PlayerPrefs guarda IDs entre modos/reinicios, admite lista vacia. Solo afecta catalogo, no inventario. Normalplay compra los mismos starters de PvP: Blade para Irelia/Sett (150g restantes), Ring para Mel (200g); ingreso inicial sigue siendo600, oro por mitadEXP sin cambios. Practica no compra starter.

Validación de la actualización del 18/10/2026: 110 pruebas PlayMode únicas aprobadas y 161 comprobaciones de reglas. Incluye los 18 niveles, todos los modos, compra/venta de objetos, Q contra magos y regresiones de stun y de Mel.
