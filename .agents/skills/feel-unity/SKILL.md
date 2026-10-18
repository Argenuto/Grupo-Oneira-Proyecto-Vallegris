---
name: feel-unity
description: Guía completa para usar el asset Feel (More Mountains) en proyectos de Unity — MMF_Player y feedbacks, juice y game feel, screen shake y cámara (Cinemachine, zoom, fade, flash), springs (rebote, squash and stretch), MMSoundManager y playlists de música, y cómo escribir feedbacks propios en C#. Úsala SIEMPRE que el usuario mencione Feel, MMF_Player, MMFeedbacks, More Mountains, MMSoundManager, "juice", "game feel", o pida que algo "rebote", "pop", "se sacuda", "tiemble la cámara", "flash al recibir daño", "hit stop / freeze frame", "squash and stretch", "música de fondo con playlist", "sliders de volumen", "sonido con pitch aleatorio" o "que pase algo cuando termine el feedback" en Unity — incluso si no dice "Feel" explícitamente pero el proyecto tiene la carpeta Assets/Feel. Also use for 'add juice', 'screen shake', 'camera shake', 'bouncy UI', 'custom MMF feedback', 'my MMF_Player does nothing' in a Unity project with Feel installed.
---

# Feel para Unity

Feel (More Mountains) es un asset de Unity para "game feel". Todo gira alrededor de **`MMF_Player`**: un componente que guarda una lista ordenada de *feedbacks* (escala, posición, partículas, sonido, shake de cámara, springs, pausas, loops…) y los reproduce. Esta skill reúne en un solo lugar lo que antes eran skills separadas; usa el router de abajo para leer solo la referencia que hace falta.

> Condensada de las skills oficiales que trae Feel (verificadas contra Feel 6.1 y Unity 6000.5+, última revisión 2026-10-11). Es material de More Mountains: úsala en tus proyectos, no la republiques.

## Router: qué referencia leer

| El usuario quiere… | Lee |
|---|---|
| Juice general: pop de escala, partículas, flicker, freeze frame; crear/llenar un `MMF_Player`; dispararlo desde código, UnityEvent, Animation Event o Timeline; pausas, loops, secuencias; cambiar valores en runtime; callback al terminar; canales; desactivar feedbacks; "mi MMF_Player no hace nada" | `references/feedbacks.md` |
| Shake de cámara o UI, Cinemachine Impulse, zoom punch, fade, flash, toggle de accesibilidad para apagar shakes | `references/screen-shake.md` |
| Movimiento rebotón/elástico: squash and stretch, hover con rebote, seguir el mouse con lag, FOV que se bambolea, clamp, springs propios | `references/springs.md` |
| Reproducir y controlar sonidos en runtime: one-shot, loop, 3D, pitch aleatorio, IDs, fades, mute/pausa de tracks, eventos | `references/sound-manager.md` |
| Instalar/configurar el MMSoundManager: persistencia, mixer propio, tracks custom, guardar volúmenes, sliders de UI, playlists de música | `references/sound-setup.md` |
| Escribir una clase de feedback nueva, extender una existente, feedbacks de valor-en-el-tiempo, shaker + evento propio | `references/custom-feedback.md` |

Si la petición toca dos áreas (ej. "golpe con shake, sonido y partículas"), lee ambas referencias; el hilo común es siempre un `MMF_Player` con varios feedbacks.

## Qué NO cubre esta skill

- **Post-processing** (pulsos de bloom, viñeta, aberración cromática, distorsión de lente) y **haptics/vibración**: Feel tiene skills oficiales para eso (`feel-post-processing-effects`, `feel-haptics`) que no están fusionadas aquí. Si el proyecto tiene `Assets/Feel/Skills/`, léelas desde ahí; si no, consulta https://feel-docs.moremountains.com/ en vez de inventar nombres de clases.
- Las referencias oficiales largas (`feedback-catalog.md`, `spring-components.md`, `shaker-pattern.md`) tampoco están incluidas. Si necesitas la ruta de menú o clase exacta de un feedback o spring que no aparece en estas referencias, búscala en `Assets/Feel/Skills/*/references/` o en la documentación, no la adivines.

## Reglas generales (aplican a todo)

