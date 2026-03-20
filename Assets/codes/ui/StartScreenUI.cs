using UnityEngine;
using UnityEngine.SceneManagement;

public class StartScreenUI : MonoBehaviour
{
    public void Btn_OnHost()
    {
        SceneManager.LoadScene(1);
    }
}
