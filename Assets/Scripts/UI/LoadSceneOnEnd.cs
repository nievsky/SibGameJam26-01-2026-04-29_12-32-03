using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneOnEnd : MonoBehaviour
{
    [SerializeField] private TypeWritterEffect endsScene;

    private void Awake()
    {
        endsScene = FindObjectOfType<TypeWritterEffect>(true);
    }
    private void Update()
    {
        if (endsScene.isEnded)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(5);
        }
    }
    
    
}
