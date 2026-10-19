# Runas seleccionadas: investigación y clasificación

Consulta: 18/10/2026. Investigación previa a la implementación. Para las decisiones finales y el funcionamiento activo, ver RUNES_IMPLEMENTATION.md; las dudas de este documento son históricas.

Se leyeron las 18 fichas de League of Legends Wiki para PC, además de los tres elixires de Triple Tonic. Se contrastaron sus descripciones con Data Dragon 16.20.1 y notas de Riot. No se mezclan valores de Wild Rift, ARAM o Arena.

## Páginas solicitadas

- Irelia, común: Conqueror, Presence of Mind, Legend: Alacrity, Last Stand. Alternativas secundarias: Bone Plating + Unflinching; Sudden Impact + Treasure Hunter; Jack of All Trades + Triple Tonic.
- Sett: Conqueror, Presence of Mind, Legend: Alacrity, Last Stand; Shield Bash + Second Wind. Atención a la sustitución oficial de Presence of Mind descrita debajo.
- Mel: Arcane Comet, Manaflow Band, Transcendence, Scorch; Legend: Haste + Cut Down.

## Clasificación

Nivel significa un escalado explícito del efecto, no el aumento indirecto por tener más AD, vida o maná. Farm significa progreso/recompensa por eliminar unidades o participar en bajas; estar cerca para recibir XP no equivale a un last hit. Impactos incluye infligir o recibir daño y CC. Tiempo se refiere al reloj de simulación durante una partida, que se detiene al pausar; no al reloj de Windows.

| Runa | Nivel del usuario | Minions eliminados | Bajas de campeones | Impactos / combate | Tiempo |
|---|---|---|---|---|---|
| Conqueror | Fuerza por carga | No | No necesita matar | Daño infligido a campeones | Cargas duran 5 s |
| Presence of Mind | Maná por impacto | No | Recupera recurso | Daño infligido | CD 8 s; recuperación por baja tras 1 s |
| Legend: Alacrity | No | Sí | Sí | No necesita impactos sin baja | Sin acumulación por minutos |
| Last Stand | No | No | No | Daño a campeones con poca vida propia | Sin temporizador |
| Bone Plating | Reducción | No | No | Recibir daño de campeón | Ventana 1,5 s; CD 55 s |
| Unflinching | No | No | No | Recibir CC de campeón | Persiste 2 s después del CC |
| Sudden Impact | Daño | No | No | Dañar campeón después de dash/blink/sigilo | Ventana 4 s; CD 10 s |
| Treasure Hunter | No | No | Enemigos diferentes | No basta golpear | Sin temporizador |
| Jack of All Trades | No | No | No | No; depende de inventario | Sin temporizador |
| Triple Tonic | Entrega en 3/6/9 | No acumula por farm | No | Primer elixir mejora golpes a minions | Dos elixires duran 60 s; primero paga al terminar |
| Arcane Comet | Daño y CD | No | No | Daño de habilidad/pet a campeón | Impacto tras 0,825 s y CD |
| Manaflow Band | No | No | No | Habilidad/debuff válido a campeón | CD 15 s; regeneración cada 5 s al completar |
| Transcendence | Hitos 5/8/11 | No | Efecto habilitado desde 11 | No basta golpear | Reduce CD restante; sin crecimiento por minutos |
| Scorch | Daño | No | No | Daño de habilidad a campeón | Daño tras 1 s; CD 10 s |
| Legend: Haste | No | Sí | Sí | No necesita impactos sin baja | Sin acumulación por minutos |
| Cut Down | No | No | No | Daño a campeón con más de 60% HP | Sin temporizador |
| Shield Bash | Daño base | No | No | Obtener escudo y atacar campeón | Ataque disponible hasta 2 s tras expirar escudo |
| Second Wind | No explícito | No | No | Recibir daño de campeón | Regeneración durante 10 s, refrescable |

Las dos Legend también admiten monstruos: 100 puntos por baja de épico y 25 por monstruo grande. No existen esos objetivos en nuestro mapa.

## Fichas resumidas

Los valores de esta sección reproducen la información numérica visible en la wiki al consultar. Los extremos discrepantes se identifican más adelante y NO se consideran confirmados para implementación.

### Conqueror

