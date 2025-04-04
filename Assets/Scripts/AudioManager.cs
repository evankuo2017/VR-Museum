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
        // 檢查是否已有實例存在，若有則刪除重複的遊戲物件
        if (Instance == null)
        {
            Instance = this;
            // 使這個遊戲物件在切換場景時不被銷毀
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}