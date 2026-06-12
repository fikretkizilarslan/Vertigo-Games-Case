using UnityEngine;
using UnityEngine.InputSystem;

namespace VertigoCase.WeaponVFX
{
    /// <summary>
    /// Kullanıcının ekrana tıklayıp sürükleyerek silahı kendi ekseninde döndürmesini sağlayan betik.
    /// Yeni Unity Input System ile uyumludur.
    /// </summary>
    [RequireComponent(typeof(ShowroomRotator))]
    public class DragToRotate : MonoBehaviour
    {
        [Header("Sürükleme Ayarları")]
        [Tooltip("Dönüş hassasiyeti")]
        [SerializeField] private float _sensitivity = 0.5f;

        [Tooltip("Durdurulurkenki yumuşatma süresi")]
        [SerializeField] private float _smoothTime = 0.2f;

        [Tooltip("Sürükleme bittikten kaç saniye sonra otomatik dönüşün başlayacağı")]
        [SerializeField] private float _autoRotateDelay = 2.0f;

        private ShowroomRotator _rotator;
        private Transform _cachedTransform;
        
        private float _rotationVelocity;
        private float _currentRotationY;
        private float _targetRotationY;
        
        private bool _isDragging = false;
        private Vector2 _lastMousePosition;
        private float _idleTimer = 0f;

        private void Awake()
        {
            _rotator = GetComponent<ShowroomRotator>();
            _cachedTransform = transform;
            _currentRotationY = _cachedTransform.localEulerAngles.y;
            _targetRotationY = _currentRotationY;
        }

        private void Update()
        {
            HandleInput();
            ApplyRotation();
        }

        private void HandleInput()
        {
            Pointer activePointer = Pointer.current;
            if (activePointer == null) return;

            // Sol tıklama veya dokunma basıldığı an
            if (activePointer.press.wasPressedThisFrame)
            {
                _isDragging = true;
                _lastMousePosition = activePointer.position.ReadValue();
                
                // Otomatik dönmeyi durdur
                if (_rotator != null)
                {
                    _rotator.CanRotate = false;
                }
                _idleTimer = 0f;
            }
            // Sürükleme devam ediyorken
            else if (activePointer.press.isPressed && _isDragging)
            {
                Vector2 currentMousePos = activePointer.position.ReadValue();
                float deltaX = currentMousePos.x - _lastMousePosition.x;
                
                // Dönüş hedefini belirle
                _targetRotationY -= deltaX * _sensitivity;
                _lastMousePosition = currentMousePos;
                _idleTimer = 0f;
            }
            // Sürükleme bittiğinde
            else if (activePointer.press.wasReleasedThisFrame)
            {
                _isDragging = false;
            }
            else
            {
                // Sürükleme yapılmıyorsa ve otomatik döndürücü durmuşsa timer'ı çalıştır
                if (_rotator != null && !_rotator.CanRotate)
                {
                    _idleTimer += Time.deltaTime;
                    if (_idleTimer >= _autoRotateDelay)
                    {
                        // Hedef açıyı mevcut açıya senkronize et ki zıplama olmasın
                        _currentRotationY = _cachedTransform.localEulerAngles.y;
                        _targetRotationY = _currentRotationY;
                        
                        _rotator.CanRotate = true;
                    }
                }
            }
        }

        private void ApplyRotation()
        {
            // Sürükleme sırasında veya yumuşatma aktifken pozisyonu güncelle
            if (_isDragging || Mathf.Abs(_targetRotationY - _currentRotationY) > 0.01f)
            {
                _currentRotationY = Mathf.SmoothDampAngle(_currentRotationY, _targetRotationY, ref _rotationVelocity, _smoothTime);
                
                Vector3 currentEuler = _cachedTransform.localEulerAngles;
                currentEuler.y = _currentRotationY;
                _cachedTransform.localEulerAngles = currentEuler;
            }
        }
    }
}

