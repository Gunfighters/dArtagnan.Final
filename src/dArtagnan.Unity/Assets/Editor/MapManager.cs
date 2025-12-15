using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Editor
{
    [CustomEditor(typeof(Tilemap))]
    public class MapManager : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Generate Map"))
            {
                var map = (Tilemap)target;
                var bounds = map.cellBounds;
                var bitMask = new int[bounds.size.x, bounds.size.y];
                foreach (var v in bounds.allPositionsWithin)
                {
                    var localPos = v - bounds.position;
                    bitMask[localPos.x, localPos.y] = map.HasTile(v) ? 1 : 0;
                }
                var serialized = JsonConvert.SerializeObject(bitMask);
                var path = EditorUtility.SaveFilePanel("Save the map", null, map.transform.parent.gameObject.name + "." + map.gameObject.name, "json");
                if (path.Length > 0)
                    File.WriteAllText(path, serialized);
            }
        }
    }
}