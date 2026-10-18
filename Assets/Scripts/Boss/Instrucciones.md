# Guía paso a paso para armar la escena del Boss desde cero

Esta es una guía práctica para construir la escena manualmente, sin depender de una plantilla ya armada. Está pensada para que puedas ir diseñando el escenario, preparar al jugador y dejar el Boss listo para probar.

---

## 1. Objetivo general

Necesitamos dejar la escena con estos elementos:

- un escenario de arena para el boss
- un jugador con controles, vida y movimiento
- un boss con vida, IA y fases
- una arena con puntos izquierdo/derecho
- un patrón de ataque con patrulla y ráfaga de proyectiles
- un overlay de ceguera para el jugador
- una prueba de gameplay rápida para validar que todo funcione

---

## 2. Preparar la escena base

### 2.1 Crear una escena limpia
- Abrir una escena nueva.
- Renombrarla como: `Scene_BossTest`.
- Elegir un fondo oscuro o un fondo de arena.
- Dejar la resolución y los ajustes de cámara según la cámara de juego.

### 2.2 Diseñar el suelo y la arena
Crea un escenario simple con:
- un suelo recto
- una pared o límites laterales
- un espacio amplio para el boss
- una altura suficiente para que se vea bien el movimiento

Recomendado:
- suelo largo, por ejemplo 20–30 unidades
- pared izquierda y derecha
- altura del piso estable
- margen para que el boss camine entre dos puntos

### 2.3 Crear la estructura visual
- Añadir un GameObject vacío llamado `Arena_Boss`
- Dentro:
  - `Ground`
  - `Walls`
  - `Decoracion`
- Si el juego es 2D, conviene que todo quede en una sola capa visual y consistente.

### 2.4 Dejar una referencia clara del espacio
Lo más importante es que el boss tenga un espacio definido:
- izquierda
- derecha
- centro
- zona donde el jugador se mueve

Esto te va a servir para armar su movimiento y ataques.

---

## 3. Preparar al jugador

### 3.1 Crear el Player
- Crear un GameObject llamado `Player`
- Agregar:
  - `SpriteRenderer`
  - `Rigidbody2D`
  - `BoxCollider2D`
  - `PlayerController`
  - `PlayerHealth`
  - `PlayerStatusEffects`

### 3.2 Configurar el Rigidbody
Para que el jugador se mueva bien:
- `Body Type`: Dynamic
- `Gravity Scale`: valor normal del proyecto
- `Constraints`: Freeze Rotation Z
- `Collision Detection`: Continuous

### 3.3 Configurar el suelo
- En `PlayerController`, asignar:
  - `groundCheck`
  - `groundCheckSize`
  - `groundLayer`
- Si el proyecto ya tiene un layer para suelo, usarlo.

### 3.4 Configurar la vida
- `PlayerHealth` ya debe tener:
  - vida base
  - eventos de daño, muerte, revive
- Si el proyecto usa `Health` base con `UnityEvent`, dejarlo tal como está.

### 3.5 Añadir el efecto de ceguera
- En el jugador, agregar:
  - `PlayerStatusEffects`
- Esto será el punto central para:
  - aplicar ceguera
  - activar overlay
  - enviar eventos de inicio y fin

### 3.6 Crear overlay visual
- Crear un `Canvas` por separado:
  - nombre: `Canvas_Blind`
  - `Render Mode`: Screen Space - Overlay
  - `Sorting Order`: alto
- Dentro:
  - `Overlay` (Image)
  - `CanvasGroup`
  - `BlindOverlay`

Configuración:
- Image color negro
- stretch full screen
- `Raycast Target`: desactivar
- `CanvasGroup.alpha`: 0
- `BlindOverlay` apuntando al `PlayerStatusEffects`

Esto hace que la pantalla se oscurezca cuando el jugador queda ciego.

---

## 4. Crear la arena del boss

### 4.1 Crear un GameObject `BossArena`
- GameObject vacío
- Añadir componente `BossArena`

### 4.2 Agregar los puntos de recorrido
- Dentro de `BossArena`, crear dos hijos vacíos:
  - `Left`
  - `Right`
- Posicionarlos a ambos lados de la arena, sobre el suelo
- Distancia recomendada: 8–12 unidades entre ambos

### 4.3 Asignar los puntos
En `BossArena`:
- arrastrar `Left` al campo `LeftPoint`
- arrastrar `Right` al campo `RightPoint`

Esto permite que el boss se mueva entre extremos sin hardcodear valores.

---

## 5. Crear el boss base

