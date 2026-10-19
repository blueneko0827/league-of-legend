# Mel — auditoría del kit y estadísticas

Referencia de PC del proyecto: **Data Dragon 16.20.1 y registros del cliente 16.20**.
Se conserva la escala de 100 unidades de LoL por unidad de Unity; el multiplicador
visual de los modelos no cambia ninguna colisión. La dificultad tampoco multiplica daño.

## Estadísticas

La tabla del usuario ya coincidía con `LeagueChampionStats`: HP 630 +99,
HP5 6 +0,55, MP 480 +28, MP5 9 +0,9, AD 54 +3,3, AS 0,625 +2,5%,
AR 21 +5,2, MR 30 +1,3, MS 330 y rango 550.
Se valida cada nivel 1–18 con `n × (0,7025 + 0,0175 × n)`.
HP5/MP5 se dividen por cinco para simular regeneración por segundo.
Las estadísticas de objetos se suman al crecimiento base; AP, penetración, resistencia,
escudos y reducción de daño participan en la resolución normal del combate.

## Diferencias con el texto aportado

El texto mezcla versiones anteriores. Se prioriza el parche fijado del proyecto:

| Campo | Referencia utilizada |
|---|---|
| Q daño inicial | 60/85/110/135/160 +55% AP |
| Q impactos siguientes | 5/7/9/11/13 +5% AP |
| Q proyectiles | 6/7/8/9/10, durante 0,5 s; lanzamiento 0,35 s, velocidad 3800 |
| Q coste / CD | 70/75/80/85/90; 10/9/8/7/6 s |
| Q geometría | rango 950; área de distribución 250; explosiones individuales de radio 200 |
| W | Escudo 80/110/140/170/200 +70% AP; **sin inmunidad** |
| W duración | 0,75 s de escudo, reflejo y velocidad decreciente (+40% inicial) |
| W CD / coste | 38/35/**33**/29/26 s; 80/60/40/20/0 |
| W reflejo | 40/45/50/55/60% +0,05% por punto de AP; daño físico ×0,7 antes de convertir a mágico |
| E daño central | 60/105/150/195/240 +70% AP |
| E aura por tick | 2/3,5/5/6,5/8 +1% AP cada 0,125 s |
| E geometría | 1000 de rango; velocidad 1100; núcleo radio 70, aura radio 230, crecimiento durante 0,5 s |
| E control / coste / CD | Raíz 1,5 s; slow 30%; 50/60/70/80/90; 11/10,5/10/9,5/9 s |
| E súbditos | 50% tanto del daño central como del aura |
| Pasiva proyectiles | 3 por lanzamiento, hasta 9; duran 5 s; 8–30 por nivel +4% AP cada uno; 50% a súbditos |
| Overwhelm primer daño | 50/60/70/80 +10% AP, según rango de R 0/1/2/3 |
| Overwhelm acumulaciones posteriores | 2/3/4/5 +0,75% AP; duran 5 s; daño almacenado reducido por RM/penetración |
| R | Global sobre marcados; requiere al menos un campeón marcado; coste 100, CD 120/100/80 s |
| R daño | 125/200/275 +30% AP, más 4/7/10 +4% AP por marca; lanzamiento 0,75 s |

R conserva las marcas anteriores y su propio impacto agrega una marca. También afecta
súbditos marcados, con 50% de daño. Los ataques normales de Mel ahora tienen proyectil
dirigido; la ráfaga de la pasiva produce impactos separados, no un único golpe agregado.
Cada objetivo de Q recibe su propio primer impacto completo.

El valor de W en rango 3 presenta una discrepancia de origen: las notas de 26.8 dicen
32 s, pero **ambos archivos fijados del cliente y Data Dragon dicen 33 s**. Se preserva
33 y se documenta la diferencia, sin mezclar selectivamente referencias de parches.

## Interacciones y límites

- E aplica raíz, no stun: bloquea caminar, Q de Irelia y Flash, pero permite básicos
  y habilidades sin desplazamiento. Respeta tenacidad e interrumpe la Q en curso.
- W puede activarse durante el lanzamiento de otra habilidad y no cancela el básico.
  Su reflejo permanece activo aunque se consuma su escudo. No refleja torres ni súbditos.
