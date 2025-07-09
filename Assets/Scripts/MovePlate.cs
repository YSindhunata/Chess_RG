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
        Game gameController = Controller.GetComponent<Game>();

        // --- PERBAIKAN LOGIKA KLIK SKILL ---
        // Jika sedang dalam mode penempatan skill dan moveplate ini adalah untuk skill
        if (gameController.inSkillPlacementMode && isSkillPlacement)
        {
            // Panggil ConfirmSkillPlacement, yang akan menangani logika spesifik skill
            // (apakah itu FireWall atau IceFreeze)
            gameController.ConfirmSkillPlacement(matrixX, matrixY);
            return; // Hentikan eksekusi, ini adalah klik untuk skill
        }
        // --- AKHIR PERBAIKAN LOGIKA KLIK SKILL ---

        // Logika gerakan catur biasa, hanya dieksekusi jika BUKAN mode penempatan skill
        if (attack)
        {
            GameObject cp = gameController.GetPosition(matrixX, matrixY);
            if (cp != null && cp.name.Contains("_king"))
            {
                Debug.LogWarning("Langkah ilegal: tidak boleh menyerang raja secara langsung!");
                return;
            }
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