using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MergeBoard
{
    /// <summary>코인·에너지·회복 시간과 입력을 막지 않는 짧은 토스트를 표시한다.</summary>
    public sealed class HUDView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI coinsLabel;
        [SerializeField] private TextMeshProUGUI energyLabel;
        [SerializeField] private TextMeshProUGUI recoveryLabel;
        [SerializeField] private TextMeshProUGUI messageLabel;
        [SerializeField] private GameObject toastRoot;
        [SerializeField] private GameObject generatorGuideRoot;
        private const float ToastDuration = 2;
        private float hideToastAt;
        public string MessageText => messageLabel.text;
        public bool IsToastVisible => toastRoot.activeSelf;
        public string EnergyText => energyLabel.text;
        public bool IsGeneratorGuideVisible => generatorGuideRoot != null && generatorGuideRoot.activeSelf;

        /// <summary>장면에 생성된 HUD 요소를 연결한다.</summary>
        public void Configure(TextMeshProUGUI coins, TextMeshProUGUI energy, TextMeshProUGUI recovery, TextMeshProUGUI message, GameObject toast, GameObject generatorGuide = null)
        {
            coinsLabel = coins; energyLabel = energy; recoveryLabel = recovery;
            messageLabel = message; toastRoot = toast;
            toastRoot.SetActive(false);
            generatorGuideRoot = generatorGuide;
        }

        /// <summary>로컬라이제이션 초기화 후 고정 가이드 문구를 문자열 테이블 값으로 갱신합니다.</summary>
        public void LocalizeStaticText()
        {
            if (generatorGuideRoot == null) return;
            var guideLabel = generatorGuideRoot.GetComponentInChildren<TextMeshProUGUI>(true);
            if (guideLabel != null) guideLabel.text = GameText.Get("guide.generator");
        }

        /// <summary>첫 생성 성공 전 씨앗팩 사용 안내의 표시 여부를 갱신한다.</summary>
        public void RenderGeneratorGuide(bool visible)
        {
            if (generatorGuideRoot != null && generatorGuideRoot.activeSelf != visible) generatorGuideRoot.SetActive(visible);
        }

        /// <summary>규칙 계층에서 전달받은 코인·에너지·다음 회복 초를 표시한다.</summary>
        public void Render(int coins, int energy, int maximumEnergy, long recoverySeconds)
        {
            coinsLabel.text = GameText.Get("hud.coins", coins);
            energyLabel.text = GameText.Get("hud.energy", energy, maximumEnergy);
            recoveryLabel.text = energy == maximumEnergy ? GameText.Get("hud.maximum") : $"{recoverySeconds / 60:00}:{recoverySeconds % 60:00}";
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
