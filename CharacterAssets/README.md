# Biblioteca de personajes

Copias locales de las fuentes disponibles y de los recursos convertidos del simulador:

- [Irelia, skin base](Irelia/Base/README.md).
- [Dummy de entrenamiento, modelo base](TrainingDummy/Base/README.md).

Esta biblioteca está dentro del proyecto, **fuera de `Assets`**, para conservar los
archivos originales y sus `.meta` sin que Unity importe una segunda copia ni registre
GUID duplicados. Los recursos activos siguen en `Assets/Resources` y
`Assets/SourceModels`; las carpetas `Unity` de esta biblioteca son copias de consulta.
No arrastrar esas copias encima de los recursos activos sin revisar los GUID.

Cada `manifest.json` registra ruta, tamaño, SHA-256 y procedencia de cada archivo.
La biblioteca contiene lo encontrado en las fuentes locales; no certifica que la
extracción original incluya todos los archivos o dependencias de una versión de LoL.
Las fuentes no identifican un parche único: no se presupone que todas correspondan
a la misma versión del juego.

Desde la raíz del proyecto, verificar sin necesitar las carpetas externas:

```powershell
python Tools/Collect-BaseCharacterAssets.py --verify
```

Para actualizar las copias desde sus fuentes locales y desde los recursos activos:

```powershell
python Tools/Collect-BaseCharacterAssets.py --irelia-source "C:\Users\rotte\Downloads\LOL Data\ireliaLOL" --dummy-source "C:\Users\rotte\Downloads"
```

El recolector actualiza los archivos incluidos sin modificar los originales ni los
recursos activos. La verificación detecta archivos faltantes, modificados o ajenos
al inventario. Git conserva estos archivos sin normalizar sus finales de línea.
