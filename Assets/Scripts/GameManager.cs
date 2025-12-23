using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

#nullable enable
public class GameManager : MonoBehaviour
{
    [Header("マネージャー参照")]
    public PlayerStatus player;
    public UIManager uiManager;// UI操作は全部これに任せる
    public ConversationManager conversationManager;

    [Header("基本シナリオデータ")]
    public TextAsset attendClassScenario; // 授業出席
    public TextAsset skipClassScenario;   // サボり
    public TextAsset LunchScenario;       // 昼休み
    public TextAsset testScenario;        // テスト用

    [Header("データプール")]
    public List<LessonData> allLessons;

    // --- ★追加: ランダムイベントのリスト ---
    [Header("ランダムイベント設定")]
    public List<RandomEventData> randomEvents;

    // 内部データ
    private DateTime currentDate = new DateTime(2025, 4, 7);
    private int currentDayOfWeek = 0; // 0=月...
    private int currentPeriod = 1;
    private int currentWeek = 1;

    private LessonData[,] weeklySchedule = new LessonData[6, 5];
    private bool[] shiftDays = new bool[6];
    private bool isGameRunning = false;

    // 外部（履修登録画面）からスケジュールを受け取る
    public void SetSchedule(LessonData[,] schedule, bool[] shifts)
    {
        this.weeklySchedule = schedule;
        this.shiftDays = shifts;
        StartGame();
    }

    void StartGame()
    {
        isGameRunning = true;
        StartNewDay();
    }

    async void StartNewDay()
    {
        currentPeriod = 1;
        uiManager.UpdateDate(currentDate);

        if (currentDayOfWeek == 0 && currentPeriod == 1)
        {
            await CheckSpecialEvents();
        }

        // 今日のスケジュール名をUIへ
        string[] scheduleNames = new string[5];
        for (int i = 0; i < 5; i++)
        {
            LessonData lesson = weeklySchedule[currentDayOfWeek, i];
            if (lesson == null)
            {
                scheduleNames[i] = "空きコマ";
            }
            else
            {
                string formatText = lesson.format == LessonFormat.Online ? "オン" : "対面";
                scheduleNames[i] = $"{lesson.lessonName} ({formatText})";
            }
        }
        uiManager.UpdateScheduleList(scheduleNames);
        ProcessCurrentPeriod();
    }

    // テスト期間などの特別イベント
    private async UniTask CheckSpecialEvents()
    {
        // testScenario が設定されていない場合の安全策を追加
        if (testScenario == null) return;
        Debug.Log($"第{currentWeek}週：テスト期間開始！");
        // UI操作を一時無効化
        uiManager.attendButton.interactable = false;
        uiManager.skipButton.interactable = false;

        await conversationManager.StartConversation(testScenario, this.GetCancellationTokenOnDestroy());
    }

    async void ProcessCurrentPeriod()
    {
        uiManager.UpdateStatusDisplay(player);

        // 1. 基本情報の取得
        LessonData currentLesson = weeklySchedule[currentDayOfWeek, currentPeriod - 1];
        bool hasClass = (currentLesson != null);
        bool isShift = shiftDays[currentDayOfWeek];

        // 2. 現在のタイミング（Context）を判定
        EventTiming timing = EventTiming.FreeTime; // デフォルト
        if (currentPeriod == 1) timing = EventTiming.Morning;
        else if (hasClass) timing = EventTiming.BeforeClass;
        // ※「放課後」はAdvancePeriodで判定するならここでは不要

        // 3. ランダムイベントの抽選チェック
        RandomEventData triggeredEvent = CheckForEvent(timing);

        // イベントが発生した場合
        if (triggeredEvent != null)
        {
            Debug.Log($"イベント発生: {triggeredEvent.eventName}");
            uiManager.SetInputActive(false); // ボタンロック

            bool isChoiceEvent = !string.IsNullOrEmpty(triggeredEvent.choiceAText);
            bool accepted = true;

            // ダイアログ表示
            if (isChoiceEvent)
            {
                accepted = await uiManager.ShowConfirmDialog(
                    $"{triggeredEvent.description}",
                    triggeredEvent.choiceAText,
                    triggeredEvent.choiceBText
                );
            }
            else
            {
                // 選択肢がない＝強制イベント（ボタンは「OK」のみなど）
                await uiManager.ShowConfirmDialog($"{triggeredEvent.eventName}\n{triggeredEvent.description}", "OK", "");
            }

            // 効果適用
            if (accepted)
            {
                ApplyEventEffect(triggeredEvent, true); // Aの効果

                // 強制スキップ効果（寝坊して授業に出られなかった等）
                if (triggeredEvent.effectTypeA == EventEffectType.ForceSkip)
                {
                    uiManager.SetInputActive(true);
                    await AdvancePeriod(); // 時間を進めて終了
                    return;
                }
            }
            else
            {
                ApplyEventEffect(triggeredEvent, false); // Bの効果
            }

            uiManager.SetInputActive(true); // ロック解除

            // イベントによって「休講(BecomeFree)」が発生した場合、hasClassの状態を再取得
            currentLesson = weeklySchedule[currentDayOfWeek, currentPeriod - 1];
            hasClass = (currentLesson != null);
        }

        // 4. 通常の画面表示
        string displayTitle;
        bool canAttend;


        if (hasClass)
        {
            displayTitle = $"【授業】{currentLesson.lessonName}\n{currentPeriod}限目";
            canAttend = true;
        }
        else
        {
            displayTitle = isShift ? "【空きコマ】本日はバイトの日です" : "【空きコマ】自由時間";
            canAttend = false;
        }

        uiManager.nextScheduleText.text = displayTitle;
        uiManager.UpdateActionButtons(hasClass, isShift);

        if (hasClass)
        {
            uiManager.UpdateCurrentPeriod(currentPeriod, $"{currentLesson.lessonName}...", canAttend);
        }
        else
        {
            uiManager.UpdateCurrentPeriod(currentPeriod, displayTitle, canAttend);
        }
    }

