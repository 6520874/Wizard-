using UnityEngine;

namespace WitcherGame
{
    public enum NightContractEncounterRole
    {
        ClueAmbush,
        Boss
    }

    // 中文说明：监听委托战斗遭遇被清理，并把战斗胜利结果回传给夜晚委托系统。
    public class NightContractEncounterWatcher : MonoBehaviour
    {
        [SerializeField] private NightContractManager manager;
        [SerializeField] private NightContractEncounterRole role;

        private bool notified;

        public void Configure(NightContractManager owner, NightContractEncounterRole encounterRole)
        {
            manager = owner;
            role = encounterRole;
        }

        private void OnDestroy()
        {
            NotifyCleared();
        }

        private void NotifyCleared()
        {
            if (notified || manager == null || !Application.isPlaying)
            {
                return;
            }

            notified = true;
            manager.NotifyEncounterCleared(role);
        }
    }
}
