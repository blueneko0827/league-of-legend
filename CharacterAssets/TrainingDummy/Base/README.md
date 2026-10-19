# Dummy de entrenamiento — modelo base

`Original` contiene los dos GLB encontrados en Downloads:

- `target_dummyIdle.glb`: modelo texturizado con animación de reposo.
- `target_dummyHit.glb`: modelo con animación de recibir golpes.

Se verificó que coinciden byte por byte con los GLB usados actualmente por el
simulador. Los dummies fijos, evasivo, móvil y de Q reutilizan este modelo base;
su comportamiento se define en el código del juego.

`Unity/Assets/SourceModels/TrainingDummy` conserva una copia de las fuentes del
proyecto. `Unity/Assets/Resources/Models/TrainingDummy` incluye el prefab, clips
`Idle` y `Hit`, material, textura y malla, con sus `.meta`.

El prefab activo sigue en
[`Assets/Resources/Models/TrainingDummy/Dummy.prefab`](../../../Assets/Resources/Models/TrainingDummy/Dummy.prefab).
El importador está en
[`Assets/Editor/IreliaGlbBake.cs`](../../../Assets/Editor/IreliaGlbBake.cs).
La carpeta `Unity` es una copia de consulta, dependiente del proyecto y URP.

No se encontró una carpeta fuente de Riot adicional para el dummy ni bancos de
sonidos o partículas propios. Por ello, este paquete conserva el modelo y las dos
animaciones disponibles, sin atribuirle recursos que no se localizaron.

**Inventario:** 20 archivos, 2.122.743 bytes, sin contar este README y el propio
`manifest.json`. Cada archivo está verificado con SHA-256.
