using UnityEngine;
using UnityEngine.AI;

namespace WitcherGame
{
    // 中文说明：通过 Unity 编辑器烘焙的 NavMesh 约束村庄移动，不再运行时生成阻挡体。
    public class WitcherVillageWalkableMap : MonoBehaviour
    {
        [SerializeField] private bool mapCollisionEnabled = true;
        [SerializeField] private bool allowMovementWhenNavMeshMissing = true;
        [SerializeField] private float navMeshSampleDistance = 0.85f;
        [SerializeField] private int navMeshAreaMask = NavMesh.AllAreas;

        private bool warnedMissingNavMesh;

        public static WitcherVillageWalkableMap Current { get; private set; }

        private void OnEnable()
        {
            Current = this;
        }

        private void OnDisable()
        {
            if (Current == this)
            {
                Current = null;
            }
        }

        private void OnDestroy()
        {
            if (Current == this)
            {
                Current = null;
            }
        }

        public Vector2 ResolveVelocity(Vector2 currentPosition, Vector2 requestedVelocity, float deltaTime)
        {
            if (!mapCollisionEnabled || requestedVelocity.sqrMagnitude <= 0.0001f || deltaTime <= 0f)
            {
                return requestedVelocity;
            }

            if (CanTravel(currentPosition, currentPosition + requestedVelocity * deltaTime))
            {
                return requestedVelocity;
            }

            Vector2 xOnlyVelocity = new Vector2(requestedVelocity.x, 0f);
            Vector2 yOnlyVelocity = new Vector2(0f, requestedVelocity.y);
            bool canMoveX = Mathf.Abs(requestedVelocity.x) > 0.001f
                && CanTravel(currentPosition, currentPosition + xOnlyVelocity * deltaTime);
            bool canMoveY = Mathf.Abs(requestedVelocity.y) > 0.001f
                && CanTravel(currentPosition, currentPosition + yOnlyVelocity * deltaTime);

            if (canMoveX && canMoveY)
            {
                return Mathf.Abs(requestedVelocity.x) > Mathf.Abs(requestedVelocity.y)
                    ? xOnlyVelocity
                    : yOnlyVelocity;
            }

            if (canMoveX)
            {
                return xOnlyVelocity;
            }

            if (canMoveY)
            {
                return yOnlyVelocity;
            }

            return Vector2.zero;
        }

        public bool TryGetNearestWalkablePoint(Vector2 requestedPoint, out Vector2 walkablePoint)
        {
            walkablePoint = requestedPoint;
            if (!mapCollisionEnabled)
            {
                return true;
            }

            if (TrySampleNavMesh(requestedPoint, out NavMeshHit hit))
            {
                walkablePoint = FromNavMeshPoint(hit.position);
                return true;
            }

            WarnMissingNavMeshOnce();
            return allowMovementWhenNavMeshMissing;
        }

        public bool IsWalkable(Vector2 point)
        {
            if (!mapCollisionEnabled)
            {
                return true;
            }

            if (TrySampleNavMesh(point, out NavMeshHit hit))
            {
                return Vector2.Distance(point, FromNavMeshPoint(hit.position)) <= navMeshSampleDistance;
            }

            WarnMissingNavMeshOnce();
            return allowMovementWhenNavMeshMissing;
        }

        private bool CanTravel(Vector2 from, Vector2 to)
        {
            if (!TrySampleNavMesh(from, out NavMeshHit fromHit) || !TrySampleNavMesh(to, out NavMeshHit toHit))
            {
                WarnMissingNavMeshOnce();
                return allowMovementWhenNavMeshMissing;
            }

            return !NavMesh.Raycast(fromHit.position, toHit.position, out _, navMeshAreaMask);
        }

        private bool TrySampleNavMesh(Vector2 worldPoint, out NavMeshHit hit)
        {
            return NavMesh.SamplePosition(ToNavMeshPoint(worldPoint), out hit, navMeshSampleDistance, navMeshAreaMask);
        }

        private static Vector3 ToNavMeshPoint(Vector2 worldPoint)
        {
            return new Vector3(worldPoint.x, 0f, worldPoint.y);
        }

        private static Vector2 FromNavMeshPoint(Vector3 navMeshPoint)
        {
            return new Vector2(navMeshPoint.x, navMeshPoint.z);
        }

        private void WarnMissingNavMeshOnce()
        {
            if (warnedMissingNavMesh || !allowMovementWhenNavMeshMissing)
            {
                return;
            }

            warnedMissingNavMesh = true;
            Debug.LogWarning("No baked Unity NavMesh was found near the village movement point. Bake the walkable mesh in Unity to enable movement blocking.");
        }
    }
}
