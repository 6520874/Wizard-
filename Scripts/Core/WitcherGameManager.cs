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

        private int totalCoins;
        private int collectedCoins;
        private bool gameEnded;

        private void Start()
        {
            totalCoins = FindObjectsOfType<WitcherCoin>().Length;
            UpdateScoreText();
            SetStatusText("Collect all coins");
        }

        public void RegisterCoin()
        {
            if (gameEnded)
            {
                return;
            }

            collectedCoins++;
            UpdateScoreText();

            if (collectedCoins >= totalCoins)
            {
                gameEnded = true;
                SetStatusText("You win! Press R to restart");
                if (player != null)
                {
                    player.SetControlEnabled(false);
                }
            }
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
                scoreText.text = $"Coins: {collectedCoins}/{totalCoins}";
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
