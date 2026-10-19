# Runas implementadas — 18/10/2026

Las bases intrínsecas de cada campeón permanecen en ChampionStatReference. Los fragmentos, las runas y sus estados se aplican después en todos los modos. Vladimir no recibe una página nueva. No se usa el reloj de partida para aumentar las cargas virtuales.

## Páginas e interfaz

- Irelia: Precisión + Valor (predeterminada), Dominación o Inspiración; selección guardada en MidLane.IreliaRunes.v1. Cambiar a otra página reinicia la partida. La opción actual no permite clic.
- Botón de dos flechas circulares debajo del retrato; menú con parejas de iconos oficiales de las ramas, sin texto dentro de las opciones. Se cierra con Escape, clic exterior, tienda, pausa o reinicio.
- Sett: Conqueror, Triumph, Alacrity, Last Stand, Shield Bash, Second Wind.
- Mel: Comet, Manaflow, Transcendence, Scorch, Legend: Haste, Cut Down.
- Irelia mantiene Conqueror, Presence of Mind, Alacrity y Last Stand; secundarias según las tres capturas originales.

## Progreso virtual por nivel

Puntos Legend = CS × 4 + (K+A) × 100; diez cargas máximas, una cada 100 puntos. No se vuelven a sumar los minions/bajas reales. Al 50% de experiencia se usa el punto medio entre los puntos de este nivel y el siguiente; cargas finales redondeadas hacia abajo. Manaflow interpola igualmente sus cargas, con redondeo hacia abajo. En PvP la primera de cada dos muertes de progresión representa el 50%; Stop Level congela este avance. Las muertes ocurridas durante Stop Level no avanzan ese contador.

Los hitos de Treasure Hunter, pociones y Transcendence se desbloquean al alcanzar su nivel entero. El daño y los enfriamientos que escalan por nivel usan L real, no niveles fraccionarios ni el reloj.

| Nivel | CS virtual | K+A | Puntos Legend | Cargas | Alacrity AS | Haste básica de Mel | Manaflow: maná adicional | Treasure acumulado |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | 0 | 0 | 0 | 0 | 3% | 0 | 25 | 0 |
| 2 | 7 | 0 | 28 | 0 | 3% | 0 | 50 | 0 |
| 3 | 13 | 0 | 52 | 0 | 3% | 0 | 75 | 0 |
| 4 | 24 | 0 | 96 | 0 | 3% | 0 | 125 | 0 |
| 5 | 35 | 1 | 240 | 2 | 6% | 3 | 150 | 0 |
| 6 | 48 | 3 | 492 | 4 | 9% | 6 | 200 | 0 |
| 7 | 60 | 4 | 640 | 6 | 12% | 9 | 225 | 0 |
| 8 | 73 | 5 | 792 | 7 | 13.5% | 10.5 | 250 | 0 |
| 9 | 86 | 6 | 944 | 9 | 16.5% | 13.5 | 250 | 50 |
| 10 | 100 | 7 | 1100 | 10 | 18% | 15 | 250 | 50 |
| 11 | 114 | 8 | 1256 | 10 | 18% | 15 | 250 | 120 |
| 12 | 128 | 9 | 1412 | 10 | 18% | 15 | 250 | 210 |
| 13 | 145 | 10 | 1580 | 10 | 18% | 15 | 250 | 210 |
| 14 | 161 | 11 | 1744 | 10 | 18% | 15 | 250 | 210 |
| 15 | 180 | 12 | 1920 | 10 | 18% | 15 | 250 | 320 |
| 16 | 201 | 12 | 2004 | 10 | 18% | 15 | 250 | 320 |
| 17 | 223 | 13 | 2192 | 10 | 18% | 15 | 250 | 320 |
| 18 | 240 | 14 | 2360 | 10 | 18% | 15 | 250 | 320 o 450 |

AS de la tabla solo incluye Alacrity. El fragmento agrega otro 10%. Treasure solo existe en la página correspondiente de Irelia; sorteo de 50% una vez al alcanzar 18, no se repite con Level Up, muertes o tick. El KDA de la tabla es virtual y no altera el marcador real. Se tomó el marcador acumulado aportado por el usuario: el incremento D de nivel 5 era inconsistente, pero D no interviene en Legend.

## Valores por nivel confirmados por las capturas del cliente

Interpolación de los extremos confirmados: f(L)=inicio+(fin−inicio)×(L−1)/17. Los valores internos no se redondean al mostrarlos con cuatro decimales en esta tabla. Las capturas confirman los extremos; no son mediciones independientes de los 18 valores intermedios.

