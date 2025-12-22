using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;

public class UIManager : MonoBehaviour
{
    [Header("ステータス表示 (Slider & Text)")]
    // 体力
    public Slider staminaSlider;
    public Image staminaFillImage;
    public TextMeshProUGUI staminaText;

    // 学力
    public Slider academicSlider;
    public Image academicFillImage;
    public TextMeshProUGUI academicText;

    // 財力
    public Slider moneySlider;
    public Image moneyFillImage;
    public TextMeshProUGUI moneyText;

    // 人間性
    public Slider humanitySlider;
    public Image humanityFillImage;
    public TextMeshProUGUI humanityText;

    [Header("やる気 (内部パラメータ用)")]
    public TextMeshProUGUI motivationText;

    [Header("スケジュール・日付表示")]
    public TextMeshProUGUI dateText;
    public TextMeshProUGUI nextScheduleText;
    public TextMeshProUGUI[] periodTexts; // 1~5限の一覧

    [Header("操作ボタン")]
    public Button attendButton;
    public Button skipButton;

    // 追加ボタン
    public Button studyButton;  // 課題・自習
    public Button playButton;   // 遊ぶ
    public Button workButton;   // バイト

    // --- ★追加: イベントダイアログ用UI ---
    [Header("イベントダイアログ")]
    public GameObject confirmDialogPanel; // 背景パネル
    public TextMeshProUGUI dialogMessageText;
    public TextMeshProUGUI yesButtonText; // ボタンラベル変更用
    public TextMeshProUGUI noButtonText;
    public Button dialogYesButton;
    public Button dialogNoButton;

    // ダイアログの結果待ちに使う
    private UniTaskCompletionSource<bool> dialogSource;

    // 危険域の色
    private Color normalColor = new Color(0.2f, 0.8f, 0.2f);
    private Color dangerColor = new Color(1.0f, 0.3f, 0.3f);

    void Start()
    {
        // ダイアログボタンの初期化
        if (dialogYesButton != null)
            dialogYesButton.onClick.AddListener(() => OnDialogResult(true));

        if (dialogNoButton != null)
            dialogNoButton.onClick.AddListener(() => OnDialogResult(false));

        if (confirmDialogPanel != null)
            confirmDialogPanel.SetActive(false);
    }

    // --- ★追加: ダイアログを表示して結果を待つ非同期関数 ---
    public async UniTask<bool> ShowConfirmDialog(string message, string yesLabel = "はい", string noLabel = "いいえ")
    {
        dialogMessageText.text = message;
        if (yesButtonText) yesButtonText.text = yesLabel;
        if (noButtonText) noButtonText.text = noLabel;

        // Noボタンのテキストが空ならボタン自体を隠す（強制イベント用）
        if (string.IsNullOrEmpty(noLabel) && dialogNoButton != null)
            dialogNoButton.gameObject.SetActive(false);
        else if (dialogNoButton != null)
            dialogNoButton.gameObject.SetActive(true);

        confirmDialogPanel.SetActive(true);

        // ボタンが押されるまで待機
        dialogSource = new UniTaskCompletionSource<bool>();
        bool result = await dialogSource.Task;

        confirmDialogPanel.SetActive(false);
        return result;
    }

    void OnDialogResult(bool result)
    {
        dialogSource?.TrySetResult(result);
    }

    // --- ★追加: イベント中にメイン画面のボタンをロックする ---
    public void SetInputActive(bool isActive)
    {
        attendButton.interactable = isActive;
        skipButton.interactable = isActive;
        studyButton.interactable = isActive;
        playButton.interactable = isActive;
        workButton.interactable = isActive;
    }

    // ボタンの表示状態を整理する関数
    public void UpdateActionButtons(bool hasClass, bool isShiftDay)
    {
        // 一旦全部リセット
        attendButton.gameObject.SetActive(true);
        skipButton.gameObject.SetActive(true);
        studyButton.gameObject.SetActive(true);
        playButton.gameObject.SetActive(true);
        workButton.gameObject.SetActive(true);

        // 授業がある場合
        if (hasClass)
        {
            // --- 授業がある時間 ---
            attendButton.interactable = true; // 出席できる
            skipButton.interactable = true;   // サボって休める

            // 授業がある時間は他をロック
            studyButton.interactable = false;
            playButton.interactable = false;
            workButton.interactable = false;
        }
        else
        {
            // 空きコマ・放課後の場合
            attendButton.gameObject.SetActive(false);

            skipButton.interactable = true;
            studyButton.interactable = true;
            playButton.interactable = true;

            if (isShiftDay)
            {
                workButton.interactable = true;
            }
            else
            {
                workButton.interactable = false;
            }
        }
    }

    // ステータスを一括更新するメソッド
    public void UpdateStatusDisplay(PlayerStatus player)
    {
        UpdateSingleStatus(staminaSlider, staminaFillImage, staminaText, player.stamina, 100, "体力");
        UpdateSingleStatus(academicSlider, academicFillImage, academicText, player.academic, 200, "学力");
        UpdateSingleStatus(moneySlider, moneyFillImage, moneyText, player.money, 50000, "財力");
        UpdateSingleStatus(humanitySlider, humanityFillImage, humanityText, player.humanity, 100, "人間性");

        if (motivationText != null)
            motivationText.text = $"やる気: {player.motivation}";
    }

    // 個別のスライダー更新
    private void UpdateSingleStatus(Slider slider, Image fillImage, TextMeshProUGUI text, int currentValue, int maxValue, string label)
    {
        slider.maxValue = maxValue;
        slider.value = currentValue;
        text.text = $"{label}: {currentValue}";

        float percentage = (float)currentValue / maxValue;
        if (percentage <= 0.1f) fillImage.color = dangerColor;
        else fillImage.color = normalColor;
    }

    public void UpdateDate(System.DateTime date)
    {
        dateText.text = $"{date.Month}月{date.Day}日";
    }

    public void UpdateScheduleList(string[] schedules)
    {
        for (int i = 0; i < 5; i++)
        {
            if (periodTexts.Length > i)
            {
                periodTexts[i].text = $"{i + 1}限: {schedules[i]}";
            }
        }
    }

    public void UpdateCurrentPeriod(int period, string subjectName, bool isAttendable)
    {
        nextScheduleText.text = $"現在 {period}限目\n内容：{subjectName}";
        attendButton.interactable = isAttendable;
        skipButton.interactable = true;
    }
}