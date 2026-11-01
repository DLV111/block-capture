# PlayerController.cs — Smooth Touch Controls for Block Capture

## Overview
The `PlayerController` class handles player movement in Block Capture. The player moves at a constant speed in 2D space, with continuous steering toward the user's touch/drag position. Implements smooth input handling for mobile touch controls while maintaining low memory allocation during gameplay.

## Requirements
- **Core Movement**: Player moves forward at constant speed (e.g., 5 units/second) with continuous steering toward touch/drag position
- **Touch Input**: Handle multi-touch input (single touch for player movement, potential multi-touch for UI)
- **Steering Logic**: Continuous direction adjustment based on touch position (not discrete steps)
- **Mobile Optimization**: 
  - Use pooled array buffers for trail vector storage to prevent GC spikes
  - Minimize allocations during movement calculations
  - Avoid expensive per-frame allocations in movement logic
- **Collision Prevention**: Prevent self-intersection with own trail (separate from tail collision detection)

## Technical Approach
- **Input Handling**: Use Unity's `Input.touchPosition` with smoothing (lerp) for touch position tracking
- **Movement Vector**: Calculate direction vector from player position to touch position, normalize and apply constant speed
- **Steering Smoothing**: Use a small time-based interpolation (e.g., `Mathf.Lerp`) to smooth direction changes
- **Memory Management**: 
  - Pre-allocate trail vector buffers with `ArrayPool<T>.Shared.Rent()` 
  - Reset buffers after use to avoid allocations
  - Use structs instead of classes for movement data where possible

## Implementation Notes
- Movement should be frame-rate independent (use `Time.deltaTime`)
- Steering should feel responsive but not twitchy (configurable smoothing factor)
- Trail vectors should be managed separately from movement logic to avoid coupling
- Consider using Unity's new Input System for better touch handling (but keep fallback for legacy)

## Acceptance Criteria
- [ ] Player moves at constant speed with smooth steering toward touch position
- [ ] No perceptible GC spikes during movement (verified via Unity Profiler)
- [ ] Trail vectors use pooled buffers without memory leaks
- [ ] Works on low-end Android devices (tested on API 24+)
- [ ] Code compiles with Unity 2022.3 LTS

## Dependencies
- Unity 2D Physics
- System.Collections.Generic (for pooled buffers)
- UnityEngine.InputSystem (preferred) or legacy Input class