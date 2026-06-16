using UnityEngine;

namespace WitcherGame
{
    // 中文说明：集中处理玩家菜单快捷键输入，协调主菜单打开关闭。
    public class PlayerInputController : MonoBehaviour
    {
        private const string ControllerName = "Player Input Controller";
        private static PlayerInputController instance;

        [SerializeField] private GeraltController player;
        [SerializeField] private GameMenuController menuController;

        public static PlayerInputController Instance => instance;

        public static PlayerInputController CreateIfMissing(GeraltController target)
        {
            if (instance != null)
            {
                instance.SetPlayer(target);
                return instance;
            }

            PlayerInputController existing = FindObjectOfType<PlayerInputController>();
            if (existing != null)
            {
                instance = existing;
                instance.SetPlayer(target);
                return existing;
            }

            PlayerInputController controller = new GameObject(ControllerName).AddComponent<PlayerInputController>();
            controller.SetPlayer(target);
            return controller;
        }

        public static void RefreshPlayerControl()
        {
            if (instance == null)
            {
                return;
            }

            bool blocked = DialogueManager.IsDialogueActive
                || (instance.menuController != null && instance.menuController.IsOpen);
            instance.player?.SetControlEnabled(!blocked);
        }

        private void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            PartyManager.CreateIfMissing();
            DialogueManager.CreateIfMissing();
            QuestManager.CreateIfMissing();
            menuController = menuController == null ? GameMenuController.CreateIfMissing(player) : menuController;
            PartyFollowManager.CreateIfMissing(player);
            RefreshPlayerControl();
        }

        private void Update()
        {
            TurnBasedBattleManager battleManager = FindObjectOfType<TurnBasedBattleManager>();
            if ((battleManager != null && battleManager.BattleActive) || DialogueManager.IsDialogueActive)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (menuController != null && menuController.IsOpen)
                {
                    menuController.Close();
                    return;
                }
            }

            if (Input.GetKeyDown(KeyCode.M) && !IsAnyUiOpen())
            {
                menuController.Open();
            }
        }

        private void SetPlayer(GeraltController target)
        {
            player = target == null ? FindObjectOfType<GeraltController>() : target;
            if (menuController != null)
            {
                menuController.SetPlayer(player);
            }
        }

        private bool IsAnyUiOpen()
        {
            return DialogueManager.IsDialogueActive
                || (menuController != null && menuController.IsOpen);
        }
    }
}
