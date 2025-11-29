using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ConversationUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject conversationWindow;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private GameObject nextIcon;

    [Header("Standing Pictures")]
    [SerializeField] private List<Image> standingPictureImages;
    [Header("名前欄で表示しないやつ")]
    [SerializeField] private List<string> DoNotShowNames;

    private void Awake()
    {
        DoNotShowNames.Add("地の文"); // これは確定だから
        DoNotShowNames.Add("スチル"); // これも確定だから
        // 最初は非表示にしておく
        Hide();
    }

    public void Show() => conversationWindow.SetActive(true);
    public void Hide()
    {
        conversationWindow.SetActive(false);
        HideAllStandingPictures();
    }
    public void ShowNextIcon(bool show) => nextIcon.SetActive(show);

    public void SetConversationText(string speakerName, string message)
    {
        if (DoNotShowNames.Contains(speakerName))
        {
            speakerNameText.text = "";
        }
        else
        {
            speakerNameText.text = speakerName;
        }
        messageText.text = message;
        messageText.ForceMeshUpdate();
    }

    public void UpdateVisibleCharacters(int count)
    {
        messageText.maxVisibleCharacters = count;
    }

    public void ShowAllCharacters()
    {
        // 念のためtextInfoが準備できているか確認
        if (messageText.textInfo != null)
        {
            messageText.maxVisibleCharacters = messageText.textInfo.characterCount;
        }
    }

    public TMP_Text GetMessageTextComponent() => messageText;
    public void ShowStandingPicture(int position, Sprite sprite)
    {
        if (position < 0 || position >= standingPictureImages.Count)
        {
            Debug.LogWarning($"[UI] 立ち絵の位置 {position} は範囲外です。(Size: {standingPictureImages.Count})");
            return;
        }

        Image targetImage = standingPictureImages[position];
        if (targetImage == null)
        {
            Debug.LogError($"[UI] 立ち絵スロット {position} がインスペクターで設定されていません！");
            return;
        }

        if (sprite != null)
        {
            targetImage.sprite = sprite;
            targetImage.color = Color.white;
        }
        else
        {
            targetImage.sprite = null;
            targetImage.color = new Color(1, 1, 1, 0);
        }
    }

    public void HideAllStandingPictures()
    {
        foreach (var image in standingPictureImages)
        {
            if (image != null)
            {
                image.sprite = null;
                image.color = new Color(1, 1, 1, 0);
            }
        }
    }
}