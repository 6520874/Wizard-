using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PixelRaid
{
    public class PixelRaidGameManager : MonoBehaviour
    {
        [SerializeField] private Text statusText;
        [SerializeField] private Text scoreText;
        [SerializeField] private PixelRaidPlayerController player;

        private int totalCoins;
        private int collectedCoins;
        private bool gameEnded;

        private void Start()
        {
            totalCoins = FindObjectsOfType<PixelRaidCoin>().Length;
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

        public void AssignPlayer(PixelRaidPlayerController playerController)
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
