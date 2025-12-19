using UnityEngine;
using System;
using Cysharp.Threading.Tasks; // UniTask
using System.Threading; // CancellationToken
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("マネージャー参照")]
    public PlayerStatus player;
    public UIManager uiManager;
    public ConversationManager conversationManager;

    [Header("基本シナリオ")]
    public TextAsset attendClassScenario; // 授業出席
    public TextAsset skipClassScenario;   // サボり
    public TextAsset LunchScenario;       // 昼休み

    public TextAsset testScenario; // ★テスト用シナリオを追加

    [Header("授業データプール")]
    // ここに作成したLessonData（プログラミング、英語など）をドラッグして登録しておく
    public List<LessonData> allLessons;

    // 内部データ
    private DateTime currentDate = new DateTime(2025, 4, 7); // ゲーム開始日は4月7日とする
    private int currentDayOfWeek = 0; // 0=月, 1=火... 4=金
    private int currentPeriod = 1;
    // 週間スケジュール (5日 x 5限)
    // 0:月曜 ... 4:金曜
    private int currentWeek = 1;
    private LessonData[,] weeklySchedule = new LessonData[5, 5];

    public void SetSchedule(LessonData[,] schedule)
    {
        this.weeklySchedule = schedule;
        StartGame();
    }

    void StartGame()
    {
        isGameRunning = true;
        StartNewDay();
    }

    void StartNewDay()
    {
        currentPeriod = 1;
        uiManager.UpdateDate(currentDate);

        if (currentDayOfWeek == 0 && currentPeriod == 1)
        {
            CheckSpecialEvents().Forget(); // 非同期でチェック
        }

        // 今日の曜日 (0~4) に基づいて、登録された授業を取得
        string[] scheduleNames = new string[5];
        for (int i = 0; i < 5; i++)
        {
            LessonData lesson = weeklySchedule[currentDayOfWeek, i];
            scheduleNames[i] = (lesson == null) ? "空きコマ" : lesson.lessonName;
        }
        uiManager.UpdateScheduleList(scheduleNames);
        ProcessCurrentPeriod();
    }

    private async UniTaskVoid CheckSpecialEvents()
    {
        {
            Debug.Log($"第{currentWeek}週：テスト期間開始！");

            // UI操作を一時無効化
            uiManager.attendButton.interactable = false;
            uiManager.skipButton.interactable = false;

            // テストシナリオを再生（会話が終わるまで待機）
            await conversationManager.StartConversation(testScenario, this.GetCancellationTokenOnDestroy());

            // テスト結果に応じたステータス変化などが必要ならここに記述
            // player.UpdateStatus(0, -20, -10); 

            // UIを元に戻す
            ProcessCurrentPeriod();
        }
    }


    // 現在のコマの状態を確認して画面更新
    void ProcessCurrentPeriod()

        LessonData currentLesson = weeklySchedule[currentDayOfWeek, currentPeriod - 1];
    string displayTitle;
    bool canAttend;

        if (currentLesson == null)
        {
            displayTitle = "空きコマ";
            canAttend = false;
        }
        else
{
    displayTitle = $"{currentLesson.lessonName}\n担当：{currentLesson.teacherName}";
    canAttend = true;
}

uiManager.UpdateCurrentPeriod(currentPeriod, displayTitle, canAttend);
    }

    // --- 時限を進める処理 ---
    async UniTask AdvancePeriod()
{
    currentPeriod++;

    // 5限終了後（放課後）
    if (currentPeriod > 5)
    {
        Debug.Log("放課後になりました。");

        // ★トリガー1：放課後のランダムイベント (30%)
        await CheckAndTriggerRandomEvent("AfterSchool");

        // 日付更新
        currentDate = currentDate.AddDays(1);
        currentDayOfWeek++;

        if (currentDayOfWeek > 4) // 金曜終了
        {
            currentDate = currentDate.AddDays(2); // 土日スキップ
            currentDayOfWeek = 0;
            currentWeek++;
            Debug.Log($"=== 第{currentWeek}週目開始 ===");
        }
        StartNewDay();
    }
    else
    {
        ProcessCurrentPeriod();
    }
}

