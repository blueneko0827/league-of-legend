# DemoMidlanePractice — versión demo

Versión demo independiente de MidLane Simulator para mejorar su presentación visual en **modo
Práctica**. Incluye el estado actual de Irelia, cinco dummies, dos torres, tienda,
runas, hechizos, animaciones, sonidos, voces, indicadores y efectos.

1. Instalar **Unity 6000.6.5f1**, con soporte Windows si se desea compilar.
2. Agregar **esta carpeta** como proyecto en Unity Hub y abrirla. También se puede
   ejecutar `Abrir-Practica.cmd` en Windows si Unity está en su ubicación estándar.
3. Esperar la primera importación y abrir **Visual Workshop → Abrir práctica**.
4. Pulsar **Play** y enfocar la pestaña **Game**.

Siempre inicia en Práctica. No hay botones para activar PvP, Normalplay o rivales.
F5 reinicia Práctica. No se necesita una API key ni una instalación de LoL para jugar.
La primera importación puede necesitar conexión para descargar los paquetes de Unity.

Leer [ARTIST_GUIDE.md](ARTIST_GUIDE.md) antes de trabajar. Allí se detallan cámara,
dimensiones, modelos, prefabs, animaciones, partículas, iluminación, HUD y el proceso
de devolución. [INTEGRATION.md](INTEGRATION.md) explica la integración posterior.

Las fuentes completas disponibles de Irelia y del dummy están en `CharacterAssets`.
Los archivos que **usa el juego** están en `Assets`: editar una copia de archivo en
`CharacterAssets` no cambia automáticamente el juego.

Se conservaron dependencias y código compartido del original para facilitar la
integración; esta edición solo ofrece Práctica. No contiene `.git`, credenciales,
API keys, grabaciones de la API local ni las cachés del proyecto original.

La versión de entrega y su validación están registradas en `Handoff`. Compartir el
ZIP preparado o esta carpeta sin `Library`, `Temp`, `Logs`, `Obj`, `UserSettings`,
`Builds` ni `TestResults`.
