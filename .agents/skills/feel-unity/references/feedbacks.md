# MMF_Player y feedbacks

`MMF_Player` (namespace `MoreMountains.Feedbacks`) guarda una lista ordenada de feedbacks (`MMF_Scale`, `MMF_Position`, `MMF_Particles`, `MMF_MMSoundManagerSound`…) y los reproduce. No es un motor de tweens general: para una interpolación suelta usa `MMTween` (`MoreMountains.Tools`, ej. `MMTween.MoveTransform(...)`) o las herramientas de animación de Unity.

## Contenido
- Quick start · Player multi-feedback · Disparar · Secuenciar · Cambiar valores en runtime · Callback al terminar · Canales · Crear en runtime · Desactivar feedbacks · Verificación · Problemas comunes · Límites

## Quick start: un cubo que hace "pop"

1. Selecciona/crea el objeto (ej. un Cube).
2. Crea un GameObject vacío `PopFeedback` y añade **MMF Player** (Add Component > More Mountains > Feedbacks > MMF Player).
3. En el inspector: **Add new feedback** > Transform > Scale.
4. En el feedback arrastra el cubo a **Animate Scale Target**. Defaults: `AnimateScaleDuration = 0.2`, `RemapCurveZero = 1`, `RemapCurveOne = 2`, con curvas que suben y vuelven a 0 (el cubo crece y regresa).
5. Equivalente por editor script (lo mismo que hace el inspector: registrar undo y luego `AddFeedback(type)`):

```csharp
using MoreMountains.Feedbacks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FeelQuickStart
{
    public static MMF_Player CreatePopFeedback(Transform target)
    {
        GameObject go = new GameObject("PopFeedback");
        Undo.RegisterCreatedObjectUndo(go, "Create PopFeedback");
        MMF_Player player = go.AddComponent<MMF_Player>();
        Undo.RecordObject(player, "Add Scale feedback");
        MMF_Scale scale = (MMF_Scale)player.AddFeedback(typeof(MMF_Scale));
        scale.Label = "Pop";
        scale.AnimateScaleTarget = target;
        scale.AnimateScaleDuration = 0.2f;
        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(go.scene);
        return player;
    }
}
```

6. Dispáralo desde gameplay:

```csharp
using MoreMountains.Feedbacks;
using UnityEngine;

public class Pickup : MonoBehaviour
{
    public MMF_Player PopFeedback; // asignar en el inspector
    public void Collect() { PopFeedback?.PlayFeedbacks(); }
}
```

**Resultado esperado**: el `MMF_Player` tiene un `MMF_Scale` apuntando al cubo; en Play mode el botón verde **Play** del inspector (o `PlayFeedbacks()`) escala el cubo y lo devuelve en 0.2 s.

## Player multi-feedback (ej. reacción a golpe)

1. `MMF_Player` en un GameObject vacío, normalmente hijo del objeto que reacciona, nombrado por el evento (`HitFeedback`).
2. Añade feedbacks con **Add new feedback**. Opciones comunes:
   - Particles > Particles Play (`MMF_Particles`): asigna **Bound Particle System**. Modo por defecto: Play.
   - Particles > Particles Instantiation (`MMF_ParticlesInstantiation`): instancia un prefab de partículas.
   - Audio > MMSoundManager Sound (`MMF_MMSoundManagerSound`): asigna **Sfx** (ver sound-manager.md).
   - Renderer > Flicker (`MMF_Flicker`): asigna **Bound Renderer**; defaults `FlickerDuration = 0.2`, `FlickerPeriod = 0.04`, `Mode = Color`.
   - Time > Freeze Frame (`MMF_FreezeFrame`): `FreezeFrameDuration = 0.1`. Cambia el time scale, así que la escena necesita un `MMTimeManager`; el botón **Automatic Setup** del feedback lo añade.
   - Transform > Position / Rotation / Scale / Squash and Stretch / Wiggle.
3. Expande cada feedback y arregla todo icono de advertencia: significa que falta un target requerido (`EvaluateRequiresSetup()` devolvió true); el cuadro de ayuda dice qué campo.
4. Renombra feedbacks con doble clic o con `Label` para encontrarlos luego por código.

**Resultado**: ningún feedback con advertencia; el botón Play reproduce todos a la vez.

## Disparar un player

- **Código**: `public MMF_Player X;` y luego
  - `X.PlayFeedbacks();`
  - `X.PlayFeedbacks(transform.position, 1f);` (posición para feedbacks que la usan, p. ej. instanciar partículas; e intensidad: 1 normal, 0.5 mitad)
  - `X.StopFeedbacks();` detiene secuencia y feedbacks activos; `StopFeedbacks(false)` detiene la secuencia pero deja terminar los que corren
  - `X.SkipToTheEnd();` (el player debe estar reproduciendo) y `X.RestoreInitialValues();`
