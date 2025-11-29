using System;
using System.Collections.Generic;

// このスクリプトはGameObjectにアタッチする必要はありません。
// JSONの構造に合わせてクラスを定義しています。

[Serializable]
public class ScenarioData
{
    public List<ScenarioEvent> data;
}

[Serializable]
public class ScenarioEvent
{
    public string event_type;
    public string name;
    public string context;
    public string expression;
    public string bgm;
    public int position; // 立ち絵の位置など、将来的な拡張用
}