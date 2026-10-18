# Movimiento con springs en Feel

Un spring de Feel lleva un valor (float, Vector2/3/4 o Color) hacia un objetivo con una oscilación amortiguada en vez de una curva de duración fija. Haces **MoveTo** (nuevo valor) o **Bump** (empujón); **Damping** y **Frequency** lo asientan de forma natural. Los bumps repetidos se acumulan sin pelearse y el movimiento no depende del framerate.

## Contenido
- Prerrequisitos · Quick start · Elegir componente · Desde código · Desde un MMF_Player · Con eventos · Clamp · Spring propio · API cruda · Verificación · Problemas · Límites

## Prerrequisitos

- Instalación de Feel verificada. Para springs: `System.Type.GetType("MoreMountains.Feedbacks.MMSpringScale, MoreMountains.Tools")` no es null.
- Springs de Volume, TextMesh Pro y UI necesitan sus paquetes (defines `MM_URP`, `MM_HDRP`, `MM_POSTPROCESSING`, `MM_UGUI2`, `MM_UI`). La lista completa de componentes está en la referencia oficial `spring-components.md` (no incluida aquí): búscala en `Assets/Feel/Skills/feel-springs/references/`.
- Código de springs: `using MoreMountains.Feedbacks;` (las clases de springs y `MMChannelData` viven en ese namespace).

## Quick start: squash and stretch al pulsar un botón

1. Selecciona el objeto (ej. un Cube) y añade Add Component > More Mountains > Springs > **MM Spring Squash And Stretch** (`MMSpringSquashAndStretch`). Deja **Target** vacío: usa el Transform del propio objeto.
2. Crea un UI Button. En su **On Click ()** pulsa **+**, arrastra el cubo y elige `MMSpringSquashAndStretch > BumpRandom ()`.
3. Por editor script:

```csharp
using MoreMountains.Feedbacks;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class FeelSpringQuickStart
{
    public static void AddSquashOnClick(GameObject target, Button button)
    {
        MMSpringSquashAndStretch spring = Undo.AddComponent<MMSpringSquashAndStretch>(target);
        Undo.RecordObject(button, "Bind spring");
        UnityEventTools.AddPersistentListener(button.onClick, spring.BumpRandom);
        EditorUtility.SetDirty(button);
        EditorSceneManager.MarkSceneDirty(target.scene);
    }
}
```

4. En Play mode pulsa varias veces y ajusta en vivo **Spring Settings > Damping** (0.4 por defecto; 0 = suelto, 1 = rígido), **Frequency** (6 oscilaciones/s) y **Randomness > Bump Amount Random Value** (20 a 100).

**Resultado**: el cubo tiene `MMSpringSquashAndStretch`; el `onClick` tiene un listener persistente a `BumpRandom`; cada clic aplasta y estira el cubo y los clics rápidos se acumulan.

## Elegir y añadir el componente correcto

Todos bajo Add Component > More Mountains > Springs:
- **Transform**: MM Spring Position / Rotation / Scale (Vector3), Squash And Stretch / Rotation Around (Float).
- **Cámara**: MM Spring Camera Field Of View, Camera Orthographic Size (Float).
- **Luz**: MM Spring Light Intensity, Light Range (Float), Light Color (Color).
- **UI**: MM Spring Image Alpha, Image Fill Amount (Float), Image Color (Color), Rect Transform Position (Vector3), Rect Transform Size Delta (Vector2).
- **Audio**: MM Spring Audio Source Volume, Audio Source Pitch (Float).
- **Volumes**: MM Spring Vignette Intensity URP, Bloom Intensity URP, y equivalentes HDRP / Post Processing v2.

Añádelo al GameObject que tiene el componente objetivo o asigna **Target** explícitamente. Secciones del inspector: **Target**, **Channel and TimeScale** (Channel 0, `TimeScaleMode` Scaled), **Spring Settings**, **Randomness**, **Test** (botones MoveTo/Bump en Play mode). En springs vectoriales el spring es unificado por defecto; marca **Separate Axis** para ajustar y hacer clamp por eje.

