// GameEnums.cs
public enum TermType
{
    FirstSemester, // 前期
    SecondSemester // 後期
}

public enum LessonFormat
{
    InPerson, // 対面
    Online    // オンライン
}

public enum TermCategory
{
    RequiredSubjects, // 必修科目
    ElectiveSubjects // 選択科目
}

// 0=月曜 ... 4=金曜 と対応させます
public enum GameDayOfWeek
{
    Monday = 0,
    Tuesday = 1,
    Wednesday = 2,
    Thursday = 3,
    Friday = 4,
    Saturday = 5
}

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