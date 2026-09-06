using System;
using UnityEngine;
using UnityEngine.UI;

namespace MergeBoard
{
    /// <summary>코인·생성기·대기시간·안내를 표시하고 생성 입력을 전달한다.</summary>
    public sealed class HUDView : MonoBehaviour
    {
        [SerializeField] private Button generateButton;
        [SerializeField] private Text coinsLabel;
        [SerializeField] private Text messageLabel;
        public Button GenerateButton => generateButton;
        public event Action GenerateClicked;

        /// <summary>장면에 생성된 HUD 요소를 연결한다.</summary>
        public void Configure(Button button, Text coins, Text message)
        {
            generateButton = button; coinsLabel = coins; messageLabel = message;
        }

        private void Awake() => generateButton.onClick.AddListener(() => GenerateClicked?.Invoke());

        /// <summary>전달받은 코인·남은 시간·입력 허용 여부를 표시한다.</summary>
        public void Render(int coins, double cooldown, bool inputAvailable)
        {
            coinsLabel.text = coins + " 코인";
            generateButton.GetComponentInChildren<Text>().text = cooldown > 0 ? $"충전 중 · {cooldown:0.0}초" : "씨앗 만들기";
            generateButton.interactable = cooldown <= 0 && inputAvailable;
        }

        /// <summary>명령 처리 결과의 안내 문구를 표시한다.</summary>
        public void ShowMessage(string message) => messageLabel.text = message;
    }
}
