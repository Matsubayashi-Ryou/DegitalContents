using UnityEngine;

public class ExamManager : MonoBehaviour
{
    // プレイヤーのステータス（本来はPlayerStatsクラスなどで管理推奨）
    public int currentKnowledge = 50; // 現在の学力
    public int currentStress = 0;     // ストレス値

    [System.Serializable]
    public class ExamResult
    {
        public string subjectName;
        public int score;      // 0-100
        public string grade;   // S, A, B, C, F
        public bool isPassed;  // 合否
    }

    [Header("Settings")]
    public int passingScore = 60;     // 合格ライン

    // 前回のEventManagerへの参照
    [SerializeField] private EventManager eventManager;

    // テスト期間専用のイベントリスト（インスペクターでJSON文字列を登録）
    [SerializeField] private System.Collections.Generic.List<string> examStudyEvents; // 勉強イベント
    [SerializeField] private System.Collections.Generic.List<string> examSkipEvents;  // サボりイベント

    public ExamResult ExecuteExam(LessonData lesson, PlayerStatus player)
    {
        // 1. 基礎点（学力依存）: 最大70点
        // 学力0で0点、学力100で70点くらいのイメージ
        float baseScore = Mathf.Clamp(player.academic * 0.7f, 0, 70);

        // 2. コンディション補正（やる気・体力）: 最大20点
        // やる気が高いと点数が伸びる
        float conditionBonus = (player.motivation / 100f) * 10f + (player.stamina / 100f) * 10f;

        // 3. 授業ごとの難易度補正（LessonDataにdifficultyがあれば使う）
        // ここでは仮に固定
        float difficultyPenalty = 0;

        // 4. ランダム要素（運）: ±10点
        float randomVar = Random.Range(-5f, 15f);

        // 合計スコア計算
        int finalScore = Mathf.FloorToInt(baseScore + conditionBonus - difficultyPenalty + randomVar);
        finalScore = Mathf.Clamp(finalScore, 0, 100);

        // 評価判定
        string grade = "F";
        bool passed = false;

        if (finalScore >= 90) { grade = "S"; passed = true; }
        else if (finalScore >= 80) { grade = "A"; passed = true; }
        else if (finalScore >= 70) { grade = "B"; passed = true; }
        else if (finalScore >= 60) { grade = "C"; passed = true; }
        else { grade = "F"; passed = false; } // 60点未満は落単

        return new ExamResult
        {
            subjectName = lesson.lessonName,
            score = finalScore,
            grade = grade,
            isPassed = passed
        };
    }
    /// <summary>
    /// テスト期間中の行動選択（勉強 or サボり）
    /// </summary>
    public void OnSelectAction(bool isStudying)
    {
        ScenarioData resultEvent = null;

        if (isStudying)
        {
            // 勉強処理: 学力を上げてストレスも上がる
            currentKnowledge += Random.Range(5, 15);
            currentStress += 10;
            Debug.Log($"勉強した！ 学力:{currentKnowledge} ストレス:{currentStress}");

            // 勉強イベントの抽選（JSONから取得）
            resultEvent = GetRandomExamEvent(examStudyEvents);
        }
        else
        {
            // サボり処理: ストレス解消、学力微減
            currentStress = Mathf.Max(0, currentStress - 20);
            currentKnowledge -= Random.Range(0, 5);
            Debug.Log($"遊んだ！ 学力:{currentKnowledge} ストレス:{currentStress}");

            // サボりイベントの抽選
            resultEvent = GetRandomExamEvent(examSkipEvents);
        }

        // ここでイベント再生処理へ (例: dialogueRunner.Play(resultEvent))
        if (resultEvent != null)
        {
            Debug.Log("イベント発生: " + resultEvent.data[0].context);
        }
    }

    /// <summary>
    /// 試験本番の判定
    /// </summary>
    /// <returns>成績評価 (S, A, B, C, F)</returns>
    public string TakeExam()
    {
        // 判定式: 学力 + 当日の運(±20)
        int finalScore = currentKnowledge + Random.Range(-20, 21);

        Debug.Log($"試験結果: {finalScore}点 (素点:{currentKnowledge})");

        if (finalScore >= 90) return "S";
        if (finalScore >= 80) return "A";
        if (finalScore >= 70) return "B";
        if (finalScore >= 60) return "C";
        return "F"; // 落単
    }

    // 簡易的なランダム取得用ヘルパー
    private ScenarioData GetRandomExamEvent(System.Collections.Generic.List<string> jsonList)
    {
        if (jsonList == null || jsonList.Count == 0) return null;
        string json = jsonList[Random.Range(0, jsonList.Count)];
        return JsonUtility.FromJson<ScenarioData>(json);
    }
}