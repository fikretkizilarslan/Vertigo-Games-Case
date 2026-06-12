using UnityEngine;

namespace VertigoCase.WeaponVFX
{
    /// <summary>
    /// Showroom ortamındaki silahı otomatik olarak kendi ekseninde döndüren betik.
    /// </summary>
    public class ShowroomRotator : MonoBehaviour
    {
        [Header("Otomatik Dönüş Ayarları")]
        [Tooltip("Dönüş hızı (derece/saniye)")]
        [SerializeField] private float _rotationSpeed = 15f;

        [Tooltip("Dönüş ekseni")]
        [SerializeField] private Vector3 _rotationAxis = Vector3.up;

        private Transform _cachedTransform;
        private bool _canRotate = true;

        public bool CanRotate
        {
            get => _canRotate;
            set => _canRotate = value;
        }

        private void Awake()
        {
            // Transform aramaları ve cacheleme optimizasyonu
            _cachedTransform = transform;
        }

        private void Update()
        {
            if (!_canRotate) return;

            // Pürüzsüz dönüş için deltaTime kullanıldı
            _cachedTransform.Rotate(_rotationAxis * (_rotationSpeed * Time.deltaTime), Space.Self);
        }
    }
}
