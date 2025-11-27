using UnityEngine;
using UnityEngine.SceneManagement;

public class DDOL : MonoBehaviour
{
    public static DDOL Instance { get; private set; }
    [SerializeField] private string destroySceneName; // 破棄用のシーン名



    void Awake()
    {
        // もしインスタンスがまだなければ
        if (Instance == null)
        {
            // このオブジェクトをシングルトンインスタンスとして設定
            Instance = this;
            // シーンをまたいでも破棄されないようにする
            DontDestroyOnLoad(gameObject);
        }
        // もしインスタンスが既に存在するなら
        else if (Instance != this)
        {
            //Debug.Break();
            // 重複するオブジェクトを破棄
            Destroy(gameObject);
        }
        // シーンがロードされたときに呼ばれるイベントに登録
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    void OnDestroy()
    {
        // シーンがロードされたときに呼ばれるイベントから登録解除
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (destroySceneName == null)
        {
            return; // 破棄用のシーン名が設定されていない場合は何もしない
        }
        if (scene.name == destroySceneName)
        {
            // 破棄用のシーンに遷移したら、このオブジェクトを破棄
            Destroy(gameObject);
        }
    }
}