// 「出席」ボタン
public async void OnClickAttend()
{
    uiManager.attendButton.interactable = false;
    uiManager.skipButton.interactable = false;
    LessonData lesson = weeklySchedule[currentDayOfWeek, currentPeriod - 1];

    // 1. 授業会話
    await conversationManager.StartConversation(attendClassScenario, this.GetCancellationTokenOnDestroy());

    // 2. パラメータ計算
    float efficiency = 1.0f;

    // 勉強のやる気が低いと効率ダウン
    if (player.motivation < 20)
    {
        efficiency = 0.2f;
        Debug.Log("勉強のやる気が低いため、身につかなかった...");
    }
    else if (player.stamina < 30)
    {
        efficiency = 0.5f;
        Debug.Log("疲労で集中できなかった...");
    }

    int finalAcademicGain = Mathf.FloorToInt(lesson.academicGain * efficiency);
    int staminaCost = lesson.staminaCost;

    // ★名前付き引数で指定（順番を気にしなくて済む）
    player.UpdateStatus(
        academicChg: finalAcademicGain,
        staminaChg: -staminaCost,
        motivationChg: -5 // 授業で少し疲れてやる気ダウン
    );

    Debug.Log($"授業完了: 学力+{finalAcademicGain}");

    await AdvancePeriod();
}

// 「サボる / 休憩」ボタン
public async void OnClickSkip()
{
    uiManager.attendButton.interactable = false;
    uiManager.skipButton.interactable = false;

    LessonData currentLesson = weeklySchedule[currentDayOfWeek, currentPeriod - 1];
    bool isEmptySlot = (currentLesson == null);

    // 1. 基本行動
    if (currentPeriod == 3) // 昼休み
    {
        await conversationManager.StartConversation(LunchScenario, this.GetCancellationTokenOnDestroy());
        // 昼食: 体力+10, やる気+5
        player.UpdateStatus(staminaChg: 10, motivationChg: 5);
    }
    else if (isEmptySlot) // 空きコマ
    {
        Debug.Log("空きコマを過ごします。");
        // 休憩: 体力+5, やる気+5
        player.UpdateStatus(staminaChg: 5, motivationChg: 5);
    }
    else // サボり
    {
        await conversationManager.StartConversation(skipClassScenario, this.GetCancellationTokenOnDestroy());
        // サボり: 体力+20, やる気-5 (罪悪感)
        player.UpdateStatus(staminaChg: 20, motivationChg: -5);
    }

    // 2. ★ランダムイベント判定 (昼休み以外)
    if (currentPeriod != 3)
    {
        string triggerType = isEmptySlot ? "EmptySlot" : "SkipClass";
        await CheckAndTriggerRandomEvent(triggerType);
    }

    await AdvancePeriod();
}

// --- ランダムイベント判定 ---
private async UniTask CheckAndTriggerRandomEvent(string triggerType)
{
    // 確率判定 (30%)
    if (UnityEngine.Random.Range(1, 101) > 30) return;

    Debug.Log($"ランダムイベント発生！ ({triggerType})");

    if (randomEventScenarios.Count == 0) return;

    int index = UnityEngine.Random.Range(0, randomEventScenarios.Count);
    TextAsset selectedScenario = randomEventScenarios[index];

    // 会話再生
    await conversationManager.StartConversation(selectedScenario, this.GetCancellationTokenOnDestroy());

    // イベント効果適用
    ApplyRandomEventEffect(index);
}

// イベント効果（仮）
private void ApplyRandomEventEffect(int eventIndex)
{
    switch (eventIndex)
    {
        case 0: // 友達と遊んだ
                // 財力-3000, 人間性+5, やる気+10 (リフレッシュ)
            player.UpdateStatus(
                moneyChg: -3000,
                humanityChg: 5,
                motivationChg: 10,
                staminaChg: -10
            );
            Debug.Log("イベント: 友達と遊んでリフレッシュ！");
            break;

        case 1: // バイトヘルプ
                // 財力+5000, 体力-30, やる気-10 (疲れ)
            player.UpdateStatus(
                moneyChg: 5000,
                staminaChg: -30,
                motivationChg: -10
            );
            Debug.Log("イベント: バイトで稼いだが疲れた...");
            break;

        case 2: // 自習
                // 学力+10, 体力-10
            player.UpdateStatus(
                academicChg: 10,
                staminaChg: -10
            );
            Debug.Log("イベント: 自習した。");
            break;
    }
}
}
