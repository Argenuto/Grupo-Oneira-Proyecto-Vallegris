# Screen shake y efectos de cámara con Feel

La mayoría de feedbacks de cámara **no mueven la cámara**: emiten un evento y un **shaker** en la cámara (o un listener de Cinemachine) lo recibe y hace el trabajo. Esta referencia cubre elegir la ruta correcta, añadir las dos mitades (feedback + receptor), disparar desde código y dar un toggle para apagar los shakes.

## Contenido
- Prerrequisitos · Elegir ruta · Cinemachine Impulse (quick start) · Rig MMCameraShaker (sin Cinemachine) · Camera Shake en cámara Cinemachine · UI shake · Zoom / Fade / Flash · Shake desde código · Canales · Toggle de accesibilidad · Verificación · Problemas · Límites

## Prerrequisitos

- Instalación de Feel verificada (ver reglas generales de SKILL.md).
- **Rutas con Cinemachine** necesitan `com.unity.cinemachine` (3.x recomendado; 2.x soportado). Comprueba que `System.Type.GetType("MoreMountains.FeedbacksForThirdParty.MMF_CinemachineImpulse, MoreMountains.Tools")` resuelva; si no, falta Cinemachine. Instalar paquetes cambia el proyecto: pregunta antes (o `Tools > More Mountains > MMFeedbacks > Install All Dependencies`).
- **Fade y Flash** necesitan uGUI (`com.unity.ugui`), presente por defecto.
- La configuración automática usa `Camera.main`: la cámara de la escena debe tener el tag **MainCamera**.

## Elegir la ruta

1. Busca `Unity.Cinemachine.CinemachineBrain` en `Camera.main` y objetos `CinemachineCamera` en la escena.
2. Decide:
   - Hay Cinemachine → **Cinemachine Impulse** para shakes de sensación física, o **Camera Shake sobre cámara Cinemachine** si el usuario quiere los controles simples de duración/amplitud/frecuencia de Feel.
   - No hay Cinemachine → **Camera Shake con rig MMCameraShaker**.
   - Solo el HUD debe temblar → **UI shake**.
3. Dile al usuario qué ruta elegiste y por qué, antes de cambiar nada.

## Cinemachine Impulse (ruta recomendada con Cinemachine)

1. Asegura una cámara con tag MainCamera.
2. GameObject vacío `ShakeFeedback` + **MMF Player** + **Add new feedback > Camera > Cinemachine Impulse**. Al añadirlo ya trae una raw signal (perfil `MM_6D_Shake`) y `Velocity = (5, 5, 5)`.
3. Expande **Automatic Setup** y pulsa **Automatic Shaker Setup** (fuera de Play mode): añade un `CinemachineBrain` a la cámara principal si hace falta, crea una `CinemachineCamera` si no existe y añade un `CinemachineImpulseListener`. Por editor script:

```csharp
using MoreMountains.Feedbacks;
using MoreMountains.FeedbacksForThirdParty;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FeelShakeQuickStart
{
    public static void CreateImpulseShake()
    {
        GameObject go = new GameObject("ShakeFeedback");
        Undo.RegisterCreatedObjectUndo(go, "Create ShakeFeedback");
        MMF_Player player = go.AddComponent<MMF_Player>();
        Undo.RecordObject(player, "Add Cinemachine Impulse");
        player.AddFeedback(typeof(MMF_CinemachineImpulse));
        player.AutomaticShakerSetup(); // ejecuta el setup automático de cada feedback
        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(go.scene);
    }
}
```

4. Llama `ShakeFeedback.PlayFeedbacks()` desde gameplay (o el botón Play en Play mode).

**Resultado**: la escena tiene una `CinemachineCamera` con `CinemachineImpulseListener`, la cámara principal un `CinemachineBrain`, y el feedback sacude la vista. La Console muestra "... automatic shaker setup : ... You're all set.".

## Camera Shake con rig MMCameraShaker (sin Cinemachine)

1. MMF Player + **Camera > Camera Shake** (`MMF_CameraShake`). Default `CameraShakeProperties = (Duration 0.1, Amplitude 0.2, Frequency 40)`.
2. Automático: **Automatic Shaker Setup** en el feedback (o `player.AutomaticShakerSetup()`). Sin cámara Cinemachine crea `CameraRig > CameraShaker` en la posición de la cámara principal, añade `MMCameraShaker` (que trae un `MMWiggle` requerido), pone el wiggle en modo posición con `WigglePermitted = false` y `WiggleType = Noise`, y mete la cámara principal como hija.
3. Manual: `CameraRig` (vacío) > `CameraShaker` (vacío, hijo) > tu cámara (hija). En `CameraShaker` añade More Mountains > Feedbacks > Shakers > Camera > MM Camera Shaker; en su `MMWiggle` marca **Position**, **Wiggle Type = Noise** y desmarca **Wiggle Permitted**.
4. Por qué el rig: el shaker desplaza su propio transform, así la cámara conserva la posición que tu controlador escriba en `CameraRig`. Si tu controlador mueve el objeto cámara directamente, mueve esa lógica a `CameraRig`.
5. Para golpes más fuertes sube Amplitude y Duration (ej. Duration 0.3, Amplitude 2, Frequency 40).

