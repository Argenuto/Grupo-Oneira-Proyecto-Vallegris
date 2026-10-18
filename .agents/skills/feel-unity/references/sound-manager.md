# Reproducir y controlar sonido con MMSoundManager

`MMSoundManager` (namespace `MoreMountains.Tools`) es el manager de audio de Feel: reproduce sonidos en AudioSources en pool, los enruta por un AudioMixer con cuatro tracks (Master, Music, Sfx, UI, más tracks custom) y se controla de tres maneras: **feedbacks de un MMF Player** (Audio > MMSoundManager *), **eventos de una línea**, o llamadas directas a `MMSoundManager.Instance`. Instalar y configurar el manager (persistencia, mixer propio, tracks custom, sliders, guardado, playlists) está en `sound-setup.md`.

## Contenido
- Prerrequisitos · Quick start · Reproducir desde código · Sonido 3D · Controlar por ID · Tracks: volumen, mute, pausa · Feedback Sound simple · Verificación · Problemas · Límites

## Prerrequisitos

- Instalación de Feel verificada y `System.Type.GetType("MoreMountains.Tools.MMSoundManager, MoreMountains.Tools")` no null, con la carpeta `Assets/Feel/MMTools/Core/MMAudio/MMSoundManager`.
- Scripts: `using MoreMountains.Tools;` (y `using MoreMountains.Feedbacks;` para clases de feedback).
- Asset de settings por defecto: `Assets/Feel/MMTools/Core/MMAudio/MMSoundManager/Settings/MMSoundManagerSettings.asset`, que usa el mixer `MMSoundManagerAudioMixer.mixer` de la misma carpeta.

## Quick start: sonido de salto con pitch aleatorio

1. GameObject vacío en la raíz de la escena `MMSoundManager` + Add Component > More Mountains > Tools > Audio > **MM Sound Manager**. Comprueba que **Settings** (`settingsSo`) referencia `MMSoundManagerSettings`; si está vacío asigna el asset por defecto. Si el proyecto necesita más (persistencia entre escenas, mixer propio), sigue `sound-setup.md` primero.
2. En el MMF Player del salto (ver feedbacks.md): **Add new feedback > Audio > MMSoundManager Sound** (`MMF_MMSoundManagerSound`). Asigna **Sfx** al clip, `MinPitch = 0.9`, `MaxPitch = 1.1`. El track por defecto es Sfx. En edit mode los botones de test del feedback permiten escuchar el clip.
3. Por editor script:

```csharp
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FeelSoundQuickStart
{
    public static void AddJumpSound(MMF_Player player, AudioClip clip)
    {
        if (Object.FindAnyObjectByType<MMSoundManager>() == null)
        {
            GameObject sm = new GameObject("MMSoundManager");
            Undo.RegisterCreatedObjectUndo(sm, "Create MMSoundManager");
            sm.AddComponent<MMSoundManager>();
        }
        Undo.RecordObject(player, "Add MMSM Sound");
        MMF_MMSoundManagerSound sound =
            (MMF_MMSoundManagerSound)player.AddFeedback(typeof(MMF_MMSoundManagerSound));
        sound.Sfx = clip;
        sound.MinPitch = 0.9f;
        sound.MaxPitch = 1.1f;
        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
    }
}
```
(El botón **Automatic Shaker Setup** del feedback también crea el MMSoundManager.)

**Resultado**: exactamente un MMSoundManager en la escena; el player tiene `MMF_MMSoundManagerSound` con `Sfx`; en Play mode suena el clip en el track Sfx con un pitch ligeramente distinto cada vez.

## Reproducir desde código (sin MMF Player)

Llamada simple (2D, track Sfx, en una posición):

```csharp
using MoreMountains.Tools;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    public AudioClip ExplosionSfx;
    public void Explode()
    {
        MMSoundManagerSoundPlayEvent.Trigger(ExplosionSfx,
            MMSoundManager.MMSoundManagerTracks.Sfx, transform.position);
    }
}
```

Con opciones (parte de los defaults y sobrescribe lo necesario):

```csharp
MMSoundManagerPlayOptions options = MMSoundManagerPlayOptions.Default; // Sfx, volumen 1, pitch 1, 2D
options.MmSoundManagerTrack = MMSoundManager.MMSoundManagerTracks.Music;
options.Loop = true;
options.Volume = 0.5f;
options.ID = 50;           // permite controlar este sonido después
options.Persistent = true; // sobrevive a cargas de escena
AudioSource source = MMSoundManagerSoundPlayEvent.Trigger(MusicClip, options);
```

Prefiere eventos a `MMSoundManager.Instance`: los eventos no fallan si nadie escucha, y `Instance` crea un manager vacío al vuelo si no existe (sin asset de settings).

## Sonido 3D

1. En el feedback (o en `MMSoundManagerPlayOptions`) pon **Spatial Blend = 1**. Opcional `MinDistance` / `MaxDistance` (1 y 500 por defecto) y `RolloffMode` (Logarithmic).
2. El sonido suena en la posición pasada a `PlayFeedbacks(position, intensity)` (o la del player), o sigue un transform con **Attach To Transform**.
3. Debe existir un `AudioListener` (normalmente en la cámara principal).

## Controlar un sonido por ID

1. Dale un **ID** distinto de cero y único (campo `ID` del feedback, u `options.ID`).
2. Feedbacks: Audio > **MMSoundManager Sound Control** (pausar / reanudar / detener / liberar) y Audio > **MMSoundManager Sound Fade**, con el mismo ID.
3. Código:

