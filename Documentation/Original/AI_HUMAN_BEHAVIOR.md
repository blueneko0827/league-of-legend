# Comportamiento humano compartido

La capa se conecta a ChampionBrain y está activa en los bots, con efectos específicos para Sett y Mel. Irelia sigue bajo control del jugador. No modifica atributos, daño, costes, alcances, proyectiles ni enfriamientos reales. Es una simulación programada de comportamiento; los nombres Bronce, Plata y Oro son perfiles iniciales de diseño, pendientes de calibración con partidas humanas.

## Organización

- Core/HumanBehavior.cs: perfiles inmutables y estado individual sin dependencias de Unity.
- ChampionHumanBehavior.cs: adaptación de eventos y decisiones a los módulos existentes.
- ChampionBrain: avanza el reloj mental durante la simulación y mantiene sus reglas de seguridad y de cada campeón.
- LaneWorld: comunica impactos y muertes mediante eventos; el reinicio elimina suscripciones anteriores.

Cada bot tiene su propia frustración, confianza, foco, reloj y secuencia de error. No se comparte estado mutable entre campeones.

## Estados y límites

| Estado | Evolución | Efectos |
| --- | --- | --- |
| Tilt | Empieza en 0; limitado a 0–85%. Sube con muerte, fallo de skillshot ofensivo confirmado por ausencia de impacto, remate comprometido perdido o intercambio desfavorable. Baja con aciertos, farm, bajas y calma. | Hasta +68% del margen de puntería, hasta −21,25% de anticipación y +17% del retraso base. Con 65% o más evita trampas complejas de Sett y seguimientos de Mel tras fallar E. |
| Confidence | Empieza en 50%; limitada a 20–80%. Cambia con bajas, muertes, resultados de intercambios y ejecución; vuelve gradualmente a 50%. | Multiplicador de agresividad 0,82–1,18; distancia preferida 1,12–0,88; umbral de ROI 1,09–0,91. Se mantienen los límites de torre, recorrido, vida y persecución. |
| AttentionFocus | FARM, COMBAT o DEFENSE según intención. Sostiene el foco brevemente para evitar oscilaciones; daño de minions/torres y peligro pueden interrumpirlo. | FARM demora el registro de habilidades secundarias del rival y las respuestas al dash; COMBAT demora la detección de remates nuevos. DEFENSE conserva la atención sobre amenazas y las acciones de protección. |

Los intercambios se evalúan tras dos segundos sin nuevos impactos de campeón, comparando impactos normalizados por vida máxima. El resultado de un skillshot ofensivo se evalúa una vez a los 1,8 s; se descarta si el rival muere/reaparece o muere el bot. Los eventos de farm solo penalizan remates comprometidos, no cualquier minion perdido en el mapa. Las victorias sobre dummies no cuentan como bajas de campeón.

La pausa congela reloj y recepción de feedback mental. La muerte modifica el estado, pero reaparecer no lo reinicia. Reiniciar la partida o cambiar dificultad desde la UI crea un estado nuevo. Los estados evolucionan por combate, no por nivel o XP.

## Perfiles

| Perfil | Retraso periférico adicional | Compromiso de foco | Persistencia del sesgo de puntería | Sensibilidad a frustración |
| --- | --- | --- | --- | --- |
| Bronce | 160 ms | 650 ms | 900 ms | ×1,2 |
| Plata | 100 ms | 500 ms | 750 ms | ×1 |
| Oro | 60 ms | 350 ms | 600 ms | ×0,8 |

Estos valores se combinan con las reacciones y herramientas específicas existentes de cada campeón. No son tiempos oficiales de cada rango. Se conservan los valores serializados de Difficulty (Normal/Hard/Expert), mostrados como Bronce/Plata/Oro en la UI.

## Reproducibilidad, diagnóstico y coste

SandboxSettings.aiHumanSeed (1337 por defecto) genera una secuencia independiente para el sesgo de puntería de cada campeón/equipo. Las mismas entradas y tiempos reproducen esa secuencia. No se altera UnityEngine.Random global ni se afirma que toda la partida sea determinista: otros módulos existentes mantienen su propia aleatoriedad.

Tab muestra foco, frustración y confianza en el diagnóstico del bot. La lógica usa escalares, eventos y un array de cuatro lanzamientos pendientes, sin búsquedas de ruta ni consultas adicionales de todos los minions por frame. Una prueba de 10.000 actualizaciones del estado mental registró 0 bytes asignados; esto no mide las asignaciones ni los FPS de todo el juego.

Mel mantiene el control de W en su módulo defensivo: la retirada genérica ya no la gasta por poca vida sin respetar sus criterios y tiempos de amenaza.

## Verificación

148 pruebas PlayMode distintas aprobadas. Agregación de los últimos resultados por caso en TestResults/human-behavior-summary.json: estados, aislamiento, semilla, pausa, muerte/reinicio, feedback de habilidades, farmeo, reacciones, evasión, seguridad y progresión.
