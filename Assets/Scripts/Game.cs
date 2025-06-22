using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using TMPro;
using UnityEngine.UI;

using UnityEngine.SceneManagement;
using System.Linq;

public class Game : MonoBehaviour
{
    public GameObject ChessPiece;

    // UI Elements for Game Over
    public TextMeshProUGUI checkmateText; // Reference to the UI TextMeshPro element for "Checkmate!"

    private string cm = "CHEKMATE!";
    public Button restartButton;
    public Button homeButton;

    // Timer giliran
    public TextMeshProUGUI turnTimerText;
    private float turnTime = 20f;
    private float currentTimer = 20f;
    private bool timerRunning = true;

    // Positions and team for each chesspieces
    private GameObject[,] positions = new GameObject[8, 8];
    private GameObject[] playerBlack = new GameObject[16];
    private GameObject[] playerWhite = new GameObject[16];

    private Dictionary<Vector2Int, GameObject> allCustomObstacles = new Dictionary<Vector2Int, GameObject>(); // Objek penghalang visual (Firewall)
    private Dictionary<Vector2Int, string> obstacleOwnerColors = new Dictionary<Vector2Int, string>(); // Pemilik firewall

    private Dictionary<GameObject, int> frozenPieces = new Dictionary<GameObject, int>(); // Bidak yang beku
    public int freezeDuration = 5; // Durasi beku untuk IceFreeze

    // --- Tambahan untuk EarthStun ---
    public int earthStunDuration = 3; // Durasi beku/penghalang untuk EarthStun
    private Dictionary<Vector2Int, int> activeEarthObstacles = new Dictionary<Vector2Int, int>(); // Key: Posisi, Value: Durasi tersisa
    private Dictionary<Vector2Int, GameObject> visualEarthObstacles = new Dictionary<Vector2Int, GameObject>(); // Key: Posisi, Value: Objek visual penghalang Earth
    public GameObject earthObstaclePrefab; // Prefab visual untuk petak yang tidak bisa ditempati (Earth Stun)
    // --- Akhir Tambahan ---

    private string currentPlayer = "white";
    private bool gameOver = false;

    public bool inSkillPlacementMode = false;
    private CommanderSkill.SkillType currentSkillToPlace;
    public GameObject skillPlacementPlate;
    private GameObject activeCommanderUsingSkill;
    private Dictionary<Vector2Int, GameObject> currentSkillPlates = new Dictionary<Vector2Int, GameObject>();

    public Vector2Int firstClickPos = new Vector2Int(-1, -1);
    public GameObject horizontalButtonPrefab;
    public GameObject verticalButtonPrefab;
    private GameObject currentOrientationButtonH;
    private GameObject currentOrientationButtonV;

    public Camera mainCamera;

    public TextMeshProUGUI whiteCommanderCooldownText;
    public TextMeshProUGUI blackCommanderCooldownText;

    private Vector3 playerWhiteBoardBottomWorldPos;
    private Vector3 playerBlackBoardBottomWorldPos;

    public BgMusic bgMusic; // drag GameObject dengan script BgMusic ke sini

    public string GetCurrentPlayer()
    {
        return currentPlayer;
    }

    public void NextTurn()
    {
        Debug.Log("--- NextTurn() dipanggil --- CurrentPlayer sebelum ganti: " + currentPlayer);
        if (gameOver) return;

        if (inSkillPlacementMode)
        {
            Debug.Log("Keluar dari skill placement mode karena giliran berganti secara paksa.");
            ExitSkillPlacementMode();
        }

        List<GameObject> frozenKeys = new List<GameObject>(frozenPieces.Keys);
        List<GameObject> piecesToUnfreeze = new List<GameObject>();

        foreach (GameObject piece in frozenKeys)
        {
            if (piece != null)
            {
                frozenPieces[piece]--;
                if (frozenPieces[piece] <= 0)
                {
                    piecesToUnfreeze.Add(piece);
                }
            }
            else
            {
                piecesToUnfreeze.Add(piece);
            }
        }

        foreach (GameObject piece in piecesToUnfreeze)
        {
            UnfreezePiece(piece);
        }

        // --- PERBAIKAN: Buat salinan kunci untuk iterasi activeEarthObstacles ---
        List<Vector2Int> earthObstacleKeys = new List<Vector2Int>(activeEarthObstacles.Keys); // Salin daftar kunci
        List<Vector2Int> squaresToClearImpassable = new List<Vector2Int>();
        foreach (Vector2Int pos in earthObstacleKeys) // Iterasi melalui salinan kunci
        {
            if (activeEarthObstacles.ContainsKey(pos)) // Periksa lagi, jaga-jaga kalau sudah dihapus
            {
                activeEarthObstacles[pos]--; // Kurangi durasi
                if (activeEarthObstacles[pos] <= 0)
                {
                    squaresToClearImpassable.Add(pos);
                }
            }
        }

        foreach (Vector2Int pos in squaresToClearImpassable)
        {
            RemoveTemporaryImpassableSquare(pos);
        }
        // --- AKHIR PERBAIKAN ---


        currentPlayer = currentPlayer == "white" ? "black" : "white";
        currentTimer = turnTime;

        GameObject[] allPiecesInPlay = playerWhite.Concat(playerBlack).ToArray();
        foreach (GameObject piece in allPiecesInPlay)
        {
            if (piece == null) continue;

            CommanderSkill skill = piece.GetComponent<CommanderSkill>();
            if (skill != null)
            {
                skill.OnTurnPassed();
                if (skill.GetRemainingCooldown() == 0)
                {
                    RemoveFirewallForPlayer(skill.GetPlayerColor());
                }
            }
        }

        GameObject visualCommanderP1 = GameObject.Find("CommanderVisual_P1");
        if (visualCommanderP1 != null)
        {
            CommanderSkill skill = visualCommanderP1.GetComponent<CommanderSkill>();
            if (skill != null)
            {
                skill.OnTurnPassed();
            }
        }

        GameObject visualCommanderP2 = GameObject.Find("CommanderVisual_P2");
        if (visualCommanderP2 != null)
        {
            CommanderSkill skill = visualCommanderP2.GetComponent<CommanderSkill>();
            if (skill != null)
            {
                skill.OnTurnPassed();
            }
        }

        UpdateCommanderCooldownUI();


        if (IsCheckmate(currentPlayer))
        {
            Debug.Log("CHECKMATE! Winner: " + (currentPlayer == "white" ? "black" : "white"));

            gameOver = true;
            timerRunning = false;

            if (bgMusic != null)
            {
                Debug.Log("Memanggil PlayVictorySound()");
                bgMusic.PlayVictorySound();
            }

            ShowGameOverUI();
        }
        Debug.Log("--- NextTurn() selesai --- CurrentPlayer setelah ganti: " + currentPlayer);
    }

