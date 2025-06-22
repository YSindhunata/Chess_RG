using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class CharacterSelection : MonoBehaviour
{
    public TextMeshProUGUI infoText;
    private int currentPlayer = 1;
    private string p1Commander = "";

    // Dalam CharacterSelection.cs (VERSI YANG SUDAH DIPERBAIKI)
    public void OnCharacterSelected(string commander)
    {
        if (currentPlayer == 1)
        {
            p1Commander = commander;
            PlayerPrefs.SetString("P1Commander", commander);

            if (commander == "plain")
            {
                PlayerPrefs.SetString("P2Commander", "plain");
                SceneManager.LoadScene("ChessGame", LoadSceneMode.Single);
            }
            // --- Pastikan ini menangani "fire" DAN "ice" ---
            else if (commander == "fire" || commander == "ice") // Menangani pilihan fire atau ice untuk P1
            {
                currentPlayer = 2;
                infoText.text = "Player 2, choose your commander (not " + commander + ")";
            }
            else // Jika ada tipe commander lain yang tidak dikenali
            {
                Debug.LogWarning("Pilihan komandan P1 tidak valid: " + commander);
                infoText.text = "Pilihan komandan tidak valid.";
                return;
            }
        }
        else // currentPlayer == 2
        {
            if (commander == p1Commander)
            {
                infoText.text = "Commander already taken by Player 1. Choose another.";
                return;
            }

            // --- Pastikan ini menangani "fire" DAN "ice" untuk P2 ---
            if (commander == "fire" || commander == "ice")
            {
                PlayerPrefs.SetString("P2Commander", commander);
                SceneManager.LoadScene("ChessGame", LoadSceneMode.Single);
            }
            else // Jika ada tipe commander lain yang tidak dikenali
            {
                Debug.LogWarning("Pilihan komandan P2 tidak valid: " + commander);
                infoText.text = "Pilihan komandan tidak valid.";
                return;
            }
        }
    }
}
