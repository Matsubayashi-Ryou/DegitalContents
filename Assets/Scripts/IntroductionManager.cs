using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;

public class IntroductionManager : MonoBehaviour
{
    [Header("マネージャー参照")]
    public RegistrationManager registrationManager; // 履修登録マネージャー
    public ConversationManager conversationManager; // 会話マネージャー

    [Header("UIパネル")]
    public GameObject introductionCanvas; // イントロ用のCanvas
    public Button nextButton;             // 「次へ」や「履修登録へ」ボタン

    [Header("シナリオデータ")]
    public TextAsset introScenario;       // 「さあ大学生活の始まりだ！」等の会話データ

    void Start()
    {
        // ゲーム開始時、まずイントロを開始
        PlayIntroductionFlow().Forget();
    }

    private async UniTaskVoid PlayIntroductionFlow()
    {
        // 1. 初期化：他のCanvasを隠してイントロだけ出す
        if (registrationManager.registrationCanvas != null)
            registrationManager.registrationCanvas.SetActive(false);

        if (registrationManager.mainGameCanvas != null)
            registrationManager.mainGameCanvas.SetActive(false);

        introductionCanvas.SetActive(true);

        // ボタンは最初は押せないようにしておく（会話中は非表示など）
        nextButton.gameObject.SetActive(false);
        nextButton.onClick.AddListener(OnNextButtonClicked);

        // 2. 導入の会話を再生 (シナリオが設定されていれば)
        if (introScenario != null && conversationManager != null)
        {
            // 会話が終わるまで待機
            await conversationManager.StartConversation(introScenario, this.GetCancellationTokenOnDestroy());
        }

        // 3. 会話が終わったらボタンを表示してユーザーの入力を待つ
        nextButton.gameObject.SetActive(true);

        // ※ ここで「ボタンを押すまで待機」したい場合の実装例
        // await nextButton.OnClickAsync(); // UniTaskの機能を使う場合
        // OnNextButtonClicked(); // そのまま遷移させる場合
    }

    // ボタンが押されたら呼ばれる
    void OnNextButtonClicked()
    {
        // イントロ画面を消す
        introductionCanvas.SetActive(false);

        // 履修登録を開始する
        registrationManager.BeginRegistration();
    }
}