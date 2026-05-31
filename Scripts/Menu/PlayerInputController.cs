using UnityEngine;

namespace WitcherGame
{
    public class PlayerInputController : MonoBehaviour
    {
        private const string ControllerName = "Player Input Controller";
        private static PlayerInputController instance;

        [SerializeField] private GeraltController player;
        [SerializeField] private GameMenuController menuController;
        [SerializeField] private EquipmentUIController equipmentUIController;

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
                || (instance.menuController != null && instance.menuController.IsOpen)
                || (instance.equipmentUIController != null && instance.equipmentUIController.IsOpen);
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
            equipmentUIController = equipmentUIController == null ? EquipmentUIController.CreateIfMissing(player) : equipmentUIController;
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
                if (equipmentUIController != null && equipmentUIController.IsOpen)
                {
                    equipmentUIController.Close();
                    return;
                }

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
            else if (Input.GetKeyDown(KeyCode.D) && !IsAnyUiOpen())
            {
                equipmentUIController.Open();
            }
        }

        private void SetPlayer(GeraltController target)
        {
            player = target == null ? FindObjectOfType<GeraltController>() : target;
            if (menuController != null)
            {
                menuController.SetPlayer(player);
            }

            if (equipmentUIController != null)
            {
                equipmentUIController.SetPlayer(player);
            }
        }

        private bool IsAnyUiOpen()
        {
            return DialogueManager.IsDialogueActive
                || (menuController != null && menuController.IsOpen)
                || (equipmentUIController != null && equipmentUIController.IsOpen);
        }
    }
}
