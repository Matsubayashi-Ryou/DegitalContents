using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    [Header("マネージャー参照")]
    public PlayerStatus player;
    public UIManager uiManager; // UI操作は全部これに任せる

    // 内部データ
    private DateTime currentDate = new DateTime(2025, 4, 1);
    private int currentPeriod = 1;
    private string[] todaysSchedule = new string[5];

    private string[] subjectList = {
        "基礎プログラミング", "コンピューティング", "英語 I", "体育",
        "線形代数", "心理学概論",
        "空きコマ", "休講"
    };

    void Start()
    {
        StartNewDay();
    }

    void StartNewDay()
    {
        currentPeriod = 1;

        // 1. 日付更新をUIに依頼
        uiManager.UpdateDate(currentDate);

        // 2. 時間割生成
        for (int i = 0; i < 5; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, subjectList.Length);
            todaysSchedule[i] = subjectList[randomIndex];
        }

        // 3. 時間割表示をUIに依頼
        uiManager.UpdateScheduleList(todaysSchedule);

        ProcessCurrentPeriod();
    }

    // 現在のコマの状態を確認して画面更新
    void ProcessCurrentPeriod()
    {
        // ステータス更新をUIに依頼
        uiManager.UpdateStatusDisplay(player);

        string subjectName = todaysSchedule[currentPeriod - 1];
        bool canAttend = !(subjectName == "空きコマ" || subjectName == "休講");

        // 今の授業表示とボタン制御をUIに依頼
        uiManager.UpdateCurrentPeriod(currentPeriod, subjectName, canAttend);
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

    public void OnClickAttend()
    {
        int efficiency = 1;
        // ※ここのロジックは後でもっと複雑になる（GameManagerの仕事）
        if (player.stamina < 30 || player.motivation < 20) efficiency = 0;

        int academicGain = 10 * efficiency;
        player.UpdateStatus(academicGain, -20, -10); // 体力・やる気消費

        Debug.Log("授業に出席");
        AdvancePeriod();
    }

    public void OnClickSkip()
    {
        player.UpdateStatus(0, 20, 10); // 回復
        Debug.Log("休憩");
        AdvancePeriod();
    }
}