**Resultado**: jerarquía `CameraRig/CameraShaker/<Main Camera>`, `CameraShaker` con `MMCameraShaker` y `MMWiggle`; el feedback hace vibrar la vista el tiempo configurado.

## Camera Shake sobre una cámara Cinemachine

1. En la `CinemachineCamera` añade **CinemachineBasicMultiChannelPerlin**, pon su **Noise Profile** en un perfil 6D (Feel trae `Assets/Feel/MMFeedbacks/MMFeedbacksForThirdParty/Cinemachine/Resources/MM_6D_Shake.asset`) y **Amplitude Gain** y **Frequency Gain** en 0 para que la cámara quede quieta.
2. Añade More Mountains > Feedbacks > Shakers > Cinemachine > **MM Cinemachine Camera Shaker** (`MMCinemachineCameraShaker`) al mismo objeto.
3. Añade **Camera > Camera Shake** a un MMF Player; buen punto de partida: Duration 0.3, Amplitude 2, Frequency 40.

**Resultado**: el feedback mueve las ganancias del Perlin y la cámara tiembla y vuelve a 0.

## UI shake

1. Bajo el Canvas crea `UIContainer` (vacío) y mete dentro los elementos que deben temblar.
2. En `UIContainer` añade `MMWiggle` (marca **Position**, desmarca **Wiggle Permitted**) y `MMCameraShaker`.
3. **Camera > Camera Shake** en un MMF Player. La UI se mueve en píxeles: usa amplitudes grandes; el ejemplo documentado usa Duration 0.5, Amplitude 20, Frequency 0.03.
4. Si también hay un shaker de cámara real, dale al shaker de UI y a su feedback su propio **Channel** para que no reaccionen los dos.

## Zoom punch, fade y flash

1. **Zoom**: **Camera > Camera Zoom** (`MMF_CameraZoom`). Defaults: `ZoomMode = For`, `ZoomFieldOfView = 30`, `ZoomTransitionDuration = 0.05`, `ZoomDuration = 0.1`. Receptor: `MMCameraZoom` en cámara normal (More Mountains > Feedbacks > Shakers > Camera > MM Camera Zoom) o `MMCinemachineZoom` en cámara Cinemachine. **Automatic Shaker Setup** añade el correcto.
2. **Fade**: **Camera > Fade** (`MMF_Fade`). `FadeType`: FadeIn (a opaco), FadeOut (a transparente) o Custom; `Duration = 1`. El setup automático crea `FadeCanvas` (screen-space overlay) con una `FadeImage` negra a pantalla completa y un `MMFader`. Manual: una Image negra a pantalla completa con `MMFader` (añade un CanvasGroup; pon su alpha en 0).
3. **Flash**: **Camera > Flash** (`MMF_Flash`, `FlashColor` blanco, `FlashDuration = 0.2`, `FlashAlpha = 1`). El setup automático crea `FlashCanvas` con una `FlashImage` y un `MMFlash`.
4. Fade y flash casan receptor por ID: `MMF_Fade.ID` ↔ ID del fader, `MMF_Flash.FlashID` ↔ ID del flash (ambos 0 por defecto).

## Shake desde código sin player

```csharp
using MoreMountains.Feedbacks;

// procesado por MMCameraShaker y MMCinemachineCameraShaker escuchando en el canal 0
MMCameraShakeEvent.Trigger(0.3f, 2f, 40f, 0f, 0f, 0f, false,
    new MMChannelData(MMChannelModes.Int, 0, null));

// detener shakes en ese canal (solo MMCinemachineCameraShaker escucha el stop)
MMCameraShakeStopEvent.Trigger(new MMChannelData(MMChannelModes.Int, 0, null));
```
Firma: `Trigger(float duration, float amplitude, float frequency, float amplitudeX, float amplitudeY, float amplitudeZ, bool infinite = false, MMChannelData channelData = null, bool useUnscaledTime = false)`.

## Canales para apuntar a una cámara

