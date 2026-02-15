# Changelog
All notable changes to this project will be documented in this file.

## [1.0.0] - 2026-01-17
### Added
- Initial release.
- Scripted Importer for `.vox` files.
- Support for MagicaVoxel hierarchy (`nTRN`, `nGRP`, `nSHP`).
- Greedy Meshing for optimized geometry.
- Texture baking support for reduced draw calls.
- Assembly Definitions for faster compilation.

## [1.1.0] - 2026-02-15
### Added
- **Dynamic Rendering System**: New modular architecture for voxel rendering.
- **Multiple Rendering Modes**: Choose between "Baked Texture" (Atlas optimized) or "Palette Style" (Classic UV mapping).
- **Extensible Settings**: Integrated `[SerializeReference]` for dynamic, per-renderer configurations in the Inspector.
- **Custom Importer Editor**: Fully revamped Inspector for `.vox` assets that automatically discovers and lists available renderers.
- **Improved API**: New base classes `VoxRenderAbstract` and `VoxRenderSettings` for easier developer extension.
- **Organized Codebase**: Refactored internal tools and utilities for better maintenance.
