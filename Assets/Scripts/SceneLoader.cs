using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // 根據傳入的場景名稱載入該場景
    public void LoadScene(string sceneName)
    {
        // 載入場景
        SceneManager.LoadScene(sceneName);
    }
}