1. En el shaker: `ChannelMode` (Int por defecto) y `Channel`, o modo MMChannel con un asset (Assets > Create > MoreMountains > MMChannel).
2. En el feedback, bajo **Feedback Settings**: el mismo `ChannelMode` y `Channel` / `MMChannelDefinition`.

Solo reaccionan los shakers cuyo canal coincide (útil en pantalla dividida o UI vs mundo).

## Toggle para que el jugador apague el shake

```csharp
using MoreMountains.Feedbacks;
using MoreMountains.FeedbacksForThirdParty;

public static class ShakeSettings
{
    public static void SetScreenShake(bool enabled)
    {
        MMF_CameraShake.FeedbackTypeAuthorized = enabled;
        MMF_CinemachineImpulse.FeedbackTypeAuthorized = enabled;
        MMF_CameraZoom.FeedbackTypeAuthorized = enabled;
    }
}
```
Conecta `SetScreenShake` a la UI de ajustes (ej. `onValueChanged` de un Toggle). Alternativa sin código: componente `MMFeedbacksAuthorizations` en la escena y desmarcar los feedbacks de cámara. Con el ajuste apagado los feedbacks de cámara no hacen nada; el resto sigue funcionando.

## Verificación

- Ruta Cinemachine: `Camera.main.GetComponent<Unity.Cinemachine.CinemachineBrain>() != null` y al menos una `CinemachineCamera` con `CinemachineImpulseListener`.
- Ruta rig: `Object.FindAnyObjectByType<MMCameraShaker>() != null`, su GameObject tiene `MMWiggle` con `PositionActive == true` y `Camera.main.transform.parent` es ese GameObject.
- Camera shake en Cinemachine: la `CinemachineCamera` tiene `CinemachineBasicMultiChannelPerlin` y `MMCinemachineCameraShaker`.
- Zoom: existe `MMCameraZoom` o `MMCinemachineZoom`. Fade/flash: `FindAnyObjectByType<MMFader>()` / `<MMFlash>()` no es null.
- El player contiene el feedback esperado y los canales de feedback y shaker coinciden.
- En Play mode la posición de la cámara (o el local position de `CameraShaker`) cambia mientras suena el feedback y vuelve al reposo. Console sin errores.

## Problemas comunes

- **El feedback suena pero nada tiembla** → no hay receptor: el impulse necesita `CinemachineImpulseListener` en la cámara Cinemachine activa; Camera Shake necesita `MMCameraShaker` o `MMCinemachineCameraShaker` → corre **Automatic Shaker Setup** o añade el receptor a mano.
- **Camera Shake no hace nada aunque hay cámara Cinemachine** → cuando ya existe una cámara Cinemachine, el setup automático de Camera Shake solo asegura brain e impulse listener; no añade `MMCinemachineCameraShaker` → sigue "Camera Shake sobre cámara Cinemachine" o usa el feedback Cinemachine Impulse.
- **`MMCinemachineCameraShaker` no hace nada** → la cámara no tiene `CinemachineBasicMultiChannelPerlin` (el shaker se salta el shake en silencio) → añádelo con un noise profile y ganancias en 0.
- **El setup automático lanza NullReferenceException** → no hay cámara con tag MainCamera → etiquétala y reintenta.
- **Tiembla una vez y el controlador lo devuelve de golpe / se ignora** → un script escribe el transform de la cámara cada frame → mueve el controlador a `CameraRig` y deja el shaker en el hijo.
- **Solo una cámara debería temblar pero tiemblan todas** → todos los shakers están en el canal 0 → canales distintos en shakers y feedbacks.
- **El impulso es muy fuerte o débil** → `Velocity` escala el impulso (5,5,5 por defecto) y la intensidad del player (`PlayFeedbacks(position, intensity)`) también lo multiplica.
- **El shake ignora el slow motion (o debería ignorarlo)** → **Timing > Timescale Mode** del feedback en Unscaled o Scaled según convenga.

## Límites

- Feel no tiene un "view shake" que mueva la imagen renderizada: para sacudir todo el frame, renderiza a una RenderTexture mostrada en un elemento UI y sacude ese elemento (UI shake).
- El comportamiento del impulso (formas de señal, propagación, disipación) es de Cinemachine; Feel solo lo dispara. El ajuste fino va en la documentación de Cinemachine.
- Automatic Shaker Setup es un cambio de escena "mejor esfuerzo": puede re-parentar la cámara o crear cámaras y canvas. Guarda la escena y ten control de versiones primero.
- Los pulsos de post-processing (distorsión de lente, aberración cromática) no están en esta skill (ver nota de "Qué NO cubre" en SKILL.md).
