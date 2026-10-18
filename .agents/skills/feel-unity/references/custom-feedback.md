# Escribir un feedback propio para Feel

Todo feedback de Feel es una clase C# serializable que hereda de `MoreMountains.Feedbacks.MMF_Feedback`. **No es un MonoBehaviour**: el `MMF_Player` lo guarda con `[SerializeReference]` y llama a sus métodos de ciclo de vida. Cualquier subclase con el atributo `[FeedbackPath]` aparece sola en el desplegable **Add new feedback**.

## Contenido
- Prerrequisitos · Quick start · Plantilla completa · Coroutines · Extender un feedback existente · Valor en el tiempo con MMF_FeedbackBase · Broadcast: evento + shaker · Verificación · Problemas · Límites

## Prerrequisitos

- Instalación verificada: `System.Type.GetType("MoreMountains.Feedbacks.MMF_Feedback, MoreMountains.Tools")` no es null.
- **Dónde poner el archivo**: en la carpeta propia del usuario (ej. `Assets/Scripts/Feedbacks/`), nunca dentro de `Assets/Feel/`, para que un update de Feel no lo borre ni lo pise.
- **Assemblies**: `MoreMountains.Tools` se referencia solo desde Assembly-CSharp. Si la carpeta destino tiene su propio `.asmdef`, añade `MoreMountains.Tools` a sus referencias.
- **Antes de escribir uno nuevo**, revisa si ya existe un feedback que haga el trabajo: `GameObject > Property`, `Events > Unity Events`, `Renderer > ShaderController`, `Renderer > Material Set Property`. (El catálogo oficial `feedback-catalog.md` no está incluido aquí; está en `Assets/Feel/Skills/feel-add-feedbacks/references/`.)

## Quick start: un feedback que llama a un método de un componente propio ("heal")

1. Crea `Assets/Scripts/Feedbacks/MMF_Heal.cs`:

```csharp
using MoreMountains.Feedbacks;
using UnityEngine;

[AddComponentMenu("")]
[System.Serializable]
[FeedbackHelp("Heals the target Health component by the specified amount when played.")]
[FeedbackPath("Gameplay/Heal")]
public class MMF_Heal : MMF_Feedback
{
    public static bool FeedbackTypeAuthorized = true;

#if UNITY_EDITOR
    public override Color FeedbackColor => MMFeedbacksInspectorColors.DebugColor;
    public override bool EvaluateRequiresSetup() => (TargetHealth == null);
    public override string RequiredTargetText => TargetHealth != null ? TargetHealth.name : "";
    public override string RequiresSetupText => "This feedback requires a TargetHealth to be set.";
#endif

    // este feedback es instantáneo
    public override float FeedbackDuration => 0f;

    [MMFInspectorGroup("Heal", true, 12, true)]
    [Tooltip("the Health component to heal")]
    public Health TargetHealth;
    [Tooltip("the amount of health to restore, multiplied by the player's intensity")]
    public float Amount = 10f;

    protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
    {
        if (!Active || !FeedbackTypeAuthorized || (TargetHealth == null)) { return; }
        float intensityMultiplier = ComputeIntensity(feedbacksIntensity, position);
        TargetHealth.Heal(Amount * intensityMultiplier);
    }
}
```
(`Health` y `Heal(float)` representan el componente y método reales del usuario: sustitúyelos antes de compilar.)

2. Espera la compilación y revisa la Console.
3. En cualquier MMF Player, **Add new feedback**: aparece la categoría Gameplay con Heal. Añádelo y asigna **Target Health**.

**Resultado**: compila; `Gameplay/Heal` aparece listado (también en Tools > More Mountains > MMFeedbacks > Output MMF_Feedbacks list); el feedback muestra icono de advertencia hasta asignar `TargetHealth`; en Play mode, reproducir el player llama a `Heal`.

## Escribir un feedback desde la plantilla

1. **Obligatorio**: heredar de `MMF_Feedback`, `[System.Serializable]` y `[FeedbackPath("Categoría/Nombre")]`. El path decide la entrada del desplegable; una clase sin él se salta. `[AddComponentMenu("")]` la saca del menú Add Component de Unity.
2. Sobrescribe lo que necesites (todos `protected`, firmas de Feel 6.1):
   - `CustomInitialization(MMF_Player owner)`: cachea referencias. Llama a `base.CustomInitialization(owner)`.
   - `CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)`: **requerido** (abstracto). Empieza con `if (!Active || !FeedbackTypeAuthorized) return;`.
   - `CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1.0f)`: detén lo que arrancó play.
   - `CustomSkipToTheEnd(...)`, `CustomRestoreInitialValues()`, `CustomReset()`, `CustomPlayerComplete()`: opcionales.
