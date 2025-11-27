using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// シーン内の「Missing」（欠如）になっているスクリプト（コンポーネント）を検索してコンソールに出力するエディタ拡張機能です。
/// </summary>
public static class MissingScriptFinder
{
    [MenuItem("Tools/Debug/Find Missing Scripts in Scene")]
    public static void FindAndLogMissingScriptsInScene()
    {
        Debug.Log("シーン内のMissingスクリプトの検索を開始します...");

        // シーン内のすべてのゲームオブジェクトを取得（非アクティブも含む）
        // HideFlagsがNoneのものに限定し、内部的なオブジェクトを除外する
        var gameObjects = Resources.FindObjectsOfTypeAll<GameObject>()
            .Where(go => go.hideFlags == HideFlags.None).ToArray();

        int missingCount = 0;
        int objectCount = 0;

        foreach (var go in gameObjects)
        {
            // プロジェクト内に存在するアセット（プレハブなど）は除外し、シーン上のインスタンスのみを対象とする
            if (AssetDatabase.Contains(go))
            {
                continue;
            }

            // ゲームオブジェクトにアタッチされているすべてのコンポーネントを取得
            // Missing状態のコンポーネントは、この配列内では null として扱われる
            var components = go.GetComponents<Component>();

            // components配列にnullが含まれているかチェック
            if (components.Any(c => c == null))
            {
                objectCount++;
                
                // SerializedObjectを使用して、どのコンポーネントがMissingか詳細に調査する
                SerializedObject so = new SerializedObject(go);
                SerializedProperty sp = so.FindProperty("m_Component");

                // コンポーネントのリストを走査
                for (int i = 0; i < sp.arraySize; i++)
                {
                    SerializedProperty componentProp = sp.GetArrayElementAtIndex(i);
                    var componentObject = componentProp.FindPropertyRelative("component").objectReferenceValue;

                    if (componentObject == null)
                    {
                        missingCount++;
                        // Missingのコンポーネントが見つかったGameObjectを警告としてログに出力
                        Debug.LogWarning($"[{objectCount}] GameObject「{go.name}」にMissingのコンポーネントが見つかりました。(Component Index: {i})", go);
                    }
                }
            }
        }

        if (missingCount > 0)
        {
            Debug.Log($"<color=orange>検索完了: 合計 {objectCount} 個のゲームオブジェクトで {missingCount} 個のMissingスクリプトが見つかりました。</color>");
        }
        else
        {
            Debug.Log("<color=green>検索完了: このシーンにMissingスクリプトは見つかりませんでした。</color>");
        }
    }
}