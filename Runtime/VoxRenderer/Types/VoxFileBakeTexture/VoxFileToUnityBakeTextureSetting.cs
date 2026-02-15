using UnityEngine;
using Miventech.NativeVoxReader.VoxRenderer.Types;

namespace Miventech.NativeVoxReader.Tools.VoxFileBakeTexture
{
    [System.Serializable]
    public class VoxFileToUnityBakeTextureSetting : VoxRenderSettings
    {
        public int maxAtlasSize = 4096;
        public int maxQuadSize = 48;
        public float Scale = 0.1f;
    }
}