    public void RemoveFirewallForPlayer(string playerColor)
    {
        List<Vector2Int> positionsToRemove = new List<Vector2Int>();
        foreach (var entry in obstacleOwnerColors)
        {
            if (entry.Value == playerColor)
            {
                positionsToRemove.Add(entry.Key);
            }
        }

        foreach (Vector2Int pos in positionsToRemove)
        {
            if (allCustomObstacles.ContainsKey(pos))
            {
                Destroy(allCustomObstacles[pos]);
                allCustomObstacles.Remove(pos);
            }
            obstacleOwnerColors.Remove(pos);
        }
        Debug.Log($"Firewall lama pemain {playerColor} telah dihapus.");
    }

    public void AddTemporaryImpassableSquare(int x, int y, int duration)
    {
        Vector2Int pos = new Vector2Int(x, y);
        if (!activeEarthObstacles.ContainsKey(pos)) // Hanya tambahkan jika belum ada EarthStun obstacle di posisi ini
        {
            activeEarthObstacles[pos] = duration;
            // Buat visual efek untuk petak yang tidak bisa dilewati
            if (earthObstaclePrefab != null)
            {
                GameObject effect = Instantiate(earthObstaclePrefab, new Vector3(x * 1.1f - 3.8f, y * 1.1f - 3.8f, -4.0f), Quaternion.identity); // Z di bawah bidak
                effect.name = $"EarthStunObstacle_Visual_{x}_{y}";
                visualEarthObstacles[pos] = effect;
            }
            Debug.Log($"Petak ({x},{y}) tidak bisa dilewati sementara selama {duration} giliran.");
        }
    }

    private void RemoveTemporaryImpassableSquare(Vector2Int pos)
    {
        if (activeEarthObstacles.ContainsKey(pos))
        {
            activeEarthObstacles.Remove(pos);
            if (visualEarthObstacles.ContainsKey(pos) && visualEarthObstacles[pos] != null)
            {
                Destroy(visualEarthObstacles[pos]);
                visualEarthObstacles.Remove(pos);
            }
            Debug.Log($"Petak ({pos.x},{pos.y}) kini bisa dilewati lagi.");
        }
    }

    public bool IsTemporarilyImpassable(int x, int y)
    {
        return activeEarthObstacles.ContainsKey(new Vector2Int(x, y));
    }


    private void UpdateCommanderCooldownUI()
    {
        GameObject whiteKing = FindKing("white");
        if (whiteKing != null)
        {
            CommanderSkill skill = whiteKing.GetComponent<CommanderSkill>();
            if (skill != null && whiteCommanderCooldownText != null)
            {
                whiteCommanderCooldownText.text = skill.GetRemainingCooldown().ToString();
            }
        }

        GameObject blackKing = FindKing("black");
        if (blackKing != null)
        {
            CommanderSkill skill = blackKing.GetComponent<CommanderSkill>();
            if (skill != null && blackCommanderCooldownText != null)
            {
                blackCommanderCooldownText.text = skill.GetRemainingCooldown().ToString();
            }
        }
    }


