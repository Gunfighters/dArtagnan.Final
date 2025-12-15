using System.Collections.Generic;
using dArtagnan.Shared;
using R3;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game.Map
{
    public class MapContainer : MonoBehaviour
    {
        public SerializableReactiveProperty<int> mapIndex;
        public List<MapContainerItem> maps;
        public Subject<Tilemap> NewTilemapSelected = new();

        public void Awake()
        {
            mapIndex.Where(i => i != 0).Subscribe(newIndex =>
            {
                for (var i = 0; i < maps.Count; i++)
                {
                    var g = maps[i].grid;
                    g.gameObject.SetActive(maps[i].index == newIndex);
                    if (maps[i].index == newIndex)
                        NewTilemapSelected.OnNext(g.GetComponentInChildren<Tilemap>());
                }
            });
            PacketChannel.On<MapData>(newData => mapIndex.Value = newData.MapIndex);
        }
    }
}