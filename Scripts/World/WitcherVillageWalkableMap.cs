using System.Collections.Generic;
using UnityEngine;

namespace WitcherGame
{
    // 中文说明：把村庄图片地图切成可走网格，并为点击移动提供轻量 A* NavMesh 寻路。
    public class WitcherVillageWalkableMap : MonoBehaviour
    {
        [SerializeField] private bool mapCollisionEnabled = true;
        [SerializeField] private bool restrictToRoadMask = true;
        [SerializeField] private bool showDebugBlockers;
        [SerializeField] private float characterRadius = 0.26f;
        [Header("Grid NavMesh")]
        [SerializeField] private Rect navBounds = new Rect(-13.2f, -7.75f, 26.4f, 13.65f);
        [SerializeField] private float navCellSize = 0.32f;
        [SerializeField] private int maxPathIterations = 5200;
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
        private const string DebugRootName = "Village NavMesh Debug";
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
        private GameObject debugRoot;
        private bool[,] walkableGrid;
        private int gridWidth;
        private int gridHeight;
        private readonly List<Vector2> reusablePath = new List<Vector2>();

        private void Awake()
        {
            DestroyOldPhysicsBlockers();
            RebuildGridNavMesh();
        }

        private void OnEnable()
        {
            Current = this;
            SetDebugVisible(showDebugBlockers);
        }

        private void OnDisable()
        {
            if (Current == this)
            {
                Current = null;
            }

            SetDebugVisible(false);
        }

