using System.Collections.Generic;
using UnityEngine;

namespace WitcherGame
{
    // 中文说明：定义村庄地图的禁行区域，并为玩家移动提供可走判定和碰撞修正。
    public class WitcherVillageWalkableMap : MonoBehaviour
    {
        [SerializeField] private bool mapCollisionEnabled = true;
        [SerializeField] private bool restrictToRoadMask = true;
        [SerializeField] private bool createPhysicsBlockers = true;
        [SerializeField] private bool usePhysicsQueries = true;
        [SerializeField] private bool showDebugBlockers;
        [SerializeField] private float characterRadius = 0.26f;
        [SerializeField] private float castSkin = 0.03f;
        [SerializeField]
        private Rect[] blockedZones =
        {
            new Rect(-12.5f, 1.3f, 3.5f, 4.2f),
            new Rect(-8.8f, 0.65f, 3.05f, 6.15f),
            new Rect(-11.9f, -6.6f, 5.35f, 4.15f),
            new Rect(-7.8f, -5.75f, 3.3f, 3.7f),
            new Rect(-3.1f, 1.2f, 6.25f, 5.55f),
            new Rect(4.65f, 1.05f, 4.6f, 4.55f),
            new Rect(9.45f, 0.35f, 3.25f, 3.5f),
            new Rect(4.9f, -6.55f, 5.85f, 4.25f),
            new Rect(0.2f, -5.85f, 3.55f, 1.8f),
            new Rect(-13.2f, 5.9f, 26.4f, 1.9f),
            new Rect(-13.2f, -7.75f, 26.4f, 1.15f)
        };

        private const string BlockerRootName = "Village Collision Blockers";
        private static readonly Vector2[][] RoadMasks =
        {
            new[]
            {
                new Vector2(-3.9f, -6.45f),
                new Vector2(3.8f, -6.45f),
                new Vector2(4.65f, -3.15f),
                new Vector2(4.15f, -0.95f),
                new Vector2(2.55f, 2.95f),
                new Vector2(1.45f, 5.65f),
                new Vector2(-1.35f, 5.65f),
                new Vector2(-2.75f, 2.75f),
                new Vector2(-4.35f, -0.65f),
                new Vector2(-4.75f, -3.4f)
            },
            new[]
            {
                new Vector2(-12.2f, -0.75f),
                new Vector2(-9.25f, -1.65f),
                new Vector2(-6.55f, -2.85f),
                new Vector2(-3.8f, -2.35f),
                new Vector2(-3.35f, -0.6f),
                new Vector2(-5.95f, 0.3f),
                new Vector2(-8.65f, 0.75f),
                new Vector2(-12.2f, 0.95f)
            },
            new[]
            {
                new Vector2(-7.35f, -4.9f),
                new Vector2(-3.35f, -4.95f),
                new Vector2(-2.75f, -3.35f),
                new Vector2(-5.85f, -2.7f),
                new Vector2(-8.25f, -2.15f),
                new Vector2(-9.85f, -3.35f)
            },
            new[]
            {
                new Vector2(2.9f, -2.9f),
                new Vector2(7.15f, -2.85f),
                new Vector2(9.65f, -1.1f),
                new Vector2(10.25f, 0.55f),
                new Vector2(7.85f, 1.1f),
                new Vector2(4.15f, 0.1f),
                new Vector2(3.35f, -0.95f)
            },
            new[]
            {
                new Vector2(8.65f, -0.85f),
                new Vector2(12.2f, -1.25f),
                new Vector2(12.2f, 1.1f),
                new Vector2(10.1f, 1.05f),
                new Vector2(8.25f, 0.55f)
            },
            new[]
            {
                new Vector2(5.75f, 1.55f),
                new Vector2(11.65f, 1.25f),
                new Vector2(12.2f, 3.65f),
                new Vector2(9.1f, 4.4f),
                new Vector2(6.65f, 3.35f)
            },
            new[]
            {
                new Vector2(-12.2f, 2.05f),
                new Vector2(-8.95f, 1.05f),
                new Vector2(-6.9f, 1.25f),
                new Vector2(-6.45f, 2.65f),
                new Vector2(-8.8f, 3.55f),
                new Vector2(-12.2f, 4.1f)
            }
        };