### 5.1 Crear el GameObject `Boss_Walker`
- Crear GameObject vacío
- Añadir:
  - `SpriteRenderer`
  - `Rigidbody2D`
  - `BoxCollider2D`
  - `BossHealth`
  - `ContactDamage`
  - `BossController`

### 5.2 Configurar el Rigidbody
- `Body Type`: Dynamic
- `Gravity Scale`: 0 o el valor apropiado según el proyecto
- `Freeze Rotation`: true
- `Linear Drag`: 0
- `Angular Drag`: 0

### 5.3 Configurar las colisiones
- `BoxCollider2D`
- `Is Trigger`: si lo necesitas para contacto especial
- `ContactDamage` ya se encarga de detectar contacto

### 5.4 Configurar `BossHealth`
- Asignar un valor de salud para el jefe
- Setear:
  - `behavioursToDisableOnDeath` con el `BossController`
  - `knockbackMultiplier = 0` si querés que el boss no rebote
- Esto ayuda a evitar que el boss termine volando al recibir golpes.

### 5.5 Configurar `ContactDamage`
- `ContactDamage` debe apuntar a la capa del jugador
- `targetLayers` = layer del `Player`

### 5.6 Configurar `BossController`
En el componente:
- asignar `arena`
- asignar `sprite`
- opcional `animator`
- asignar `contactDamage`
- asignar `projectileSpawn`

`projectileSpawn` puede ser un hijo del boss, por ejemplo:
- `ProjectileSpawn`

Esto permite que los proyectiles salgan desde una posición exacta.

---

## 6. Definir la lógica del boss

### 6.1 La idea del patrón
El boss no se mueve “a mano” con una IA gigante. Se arma con un patrón.

El patrón se compone de acciones:
- esperar
- mover
- patrullar
- disparar ráfaga
- etc.

Todo eso se organiza en una lista llamada `BossPattern`.

### 6.2 Crear el patrón
- Crear un ScriptableObject `BossPattern`
- Nombre sugerido:
  - `BossPattern_Walker`

### 6.3 Crear la primera fase
En `BossController`:
- `phases` = 1 elemento
- `name`: `Fase 1`
- `startsAtHealthPercent`: `100`
- `pattern`: asignar `BossPattern_Walker`
- `speedMultiplier`: `1`
- `transitionInvulnerableTime`: `1`

Esto significa:
- al empezar el combate, entra en la fase 1
- ejecuta el patrón
- se repite mientras el boss siga vivo

---

## 7. Armar el patrón del boss

### 7.1 Acción 1: Patrulla
Agregar una acción:
- `PatrolAction`

Parámetros sugeridos:
- `passes = 2`
- `speed = 5`
- `pauseAtEdges = 0.3`
- `damageOnContact = true`

Esto hace que:
- el boss camine entre izquierda y derecha
- haga daño al tocar al jugador
- pause un momento en cada extremo

### 7.2 Acción 2: Espera
Agregar:
- `WaitAction`

Parámetros:
- `duration = 0.5f`

Esto sirve para darle ritmo al patrón y separar movimientos.

### 7.3 Acción 3: Ráfaga de proyectiles
Agregar:
- `ProjectileBurstAction`

Parámetros recomendados:
- `count = 5`
- `spreadAngle = 50`
- `aim = AtPlayer`
- `projectileSpeed = 8`
- `intervalBetweenShots = 0`
- `telegraphDuration = 0.8`
- `blind.applyBlind = true`
- `blind.duration = 3`
- `blind.intensity = 1`

Esto hará que:
- el boss se avise visualmente
- tire varios proyectiles hacia el jugador
- si impactan, el jugador reciba daño y ceguera

### 7.4 Acción 4: Espera final
Agregar otra espera:
- `WaitAction`
- `duration = 1f`

Esto hace que el patrón tenga un cierre antes de volver a empezar.

### 7.5 Configurar loop
En el `BossPattern`:
- `loopMode = Loop`
- `delayBetweenLoops = 0.5f`

Resultado:
- el patrón se repite en bucle
- no se queda estático

---

## 8. Configurar el proyectil del boss

### 8.1 Crear un prefab de proyectil
- Crear un GameObject `BossProjectile`
- Añadir:
  - `SpriteRenderer`
  - `Rigidbody2D`
  - `CircleCollider2D`
  - `BossProjectile`

### 8.2 Configurar el Rigidbody
- `Body Type`: Dynamic
- `Gravity Scale`: 0
- `Collision Detection`: Continuous
- `Is Trigger`: yes, en el collider

