using UnityEngine;

namespace Miventech.NativeVoxReader.Data
{
    // Represents an individual model within the VOX file (corresponds to SIZE and XYZI chunks)
    [System.Serializable]
    public class VoxModel
    {
        public Vector3Int size; // Model dimensions
        public Vector3Int position; // Model position in the world
        public Voxel[] voxels;  // List of voxels it contains

        public VoxModel()
        {
            size = Vector3Int.zero;
            position = Vector3Int.zero;
            voxels = new Voxel[0];
        }
    }
}


