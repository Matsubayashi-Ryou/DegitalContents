using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("ステータス表示 (Slider & Text)")]
    // 体力
    public Slider staminaSlider;
    public Image staminaFillImage; // 色を変えるため
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
    // 企画書には4パラメータとありますが、やる気も重要なのでとりあえず表示しておきます
    // 必要なければ非表示でもOK
    public TextMeshProUGUI motivationText;

    [Header("スケジュール・日付表示")]
    public TextMeshProUGUI dateText;
    public TextMeshProUGUI nextScheduleText;
    public TextMeshProUGUI[] periodTexts; // 1~5限の一覧

    [Header("操作ボタン")]
    public Button attendButton;
    public Button skipButton;

    // 追加ボタン
    public Button studyButton;  // 課題・自習（学力UP）
    public Button playButton;   // 遊ぶ（人間性UP）
    public Button workButton;   // バイト（財力UP）

    // 危険域の色
    private Color normalColor = new Color(0.2f, 0.8f, 0.2f); // 緑っぽい色
    private Color dangerColor = new Color(1.0f, 0.3f, 0.3f); // 赤っぽい色

    // ボタンの表示状態を整理する関数
    public void UpdateActionButtons(bool hasClass, bool isShiftDay)
    {
        // 一旦全部リセット
        attendButton.gameObject.SetActive(false);
        skipButton.gameObject.SetActive(true); // 休むはいつでも選べる
        studyButton.gameObject.SetActive(true);
        playButton.gameObject.SetActive(true);
        workButton.gameObject.SetActive(true);

        // 授業がある場合
        if (hasClass)
        {
            attendButton.gameObject.SetActive(true);

            // 授業がある時間は、サボらないと他のことはできない
            // UI設計としては「サボる(Rest)」を押した後に自由行動メニューを出すのが綺麗ですが
            // 今回はシンプルに「授業があるなら授業ボタンがメイン」とします
            studyButton.interactable = false;
            playButton.interactable = false;
            workButton.interactable = false;
        }
        else
        {
            // 空きコマ・放課後の場合
            attendButton.gameObject.SetActive(false);

            studyButton.interactable = true;
            playButton.interactable = true;

            // シフトが入っている日ならバイトボタン有効、そうでなければ無効（あるいは臨時バイト？）
            // 仕様：シフト設定した日はバイトが推奨される
            if (isShiftDay)
            {
                workButton.interactable = true;
                // シフトの日は遊びにくい…とするなら playButton.interactable = false; とかもアリ
            }
            else
            {
                // シフトじゃない日はバイトできない（あるいはヘルプ待ち）
                workButton.interactable = false;
            }
        }
    }

    // ステータスを一括更新するメソッド
    public void UpdateStatusDisplay(PlayerStatus player)
    {
        // --- 体力 (0～100) ---
        UpdateSingleStatus(staminaSlider, staminaFillImage, staminaText, player.stamina, 100, "体力");

        // --- 学力 (上限なしだが、スライダー表示用に仮に200をMAXとする) ---
        UpdateSingleStatus(academicSlider, academicFillImage, academicText, player.academic, 200, "学力");

        // --- 財力 (仮に 100,000 をMAXとする) ---
        UpdateSingleStatus(moneySlider, moneyFillImage, moneyText, player.money, 50000, "財力");

        // --- 人間性 (仮に 100 をMAXとする) ---
        UpdateSingleStatus(humanitySlider, humanityFillImage, humanityText, player.humanity, 100, "人間性");

        // やる気（テキストのみ更新）
        if (motivationText != null)
            motivationText.text = $"やる気: {player.motivation}";
    }

    // 個別のスライダーとテキストを更新するヘルパー関数
    private void UpdateSingleStatus(Slider slider, Image fillImage, TextMeshProUGUI text, int currentValue, int maxValue, string label)
    {
        // スライダー設定
        slider.maxValue = maxValue;
        slider.value = currentValue;

        // テキスト設定
        text.text = $"{label}: {currentValue}";

        // 色の変化 (残り1割以下なら赤)
        float percentage = (float)currentValue / maxValue;
        if (percentage <= 0.1f)
        {
            fillImage.color = dangerColor;
        }
        else
        {
            fillImage.color = normalColor;
        }
    }

    // 日付更新
    public void UpdateDate(System.DateTime date)
    {
        dateText.text = $"{date.Month}月{date.Day}日";
    }

    // 今日の時間割リスト更新
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

    // 現在のコマ情報更新
    public void UpdateCurrentPeriod(int period, string subjectName, bool isAttendable)
    {
        nextScheduleText.text = $"現在 {period}限目\n内容：{subjectName}";
        attendButton.interactable = isAttendable;
        skipButton.interactable = true; // スキップは常に可能とする
    }
}