```csharp
MMSoundManagerSoundControlEvent.Trigger(MMSoundManagerSoundControlEventTypes.Stop, 50);
MMSoundManagerSoundFadeEvent.Trigger(MMSoundManagerSoundFadeEvent.Modes.PlayFade, 50, 2f, 0f,
    new MMTweenType(MMTween.MMTweenCurve.EaseInCubic)); // baja el sonido 50 a 0 en 2 s
MMSoundManagerSoundControlEvent.Trigger(MMSoundManagerSoundControlEventTypes.Free, 50); // devuelve su AudioSource al pool
```

Solo reaccionan los sonidos con ese ID.

## Tracks: volumen, mute y pausa

1. Mute sin código: en el **On Click ()** de un botón arrastra el objeto MMSoundManager y elige `MMSoundManager > MuteMaster` (o `UnmuteMaster`, `MuteMusic`, `MuteSfx`, `MuteUI`…).
2. Volumen por código o UnityEvents: `SetVolumeMusic(float)`, `SetVolumeSfx(float)`, `SetVolumeUI(float)`, `SetVolumeMaster(float)`. Para sliders sincronizados usa `MMSoundManagerTrackVolumeSlider` (ver `sound-setup.md`).
3. Feedbacks: Audio > **MMSoundManager Track Control** (mute, unmute, set volume, play, pause, stop, free de un track), **Track Fade**, **All Sounds Control**.
4. Código:

```csharp
MMSoundManagerTrackEvent.Trigger(MMSoundManagerTrackEventTypes.PauseTrack, MMSoundManager.MMSoundManagerTracks.Music);
MMSoundManagerTrackEvent.Trigger(MMSoundManagerTrackEventTypes.SetVolumeTrack, MMSoundManager.MMSoundManagerTracks.Sfx, 0.5f);
MMSoundManagerTrackFadeEvent.Trigger(MMSoundManagerTrackFadeEvent.Modes.PlayFade,
    MMSoundManager.MMSoundManagerTracks.Music, 2f, 0f, new MMTweenType(MMTween.MMTweenCurve.EaseInCubic));
MMSoundManagerAllSoundsControlEvent.Trigger(MMSoundManagerAllSoundsControlEventTypes.Stop);
```
5. Ver si hay actividad: `MMSoundManager.Instance.HasSoundsPlaying(MMSoundManager.MMSoundManagerTracks.Sfx)`.

El track elegido se silencia o cambia de volumen sin afectar a los demás.

## Feedback Sound simple (sin manager)

Para prototipos o un objeto aislado: Audio > **Sound** (`MMF_Sound`). `PlayMethod` por defecto es `Event`, que necesita un MMSoundManager que lo atrape; ponlo en `Cached` (AudioSource al inicializar), `OnDemand` (al reproducir) o `Pool` para sonar sin manager. Para cualquier cosa más allá de prototipos prefiere MMSoundManager Sound: tiene tracks, IDs, fades y modos solo.

## Verificación

- Exactamente un manager: `Object.FindObjectsByType<MMSoundManager>().Length == 1` y `settingsSo != null`.
- El feedback existe con clip: `player.GetFeedbackOfType<MMF_MMSoundManagerSound>().Sfx != null` (o `RandomSfx` con entradas).
- Track correcto: `sound.MmSoundManagerTrack == MMSoundManager.MMSoundManagerTracks.Sfx` (o el elegido).
- En Play mode, justo al reproducir `HasSoundsPlaying(<track>)` es true; en un loop se mantiene true.
- Botones de mute: `button.onClick.GetPersistentMethodName(0)` es el esperado (ej. `MuteMaster`). Console sin errores.

## Problemas comunes

- **El feedback de sonido no suena** → sin clip, track muteado o a volumen 0, o clip puesto en el modo de fuente equivocado (`Source = AudioResource` con solo Sfx llenado) → asigna el clip para la fuente elegida y revisa los volúmenes de los tracks en el inspector del manager.
- **Todo en silencio tras probar un botón de mute** → se guardaron los ajustes muteados y `AutoLoad` los restaura → desmutea (`UnmuteMaster`), dispara `ResetSettings`, o borra datos con Tools > More Mountains > Delete all saved data (pregunta antes: borra todos los saves de MMTools).
- **Dos managers / sonidos duplicados al cargar una escena** → es un singleton persistente y cada escena trae su copia → deja uno en una escena bootstrap, o acepta que los duplicados se destruyen solos.
- **Feedback Sound simple en silencio** → `PlayMethod = Event` sin manager escuchando → añade un MMSoundManager o cambia a `Cached`.
- **Se cortan sonidos cuando suenan muchos** → pool pequeño (`AudioSourcePoolSize = 10`) → súbelo en el manager; usa `DoNotAutoRecycleIfNotDonePlaying` para sonidos que deben terminar.
- **El mismo clip se apila muy fuerte** → `DoNotPlayIfClipAlreadyPlaying`, o baja `MaximumConcurrentInstances` (3 por defecto) del feedback.
- **Un sonido se detiene al cambiar de escena** → no es persistente → `Persistent = true` en el feedback o en `MMSoundManagerPlayOptions` (para playlists ver `sound-setup.md`).

## Límites

- MMSoundManager es opcional y convive con otros sistemas (FMOD, Wwise o AudioSources sueltos); no maneja middleware.
- Usa el AudioMixer de Unity: efectos, snapshots y ruteo son de Unity, configurados en el asset del mixer.
- Los filtros de audio por fuente (feedbacks Audio > Audio Filter *) y los de propiedades de AudioSource no pasan por el manager (ver feedbacks.md). Para springs de pitch/volumen en un AudioSource ver springs.md (`MMSpringAudioSourcePitch`, `MMSpringAudioSourceVolume`).
- Haptics sincronizados con audio (clips de Nice Vibrations) no están en esta skill.
