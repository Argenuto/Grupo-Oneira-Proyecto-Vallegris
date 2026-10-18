# Configurar el Sound Manager y el Playlist Manager

La pila de audio de Feel tiene dos managers (namespace `MoreMountains.Tools`). **`MMSoundManager`** reproduce sonidos en pool a través de un AudioMixer, con tracks Master, Music, Sfx y UI más tracks custom; su configuración vive en un asset `MMSoundManagerSettingsSO`. **`MMSMPlaylistManager`** reproduce assets `MMSMPlaylist` (listas ordenadas de canciones) con fades, órdenes de reproducción y encadenado. Para reproducir o controlar sonidos individuales ver `sound-manager.md`.

## Contenido
- Prerrequisitos · Quick start · Colocar el MMSoundManager · Settings y mixer propios · Track custom · Guardar/restaurar ajustes · Sliders de volumen y mute · Asset de playlist · Playlist manager · Controlar playlists · Verificación · Problemas · Límites

## Prerrequisitos

- Instalación verificada: `System.Type.GetType("MoreMountains.Tools.MMSoundManager, MoreMountains.Tools")` y `...MMSMPlaylistManager...` no null, y existe `Assets/Feel/MMTools/Core/MMAudio`.
- Assets por defecto: `Assets/Feel/MMTools/Core/MMAudio/MMSoundManager/Settings/MMSoundManagerSettings.asset` (settings) y `MMSoundManagerAudioMixer.mixer` en la misma carpeta.
- Scripts: `using MoreMountains.Tools;`.
- Editar mixer y settings cambia assets del proyecto: confirma con el usuario antes de modificar los assets por defecto de Feel (mejor duplicarlos, ver abajo).

## Quick start: manager persistente + playlist aleatoria que arranca al cargar

1. En la primera escena que carga el juego (bootstrap o menú principal) crea un GameObject raíz `MMSoundManager` + Add Component > More Mountains > Tools > Audio > **MM Sound Manager**. Añadido por inspector, `settingsSo` se llena solo con el `MMSoundManagerSettings` por defecto. Añadido por código hay que asignarlo (ver script).
2. Crea un asset de playlist: Create > MoreMountains > Audio > **MMSM Playlist**, llámalo `GameplayMusic`. `Track = Music` (default), `PlayOrder = RandomUnique`, `PlayMode = PlayForever` (default) y una entrada en **Songs** por clip (`Name`, `Clip`; deja `Options` por defecto).
3. GameObject raíz `MusicPlayer` + **MMSMPlaylistManager** (busca "MMSM Playlist Manager"). `Playlist = GameplayMusic`, marca `PlayOnStart` y `Persistent` si la música debe continuar entre escenas.
4. Equivalente por editor script:

```csharp
using MoreMountains.Tools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FeelAudioSetup
{
    const string DefaultSettingsPath =
        "Assets/Feel/MMTools/Core/MMAudio/MMSoundManager/Settings/MMSoundManagerSettings.asset";

    public static void SetupAudio(AudioClip[] songs, string playlistPath = "Assets/Audio/GameplayMusic.asset")
    {
        // sound manager (uno por juego)
        MMSoundManager manager = Object.FindAnyObjectByType<MMSoundManager>();
        if (manager == null)
        {
            GameObject go = new GameObject("MMSoundManager");
            Undo.RegisterCreatedObjectUndo(go, "Create MMSoundManager");
            manager = go.AddComponent<MMSoundManager>();
        }
        if (manager.settingsSo == null)
        {
            manager.settingsSo = AssetDatabase.LoadAssetAtPath<MMSoundManagerSettingsSO>(DefaultSettingsPath);
        }

        // asset de playlist (la carpeta destino debe existir)
        MMSMPlaylist playlist = ScriptableObject.CreateInstance<MMSMPlaylist>();
        playlist.Track = MMSoundManager.MMSoundManagerTracks.Music;
        playlist.PlayOrder = MMSMPlaylist.PlayOrders.RandomUnique;
        playlist.Songs = new System.Collections.Generic.List<MMSMPlaylistSong>();
        foreach (AudioClip clip in songs)
        {
            MMSMPlaylistSong song = new MMSMPlaylistSong();
            song.Name = clip.name;
            song.Clip = clip;
            song.Options = MMSoundManagerPlayOptions.Default;
            song.Options.MmSoundManagerTrack = MMSoundManager.MMSoundManagerTracks.Music;
            playlist.Songs.Add(song);
        }
        AssetDatabase.CreateAsset(playlist, playlistPath);
        AssetDatabase.SaveAssets();

        // playlist manager
        GameObject musicGo = new GameObject("MusicPlayer");
        Undo.RegisterCreatedObjectUndo(musicGo, "Create MusicPlayer");
        MMSMPlaylistManager playlistManager = musicGo.AddComponent<MMSMPlaylistManager>();
        playlistManager.Playlist = playlist;
        playlistManager.PlayOnStart = true;
        playlistManager.Persistent = true;

        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(playlistManager);
        EditorSceneManager.MarkSceneDirty(musicGo.scene);
    }
}
```

