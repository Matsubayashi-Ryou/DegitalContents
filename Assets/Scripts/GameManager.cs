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
    private int currentPeriod = 1;
    // 今日の時間割（nullなら空きコマとする）
    private LessonData[] todaysSchedule = new LessonData[5];


    void Start()
    {
        StartNewDay();
    }

    void StartNewDay()
    {
        currentPeriod = 1;
        uiManager.UpdateDate(currentDate);

        // --- 時間割生成ロジック ---
        // 表示用の文字列リストを作成
        string[] scheduleNames = new string[5];

        for (int i = 0; i < 5; i++)
        {
            // 30%の確率で空きコマ、それ以外は授業あり（仮のロジック）
            if (UnityEngine.Random.Range(0, 100) < 30)
            {
                todaysSchedule[i] = null; // 空きコマ
                scheduleNames[i] = "空きコマ";
            }
            else
            {
                // 登録された授業リストからランダムに1つ選ぶ
                int randomIndex = UnityEngine.Random.Range(0, allLessons.Count);
                todaysSchedule[i] = allLessons[randomIndex];

                // 表示用に「授業名 (形態)」みたいな文字列を作る
                string formatText = todaysSchedule[i].format == LessonFormat.Online ? "オン" : "対面";
                scheduleNames[i] = $"{todaysSchedule[i].lessonName} ({formatText})";
            }
        }

        // 時間割表を更新
        uiManager.UpdateScheduleList(scheduleNames);

        ProcessCurrentPeriod();
    }


    // 現在のコマの状態を確認して画面更新
    void ProcessCurrentPeriod()
    {
        uiManager.UpdateStatusDisplay(player);

        LessonData currentLesson = todaysSchedule[currentPeriod - 1];

        string displayTitle;
        bool canAttend;

        if (currentLesson == null)
        {
            displayTitle = "空きコマ";
            canAttend = false;
        }
        else
        {
            displayTitle = $"{currentLesson.lessonName}\n担当：{currentLesson.teacherName}\n形態：{currentLesson.format}";
            canAttend = true;
        }

        uiManager.UpdateCurrentPeriod(currentPeriod, displayTitle, canAttend);
    }


    void AdvancePeriod()
    {
        currentPeriod++;
        if (currentPeriod > 5)
        {
            currentDate = currentDate.AddDays(1);
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
        LessonData lesson = todaysSchedule[currentPeriod - 1];


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
        if (player.stamina < 30) efficiency = 0.5f;     // 疲れてると半分しか身につかない
        if (player.motivation < 20) efficiency = 0.2f;  // やる気がないとほぼ無理

        int finalAcademicGain = Mathf.FloorToInt(gain * efficiency);

        // 学力UP、体力消費(授業データ依存)、やる気消費(固定あるいはデータ依存)
        player.UpdateStatus(finalAcademicGain, -cost, -10);

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

        player.UpdateStatus(0, 20, 10);
        Debug.Log("休憩完了");

        AdvancePeriod();
    }

}