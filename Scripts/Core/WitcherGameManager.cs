using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WitcherGame
{
    // 中文说明：负责游戏主流程的基础初始化和场景内核心状态协调。
    public class WitcherGameManager : MonoBehaviour
    {
        [SerializeField] private Text statusText;
        [SerializeField] private Text scoreText;
        [SerializeField] private GeraltController player;

        private bool gameEnded;

        private void Start()
        {
            UpdateScoreText();
            SetStatusText("探索地图，接触怪物进入回合制战斗");
        }

        public void RegisterPlayerDefeat(string reason)
        {
            if (gameEnded)
            {
                return;
            }

            gameEnded = true;
            SetStatusText($"{reason}Press R to restart");
            if (player != null)
            {
                player.SetControlEnabled(false);
            }
        }

        public void AssignPlayer(GeraltController playerController)
        {
            player = playerController;
        }

        public void SetHud(Text score, Text status)
        {
            scoreText = score;
            statusText = status;
            UpdateScoreText();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().path);
            }
        }

        private void UpdateScoreText()
        {
            if (scoreText != null)
            {
                scoreText.text = string.Empty;
            }
        }

        private void SetStatusText(string value)
        {
            if (statusText != null)
            {
                statusText.text = value;
            }
        }
    }
}