1. **Verifica la instalación antes de tocar nada.** `System.Type.GetType("MoreMountains.Feedbacks.MMF_Player, MoreMountains.Tools")` no debe ser null y debe existir la carpeta `Assets/Feel/`. Si falla, dile al usuario que Feel no está instalado (https://assetstore.unity.com/packages/tools/particles-effects/feel-183370) y detente.
2. **Revisa la versión de Unity.** Estas guías se verificaron con Unity 6000.5+ y Feel 6.1. Si el proyecto usa una versión menor, avisa y confirma que las APIs existan antes de asumirlas.
3. **Instalar paquetes cambia el proyecto**: pregunta antes (Cinemachine, TextMesh Pro/uGUI 2.x, Post Processing). Algunos feedbacks solo aparecen si su paquete está instalado.
4. **Nunca edites archivos dentro de `Assets/Feel/`** (ni assets por defecto del sound manager): una actualización los pisa. Duplica los assets y guarda tus scripts en tu propia carpeta (ej. `Assets/Scripts/Feedbacks/`).
5. **Namespaces**: `using MoreMountains.Feedbacks;` para `MMF_Player`/feedbacks/springs, `using MoreMountains.Tools;` para `MMSoundManager`, playlists y tweens, y `using MoreMountains.FeedbacksForThirdParty;` para Cinemachine. `MoreMountains.Tools` se referencia solo desde Assembly-CSharp; dentro de un `.asmdef` propio hay que añadir la referencia.
6. **Escenas**: tras cambios por editor script usa `Undo.RecordObject`/`EditorUtility.SetDirty` y `EditorSceneManager.MarkSceneDirty`. Las acciones "Automatic Shaker Setup" pueden re-parentar la cámara o crear canvas: guarda la escena y ten el proyecto bajo control de versiones primero.
7. **Si hay MCP for Unity disponible** (skill `unity-mcp-orchestrator`), úsalo para crear GameObjects/componentes y asignar referencias en el editor; los editor scripts de estas referencias sirven para saber qué tipos y propiedades tocar.
8. **Verifica al terminar**: sin errores en la Console, sin iconos de advertencia en los feedbacks (`EvaluateRequiresSetup()`), y prueba en Play mode.

## Conceptos clave (para decidir rápido)

- **`MMF_Player`** (un solo componente por GameObject) contiene `FeedbacksList`. Por defecto todos los feedbacks arrancan a la vez (`Parallel`); `Sequential`, `Pause` y `Looper` cambian eso.
- **Feedbacks "directos"** (Scale, Position, Particles, Flicker, Sound…) actúan sobre un target que les asignas. **Feedbacks "de broadcast"** (camera shake, zoom, fade, flash, post-processing, springs por canal) mandan un evento y necesitan un **receptor/shaker** en la escena; si no hay receptor, "no pasa nada". Casi siempre el botón **Automatic Shaker Setup** lo resuelve.
- **Canales** (`Channel` + `ChannelMode`) permiten que un feedback llegue solo a ciertos receptores (varias cámaras, UI vs mundo, varios reproductores de música).
- **Intensidad**: `PlayFeedbacks(position, intensity)` multiplica la fuerza de los feedbacks (1 = normal).
- **`FeedbackTypeAuthorized`** (bool estático por clase) y `MMFeedbacks.GlobalMMFeedbacksActive` apagan tipos de feedback o todo: base de los toggles de accesibilidad.
- **Cuándo usar qué**:
  - Efecto con duración fija sobre una propiedad → feedback con curva (feedbacks.md).
  - Movimiento elástico que se asienta solo y acumula golpes → spring (springs.md).
  - Cámara o pantalla completa → screen-shake.md.
  - Una sola cosa que no existe → feedback propio (custom-feedback.md), pero antes busca si ya hay uno (`GameObject > Property`, `Events > Unity Events`, `Renderer > ShaderController`, `Renderer > Material Set Property`).

## Flujo de trabajo recomendado

1. Identifica el área (tabla del router) y confirma instalación + versión.
2. Elige la ruta técnica y dile al usuario cuál elegiste y por qué (sobre todo en cámara: Cinemachine vs rig).
3. Crea el `MMF_Player` (hijo del objeto que reacciona, nombrado por el evento: `HitFeedback`, `JumpFeedback`) y añade los feedbacks.
4. Arregla toda advertencia de setup (target sin asignar, receptor faltante).
5. Conecta el disparo (código, UnityEvent, animación, Timeline, `AutoPlayOnStart`).
6. Prueba en Play mode con el botón Play del inspector y confirma el resultado esperado de la referencia.