    private void ShowGameOverUI()
    {
        if (checkmateText != null)
        {
            checkmateText.text = "CHECKMATE!\n" + (currentPlayer == "white" ? "Black" : "White") + " Wins!";
            checkmateText.gameObject.SetActive(true);
        }
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(true);
        }
        if (homeButton != null)
        {
            homeButton.gameObject.SetActive(true);
        }
    }

    public bool IsCheckmate(string player)
    {
        GameObject king = FindKing(player);
        if (king == null) return false;
        if (!IsUnderAttack(king.GetComponent<Chessman>().GetXBoard(), king.GetComponent<Chessman>().GetYBoard(), player))
            return false;
        GameObject[] pieces = player == "white" ? playerWhite : playerBlack;
        foreach (GameObject piece in pieces)
        {
            if (piece == null) continue;
            Chessman cm = piece.GetComponent<Chessman>();

            List<Vector2Int> moves = cm.GetLegalMoves();
            foreach (Vector2Int move in moves)
            {
                int oldX = cm.GetXBoard();
                int oldY = cm.GetYBoard();
                GameObject captured = GetPosition(move.x, move.y);

                SetPositionEmpty(oldX, oldY);
                cm.SetXBoard(move.x);
                cm.SetYBoard(move.y);
                SetPosition(piece);

                if (captured != null) captured.SetActive(false);

                bool stillInCheck = IsUnderAttack(FindKing(player).GetComponent<Chessman>().GetXBoard(), FindKing(player).GetComponent<Chessman>().GetYBoard(), player);

                SetPositionEmpty(move.x, move.y);

                cm.SetXBoard(oldX);
                cm.SetYBoard(oldY);
                SetPosition(piece);
                if (captured != null)
                {
                    captured.SetActive(true);
                    SetPosition(captured);
                }

                if (!stillInCheck)
                    return false;
            }
        }
        return true;
    }

    public bool IsUnderAttack(int x, int y, string player)
    {
        GameObject[] enemyPieces = player == "white" ? playerBlack : playerWhite;
        foreach (GameObject piece in enemyPieces)
        {
            if (piece == null) continue;
            Chessman cm = piece.GetComponent<Chessman>();
            List<Vector2Int> enemyMoves = cm.GetPotentialMoves();
            foreach (Vector2Int pos in enemyMoves)
            {
                if (pos.x == x && pos.y == y)
                    return true;
            }
        }
        return false;
    }

    public GameObject FindKing(string player)
    {
        GameObject[] pieces = player == "white" ? playerWhite : playerBlack;
        foreach (GameObject piece in pieces)
        {
            if (piece == null) continue;
            if (piece.name == player + "_king") return piece;
        }
        return null;
    }



    void Start()
    {
        string p1Commander = PlayerPrefs.GetString("P1Commander", "plain");
        string p2Commander = PlayerPrefs.GetString("P2Commander", "plain");

        playerWhite = new GameObject[]
        {
        Create("white_rook", 0,0), Create("white_knight", 1,0), Create("white_bishop", 2,0),
        Create("white_queen", 3,0), null, Create("white_bishop", 5,0),
        Create("white_knight", 6,0), Create("white_rook", 7,0), Create("white_pawn", 0,1),
        Create("white_pawn", 1,1), Create("white_pawn", 2,1), Create("white_pawn", 3,1),
        Create("white_pawn", 4,1), Create("white_pawn", 5,1), Create("white_pawn", 6,1),
        Create("white_pawn", 7,1)
        };

        GameObject whiteKing = Create("white_king", 4, 0);
        if (p1Commander != "plain") AddCommanderSkill(whiteKing, p1Commander);
        playerWhite[4] = whiteKing;

        playerBlack = new GameObject[]
        {
        Create("black_rook", 0,7), Create("black_knight", 1,7), Create("black_bishop", 2,7),
        Create("black_queen", 3,7), null, Create("black_bishop", 5,7),
        Create("black_knight", 6,7), Create("black_rook", 7,7), Create("black_pawn", 0,6),
        Create("black_pawn", 1,6), Create("black_pawn", 2, 6), Create("black_pawn", 3, 6),
        Create("black_pawn", 4, 6), Create("black_pawn", 5, 6), Create("black_pawn", 6, 6),
        Create("black_pawn", 7, 6)
        };

        GameObject blackKing = Create("black_king", 4, 7);
        if (p2Commander != "plain") AddCommanderSkill(blackKing, p2Commander);
        playerBlack[4] = blackKing;

        for (int i = 0; i < playerBlack.Length; i++)
        {
            SetPosition(playerBlack[i]);
            SetPosition(playerWhite[i]);
        }

        if (checkmateText != null) checkmateText.gameObject.SetActive(false);
        if (restartButton != null) restartButton.gameObject.SetActive(false);
        if (homeButton != null) homeButton.gameObject.SetActive(false);

        turnTimerText.text = Mathf.Ceil(currentTimer).ToString();

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // === BUAT KOMANDER VISUAL PLAYER 1 ===
        string p1Type = PlayerPrefs.GetString("P1Commander", "plain");
        if (p1Type != "plain")
        {
            GameObject prefab = Resources.Load<GameObject>("CommanderFire");
            if (prefab != null)
            {
                GameObject commanderP1 = Instantiate(prefab, new Vector3(-3.5f, -6.25f, -1f), Quaternion.identity);
                commanderP1.name = "CommanderVisual_P1";

                SpriteRenderer sr = commanderP1.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = Resources.Load<Sprite>(p1Type + "_commander");
                    commanderP1.transform.localScale = new Vector3(0.35f, 0.35f, -1f);
                }

                CommanderSkill skill = commanderP1.AddComponent<CommanderSkill>();
                skill.skillType = ParseSkillType(p1Type);
                skill.cooldownTurns = 5;

                commanderP1.AddComponent<CommanderClick>();
            }
            else
            {
                Debug.LogError("Prefab 'CommanderFire' tidak ditemukan di Resources! Pastikan sudah ada.");
            }
        }

        // === BUAT KOMANDER VISUAL PLAYER 2 ===
        string p2Type = PlayerPrefs.GetString("P2Commander", "plain");
        if (p2Type != "plain")
        {
            GameObject prefab2 = Resources.Load<GameObject>("CommanderFire");
            if (prefab2 != null)
            {
                GameObject commanderP2 = Instantiate(prefab2, new Vector3(3.5f, 6f, -1f), Quaternion.identity);
                commanderP2.name = "CommanderVisual_P2";

                SpriteRenderer sr = commanderP2.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = Resources.Load<Sprite>(p2Type + "_commander");
                    commanderP2.transform.localScale = new Vector3(1.25f, 1f, -1f);
                }

                CommanderSkill skill = commanderP2.AddComponent<CommanderSkill>();
                skill.skillType = ParseSkillType(p2Type);
                skill.cooldownTurns = 5;

                commanderP2.AddComponent<CommanderClick>();
            }
            else
            {
                Debug.LogError("Prefab 'CommanderFire' tidak ditemukan di Resources! Pastikan sudah ada.");
            }
        }

        float boardMinY = -3.8f;
        float boardMaxY = 3.8f;
        float boardCenterX = -3.8f + (7 * 1.1f) / 2f;
        float worldOffsetFromBoard = 0.5f;

        playerWhiteBoardBottomWorldPos = new Vector3(boardCenterX, boardMinY - worldOffsetFromBoard, 0);
        playerBlackBoardBottomWorldPos = new Vector3(boardCenterX, boardMaxY + worldOffsetFromBoard, 0);

        UpdateCommanderCooldownUI();
    }


    public GameObject Create(string name, int x, int y)
    {
        GameObject obj = Instantiate(ChessPiece, new Vector3(0, 0, -1), Quaternion.identity);
        Chessman cm = obj.GetComponent<Chessman>();
        cm.name = name;
        cm.SetXBoard(x);
        cm.SetYBoard(y);
        cm.Activate();
        return obj;
    }

    public void SetPosition(GameObject obj)
    {
        Chessman cm = obj.GetComponent<Chessman>();
        positions[cm.GetXBoard(), cm.GetYBoard()] = obj;
    }

    public void SetPositionEmpty(int x, int y)
    {
        positions[x, y] = null;
    }

    public GameObject GetPosition(int x, int y)
    {
        return positions[x, y];
    }

    public void SetCustomObstacle(int x, int y, GameObject obj, string playerColor)
    {
        Vector2Int pos = new Vector2Int(x, y);
        if (!allCustomObstacles.ContainsKey(pos))
        {
            allCustomObstacles[pos] = obj;
            obstacleOwnerColors[pos] = playerColor;

            Debug.Log($"Firewall baru '{obj.name}' ditempatkan oleh {playerColor} di ({x},{y}). Total obstacles: {allCustomObstacles.Count}");
        }
        else
        {
            Debug.LogWarning($"SetCustomObstacle: Posisi ({x},{y}) sudah memiliki obstacle lain.");
        }
    }

    // IsCustomObstacle kini juga memeriksa EarthStun obstacles
    public bool IsCustomObstacle(int x, int y)
    {
        return allCustomObstacles.ContainsKey(new Vector2Int(x, y)) || activeEarthObstacles.ContainsKey(new Vector2Int(x, y));
    }

    // Metode untuk mendapatkan warna pemilik obstacle (hanya berlaku untuk Firewall)
    public string GetObstacleOwnerColor(int x, int y)
    {
        Vector2Int pos = new Vector2Int(x, y);
        if (obstacleOwnerColors.ContainsKey(pos)) // Ini untuk Firewall
        {
            return obstacleOwnerColors[pos];
        }
        // EarthStun obstacle tidak punya pemilik spesifik dalam konteks ini, bersifat netral/menghambat semua.
        return null;
    }


    public bool PositionOnBoard(int x, int y)
    {
        if (x < 0 || y < 0 || x >= positions.GetLength(0) || y >= positions.GetLength(1)) return false;
        return true;
    }

    public void OnRestartButtonClick()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnHomeButtonClick()
    {
        SceneManager.LoadScene("Home");
    }

    public void EnterSkillPlacementMode(CommanderSkill.SkillType skillType)
    {
        Debug.Log($"Memasuki mode penempatan skill: {skillType}");
        inSkillPlacementMode = true;
        currentSkillToPlace = skillType;
        activeCommanderUsingSkill = FindKing(currentPlayer);
        firstClickPos = new Vector2Int(-1, -1);

        GameObject[] currentMovePlates = GameObject.FindGameObjectsWithTag("MovePlate");
        for (int i = 0; i < currentMovePlates.Length; i++)
        {
            Destroy(currentMovePlates[i]);
        }

        ShowSkillPlacementOptions();
    }

    public void ExitSkillPlacementMode()
    {
        Debug.Log("Keluar dari mode penempatan skill.");
        inSkillPlacementMode = false;
        currentSkillToPlace = CommanderSkill.SkillType.FireWall;
        activeCommanderUsingSkill = null;
        firstClickPos = new Vector2Int(-1, -1);
        ClearSkillPlacementPlates();
        HideOrientationButtons();
    }

    private void ShowSkillPlacementOptions()
    {
        Debug.Log("Menampilkan opsi penempatan skill untuk: " + currentSkillToPlace);
        ClearSkillPlacementPlates();
        HideOrientationButtons();

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (currentSkillToPlace == CommanderSkill.SkillType.FireWall)
                {
                    if (GetPosition(x, y) == null && !IsCustomObstacle(x, y)) // Firewall hanya di petak kosong
                    {
                        GameObject mp = Instantiate(skillPlacementPlate, new Vector3(x * 1.1f - 3.85f, y * 1.1f - 3.85f, -3.0f), Quaternion.identity);
                        MovePlate mpScript = mp.GetComponent<MovePlate>();
                        mpScript.SetCoords(x, y);
                        mpScript.isSkillPlacement = true;
                        mpScript.SetReference(activeCommanderUsingSkill);
                        currentSkillPlates.Add(new Vector2Int(x, y), mp);
                        mp.GetComponent<SpriteRenderer>().color = new Color(0.0f, 0.0f, 1.0f, 0.7f); // Biru transparan
                    }
                }
                else if (currentSkillToPlace == CommanderSkill.SkillType.IceFreeze)
                {
                    GameObject targetPiece = GetPosition(x, y);
                    if (targetPiece != null && targetPiece.GetComponent<Chessman>().player != currentPlayer && !IsPieceFrozen(targetPiece))
                    {
                        GameObject mp = Instantiate(skillPlacementPlate, new Vector3(x * 1.1f - 3.85f, y * 1.1f - 3.85f, -3.0f), Quaternion.identity);
                        MovePlate mpScript = mp.GetComponent<MovePlate>();
                        mpScript.SetCoords(x, y);
                        mpScript.isSkillPlacement = true;
                        mpScript.SetReference(activeCommanderUsingSkill);
                        currentSkillPlates.Add(new Vector2Int(x, y), mp);
                        mp.GetComponent<SpriteRenderer>().color = new Color(0.0f, 0.7f, 1.0f, 0.7f); // Biru muda transparan
                    }
                }
                else if (currentSkillToPlace == CommanderSkill.SkillType.EarthStun)
                {
                    // Untuk EarthStun, pemain bisa memilih petak mana saja di papan.
                    // Tidak peduli ada bidak atau kosong. Validasi 2x2 akan di ConfirmSkillPlacement
                    GameObject mp = Instantiate(skillPlacementPlate, new Vector3(x * 1.1f - 3.85f, y * 1.1f - 3.85f, -3.0f), Quaternion.identity);
                    MovePlate mpScript = mp.GetComponent<MovePlate>();
                    mpScript.SetCoords(x, y);
                    mpScript.isSkillPlacement = true;
                    mpScript.SetReference(activeCommanderUsingSkill);
                    currentSkillPlates.Add(new Vector2Int(x, y), mp);
                    mp.GetComponent<SpriteRenderer>().color = new Color(0.8f, 0.5f, 0.2f, 0.7f); // Coklat transparan
                }
            }
        }
        Debug.Log("Menampilkan opsi penempatan skill. Pilih target.");
    }

    private void ClearSkillPlacementPlates()
    {
        foreach (var entry in currentSkillPlates)
        {
            Destroy(entry.Value);
        }
        currentSkillPlates.Clear();
    }

    public void ConfirmSkillPlacement(int x, int y)
    {
        Debug.Log($"ConfirmSkillPlacement dipanggil untuk ({x},{y}). Mode: {inSkillPlacementMode}, Skill: {currentSkillToPlace}");

        if (!inSkillPlacementMode || activeCommanderUsingSkill == null) return;

        CommanderSkill commanderSkill = activeCommanderUsingSkill.GetComponent<CommanderSkill>();
        if (commanderSkill == null) return;

        if (currentSkillToPlace == CommanderSkill.SkillType.FireWall)
        {
            if (firstClickPos.x == -1)
            {
                firstClickPos = new Vector2Int(x, y);
                ClearSkillPlacementPlates();
                ShowOrientationButtons(firstClickPos);
                Debug.Log($"Petak awal skill FireWall dipilih: ({x},{y}). Pilih orientasi.");
            }
        }
        else if (currentSkillToPlace == CommanderSkill.SkillType.IceFreeze)
        {
            GameObject targetPiece = GetPosition(x, y);
            if (targetPiece != null && targetPiece.GetComponent<Chessman>().player != currentPlayer && !IsPieceFrozen(targetPiece))
            {
                Debug.Log($"Target valid untuk Freeze: {targetPiece.name} di ({x},{y}).");
                commanderSkill.ActivateFreeze(new Vector2Int(x, y), currentPlayer);
                ExitSkillPlacementMode();
                UpdateCommanderCooldownUI();
            }
            else
            {
                Debug.LogWarning("Target tidak valid untuk skill Freeze (bukan lawan atau sudah beku). Skill hangus.");
                ExitSkillPlacementMode();
                Debug.Log("Memanggil NextTurn() karena target Freeze tidak valid.");
                NextTurn();
                UpdateCommanderCooldownUI();
            }
        }
        else if (currentSkillToPlace == CommanderSkill.SkillType.EarthStun)
        {
            // Untuk EarthStun, pemain memilih satu petak (x,y)
            // Sistem akan secara acak memilih salah satu dari 4 kemungkinan 2x2 area yang mencakup (x,y).
            List<Vector2Int[]> possibleAreas = new List<Vector2Int[]>();

            // Area 1: (x,y) adalah sudut kiri bawah
            if (PositionOnBoard(x, y) && PositionOnBoard(x + 1, y) && PositionOnBoard(x, y + 1) && PositionOnBoard(x + 1, y + 1))
            {
                possibleAreas.Add(new Vector2Int[] { new Vector2Int(x, y), new Vector2Int(x + 1, y), new Vector2Int(x, y + 1), new Vector2Int(x + 1, y + 1) });
            }
            // Area 2: (x,y) adalah sudut kanan bawah
            if (PositionOnBoard(x - 1, y) && PositionOnBoard(x, y) && PositionOnBoard(x - 1, y + 1) && PositionOnBoard(x, y + 1))
            {
                possibleAreas.Add(new Vector2Int[] { new Vector2Int(x - 1, y), new Vector2Int(x, y), new Vector2Int(x - 1, y + 1), new Vector2Int(x, y + 1) });
            }
            // Area 3: (x,y) adalah sudut kiri atas
            if (PositionOnBoard(x, y - 1) && PositionOnBoard(x + 1, y - 1) && PositionOnBoard(x, y) && PositionOnBoard(x + 1, y))
            {
                possibleAreas.Add(new Vector2Int[] { new Vector2Int(x, y - 1), new Vector2Int(x + 1, y - 1), new Vector2Int(x, y), new Vector2Int(x + 1, y) });
            }
            // Area 4: (x,y) adalah sudut kanan atas
            if (PositionOnBoard(x - 1, y - 1) && PositionOnBoard(x, y - 1) && PositionOnBoard(x - 1, y) && PositionOnBoard(x, y))
            {
                possibleAreas.Add(new Vector2Int[] { new Vector2Int(x - 1, y - 1), new Vector2Int(x, y - 1), new Vector2Int(x - 1, y), new Vector2Int(x, y) });
            }

            if (possibleAreas.Count > 0)
            {
                int randomIndex = Random.Range(0, possibleAreas.Count);
                Vector2Int[] selectedArea = possibleAreas[randomIndex];

                Debug.Log($"Target valid untuk EarthStun. Area acak dipilih: ({selectedArea[0].x},{selectedArea[0].y}) sampai ({selectedArea[3].x},{selectedArea[3].y}).");
                commanderSkill.ActivateEarthStun(selectedArea[0], currentPlayer); // Meneruskan sudut kiri bawah area sebagai titik awal
                ExitSkillPlacementMode();
                UpdateCommanderCooldownUI();
                // NextTurn() TIDAK DIPANGGIL DI SINI untuk EarthStun yang BERHASIL (sesuai permintaan user)
            }
            else
            {
                Debug.LogWarning($"Tidak ada area 2x2 valid yang mencakup ({x},{y}). Pilih ulang.");
                ExitSkillPlacementMode(); // Skill hangus jika area tidak valid
                Debug.Log("Memanggil NextTurn() karena area EarthStun tidak valid.");
                NextTurn(); // Giliran berganti jika area tidak valid
                UpdateCommanderCooldownUI();
            }
        }
    }

    private void ShowOrientationButtons(Vector2Int centerPos)
    {
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera tidak terhubung! Pastikan Main Camera diseret ke slot 'mainCamera' di Inspector GameObject 'GameController'.");
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("Gagal menemukan Main Camera bahkan setelah mencoba Camera.main. Pastikan kamera di scene Anda memiliki tag 'MainCamera'.");
                return;
            }
        }

        Vector3 screenPos;
        if (currentPlayer == "white")
        {
            screenPos = mainCamera.WorldToScreenPoint(playerWhiteBoardBottomWorldPos);
        }
        else // currentPlayer == "black"
        {
            screenPos = mainCamera.WorldToScreenPoint(playerBlackBoardBottomWorldPos);
        }

        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("Canvas tidak ditemukan! Pastikan ada objek bernama 'Canvas' di scene.");
            return;
        }
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, mainCamera, out localPos);

        float buttonYSpacing = 100f;
        float buttonXSpacing = -580f;

        // Tombol Horizontal
        if (horizontalButtonPrefab != null)
        {
            currentOrientationButtonH = Instantiate(horizontalButtonPrefab, canvas.transform);
            currentOrientationButtonH.GetComponent<RectTransform>().localPosition = localPos + new Vector2(-buttonXSpacing, buttonYSpacing);
            currentOrientationButtonH.GetComponent<Button>().onClick.AddListener(() => PlaceFireWallSkill(centerPos, true));
            currentOrientationButtonH.gameObject.SetActive(true);
        }

        // Tombol Vertical
        if (verticalButtonPrefab != null)
        {
            currentOrientationButtonV = Instantiate(verticalButtonPrefab, canvas.transform);
            currentOrientationButtonV.GetComponent<RectTransform>().localPosition = localPos + new Vector2(-buttonXSpacing + 200, buttonYSpacing);
            currentOrientationButtonV.GetComponent<Button>().onClick.AddListener(() => PlaceFireWallSkill(centerPos, false));
            currentOrientationButtonV.gameObject.SetActive(true);
        }
        Debug.Log("Menampilkan tombol orientasi.");
    }

    private void HideOrientationButtons()
    {
        if (currentOrientationButtonH != null)
        {
            Destroy(currentOrientationButtonH);
            currentOrientationButtonH = null;
        }
        if (currentOrientationButtonV != null)
        {
            Destroy(currentOrientationButtonV);
            currentOrientationButtonV = null;
        }
        Debug.Log("Tombol orientasi disembunyikan.");
    }

    public void PlaceFireWallSkill(Vector2Int centerPos, bool isHorizontal)
    {
        Debug.Log("PlaceFireWallSkill dipanggil.");
        if (!inSkillPlacementMode || activeCommanderUsingSkill == null) return;
        if (currentSkillToPlace != CommanderSkill.SkillType.FireWall) return;

        CommanderSkill commanderSkill = activeCommanderUsingSkill.GetComponent<CommanderSkill>();
        if (commanderSkill != null)
        {
            commanderSkill.PlaceFireWall(centerPos, isHorizontal, currentPlayer);
        }

        ExitSkillPlacementMode();
        Debug.Log("Memanggil NextTurn() setelah PlaceFireWallSkill.");
        NextTurn();
        UpdateCommanderCooldownUI();
    }


    public void FreezePieceAtPosition(Vector2Int pos, string freezingPlayerColor)
    {
        GameObject pieceToFreeze = GetPosition(pos.x, pos.y);
        if (pieceToFreeze != null && pieceToFreeze.GetComponent<Chessman>().player != freezingPlayerColor)
        {
            if (!frozenPieces.ContainsKey(pieceToFreeze))
            {
                frozenPieces[pieceToFreeze] = freezeDuration;
                SpriteRenderer sr = pieceToFreeze.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = Color.cyan;
                }
                Debug.Log($"Bidak {pieceToFreeze.name} dibekukan selama {freezeDuration} giliran!");
            }
            else
            {
                Debug.LogWarning($"Bidak {pieceToFreeze.name} sudah dibekukan.");
            }
        }
        else
        {
            Debug.LogWarning($"Tidak ada bidak lawan valid di ({pos.x},{pos.y}) untuk dibekukan.");
        }
    }

    // Metode Baru: FreezeArea untuk EarthStun
    public void FreezeArea(Vector2Int areaOriginPos, string deployingPlayerColor, int duration)
    {
        Debug.Log($"Mengaktifkan FreezeArea di sekitar ({areaOriginPos.x},{areaOriginPos.y}) dengan durasi {duration}.");
        // Area 2x2, dimulai dari areaOriginPos (sudut kiri bawah)
        for (int dx = 0; dx < 2; dx++)
        {
            for (int dy = 0; dy < 2; dy++)
            {
                int targetX = areaOriginPos.x + dx;
                int targetY = areaOriginPos.y + dy;

                if (PositionOnBoard(targetX, targetY))
                {
                    GameObject pieceToFreeze = GetPosition(targetX, targetY);
                    // Bekukan bidak lawan jika ada
                    if (pieceToFreeze != null && pieceToFreeze.GetComponent<Chessman>().player != deployingPlayerColor)
                    {
                        if (!frozenPieces.ContainsKey(pieceToFreeze))
                        {
                            frozenPieces[pieceToFreeze] = duration;
                            SpriteRenderer sr = pieceToFreeze.GetComponent<SpriteRenderer>();
                            if (sr != null)
                            {
                                sr.color = Color.gray; // Warna beku/stun untuk Earth
                            }
                            Debug.Log($"Bidak {pieceToFreeze.name} di ({targetX},{targetY}) dibekukan/distun selama {duration} giliran!");
                        }
                    }
                    else if (pieceToFreeze == null)
                    {
                        // Jika petak kosong, jadikan obstacle tidak dapat ditempati
                        // Periksa apakah sudah ada obstacle lain di sana (misalnya Firewall)
                        if (!allCustomObstacles.ContainsKey(new Vector2Int(targetX, targetY)) && !activeEarthObstacles.ContainsKey(new Vector2Int(targetX, targetY)))
                        {
                            GameObject obstacleVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            obstacleVisual.transform.localScale = new Vector3(1f, 1f, 0.5f);
                            obstacleVisual.transform.position = new Vector3(targetX * 1.1f - 3.8f, targetY * 1.1f - 3.8f, -5.0f); // Posisi Z di bawah bidak
                            obstacleVisual.name = "EarthStunObstacle";
                            obstacleVisual.GetComponent<Renderer>().material.color = new Color(0.5f, 0.25f, 0f, 0.7f); // Warna tanah

                            obstacleVisual.AddComponent<BoxCollider2D>();

                            activeEarthObstacles[new Vector2Int(targetX, targetY)] = duration; // Daftarkan durasi
                            visualEarthObstacles[new Vector2Int(targetX, targetY)] = obstacleVisual; // Daftarkan objek visual
                            Debug.Log($"EarthStun obstacle ditempatkan di ({targetX},{targetY}) selama {duration} giliran!");
                        }
                        else
                        {
                            Debug.LogWarning($"Petak ({targetX},{targetY}) sudah ada obstacle, tidak bisa menempatkan EarthStun obstacle.");
                        }
                    }
                    else
                    {
                        Debug.Log($"Bidak {pieceToFreeze.name} di ({targetX},{targetY}) adalah bidak sendiri, tidak dibekukan.");
                    }
                }
            }
        }
    }
    // Akhir Metode Baru FreezeArea


    public bool IsPieceFrozen(GameObject piece)
    {
        if (piece == null) return false;
        return frozenPieces.ContainsKey(piece) && frozenPieces[piece] > 0;
    }

    private void UnfreezePiece(GameObject piece)
    {
        if (frozenPieces.ContainsKey(piece))
        {
            frozenPieces.Remove(piece);
            if (piece != null)
            {
                SpriteRenderer sr = piece.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = Color.white;
                    piece.GetComponent<Chessman>().Activate(); // Memuat ulang sprite asli dan warna default
                }
                Debug.Log($"Bidak {piece.name} tidak lagi beku.");
            }
            else
            {
                Debug.Log($"Bidak yang sudah dibekukan tidak lagi ditemukan (mungkin sudah dimakan/dihancurkan).");
            }
        }
    }


    void Update()
    {
        if (gameOver || !timerRunning) return;

        currentTimer -= Time.deltaTime;
        if (currentTimer <= 0f)
        {
            Debug.Log("Waktu habis! Giliran diskip ke pemain berikutnya.");
            currentTimer = turnTime;
            NextTurn();
        }

        turnTimerText.text = Mathf.Ceil(currentTimer).ToString();

       // if (IsCheckmate(currentPlayer)) // asumsikan ini method boolean
       // {
       //     bgMusic.PlayVictorySound();
       // }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Tombol Space ditekan.");
            if (inSkillPlacementMode)
            {
                Debug.Log("Anda sudah dalam mode penempatan skill. Pilih lokasi di papan.");
                return;
            }

            GameObject[] currentPieces = GetCurrentPlayer() == "white" ? playerWhite : playerBlack;

            foreach (GameObject piece in currentPieces)
            {
                if (piece == null) continue;

                CommanderSkill skill = piece.GetComponent<CommanderSkill>();
                if (skill != null && skill.CanUseSkill())
                {
                    Debug.Log($"Mengaktifkan skill {skill.skillType} dari {piece.name}.");
                    skill.UseSkill();
                    Debug.Log($"Player {currentPlayer} menggunakan skill {skill.skillType} dan memasuki mode penempatan.");
                    break;
                }
            }
        }
    }


    private void AddCommanderSkill(GameObject piece, string commanderType)
    {
        var skill = piece.AddComponent<CommanderSkill>();
        switch (commanderType)
        {
            case "fire":
                skill.skillType = CommanderSkill.SkillType.FireWall;
                break;
            case "ice":
                skill.skillType = CommanderSkill.SkillType.IceFreeze;
                break;
            case "earth":
                skill.skillType = CommanderSkill.SkillType.EarthStun;
                break;
        }
    }

    private CommanderSkill.SkillType ParseSkillType(string type)
    {
        switch (type)
        {
            case "fire": return CommanderSkill.SkillType.FireWall;
            case "ice": return CommanderSkill.SkillType.IceFreeze;
            case "earth": return CommanderSkill.SkillType.EarthStun;
            default: return CommanderSkill.SkillType.FireWall;
        }
    }
}