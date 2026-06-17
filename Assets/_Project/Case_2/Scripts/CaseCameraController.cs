using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace BattlePass.Showcase
{
    /// <summary>
    /// Weapon VFX showcase sahnesinde orbit kamera: döndürme ve yakınlaştırma.
    /// Zoom limitleri sahne/editördeki başlangıç mesafesine göre hesaplanır.
    /// </summary>
    public class CaseCameraController : MonoBehaviour
    {
        [Header("Target")]
        public Transform target;

        [Header("Orbit Settings")]
        public float rotationSpeed = 5f;

        [Tooltip("Tekerlek hassasiyeti. 1 = orta hız, düşük = yavaş, yüksek = hızlı.")]
        [SerializeField] private float zoomSpeed = 1.2f;

        [Tooltip("Zoom hedefine yaklaşma süresi (sn). Düşük = daha seri, yüksek = daha yumuşak.")]
        [SerializeField] private float zoomSmoothTime = 0.1f;

        [Tooltip("Başlangıç mesafesinin bu katına kadar yakınlaşır (ör. 0.6 = %40 yakın).")]
        [SerializeField] private float minZoomMultiplier = 0.6f;

        [Tooltip("Başlangıç mesafesinin bu katına kadar uzaklaşır (ör. 1.15 = %15 uzak).")]
        [SerializeField] private float maxZoomMultiplier = 1.15f;

        private float currentYaw;
        private float currentPitch;
        private float distance;
        private float targetDistance;
        private float zoomVelocity;
        private float minDistance;
        private float maxDistance;

        private void Awake()
        {
            CaptureInitialOrbitFromTransform();
        }

        private void CaptureInitialOrbitFromTransform()
        {
            if (target == null)
                return;

            Vector3 offset = transform.position - target.position;
            float initialDistance = offset.magnitude;
            if (initialDistance <= 0.0001f)
                return;

            distance = initialDistance;
            targetDistance = initialDistance;
            minDistance = initialDistance * Mathf.Max(0.1f, minZoomMultiplier);
            maxDistance = initialDistance * Mathf.Max(minZoomMultiplier, maxZoomMultiplier);

            Quaternion lookRotation = Quaternion.LookRotation(-offset.normalized, Vector3.up);
            Vector3 euler = lookRotation.eulerAngles;
            currentPitch = euler.x > 180f ? euler.x - 360f : euler.x;
            currentYaw = euler.y;
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            HandleInput();
            UpdateCameraPosition();
        }

        private void HandleInput()
        {
            if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
                HandleTouchInput();
            else
                HandleMouseInput();

            distance = Mathf.SmoothDamp(distance, targetDistance, ref zoomVelocity, zoomSmoothTime);
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
            currentPitch = Mathf.Clamp(currentPitch, -30f, 80f);
        }

        private void HandleMouseInput()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
                return;

            if (mouse.leftButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                currentYaw += delta.x * rotationSpeed * 0.02f;
                currentPitch -= delta.y * rotationSpeed * 0.02f;
            }

            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
                ApplyZoomScroll(scroll);
        }

        private void ApplyZoomScroll(float scroll)
        {
            const float scrollWheelStep = 120f;
            const float zoomPerNotch = 0.085f;

            float notches = scroll / scrollWheelStep;
            float step = notches * zoomSpeed * zoomPerNotch;
            targetDistance *= 1f - step;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }

        private void ApplyZoomPinch(float pinchDeltaPixels)
        {
            float step = pinchDeltaPixels * zoomSpeed * 0.0004f;
            targetDistance *= 1f - step;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }

        private void HandleTouchInput()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
                return;

            if (touchscreen.touches.Count == 1)
            {
                TouchControl touch = touchscreen.touches[0];
                if (touch.phase.ReadValue() == TouchPhase.Moved)
                {
                    Vector2 delta = touch.delta.ReadValue();
                    currentYaw += delta.x * rotationSpeed * 0.02f;
                    currentPitch -= delta.y * rotationSpeed * 0.02f;
                }

                return;
            }

            if (touchscreen.touches.Count < 2)
                return;

            TouchControl t0 = touchscreen.touches[0];
            TouchControl t1 = touchscreen.touches[1];

            Vector2 p0 = t0.position.ReadValue();
            Vector2 p1 = t1.position.ReadValue();
            Vector2 d0 = t0.delta.ReadValue();
            Vector2 d1 = t1.delta.ReadValue();

            Vector2 prevT0 = p0 - d0;
            Vector2 prevT1 = p1 - d1;

            float prevPinch = Vector2.Distance(prevT0, prevT1);
            float currPinch = Vector2.Distance(p0, p1);
            ApplyZoomPinch(currPinch - prevPinch);
        }

        private void UpdateCameraPosition()
        {
            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            Vector3 direction = rotation * Vector3.back * distance;

            transform.position = target.position + direction;
            transform.LookAt(target.position);
        }
    }
}
