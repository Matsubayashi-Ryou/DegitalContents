using UnityEngine;

[CreateAssetMenu(fileName = "NewLesson_", menuName = "MyGame/LessonData")]
public class LessonData : ScriptableObject
{
    [Header("基本情報")]
    public string lessonName;       // 授業名
    public string teacherName;      // 教員名
    public int credits = 2;         // 単位数
    public TermType term;           // 開講区分
    public LessonFormat format;     // 授業形態
    public TermCategory category;   // 授業区分
    public bool isRequiredLottery;  // 抽選科目かどうか

    [Header("開講日時")]
    public GameDayOfWeek day;       // 曜日
    [Range(1, 5)] public int period = 1; // 時限 (1~5)

    [Header("パラメータ変動")]
    [Tooltip("一回の受講で消費する体力")]
    public int staminaCost = 10;

    [Tooltip("一回の受講で向上する学力")]
    public int academicGain = 5;

    [Header("採点区分 (合計100%になるように設定)")]
    [Range(0, 100)] public int reportWeight;       // レポート
    [Range(0, 100)] public int testWeight;         // テスト
    [Range(0, 100)] public int participationWeight; // 授業参画度（出席点）

    // エディタ上で値が変更されたときに呼ばれる検証用関数
    private void OnValidate()
    {
        // 採点配分チェック
        int total = reportWeight + testWeight + participationWeight;
        if (total != 100)
        {
            Debug.LogWarning($"[LessonData: {name}] 採点配分の合計が {total}% になっています！");
        }

        // 時限チェック（念の為）
        if (period < 1 || period > 5)
        {
            Debug.LogError($"[LessonData: {name}] 時限は1～5の間で設定してください。");
            period = Mathf.Clamp(period, 1, 5);
        }
    }

    // UI表示用に「月1」のような文字列を返す便利関数
    public string GetTimeSlotString()
    {
        string dayStr = day switch
        {
            GameDayOfWeek.Monday => "月",
            GameDayOfWeek.Tuesday => "火",
            GameDayOfWeek.Wednesday => "水",
            GameDayOfWeek.Thursday => "木",
            GameDayOfWeek.Friday => "金",
            _ => "？"
        };
        return $"{dayStr}{period}";
    }
}