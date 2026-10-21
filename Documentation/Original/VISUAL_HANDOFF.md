# Entrega para edición visual de Práctica

Se creó un proyecto independiente en `C:\Users\rotte\Downloads\MidLane Practice Visual`
y un archivo para compartir en `C:\Users\rotte\Downloads\MidLane Practice Visual.zip`.
El código, recursos y ajustes activos del simulador completo permanecen intactos.

La versión demo también está publicada en el repositorio privado
[Leonardo-imbimbo/DemoMidlanePractice](https://github.com/Leonardo-imbimbo/DemoMidlanePractice),
rama `main`, commit inicial `8a7b39881bffba0406ee66f27112ed093f77e3e2`.
La carpeta independiente es ahora su checkout local. Git excluye cachés y conserva
los bytes originales para mantener válida la línea base de devolución del artista.

La copia arranca y reinicia siempre en Práctica. Conserva los cinco dummies, Irelia,
torres, tienda, runas, hechizos, cámara, animaciones, partículas, audio y HUD actuales.
Se ocultaron los controles de otros modos y rivales. Sus preferencias tienen un
espacio de nombres separado. Se conservaron dependencias compartidas para facilitar
la integración posterior.

En la copia entregada:

- `README.md`: apertura con Unity 6000.6.5f1 y acceso directo a Práctica.
- `ARTIST_GUIDE.md`: mapa de recursos y código visual, normalización de escalas,
  cámara, animaciones, partículas y persistencia de cambios fuera de Play.
- `INTEGRATION.md`: procedimiento para incorporar la devolución al proyecto completo.
- `CharacterAssets`: fuentes originales disponibles y copias convertidas.
- `References`: capturas aportadas por el usuario.
- `Handoff/Baseline.json`: hashes del estado original de trabajo y de la entrega,
  basado en commit `499b3f8ea098b165c7af901b1b36fc1a4748d329` más cambios locales.
- `Handoff/Validation`: seis pruebas PlayMode aprobadas, siete comprobaciones del
  exportador y captura del estado visual inicial.
- `Tools/Export-VisualChanges.py`: reúne cambios artísticos y sus `.meta`, propone
  borrados y marca scripts que necesitan fusionarse; nunca aplica cambios al original.

Los 800 archivos `.meta` existentes de Assets conservaron sus GUID. La integración
debe preservar esos identificadores y revisar cambios de scripts de presentación;
no debe copiar sobre el original los adaptadores que fuerzan Práctica ni la identidad
de producto de esta edición. La guía distingue los recursos activos de sus fuentes.

Preparación reproducible desde este proyecto:

```powershell
python Tools/Prepare-PracticeVisualProject.py TestResults/NuevaCopiaVisual
```

El destino debe ser nuevo. Después se valida en Unity, se finaliza **una sola vez**
su línea base y se publica con `Tools/Package-PracticeVisualProject.py`. Las plantillas
están en `Tools/PracticeVisualTemplate`; no forman parte de los scripts de Unity del
simulador completo. No reinicializar la línea base de una entrega ya editada.