| Nivel | Conqueror AF/carga | Conqueror AD/carga | Presence: maná melee | Bone: reducción | Shield Bash: base | Vida del fragmento Mel/Sett |
|---|---:|---:|---:|---:|---:|---:|
| 1 | 1.8 | 1.08 | 6 | 30 | 5 | 10 |
| 2 | 1.9294 | 1.1576 | 8.5882 | 31.7647 | 6.4706 | 20 |
| 3 | 2.0588 | 1.2353 | 11.1765 | 33.5294 | 7.9412 | 30 |
| 4 | 2.1882 | 1.3129 | 13.7647 | 35.2941 | 9.4118 | 40 |
| 5 | 2.3176 | 1.3906 | 16.3529 | 37.0588 | 10.8824 | 50 |
| 6 | 2.4471 | 1.4682 | 18.9412 | 38.8235 | 12.3529 | 60 |
| 7 | 2.5765 | 1.5459 | 21.5294 | 40.5882 | 13.8235 | 70 |
| 8 | 2.7059 | 1.6235 | 24.1176 | 42.3529 | 15.2941 | 80 |
| 9 | 2.8353 | 1.7012 | 26.7059 | 44.1176 | 16.7647 | 90 |
| 10 | 2.9647 | 1.7788 | 29.2941 | 45.8824 | 18.2353 | 100 |
| 11 | 3.0941 | 1.8565 | 31.8824 | 47.6471 | 19.7059 | 110 |
| 12 | 3.2235 | 1.9341 | 34.4706 | 49.4118 | 21.1765 | 120 |
| 13 | 3.3529 | 2.0118 | 37.0588 | 51.1765 | 22.6471 | 130 |
| 14 | 3.4824 | 2.0894 | 39.6471 | 52.9412 | 24.1176 | 140 |
| 15 | 3.6118 | 2.1671 | 42.2353 | 54.7059 | 25.5882 | 150 |
| 16 | 3.7412 | 2.2447 | 44.8235 | 56.4706 | 27.0588 | 160 |
| 17 | 3.8706 | 2.3224 | 47.4118 | 58.2353 | 28.5294 | 170 |
| 18 | 4 | 2.4 | 50 | 60 | 30 | 180 |

| Nivel | Comet base | Comet CD (s) | Scorch | Sudden Impact | Transcendence AH |
|---|---:|---:|---:|---:|---:|
| 1 | 15 | 20 | 20 | 20 | 0 |
| 2 | 20 | 19.2941 | 21.1765 | 23.5294 | 0 |
| 3 | 25 | 18.5882 | 22.3529 | 27.0588 | 0 |
| 4 | 30 | 17.8824 | 23.5294 | 30.5882 | 0 |
| 5 | 35 | 17.1765 | 24.7059 | 34.1176 | 5 |
| 6 | 40 | 16.4706 | 25.8824 | 37.6471 | 5 |
| 7 | 45 | 15.7647 | 27.0588 | 41.1765 | 5 |
| 8 | 50 | 15.0588 | 28.2353 | 44.7059 | 10 |
| 9 | 55 | 14.3529 | 29.4118 | 48.2353 | 10 |
| 10 | 60 | 13.6471 | 30.5882 | 51.7647 | 10 |
| 11 | 65 | 12.9412 | 31.7647 | 55.2941 | 10 |
| 12 | 70 | 12.2353 | 32.9412 | 58.8235 | 10 |
| 13 | 75 | 11.5294 | 34.1176 | 62.3529 | 10 |
| 14 | 80 | 10.8235 | 35.2941 | 65.8824 | 10 |
| 15 | 85 | 10.1176 | 36.4706 | 69.4118 | 10 |
| 16 | 90 | 9.4118 | 37.6471 | 72.9412 | 10 |
| 17 | 95 | 8.7059 | 38.8235 | 76.4706 | 10 |
| 18 | 100 | 8 | 40 | 80 | 10 |

- Conqueror: requiere golpes a campeones, hasta 12 cargas, duración refrescable 5 s. Cura 8% melee/5% ranged del daño a campeones una vez completo. Una misma habilidad no acumula por cada on-hit/tick simultáneo; DoT admite una nueva aplicación cada 4 s por cast.
- Presence of Mind: CD 8 s en recuperación por daño, ranged recibe 80%; baja real recupera 15% del recurso máximo tras 1 s.
- Bone Plating: el golpe que activa no se reduce; siguientes tres ataques/habilidades del mismo campeón durante 1,5 s, CD 55 s, una reducción por cast de habilidad. No se activa por daño que no alcance vida.
- Shield Bash: añade 2,5% de vida adicional y 15% del escudo inicial a la base; consume un básico contra campeón y dura hasta 2 s después de expirar el escudo. W, Sterak, Maw, Barrier, Eclipse y otros escudos de objetos notifican el efecto.
- Comet: suma 10% AD adicional +5% AP; multiplica hasta ×2 a 750 unidades. Proyectil hacia ubicación capturada, impacto tras 0,825 s, radio 140 unidades más radio del objetivo. Se puede esquivar; no usa el antiguo reembolso de CD por cada golpe.
- Scorch: daño mágico a la víctima tras 1 s; CD 10 s.
- Sudden Impact: ventana 4 s después de Q/Flash, daño verdadero adicional, CD 10 s. Un salto a minion puede prepararlo, pero solo se consume al dañar campeón.
- Transcendence: desde 11 reduce 20% del CD restante de básicas por baja real; el reset global de muerte de nuestro modo eclipsa esta parte, pero no sus +10 AH.