- **UnityEvent** (OnClick de un botón, etc.): añade una entrada, arrastra el objeto con el MMF Player y elige `MMF_Player > PlayFeedbacks ()`.
- **Animation Event**: pon el MMF Player en el mismo GameObject que el Animator, añade el evento en el frame deseado y elige la función `PlayFeedbacks()`.
- **Timeline**: Signal Emitter en la marker track, crea un Signal asset, añade un **Signal Receiver** al objeto del director y pon como reacción el `PlayFeedbacks()` del MMF Player.
- **Automático**: en Settings del player activa `AutoPlayOnStart` o `AutoPlayOnEnable`.

Para comprobarlo en código: `player.IsPlaying` es true justo después de `PlayFeedbacks()` mientras corren.

## Secuenciar: pausas, loops, modo secuencial

1. Por defecto todos arrancan a la vez (`PlayMode = Parallel`); cada feedback se puede retrasar con **Timing > Initial Delay**.
2. Orden estricto: `PlayMode = Sequential`; `SequentialDelay` (0 por defecto) añade un hueco tras cada feedback.
3. Pausas (categoría Pause):
   - **Pause > Pause** (`MMF_Pause`, `PauseDuration = 1`): lo de arriba arranca junto, lo de abajo arranca tras la pausa.
   - **Pause > Holding Pause** (`MMF_HoldingPause`): espera a que terminen todos los de arriba y luego su propia duración.
   - **Pausa dirigida por script**: en `MMF_Pause` activa `ScriptDriven`; la secuencia espera hasta que el código llame `player.ResumeFeedbacks()`. Opcional `AutoResume` y `AutoResumeAfter` (0.25 s).
4. Loops (categoría Loop): **Loop > Looper** (`MMF_Looper`) vuelve a la última pausa (`LoopAtLastPause`), al último **Looper Start** (`LoopAtLastLoopStart`) o al inicio. `NumberOfLoops = 2` por defecto; `InfiniteLoop` repite hasta que lo detengas.
5. Repeticiones por feedback: **Timing > Number Of Repeats**, **Repeat Forever**, **Delay Between Repeats** (1 s).
6. Reversa: `Direction = BottomToTop` en Settings, o `player.Direction = MMFeedbacks.Directions.BottomToTop;`, o `player.PlayFeedbacksInReverse()`. `ChangeDirection()` invierte.

**Resultado**: la duración total mostrada coincide con la línea de tiempo deseada y los feedbacks arrancan en el orden configurado.

## Cambiar valores en runtime

```csharp
using System.Collections.Generic;
using MoreMountains.Feedbacks;

MMF_Scale scale = player.GetFeedbackOfType<MMF_Scale>();          // primero que coincida
MMF_Scale named = player.GetFeedbackOfType<MMF_Scale>("Pop");     // por Label
List<MMF_Scale> all = player.GetFeedbacksOfType<MMF_Scale>();     // todos
scale.AnimateScaleDuration = 0.5f;
player.ComputeCachedTotalDuration(); // si cambiaste tiempos/duraciones
```

- Muchos feedbacks cachean datos al inicializar: si un cambio no surte efecto, llama `player.Initialization();` antes de `PlayFeedbacks()`.
- Para variar la fuerza sin editar campos: `player.PlayFeedbacks(position, 0.5f)` o `player.FeedbacksIntensity`.
- Para reproducir un solo feedback: `scale.Play(transform.position, 1f);`.

## Ejecutar código cuando el player termina

1. Inspector: en **Events** deja `TriggerUnityEvents` activo (true por defecto) y enlaza un método a **OnComplete**.
2. Código con UnityEvents:
   ```csharp
   private void OnEnable()  { MyPlayer.Events.OnComplete.AddListener(OnPlayerComplete); }
   private void OnDisable() { MyPlayer.Events.OnComplete.RemoveListener(OnPlayerComplete); }
   ```
3. Eventos globales: `Events.TriggerMMFeedbacksEvents = true` (false por defecto), registra `MMFeedbacksEvent.Register(OnMMFeedbacksEvent)` con firma `void OnMMFeedbacksEvent(MMFeedbacks source, MMFeedbacksEvent.EventTypes type)` y reacciona a `EventTypes.Complete`. Desregistra en `OnDisable`.
4. Async: `await FirstPlayer.PlayFeedbacksTask(transform.position);` y continúa.

El callback se dispara una vez por reproducción completada, cuando todos los feedbacks terminaron.

## Controlar players sin referencia directa (canales)

1. En cada player: **Settings > Play Settings**, `MMF_ChannelMode` (Int por defecto) y `MMF_Channel` (0), o modo MMChannel con un asset `MMF_ChannelDefinition` (Assets > Create > MoreMountains > MMChannel).
2. Emitir:
   ```csharp
   using MoreMountains.Feedbacks;
   MMChannelData channel = new MMChannelData(MMChannelModes.Int, 12, null);
   MMF_PlayerEvent.Trigger(channel, true, transform.position, MMF_PlayerEvent.Modes.PlayFeedbacks);
   ```
   Otros modos: `StopFeedbacks`, `PauseFeedbacks`, `ResumeFeedbacks`, `ChangeDirection`, `RestoreInitialValues`, `SkipToTheEnd`. Los players solo escuchan en Play mode (se registran en `Awake` si `Application.isPlaying`).

