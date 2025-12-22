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

    // moneyChg を追加
    // 引数の順番: 学力, 体力, 財力, 人間性, やる気
    public void UpdateStatus(int academicChg = 0, int staminaChg = 0, int moneyChg = 0, int humanityChg = 0, int motivationChg = 0)
    {
        UpdateAllStats(academicChg, staminaChg, moneyChg, humanityChg, motivationChg);
    }


    // 全ステータス対応
    public void UpdateAllStats(int academicChg, int staminaChg, int moneyChg, int humanityChg, int motivationChg)
    {
        academic += academicChg;

        stamina += staminaChg;
        stamina = Mathf.Clamp(stamina, 0, 100);

        money += moneyChg;
        // money = Mathf.Max(0, money); // 借金を許すかどうかはお好みで

        humanity += humanityChg;

        motivation += motivationChg;
        motivation = Mathf.Clamp(motivation, 0, 100);

        Debug.Log($"Status Updated: Ac:{academic} St:{stamina} Mo:{money} Hu:{humanity} Moti:{motivation}");
    }
}