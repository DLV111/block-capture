# Block Capture — Paper.io 2 Inspired Game Design Spec

## Overview
Block Capture is an original territory-capture game inspired by the Paper.io 2 concept but created to avoid copyright concerns. Players compete to capture territory by drawing continuous trails that expand their controlled area.

## Core Features
- **Smooth Touch Controls**: Players move at constant speed, steering toward touch/drag positions
- **Dynamic Territory Management**: Real-time polygon mesh generation and union operations
- **Collision Detection**: Precise line intersection math to prevent self-intersection
- **Mobile Optimization**: Efficient memory management to avoid GC spikes

## Technical Architecture
- **PlayerController.cs**: Touch input handling and movement logic
- **TerritoryManager.cs**: Polygon mesh generation and union operations
- **TailCollision.cs**: Line intersection detection for collision prevention
- **C# Clipper Integration**: Polygon union operations for territory expansion

## Development Pipeline
- Planned with DeepSeek V3 (free) coding and manual review with Gemini/GPT
- Kanban board: `paper-io-2-unity` (now migrated to `block-capture`)
- Spec documents living in `spec/` folder
- Mobile-first approach with Android optimization

## Next Steps
1. Implement PlayerController.cs with touch controls
2. Create TerritoryManager.cs with polygon mesh system
3. Develop TailCollision.cs with line intersection logic
4. Optimize for mobile performance
5. Test and iterate based on gameplay feedback

Review with Gemini and GPT manually.
Coded by DeepSeek V3 (free).