## Efectos sin crecimiento propio por nivel

| Runa | Funcionamiento |
|---|---|
| Last Stand | +5–11% daño físico/mágico contra campeones con vida propia por debajo de 60%, máximo en 30%. |
| Cut Down | +8% daño físico/mágico a campeones por encima de 60% HP. |
| Unflinching | +10 armadura/RM durante CC y 2 s posteriores; no tenacidad. |
| Second Wind | Regeneración basada en 4% de vida faltante durante 10 s; refresca sin apilar efectos independientes. |
| Jack of All Trades | +1 AH por tipo distinto de estadística del inventario; 8/20 AF totales a 5/10 tipos; compra/venta recalcula. |
| Triumph (Sett) | Por baja real, tras 1 s cura 2,5% HP máximo +5% HP faltante y concede 20 oro solo a Sett. |
| Manaflow completa | Restaura 1% del maná faltante cada 5 s. Aumentar capacidad por progreso no rellena el recurso. |

## Triple Tonic y reglas de reinicio

- Elixires 2151/2152/2150 entregados una sola vez en 3/6/9. Todos se consumen manualmente desde inventario (clic o teclas 1–6); si no hay espacio esperan a un slot libre. No se venden en tienda ni generan ingresos por venta.
- PvP: Avarice paga 60 oro inmediatamente al usar; +5 verdadero on-hit contra minions (incluida Q). Force añade 25 AF. Sus buffs temporales terminan con la siguiente muerte de cualquier campeón. Nunca vuelven a otorgarse tras morir.
- Normalplay: ambos buffs duran 60 s de simulación; Avarice paga sus 60 oro al expirar (pendiente si el oro está congelado).
- Práctica: no entrega elixires.
- Skill es permanente: adelanta el siguiente punto legal del plan, no da nivel/XP/oro ni supera los límites de R. A nivel 17 puede tener los 18 puntos; nivel 18 ya no agrega otro. Se conserva al reconstruir niveles PvP.
- Treasure Hunter, Avarice y Triumph no compensan su oro al rival. El oro común por niveles conserva su tabla y ledger separados.
- Cada muerte de campeón reinicia también CD de runas para ambos bandos, sin borrar Legend/Manaflow/Treasure. El campeón muerto pierde efectos temporales como Conqueror; Force/Avarice cumplen la regla del modo.
- Pausa detiene sus temporizadores. Los resets de inactividad de habilidades no se extendieron a runas: el usuario pidió específicamente reset de runas por muerte.

## Referencias y validación

Valores: Data Dragon 16.20.1 + capturas del cliente aportadas por el usuario. Las discrepancias previas de la wiki no reemplazan esas cifras. Investigación: RUNES_RESEARCH.md. Pesos Legend y detalles de activación contrastados con las fichas PC; progresión CS/KDA/Manaflow/Treasure y reglas de modos son personalizadas.

Las pruebas de bases oficiales desactivan explícitamente la capa de runas: así continúan verificando los atributos intrínsecos y no confunden fragmentos con estadísticas base. RuneSystemTests verifica la capa activa por separado. Resultados finales de validación se registran en TestResults/runes-validation-summary.json.

Validación final de runas: 188/188 pruebas PlayMode aprobadas (incluida captura renderizada del selector), 161 comprobaciones de reglas aprobadas. Informe: TestResults/runes-validation-summary.json.

## Paso del 50% de experiencia

| Nivel actual | Legend al comenzar | Legend al 50% XP | Manaflow al comenzar | Manaflow al 50% XP |
|---|---:|---:|---:|---:|
| 1 | 0 | 0 | 25 | 25 |
| 2 | 0 | 0 | 50 | 50 |
| 3 | 0 | 0 | 75 | 100 |
| 4 | 0 | 1 | 125 | 125 |
| 5 | 2 | 3 | 150 | 175 |
| 6 | 4 | 5 | 200 | 200 |
| 7 | 6 | 7 | 225 | 225 |
| 8 | 7 | 8 | 250 | 250 |
| 9 | 9 | 10 | 250 | 250 |
| 10 | 10 | 10 | 250 | 250 |
| 11 | 10 | 10 | 250 | 250 |
| 12 | 10 | 10 | 250 | 250 |
| 13 | 10 | 10 | 250 | 250 |
| 14 | 10 | 10 | 250 | 250 |
| 15 | 10 | 10 | 250 | 250 |
| 16 | 10 | 10 | 250 | 250 |
| 17 | 10 | 10 | 250 | 250 |
| 18 | 10 | 10 | 250 | 250 |

Con la interpolación acordada, Legend completa sus diez cargas al 50% del nivel 9 (1022 puntos). Manaflow completa las diez al alcanzar nivel 8; el punto medio entre 9 y 10 cargas se redondea a 9.
