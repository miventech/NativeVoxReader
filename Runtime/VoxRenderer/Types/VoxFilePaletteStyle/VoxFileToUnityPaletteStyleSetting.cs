using UnityEngine;
using Miventech.NativeVoxReader.VoxRenderer.Types;

namespace Miventech.NativeVoxReader.Tools.VoxFilePaletteStyle
{
    [System.Serializable]
    public class VoxFileToUnityPaletteStyleSetting : VoxRenderSettings
    {
        public float Scale = 0.1f;
        public Texture CustomTextureAtlas;
    }
}
