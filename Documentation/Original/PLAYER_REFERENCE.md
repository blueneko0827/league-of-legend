# Referencia de Irelia

Referencia congelada el **14 de octubre de 2026**. LoL PC, Data Dragon **16.20.1**, sin objetos, runas ni poder de habilidad. Escala: **100 unidades de LoL = 1 metro de Unity**. No se actualiza el balance automáticamente.

Las estadísticas base de Irelia y los rivales se centralizaron en `LeagueChampionStats.cs`. Ver [CHAMPION_STATS.md](CHAMPION_STATS.md). `BladeTuning.asset` conserva ajustes de habilidades y práctica, sin una segunda copia de vida, daño o velocidad.

## Enfriamientos y maná

| Habilidad | Enfriamiento por rango (s) | Maná |
|---|---|---|
| Q · Embate de Cuchillas | 10 / 9 / 8 / 7 / 6 | 15 |
| W · Danza de Insurrección | 20 / 18 / 16 / 14 / 12 | 70 / 75 / 80 / 85 / 90 |
| E · Dueto Perfecto | 16 / 14,5 / 13 / 11,5 / 10 | 50 en E1; E2 gratuito |
| R · Filo de Vanguardia | 125 / 105 / 85 | 100 |

Contrastados con [Data Dragon de Riot, Irelia 16.20.1](https://ddragon.leagueoflegends.com/cdn/16.20.1/data/en_US/champion/Irelia.json). Se usan los [nombres oficiales en español latinoamericano](https://ddragon.leagueoflegends.com/cdn/16.20.1/data/es_MX/champion/Irelia.json). La aceleración aplica `base × 100 / (100 + aceleración)`; el perfil inicial tiene cero.

## Tiempos implementados

| Elemento | Valor |
|---|---|
| Ataque básico | 0,656 ataques/s de base; preparación equivalente a 0,3 s en un ciclo de 1,525 s, ajustada por velocidad de ataque |
| Q | Sin preparación; velocidad 1400 unidades/s + movimiento; separación mínima de 0,1 s tras reiniciar |
| W | Hasta 1,5 s mantenida; máximo daño a 0,75 s; golpe con preparación de 0,25 s y protección durante esa preparación |
| E | E2 tras 0,25 s; ventana 3,5 s; preparación de E2 0,125 s; viaje de cada hoja 0,25 s y convergencia 0,25 s |
| E: control | Aturdimiento 0,75 s; marca de campeón 5 s |
| R | Preparación 0,4 s; velocidad 2000 unidades/s; muro 2,5 s; ralentización al cruzar 90% durante 1,5 s |
| Pasiva | Cuatro cargas, 6 s; velocidad adicional por carga del 10% al 25% según nivel |

Los campos se contrastaron con los [datos del cliente 16.20 extraídos por CommunityDragon](https://raw.communitydragon.org/16.20/game/data/characters/irelia/irelia.bin.json). Algunos campos publicados conservan datos antiguos o difieren de los rangos de presentación de Data Dragon. Las pequeñas ventanas de encadenamiento, las hitboxes y la protección durante W son calibraciones editables del simulador, no una reproducción de todas las reglas internas del servidor.

Las actualizaciones de Riot contrastadas son [11.14](https://www.leagueoflegends.com/en-us/news/game-updates/patch-11-14-notes/) para velocidad de Q y viaje fijo de E, [14.21](https://www.leagueoflegends.com/en-us/news/game-updates/patch-14-21-notes/) para Q, [25.13](https://www.leagueoflegends.com/en-gb/news/game-updates/patch-25-13-notes/) para la pasiva, [25.17](https://www.leagueoflegends.com/en-sg/news/game-updates/patch-25-17-notes/) para W/E y [26.17](https://www.leagueoflegends.com/en-gb/news/game-updates/league-of-legends-patch-26-17-notes/) para daño de Q y coeficientes de W/R.

## Progresión

Nivel inicial 3, con Q/W/E en rango 1 para ahorrar tiempo. El requisito para subir del nivel `n` al siguiente es `180 + 100 × n` XP. Q/W/E: cinco rangos, con límite `(nivel + 1) / 2`; R: niveles 6, 11 y 16. Máximo nivel 18. Los puntos posteriores se gastan por elección del usuario; la IA los reparte automáticamente.

Las muertes de súbditos enemigos a menos de 1600 unidades conceden XP: 62 por guerrero, 31 por mago y 75 por cañón. No se requiere último golpe para XP; sí para oro. No hay reparto por múltiples campeones aliados ni aceleración completa de recompensas Swiftplay. Ver [SWIFTPLAY_REFERENCE.md](SWIFTPLAY_REFERENCE.md). La curva de crecimiento de estadísticas es `n × (0,7025 + 0,0175 × n)`, con `n = nivel - 1`.

## Límites

Modelos, selección con mouse, hitboxes y muro de R simplificados. El muro es un rombo que permite cruzarlo con daño y ralentización. No se simulan latencia, ticks del servidor, objetos, runas, destello ni todas las interacciones de control y proyectiles. Los rivales todavía tienen kits provisionales.
