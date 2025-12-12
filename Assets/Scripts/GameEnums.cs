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