**Resultado**: un solo MMSoundManager (con `settingsSo`) y un MMSMPlaylistManager que referencia una playlist con todos los clips asignados. En Play mode la música arranca en el track Music; el grupo **Status** del playlist manager muestra `CurrentSongName` y `CurrentProgress` avanzando.

## Colocar el MMSoundManager correctamente

1. Ponlo en un GameObject raíz de la primera escena que carga. Es un `MMPersistentSingleton`: la primera instancia hace `DontDestroyOnLoad` y cualquier duplicado posterior se destruye. Las copias en otras escenas son inocuas y permiten probar esas escenas por separado.
2. Asegura que `settingsSo` esté asignado. Sin él fallan las llamadas que dependen de settings (cambios de volumen de track, guardado, tracks custom).
3. Pool: `AudioSourcePoolSize` (10 por defecto) pre-crea AudioSources; ponlo por encima del máximo de sonidos simultáneos. `PoolCanExpand` (true) añade fuentes cuando se agota.
4. `MMSoundManager.Instance` crea un manager vacío al vuelo si no hay ninguno; no te apoyes en eso: no tiene asset de settings.

**Resultado**: tras cargar una segunda escena en Play mode, `Object.FindObjectsByType<MMSoundManager>().Length == 1`.

## Settings y AudioMixer propios (que un update de Feel no pise)

1. Duplica `MMSoundManagerSettings.asset` y `MMSoundManagerAudioMixer.mixer` (Ctrl+D) a una carpeta del proyecto (ej. `Assets/Audio/`). Alternativa: Create > MoreMountains > Audio > MMSoundManagerSettings.
2. En el settings copiado asigna `TargetAudioMixer` al mixer copiado y `MasterAudioMixerGroup`, `MusicAudioMixerGroup`, `SfxAudioMixerGroup`, `UIAudioMixerGroup` a sus grupos.
3. El mixer debe exponer parámetros de volumen con los nombres de **Settings**: `MasterVolumeParameter = "MasterVolume"`, `MusicVolumeParameter = "MusicVolume"`, `SfxVolumeParameter = "SfxVolume"`, `UIVolumeParameter = "UIVolume"` (defaults). El mixer copiado ya los tiene; uno propio debe exponer parámetros con esos nombres, o cambia los nombres en Settings.
4. Asigna el nuevo settings al `settingsSo` del manager.
5. Otros sistemas pueden rutear sus AudioSources a los mismos grupos del mixer para que los volúmenes de track les apliquen.

## Track custom (ej. Dialogue)

1. Abre el `TargetAudioMixer` del settings. Añade un grupo `Dialogue`. Con el grupo seleccionado, clic derecho en **Volume** del inspector > **Expose 'Volume (of Dialogue)' to script**. En **Exposed Parameters** renómbralo `Dialogue`.
2. Create > MoreMountains > Audio > MMSoundManagerTrack → `DialogueTrack`. `VolumeParameter = "Dialogue"` y `AudioMixerGroup` = grupo Dialogue. `DefaultVolume` (1) y `DefaultOn` (true) fijan el estado inicial.
3. Añade `DialogueTrack` a la lista `CustomTracks` del settings.
4. Sonidos y playlists lo usan con track = Other y asignando el custom track (`MMSoundManagerPlayOptions.CustomTrack`, o el campo **Custom Track** en feedbacks y sliders de volumen).

Para cambiar su volumen: `MMSoundManagerTrackEvent.Trigger(MMSoundManagerTrackEventTypes.SetVolumeTrack, MMSoundManager.MMSoundManagerTracks.Other, 0.5f, dialogueTrack)`.

## Guardar y restaurar ajustes de audio

