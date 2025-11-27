using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[InitializeOnLoad]
public static class LockInspector
{
    static GUIStyle _labelStyle;
    static GUIContent _lockIcon;
    static bool _prevLocked;

    static LockInspector()
    {
        // 初期ロック状態キャッシュ
        _prevLocked = ActiveEditorTracker.sharedTracker.isLocked;

        // ヘッダー描画後にフック
        Editor.finishedDefaultHeaderGUI -= OnFinishedDefaultHeaderGUI;
        Editor.finishedDefaultHeaderGUI += OnFinishedDefaultHeaderGUI;

        // 常にタブタイトルを更新（クリックで消える問題対策）
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.update += OnEditorUpdate;
    }

    private static void OnEditorUpdate()
    {
        var tracker = ActiveEditorTracker.sharedTracker;
        if (tracker.isLocked)
            UpdateInspectorWindowTitles();
    }

    [MenuItem("Tools/Toggle Inspector Lock %l")]
    private static void ToggleLock()
    {
        var tracker = ActiveEditorTracker.sharedTracker;
        tracker.isLocked = !tracker.isLocked;
        tracker.ForceRebuild();
    }

    private static void UpdateInspectorWindowTitles()
    {
        var tracker = ActiveEditorTracker.sharedTracker;
        var windows = Resources.FindObjectsOfTypeAll<EditorWindow>()
                               .Where(w => w.GetType().Name == "InspectorWindow");
        foreach (var win in windows)
        {
            win.titleContent = new GUIContent(
                "インスペクター" + (tracker.isLocked ? " [ロック中]" : "")
            );
            win.Repaint();
        }
    }

    [MenuItem("Tools/Toggle Inspector Debug %k")]
    private static void ToggleDebugMode()
    {
        var list = Resources.FindObjectsOfTypeAll<EditorWindow>();
        var inspectorWindow = ArrayUtility.Find(list, w => w.GetType().Name == "InspectorWindow");
        if (inspectorWindow == null) return;

        var tracker = ActiveEditorTracker.sharedTracker;
        bool isNormal = tracker.inspectorMode == InspectorMode.Normal;
        string methodName = isNormal ? "SetDebug" : "SetNormal";

        var method = inspectorWindow.GetType()
            .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(inspectorWindow, null);
        tracker.ForceRebuild();
    }

    private static void OnFinishedDefaultHeaderGUI(Editor editor)
    {
        var tracker = ActiveEditorTracker.sharedTracker;
        bool isLocked = tracker.isLocked;

        // ロック状態の変化を検知
        if (isLocked != _prevLocked)
        {
            UpdateInspectorWindowTitles();
            _prevLocked = isLocked;
        }

        if (!isLocked) return;

        // スタイル／アイコン初期化
        if (_labelStyle == null)
        {
            _labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
                fontSize = 20  // フォントサイズを大きく
            };
            _lockIcon = EditorGUIUtility.IconContent("LockIcon-On");
        }

        // アクティブエディタ一覧
        var editors = ActiveEditorTracker.sharedTracker.activeEditors;
        if (editors == null || editors.Length == 0) return;

        // 最上部・最下部にバナー描画
        if (editor == editors[0] || editor == editors[editors.Length - 1])
            DrawBanner();
    }

    private static void DrawBanner()
    {
        // 高さを60に
        Rect rect = EditorGUILayout.GetControlRect(false, 60f);
        EditorGUI.DrawRect(rect, new Color(1f, 0.2f, 0.2f, 0.9f));

        // 大きな鍵アイコン
        var iconSize = 32;
        var iconRect = new Rect(rect.x + 8, rect.y + (rect.height - iconSize) / 2, iconSize, iconSize);
        GUI.Label(iconRect, _lockIcon);

        // テキストオフセット
        var textRect = rect;
        textRect.xMin += iconSize + 16;
        GUI.Label(textRect, "インスペクターロック中!", _labelStyle);
    }
}