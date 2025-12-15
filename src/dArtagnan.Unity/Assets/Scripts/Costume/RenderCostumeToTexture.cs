using System;
using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Costume
{
    [RequireComponent(typeof(RawImage))]
    public class RenderCostumeToTexture : MonoBehaviour, IDragHandler
    {
        [SerializeField] private Camera previewCam;
        public Character4D character;
        public RenderTexture renderTexturePrefab;
        public Transform pivot;
        public RawImage RawImage { get; private set; }
        public float dragSensitivity;
        
        private float totalDragX = 0f;
        private readonly Vector2[] _directions = { Vector2.down, Vector2.right, Vector2.up, Vector2.left };

        private void Awake()
        {
            RawImage = GetComponent<RawImage>();
            var r = Instantiate(renderTexturePrefab, pivot);
            previewCam.targetTexture = r;
            RawImage.texture = r;
            character.HardReset();
            previewCam.transform.SetParent(null);
        }

        private void LateUpdate()
        {
            previewCam.transform.localScale = Vector3.one;
        }

        public void OnDrag(PointerEventData eventData)
        {
            totalDragX += eventData.delta.x;
            
            float sensitivity = dragSensitivity;
            int directionIndex = Mathf.FloorToInt(totalDragX / sensitivity) % 4;
            if (directionIndex < 0) directionIndex += 4;
            
            character.SetDirection(_directions[directionIndex]);
        }
    }
}