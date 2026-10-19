# Integración posterior al simulador completo

`Handoff/Baseline.json` identifica el commit de referencia y los hashes de la copia
de trabajo original. Incluye los cambios locales existentes al preparar la entrega;
no se limita al último commit. Después de importar y validar, se guardan también los
hashes de la edición entregada al profesional. Los `.meta` existentes conservan GUID.

El exportador compara el resultado del profesional contra **la entrega**, por lo
que las restricciones de Práctica añadidas al preparar esta edición no aparecen
como cambios artísticos si no se tocaron después.

1. Revisar `VisualChangeReport.json` y las notas del profesional.
2. Crear una rama en el proyecto completo y conservar una referencia de su estado.
3. Comparar cada archivo con `original_sha256`. Si el proyecto completo avanzó desde
   esa versión, resolver las diferencias antes de incorporar la devolución.
4. Incorporar recursos, configuraciones, animaciones, efectos, shaders y prefabs con
   sus `.meta`. Las rutas dentro de `Files/` son relativas a la raíz del proyecto.
5. Fusionar manualmente los scripts de presentación y las entradas marcadas
   `merge_required`. Las propuestas de borrado se revisan: el ZIP no borra archivos.
6. Probar Práctica, PvP y Normalplay en ambos equipos, movimiento, selección, cámara,
   Q/W/E/R, marcas, HUD, animaciones, partículas y referencias de materiales.

**No reemplazar el proyecto completo por esta copia ni importar en bloque sus
ProjectSettings y adaptadores de distribución.** Sus diferencias específicas son:

- `LaneWorld.PracticeVisualOnly.cs`: obliga Práctica al iniciar y reiniciar.
- `LaneWorld.cs` y `LaneWorld.Practice.cs`: llaman al adaptador e impiden otros modos.
- `PracticeHud.cs`: oculta selección de rival y de modo/dificultad.
- Claves `MidLane.PracticeVisual.*`: preferencias aisladas para esta edición.
- `SandboxSettings.asset`: Práctica activada, PvP y oleadas desactivados.
- `ProjectSetup.cs` y `ProjectSettings/ProjectSettings.asset`: nombre/producto propios.
- `PracticeVisualWorkshop.cs` y las pruebas de entrega: solo herramientas de esta copia.

El código y los recursos comunes permanecen disponibles, aunque no se habiliten
otros modos. Esto evita amputar dependencias y permite devolver mejoras visuales
compartidas. Se conservaron también las fuentes de animación, partículas y audio
sin exigir acceso a las carpetas externas originales.
