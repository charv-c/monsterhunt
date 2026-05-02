using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TransScene : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TransSceneTo(int sceneId)
    {
        SceneManager.LoadScene(sceneId);//切换至相应sceneId场景
    }
}