## Crear un player enteramente en runtime

```csharp
using MoreMountains.Feedbacks;
using UnityEngine;

public class DynamicPlayerCreator : MonoBehaviour
{
    public void Create()
    {
        MMF_Player newPlayer = gameObject.AddComponent<MMF_Player>();
        // inicializar a mano; si no, la inicialización de Start correría después de nuestro Play
        newPlayer.InitializationMode = MMFeedbacks.InitializationModes.Script;
        for (int i = 0; i < transform.childCount; i++)
        {
            MMF_Scale scale = (MMF_Scale)newPlayer.AddFeedback(typeof(MMF_Scale));
            scale.Label = "Scale child " + i;
            scale.AnimateScaleTarget = transform.GetChild(i);
            MMF_Pause pause = (MMF_Pause)newPlayer.AddFeedback(typeof(MMF_Pause));
            pause.PauseDuration = 0.2f;
        }
        newPlayer.Initialization();
        newPlayer.PlayFeedbacks();
    }
}
```
Quita un feedback con `player.RemoveFeedback(index)`.

## Desactivar feedbacks (ajustes, accesibilidad)

1. Un tipo, en todos lados: `MMF_Scale.FeedbackTypeAuthorized = false;` (cada clase de feedback tiene su switch estático).
2. Todo: `MMFeedbacks.GlobalMMFeedbacksActive = false;`
3. Sin código: componente **MMFeedbacksAuthorizations** en un GameObject vacío y desmarca los tipos.
4. Un player: desactiva su GameObject/componente o `player.CanPlay = false`.
5. Un feedback: desmarca el checkbox junto a su nombre (`Active = false`).

## Verificación

- El GameObject tiene exactamente un `MMF_Player` (el componente es `[DisallowMultipleComponent]`).
- `player.FeedbacksList.Count` coincide con lo esperado y cada uno es del tipo correcto (`player.GetFeedbackOfType<MMF_Scale>() != null`).
- Sin setup pendiente: `player.FeedbacksList.TrueForAll(f => !f.EvaluateRequiresSetup())`.
- Targets asignados (ej. `scale.AnimateScaleTarget != null`).
- En Play mode, tras `PlayFeedbacks()`: `player.IsPlaying` true, `player.TimesPlayed` aumenta, `player.TotalDuration > 0` para feedbacks con tiempo.
- Escena marcada dirty y guardada (`EditorSceneManager.SaveOpenScenes()`) si el cambio debe persistir. Console sin errores.

## Problemas comunes

- **`PlayFeedbacks` no hace nada** → referencia null, player o GameObject desactivado, `CanPlay` false, o cooldown activo → revisa la referencia, activa el objeto, mira `CooldownDuration` del player y del Timing de cada feedback.
- **Icono de advertencia en un feedback** → falta un target → asigna el campo que indica el cuadro de ayuda.
- **Cambios en runtime ignorados** → el feedback cacheó valores → `player.Initialization()` tras el cambio y `ComputeCachedTotalDuration()` si tocaste tiempos.
- **Player creado por código no reproduce** → se inicializó en Start, después de tu Play → `InitializationMode = Script` y `Initialization()` manual antes de `PlayFeedbacks()`.
- **No se puede relanzar mientras corre** → `CanPlayWhileAlreadyPlaying` false en el player o `AllowAdditivePlays` false en el feedback → actívalo.
- **Feedbacks copiados se comportan raro** → usaste "Copy Component Values" de Unity → usa **Copy All / Paste** del player (o un Preset del MMF Player).
- **Cambios hechos en Play mode se pierden** → comportamiento normal de Unity → botón **Keep Playmode Changes** antes de salir.
- **`OnComplete` nunca se dispara** → `Events.TriggerUnityEvents` está apagado; para `MMFeedbacksEvent` hay que encender `Events.TriggerMMFeedbacksEvents`.
- **`SkipToTheEnd` no hace nada** → solo funciona mientras el player reproduce y puede tardar hasta 3 frames.
- **Un feedback falta en Add new feedback** → su paquete no está instalado (su define no existe) → instala el paquete (pregunta antes; `Tools > More Mountains > MMFeedbacks > Install All Dependencies` instala Cinemachine, Post Processing y 2D Animation cuando están los demos de Feel). El define se añade tras recompilar.

## Límites

- La vista previa en edit mode (Tools > More Mountains > MMFeedbacks > Enable MMF Player Preview) es experimental, solo algunos feedbacks la soportan y modifica la escena de verdad: prefiere probar en Play mode.
- Los feedbacks que hacen broadcast a shakers (cámara, post-processing, muchos de audio/cámara) no hacen nada sin el shaker correspondiente en la escena (ver screen-shake.md).
- Feel no añade costo propio: partículas masivas o post-processing en móvil cuestan lo que cuestan esos sistemas.
