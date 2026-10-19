# Mel: comportamiento y contraste del documento

Revisión del 18/10/2026. Los cambios de esta tarea afectan exclusivamente a Mel.
La revisión de IA no modifica las estadísticas de Irelia/Sett ni la economía compartida.
La corrección posterior de proyectiles y W/E se describe en MEL_ABILITY_AUDIT.md.

## Comportamiento implementado

- La planificación de oleada/recuperación se evalúa cada 0,4 s. La ejecución de un
  combo continúa cada 0,08 s para no introducir pausas de 0,4 s entre sus acciones.
- Prioriza el último golpe con un básico (también contempla los proyectiles de su
  pasiva) sobre un combo. Una R letal tiene prioridad sobre ese último golpe.
- No vuelve a la fuente por estar al 30% contra Irelia al 50%. Inicia recuperación
  solamente con vida <25%, rival vivo >60% y Q del rival observada como disponible.
  Una vez que inicia el regreso, recupera vida/maná antes de volver. En PvP nunca
  inicia ese regreso; el kiteo hacia su torre sigue permitido.
- Dificultades Fácil / Normal / Experto: reacción de W 350/300/250 ms, probabilidad
  de continuar con Q tras fallar E 100/40/10%, Flash ofensivo 0/20/70%.
  En código esas dificultades se llaman `Normal`, `Hard`, `Expert` respectivamente.
- E → básico → Q si el núcleo de E conecta; si falla, la Q por inercia conserva el
  punto original y la decisión se sortea una sola vez por E.
- Cerca de Irelia y con más de 60% de vida: Q → E → W → básico → R, condicionado
  a habilidades aprendidas, maná, enfriamientos, alcance y elegibilidad de R.
  Experto reserva W para amenazas E/R y no la gasta solamente para reunir 9 misiles.
- W tiene su propio reloj de reacción a señales visibles: segundo filo de E,
  proyectil R, y también W cargada/pánico en las dificultades inferiores.
  Experto puede intentar esquivar E tras su retraso humano; no consulta el ratón
  ni órdenes futuras de Irelia. Una E ya lanzada puede intentar interceptar una Q
  que esté ocurriendo, sin garantizar un acierto contra un dash tan rápido.
- Kiteo recto / diagonal / circular según dificultad. Si no hay trade, acompaña
  la oleada sin autoatacar minions sanos; considera minions propios ejecutables
  como posibles puntos de salto de Irelia. Desde más de 600 unidades, las ventanas
  de pesca de E son un sorteo del 30% cada 0,4 s mientras Irelia se expone.
- R requiere al menos dos marcas, o una marca + rival <40% + raíz/stun. El cálculo
  del remate incluye resistencias, escudos, regeneración y Overwhelm.
- Flash ofensivo se sortea una vez por oportunidad de remate con el básico/pasiva,
  incluyendo el seguimiento de una E propia que ya esté en vuelo (E → Flash),
  solamente si el rival se aleja y no se lo alcanza con un hechizo disponible.
  No se gasta para dar alcance a R: la definitiva es global. E → Flash puede fallar
  si Irelia esquiva el proyectil; no modifica el daño ni la colisión de la E.
- Conserva compras y build actuales. Respeta las torres y no deja una orden de
  persecución que la arrastre bajo torre entre decisiones.

La prioridad de oro describe las decisiones de farmeo; no introduce oro adicional
por minion ni altera la progresión personalizada de Normalplay/PvP.

## Fuentes contrastadas

Se volvió a descargar el JSON de **Data Dragon 16.20.1** desde Riot y el registro de
**cliente 16.20**. Sus objetos JSON coinciden con las referencias previamente fijadas
del proyecto. El registro del cliente se distribuye a través del espejo CommunityDragon;
es un archivo del cliente, no una respuesta de la API de estadísticas de una partida.
Data Dragon no contiene todas las fórmulas ni el código del servidor.

