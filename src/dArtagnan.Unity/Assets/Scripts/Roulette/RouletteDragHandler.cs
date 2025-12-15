using UnityEngine;
using UnityEngine.EventSystems;

namespace Roulette
{
    public class RouletteDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private float rotationSensitivity;
        [SerializeField] private float velocityDamping;
        
        private Vector2 _lastPosition;
        private float _currentVelocity;
        private bool _isDragging;
        private RectTransform _rectTransform;
        private float _startAngle;
        private float _initialRotation;
        private float _lastAngle;
        private float _lastTime;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;
            
            Vector2 center = _rectTransform.position;
            Vector2 startDirection = eventData.position - center;
            _startAngle = Mathf.Atan2(startDirection.y, startDirection.x) * Mathf.Rad2Deg;
            _initialRotation = transform.eulerAngles.z;
            _lastAngle = _startAngle;
            _lastTime = Time.time;
            
            _currentVelocity = 0;
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 center = _rectTransform.position;
            Vector2 currentDirection = eventData.position - center;
            float currentAngle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;
            
            // Calculate velocity for inertia
            float angleDelta = Mathf.DeltaAngle(_lastAngle, currentAngle);
            float timeDelta = Time.time - _lastTime;
            if (timeDelta > 0)
            {
                _currentVelocity = angleDelta / timeDelta;
            }
            
            // Calculate total rotation from start
            float totalRotation = Mathf.DeltaAngle(_startAngle, currentAngle);
             
            // Set absolute rotation
            transform.rotation = Quaternion.Euler(0, 0, _initialRotation + totalRotation);
            
            _lastAngle = currentAngle;
            _lastTime = Time.time;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;
        }

        private void Update()
        {
            if (!_isDragging && Mathf.Abs(_currentVelocity) > 0.1f)
            {
                _currentVelocity *= velocityDamping;
                transform.Rotate(0, 0, _currentVelocity * Time.deltaTime);
            }
        }
    }
}