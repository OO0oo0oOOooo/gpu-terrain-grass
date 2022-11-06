# Simple-Terrain-Grass-GPU
Very basic GPU instatiation of a mesh to a specified terrain paint texture layer for the terrain this script is placed on. - Graphics.DrawMeshInstancedIndirect


How to use:
1. Place script on unity terrain.
2. Create a material and set the shader to the one provided. (It is very basic and only can set the individual meshs provided color from the compute buffer)
3. Set the mesh of the object you want instatiated
4. Set layerIndex to terrain paint layer index you want the grass to be instatiated on.
5. Population is the amount of meshes instatiated. (The total amount before the terrain alphamap is sampled and culled)
6. Grass Threshhold is the blend weight of the texture layer selected.
7. Range Rect Padding is the distance from the edge of the terrain that meshes will be sampled. (guards from getting an outofrange error while checking the aplhamap) (if this is 0 it also will skip the current iteration but is slower)
