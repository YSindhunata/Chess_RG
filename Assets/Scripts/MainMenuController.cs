using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void OnStartGame()
    {
        //PlayerPrefs.SetString("P1Commander", "plain");
        //PlayerPrefs.SetString("P2Commander", "plain");
        PlayerPrefs.SetString("P1Commander", "fire"); // Atur P1 sebagai "fire"
        PlayerPrefs.SetString("P2Commander", "ice");  // Atur P2 sebagai "ice"
        SceneManager.LoadScene("ChessGame", LoadSceneMode.Single); // PENTING
    }

    public void OnChooseCharacter()
    {
        SceneManager.LoadScene("ChooseCharacter", LoadSceneMode.Single); // PENTING
    }

}