- Se devuelve el proyectil de W/R de Irelia. R reflejada conserva su pared y cambia
  el propietario del daño; los efectos de suelo y la comprobación de origen de R
  no son proyectiles y no se reflejan. Durante Rebuttal, Mel bloquea la convergencia
  de E de Irelia que la alcanza: no recibe daño, stun ni una nueva marca Unsteady.
  No se devuelve un proyectil dirigido ni se protege a otros objetivos de esa E.
  No limpia un stun anterior; el reflejo sigue activo aunque el escudo se consuma.
- Las marcas identifican la vida del objetivo: no sobreviven a su muerte/reaparición.
  La ejecución contempla vida y escudos disponibles, y nunca ejecuta los dummies.
- Se conservan las reglas personalizadas de PvP, compras, progresión y reinicio de CD.
- Las fórmulas provienen del cliente, pero no se dispone del código del servidor.
  La distribución exacta de cada misil Q, la exposición temporal del aura E y los VFX
  se reconstruyen en Unity. No se implementa un sistema universal de rebote para
  todas las habilidades de campeones que no están disponibles en este simulador.

## Fuentes y reproducción

- [Data Dragon: Mel 16.20.1](https://ddragon.leagueoflegends.com/cdn/16.20.1/data/en_US/champion/Mel.json)
- [Registros del cliente 16.20, espejo CommunityDragon](https://raw.communitydragon.org/16.20/game/data/characters/mel/mel.bin.json)
- [Riot 26.3: cambio a escudo y geometría de Q/E](https://www.leagueoflegends.com/en-us/news/game-updates/patch-26-3-notes/)
- [Riot 26.5: aumento de daño base R](https://www.leagueoflegends.com/en-us/news/game-updates/league-of-legends-patch-26-5-notes/)
- [Riot 26.8: Q y duración/enfriamiento W](https://www.leagueoflegends.com/en-us/news/game-updates/league-of-legends-patch-26-8-notes/)
- [Riot: implementación de reflexión y atribución de daño](https://www.riotgames.com/en/news/mel-reflect-tech-blog)

`Tools/Import-MelReference.py` extrae una referencia auditable de las dos descargas
fijadas, incluyendo hashes SHA-256, a `Assets/Resources/LeagueData/MelReference.json`.
El cliente complementa el crecimiento AD que Data Dragon publica como cero.
Las pruebas de `MelAbilitiesTests` contrastan estadísticas, impactos reales, áreas,
costes, enfriamientos, raíz, reflejo, marcas, ejecución y R global.

## Corrección de proyectiles y bloqueo E — 18/10/2026

El ataque básico de Mel se corrigió de3500 a **1600 unidades/s** (16 Unity/s).
Los proyectiles adicionales de Searing Brilliance conservan5000, el valor de su
componente de movimiento, que es distinto del básico. La IA calcula el viaje del
last hit con la nueva velocidad del básico.

Q conserva **3800** (38 Unity/s), lanzamiento0,35s, y E **1100** (11 Unity/s),
lanzamiento0,25s. No se aumentaron esas velocidades ni se redujeron sus tiempos.
Q ahora muestra misiles viajando, en lugar de una línea instantánea al impactar;
cada misil se sincroniza con el daño por su distancia exacta al punto de explosión.
La primera explosión a950 unidades tarda0,35+950/3800=0,60s. En E, el núcleo
puede golpear antes de alcanzar el centro del campeón: suma su radio70 al radio
del objetivo65; a500 unidades la raíz comienza cerca de0,25+(500−135)/1100=0,582s.
El aura230 puede ralentizar o hacer daño antes de ese impacto central.

Tools/Import-MelReference.py ahora conserva `projectiles` con los registros, tipo
de movimiento y velocidad efectiva, además del campo heredado `missileSpeed`.
Para Q el movimiento indica3800 aunque el campo heredado diga3000; no se confunden.
La interacción W/E solicitada se aplica únicamente cuando la convergencia impacta
a Mel mientras W está activa, sin modificar daño, alcance ni tiempos de Irelia.

Validación de esta corrección:73/73 pruebas PlayMode de Mel (kit, IA y proyectiles),
161 reglas de combate. Detalles en `TestResults/mel-projectile-validation.json`
y `TestResults/mel-projectiles.xml`. Se midió el avance de los básicos y de los
misiles Q, la posición de E y el inicio de la raíz por contacto de bordes; también
se probaron W activa/agotada/expirada, ausencia de cleanse y otros objetivos de E.
