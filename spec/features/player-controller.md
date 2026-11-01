# PlayerController.cs — Continuous Steering & Trail Logging for Paper.io 2 Clone

## Overview
The `PlayerController` class handles player movement, continuous directional steering, and zero-allocation trail logging for a **Paper.io 2** style gameplay loop. 

The player character moves forward continuously at a constant speed in 2D world space. The user controls direction via touch steering, constrained by a maximum angular rotation rate ($\omega_{\text{max}}$) that guarantees a minimum turning radius. This turning radius naturally prevents tight hairpin self-intersections with the player's immediate unclosed line.

---

## Technical Specifications & Mathematical Models

### 1. Constant Forward Velocity & Heading
The player operates on a continuous velocity vector defined by a scalar speed $V$ and a normalized direction vector $\mathbf{d}_t = (\cos\theta_t, \sin\theta_t)^T$.

$$\mathbf{p}_{t+\Delta t} = \mathbf{p}_t + V \cdot \mathbf{d}_t \cdot \Delta t$$

- **Constant Speed ($V$)**: Player cannot stop, slow down, or accelerate. Default: $5.0 \text{ units/sec}$.
- **Unstoppable Motion**: If touch input is released, $\mathbf{d}_t$ remains fixed at its current heading angle $\theta_t$.

---

### 2. Angular Velocity-Limited Steering
Target direction $\theta_{\text{target}}$ is calculated based on relative input drag vector $\mathbf{u} = (u_x, u_y)^T$:

$$\theta_{\text{target}} = \text{atan2}(u_y, u_x)$$

To eliminate instantaneous $180^\circ$ turns and prevent self-intersection with the immediate trail, heading rotation is strictly clamped per frame by maximum angular speed $\omega_{\text{max}}$:

$$\theta_{t+\Delta t} = \text{MoveTowardsAngle}(\theta_t, \theta_{\text{target}}, \omega_{\text{max}} \cdot \Delta t)$$

$$\mathbf{d}_{t+\Delta t} = \begin{pmatrix} \cos\theta_{t+\Delta t} \\ \sin\theta_{t+\Delta t} \end{pmatrix}$$

#### Minimum Turning Radius Guarantee
The minimum turning radius $R_{\text{min}}$ is mathematically bounded by:

$$R_{\text{min}} = \frac{V}{\omega_{\text{max}}}$$

> **Constraint**: $\omega_{\text{max}}$ must be calibrated such that $2 \cdot R_{\text{min}} > w_{\text{trail}}$ (where $w_{\text{trail}}$ is the trail line width) to physically prevent self-overlapping turns.

---

### 3. Touch Input Handling
- **Relative Drag Control (Primary)**: Direction is determined by the displacement vector from initial touch point $\mathbf{T}_{\text{start}}$ to current touch point $\mathbf{T}_{\text{current}}$. This prevents hand occlusion over the player's position.
- **Dead Zone Threshold**: If $|\mathbf{T}_{\text{current}} - \mathbf{T}_{\text{start}}| < r_{\text{deadzone}}$, maintain current heading $\theta_t$ without updating $\theta_{\text{target}}$ to avoid micro-jitter.

---

### 4. Zero-GC Trail Vertex Logging
To eliminate Garbage Collection (GC) spikes while recording trail points for polygon triangulation and line rendering:

1. **Memory Allocation**:
   - Pre-allocate trail vector memory using `NativeArray<Vector2>` allocated with `Allocator.Persistent` during `Awake()`.
   - Dispose persistent buffers strictly inside `OnDestroy()`.
   - Use `NativeSlice<Vector2>` for passing temporary point sequences to line renderers or collision processors without copying data.

2. **Vertex Optimization (Downsampling)**:
   Points are **not** appended every frame. A new vertex $\mathbf{p}_{\text{new}}$ is logged to the buffer only when:
   - **Distance Threshold**: $|\mathbf{p}_{\text{current}} - \mathbf{p}_{\text{last}}| \ge d_{\text{min}}$
   - **Angular Threshold**: $|\theta_{\text{current}} - \theta_{\text{last}}| \ge \Delta\theta_{\text{min}}$

---

## Acceptance Criteria

- [ ] **Unstoppable Speed**: Player moves forward at a constant speed ($V = 5.0 \text{ units/sec}$) in all frames. Releasing touch controls maintains the current forward vector.
- [ ] **Clamped Turn Rate**: Steering angle changes are capped at $\omega_{\text{max}}$ ($\deg/\text{sec}$), preventing hairpin turns and immediate self-overlap.
- [ ] **Relative Drag Input**: Touch control calculates direction from relative touch offsets; minor touch movement within the dead zone ($r_{\text{deadzone}}$) leaves direction unchanged.
- [ ] **Zero GC Allocations**: Zero bytes allocated during steady-state movement (`Update` loop verified in Unity Profiler GC Alloc monitor).
- [ ] **Optimized Trail Buffer**: Trail vertices log only on reaching $d_{\text{min}}$ or $\Delta\theta_{\text{min}}$ thresholds using a pre-allocated `NativeArray<Vector2>`.
- [ ] **Lifecycle Memory Cleanup**: `NativeArray` memory is explicitly disposed in `OnDestroy()` without memory leaks across scene reloads.

---

## Dependencies
- `UnityEngine.InputSystem`
- `Unity.Collections` (`NativeArray<T>`, `NativeSlice<T>`)
- `Unity.Mathematics` (`math.atan2`, `math.cos`, `math.sin`)