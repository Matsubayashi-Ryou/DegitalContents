using UnityEngine;
using System.Collections.Generic;

public enum UniversityTrigger
{
    FreePeriod, // 空きコマ
    AfterSchool, // 放課後
    SkipClass    // サボり
}

public class EventManager : MonoBehaviour
{
    // 各トリガーに対応するイベントデータのリスト（JSONの中身をここに登録する想定）
    // ※本来はTextAsset(JSONファイル)やScriptableObjectのリストにしても良いです
    [Header("Event Databases")]
    [SerializeField] private List<string> freePeriodEventsJSON; // 空きコマ用JSONリスト
    [SerializeField] private List<string> afterSchoolEventsJSON; // 放課後用JSONリスト
    [SerializeField] private List<string> skipClassEventsJSON;   // サボり用JSONリスト

    /// <summary>
    /// トリガー発生時に呼び出す関数
    /// </summary>
    /// <returns>イベントが発生したらScenarioData、外れたらnullを返す</returns>
    public ScenarioData TryTriggerEvent(UniversityTrigger trigger)
    {
        // 1. 1d100 <= 30 の判定
        int roll = Random.Range(1, 101); // 1〜100
        if (roll > 30)
        {
            Debug.Log($"イベント発生せず (Roll: {roll})");
            return null;
        }

        // 2. トリガーに応じたリストを取得
        List<string> targetList = null;
        switch (trigger)
        {
            case UniversityTrigger.FreePeriod:
                targetList = freePeriodEventsJSON;
                break;
            case UniversityTrigger.AfterSchool:
                targetList = afterSchoolEventsJSON;
                break;
            case UniversityTrigger.SkipClass:
                targetList = skipClassEventsJSON;
                break;
        }

        // リストが空なら何もしない
        if (targetList == null || targetList.Count == 0) return null;

        // 3. リストからランダムに1つ選ぶ
        string selectedJson = targetList[Random.Range(0, targetList.Count)];
        Debug.Log($"イベント発生！ (Roll: {roll}) : {trigger}");

        // 4. JSONをパースして返す
        return JsonUtility.FromJson<ScenarioData>(selectedJson);
    }
}