### 8.3 Configurar los layers
- `targetLayers`: layer del jugador
- `obstacleLayers`: capa del suelo/paredes

Esto hace que:
- si toca al jugador, hace daño y aplica ceguera
- si toca un obstáculo, desaparece

### 8.4 Guardarlo como prefab
- Guardar en:
  - `Assets/Prefabs/Boss/BossProjectile.prefab`

### 8.5 Asignarlo al patrón
En la acción `ProjectileBurstAction`:
- arrastrar el prefab al campo `projectilePrefab`

---

## 9. Configurar el combate activo

### 9.1 Activar el boss al inicio
En `BossController`:
- dejar `autoStart = true`

Esto hace que al entrar en la escena el combate arranque solo.

### 9.2 Verificar que busca al jugador
`BossController` busca al `Player` con tag `Player`:
- el objeto del jugador debe tener el tag `Player`

### 9.3 Comprobar que hay arena
- `BossArena` debe estar asignado
- `projectileSpawn` debe estar correcto

---

## 10. Verificar el flujo de juego

### 10.1 Probar la patrulla
- entrar en play
- ver que el boss camina de un lado al otro
- comprobar que solo hace daño durante la patrulla

### 10.2 Probar el telegraph
- antes de patrullar debe ponerse amarillo
- antes de disparar debe ponerse amarillo durante un tiempo

Esto confirma que:
- la acción de telegraph está funcionando
- hay una advertencia visual antes del daño

### 10.3 Probar la ráfaga
- el boss dispara 5 proyectiles
- los proyectiles intentan impactar al jugador
- si impactan:
  - daña
  - aplica ceguera
  - empieza el fade del overlay

### 10.4 Probar el ciclo
- dejar el boss activo 30–60 segundos
- comprobar que:
  - se repite
  - no hay errores
  - no se acumulan coroutines
  - no quedan objetos colgando

### 10.5 Probar muerte
- reducir la vida del boss hasta 0
- confirmar que:
  - deja de patrullar
  - no se siguen disparando proyectiles
  - no quedan coroutines de movimiento
  - no se generan errores en consola

---

## 11. Listado final de la escena mínima

Si quieres dejar la escena funcionando con lo mínimo necesario, este es el checklist final:

- [ ] Escena nueva creada
- [ ] Suelo y arena armados
- [ ] Jugador con movimiento y vida
- [ ] Tag `Player` en el jugador
- [ ] BossArena con Left y Right
- [ ] Boss con `BossController`
- [ ] BossHealth configurado
- [ ] ContactDamage apuntando al Player
- [ ] ScriptableObject `BossPattern`
- [ ] Fase 1 con patrón del boss
- [ ] `PatrolAction` en el patrón
- [ ] `WaitAction` en el patrón
- [ ] `ProjectileBurstAction` en el patrón
- [ ] Proyectil prefab asignado
- [ ] `PlayerStatusEffects` agregado
- [ ] Overlay visual de ceguera armado
- [ ] `autoStart` activado
- [ ] Prueba en Play
- [ ] Validación de daño, telegraph y ciclo

---

## 12. Orden recomendado de trabajo

Para no perderte, hazlo en este orden:

1. Diseñar la arena
2. Crear el jugador
3. Crear el boss
4. Crear el BossArena
5. Crear el patrón básico
6. Agregar Patrulla
7. Agregar Espera
8. Agregar Proyectiles
9. Crear el prefab del proyectil
10. Probar daño y ceguera
11. Ajustar timings y velocidad
12. Probar el ciclo completo
13. Ajustar balance

---

## 13. Consejo práctico

Si quieres hacerlo más rápido y claro:
- primero arma un boss que solo patrulla
- luego agrega la ráfaga de proyectiles
- luego agrega la ceguera
- luego repite en loop
- y al final revisas visuales, tiempos y balance

Eso te evita perder tiempo en un boss “completo” desde el primer intento.

---

## 14. Resumen breve

La idea central es esta:

- el escenario define la arena
- el jugador es el objetivo
- el boss usa un patrón de acciones
- cada acción es un bloque simple
- la fase del boss decide qué patrón usa
- la ceguera y los proyectiles son un efecto del patrón, no un sistema aparte

Cuando esto queda ordenado, el boss se vuelve fácil de reutilizar para otros jefes.

Si quieres, te puedo hacer ahora una segunda parte con:
- un “checklist de escena listo para copiar”
- o una versión aún más detallada con “qué poner en cada campo del inspector” para cada componente.