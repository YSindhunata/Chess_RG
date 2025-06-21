using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class CharacterSelection : MonoBehaviour
{
    public TextMeshProUGUI infoText;
    private int currentPlayer = 1;
    private string p1Commander = "";

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
            else
            {
                currentPlayer = 2;
                infoText.text = "Player 2, choose your commander (not " + commander + ")";
            }
        }
        else
        {
            if (commander == p1Commander)
            {
                infoText.text = "Commander already taken by Player 1. Choose another.";
                return;
            }

            PlayerPrefs.SetString("P2Commander", commander);
            SceneManager.LoadScene("ChessGame", LoadSceneMode.Single);

        }
    }
}
