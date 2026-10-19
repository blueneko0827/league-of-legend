# Perfil de práctica Swiftplay

Referencia consultada el 14 de octubre de 2026. El simulador reproduce el inicio rápido, la progresión de habilidades y las oleadas de la fase de línea. No reproduce todavía todas las reglas del modo completo.

## Inicio y habilidades

Los dos campeones empiezan en nivel 3, con Q/W/E en rango 1, R sin aprender y 1400 de oro. En Irelia las mejoras posteriores siguen siendo manuales. El rival gasta automáticamente los puntos ganados por XP, con R en niveles 6/11/16, Q como prioridad y W como segunda prioridad para Sett o E para Vladimir/Mel. La IA no puede lanzar una habilidad sin aprender y sus costes/enfriamientos dependen del rango.

Los valores de inicio están publicados por [Riot en las notas 26.1](https://www.leagueoflegends.com/en-us/news/game-updates/patch-26-1-notes/) y en [soporte de Swiftplay](https://support.riotgames.com/en-us/league-of-legends/gameplay/swiftplay-game-mode). Q/W/E preaprendidas es la comodidad de práctica solicitada por el usuario. Los costes y enfriamientos del rival vienen de Data Dragon 16.20.1: [Sett](https://ddragon.leagueoflegends.com/cdn/16.20.1/data/en_US/champion/Sett.json), [Vladimir](https://ddragon.leagueoflegends.com/cdn/16.20.1/data/en_US/champion/Vladimir.json) y [Mel](https://ddragon.leagueoflegends.com/cdn/16.20.1/data/en_US/champion/Mel.json). Sus efectos y daños aún conservan el kit provisional.

## Oleadas

El reloj de práctica empieza en 0:28. Saltar esos 28 segundos permite tener la primera oleada al cabo de dos segundos, correspondiente al inicio 0:30 publicado en [el anuncio de Swiftplay](https://www.leagueoflegends.com/en-us/news/dev/dev-a-swifter-swiftplay/).

| Oleadas / reloj | Guerreros | Magos | Cañones | Intervalo |
|---|---:|---:|---:|---:|
| 1 y 2 | 3 | 3 | 0 | 30 s |
| Desde la 3, antes de 12:00 | 3 | 3 | 1 | 30 s |
| Desde 12:00 | 2 | 3 | 1 | 25 s |
| Desde 30:00 | 2 | 2 | 1 | 20 s |

Se usa la indicación explícita de las notas 26.1: cañón **desde la tercera oleada**. El texto de soporte dice «después de la tercera», pero ambas referencias coinciden en cañón en todas las oleadas posteriores. Para el tramo tardío, el perfil adapta la reducción de un guerrero publicada para la Grieta a la aceleración de Swiftplay en 12:00; conserva el ajuste de 30:00 de la Grieta. Esa combinación es una interpretación del simulador, no una tabla completa de servidor Swiftplay verificada. No se omiten oleadas por alcanzar 80 unidades, como ocurría en el prototipo.

## Tipos y modelos

| Tipo | Vida base | Daño físico base | Alcance | Ataques/s | Preparación | XP base | Oro base |
|---|---:|---:|---:|---:|---:|---:|---:|
| Guerrero | 430 | 11 | 110 | 1,25 | 0,393 s | 62 | 20 |
| Mago | 275 | 19,5 | 550 | 0,667 | 0,47 s | 31 | 14 |
| Cañón | 750 | 36 | 300 | 1 | 0,3 s | 75 | 50 |

Velocidad base común: 350 unidades/s. Vida, daño, alcance, velocidad de ataque, preparación y radios corresponden a `CharacterRecords/Root` en los datos de cliente 16.20 extraídos: [guerrero](https://raw.communitydragon.org/16.20/game/data/characters/sru_orderminionmelee/sru_orderminionmelee.bin.json), [mago](https://raw.communitydragon.org/16.20/game/data/characters/sru_orderminionranged/sru_orderminionranged.bin.json), [cañón](https://raw.communitydragon.org/16.20/game/data/characters/sru_orderminionsiege/sru_orderminionsiege.bin.json). XP y oro son las recompensas base publicadas por Riot en 26.1. Estos registros no incluyen todos los ajustes del servidor sobre el tiempo de partida.

Los magos y cañones generan proyectiles dirigidos, con impacto al llegar y pausa compartida. El daño base contra torres usa 60% para guerreros/magos y 84% para cañón, según 26.1. Las velocidades de esos proyectiles son calibraciones visuales de práctica. La prioridad de cañón bajo torre se implementa antes de la distancia, manteniendo la respuesta de torre a ataques contra campeones.

Los tres modelos estáticos proporcionados en `C:\Users\rotte\Downloads\Minions_Unity_3D\Minions` se copiaron a `Assets/Resources/Minions`, sin modificar los originales. Se normaliza altura y apoyo en el suelo y se conservan sus materiales de partes, adaptándolos a URP con colores azules/rojos. Conservan la orientación +Z declarada por el autor. El collider común permanece separado de la geometría de selección visible.

Pendientes: escalado temporal de estadísticas/recompensas, bonificación acelerada de XP/oro, oro por proximidad, compensación por ir perdiendo, Frenesí, reparto de XP entre varios aliados, aggro y distribución inicial de ataques completos. No hay inhibidores ni supers en esta línea de dos torres.
