using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using System;
using TMPro;

#nullable enable

public class ConversationManager : MonoBehaviour
{

    [Header("Components")]
    [SerializeField] private ConversationUI conversationUI;
    [SerializeField] private PlayerInput playerInput;

    [Header("Settings")]
    [SerializeField] private float charsPerSecond = 20f;
    [SerializeField] private float autoModeWaitSeconds = 1.5f;
    [SerializeField] private float fastForwardMultiplier = 3.0f;

    public bool IsConversationActive => _isConversationActive;
    public bool IsAutoMode { get; set; } = false;

    // Updateで立てて、asyncメソッドが参照するフラグ
    private bool _submitPressedThisFrame = false;

    private Queue<ScenarioEvent> _eventQueue;
    private CancellationTokenSource? _cts;
    private bool _isConversationActive = false;

    private InputAction _submitAction;
    private InputAction _fastForwardAction;
    private InputAction _toggleAutoModeAction;

    void Awake()
    {
        _eventQueue = new Queue<ScenarioEvent>();

        if (playerInput == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerInput = playerObj.GetComponent<PlayerInput>();
        }
        if (playerInput == null)
        {
            Debug.LogError("[ConversationManager] PlayerInputコンポーネントが見つかりません！");
            this.enabled = false;
            return;
        }

        _submitAction = playerInput.actions.FindActionMap("Conversation").FindAction("Submit");
        _fastForwardAction = playerInput.actions.FindActionMap("Conversation").FindAction("FastForward");
        _toggleAutoModeAction = playerInput.actions.FindActionMap("Conversation").FindAction("ToggleAutoMode");

        if (_submitAction == null || _fastForwardAction == null || _toggleAutoModeAction == null)
        {
            Debug.LogError("必要なアクション(Submit/FastForward/ToggleAutoMode)が見つかりません。");
            this.enabled = false;
            return;
        }
    }

    void Update()
    {
        // フラグは毎フレームの最初にリセット
        // _submitPressedThisFrame = false;

        if (!_isConversationActive || playerInput.currentActionMap.name != "Conversation")
        {
            return;
        }

        // Submitが押されたらフラグを立てる
        if (_submitAction != null && _submitAction.WasPressedThisFrame())
        {
            _submitPressedThisFrame = true;
        }

        // オートモードの切り替えを検知
        if (_toggleAutoModeAction != null && _toggleAutoModeAction.WasPressedThisFrame())
        {
            IsAutoMode = !IsAutoMode;
            Debug.Log($"[System] Auto Mode: {(IsAutoMode ? "ON" : "OFF")}");
        }
    }

