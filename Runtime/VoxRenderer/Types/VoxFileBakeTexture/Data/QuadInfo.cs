using UnityEngine;
namespace Miventech.NativeVoxReader.Tools.VoxFileBakeTexture.Data
{
    public class QuadInfo
    {
        public Vector3 v0, v1, v2, v3;
        public Color32[] colors; // Array of colors for the texture
        public int width;
        public int height;
        public int faceDir;
    }
}