1. En **Settings** del asset: `AutoLoad` activo por defecto (carga lo guardado al iniciar) y `AutoSave` apagado. Activa `AutoSave` para guardar en cada cambio, o guarda explícitamente.
2. Llamadas explícitas: `MMSoundManagerEvent.Trigger(MMSoundManagerEventTypes.SaveSettings)`, `LoadSettings`, `ResetSettings`; o el feedback Audio > **MMSoundManager Save and Load**.
3. Se guarda vía `MMSaveLoadManager` (carpeta `MMSoundManager/`). En desarrollo, Tools > More Mountains > Delete all saved data los borra: borra todos los saves de MMTools, así que pregunta antes.

**Resultado**: tras guardar un volumen de Music bajo y reiniciar Play mode, Music arranca con el volumen guardado.

## Sliders de volumen y controles de mute

1. Por cada UI Slider (rango 0–1) añade `MMSoundManagerTrackVolumeSlider` en el mismo GameObject, fija `Track` (o `Track = Other` + `CustomTrack`), `MinVolume = 0`, `MaxVolume = 1`.
2. Enlaza el **On Value Changed (Single)** del slider a `MMSoundManagerTrackVolumeSlider > UpdateVolume` en la sección **dynamic float** (así lo cablea el demo `FeelMMSoundManagerTrackControl`). El componente no se suscribe solo.
3. El componente mantiene el slider sincronizado con el track: empieza en modo Write y pasa un instante a Read al captar eventos de mute, unmute o fade, para que el slider siga cambios hechos desde otro lado.
4. Botones/toggles de mute: enlaza métodos de `MMSoundManager` como `MuteMusic` / `UnmuteMusic`, `MuteSfx` / `UnmuteSfx`, `MuteMaster` / `UnmuteMaster`.
5. Cableado por editor script:

```csharp
using MoreMountains.Tools;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.UI;

public static class FeelVolumeSliderSetup
{
    public static void Wire(Slider slider, MMSoundManager.MMSoundManagerTracks track)
    {
        MMSoundManagerTrackVolumeSlider volume = Undo.AddComponent<MMSoundManagerTrackVolumeSlider>(slider.gameObject);
        volume.Track = track;
        Undo.RecordObject(slider, "Wire volume slider");
        UnityEventTools.AddPersistentListener(slider.onValueChanged, volume.UpdateVolume);
        EditorUtility.SetDirty(slider);
    }
}
```

**Resultado**: mover el slider cambia el volumen de ese track; mutear el track desde otro lado lleva el slider a 0.

## Asset de playlist

Create > MoreMountains > Audio > MMSM Playlist. Campos: `Track` (Music por defecto), `PlayOrder` (Normal, ReverseOrder, Random, RandomUnique), `RandomizeOrderSeed` (true), `PlayMode` (PlayForever, PlayOnce, PlayXTimes con `MaxAmountOfPlays`, 10 por defecto), `NextPlaylist` (cambia a otra playlist cuando esta termina). **Songs**: cada `MMSMPlaylistSong` tiene `Name` (organizativo), `Clip` y `Options` (un `MMSoundManagerPlayOptions`: volumen, pitch, fade y espacial por canción). Todas las canciones deben tener `Clip`.

## Playlist manager

1. **Settings**: `Channel` (0; lo usan eventos y feedbacks para dirigirse a este manager), `Playlist`, `PlayOnStart`, `VolumeMultiplier` / `PitchMultiplier` (1), `Persistent` (sobrevive a cargas de escena: la primera instancia hace `DontDestroyOnLoad` y los duplicados se destruyen; `AutomaticallyUnparentOnAwake`, true, lo mueve primero a la raíz), `AutoHandleApplicationPause` (true).
2. **Fade**: `FadeIn`, `FadeOut`, `FadeDuration` (1 s) para transiciones entre canciones.
3. **Time**: `BindPitchToTimeScale` mapea `Time.timeScale` (`TimescaleRemapFrom`, 0–2 por defecto) a pitch (`TimescaleRemapTo`, 0.8–1.2), así la música se ralentiza con el slow motion.
4. **Eventos**: `OnSongStart`, `OnSongEnd`, `OnPause`, `OnStop`, `OnPlaylistChange`, `OnPlaylistEnd` (útil para un "now playing" en la UI).
5. Debe haber un `MMSoundManager` en la escena, idealmente en su propio GameObject.

**Resultado**: en Play mode **Status** muestra la canción actual y los botones **Test Controls** (play, pause, next, previous) funcionan.

## Controlar playlists desde el gameplay