**Resultado**: en Play mode el botón Bump de **Test** mueve visiblemente la propiedad y se asienta.

## Mover un spring desde código

```csharp
using MoreMountains.Feedbacks;
using UnityEngine;

public class LightPulse : MonoBehaviour
{
    public MMSpringLightIntensity Spring;
    public void OnShoot()  { Spring.Bump(25f); }   // empuja el valor; vuelve al objetivo
    public void PowerUp()  { Spring.MoveTo(3f); }  // nuevo valor de reposo
}
```

Comandos en todos los componentes: `MoveTo(v)`, `MoveToAdditive(v)`, `MoveToSubtractive(v)`, `MoveToRandom()`, `MoveToRandom(min, max)` (solo float), `MoveToInstant(v)`, `Bump(amount)`, `BumpRandom()`, `BumpRandom(min, max)` (solo float), `Stop()`, `Finish()`, `RestoreInitialValue()`, `ResetInitialValue()`.

El bump interactúa con los ajustes: un spring suelto (damping bajo) viaja lejos con un bump pequeño; uno rígido necesita bumps grandes. Parte de los valores de prueba (`TestBumpAmount = 75`, `TestMoveToValue = 2` en springs float) y ajusta.

## Desde un MMF_Player

1. Anota el tipo de valor del spring (arriba de su sección **Spring Settings**).
2. En un MMF Player añade el feedback del mismo tipo: Springs > Spring Float (`MMF_SpringFloat`), Spring Vector2, Vector3, Vector4 o Color.
3. Arrastra el spring a **Target Spring**, o déjalo vacío y usa el mismo **Channel** para alcanzar todos los springs de ese tipo en el canal.
4. Elige el **Command** (Bump por defecto) y su valor (`BumpAmount = 75`, `MoveToValue = 2` en Spring Float). Opcional: `OverrideDamping` / `OverrideFrequency` para ese comando.
5. Ejemplo de la documentación: `MMSpringCameraFieldOfView` en la cámara principal y un Spring Float con `BumpAmount = 1000` apuntándole = patada de FOV fuerte.

Feedbacks de transform con spring incluidos que no necesitan componente: Transform > **Position Spring** (`MMF_PositionSpring`), **Scale Spring** (`MMF_ScaleSpring`), **Squash and Stretch Spring** (`MMF_SquashAndStretchSpring`). Todos con `Mode = Bump`, damping 0.4 / frequency 6.

## Controlar springs con eventos (sin referencia)

```csharp
using MoreMountains.Feedbacks;

// todos los springs float escuchando en el canal 3 se mueven a 2
MMChannelData channel = new MMChannelData(MMChannelModes.Int, 3, null);
MMSpringFloatEvent.Trigger(SpringCommands.MoveTo, null, channel, moveToValue: 2f);
// bump de 50
MMSpringFloatEvent.Trigger(SpringCommands.Bump, null, channel, bumpAmount: 50f);
```
Pon primero el **Channel** de los springs en 3. Vector3 y Color tienen eventos equivalentes (`MMSpringVector3Event`, `MMSpringColorEvent`, …).

## Clamp

En un spring float, o en cada eje de uno vectorial con **Separate Axis**, abre los ajustes de clamp (`MMSpringClampSettings`):
- `ClampMin` / `ClampMax` con `ClampMinValue` / `ClampMaxValue` (0 y 10 por defecto), o `ClampMinInitial` / `ClampMaxInitial` para usar el valor inicial como límite.
- `ClampMinBounce` / `ClampMaxBounce` para que rebote contra el límite en vez de detenerse.

**Resultado**: el valor nunca cruza el clamp; con bounce, rebota.

## Spring propio para cualquier propiedad

Crea el script fuera de la carpeta Feel:

```csharp
using MoreMountains.Feedbacks;
using UnityEngine;

public class Rigidbody2DMassSpring : MMSpringFloatComponent<Rigidbody2D>
{
    public override float TargetFloat
    {
        get => Target.mass;
        set => Target.mass = value;
    }
}
```
Para otros tipos hereda de `MMSpringVector2Component<T>` (override `TargetVector2`), `MMSpringVector3Component<T>` (`TargetVector3`), `MMSpringVector4Component<T>` o `MMSpringColorComponent<T>`. Añádelo a un objeto que tenga el componente objetivo: obtiene inspector completo, comandos, canales y soporte de eventos automáticamente.

## API cruda dentro de tus scripts

```csharp
using MoreMountains.Feedbacks;
using UnityEngine;

public class FollowMouseSpring : MonoBehaviour
{
    public MMSpringVector2 Spring = new MMSpringVector2(); // muestra damping/frequency/clamp en el inspector
    private RectTransform _rectTransform;
    private Vector2 _localPoint;

    private void Start() { _rectTransform = GetComponent<RectTransform>(); }

    private void Update()
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rectTransform.parent as RectTransform, Input.mousePosition, null, out _localPoint);
        Spring.MoveTo(_localPoint);
        Spring.UpdateSpringValue(Time.deltaTime); // los springs crudos los avanzas tú
        _rectTransform.anchoredPosition = Spring.CurrentValue;
    }
}
```
Tipos: `MMSpringFloat`, `MMSpringVector2`, `MMSpringVector3`, `MMSpringVector4`, `MMSpringColor`. (El ejemplo lee `Input.mousePosition`: requiere el Input Manager antiguo o **Active Input Handling = Both**.)

## Verificación

- El objeto tiene el componente (`target.GetComponent<MMSpringSquashAndStretch>() != null`, o el tipo elegido).
- Ajustes: `spring.FloatSpring.Damping / .Frequency` (float), `spring.SpringVector3` (Vector3) o `spring.ColorSpring` (color) tienen los valores deseados.
- UnityEvent: `button.onClick.GetPersistentEventCount() > 0` y `GetPersistentMethodName(0) == "BumpRandom"` (o el comando elegido).
- Feedback: el MMF Player tiene `MMF_SpringFloat` (o su tipo) con `TargetSpring` asignado o con canal igual al del spring.
- En Play mode, tras un Bump, la propiedad (ej. `transform.localScale`) difiere del valor inicial y luego se asienta (un par de segundos con ajustes por defecto). Console sin errores.

## Problemas comunes

- **Bump no hace nada** → el bump es diminuto para un spring rígido, o no hay target → súbelo (prueba 75 o más) y revisa **Target** o que el componente objetivo exista en el mismo objeto.
- **Spring Float feedback no hace nada** → tipo de valor distinto (ej. spring Vector3 con Spring Float) o canal distinto → usa el feedback del tipo del spring y asigna **Target Spring** o alinea canales.
- **El valor deriva a otro reposo tras MoveTo** → así funciona MoveTo (cambia el objetivo) → usa Bump para volver al reposo, o `RestoreInitialValue()`.
- **El objeto se hunde bajo el piso al rebotar** → sin clamp → `ClampMin` con `ClampMinInitial` (y `ClampMinBounce` para rebote).
- **Ignora el slow motion (o debería ignorarlo)** → `TimeScaleMode` en Scaled o Unscaled según corresponda.
- **El spring crudo no se mueve** → no llamas `UpdateSpringValue(deltaTime)` cada frame → hazlo en `Update`.
- **Errores de compilación o spring de Volume ausente** → falta el paquete del render pipeline → usa la variante del pipeline del proyecto.

## Límites

- Los springs no tienen duración fija: para efectos que deben terminar en un instante exacto usa feedbacks de curva (feedbacks.md), o pon `DeclaredDuration` en feedbacks de spring solo para informar la duración total del player.
- Un componente de spring controla una propiedad; varios springs sobre la misma propiedad pelean. Combina movimientos con un solo spring (los bumps suman) o anida objetos.
- Los springs animan en Play mode; no son una herramienta de animación en edit mode.
- No es simulación física (masa, colisiones): es interpolación de oscilador amortiguado para game feel.
