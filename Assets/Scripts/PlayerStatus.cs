using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    [Header("主要パラメータ")]
    public int academic = 0;   // 学力
    public int stamina = 100;  // 体力 (最大100とする)
    public int money = 5000;   // 財力
    public int humanity = 40;   // 人間性

    [Header("内部状態")]
    public int motivation = 50; // やる気 (最大100とする)

    // 値を更新する関数（上限・下限チェック付き）
    public void UpdateStatus(int academicChg = 0, int staminaChg = 0, int motivationChg = 0, int humanityChg = 0)
    {
        academic += academicChg;

        stamina += staminaChg;
        stamina = Mathf.Clamp(stamina, 0, 100); // 0~100の間に制限

        humanity += humanityChg;
        humanity = Mathf.Max(humanity, 0);

        motivation += motivationChg;
        motivation = Mathf.Clamp(motivation, 0, 100);
    }
}