1. Llamadas directas al manager: `Play()`, `Pause()`, `Stop()`, `StopWithFade()`, `PlayNextSong()`, `PlayPreviousSong()`, `PlaySongAt(index)`, `QueueSongAtIndex(index)`, `SetCurrentSongLoop(bool)`, `ChangePlaylist(playlist)`, `ChangePlaylistAndPlay(playlist)`, `SetVolumeMultiplier(float)`, `SetPitchMultiplier(float)`. También se pueden enlazar a botones de UI.
2. Eventos (sin referencia; el argumento es el canal):

```csharp
using MoreMountains.Tools;

MMPlaylistPlayEvent.Trigger(0);
MMPlaylistPauseEvent.Trigger(0);
MMPlaylistPlayNextEvent.Trigger(0);
MMPlaylistPlayPreviousEvent.Trigger(0);
MMPlaylistPlayIndexEvent.Trigger(0, 3);
MMPlaylistVolumeMultiplierEvent.Trigger(0, 0.5f, true);
MMPlaylistChangeEvent.Trigger(0, menuPlaylist, true); // cambia de playlist y la reproduce
MMPlaylistStopEvent.Trigger(0);
```

3. Desde un MMF Player: Audio > **MMPlaylist** (`MMF_Playlist`): `Mode = Play`, `PlayNext` (default), `PlayPrevious`, `Stop`, `Pause`, `PlaySongAt` (`SongIndex`), `SetVolumeMultiplier`, `ChangePlaylist` (`NewPlaylist`, `ChangePlaylistAndPlay`). Su **Channel** debe coincidir con el del manager.

**Resultado**: la música responde a los comandos y `CurrentSongIndex` se actualiza.

## Verificación

- Un solo MMSoundManager en la escena, con `settingsSo != null` y `settingsSo.TargetAudioMixer != null`.
- Tracks custom pedidos: `settingsSo.CustomTracks` los contiene, cada uno con `VolumeParameter` no vacío y `AudioMixerGroup`.
- Playlist: `AssetDatabase.LoadAssetAtPath<MMSMPlaylist>(path)` no es null, `Songs.Count > 0`, todo `song.Clip != null`, y `Track`, `PlayOrder`, `PlayMode` coinciden con lo pedido.
- Playlist manager: `FindAnyObjectByType<MMSMPlaylistManager>()` tiene `Playlist` asignado y `PlayOnStart` / `Persistent` como se pidió.
- Sliders: cada uno tiene `MMSoundManagerTrackVolumeSlider` con el `Track` correcto y `slider.onValueChanged.GetPersistentMethodName(0) == "UpdateVolume"`.
- En Play mode: `MMSoundManager.Instance.HasSoundsPlaying(MMSoundManager.MMSoundManagerTracks.Music)` es true al arrancar la playlist y `CurrentSongName` no está vacío. Console sin errores.

## Problemas comunes

- **NullReferenceException del MMSoundManager al cambiar volúmenes o guardar** → `settingsSo` sin asignar (típico al añadirlo por código) → asigna un settings.
- **La música se corta al cargar escena** → el playlist manager no es Persistent → marca `Persistent` y deja el sound manager en la primera escena.
- **Dos canciones a la vez tras cambiar de escena** → cada escena tiene su playlist manager no persistente → haz uno persistente o quita los duplicados.
- **El playlist manager no reproduce nada** → sin Playlist, `PlayOnStart` apagado sin llamada a play, Songs vacío o clips null, track Music muteado o a 0, o canal distinto al de eventos/feedback → revisa cada uno.
- **El slider de volumen no hace nada** → `onValueChanged` no está enlazado a `UpdateVolume`, o `Track` equivocado → enlázalo como dynamic float.
- **El volumen del track custom nunca cambia** → el parámetro del mixer no está expuesto o su nombre difiere de `VolumeParameter` → exponlo e iguala los nombres exactamente.
- **Todo en silencio tras probar** → se recargan ajustes muteados (`AutoLoad`) → desmutea, `ResetSettings` o borra datos (con permiso).
- **Un update de Feel revirtió mis cambios al mixer** → editaste los assets por defecto de Feel → usa settings y mixer duplicados en una carpeta del proyecto.

## Límites

- Los managers manejan el AudioMixer de Unity; no se integran con FMOD ni Wwise.
- Cada playlist manager reproduce una playlist a la vez; para música y ambiente simultáneos usa managers con canales distintos para que los eventos apunten al correcto.
- Las playlists reproducen `AudioClip`s; para streaming o música adaptativa usa herramientas dedicadas.
- Reproducción de sonidos individuales, feedbacks de sonido y control en runtime están en `sound-manager.md`.
