/*
用來在整個遊戲播放某個音樂
*/
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // Singleton 實例
    public static AudioManager Instance;

    // 背景音樂的 AudioSource
    public AudioSource bgMusic;

    private void Awake()
    {
        Debug.LogWarning("[AudioManager] Awake - 開始執行");
        
        // 檢查是否已有實例存在，若有則刪除重複的遊戲物件
        if (Instance == null)
        {
            Debug.LogWarning("[AudioManager] 設定為新的單例實例");
            Instance = this;
            // 使這個遊戲物件在切換場景時不被銷毀
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("[AudioManager] 發現重複實例，銷毀當前物件");
            Destroy(gameObject);
        }
        
        Debug.LogWarning("[AudioManager] Awake - 完成");
    }
}