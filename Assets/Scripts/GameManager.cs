using UnityEngine;
using System;
using Cysharp.Threading.Tasks; // UniTask
using System.Threading; // CancellationToken
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("マネージャー参照")]
    public PlayerStatus player;
    public UIManager uiManager; // UI操作は全部これに任せる
    public ConversationManager conversationManager;

    [Header("シナリオデータ")]
    public TextAsset attendClassScenario; // ★JSONファイルをアタッチ
    public TextAsset skipClassScenario;   // ★JSONファイルをアタッチ

    [Header("授業データプール")]
    // ここに作成したLessonData（プログラミング、英語など）をドラッグして登録しておく
    public List<LessonData> allLessons;
    // 内部データ
    private DateTime currentDate = new DateTime(2025, 4, 7); // ゲーム開始日は4月7日とする
    private int currentDayOfWeek = 0; // 0=月, 1=火... 4=金
    private int currentPeriod = 1;
    // 週間スケジュール (5日 x 5限)
    // 0:月曜 ... 4:金曜
    private LessonData[,] weeklySchedule = new LessonData[5, 5];
    // 今日の時間割（nullなら空きコマとする）
    private LessonData[] todaysSchedule = new LessonData[5];
    private bool isGameRunning = false;
    // ★外部（履修登録画面）からスケジュールを受け取る関数
    public void SetSchedule(LessonData[,] schedule)
    {
        this.weeklySchedule = schedule;
        StartGame(); // 登録完了したらゲーム開始
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

        // 今日の曜日 (0~4) に基づいて、登録された授業を取得
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


    // 現在のコマの状態を確認して画面更新
    void ProcessCurrentPeriod()
    {
        uiManager.UpdateStatusDisplay(player);

        // 配列は0始まり、currentPeriodは1始まりなので -1 する
        LessonData currentLesson = weeklySchedule[currentDayOfWeek, currentPeriod - 1];

        string displayTitle;
        bool canAttend;

        if (currentLesson == null)
        {
            displayTitle = "空きコマ";
            canAttend = false; // 授業がないので出席ボタンは押せない
        }
        else
        {
            // 授業情報の詳細を表示
            displayTitle = $"{currentLesson.lessonName}\n" +
                           $"担当：{currentLesson.teacherName}\n" +
                           $"形態：{currentLesson.format}\n" +
                           $"消費体力：{currentLesson.staminaCost} / 学力上昇：{currentLesson.academicGain}";
            canAttend = true;
        }

        uiManager.UpdateCurrentPeriod(currentPeriod, displayTitle, canAttend);
    }


    void AdvancePeriod()
    {
        currentPeriod++;
        if (currentPeriod > 5)
        {
            // 次の日へ
            currentDate = currentDate.AddDays(1);
            currentDayOfWeek++;

            // 金曜(4)が終わったら月曜(0)に戻す（週末処理を入れるならここで分岐）
            if (currentDayOfWeek > 4)
            {
                currentDate = currentDate.AddDays(2); // 土日飛ばし
                currentDayOfWeek = 0;
                Debug.Log("=== 一週間終了 ===");
            }
            StartNewDay();
        }
        else
        {
            ProcessCurrentPeriod();
        }
    }

    public async void OnClickAttend()
    {
        // 1. ボタンを連打できないように無効化するなどの処理推奨
        uiManager.attendButton.interactable = false;
        uiManager.skipButton.interactable = false;
        LessonData lesson = weeklySchedule[currentDayOfWeek, currentPeriod - 1];


        // 2. 会話パートを開始し、終わるまで待機 (await)
        // This.GetCancellationTokenOnDestroy() はUniTaskの機能で、
        // ゲーム停止時などに安全にキャンセルするためのトークンを取得
        await conversationManager.StartConversation(attendClassScenario, this.GetCancellationTokenOnDestroy());

        // --- ここから下は会話が終わった後に実行される ---
        // --- パラメータ計算 ---
        // 授業データの値を使用する
        int cost = lesson.staminaCost;
        int gain = lesson.academicGain;

        // 3. ステータス計算
        float efficiency = 1.0f;
        string logMsg = "";

        if (player.stamina < 30)
        {
            efficiency = 0.5f;     // 疲れてると半分しか身につかない
            logMsg += "（疲労）";
        }
        if (player.motivation < 20)
        {
            efficiency = 0.2f;  // やる気がないとほぼ無理
            logMsg += "（やる気不足）";
        }
        if (lesson.format == LessonFormat.Online)
        {
            // オンラインはサボりやすいが、体力消費が少ないので出席しやすい
            efficiency = 0.1f;
            logMsg += "（オンライン）";
        }


        int finalAcademicGain = Mathf.FloorToInt(gain * efficiency);

        // 学力UP、体力消費(授業データ依存)、やる気消費(固定あるいはデータ依存)
        player.UpdateStatus(finalAcademicGain, -cost, -5);

        Debug.Log($"「{lesson.lessonName}」に出席。学力+{finalAcademicGain}, 体力-{cost}");

        // 次へ
        AdvancePeriod();

        // ボタン復帰などは AdvancePeriod 内の UI更新処理で行われるはず
    }


    public async void OnClickSkip()
    {
        uiManager.attendButton.interactable = false;
        uiManager.skipButton.interactable = false;

        // サボり用シナリオ再生
        await conversationManager.StartConversation(skipClassScenario, this.GetCancellationTokenOnDestroy());


        player.UpdateStatus(0, 20, 10, -5);
        Debug.Log("休憩完了");

        AdvancePeriod();
    }
}