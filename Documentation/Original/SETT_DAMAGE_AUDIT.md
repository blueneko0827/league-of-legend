# Auditoría de daño de Sett — 15 de octubre de 2026

La tabla del usuario coincide con todas las estadísticas base y de crecimiento de Irelia y Sett. Se verifica cada nivel de 1 a 18, sin objetos ni buffs, usando la curva `n * (0.7025 + 0.0175 * n)`, n = nivel - 1. HP5 y MP5 se dividen por cinco para el tick de simulación. Los incrementos no se suman linealmente en los niveles iniciales.

Valores al comenzar la práctica (nivel 3, sin objetos):

| Campeón | Vida | Maná | AD | Armadura | MR | AS | HP5 base | MP5 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Irelia | 799.6250 | 423.7500 | 70.1625 | 42.9325 | 33.0237 | 0.680190 | 4.7538 | 9.1800 |
| Sett | 838.1500 | 0.0000 | 65.9000 | 39.9325 | 31.0237 | 0.641133 | 7.7375 | 0.0000 |

## Defectos corregidos

- El puño derecho solo cambiaba el alcance; faltaban su daño adicional `5 * nivel + 55% AD adicional` y la preparación a 8 veces la velocidad del izquierdo. Se conservan el AS base, sus bonus de objetos y los rangos personalizados pedidos.
- Q ahora garantiza izquierda seguida de derecha. Solo dos ataques reciben su bonus de Q; la derecha mantiene su pasiva. Fallar/cancelar la preparación no consume el puño ni añade daño.
- Se implementa la regeneración pasiva que faltaba: 0.15/0.5/1/2 HP por segundo por cada 5% de vida perdida, a niveles 1/6/11/16. El registro del cliente expresa el paso en 1% (0.03/0.1/0.2/0.4), limitado al 95% de vida perdida. Esto se añade al HP5 base; los objetos de regeneración base no multiplican esta pasiva.
- El cálculo financiero de la IA y su predicción de last-hit usan daño y preparación del puño actual. El cálculo del combo recorre su secuencia real de izquierda/derecha y los dos golpes potenciados por Q, sin consumir estados. Los on-hit y las probabilidades de acertar habilidades siguen siendo estimaciones conservadoras, no predicción exacta del servidor de LoL.

## Comprobaciones de daño

A nivel 3, Sett tiene 65.9 AD e Irelia 42.9325 de armadura. Sin objetos, escudos ni W: izquierda = 46.10568 de daño físico; derecha = 56.60019, frente a 46.10568 que infligían ambos antes. El bonus crece con nivel y AD de objetos, no con la dificultad.

W mantiene 80/100/120/140/160 + Grit * (0.25 + 0.0025 * AD adicional). El centro es daño verdadero y los flancos son físicos. Se valida una W con 200 de temple y +10 AD: 135 de daño verdadero. E y R mantienen sus escalados existentes y los rangos del modo personalizado.

Irelia conserva su mitigación física/mágica de W mientras carga, y no mitiga daño verdadero. Armadura, Plated Steelcaps, escudos y Death's Dance también pueden reducir o aplazar el daño visible. El modo prueba bloquea todo daño al jugador cuando está activado; su valor predeterminado sigue desactivado. No se encontró un multiplicador de daño reducido exclusivo de la dificultad de Sett.

## Referencias

- Perfil base congelado en el proyecto: Riot Data Dragon 16.20.1 y registros del cliente 16.20. Tabla aportada por el usuario como contrato de esta revisión.
- [Riot: Sett](https://www.leagueoflegends.com/en-us/champions/sett/): alternancia de puños, derecha más rápida y fuerte, regeneración según vida perdida.
- [Riot, parche 12.20](https://www.leagueoflegends.com/en-gb/news/game-updates/patch-12-20-notes/): regeneración pasiva y 55% AD adicional.
- [Registro del cliente 16.20](https://raw.communitydragon.org/16.20/game/data/characters/sett/sett.bin.json): copia existente TestResults/Reference-sett.json, fórmulas RightPunchBonus, tooltipregenpermissinghealthcalc y daño de Q/W. Se conservan las referencias del parche del simulador; no se migró todo el proyecto al parche publicado hoy.


Validación cerrada: 226 pruebas funcionales distintas aprobadas, 6 capturas omitidas sin dispositivo gráfico y 161 reglas de combate aprobadas. Suite sett-damage-final.xml más regresión final 53/53 en sett-damage-regression.xml; esta última cierra la expectativa de proyectil inminente. Sin errores C# ni excepciones en la ejecución final. Las capturas de pantalla solo se ejecutan con gráficos.


Corrección geométrica posterior: perfil del usuario 790 de alcance, 805 de ancho exterior máximo y 180 de franja verdadera. El código anterior daba solo ≈360 y 60 de ancho, respectivamente. El exterior es un trapecio con una base de aproximadamente 426 y ancho final 805; la franja verdadera independiente tiene 180. Las dimensiones son las mismas al dibujar y al probar el círculo del objetivo. La fórmula de daño no cambia; el área corregida sí permite los impactos laterales y centrales que antes quedaban fuera. Pruebas verifican 130 de daño verdadero o su equivalente físico mitigado con 200 Grit y rango 1, además del escudo inmediato.


Validación de inactividad PvP y W de Sett: 80 pruebas funcionales distintas aprobadas, 161 reglas de combate y captura visual aprobada. Los casos nuevos cubren relojes individuales, movimiento real, daño protegido por escudo, daño persistente, muerte/reaparición, pausa, salida de PvP, habilidades falladas, dimensiones del indicador en cuatro direcciones y daño verdadero/físico. Resultados en TestResults/pvp-idle-haymaker.xml y pvp-idle-haymaker-final.xml; resumen pvp-idle-haymaker-validation.json. Captura sett-haymaker-corrected.png. El helper de captura queda solo en TestResults.
