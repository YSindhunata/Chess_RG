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
    public TextMeshProUGUI checkmateText;

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

    private Dictionary<Vector2Int, GameObject> allCustomObstacles = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, string> obstacleOwnerColors = new Dictionary<Vector2Int, string>();

    private Dictionary<GameObject, int> frozenPieces = new Dictionary<GameObject, int>();
    public int freezeDuration = 5;

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


    public string GetCurrentPlayer()
    {
        return currentPlayer;
    }

    public void NextTurn()
    {
        Debug.Log("--- NextTurn() dipanggil --- CurrentPlayer sebelum ganti: " + currentPlayer); // DEBUG LOG
        if (gameOver) return;

        if (inSkillPlacementMode)
        {
            Debug.Log("Keluar dari skill placement mode karena giliran berganti secara paksa."); // DEBUG LOG
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

        currentPlayer = currentPlayer == "white" ? "black" : "white";
        currentTimer = turnTime;

        // --- PERBAIKAN DI SINI: Kurangi cooldown untuk SEMUA CommanderSkill ---
        // 1. Kurangi cooldown untuk skill yang terpasang di Raja (bidak catur)
        GameObject[] allPiecesInPlay = playerWhite.Concat(playerBlack).ToArray(); // Gabungkan semua bidak aktif
        foreach (GameObject piece in allPiecesInPlay)
        {
            if (piece == null) continue;

            CommanderSkill skill = piece.GetComponent<CommanderSkill>();
            if (skill != null)
            {
                skill.OnTurnPassed(); // Kurangi cooldown milik pemain yang giliran BARU SELESAI
                if (skill.GetRemainingCooldown() == 0)
                {
                    RemoveFirewallForPlayer(skill.GetPlayerColor());
                }
            }
        }

        // 2. Kurangi cooldown untuk skill yang terpasang di objek visual komandan
        GameObject visualCommanderP1 = GameObject.Find("CommanderVisual_P1");
        if (visualCommanderP1 != null)
        {
            CommanderSkill skill = visualCommanderP1.GetComponent<CommanderSkill>();
            if (skill != null)
            {
                skill.OnTurnPassed(); // Cooldown objek visual
                // Tidak perlu hapus firewall di sini, karena firewall dikelola oleh skill di Raja
            }
        }

        GameObject visualCommanderP2 = GameObject.Find("CommanderVisual_P2");
        if (visualCommanderP2 != null)
        {
            CommanderSkill skill = visualCommanderP2.GetComponent<CommanderSkill>();
            if (skill != null)
            {
                skill.OnTurnPassed(); // Cooldown objek visual
            }
        }
        // --- AKHIR PERBAIKAN ---

        UpdateCommanderCooldownUI();


        if (IsCheckmate(currentPlayer))
        {
            Debug.Log("CHECKMATE! Winner: " + (currentPlayer == "white" ? "black" : "white"));
            gameOver = true;
            timerRunning = false;
            ShowGameOverUI();
        }
        Debug.Log("--- NextTurn() selesai --- CurrentPlayer setelah ganti: " + currentPlayer); // DEBUG LOG
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

                // --- PERBAIKAN DI SINI ---
                // 'newY' tidak ada dalam scope ini. Gunakan 'move.y'
                SetPositionEmpty(move.x, move.y);
                // --- AKHIR PERBAIKAN ---

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
                GameObject commanderP1 = Instantiate(prefab, new Vector3(-6.5f, 2f, -1f), Quaternion.identity);
                commanderP1.name = "CommanderVisual_P1";

                SpriteRenderer sr = commanderP1.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = Resources.Load<Sprite>(p1Type + "_commander");
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
                GameObject commanderP2 = Instantiate(prefab2, new Vector3(9f, 5f, -1f), Quaternion.identity);
                commanderP2.name = "CommanderVisual_P2";

                SpriteRenderer sr = commanderP2.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = Resources.Load<Sprite>(p2Type + "_commander");
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

    public bool IsCustomObstacle(int x, int y)
    {
        bool isObstacle = allCustomObstacles.ContainsKey(new Vector2Int(x, y));
        return isObstacle;
    }

    public string GetObstacleOwnerColor(int x, int y)
    {
        Vector2Int pos = new Vector2Int(x, y);
        if (obstacleOwnerColors.ContainsKey(pos))
        {
            return obstacleOwnerColors[pos];
        }
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
        Debug.Log($"Memasuki mode penempatan skill: {skillType}"); // DEBUG LOG
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
        Debug.Log("Keluar dari mode penempatan skill."); // DEBUG LOG
        inSkillPlacementMode = false;
        currentSkillToPlace = CommanderSkill.SkillType.FireWall;
        activeCommanderUsingSkill = null;
        firstClickPos = new Vector2Int(-1, -1);
        ClearSkillPlacementPlates();
        HideOrientationButtons();
    }

    private void ShowSkillPlacementOptions()
    {
        Debug.Log("Menampilkan opsi penempatan skill untuk: " + currentSkillToPlace); // DEBUG LOG
        ClearSkillPlacementPlates();
        HideOrientationButtons();

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (currentSkillToPlace == CommanderSkill.SkillType.FireWall)
                {
                    if (GetPosition(x, y) == null && !IsCustomObstacle(x, y))
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
                        mpScript.SetReference(activeCommanderUsingSkill); // Komandan yang menggunakan skill
                        currentSkillPlates.Add(new Vector2Int(x, y), mp);
                        mp.GetComponent<SpriteRenderer>().color = new Color(0.0f, 0.7f, 1.0f, 0.7f); // Biru muda transparan
                    }
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
        Debug.Log($"ConfirmSkillPlacement dipanggil untuk ({x},{y}). Mode: {inSkillPlacementMode}, Skill: {currentSkillToPlace}"); // DEBUG LOG

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
                Debug.Log($"Target valid untuk Freeze: {targetPiece.name} di ({x},{y})."); // DEBUG LOG
                commanderSkill.ActivateFreeze(new Vector2Int(x, y), currentPlayer);
                ExitSkillPlacementMode();
                // NextTurn() TIDAK DIPANGGIL DI SINI untuk Freeze yang BERHASIL
                UpdateCommanderCooldownUI();
            }
            else
            {
                Debug.LogWarning("Target tidak valid untuk skill Freeze (bukan lawan atau sudah beku). Skill hangus.");
                ExitSkillPlacementMode();
                Debug.Log("Memanggil NextTurn() karena target Freeze tidak valid."); // DEBUG LOG
                NextTurn(); // Giliran berganti jika target tidak valid
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
        Debug.Log("PlaceFireWallSkill dipanggil."); // DEBUG LOG
        if (!inSkillPlacementMode || activeCommanderUsingSkill == null) return;
        if (currentSkillToPlace != CommanderSkill.SkillType.FireWall) return;

        CommanderSkill commanderSkill = activeCommanderUsingSkill.GetComponent<CommanderSkill>();
        if (commanderSkill != null)
        {
            commanderSkill.PlaceFireWall(centerPos, isHorizontal, currentPlayer);
        }

        ExitSkillPlacementMode();
        Debug.Log("Memanggil NextTurn() setelah PlaceFireWallSkill."); // DEBUG LOG
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
                    sr.color = Color.white; // Kembalikan warna ke putih/default
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

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Tombol Space ditekan."); // DEBUG LOG
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
                    Debug.Log($"Mengaktifkan skill {skill.skillType} dari {piece.name}."); // DEBUG LOG
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
        }
    }

    private CommanderSkill.SkillType ParseSkillType(string type)
    {
        switch (type)
        {
            case "fire": return CommanderSkill.SkillType.FireWall;
            case "ice": return CommanderSkill.SkillType.IceFreeze;
            default: return CommanderSkill.SkillType.FireWall;
        }
    }
}