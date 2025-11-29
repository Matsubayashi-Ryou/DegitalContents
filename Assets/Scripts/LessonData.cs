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
        int total = reportWeight + testWeight + participationWeight;
        if (total != 100)
        {
            Debug.LogWarning($"[LessonData: {name}] 採点配分の合計が {total}% になっています！ 100%になるように調整してください。");
        }
    }
}