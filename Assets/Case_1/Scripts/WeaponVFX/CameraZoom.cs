using UnityEngine;
using UnityEngine.InputSystem;

namespace VertigoCase.WeaponVFX
{
    /// <summary>
    /// Fare tekerleği (Mouse Wheel) ile kameranın yakınlaşmasını/uzaklaşmasını (Zoom In/Out) sağlayan betik.
    /// Yeni Unity Input System ile uyumludur.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraZoom : MonoBehaviour
    {
        [Header("Zoom Ayarları")]
        [Tooltip("Her tekerlek hareketinde zoom yapma adımı")]
        [SerializeField] private float _sensitivity = 5f;

        [Tooltip("Minimum Zoom Limiti (FOV / Orthographic Size)")]
        [SerializeField] private float _minZoom = 20f;

        [Tooltip("Maksimum Zoom Limiti (FOV / Orthographic Size)")]
        [SerializeField] private float _maxZoom = 70f;

        [Tooltip("Pürüzsüz geçiş (Smooth) süresi")]
        [SerializeField] private float _smoothTime = 0.15f;

        private Camera _camera;
        private float _targetZoom;
        private float _currentZoom;
        private float _zoomVelocity;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            
            // Başlangıç zoom değerini kameranın tipine göre belirle
            if (_camera.orthographic)
            {
                _currentZoom = _camera.orthographicSize;
            }
            else
            {
                _currentZoom = _camera.fieldOfView;
            }
            _targetZoom = _currentZoom;
        }

        private void Update()
        {
            HandleInput();
            ApplyZoom();
        }

        private void HandleInput()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            // Fare tekerleğinin hareket değerini al
            float scrollValue = mouse.scroll.ReadValue().y;
            
            if (Mathf.Abs(scrollValue) > 0.01f)
            {
                // Yukarı kaydırma (zoom-in) pozitif, aşağı kaydırma (zoom-out) negatiftir.
                // İşletim sistemi veya donanım farkından etkilenmemek için scroll değerinin sadece yönünü (işaretini) alıyoruz.
                float scrollDirection = Mathf.Sign(scrollValue);
                
                // direction > 0 ise yukarı (yakınlaşma), direction < 0 ise aşağı (uzaklaşma)
                _targetZoom -= scrollDirection * _sensitivity;
                _targetZoom = Mathf.Clamp(_targetZoom, _minZoom, _maxZoom);
            }
        }

        private void ApplyZoom()
        {
            if (Mathf.Abs(_targetZoom - _currentZoom) > 0.01f)
            {
                _currentZoom = Mathf.SmoothDamp(_currentZoom, _targetZoom, ref _zoomVelocity, _smoothTime);
                
                if (_camera.orthographic)
                {
                    _camera.orthographicSize = _currentZoom;
                }
                else
                {
                    _camera.fieldOfView = _currentZoom;
                }
            }
        }
    }
}