3. Sobrescribe `FeedbackDuration` para que el player calcule su duración total y las holding pauses esperen. Para uno con tiempo: `public override float FeedbackDuration { get { return ApplyTimeMultiplier(Duration); } set { Duration = value; } }`.
4. Respeta el contexto del player: `ComputeIntensity(feedbacksIntensity, position)` para la fuerza, `NormalPlayDirection` para soportar reversa, y `FeedbackDeltaTime` / `FeedbackTime` en vez de `Time.deltaTime` para respetar escalado/sin escalar y la vista previa del editor.
5. Pulido de editor dentro de `#if UNITY_EDITOR`: `FeedbackColor` (colores en `MMFeedbacksInspectorColors`), `EvaluateRequiresSetup()`, `RequiresSetupText`, `RequiredTargetText`. Agrupa campos con `[MMFInspectorGroup("Nombre", true, colorIndex, requiresSetup)]`: el último argumento pone el icono de advertencia en ese grupo.
6. Añade `[Tooltip]` a los campos públicos; se muestran en el inspector.

## Algo en el tiempo (coroutines)

Los feedbacks no son MonoBehaviours: inicia coroutines en el player anfitrión con `Owner.StartCoroutine(...)`, guarda el handle y deténlo en `CustomStopFeedback` con `Owner.StopCoroutine(_coroutine)`. Dentro avanza el tiempo con `FeedbackDeltaTime` y marca `IsPlaying = true/false` al empezar/terminar.

```csharp
protected Coroutine _coroutine;

protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
{
    if (!Active || !FeedbackTypeAuthorized) { return; }
    if (_coroutine != null) { Owner.StopCoroutine(_coroutine); }
    _coroutine = Owner.StartCoroutine(AnimateCo());
}

protected virtual System.Collections.IEnumerator AnimateCo()
{
    IsPlaying = true;
    float elapsed = 0f;
    while (elapsed < FeedbackDuration)
    {
        float t = elapsed / FeedbackDuration;
        // aplicar t al target aquí
        elapsed += FeedbackDeltaTime;
        yield return null;
    }
    IsPlaying = false;
    _coroutine = null;
}

protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
{
    if (_coroutine != null) { Owner.StopCoroutine(_coroutine); _coroutine = null; }
    IsPlaying = false;
}
```

**Resultado**: el efecto dura `FeedbackDuration` y se detiene limpio al parar el player.

## Extender un feedback existente (mejor que copiarlo o editar el código de Feel)

1. Encuentra la clase (`MMF_` + nombre) y el método `protected virtual` que quieres cambiar.
2. Hereda y sobrescribe. Ejemplo de la documentación: sufijo en TMP Count To (`MMF_TMPCountTo.UpdateText(float)` es `protected virtual`; los feedbacks TMP requieren el define `MM_UGUI2`, es decir uGUI 2.x):

```csharp
using MoreMountains.Feedbacks;

[System.Serializable] // tampoco se hereda: necesario para la lista [SerializeReference] del player
[FeedbackPath("TextMesh Pro/TMP Count To With Suffix")]
public class MMF_TMPCountToSuffix : MMF_TMPCountTo
{
    public string Suffix = "$";

    protected override void UpdateText(float currentValue)
    {
        base.UpdateText(currentValue);
        TargetTMPText.text = TargetTMPText.text + Suffix;
    }
}
```

3. Dale a la subclase su propio `[FeedbackPath]`: el atributo **no se hereda** (Feel lo lee con `GetCustomAttributes(false)`), así que una subclase sin él no aparece en **Add new feedback**.

**Resultado**: la entrada nueva aparece junto a la original y se comporta igual más tu cambio.

## Feedback de valor-en-el-tiempo con MMF_FeedbackBase

Para animar rápido cualquier propiedad numérica/color (aquí el alpha de un CanvasGroup, como hace `MMF_CanvasGroup` de Feel):

1. Hereda de `MMF_FeedbackBase` (abstracta; aporta `Mode = OverTime/Instant/ToDestination`, `Duration = 0.2`, `RelativeValues`, `AllowAdditivePlays`, `StartsOff`, `EndsOff`).
2. Implementa `FillTargets()`: construye un `MMF_FeedbackBaseTarget` cuyo `Target` es un `MMPropertyReceiver` apuntando al componente y al nombre de propiedad, y añádelo a `_targets`:

```csharp
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;

[System.Serializable]
[FeedbackPath("UI/My CanvasGroup Alpha")]
public class MMF_MyCanvasGroupAlpha : MMF_FeedbackBase
{
    [MMFInspectorGroup("Canvas Group", true, 12, true)]
    public CanvasGroup TargetCanvasGroup;
    public MMTweenType AlphaCurve = new MMTweenType(new AnimationCurve(
        new Keyframe(0, 0), new Keyframe(0.3f, 1f), new Keyframe(1, 0)));
    public float RemapZero = 0f;
    public float RemapOne = 1f;
    public float InstantAlpha;

    protected override void FillTargets()
    {
        if (TargetCanvasGroup == null) { return; }
        MMF_FeedbackBaseTarget target = new MMF_FeedbackBaseTarget();
        MMPropertyReceiver receiver = new MMPropertyReceiver();
        receiver.TargetObject = TargetCanvasGroup.gameObject;
        receiver.TargetComponent = TargetCanvasGroup;
        receiver.TargetPropertyName = "alpha";
        receiver.RelativeValue = RelativeValues;
        target.Target = receiver;
        target.LevelCurve = AlphaCurve;
        target.RemapLevelZero = RemapZero;
        target.RemapLevelOne = RemapOne;
        target.InstantLevel = InstantAlpha;
        _targets.Add(target);
    }
}
```