        public static WitcherVillageWalkableMap Current { get; private set; }
        private GameObject blockerRoot;
        private readonly HashSet<Collider2D> blockerColliders = new HashSet<Collider2D>();
        private readonly Collider2D[] overlapResults = new Collider2D[32];
        private readonly RaycastHit2D[] castResults = new RaycastHit2D[32];

        private void Awake()
        {
            if (createPhysicsBlockers)
            {
                RebuildPhysicsBlockers();
            }
        }

        private void OnEnable()
        {
            Current = this;
            if (blockerRoot != null)
            {
                blockerRoot.SetActive(true);
            }
        }

        private void OnDisable()
        {
            if (Current == this)
            {
                Current = null;
            }

            if (blockerRoot != null)
            {
                blockerRoot.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (Current == this)
            {
                Current = null;
            }

            if (blockerRoot != null)
            {
                Destroy(blockerRoot);
            }
        }

        public Vector2 ResolveVelocity(Vector2 currentPosition, Vector2 requestedVelocity, float deltaTime)
        {
            if (!mapCollisionEnabled || requestedVelocity.sqrMagnitude <= 0.0001f || deltaTime <= 0f)
            {
                return requestedVelocity;
            }

            if (CanMoveTo(currentPosition, requestedVelocity, deltaTime))
            {
                return requestedVelocity;
            }

            Vector2 xOnlyVelocity = new Vector2(requestedVelocity.x, 0f);
            Vector2 yOnlyVelocity = new Vector2(0f, requestedVelocity.y);
            bool canMoveX = Mathf.Abs(requestedVelocity.x) > 0.001f && CanMoveTo(currentPosition, xOnlyVelocity, deltaTime);
            bool canMoveY = Mathf.Abs(requestedVelocity.y) > 0.001f && CanMoveTo(currentPosition, yOnlyVelocity, deltaTime);

            if (canMoveX && canMoveY)
            {
                return Mathf.Abs(requestedVelocity.x) > Mathf.Abs(requestedVelocity.y)
                    ? new Vector2(requestedVelocity.x, 0f)
                    : new Vector2(0f, requestedVelocity.y);
            }

            if (canMoveX)
            {
                return new Vector2(requestedVelocity.x, 0f);
            }

            if (canMoveY)
            {
                return new Vector2(0f, requestedVelocity.y);
            }

            return Vector2.zero;
        }

        private bool CanMoveTo(Vector2 currentPosition, Vector2 requestedVelocity, float deltaTime)
        {
            Vector2 desiredPosition = currentPosition + requestedVelocity * deltaTime;
            if (!IsWalkable(desiredPosition))
            {
                return false;
            }

            return !usePhysicsQueries || !HitsPhysicsBlocker(currentPosition, requestedVelocity, deltaTime);
        }

        public bool TryGetNearestWalkablePoint(Vector2 requestedPoint, out Vector2 walkablePoint)
        {
            walkablePoint = requestedPoint;
            if (!mapCollisionEnabled || IsWalkable(requestedPoint))
            {
                return true;
            }

            const int angleSteps = 20;
            const float radiusStep = 0.2f;
            const float maxRadius = 2.8f;
            for (float radius = radiusStep; radius <= maxRadius; radius += radiusStep)
            {
                for (int i = 0; i < angleSteps; i++)
                {
                    float angle = i * Mathf.PI * 2f / angleSteps;
                    Vector2 candidate = requestedPoint + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    if (IsWalkable(candidate))
                    {
                        walkablePoint = candidate;
                        return true;
                    }
                }
            }

            return false;
        }

        public bool IsWalkable(Vector2 point)
        {
            if (!mapCollisionEnabled)
            {
                return true;
            }

            if (restrictToRoadMask && !IsInsideAnyRoadMask(point))
            {
                return false;
            }

            if (usePhysicsQueries && blockerColliders.Count > 0)
            {
                return !OverlapsPhysicsBlocker(point);
            }

            float radius = Mathf.Max(0f, characterRadius);
            return IsPointClearOfFallbackRects(point)
                && IsPointClearOfFallbackRects(point + new Vector2(radius, 0f))
                && IsPointClearOfFallbackRects(point + new Vector2(-radius, 0f))
                && IsPointClearOfFallbackRects(point + new Vector2(0f, radius))
                && IsPointClearOfFallbackRects(point + new Vector2(0f, -radius));
        }

        private bool IsPointClearOfFallbackRects(Vector2 point)
        {
            for (int i = 0; i < blockedZones.Length; i++)
            {
                if (blockedZones[i].Contains(point))
                {
                    return false;
                }
            }

            return true;
        }

        private bool OverlapsPhysicsBlocker(Vector2 point)
        {
            float radius = Mathf.Max(0.01f, characterRadius);
            int hitCount = Physics2D.OverlapCircleNonAlloc(point, radius, overlapResults);
            for (int i = 0; i < hitCount; i++)
            {
                if (IsBlockerCollider(overlapResults[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HitsPhysicsBlocker(Vector2 currentPosition, Vector2 requestedVelocity, float deltaTime)
        {
            float distance = requestedVelocity.magnitude * deltaTime;
            if (distance <= 0.0001f)
            {
                return false;
            }

            Vector2 direction = requestedVelocity.normalized;
            float radius = Mathf.Max(0.01f, characterRadius - castSkin);
            int hitCount = Physics2D.CircleCastNonAlloc(currentPosition, radius, direction, castResults, distance + castSkin);
            for (int i = 0; i < hitCount; i++)
            {
                if (IsBlockerCollider(castResults[i].collider))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsBlockerCollider(Collider2D collider)
        {
            return collider != null && blockerColliders.Contains(collider);
        }

        private static bool IsInsideAnyRoadMask(Vector2 point)
        {
            for (int i = 0; i < RoadMasks.Length; i++)
            {
                if (IsInsidePolygon(point, RoadMasks[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsInsidePolygon(Vector2 point, Vector2[] polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                bool crossesY = polygon[i].y > point.y != polygon[j].y > point.y;
                if (crossesY)
                {
                    float xAtY = (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x;
                    if (point.x < xAtY)
                    {
                        inside = !inside;
                    }
                }
            }

            return inside;
        }

        private void RebuildPhysicsBlockers()
        {
            blockerColliders.Clear();
            GameObject oldRoot = GameObject.Find(BlockerRootName);
            if (oldRoot != null)
            {
                Destroy(oldRoot);
            }

            blockerRoot = new GameObject(BlockerRootName);

            for (int i = 0; i < blockedZones.Length; i++)
            {
                Rect zone = blockedZones[i];
                GameObject blocker = new GameObject($"Village Blocker {i + 1:00}");
                blocker.transform.SetParent(blockerRoot.transform, false);
                blocker.transform.position = new Vector3(zone.center.x, zone.center.y, 0f);

                BoxCollider2D collider = blocker.AddComponent<BoxCollider2D>();
                collider.isTrigger = false;
                collider.size = zone.size;
                blockerColliders.Add(collider);

                if (showDebugBlockers)
                {
                    GameObject visual = new GameObject("Debug Visual");
                    visual.transform.SetParent(blocker.transform, false);
                    SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
                    renderer.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(255, 64, 64, 64));
                    renderer.color = new Color32(255, 64, 64, 64);
                    renderer.sortingOrder = 300;
                    visual.transform.localScale = new Vector3(zone.width, zone.height, 1f);
                }
            }

            Physics2D.SyncTransforms();
        }
    }
}
