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


    public void OnExitGame()
    {
        // Ini akan keluar dari aplikasi saat dijalankan di build (Windows, Mac, Android, dll.)
        Application.Quit();

        // Untuk editor Unity, Application.Quit() tidak akan menghentikan editor.
        // Anda bisa menggunakan kode di bawah ini jika ingin ada log di editor
        // saat tombol exit ditekan, tapi tidak wajib.
        #if UNITY_EDITOR
        Debug.Log("Exiting game (This only works in a built application)");
        UnityEditor.EditorApplication.isPlaying = false; // Menghentikan mode play di editor
        #endif
    }
}