3. Referencia real: `Assets/Feel/MMFeedbacks/MMFeedbacksForThirdParty/MMTools/Feedbacks/MMF_CanvasGroup.cs` (también muestra el soporte de `ToDestination` y la adquisición automática de targets).

**Resultado**: el feedback anima la propiedad a lo largo de la curva durante `Duration`.

## Broadcast: evento + shaker

Para un feedback que maneja cualquier cantidad de receptores en un canal (como los de post-processing y cámara de Feel):

1. Escribe un struct de evento con un `Delegate` estático, `Register`, `Unregister`, `Trigger` y un método `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` que ponga el evento en null (para que el play mode con domain reload desactivado arranque limpio).
2. Escribe un shaker que herede de `MoreMountains.Feedbacks.MMShaker`: sobrescribe `Initialization()`, `Shake()` (usa `ShakeFloat(curve, remapMin, remapMax, relative, initial)`), `GrabInitialValues()`, `ResetTargetValues()`, `ResetShakerValues()`, y `StartListening()` / `StopListening()` para registrar el handler del evento. El handler comprueba `CheckEventAllowed(channelData)` y luego llama a `Play()`. `MMShaker` ya llama a `StartListening()` desde `OnEnable`, lo cual importa porque los eventos estáticos se reinician con el domain reload.
3. Escribe el feedback: `HasChannel => true` y, en `CustomPlayFeedback`, llama `TuEvento.Trigger(..., ChannelData, ...)`.
4. El ejemplo oficial completo de tres piezas (modelado sobre `MMAudioSourceVolumeShaker`) está en `references/shaker-pattern.md` de la skill oficial (`Assets/Feel/Skills/feel-custom-feedback/references/`), no incluido aquí: léelo de ahí antes de escribir uno.

**Resultado**: con el shaker en un objeto y el feedback reproduciéndose, el valor se anima; los canales que no coinciden se ignoran.

## Verificación

- El script compila sin errores en la Console.
- El tipo resuelve y es un feedback: `typeof(MMF_Feedback).IsAssignableFrom(typeof(MMF_Heal))`.
- `FeedbackPathAttribute.GetFeedbackDefaultPath(typeof(MMF_Heal))` devuelve el path esperado (`"Gameplay/Heal"`) y aparece en la salida de Output MMF_Feedbacks list.
- `player.AddFeedback(typeof(MMF_Heal))` devuelve una instancia no null, y `EvaluateRequiresSetup()` es true hasta asignar targets y luego false.
- En Play mode `player.PlayFeedbacks()` produce el efecto y `player.TotalDuration` refleja `FeedbackDuration`.
- El archivo vive fuera de `Assets/Feel/`.

## Problemas comunes

- **No aparece en Add new feedback** → falta `[FeedbackPath]` en la propia clase (no se hereda), hay error de compilación, o la clase es abstracta → añade el atributo, arregla errores, hazla concreta.
- **Campos se resetean o el feedback desaparece tras recargar dominio** → clase no serializable, o renombrada/movida sin `[MovedFrom]` → `[System.Serializable]`; al renombrar añade `[UnityEngine.Scripting.APIUpdating.MovedFrom(...)]` como los feedbacks de Feel para que los datos `[SerializeReference]` sigan resolviendo.
- **`StartCoroutine` no existe** → no son MonoBehaviours → usa `Owner.StartCoroutine(...)`.
- **La duración total del player es 0 y las holding pauses no esperan** → no sobrescribiste `FeedbackDuration` → devuelve la duración real.
- **Ignora los ajustes de timescale o se congela en la vista previa del editor** → usa `Time.deltaTime` → usa `FeedbackDeltaTime`.
- **Cambios perdidos tras actualizar Feel** → pusiste el archivo en `Assets/Feel` (o editaste un archivo de Feel) → carpeta propia y extender en vez de modificar.
- **CS0246 tipo o namespace no encontrado** → el asmdef del script no referencia `MoreMountains.Tools` → añade la referencia.
- **El shaker deja de reaccionar tras recompilar** → no se re-registra el evento → regístralo en `StartListening()` (llamado desde `MMShaker.OnEnable`), no solo en `Awake`.

## Límites

- Los feedbacks corren dentro de un `MMF_Player`; no son componentes independientes. Para un efecto basado en componente escribe un MonoBehaviour (o un componente spring, ver springs.md) y manéjalo desde un feedback.
- `MMF_FeedbackBase` mueve propiedades por reflexión (`MMPropertyReceiver`); en rutas muy calientes un `CustomPlayFeedback` escrito a mano es más barato.
- El soporte de vista previa en edit mode depende de que uses los helpers de tiempo de Feel; no está garantizado en código propio.
- No modifiques los archivos fuente de Feel para personalizar comportamiento: hereda, así los updates no duelen.
