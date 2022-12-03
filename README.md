# Simple-Terrain-Grass-GPU
Very basic GPU instatiation of a mesh to a specified terrain paint texture layer for the terrain this script is placed on. - Graphics.DrawMeshInstancedIndirect


# How To Use
* Place script on unity terrain.
* Create a material and set the shader to the one provided.
* Set the mesh of the object you want instatiated.
* Set layerIndex to terrain paint layer index you want the grass to be instatiated on.
* Population is the amount of meshes instatiated. (The total amount before the terrain alphamap is sampled and culled)
* Grass Threshhold is the blend weight of the texture layer selected.
