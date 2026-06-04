using System.Collections.Generic;
using UnityEngine;

namespace WitcherGame
{
    // 中文说明：根据 PartyManager 的入队成员，在地图上生成并维护队友跟随队列。
    public class PartyFollowManager : MonoBehaviour
    {
        private const string ManagerName = "Party Follow Manager";
        private const int MaxHistory = 160;
        private static PartyFollowManager instance;

        [SerializeField] private GeraltController player;
        [SerializeField] private float followerScale = 0.56f;
        [SerializeField] private float followGap = 0.82f;
        [SerializeField] private float historyRecordDistance = 0.035f;
        [SerializeField] private float stageMaxY = 5f;

        private readonly List<Vector2> playerHistory = new List<Vector2>();
        private readonly Dictionary<PartyMember, PartyFollowerController> followers = new Dictionary<PartyMember, PartyFollowerController>();
        private PartyManager partyManager;

        public static PartyFollowManager CreateIfMissing(GeraltController target)
        {
            if (instance != null)
            {
                instance.SetPlayer(target);
                return instance;
            }

            PartyFollowManager existing = FindObjectOfType<PartyFollowManager>();
            if (existing != null)
            {
                instance = existing;
                instance.SetPlayer(target);
                return existing;
            }

            PartyFollowManager manager = new GameObject(ManagerName).AddComponent<PartyFollowManager>();
            manager.SetPlayer(target);
            return manager;
        }

        private void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            partyManager = PartyManager.CreateIfMissing();
            partyManager.PartyChanged += RefreshFollowers;
            SeedHistory();
            RefreshFollowers();
        }

        private void OnDestroy()
        {
            if (partyManager != null)
            {
                partyManager.PartyChanged -= RefreshFollowers;
            }
        }

        private void LateUpdate()
        {
            player = player == null ? FindObjectOfType<GeraltController>() : player;
            if (player == null)
            {
                return;
            }

            RecordPlayerPosition();
            RefreshFollowers();
            IReadOnlyList<PartyMember> activeMembers = partyManager.ActiveParty;
            int visibleIndex = 0;
            for (int i = 0; i < activeMembers.Count; i++)
            {
                PartyMember member = activeMembers[i];
                if (!ShouldCreateFollower(member) || !followers.TryGetValue(member, out PartyFollowerController follower) || follower == null)
                {
                    continue;
                }

                Vector2 target = GetHistoryPoint((visibleIndex + 1) * followGap);
                follower.MoveToward(target, Time.deltaTime);
                visibleIndex++;
            }
        }

        private void SetPlayer(GeraltController target)
        {
            player = target == null ? FindObjectOfType<GeraltController>() : target;
            SeedHistory();
        }

        private void RefreshFollowers()
        {
            partyManager = partyManager == null ? PartyManager.CreateIfMissing() : partyManager;
            IReadOnlyList<PartyMember> activeMembers = partyManager.ActiveParty;

            List<PartyMember> staleMembers = new List<PartyMember>();
            foreach (KeyValuePair<PartyMember, PartyFollowerController> pair in followers)
            {
                if (pair.Key == null || !IsVisibleFollower(pair.Key, activeMembers))
                {
                    staleMembers.Add(pair.Key);
                }
            }

            for (int i = 0; i < staleMembers.Count; i++)
            {
                PartyMember member = staleMembers[i];
                if (member != null && followers.TryGetValue(member, out PartyFollowerController follower) && follower != null)
                {
                    Destroy(follower.gameObject);
                }

                followers.Remove(member);
            }

            for (int i = 0; i < activeMembers.Count; i++)
            {
                PartyMember member = activeMembers[i];
                if (!ShouldCreateFollower(member) || followers.ContainsKey(member))
                {
                    continue;
                }

                PartyFollowerController follower = CreateFollower(member, i);
                if (follower != null)
                {
                    followers[member] = follower;
                }
            }
        }

        private PartyFollowerController CreateFollower(PartyMember member, int partyIndex)
        {
            string visualFolder = PartyAnimationLibrary.GetVisualFolder(member.Name);
            if (string.IsNullOrEmpty(visualFolder))
            {
                return null;
            }

            GameObject followerObject = new GameObject($"{member.Name} Map Follower");
            followerObject.transform.SetParent(transform, false);
            PartyFollowerController follower = followerObject.AddComponent<PartyFollowerController>();
            follower.Configure(member, followerScale, stageMaxY);
            Vector2 spawnPosition = GetHistoryPoint(Mathf.Max(1, partyIndex) * followGap);
            follower.SnapTo(spawnPosition);
            return follower;
        }

        private void RecordPlayerPosition()
        {
            Vector2 current = player.transform.position;
            if (playerHistory.Count == 0 || Vector2.Distance(playerHistory[0], current) >= historyRecordDistance)
            {
                playerHistory.Insert(0, current);
                while (playerHistory.Count > MaxHistory)
                {
                    playerHistory.RemoveAt(playerHistory.Count - 1);
                }
            }
        }

        private void SeedHistory()
        {
            playerHistory.Clear();
            if (player == null)
            {
                return;
            }

            Vector2 position = player.transform.position;
            for (int i = 0; i < 24; i++)
            {
                playerHistory.Add(position + Vector2.left * followGap * (i / 6f));
            }
        }

        private Vector2 GetHistoryPoint(float distanceBehind)
        {
            if (playerHistory.Count == 0)
            {
                return player == null ? Vector2.zero : (Vector2)player.transform.position + Vector2.left * distanceBehind;
            }

            float traveled = 0f;
            for (int i = 1; i < playerHistory.Count; i++)
            {
                float segment = Vector2.Distance(playerHistory[i - 1], playerHistory[i]);
                if (traveled + segment >= distanceBehind)
                {
                    float t = segment <= 0.0001f ? 0f : (distanceBehind - traveled) / segment;
                    return Vector2.Lerp(playerHistory[i - 1], playerHistory[i], t);
                }

                traveled += segment;
            }

            return playerHistory[playerHistory.Count - 1];
        }

        private static bool ShouldCreateFollower(PartyMember member)
        {
            return member != null
                && member.IsJoined
                && member.Name != "猎魔人"
                && !string.IsNullOrEmpty(PartyAnimationLibrary.GetVisualFolder(member.Name));
        }

        private static bool IsVisibleFollower(PartyMember member, IReadOnlyList<PartyMember> activeMembers)
        {
            if (!ShouldCreateFollower(member))
            {
                return false;
            }

            for (int i = 0; i < activeMembers.Count; i++)
            {
                if (activeMembers[i] == member)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
