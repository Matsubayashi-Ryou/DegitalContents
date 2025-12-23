using UnityEngine;
using System;

[Serializable] // Inspectorで確認できるようにする
public class LessonProgress
{
    public LessonData lessonData; // どの授業か

    [Header("進捗状況")]
    public int attendedCount = 0;   // 出席した回数
    public int totalClassCount = 0; // 授業があった総回数（休講は含めない想定）

    // コンストラクタ
    public LessonProgress(LessonData data)
    {
        this.lessonData = data;
    }

    // --- 成績計算ロジック ---

    // 1. 出席点 (100点満点)
    public float GetAttendanceScore()
    {
        if (totalClassCount == 0) return 100f; // まだ授業がない場合は満点扱い
        float rate = (float)attendedCount / totalClassCount;
        return Mathf.Clamp01(rate) * 100f;
    }

    // 2. 総合評価スコア計算
    // academicScore: プレイヤーのその時点の学力 (0~100想定だが青天井の場合は調整必要)
    // ここでは学力=100を基準(満点)として計算します
    public float CalculateFinalScore(int academicScore)
    {
        // 学力が200とかいくゲームバランスなら、100でキャップするか、調整係数を入れる
        // 今回は「学力そのまま」を点数として扱います (例: 学力80ならテスト80点)
        float testScore = Mathf.Clamp(academicScore, 0, 100);
        float reportScore = Mathf.Clamp(academicScore, 0, 100); // レポートも学力依存とする
        float attendanceScore = GetAttendanceScore();

        // LessonDataの配分(%)に従って合算
        float total = 0f;
        total += reportScore * (lessonData.reportWeight / 100f);
        total += testScore * (lessonData.testWeight / 100f);
        total += attendanceScore * (lessonData.participationWeight / 100f);

        return total;
    }

    // 単位取得できたか？ (60点以上)
    public bool IsPassed(int finalAcademicScore)
    {
        return CalculateFinalScore(finalAcademicScore) >= 60f;
    }
}