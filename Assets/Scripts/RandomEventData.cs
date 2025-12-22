using UnityEngine;

// イベント発生タイミング
public enum EventTiming
{
    Morning,        // 1限の前（朝）
    BeforeClass,    // 授業が始まる前（授業がある場合）
    FreeTime,       // 空きコマ
    AfterSchool     // 放課後
}

// イベントの効果タイプ
public enum EventEffectType
{
    StatusChange,   // パラメータが変わるだけ
    ForceSkip,      // 強制的に授業欠席（寝坊など）
    BecomeFree      // 休講（授業が消滅）
}

[CreateAssetMenu(fileName = "NewRandomEvent", menuName = "Game/RandomEventData")]
public class RandomEventData : ScriptableObject
{
    [Header("イベント基本情報")]
    public string eventName;      // 例: "悪魔の囁き"
    [TextArea] public string description;    // 例: "ねえ、この後カラオケ行かない？"

    [Header("発生条件")]
    public EventTiming timing;
    [Range(0, 100)] public int probability; // 発生確率(%)

    // ★文脈判定用フラグ
    public bool requiresNextClassExists;      // 「次のコマに授業がある」ことが条件
    public bool requiresNextClassEmpty;       // 「次のコマが空き」であることが条件
    public bool requiresNoClassesRemaining;   // 「今日この後、授業が1つもない」ことが条件（飲み会など）

    [Header("選択肢（空欄なら強制イベント）")]
    public string choiceAText; // 例: "行く！" (空なら強制実行)
    public string choiceBText; // 例: "断る"

    [Header("選択肢Aの効果（または強制効果）")]
    public EventEffectType effectTypeA;
    public int staminaChgA;
    public int academicChgA;
    public int moneyChgA;
    public int humanityChgA;
    public int motivationChgA;

    [Header("選択肢Bの効果（断った場合など）")]
    public EventEffectType effectTypeB;
    public int staminaChgB;
    public int academicChgB;
    public int moneyChgB;
    public int humanityChgB;
    public int motivationChgB;
}