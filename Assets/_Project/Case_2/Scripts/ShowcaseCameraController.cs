using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace BattlePass.Showcase
{
    public class ShowcaseCameraController : MonoBehaviour
    {
        [Header("Target")]
        public Transform target;

        [Header("Orbit Settings")]
        public float rotationSpeed = 5f;
        public float zoomSpeed = 2f;
        public float minZoom = 2f;
        public float maxZoom = 10f;

        private float currentYaw = 90f;
        private float currentPitch;
        private float distance = 0.8f;

        private void Start()
        {
            if (target == null)
                return;

            Vector3 offset = transform.position - target.position;
            if (offset.sqrMagnitude <= 0.0001f)
                return;

            distance = offset.magnitude;
            Quaternion lookRotation = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
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

            distance = Mathf.Clamp(distance, minZoom, maxZoom);
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
                distance -= scroll * zoomSpeed * 0.001f;
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

            float prevDistance = Vector2.Distance(prevT0, prevT1);
            float currDistance = Vector2.Distance(p0, p1);
            distance -= (currDistance - prevDistance) * zoomSpeed * 0.01f;
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