    // --- ★追加: イベント抽選ロジック ---
    private RandomEventData CheckForEvent(EventTiming currentTiming)
    {
        if (randomEvents == null || randomEvents.Count == 0) return null;

        bool hasClassNext = false;
        if (currentPeriod < 5)
            hasClassNext = (weeklySchedule[currentDayOfWeek, currentPeriod] != null);

        // 今日の残りの授業があるかチェック
        bool hasClassesRemaining = false;
        for (int i = currentPeriod; i < 5; i++)
        {
            if (weeklySchedule[currentDayOfWeek, i] != null)
            {
                hasClassesRemaining = true;
                break;
            }
        }

        List<RandomEventData> candidates = new List<RandomEventData>();

        foreach (var evt in randomEvents)
        {
            // 条件チェック
            if (evt.timing != currentTiming) continue;
            if (evt.requiresNextClassExists && !hasClassNext) continue;
            if (evt.requiresNextClassEmpty && hasClassNext) continue;
            if (evt.requiresNoClassesRemaining && hasClassesRemaining) continue;

            // 確率チェック
            if (UnityEngine.Random.Range(0, 100) < evt.probability)
            {
                candidates.Add(evt);
            }
        }

        if (candidates.Count > 0)
        {
            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }
        return null;
    }

    // --- ★追加: イベント効果適用ロジック ---
    private void ApplyEventEffect(RandomEventData evt, bool isChoiceA)
    {
        int st = isChoiceA ? evt.staminaChgA : evt.staminaChgB;
        int ac = isChoiceA ? evt.academicChgA : evt.academicChgB;
        int mo = isChoiceA ? evt.moneyChgA : evt.moneyChgB;
        int hu = isChoiceA ? evt.humanityChgA : evt.humanityChgB;
        int mot = isChoiceA ? evt.motivationChgA : evt.motivationChgB;
        EventEffectType type = isChoiceA ? evt.effectTypeA : evt.effectTypeB;

        player.UpdateStatus(academicChg: ac, staminaChg: st, moneyChg: mo, humanityChg: hu, motivationChg: mot);
        Debug.Log($"イベント効果適用: {(isChoiceA ? "A" : "B")}");

        // 特殊効果: 休講
        if (type == EventEffectType.BecomeFree)
        {
            Debug.Log("休講になりました。");
            weeklySchedule[currentDayOfWeek, currentPeriod - 1] = null;
        }
    }


    // --- 時限を進める処理 ---
    async UniTask AdvancePeriod()
    {
        currentPeriod++;

        // 5限終了後（放課後）
        if (currentPeriod > 5)
        {
            await EndDayProcess();

        }
        else
        {
            ProcessCurrentPeriod();
        }
    }
    async Task EndDayProcess()
    {
        Debug.Log("放課後になりました。");

        // トリガー：放課後のランダムイベント (30%)
        await CheckAndTriggerRandomEvent("AfterSchool");

        // 放課後用のランダムイベント判定
        // 放課後は「次がない」のでタイミングのみチェック
        RandomEventData triggeredEvent = CheckForEvent(EventTiming.AfterSchool);
        if (triggeredEvent != null)
        {
            // ※ここでは簡易的にログのみ。必要ならProcessCurrentPeriod同様にダイアログ処理を入れる
            Debug.Log($"放課後イベント発生: {triggeredEvent.eventName}");
        }

        // 日付更新
        currentDate = currentDate.AddDays(1);
        currentDayOfWeek++;

        if (currentDayOfWeek > 5)
        {
            currentDate = currentDate.AddDays(1); // 日飛ばし
            currentDayOfWeek = 0;
            currentWeek++;
            Debug.Log($"=== 第{currentWeek}週目開始 ===");
        }
        Debug.Log("=== 一日の終了 ===");

        // ★睡眠処理：体力+50
        player.UpdateAllStats(0, 50, 0, 0, 0);
        Debug.Log("睡眠をとりました (体力+50)");
        StartNewDay();
        Debug.Log("放課後になりました。");
    }