        private void OnDestroy()
        {
            if (Current == this)
            {
                Current = null;
            }

            if (debugRoot != null)
            {
                Destroy(debugRoot);
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

            return true;
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

        public bool TryFindPath(Vector2 start, Vector2 destination, List<Vector2> result)
        {
            result?.Clear();
            if (result == null)
            {
                return false;
            }

            if (!mapCollisionEnabled)
            {
                result.Add(destination);
                return true;
            }

            if (walkableGrid == null)
            {
                RebuildGridNavMesh();
            }

            if (!TryGetNearestWalkablePoint(start, out Vector2 safeStart) ||
                !TryGetNearestWalkablePoint(destination, out Vector2 safeDestination) ||
                !TryWorldToGrid(safeStart, out int startX, out int startY) ||
                !TryWorldToGrid(safeDestination, out int endX, out int endY))
            {
                return false;
            }

            if (startX == endX && startY == endY)
            {
                result.Add(safeDestination);
                return true;
            }

            int nodeCount = gridWidth * gridHeight;
            float[] gScore = new float[nodeCount];
            float[] fScore = new float[nodeCount];
            int[] cameFrom = new int[nodeCount];
            bool[] closed = new bool[nodeCount];
            List<int> open = new List<int>(128);

            for (int i = 0; i < nodeCount; i++)
            {
                gScore[i] = float.PositiveInfinity;
                fScore[i] = float.PositiveInfinity;
                cameFrom[i] = -1;
            }

            int startIndex = ToIndex(startX, startY);
            int endIndex = ToIndex(endX, endY);
            gScore[startIndex] = 0f;
            fScore[startIndex] = Heuristic(startX, startY, endX, endY);
            open.Add(startIndex);

            int iterations = 0;
            while (open.Count > 0 && iterations++ < maxPathIterations)
            {
                int currentListIndex = FindLowestScoreIndex(open, fScore);
                int currentIndex = open[currentListIndex];
                if (currentIndex == endIndex)
                {
                    BuildPath(cameFrom, currentIndex, safeDestination, result);
                    SimplifyPath(result);
                    return result.Count > 0;
                }

                open.RemoveAt(currentListIndex);
                closed[currentIndex] = true;
                int currentX = currentIndex % gridWidth;
                int currentY = currentIndex / gridWidth;

                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        if (x == 0 && y == 0)
                        {
                            continue;
                        }

                        int nextX = currentX + x;
                        int nextY = currentY + y;
                        if (!IsGridWalkable(nextX, nextY) || !CanStepDiagonal(currentX, currentY, nextX, nextY))
                        {
                            continue;
                        }

                        int nextIndex = ToIndex(nextX, nextY);
                        if (closed[nextIndex])
                        {
                            continue;
                        }

                        float stepCost = x != 0 && y != 0 ? 1.4142f : 1f;
                        float tentativeScore = gScore[currentIndex] + stepCost;
                        if (tentativeScore >= gScore[nextIndex])
                        {
                            continue;
                        }

                        cameFrom[nextIndex] = currentIndex;
                        gScore[nextIndex] = tentativeScore;
                        fScore[nextIndex] = tentativeScore + Heuristic(nextX, nextY, endX, endY);
                        if (!open.Contains(nextIndex))
                        {
                            open.Add(nextIndex);
                        }
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

        private void RebuildGridNavMesh()
        {
            float safeCellSize = Mathf.Max(0.08f, navCellSize);
            gridWidth = Mathf.Max(1, Mathf.CeilToInt(navBounds.width / safeCellSize));
            gridHeight = Mathf.Max(1, Mathf.CeilToInt(navBounds.height / safeCellSize));
            walkableGrid = new bool[gridWidth, gridHeight];

            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    walkableGrid[x, y] = IsWalkableWithoutGrid(GridToWorld(x, y));
                }
            }

            BuildDebugGrid();
        }

        private bool IsWalkableWithoutGrid(Vector2 point)
        {
            if (!mapCollisionEnabled)
            {
                return true;
            }

            if (restrictToRoadMask && !IsInsideAnyRoadMask(point))
            {
                return false;
            }

            float radius = Mathf.Max(0f, characterRadius);
            return IsPointClearOfFallbackRects(point)
                && IsPointClearOfFallbackRects(point + new Vector2(radius, 0f))
                && IsPointClearOfFallbackRects(point + new Vector2(-radius, 0f))
                && IsPointClearOfFallbackRects(point + new Vector2(0f, radius))
                && IsPointClearOfFallbackRects(point + new Vector2(0f, -radius));
        }

        private bool TryWorldToGrid(Vector2 point, out int x, out int y)
        {
            float safeCellSize = Mathf.Max(0.08f, navCellSize);
            x = Mathf.FloorToInt((point.x - navBounds.xMin) / safeCellSize);
            y = Mathf.FloorToInt((point.y - navBounds.yMin) / safeCellSize);
            return IsGridInside(x, y);
        }

        private Vector2 GridToWorld(int x, int y)
        {
            float safeCellSize = Mathf.Max(0.08f, navCellSize);
            return new Vector2(
                navBounds.xMin + (x + 0.5f) * safeCellSize,
                navBounds.yMin + (y + 0.5f) * safeCellSize);
        }

        private bool IsGridWalkable(int x, int y)
        {
            return IsGridInside(x, y) && walkableGrid != null && walkableGrid[x, y];
        }

        private bool IsGridInside(int x, int y)
        {
            return x >= 0 && y >= 0 && x < gridWidth && y < gridHeight;
        }

        private bool CanStepDiagonal(int currentX, int currentY, int nextX, int nextY)
        {
            if (currentX == nextX || currentY == nextY)
            {
                return true;
            }

            return IsGridWalkable(nextX, currentY) && IsGridWalkable(currentX, nextY);
        }

        private int ToIndex(int x, int y)
        {
            return y * gridWidth + x;
        }

        private static float Heuristic(int x, int y, int endX, int endY)
        {
            int dx = Mathf.Abs(x - endX);
            int dy = Mathf.Abs(y - endY);
            return Mathf.Max(dx, dy) + (1.4142f - 1f) * Mathf.Min(dx, dy);
        }

        private static int FindLowestScoreIndex(List<int> open, float[] fScore)
        {
            int bestListIndex = 0;
            float bestScore = fScore[open[0]];
            for (int i = 1; i < open.Count; i++)
            {
                float score = fScore[open[i]];
                if (score < bestScore)
                {
                    bestScore = score;
                    bestListIndex = i;
                }
            }

            return bestListIndex;
        }

        private void BuildPath(int[] cameFrom, int currentIndex, Vector2 destination, List<Vector2> result)
        {
            reusablePath.Clear();
            while (currentIndex >= 0)
            {
                int x = currentIndex % gridWidth;
                int y = currentIndex / gridWidth;
                reusablePath.Add(GridToWorld(x, y));
                currentIndex = cameFrom[currentIndex];
            }

            result.Clear();
            for (int i = reusablePath.Count - 1; i >= 0; i--)
            {
                result.Add(reusablePath[i]);
            }

            if (result.Count == 0 || Vector2.Distance(result[result.Count - 1], destination) > navCellSize * 0.5f)
            {
                result.Add(destination);
            }
        }

        private void SimplifyPath(List<Vector2> path)
        {
            if (path == null || path.Count <= 2)
            {
                return;
            }

            for (int i = path.Count - 2; i > 0; i--)
            {
                Vector2 previous = path[i - 1];
                Vector2 current = path[i];
                Vector2 next = path[i + 1];
                Vector2 a = (current - previous).normalized;
                Vector2 b = (next - current).normalized;
                if (Vector2.Dot(a, b) > 0.985f)
                {
                    path.RemoveAt(i);
                }
            }
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

        private void DestroyOldPhysicsBlockers()
        {
            GameObject oldRoot = GameObject.Find(BlockerRootName);
            if (oldRoot != null)
            {
                Destroy(oldRoot);
            }
        }

        private void SetDebugVisible(bool visible)
        {
            if (debugRoot != null)
            {
                debugRoot.SetActive(visible);
            }
        }

        private void BuildDebugGrid()
        {
            if (!showDebugBlockers)
            {
                return;
            }

            if (debugRoot != null)
            {
                Destroy(debugRoot);
            }

            debugRoot = new GameObject(DebugRootName);
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    if (!walkableGrid[x, y])
                    {
                        continue;
                    }

                    GameObject cell = new GameObject($"Nav Cell {x:00}_{y:00}");
                    cell.transform.SetParent(debugRoot.transform, false);
                    cell.transform.position = GridToWorld(x, y);
                    cell.transform.localScale = Vector3.one * navCellSize * 0.82f;
                    SpriteRenderer renderer = cell.AddComponent<SpriteRenderer>();
                    renderer.sprite = WitcherSpriteLibrary.GetSolidSprite(new Color32(38, 160, 255, 42));
                    renderer.color = new Color32(38, 160, 255, 42);
                    renderer.sortingOrder = 301;
                }
            }
        }
    }
}