Daño válido a campeones genera hasta 12 cargas, refrescables durante 5 s. Básicos: 2 cargas melee y 1 ranged; habilidades válidas normalmente 2. No debe duplicarse por cada on-hit del mismo golpe ni indiscriminadamente por cada tick. La wiki muestra por carga 1,08–2,56 AD o 1,8–4,26 AP; discrepante con Data Dragon. A cargas máximas cura 8% melee / 5% ranged del daño posterior a mitigación contra campeones, no contra minions.

Fuente: [Conqueror](https://wiki.leagueoflegends.com/en-us/Conqueror).

### Presence of Mind

Daño a campeón restaura maná según nivel, con CD 8 s. Wiki: 6–56 melee, 80% para ranged; Data Dragon: 6–50. Baja/asistencia restaura 15% del maná/energía máximo tras 1 s. La wiki indica que LoL sustituye esta runa por Triumph para campeones sin maná ni energía: incluye a Sett. Rectifica la explicación previa que solamente la describía como inútil para él.

Fuente: [Presence of Mind](https://wiki.leagueoflegends.com/en-us/Presence_of_Mind).

### Legend: Alacrity y Legend: Haste

Cada 100 puntos otorga una carga permanente, máximo 10. Minion eliminado: 4 puntos; baja/asistencia de campeón: 100. Solo farm: 25 minions por carga, 250 para completar. Alacrity comienza en 3% AS y añade 1,5% por carga, máximo 18%. Haste comienza en 0 y añade 1,5 de aceleración de habilidades básicas por carga, máximo 15; no aplica a R, invocadores ni objetos.

Fuentes: [Alacrity](https://wiki.leagueoflegends.com/en-us/Legend:_Alacrity), [Haste](https://wiki.leagueoflegends.com/en-us/Legend:_Haste).

### Last Stand

Aumenta daño a campeones un 5% cuando la vida propia está debajo de 60%; aumenta hasta 11% al llegar al umbral de 30%. No es una carga permanente, ni escala explícitamente por nivel, ni exige matar al objetivo.

Fuente: [Last Stand](https://wiki.leagueoflegends.com/en-us/Last_Stand).

### Bone Plating

Recibir daño de campeón habilita una ventana de 1,5 s para reducir los siguientes tres ataques/habilidades de ese mismo campeón; CD 55 s. El golpe que activa no se reduce. Wiki muestra 30–63,53 por nivel; Data Dragon, 30–60. Se aplica después de mitigación; la ficha incluye daño verdadero. La wiki indica que daño absorbido íntegramente por escudos no la activa.

Fuente: [Bone Plating](https://wiki.leagueoflegends.com/en-us/Bone_Plating).

### Unflinching

Otorga 10 de armadura y 10 de resistencia mágica durante CC enemigo y 2 s posteriores. No otorga tenacidad en esta versión. En un impacto que simultáneamente daña y aplica CC, las resistencias se activan después de ese daño inicial.

Fuente: [Unflinching](https://wiki.leagueoflegends.com/en-us/Unflinching).

### Sudden Impact

Tras dash/blink/salir de sigilo, permite infligir 20–80 de daño verdadero adicional por nivel al dañar a un campeón dentro de los siguientes 4 s; CD 10 s. Q de Irelia y Flash son activadores relevantes. Saltar a un minion puede preparar el efecto, pero el daño adicional exige después golpear a un campeón.

Fuente: [Sudden Impact](https://wiki.leagueoflegends.com/en-us/Sudden_Impact).

### Treasure Hunter

Primera baja/asistencia de cada campeón enemigo distinto concede respectivamente 50/70/90/110/130 de oro: máximo 450 con cinco identidades. Repetir bajas sobre el mismo oponente no vuelve a pagar. En un 1v1 fijo solo se consigue la primera recompensa. Cambiar el bot del simulador no debería inventar oponentes nuevos sin una regla personalizada explícita.

Fuente: [Treasure Hunter](https://wiki.leagueoflegends.com/en-us/Treasure_Hunter).

### Jack of All Trades

Cada tipo diferente de estadística aportada por el inventario actual da una carga y 1 de AH. A 5 tipos otorga 8 de fuerza adaptable; a 10, 20 en total (4,8/12 AD o 8/20 AP). Dos objetos con AD no cuentan como dos tipos. Debe recalcularse al comprar/vender. Movimiento plano y porcentual son tipos distintos; efectos de objetos tienen excepciones documentadas. Las estadísticas de runas no cuentan como estadísticas de objetos.

Fuente: [Jack of All Trades](https://wiki.leagueoflegends.com/en-us/Jack_of_All_Trades).

### Triple Tonic

Entrega consumibles en niveles 3, 6 y 9. Avarice: +5 daño verdadero on-hit a minions por 60 s y 60 oro al terminar. Force: 25 fuerza adaptable (15 AD o 25 AP) durante 60 s. Skill: punto de habilidad adicional, sin subir nivel ni saltar límites legales de rango; no permite superar los 18 puntos totales de Irelia. Los consumibles requieren uso; no debe confundirse recibirlos con activarlos automáticamente. Si no hay espacio, Avarice/Force esperan; Skill se consume automáticamente.

Fuentes: [Triple Tonic](https://wiki.leagueoflegends.com/en-us/Triple_Tonic), [Avarice](https://wiki.leagueoflegends.com/en-us/Elixir_of_Avarice), [Force](https://wiki.leagueoflegends.com/en-us/Elixir_of_Force), [Skill](https://wiki.leagueoflegends.com/en-us/Elixir_of_Skill).

### Arcane Comet

Una habilidad/pet que daña a un campeón lanza un proyectil hacia su ubicación de activación, con impacto tras 0,825 s y radio dañino 140 unidades (indicador visual 175). Puede esquivarse. Wiki muestra 15–110 +10% bonus AD +5% AP y CD 20–6,59; Data Dragon, 15–100 y CD 20–8. El daño crece hasta +100% por distancia; Riot confirma máximo a 750 unidades y la eliminación del antiguo reembolso de CD por golpes posteriores.

Fuentes: [Arcane Comet](https://wiki.leagueoflegends.com/en-us/Arcane_Comet), [Riot 26.9](https://www.leagueoflegends.com/en-gb/news/game-updates/league-of-legends-patch-26-9-notes/).

### Manaflow Band

Habilidad/debuff válido a campeón concede +25 maná máximo, hasta +250, con CD 15 s. No basta un básico corriente. No rellena automáticamente el maná actual. Al completar, restaura 1% del maná faltante cada 5 s. El progreso es por activaciones válidas, no por CS, kills ni minutos; debe distinguirse del recurso temporal de Presence of Mind.

Fuente: [Manaflow Band](https://wiki.leagueoflegends.com/en-us/Manaflow_Band).

### Transcendence

Nivel 5 concede +5 AH; nivel 8 otros +5. Desde 11, baja/asistencia reduce 20% de los enfriamientos restantes de habilidades básicas. No acumula AH por minions. En nuestro simulador, el reset completo al morir cualquier campeón eclipsa el beneficio de la baja, pero no los +10 AH.

Fuente: [Transcendence](https://wiki.leagueoflegends.com/en-us/Transcendence).

### Scorch

Daño de habilidad a campeón provoca daño mágico adicional tras 1 s, con CD 10 s. Wiki muestra 20–42,35 por nivel; Data Dragon 20–40. No se aplica una vez por misil de Mel si sigue en CD. Su daño de proc no debe disparar recursivamente nuevas activaciones de runas.

Fuente: [Scorch](https://wiki.leagueoflegends.com/en-us/Scorch).

### Cut Down

Aumenta 8% el daño contra campeones con más de 60% de vida máxima. Se evalúa el porcentaje de vida del objetivo; no la diferencia de vida máxima entre los dos campeones. Sin cargas ni CD.

Fuente: [Cut Down](https://wiki.leagueoflegends.com/en-us/Cut_Down).

### Shield Bash

Obtener un escudo prepara un básico potenciado contra campeón. Wiki: 5–32,94 por nivel +2,5% vida adicional +15% escudo; Data Dragon: base 5–30. Usa valor inicial del escudo, no solamente lo que queda al atacar; una activación por escudo. La oportunidad termina 2 s después de expirar. No sigue dando armadura/RM, efecto eliminado. Relevante para W y Sterak de Sett, no por acumular Grit sin lanzar W.

Fuente: [Shield Bash](https://wiki.leagueoflegends.com/en-us/Shield_Bash).

### Second Wind

Recibir daño de campeón inicia regeneración equivalente a 4% de vida faltante durante 10 s; nuevos daños refrescan duración, no generan una pila independiente por golpe. Depende de vida faltante, no de un escalado explícito de nivel. La wiki excluye activación por daño 0, invulnerabilidad o daño absorbido por escudos.

Fuente: [Second Wind](https://wiki.leagueoflegends.com/en-us/Second_Wind).

## Fragmentos solicitados

- Todos: +10% AS y +9 fuerza adaptable (5,4 AD o 9 AP).
- Irelia: +65 HP fijos.
- Mel/Sett: fragmento de HP por nivel. Las capturas dicen 10–180, pero la tabla actual de la wiki dice 10–200; pendiente de verificar con cliente del parche elegido antes de fijar fórmula.
- El 3% inicial de Alacrity es adicional al fragmento: Irelia/Sett tendrían 13% AS de runas antes de obtener cargas Legend. No se incorpora a sus bases intrínsecas.

Fuente: [Rune, Shards](https://wiki.leagueoflegends.com/en-us/Rune#Shards).

## Discrepancias que no deben convertirse automáticamente en cambios de balance

| Campo | Wiki consultada, descripción principal | Data Dragon 16.20.1 |
|---|---|---|
| Conqueror, fuerza adaptable/carga | 1,8–4,26 AP equivalente | 1,8–4 |
| Presence of Mind, maná melee | 6–56 | 6–50 |
| Bone Plating, reducción | 30–63,53 | 30–60 |
| Arcane Comet, base / CD | 15–110 / 20–6,59 s | 15–100 / 20–8 s |
| Scorch, daño | 20–42,35 | 20–40 |
| Shield Bash, daño base | 5–32,94 | 5–30 |

La propia sección histórica de varias fichas muestra extremos distintos de su descripción principal. Por lo tanto, estas diferencias NO demuestran por sí solas un cambio de parche: podrían corresponder a fórmulas/plantillas, datos desactualizados o diferencias de presentación. Para implementar números exactos: contrastar con registros del cliente o mediciones reales y congelar una referencia común; no elegir arbitrariamente la cifra mayor.

Data Dragon: [Runas 16.20.1](https://ddragon.leagueoflegends.com/cdn/16.20.1/data/en_US/runesReforged.json), [Objetos 16.20.1](https://ddragon.leagueoflegends.com/cdn/16.20.1/data/en_US/item.json). Copia de las 18 descripciones en TestResults/runes-ddragon-16.20.1.json (ignorado por Git).

## Consecuencias para nuestros modos, sin decisiones implementadas

1. Nivel/XP y oro están relacionados; el reloj es independiente. Subir con Level Up no avanza minutos ni concede CS.
2. Ninguna runa seleccionada acumula poder permanente únicamente por alcanzar minutos de partida. Sus segundos son ventanas de combate, regeneraciones o duración de consumibles.
3. Legend requiere una decisión: eventos reales de farm/bajas o progreso equivalente por nivel/XP para ambos campeones. La segunda opción sería personalizada; no sumar ambos sistemas para el mismo progreso.
4. Manaflow acumula por impactos válidos; no otorgar cargas por nivel sin definir una adaptación.
5. Treasure Hunter y Avarice introducen oro fuera de nuestra tabla. Mantenerlos fieles introduce excepciones económicas; repartir ese oro al oponente sería otra adaptación, no la regla oficial.
6. Triple Tonic requiere distinguir puntos de habilidad de nivel y de estadísticas. Su punto adicional no paga oro de Level Up ni sube minions de nivel.
7. La selección persistente de Irelia debe estar separada del estado de partida. Cambiar páginas no debe volver a entregar oro/elixires ya reclamados; las cargas temporales tampoco deberían transferirse sin definirlo.
8. No se decidió si el reset de CD por muerte incluirá runas. La regla actual cubre habilidades, invocadores y objetos; extenderla a Bone Plating/Comet/etc. debe ser explícito. Las cargas permanentes no son enfriamientos y no deberían borrarse por ese reset.
9. Práctica no consume maná y reinicia CD de habilidades: eso reduce la utilidad observable de ciertas runas, pero no debe alterar sus valores en el resto de modos.

No se modificaron scripts, UI, estadísticas ni preferencias como parte de esta investigación.
