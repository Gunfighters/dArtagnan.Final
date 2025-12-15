#if UNITY_EDITOR
using UnityEditor;

namespace Utils
{
    public class DefaultTextureSizer : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            var texImporter = (TextureImporter) assetImporter;
            texImporter.maxTextureSize = 1024;
        }
    }
}
#endif