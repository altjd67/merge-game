using UnityEngine;
using UnityEngine.UI;

namespace MergeBoard
{
    /// <summary>코인·에너지·회복 시간과 입력을 막지 않는 짧은 토스트를 표시한다.</summary>
    public sealed class HUDView : MonoBehaviour
    {
        [SerializeField] private Text coinsLabel;
        [SerializeField] private Text energyLabel;
        [SerializeField] private Text recoveryLabel;
        [SerializeField] private Text messageLabel;
        [SerializeField] private GameObject toastRoot;
        private const float ToastDuration = 2;
        private float hideToastAt;
        public string MessageText => messageLabel.text;
        public bool IsToastVisible => toastRoot.activeSelf;
        public string EnergyText => energyLabel.text;

        /// <summary>장면에 생성된 HUD 요소를 연결한다.</summary>
        public void Configure(Text coins, Text energy, Text recovery, Text message, GameObject toast)
        {
            coinsLabel = coins; energyLabel = energy; recoveryLabel = recovery;
            messageLabel = message; toastRoot = toast;
            toastRoot.SetActive(false);
        }

        /// <summary>규칙 계층에서 전달받은 코인·에너지·다음 회복 초를 표시한다.</summary>
        public void Render(int coins, int energy, int maximumEnergy, long recoverySeconds)
        {
            coinsLabel.text = coins + " 코인";
            energyLabel.text = $"에너지 {energy}/{maximumEnergy}";
            recoveryLabel.text = energy == maximumEnergy ? "최대" : $"{recoverySeconds / 60:00}:{recoverySeconds % 60:00}";
        }

        /// <summary>2초 동안 토스트를 표시한다. 연속 메시지는 교체하고 표시 시간을 새로 시작한다.</summary>
        public void ShowMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            messageLabel.text = message;
            toastRoot.SetActive(true);
            toastRoot.transform.SetAsLastSibling();
            hideToastAt = Time.unscaledTime + ToastDuration;
        }

        private void Update()
        {
            if (toastRoot != null && toastRoot.activeSelf && Time.unscaledTime >= hideToastAt) toastRoot.SetActive(false);
        }
    }
}
