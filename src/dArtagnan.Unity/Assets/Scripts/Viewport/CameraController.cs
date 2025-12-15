using Cysharp.Threading.Tasks;
using Game;
using Game.Map;
using Game.Player.Components;
using R3;
using UnityEngine;
using UnityEngine.Rendering;

namespace Viewport
{
    [RequireComponent(typeof(Camera))]
    [RequireComponent(typeof(Volume))]
    public class CameraController : MonoBehaviour
    {
        private Volume _volume;
        public MapContainer MapContainer;
        private PlayerModel _targetModel;
        private Vector3 offset = new(0, 0, -10);
        private Camera cam;
        public float cameraMoveSpeed;
        private float height => cam.orthographicSize;
        private float width => height * cam.aspect;
        private Vector2 mapSize;
        private Vector2 center;

        private void Awake()
        {
            _volume = GetComponent<Volume>();
            cam = GetComponent<Camera>();
            MapContainer.NewTilemapSelected.Subscribe(newMap =>
            {
                mapSize = new Vector2(newMap.size.x * newMap.cellSize.x, newMap.size.y * newMap.cellSize.y) / 2;
                center = newMap.cellBounds.center;
                cam.transform.position = new Vector3(center.x, center.y, cam.transform.position.z);
            });
        }

        private void Start()
        {
            GameModel.Instance.CameraTarget
                .WhereNotNull()
                .Subscribe(newTarget => _targetModel = newTarget);
        }

        private void Update()
        {
            if (_targetModel is not null)
            {
                LimitCameraArea();
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(center, mapSize * 2);
        }

        private void LimitCameraArea()
        {
            transform.position = Vector3.Lerp(transform.position,
                _targetModel.transform.position + offset,
                Time.deltaTime * cameraMoveSpeed);
            var lx = mapSize.x - width;
            var clampX = Mathf.Clamp(transform.position.x, -lx + center.x, lx + center.x);

            var ly = mapSize.y - height;
            var clampY = Mathf.Clamp(transform.position.y, -ly + center.y, ly + center.y);

            transform.position = new Vector3(clampX, clampY, -10f);
        }
    }
}