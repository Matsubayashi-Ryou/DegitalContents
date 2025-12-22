using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    [Header("主要パラメータ")]
    public int academic = 0;   // 学力
    public int stamina = 100;  // 体力 (最大100とする)
    public int money = 5000;   // 財力
    public int humanity = 40;  // 人間性

    [Header("内部状態")]
    public int motivation = 50; // 勉強のやる気 (最大100とする)

    // ★修正点: moneyChg を追加しました
    // 引数の順番: 学力, 体力, 財力, 人間性, やる気
    public void UpdateStatus(int academicChg = 0, int staminaChg = 0, int moneyChg = 0, int humanityChg = 0, int motivationChg = 0)
    {
        academic += academicChg;

        stamina += staminaChg;
        stamina = Mathf.Clamp(stamina, 0, 100); // 0~100

        // ★財力の更新処理を追加
        money += moneyChg;
        // マイナスにならないようにする場合
        money = Mathf.Max(money, 0);

        humanity += humanityChg;
        humanity = Mathf.Max(humanity, 0);

        motivation += motivationChg;
        motivation = Mathf.Clamp(motivation, 0, 100);
    }
}