    public async UniTask StartConversation(TextAsset scenarioJson, CancellationToken token)
    {
        Debug.Log("1. StartConversation が呼ばれました");
        if (_isConversationActive)
        {
            Debug.LogWarning($"_isConversationActive:{_isConversationActive}");
            return;
        }
        _isConversationActive = true;
        if (scenarioJson == null)
        {
            Debug.LogError("3. シナリオファイル(TextAsset)が null です！ GameManagerのInspectorを確認してください。");
            return;
        }
        playerInput.SwitchCurrentActionMap("Conversation");
        Debug.Log($"4. JSONテキスト読み込み開始: {scenarioJson.name}");
        var scenarioData = JsonUtility.FromJson<ScenarioData>(scenarioJson.text);
        if (scenarioData == null)
        {
            Debug.LogError("5. JSONパース結果が null です。JSONの書式が間違っています。");
            EndConversation();
            return;
        }
        // ★ここで落ちている可能性大
        if (scenarioData.data == null)
        {
            Debug.LogError("6. JSONデータの 'data' リストが null です。JSONは { \"data\": [ ... ] } の形式になっていますか？");
            EndConversation();
            return;
        }

        if (scenarioData.data.Count == 0)
        {
            Debug.LogWarning("7. 会話イベントが0件です。JSONの中身が空ではありませんか？");
            EndConversation();
            return;
        }

        Debug.Log($"8. イベントキュー作成開始: {scenarioData.data.Count}件");
        if (scenarioData == null || scenarioData.data.Count == 0)
        {
            EndConversation(); // データがない場合もちゃんと終了処理を通す
            return;
        }
        _eventQueue.Clear();
        foreach (var evt in scenarioData.data) { _eventQueue.Enqueue(evt); }
        _cts = new CancellationTokenSource();
        conversationUI.Show();
        try { await ProcessConversationLoop(_cts.Token); }
        finally { EndConversation(); }
    }
    private void EndConversation()
    {
        conversationUI.Hide();
        if (_cts != null) { _cts.Cancel(); _cts.Dispose(); _cts = null; }
        _isConversationActive = false;
        if (playerInput != null) playerInput.SwitchCurrentActionMap("UI");
    }
    private async UniTask ProcessConversationLoop(CancellationToken token)
    {
        Debug.Log($"会話ループ開始。イベント数: {_eventQueue.Count}"); // ★確認用
        while (_eventQueue.Count > 0)
        {

            if (token.IsCancellationRequested) break;
            var currentEvent = _eventQueue.Dequeue();
            Debug.Log($"イベント処理中: Type={currentEvent.event_type}, Context={currentEvent.context}"); // ★確認用
            switch (currentEvent.event_type)
            {
                case "text": await ProcessTextEvent(currentEvent, token); break;
                case "standing_picture": ProcessStandingPictureEvent(currentEvent); break;
                default: // ★ここを通っているならスペルミス
                    Debug.LogError($"未知のイベントタイプです: {currentEvent.event_type}");
                    break;
            }
            if (_fastForwardAction != null && _fastForwardAction.IsPressed())
            {
                await UniTask.Yield(token);
            }
        }
    }
    private async UniTask ProcessTextEvent(ScenarioEvent textEvent, CancellationToken token)
    {
        conversationUI.SetConversationText(textEvent.name, textEvent.context);
        await UniTask.Yield(PlayerLoopTiming.Update, token);
        await TypeSentenceAsync(token);
        if (token.IsCancellationRequested) return;
        conversationUI.ShowNextIcon(true);
        await WaitForNextSignalAsync(token);
        conversationUI.ShowNextIcon(false);
    }

    private async UniTask TypeSentenceAsync(CancellationToken token)
    {
        TMP_Text textComponent = conversationUI.GetMessageTextComponent();
        int totalVisibleCharacters = textComponent.textInfo.characterCount;
        textComponent.maxVisibleCharacters = 0;
        float baseDelay = 1f / charsPerSecond;
        for (int i = 0; i < totalVisibleCharacters; i++)
        {
            if (_submitPressedThisFrame)
            {
                conversationUI.ShowAllCharacters();
                _submitPressedThisFrame = false; // フラグを消費
                return;
            }
            textComponent.maxVisibleCharacters = i + 1;
            float currentMultiplier = (_fastForwardAction != null && _fastForwardAction.IsPressed()) ? fastForwardMultiplier : 1.0f;
            await UniTask.Delay(TimeSpan.FromSeconds(baseDelay / currentMultiplier), cancellationToken: token);
            if (token.IsCancellationRequested) break;
        }
    }
    private async UniTask WaitForNextSignalAsync(CancellationToken token)
    {
        if (_fastForwardAction != null && _fastForwardAction.IsPressed()) return;
        Func<bool> submitPredicate = () => _submitPressedThisFrame;
        if (IsAutoMode)
        {
            await UniTask.WhenAny(
                UniTask.Delay(TimeSpan.FromSeconds(autoModeWaitSeconds), cancellationToken: token),
                UniTask.WaitUntil(submitPredicate, cancellationToken: token)
            );
        }
        else
        {
            await UniTask.WaitUntil(submitPredicate, cancellationToken: token);
        }
        _submitPressedThisFrame = false;
    }
    private void ProcessStandingPictureEvent(ScenarioEvent pictureEvent)
    {
        if (string.IsNullOrEmpty(pictureEvent.name) || string.IsNullOrEmpty(pictureEvent.expression)) { conversationUI.ShowStandingPicture(pictureEvent.position, null); return; }
        string path = $"images/standing_pictures/{pictureEvent.name}/{pictureEvent.expression}";
        var sprite = Resources.Load<Sprite>(path);
        if (sprite == null) { Debug.LogError($"[CanvasationManager] スプライト読み込み失敗！ Path='{path}'"); }
        conversationUI.ShowStandingPicture(pictureEvent.position, sprite);
    }
}