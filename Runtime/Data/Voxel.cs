using UnityEngine;

namespace Miventech.NativeVoxReader.Data
{
    // Represents a single voxel in local coordinates
    [System.Serializable]
    public struct Voxel
    {
        public byte x;
        public byte y;
        public byte z;
        public byte colorIndex; // Palette index (1-255)

        public Voxel(byte x, byte y, byte z, byte colorIndex)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.colorIndex = colorIndex;
        }
    }
}


