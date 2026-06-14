using UnityEngine;

namespace WitcherGame
{
    public enum NightInvestigationNodeId
    {
        OldWell,
        OldMill,
        WidowHouse
    }

    // 中文说明：地图调查点交互组件，当前任务目标靠近后自动通知夜晚委托系统。
    [RequireComponent(typeof(Collider2D))]
    public class NightInvestigationNode : MonoBehaviour
    {
        [SerializeField] private NightContractManager manager;
        [SerializeField] private NightInvestigationNodeId nodeId;
        [SerializeField] private string displayName;
        [SerializeField] private float interactDistance = 1.15f;
        [SerializeField] private TextMesh label;

        private GeraltController player;
        private SpriteRenderer markerRenderer;
        private bool completed;

        public void Configure(NightContractManager owner, NightInvestigationNodeId id, string nodeName)
        {
            manager = owner;
            nodeId = id;
            displayName = nodeName;
            name = $"调查点_{displayName}";
            RefreshLabel(false);
        }

        public void SetCompleted(bool isCompleted)
        {
            completed = isCompleted;
            if (markerRenderer == null)
            {
                markerRenderer = GetComponent<SpriteRenderer>();
            }

            if (markerRenderer != null)
            {
                markerRenderer.color = completed
                    ? new Color32(88, 92, 88, 170)
                    : new Color32(214, 172, 87, 236);
            }

            RefreshLabel(false);
        }

        private void Awake()
        {
            markerRenderer = GetComponent<SpriteRenderer>();
            Collider2D nodeCollider = GetComponent<Collider2D>();
            nodeCollider.isTrigger = true;
        }

        private void Update()
        {
            if (completed || DialogueManager.IsDialogueActive)
            {
                return;
            }

            player = player == null ? FindObjectOfType<GeraltController>() : player;
            bool nearPlayer = player != null && Vector2.Distance(player.transform.position, transform.position) <= interactDistance;
            RefreshLabel(nearPlayer);
            if (nearPlayer && manager != null && manager.ShouldAutoTriggerNode(nodeId))
            {
                manager.InteractWithNode(nodeId);
                return;
            }

            if (nearPlayer && Input.GetKeyDown(KeyCode.E))
            {
                manager?.InteractWithNode(nodeId);
            }
        }

        private void OnMouseDown()
        {
            if (completed || DialogueManager.IsDialogueActive)
            {
                return;
            }

            manager?.InteractWithNode(nodeId);
        }

        private void RefreshLabel(bool isNearPlayer)
        {
            if (label == null)
            {
                label = GetComponentInChildren<TextMesh>();
            }

            if (label == null)
            {
                return;
            }

            string actionText = completed ? "已调查" : isNearPlayer ? "自动调查中" : "靠近触发";
            label.text = $"{displayName}\n{actionText}";
            label.color = completed
                ? new Color32(158, 160, 148, 210)
                : isNearPlayer
                    ? new Color32(255, 231, 156, 255)
                    : new Color32(218, 201, 148, 230);
        }
    }
}