    // 「出席」ボタン
    public async void OnClickAttend()
    {
        uiManager.SetInputActive(false);
        uiManager.attendButton.interactable = false;
        uiManager.skipButton.interactable = false;
        LessonData lesson = weeklySchedule[currentDayOfWeek, currentPeriod - 1];

        // 1. 授業会話
        await conversationManager.StartConversation(attendClassScenario, this.GetCancellationTokenOnDestroy());

        // 2. パラメータ計算
        int cost = lesson.staminaCost;
        int gain = lesson.academicGain;
        float efficiency = 1.0f;

        if (player.motivation < 20)
        {
            efficiency = 0.2f;
            Debug.Log("やる気不足...");
        }
        else if (player.stamina < 30)
        {
            efficiency = 0.5f;
            Debug.Log("疲労...");
        }
        if (lesson.format == LessonFormat.Online)
        {
            efficiency = 0.1f; // オンライン授業の効率（仮）
        }

        int finalAcademicGain = Mathf.FloorToInt(lesson.academicGain * efficiency);
        int staminaCost = lesson.staminaCost;

        player.UpdateStatus(
            academicChg: finalAcademicGain,
            staminaChg: -staminaCost,
            motivationChg: -5
        );

        Debug.Log($"「{lesson.lessonName}」に出席。");
        await AdvancePeriod();
    }

    // 2. 課題・自習
    public async void OnClickStudy()
    {
        uiManager.studyButton.interactable = false; // 連打防止

        // 学力+10, 体力-10
        player.UpdateAllStats(10, -10, 0, 0, 0);

        Debug.Log("課題・自習: 学力+10, 体力-10");
        await AdvancePeriod();
    }

    // 3. 遊ぶ (財力-3000, 人間性+30)
    public async void OnClickPlay()
    {
        if (player.money < 3000) return; // お金不足チェック

        uiManager.playButton.interactable = false;

        // 財力-3000, 人間性+30
        player.UpdateAllStats(0, 0, -3000, 30, 0);

        Debug.Log("遊ぶ: 財力-3000, 人間性+30");
        await AdvancePeriod();
    }

    // 4. バイト (財力+3000, 体力-40)
    public async void OnClickWork()
    {
        uiManager.workButton.interactable = false;

        // 体力-40, 財力+3000
        player.UpdateAllStats(0, -40, 3000, 0, 0);

        Debug.Log("バイト: 財力+3000, 体力-40");
        await AdvancePeriod();
    }
    // 5. 休む (体力回復)
    public async Task OnClickRest()
    {
        player.UpdateAllStats(0, 30, 0, 0, 10);
        Debug.Log("休憩した");
        await AdvancePeriod();
    }

    // 「サボる / 休憩」ボタン（空きコマ処理含む）
    public async void OnClickSkip()
    {
        uiManager.attendButton.interactable = false;
        uiManager.skipButton.interactable = false;
        LessonData currentLesson = weeklySchedule[currentDayOfWeek, currentPeriod - 1];
        bool isEmptySlot = (currentLesson == null);

        // 基本行動
        if (currentPeriod == 3) // 昼休み
        {
            await conversationManager.StartConversation(LunchScenario, this.GetCancellationTokenOnDestroy());
            player.UpdateStatus(staminaChg: 10, motivationChg: 5);
        }
        else if (isEmptySlot) // 空きコマ
        {
            Debug.Log("空きコマを過ごします。");
            player.UpdateStatus(staminaChg: 5, motivationChg: 5);
        }
        else // サボり（自分の意志で）
        {
            await conversationManager.StartConversation(skipClassScenario, this.GetCancellationTokenOnDestroy());
            player.UpdateStatus(staminaChg: 20, motivationChg: -5, academicChg: -5);
        }

        // 2. ★ランダムイベント判定 (昼休み以外)
        if (currentPeriod != 3)
        {
            string triggerType = isEmptySlot ? "EmptySlot" : "SkipClass";
            await CheckAndTriggerRandomEvent(triggerType);
        }

        await AdvancePeriod();
    }
    private async UniTask CheckAndTriggerRandomEvent(string triggerType)
    {
        // 確率判定 (30%)
        if (UnityEngine.Random.Range(1, 101) > 30) return;

        Debug.Log($"ランダムイベント発生！ ({triggerType})");

        //if (randomEventScenarios.Count == 0) return;

        // int index = UnityEngine.Random.Range(0, randomEventScenarios.Count);
        // TextAsset selectedScenario = randomEventScenarios[index];

        // 会話再生
        //await conversationManager.StartConversation(selectedScenario, this.GetCancellationTokenOnDestroy());

        // イベント効果適用
        //ApplyRandomEventEffect(index);
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
