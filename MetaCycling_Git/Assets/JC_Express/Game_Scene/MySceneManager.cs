using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class MySceneManager : MonoBehaviour
{
    public void ReloadMyScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }
    public void LoadMyScene_Single(string name)
    {
        SceneManager.LoadScene(name, LoadSceneMode.Single);
    }
    public void LoadMyScene_Addictive(string name)
    {
        SceneManager.LoadScene(name, LoadSceneMode.Additive);
    }

    public void LoadMySceneAsync(string name)
    {
        StartCoroutine(LoadAsync(name));
    }
    IEnumerator LoadAsync(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
    
    public void UnloadMyScene_Async(string name)
    {
        StartCoroutine(UnloadAsync(name));
    }
    IEnumerator UnloadAsync(string sceneName)
    {
        AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(sceneName);
        while (!asyncUnload.isDone)
        {
            yield return null;
        }
    }

}
