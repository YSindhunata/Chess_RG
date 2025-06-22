using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommanderSkill : MonoBehaviour
{
    public enum SkillType { FireWall, IceFreeze, EarthStun /*, WaterFreeze */ }
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
            case SkillType.IceFreeze:
                game.EnterSkillPlacementMode(SkillType.IceFreeze);
                Debug.Log($"Player {game.GetCurrentPlayer()} memasuki mode penempatan skill IceFreeze.");
                break;
            case SkillType.EarthStun:
                game.EnterSkillPlacementMode(CommanderSkill.SkillType.EarthStun); // Memasuki mode pemilihan petak
                Debug.Log($"Player {game.GetCurrentPlayer()} memasuki mode penempatan skill EarthStun.");
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

            if (game.PositionOnBoard(fx, fy) && game.GetPosition(fx, fy) == null && !game.IsCustomObstacle(fx, fy))
            {
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.transform.localScale = new Vector3(1f, 1f, 0.5f);
                wall.transform.position = new Vector3(fx * 1.1f - 3.8f, fy * 1.1f - 3.8f, -5.0f);
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

    public void ActivateFreeze(Vector2Int targetPos, string deployingPlayerColor)
    {
        Debug.Log($"Mencoba membekukan bidak di {targetPos.x},{targetPos.y} oleh {deployingPlayerColor}");
        remainingCooldown = cooldownTurns;

        game.FreezePieceAtPosition(targetPos, deployingPlayerColor);
    }

    // --- Metode Baru: Mengaktifkan EarthStun (AoE Freeze) ---
    public void ActivateEarthStun(Vector2Int areaOriginPos, string deployingPlayerColor)
    {
        Debug.Log($"Mencoba melakukan stun area di sekitar ({areaOriginPos.x},{areaOriginPos.y}) oleh {deployingPlayerColor}");
        remainingCooldown = cooldownTurns; // Set cooldown saat skill digunakan

        // Panggil metode di Game.cs untuk membekukan area
        // Durasi khusus untuk EarthStun (lebih singkat)
        game.FreezeArea(areaOriginPos, deployingPlayerColor, game.earthStunDuration);
    }
    // --- Akhir Metode ActivateEarthStun ---


    public int GetRemainingCooldown()
    {
        return remainingCooldown;
    }
}