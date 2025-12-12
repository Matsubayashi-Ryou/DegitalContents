using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class RegistrationManager : MonoBehaviour
{
    [Header("参照")]
    public GameManager gameManager;
    public GameObject registrationCanvas;
    public GameObject mainGameCanvas;

    [Header("UIパーツ")]
    public Transform paletteContent; // 授業一覧の親オブジェクト
    public GameObject paletteButtonPrefab;

    // 左側の時間割グリッドのボタン (Slot0=月1, Slot1=月2... Slot24=金5)
    public Button[] timeTableButtons;

    public Button startButton;

    // 内部データ: 現在作成中の時間割
    private LessonData[,] tempSchedule = new LessonData[5, 5];

    void Start()
    {
        registrationCanvas.SetActive(true);
        mainGameCanvas.SetActive(false);

        InitializeTimeTableButtons(); // グリッドの初期化
        InitializePalette();          // 右側リストの初期化

        RefreshTimeTableView();       // 画面描画
    }

    // --- 初期化処理 ---

    // 授業リスト（パレット）の生成
    void InitializePalette()
    {
        // 既存のボタンがあれば消す（再読み込み対応）
        foreach (Transform child in paletteContent)
        {
            Destroy(child.gameObject);
        }

        // GameManagerに登録されている全授業リストからボタンを作る
        foreach (var lesson in gameManager.allLessons)
        {
            GameObject btnObj = Instantiate(paletteButtonPrefab, paletteContent);

            // ボタンの表示テキスト: 「月1: プログラミング基礎」
            TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (txt)
            {
                txt.text = $"[{lesson.GetTimeSlotString()}] {lesson.lessonName}\n" +
                           $"<size=20>{lesson.credits}単位 / 体-{lesson.staminaCost}</size>";
            }

            // ボタンクリック時の挙動: この授業を登録しようとする
            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => TryRegisterLesson(lesson));
        }
    }

    // 時間割グリッドボタンの初期設定
    void InitializeTimeTableButtons()
    {
        for (int i = 0; i < timeTableButtons.Length; i++)
        {
            int index = i;
            // グリッドをクリックしたら、そのコマの登録を解除する
            timeTableButtons[i].onClick.AddListener(() => RemoveLessonAtIndex(index));
        }
    }

    // --- 登録・解除ロジック ---

    // リストから授業ボタンを押したときの処理
    void TryRegisterLesson(LessonData lesson)
    {
        int dayIndex = (int)lesson.day; // Enumをint(0~4)に変換
        int periodIndex = lesson.period - 1; // 1~5 を 0~4 に変換

        // 同じ時間に既に授業が入っているか確認
        LessonData existing = tempSchedule[dayIndex, periodIndex];

        if (existing != null)
        {
            Debug.Log($"上書き: {existing.lessonName} -> {lesson.lessonName}");
        }

        // 配列にセット
        tempSchedule[dayIndex, periodIndex] = lesson;

        // 画面更新
        RefreshTimeTableView();
    }

    // 時間割グリッドを押して登録解除する処理
    void RemoveLessonAtIndex(int index)
    {
        int day = index / 5;
        int period = index % 5;

        if (tempSchedule[day, period] != null)
        {
            Debug.Log($"登録解除: {tempSchedule[day, period].lessonName}");
            tempSchedule[day, period] = null;
            RefreshTimeTableView();
        }
    }

    // --- 画面描画 ---

    // tempScheduleの内容に合わせて左側のグリッド表示を更新
    void RefreshTimeTableView()
    {
        for (int day = 0; day < 5; day++)
        {
            for (int period = 0; period < 5; period++)
            {
                // UI上のインデックス計算 (縦5つ区切りなら day * 5 + period ? 
                // Grid Layout Groupの設定によりますが、ここでは以前の仕様に合わせて
                // Constraint=Column5 (横並び) であれば、順番は 月1,月2...ではなく 月1,火1... になるかも？
                // ★重要: Grid Layout Groupが「Start Corner: Top Left」「Constraint: Fixed Column Count = 5」の場合、
                // 通常は左から右へ埋まるので、
                // Slot0=月1, Slot1=火1, Slot2=水1... という並びになります。
                // 以前のコードでは「Slot0=月1, Slot1=月2...」としていたので、
                // Layout Group の Constraint を「Fixed Row Count = 5」にして、縦に並べている前提か、
                // あるいは単純に5x5の25個のボタン配列を、
                // 0~4:月曜、5~9:火曜... とみなして実装します。

                int uiIndex = (day * 5) + period;

                if (uiIndex >= timeTableButtons.Length) continue;

                LessonData data = tempSchedule[day, period];
                TextMeshProUGUI txt = timeTableButtons[uiIndex].GetComponentInChildren<TextMeshProUGUI>();
                Image bgImage = timeTableButtons[uiIndex].GetComponent<Image>();

                if (data == null)
                {
                    txt.text = ""; // 空白
                    bgImage.color = Color.white;
                }
                else
                {
                    txt.text = data.lessonName;
                    // オンラインなら色を変えるなどの演出
                    if (data.format == LessonFormat.Online)
                    {
                        bgImage.color = new Color(0.8f, 1f, 1f); // 水色っぽく
                    }
                    else
                    {
                        bgImage.color = new Color(1f, 0.9f, 0.8f); // オレンジっぽく
                    }
                }
            }
        }
    }

    // --- 完了処理 ---

    public void OnClickComplete()
    {
        // GameManagerにデータを渡す
        gameManager.SetSchedule(tempSchedule);

        registrationCanvas.SetActive(false);
        mainGameCanvas.SetActive(true);
    }
}