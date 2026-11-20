# Casino Anomaly: floor material proof of concept

This independent Godot 4.5 project tests a physically based oak-plank floor material and basic first-person movement. It is **not yet integrated** into the Windows casino shell.

The source asset is [Oak Wood Planks](https://polyhaven.com/a/oak_wood_planks), created by Dimitrios Savva and distributed by Poly Haven under [CC0](https://polyhaven.com/license). The material uses the 2K diffuse (JPG), roughness (JPG), and OpenGL normal (PNG) maps. Source map downloads are stored under `assets/oak_wood_planks/` by the one-time test workflow. Provenance and SHA-256 values are written to `assets/oak_wood_planks/source.json`.

Godot automatically imports the image files as textures. The floor consists of a 14 × 14 meter box mesh, with UV tiling selected to approximately match the asset's 1.2-meter physical width. The scene offers WASD movement and mouse look, with Escape releasing the pointer.

This is an asset-import and material-preview test, not the final casino room design.
