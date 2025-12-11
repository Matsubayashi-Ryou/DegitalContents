using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class RegistrationManager : MonoBehaviour
{
    [Header("参照")]
    public GameManager gameManager;
    public GameObject registrationCanvas; // 履修登録用のCanvas全体
    public GameObject mainGameCanvas;     // メインゲーム用のCanvas全体

    [Header("UIパーツ")]
    public Transform paletteContent; // 授業一覧を並べるScrollViewのContent
    public GameObject paletteButtonPrefab; // 授業選択用ボタンのプレハブ
    public Button[] timeTableButtons; // 0~24番目 (月1, 月2... 金5) の順で割り当てる
    public Button startButton;

    // 内部データ
    private LessonData selectedLesson = null; // 現在パレットで選択中の授業
    private LessonData[,] tempSchedule = new LessonData[5, 5]; // 作成中の時間割

    void Start()
    {
        // ゲーム開始時は履修登録画面だけ表示
        registrationCanvas.SetActive(true);
        mainGameCanvas.SetActive(false);

        InitializePalette();
        InitializeTimeTable();
    }

    // 授業パレットの生成
    void InitializePalette()
    {
        // GameManagerに登録されている全授業リストを取得してボタン化
        foreach (var lesson in gameManager.allLessons)
        {
            GameObject btnObj = Instantiate(paletteButtonPrefab, paletteContent);

            // ボタンのラベル設定
            TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (txt) txt.text = $"{lesson.lessonName}\n({lesson.credits}単位)";

            // ボタンクリック時の挙動
            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => OnSelectPalette(lesson));
        }

        // 「削除（空きコマにする）」ボタンも追加
        GameObject clearBtnObj = Instantiate(paletteButtonPrefab, paletteContent);
        clearBtnObj.GetComponentInChildren<TextMeshProUGUI>().text = "取消（空きコマ）";
        clearBtnObj.GetComponent<Image>().color = Color.gray;
        clearBtnObj.GetComponent<Button>().onClick.AddListener(() => OnSelectPalette(null));
    }

    // 時間割ボタンの初期化
    void InitializeTimeTable()
    {
        for (int i = 0; i < timeTableButtons.Length; i++)
        {
            int index = i; // クロージャ用
            timeTableButtons[i].onClick.AddListener(() => OnClickSlot(index));

            // 初期表示
            timeTableButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = "未登録";
        }
    }

    // パレットから授業を選んだとき
    void OnSelectPalette(LessonData lesson)
    {
        selectedLesson = lesson;
        Debug.Log(lesson == null ? "「取消」を選択中" : $"「{lesson.lessonName}」を選択中");
    }

    // 時間割のマスをクリックしたとき
    void OnClickSlot(int index)
    {
        // 1次元配列のインデックス(0~24)を 2次元(曜日, 時限)に変換
        int day = index / 5;    // 0~4
        int period = index % 5; // 0~4

        // データをセット
        tempSchedule[day, period] = selectedLesson;

        // ボタンの見た目更新
        string displayName = selectedLesson == null ? "空き" : selectedLesson.lessonName;
        timeTableButtons[index].GetComponentInChildren<TextMeshProUGUI>().text = displayName;

        // 色を変えたりしても良い
        // Color c = selectedLesson == null ? Color.white : Color.cyan;
        // timeTableButtons[index].GetComponent<Image>().color = c;
    }

    // 「登録完了」ボタン
    public void OnClickComplete()
    {
        // ここで「必修が足りない！」などの警告を出しても良い

        // GameManagerにデータを渡す
        gameManager.SetSchedule(tempSchedule);

        // 画面切り替え
        registrationCanvas.SetActive(false);
        mainGameCanvas.SetActive(true);
    }
}