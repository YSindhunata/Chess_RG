using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommanderSkill : MonoBehaviour
{
    public enum SkillType { FireWall /*, WaterFreeze, EarthStun */ }
    public SkillType skillType;
    public int cooldownTurns = 5;
    private int remainingCooldown = 0;

    private Chessman chessman;
    private Game game;

    private string playerColor;

    void Start()
    {
        chessman = GetComponent<Chessman>();
        game = GameObject.FindGameObjectWithTag("GameController").GetComponent<Game>();
        if (chessman != null)
        {
            playerColor = chessman.player;
        }
    }

    public bool CanUseSkill()
    {
        if (game == null)
        {
            game = GameObject.FindGameObjectWithTag("GameController").GetComponent<Game>();
            if (game == null) return false;
        }
        return remainingCooldown == 0 && game.GetCurrentPlayer() == playerColor;
    }

    public string GetPlayerColor()
    {
        return playerColor;
    }

    public void UseSkill()
    {
        if (!CanUseSkill()) return;

        switch (skillType)
        {
            case SkillType.FireWall:
                game.EnterSkillPlacementMode(SkillType.FireWall);
                Debug.Log($"Player {game.GetCurrentPlayer()} memasuki mode penempatan skill FireWall.");
                break;
        }
    }

    public void OnTurnPassed()
    {
        if (remainingCooldown > 0)
            remainingCooldown--;
    }

    public void PlaceFireWall(Vector2Int startPos, bool isHorizontal, string deployingPlayerColor)
    {
        Debug.Log($"Mencoba menempatkan Firewall di {startPos.x},{startPos.y} dengan orientasi Horizontal: {isHorizontal} oleh {deployingPlayerColor}");

        remainingCooldown = cooldownTurns;

        for (int i = -1; i <= 1; i++)
        {
            int fx, fy;
            if (isHorizontal)
            {
                fx = startPos.x + i;
                fy = startPos.y;
            }
            else
            {
                fx = startPos.x;
                fy = startPos.y + i;
            }

            // Firewall bisa ditempatkan di petak kosong atau petak dengan bidak pemain (yang akan berada di atas firewall)
            // Namun, dalam kasus ini, kita hanya menempatkan di petak kosong
            if (game.PositionOnBoard(fx, fy) && game.GetPosition(fx, fy) == null && !game.IsCustomObstacle(fx, fy))
            {
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.transform.localScale = new Vector3(1f, 1f, 0.5f);
                // --- PENTING: Sesuaikan Z-position agar di bawah bidak (-1.0f) ---
                wall.transform.position = new Vector3(fx * 1.1f - 3.8f, fy * 1.1f - 3.8f, -5.0f); // Z=-1.5f atau lebih rendah dari -1.0f
                // --- AKHIR PERUBAHAN ---
                wall.name = "Firewall";
                wall.GetComponent<Renderer>().material.color = Color.red;

                wall.AddComponent<BoxCollider2D>();

                game.SetCustomObstacle(fx, fy, wall, deployingPlayerColor);

                Debug.Log($"Firewall ditempatkan di ({fx}, {fy})");
            }
            else
            {
                Debug.LogWarning($"Tidak bisa menempatkan Firewall di ({fx}, {fy}). Sudah ada bidak/rintangan atau di luar papan.");
            }
        }
    }

    public int GetRemainingCooldown()
    {
        return remainingCooldown;
    }
}