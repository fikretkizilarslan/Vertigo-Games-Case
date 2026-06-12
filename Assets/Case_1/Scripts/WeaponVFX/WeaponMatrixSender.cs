using UnityEngine;

namespace VertigoCase.WeaponVFX
{
    /// <summary>
    /// Çok parçalı modellerde gürültü (noise) dokusunun senkronize olmasını sağlamak için
    /// silahın root (ana) objesinin WorldToLocal matrisini shader'a global olarak gönderir.
    /// </summary>
    [ExecuteInEditMode] // Oyunu başlatmadan da editör ekranında test edebilmek için
    public class WeaponMatrixSender : MonoBehaviour
    {
        [Tooltip("Shader Graph içinde oluşturduğunuz Matrix 4x4 parametresinin referans adı")]
        [SerializeField] private string _matrixPropertyName = "_RootWorldToLocal";

        private Transform _cachedTransform;
        private int _matrixPropId;

        private void Awake()
        {
            _cachedTransform = transform;
            _matrixPropId = Shader.PropertyToID(_matrixPropertyName);
        }

        private void Update()
        {
            if (_cachedTransform == null)
            {
                _cachedTransform = transform;
            }

            // Ana objenin dünya-lokal dönüşüm matrisini shader'a gönderir
            Shader.SetGlobalMatrix(_matrixPropId, _cachedTransform.worldToLocalMatrix);
        }
    }
}