- [Riot Data Dragon: Mel 16.20.1](https://ddragon.leagueoflegends.com/cdn/16.20.1/data/en_US/champion/Mel.json)
- [Registro del cliente 16.20](https://raw.communitydragon.org/16.20/game/data/characters/mel/mel.bin.json)
- [Riot 26.3: cambios de Q, escudo W, E y R](https://www.leagueoflegends.com/en-us/news/game-updates/patch-26-3-notes/)
- [Riot 26.5: impactos Q y daño inicial R](https://www.leagueoflegends.com/en-us/news/game-updates/league-of-legends-patch-26-5-notes/)
- [Riot 26.8: daño Q y enfriamiento/duración W](https://www.leagueoflegends.com/en-us/news/game-updates/league-of-legends-patch-26-8-notes/)

Las notas de cambios no constituyen una ficha completa: para lo que no detallan,
la columna de referencia procede de los datos del cliente fijado, no de una prueba
de impacto dentro de una partida real. Las distancias/velocidades se expresan en
unidades de LoL; Unity usa la conversión 100:1.

## Diferencias numéricas

| Dato | Documento nuevo | Simulador actual | Referencia contrastada |
|---|---|---|---|
| Ataque básico, alcance / movimiento | 550 / 330 | 550 / 330 | Coinciden con Data Dragon |
| Proyectil básico, velocidad | 2000 | **1600** (corregido desde 3500) | **1600** en movimiento y `missileSpeed` del ataque básico del cliente |
| Misiles adicionales de pasiva, velocidad | No separa del AA | 5000 | Movimiento 5000; campo heredado `missileSpeed` 3000. Son campos distintos |
| Pasiva: cargas, duración, daño por misil | 3 por habilidad, máx. 9; 5 s; 8–30 +4% AP | Igual | Coincide con referencia fijada |
| Q alcance / distribución | 950 / radio 250 | Igual | Coincide; explosión individual radio 200 |
| Q velocidad | **1500** | **3800** | **3800**, Riot 26.3 |
| Q primer impacto | **65/95/125/155/185 +45% AP** | **60/85/110/135/160 +55% AP** | Simulador coincide con Riot 26.8 |
| Q siguientes impactos | Sin cifras | 5/7/9/11/13 +5% AP | Coincide con Riot 26.5 y cliente |
| Q lanzamiento / distribución temporal | No aclara lanzamiento; ráfaga 0,5 s | 0,35 s + 0,5 s de distribución, además del viaje | Coincide con Riot 26.3 |
| Q CD | 10/9/8/7/6 s | Igual | Coincide con Data Dragon |
| W protección | **Inmunidad 0,75 s** | **Escudo 80/110/140/170/200 +70% AP, sin inmunidad** | Escudo desde Riot 26.3; ratio actual en cliente |
| W velocidad adicional | **30%** | **40% decreciente** | 40% en Riot 26.3; duración 0,75 s desde Riot 26.8 |
| W CD | **35/32/29/26/23 s** | **38/35/33/29/26 s** | Data Dragon y cliente coinciden con simulador; notas de Riot 26.8 dicen **32**, no 33, en rango 3 |
| W reflejo | 40/45/50/55/60% +5% por 100 AP | Igual; físico ×0,7 antes de convertir a mágico | Coincide con cliente y cambio de Riot 26.3 |
| E alcance | **1050** | **1000** | Simulador coincide con Riot 26.3 / Data Dragon |
| E velocidad | **1200** | **1100** | Simulador coincide con Riot 26.3 |
| E raíz | **1,25/1,5/1,75/2/2,25 s** | **1,5 s en todos los rangos**, antes de tenacidad | Simulador coincide con Riot 26.3 |
| E daño central | 60/105/150/195/240 **+60% AP** | Mismos bases, **+70% AP** | Simulador coincide con cálculo del cliente |
| E daño por tick | **2/4/6/8/10** +1% AP | **2/3,5/5/6,5/8** +1% AP cada 0,125 s | Bases equivalentes a 16/28/40/52/64 por segundo en Riot 26.3; ratio del cliente |
| E slow / daño a minions | 30% / 50% | Igual | Coincide con cliente |
| E CD | **12/11,5/11/10,5/10 s** | **11/10,5/10/9,5/9 s** | Simulador coincide con Riot 26.3 / Data Dragon |
| R rango y marcas | Global; necesita campeón marcado; no consume salvo ejecución | Igual | Coincide con cliente |
| R lanzamiento | **1 s** | **0,75 s** | 0,75 s en `mCastTime` del cliente |
| R daño inicial | **150/250/350 +55% AP** | **125/200/275 +30% AP** | Bases de Riot 26.5; ratio del cliente |
| R por marca | **+1% AP**, crecimiento “exponencial” | **4/7/10 +4% AP por marca**, crecimiento lineal | Coincide con Riot 26.3 y cálculo del cliente |
| R CD | 120/100/80 s | Igual | Coincide con Data Dragon |

Costes de maná conservados y verificados en Data Dragon: Q 70/75/80/85/90,
W 80/60/40/20/0, E 50/60/70/80/90, R 100.

No se han sustituido cifras por las del documento. En particular, el alcance 250
que Data Dragon publica para W es un campo de lanzamiento de una habilidad propia,
no prueba de un radio de reflexión 250: se conserva el radio funcional 175 del kit.
La corrección posterior de W bloquea para Mel el daño, stun y marca de la convergencia
de E, sin devolver un proyectil ni afectar a los demás objetivos. No añade un motor
universal de reflexión ni inmunidad a otros efectos de suelo.

## Validación

**80 pruebas PlayMode aprobadas y 161 reglas de combate aprobadas.**

Las pruebas se ejecutan en `TestResults/PlayerValidationProject`, sin detener la
sesión principal de Unity. Resultados detallados en `TestResults/mel-ai-verified.xml`.
Las pruebas del kit eliminan el starter de **Mel** en su fixture para comprobar
estadísticas desnudas: el Doran's Ring automático de Normalplay aporta 90 HP y 18 AP.
Eso no es una discrepancia en el crecimiento del campeón.

La primera ejecución incluyó también toda la suite histórica `LaneStrategyTests`.
Ocho casos de Sett/progresión fallaron con expectativas anteriores a los cambios ya
existentes (starter automático, oro por nivel y aprendizaje automático); no se
modificaron esas reglas de Sett/Irelia como parte de esta tarea.

Se repitió `LaneStrategyTests` en la copia aislada desactivando únicamente la nueva
ruta de IA de Mel y su reacción propia: **los mismos ocho casos fallaron con los
mismos mensajes** (16/24 aprobados). No son regresiones de esta tarea. Se restauró
la versión actual del script en la copia al finalizar. Evidencia en
`TestResults/mel-ai-lane-baseline.xml` y `TestResults/mel-ai-validation.json`.

## Corrección de prioridades de línea — 18/10/2026

El posicionamiento anterior usaba a Irelia como centro incluso con oleada viva;
la prioridad de last hit inmediato no impedía una persecución larga. Normalplay
con oleadas ahora utiliza `ChampionMelLane.cs`; PvP conserva su política de duelo.

- La posición de farmeo se deriva del grupo cercano de minions enemigos y de los
  magos propios. Sin minions enemigos acompaña la oleada propia o espera cerca de
  mid, sin elegir la posición de Irelia como objetivo neutral.
- Las predicciones incluyen proyectiles en vuelo, windups y siguientes ataques
  de minions que ya están atacando. También contempla los propios básicos/pasiva
  de Mel y la primera explosión de una Q de farm ya comprometida. No inventa
  futuras decisiones de campeones ni altera la vida real de los minions.
- Prepara el próximo last hit antes de que esté ejecutable, camina a alcance y
  espera la ventana del básico; el tiempo de viaje usa1600 unidades/s.
- Normal/Difícil/Experto anticipan1,1/1,5/2s, con reacción de farm180/120/80ms
  y márgenes conservadores de daño87/94/100%. La finalidad de farmear es común.
- Los intercambios duran hasta1,75s, con350 unidades de separación ofensiva
  respecto a la posición de farm y una vuelta de al menos1,2s antes del siguiente.
  Termina el windup ya iniciado; un last hit próximo interrumpe el resto del combo.
  Posición de la oleada y origen del intercambio limitan el desplazamiento.
- Una eliminación probable puede justificar una acción ofensiva; el cálculo
  contempla vida/escudos/regeneración, daño disponible y peligro de respuesta.
  R global letal conserva prioridad sobre el farm y no necesita perseguir.
- Cede last hits que la exponen a una ráfaga peligrosa. Q puede asegurar un cañón
  que se perdería antes de llegar el básico, varios minions bajos o farm inseguro
  para el básico desde alcance de Q. Conserva maná para E/W; no usa E/W para limpiar.
- No cambia estadísticas, economía, experiencia, build ni compras. El objetivo
  de farmear no añade oro por minion a las reglas personalizadas de Normalplay.

Las pruebas largas recorren ambos lados y las tres dificultades; incluyen
oleada sana, combate real entre minions, Irelia alejándose, hechizos listos,
preparación de último golpe, regreso tras trade, cesión de farm peligroso,
prioridad de R letal y conservación de la persecución propia de PvP.

Validacion final del farm de Mel: 90 pruebas PlayMode distintas aprobadas y161 reglas. Incluye ambos equipos/tres dificultades, oleada combatiendo, hechizos listos, canon urgente y reserva de mana. Informe: `TestResults/mel-lane-farming-validation.json`. Se conservo la sesion principal de Unity.
