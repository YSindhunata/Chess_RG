using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovePlate : MonoBehaviour
{
    public GameObject Controller;
    GameObject reference = null;

    // Board positions, not world
    int matrixX;
    int matrixY;

    //false : movement, true : attacking
    public bool attack = false;
    // --- Tambahan Baru ---
    public bool isSkillPlacement = false; // Flag baru untuk membedakan MovePlate skill
    // --- Akhir Tambahan Baru ---

    public void Start()
    {
        if (attack)
        {
            // Change the color of moveplate to red
            gameObject.GetComponent<SpriteRenderer>().color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
        }
        // Catatan: Warna untuk isSkillPlacement akan diatur oleh Game.cs saat di-instantiate.
        // Jadi tidak perlu logic di sini jika Anda ingin Game.cs yang mengontrol warnanya.
    }

    // When tapping the moveplate and movement on chesspiece
    public void OnMouseUp()
    {
        Controller = GameObject.FindGameObjectWithTag("GameController");
        Game gameController = Controller.GetComponent<Game>(); // Dapatkan referensi Game sekali

        // --- Perubahan: Prioritaskan logika penempatan skill jika dalam mode ini ---
        // Ini adalah klik pertama untuk memilih petak tengah firewall
        if (gameController.inSkillPlacementMode && isSkillPlacement && gameController.firstClickPos.x == -1)
        {
            gameController.ConfirmSkillPlacement(matrixX, matrixY);
            return; // Hentikan eksekusi, ini adalah klik untuk skill, bukan gerakan catur
        }
        // --- Akhir Perubahan ---

        // Logika gerakan catur biasa, hanya dieksekusi jika bukan mode penempatan skill
        if (attack)
        {
            GameObject cp = gameController.GetPosition(matrixX, matrixY);
            Destroy(cp);
        }

        gameController.SetPositionEmpty(reference.GetComponent<Chessman>().GetXBoard(), reference.GetComponent<Chessman>().GetYBoard());

        reference.GetComponent<Chessman>().SetXBoard(matrixX);
        reference.GetComponent<Chessman>().SetYBoard(matrixY);
        reference.GetComponent<Chessman>().SetCoords();

        gameController.SetPosition(reference);

        reference.GetComponent<Chessman>().DestroyMovePlates();

        // Ganti giliran pemain
        gameController.NextTurn();

    }


    //Coords on moveplate
    public void SetCoords(int x, int y)
    {
        matrixX = x;
        matrixY = y;
    }

    public void SetReference(GameObject obj)
    {
        reference = obj;
    }

    public GameObject GetReference()
    {
        return reference;
    }
}