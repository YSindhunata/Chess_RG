using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using TMPro;
using UnityEngine.UI;

public class Game : MonoBehaviour
{
    
    public GameObject ChessPiece;

    //Timer giliran 
    public TextMeshProUGUI turnTimerText;
    private float turnTime = 20f; // Durasi per giliran
    private float currentTimer = 20f;
    private bool timerRunning = true;

    // Positions and team fir each chesspieces
    private GameObject[,] positions = new GameObject[8, 8];
    private GameObject[] playerBlack = new GameObject[16];
    private GameObject[] playerWhite = new GameObject[16];

    private string currentPlayer = "white";
    private bool gameOver = false;


    public string GetCurrentPlayer()
    {
        return currentPlayer;
    }

    public void NextTurn()
    {
        if (gameOver) return;

        currentPlayer = currentPlayer == "white" ? "black" : "white";
        currentTimer = turnTime; //reset timer

        if (IsCheckmate(currentPlayer))
        {
            Debug.Log("CHECKMATE! Winner: " + (currentPlayer == "white" ? "black" : "white"));
            gameOver = true;
            timerRunning = false;
        }
    }

    //Logika skakmat
    public bool IsCheckmate(string player)
    {
        GameObject king = FindKing(player);
        if (king == null) return false;

        // Jika raja tidak sedang diserang, bukan checkmate
        if (!IsUnderAttack(king.GetComponent<Chessman>().GetXBoard(), king.GetComponent<Chessman>().GetYBoard(), player))
            return false;

        // Coba semua langkah pemain, jika ada langkah legal yang bisa menghindari skak, bukan checkmate
        GameObject[] pieces = player == "white" ? playerWhite : playerBlack;
        foreach (GameObject piece in pieces)
        {
            if (piece == null) continue;
            Chessman cm = piece.GetComponent<Chessman>();

            List<Vector2Int> moves = cm.GetLegalMoves();
            foreach (Vector2Int move in moves)
            {
                // Simulasikan langkah
                int oldX = cm.GetXBoard();
                int oldY = cm.GetYBoard();
                GameObject captured = GetPosition(move.x, move.y);

                SetPositionEmpty(oldX, oldY);
                cm.SetXBoard(move.x);
                cm.SetYBoard(move.y);
                SetPosition(piece);

                if (captured != null) captured.SetActive(false);

                bool stillInCheck = IsUnderAttack(FindKing(player).GetComponent<Chessman>().GetXBoard(), FindKing(player).GetComponent<Chessman>().GetYBoard(), player);

                // Undo simulasi
                SetPositionEmpty(move.x, move.y);
                cm.SetXBoard(oldX);
                cm.SetYBoard(oldY);
                SetPosition(piece);
                if (captured != null) captured.SetActive(true);
                SetPosition(captured);

                if (!stillInCheck)
                    return false; // Ada langkah legal untuk keluar dari skak
            }
        }

        return true; // Tidak ada langkah legal, checkmate
    }

    public bool IsUnderAttack(int x, int y, string player)
    {
        GameObject[] enemyPieces = player == "white" ? playerBlack : playerWhite;

        foreach (GameObject piece in enemyPieces)
        {
            if (piece == null) continue;
            Chessman cm = piece.GetComponent<Chessman>();
            List<Vector2Int> enemyMoves = cm.GetLegalMoves();

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


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerWhite = new GameObject[]
        {
            Create("white_rook", 0,0), Create("white_knight", 1,0), Create("white_bishop", 2,0),
            Create("white_queen", 3,0), Create("white_king", 4,0), Create("white_bishop", 5,0),
            Create("white_knight", 6,0), Create("white_rook", 7,0), Create("white_pawn", 0,1),
            Create("white_pawn", 1,1), Create("white_pawn", 2,1), Create("white_pawn", 3,1),
            Create("white_pawn", 4,1), Create("white_pawn", 5,1), Create("white_pawn", 6,1),
            Create("white_pawn", 7,1)
        };

        playerBlack = new GameObject[]
        {
            Create("black_rook", 0,7), Create("black_knight", 1,7), Create("black_bishop", 2,7),
            Create("black_queen", 3,7), Create("black_king", 4,7), Create("black_bishop", 5,7),
            Create("black_knight", 6,7), Create("black_rook", 7,7), Create("black_pawn", 0,6),
            Create("black_pawn", 1,6), Create("black_pawn", 2, 6), Create("black_pawn", 3, 6),
            Create("black_pawn", 4, 6), Create("black_pawn", 5, 6), Create("black_pawn", 6, 6),
            Create("black_pawn", 7, 6)
        };

        // Set all piece positions on the position board
        for (int i = 0; i < playerBlack.Length; i++)
        {
            SetPosition(playerBlack[i]);
            SetPosition(playerWhite[i]);
        }

        turnTimerText.text = Mathf.Ceil(currentTimer).ToString();
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

    // Movement on chesspieces using moveplate sprites
    public void SetPositionEmpty(int x, int y)
    {
        positions[x, y] = null;
    }

    public GameObject GetPosition(int x, int y)
    {
        return positions[x, y];
    }

    public bool PositionOnBoard(int x, int y)
    {
        if (x < 0 || y < 0 || x >= positions.GetLength(0) || y >= positions.GetLength(1)) return false;
        return true;
    }



    // Update is called once per frame
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
    }
}
