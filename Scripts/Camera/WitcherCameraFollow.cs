using UnityEngine;

namespace WitcherGame
{
    // 中文说明：负责让主相机跟随玩家，并处理相机边界和受击震动。
    public class WitcherCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(2.4f, 1.25f, -10f);
        [SerializeField] private float smoothTime = 0.18f;
        [SerializeField] private float minX = -7.6f;
        [SerializeField] private float maxX = 54f;
        [SerializeField] private float minY = -0.2f;
        [SerializeField] private float maxY = 0.55f;

        private Vector3 velocity;
        private float shakeTimer;
        private float shakeDuration = 0.1f;
        private float shakeStrength;

        public void SetTarget(Transform followTarget)
        {
            target = followTarget;
        }

        public void ConfigureBounds(float stageMinX, float stageMaxX, float stageMinY, float stageMaxY)
        {
            minX = stageMinX;
            maxX = stageMaxX;
            minY = stageMinY;
            maxY = stageMaxY;
        }

        public void ConfigureView(Vector3 viewOffset, float followSmoothTime)
        {
            offset = viewOffset;
            smoothTime = Mathf.Max(0.01f, followSmoothTime);
        }

        public void AddShake(float strength, float duration)
        {
            shakeStrength = Mathf.Max(shakeStrength, strength);
            shakeDuration = Mathf.Max(0.01f, duration);
            shakeTimer = Mathf.Max(shakeTimer, duration);
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desiredPosition = target.position + offset;
            Camera camera = GetComponent<Camera>();
            if (camera != null && camera.orthographic)
            {
                float halfWidth = camera.orthographicSize * camera.aspect;
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX + halfWidth, maxX - halfWidth);
            }
            else
            {
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
            }

            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
            Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
            if (shakeTimer > 0f)
            {
                shakeTimer -= Time.unscaledDeltaTime;
                float shake01 = Mathf.Clamp01(shakeTimer / shakeDuration);
                Vector2 offsetShake = Random.insideUnitCircle * shakeStrength * shake01;
                smoothedPosition += new Vector3(offsetShake.x, offsetShake.y, 0f);
            }
            else
            {
                shakeStrength = 0f;
            }

            transform.position = smoothedPosition;
        }
    }
}
