using UnityEngine;

namespace WitcherGame
{
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
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
        }
    }
}
