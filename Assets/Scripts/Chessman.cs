using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chessman : MonoBehaviour
{
    // References
    public GameObject Controller;
    public GameObject movePlate;

    // Positions
    private int xBoard = -1;
    private int yBoard = -1;

    //Variable to keep track of "black" or "white"
    public string player;

    //References for all sprite that the chesspiece can be
    public Sprite black_queen, black_knight, black_bishop, black_king, black_rook, black_pawn;
    public Sprite white_queen, white_knight, white_bishop, white_king, white_rook, white_pawn;

    
    public void Activate()
    {
        Controller = GameObject.FindGameObjectWithTag("GameController");
        //take the instantiated location and adjust the transform
        SetCoords();

        switch (this.name)
        {
            case "black_queen": this.GetComponent<SpriteRenderer>().sprite = black_queen; player = "black"; break;
            case "black_knight": this.GetComponent<SpriteRenderer>().sprite = black_knight; player = "black"; break;
            case "black_bishop": this.GetComponent<SpriteRenderer>().sprite = black_bishop; player = "black"; break;
            case "black_king": this.GetComponent<SpriteRenderer>().sprite = black_king; player = "black"; break;
            case "black_rook": this.GetComponent<SpriteRenderer>().sprite = black_rook; player = "black"; break;
            case "black_pawn": this.GetComponent<SpriteRenderer>().sprite = black_pawn; player = "black"; break;

            case "white_queen": this.GetComponent<SpriteRenderer>().sprite = white_queen; player = "white"; break;
            case "white_knight": this.GetComponent<SpriteRenderer>().sprite = white_knight; player = "white"; break;
            case "white_bishop": this.GetComponent<SpriteRenderer>().sprite = white_bishop; player = "white"; break;
            case "white_king": this.GetComponent<SpriteRenderer>().sprite = white_king; player = "white"; break;
            case "white_rook": this.GetComponent<SpriteRenderer>().sprite = white_rook; player = "white"; break;
            case "white_pawn": this.GetComponent<SpriteRenderer>().sprite = white_pawn; player = "white"; break;
        }
    }
    //hgjhfjhgjhgjhg
    public void SetCoords()
    {
        float x = xBoard;
        float y = yBoard;

        x *= 0.66f;
        y *= 0.66f;

        x += -2.3f;
        y += -2.3f;

        this.transform.position = new Vector3(x, y, -1.0f);
    }

    public int GetXBoard()
    {
        return xBoard;
    }

    public int GetYBoard()
    {
        return yBoard;
    }

    public void SetXBoard(int x)
    {
        xBoard = x;
    }

    public void SetYBoard(int y)
    {
        yBoard = y;
    }

    //Destroy previous moveplate when switching chesspiece
    private void OnMouseUp()
    {
        Game gameController = Controller.GetComponent<Game>();

        // Cek apakah giliran pemain yang sesuai
        if (player == gameController.GetCurrentPlayer())
        {
            DestroyMovePlates();
            InitiateMovePlates();
        }
    }

    public void DestroyMovePlates()
    {
        GameObject[] movePlates = GameObject.FindGameObjectsWithTag("MovePlate");
        for (int i = 0; i < movePlates.Length; i++)
        {
            Destroy(movePlates[i]);
        }
    }

    // Making movement on chesspiece
    public void InitiateMovePlates()
    {
        switch (this.name)
        {
            case "black_queen":
            case "white_queen":
                LineMovePlate(1, 0);
                LineMovePlate(0, 1);
                LineMovePlate(1, 1);
                LineMovePlate(-1, 0);
                LineMovePlate(0, -1);
                LineMovePlate(-1, -1);
                LineMovePlate(-1, 1);
                LineMovePlate(1, -1);
                break;
            case "black_knight":
            case "white_knight":
                LMovePlate();
                break;
            case "black_bishop":
            case "white_bishop":
                LineMovePlate(1, 1);
                LineMovePlate(1, -1);
                LineMovePlate(-1, 1);
                LineMovePlate(-1, -1);
                break;

            case "black_king":
            case "white_king":
                SurroundMovePlate();
                break;
            case "black_rook":
            case "white_rook":
                LineMovePlate(1, 0);
                LineMovePlate(0, 1);
                LineMovePlate(-1, 0);
                LineMovePlate(0, -1);
                break;
            case "black_pawn":
                PawnMovePlate(xBoard, yBoard - 1);
                break;
            case "white_pawn":
                PawnMovePlate(xBoard, yBoard + 1);
                break;
        }
    }

    public void LineMovePlate(int xIncrement, int yIncrement)
    {
        Game sc = Controller.GetComponent<Game>();

        int x = xBoard + xIncrement;
        int y = yBoard + yIncrement;

        while(sc.PositionOnBoard(x,y) && sc.GetPosition(x,y) == null)
        {
            MovePlateSpawn(x,y);
            x += xIncrement;
            y += yIncrement;
        }

        if (sc.PositionOnBoard(x,y) && sc.GetPosition(x, y).GetComponent<Chessman>().player != player)
        {
            MovePlateAttactSpawn(x, y);
        }
    }

    public void LMovePlate()
    {
        PointMovePlate(xBoard + 1, yBoard + 2);
        PointMovePlate(xBoard - 1, yBoard + 2);
        PointMovePlate(xBoard + 2, yBoard + 1);
        PointMovePlate(xBoard + 2, yBoard - 1);
        PointMovePlate(xBoard + 1, yBoard - 2);
        PointMovePlate(xBoard - 1, yBoard - 2);
        PointMovePlate(xBoard - 2, yBoard + 1);
        PointMovePlate(xBoard - 2, yBoard - 1);
    }

    public void SurroundMovePlate()
    {
        PointMovePlate(xBoard, yBoard + 1);
        PointMovePlate(xBoard, yBoard - 1);
        PointMovePlate(xBoard - 1, yBoard - 1);
        PointMovePlate(xBoard - 1, yBoard - 0);
        PointMovePlate(xBoard - 1, yBoard + 1);
        PointMovePlate(xBoard + 1, yBoard - 1);
        PointMovePlate(xBoard + 1, yBoard - 0);
        PointMovePlate(xBoard + 1, yBoard + 1);
    }

    public void PointMovePlate(int x, int y)
    {
        Game sc = Controller.GetComponent<Game>();
        if (sc.PositionOnBoard(x, y))
        {
            GameObject cp = sc.GetPosition(x, y);
            if (cp == null)
            {
                MovePlateSpawn(x, y);
            }
            else if (cp.GetComponent<Chessman>().player != player)
            {
                MovePlateAttactSpawn(x, y);
            }
        }
    }


    public void PawnMovePlate(int x, int y)
    {
        Game sc = Controller.GetComponent<Game>();
        // Satu langkah maju
        if (sc.PositionOnBoard(x, y) && sc.GetPosition(x, y) == null)
        {
            MovePlateSpawn(x, y);

            // Dua langkah maju dari posisi awal
            if (player == "white" && yBoard == 1 && sc.PositionOnBoard(x, y + 1) && sc.GetPosition(x, y + 1) == null)
            {
                MovePlateSpawn(x, y + 1);
            }
            else if (player == "black" && yBoard == 6 && sc.PositionOnBoard(x, y - 1) && sc.GetPosition(x, y - 1) == null)
            {
                MovePlateSpawn(x, y - 1);
            }
        }

        // Cek serangan diagonal
        if (sc.PositionOnBoard(x + 1, y) && sc.GetPosition(x + 1, y) != null && sc.GetPosition(x + 1, y).GetComponent<Chessman>().player != player)
        {
            MovePlateAttactSpawn(x + 1, y);
        }

        if (sc.PositionOnBoard(x - 1, y) && sc.GetPosition(x - 1, y) != null && sc.GetPosition(x - 1, y).GetComponent<Chessman>().player != player)
        {
            MovePlateAttactSpawn(x - 1, y);
        }
    }


    public void MovePlateSpawn(int matrixX, int matrixY) 
    {
        float x = matrixX;
        float y = matrixY;

        x *= 0.66f;
        y *= 0.66f;

        x += -2.3f;
        y += -2.3f;

        GameObject mp = Instantiate(movePlate, new Vector3(x, y, -3.0f), Quaternion.identity);

        MovePlate mpScript = mp.GetComponent<MovePlate>();
        mpScript.SetReference(gameObject);
        mpScript.SetCoords(matrixX, matrixY);
    }

    public void MovePlateAttactSpawn(int matrixX, int matrixY)
    {
        float x = matrixX;
        float y = matrixY;

        x *= 0.66f;
        y *= 0.66f;

        x += -2.3f;
        y += -2.3f;

        GameObject mp = Instantiate(movePlate, new Vector3(x, y, -3.0f), Quaternion.identity);

        MovePlate mpScript = mp.GetComponent<MovePlate>();
        mpScript.attack = true;
        mpScript.SetReference(gameObject);
        mpScript.SetCoords(matrixX, matrixY);
    }

    //Logika Skakmat
    public List<Vector2Int> GetLegalMoves()
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        Game sc = Controller.GetComponent<Game>();

        // Gunakan kembali semua logika seperti di InitiateMovePlates, tetapi hanya hitung koordinat legal
        switch (this.name)
        {
            case "black_queen":
            case "white_queen":
                moves.AddRange(GetLineMoves(1, 0));
                moves.AddRange(GetLineMoves(0, 1));
                moves.AddRange(GetLineMoves(1, 1));
                moves.AddRange(GetLineMoves(-1, 0));
                moves.AddRange(GetLineMoves(0, -1));
                moves.AddRange(GetLineMoves(-1, -1));
                moves.AddRange(GetLineMoves(-1, 1));
                moves.AddRange(GetLineMoves(1, -1));
                break;

            case "white_knight":
            case "black_knight":
                int[,] knightMoves = { { 1, 2 }, { -1, 2 }, { 2, 1 }, { 2, -1 }, { 1, -2 }, { -1, -2 }, { -2, 1 }, { -2, -1 } };
                for (int i = 0; i < knightMoves.GetLength(0); i++)
                {
                    int dx = knightMoves[i, 0];
                    int dy = knightMoves[i, 1];
                    TryAddMove(dx, dy, moves);
                }
                break;

            case "black_bishop":
            case "white_bishop":
                moves.AddRange(GetLineMoves(1, 1));
                moves.AddRange(GetLineMoves(-1, 1));
                moves.AddRange(GetLineMoves(1, -1));
                moves.AddRange(GetLineMoves(-1, -1));
                break;

            case "black_king":
            case "white_king":
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        if (dx != 0 || dy != 0)
                            TryAddMove(dx, dy, moves, oneStep: true);
                break;

            case "black_rook":
            case "white_rook":
                moves.AddRange(GetLineMoves(1, 0));
                moves.AddRange(GetLineMoves(0, 1));
                moves.AddRange(GetLineMoves(-1, 0));
                moves.AddRange(GetLineMoves(0, -1));
                break;

            case "black_pawn":
            case "white_pawn":
                int dir = (player == "white") ? 1 : -1;
                int startRow = (player == "white") ? 1 : 6;

                if (sc.PositionOnBoard(xBoard, yBoard + dir) && sc.GetPosition(xBoard, yBoard + dir) == null)
                {
                    moves.Add(new Vector2Int(xBoard, yBoard + dir));
                    if (yBoard == startRow && sc.GetPosition(xBoard, yBoard + dir * 2) == null)
                        moves.Add(new Vector2Int(xBoard, yBoard + dir * 2));
                }

                if (sc.PositionOnBoard(xBoard + 1, yBoard + dir))
                {
                    var cp = sc.GetPosition(xBoard + 1, yBoard + dir);
                    if (cp != null && cp.GetComponent<Chessman>().player != player)
                        moves.Add(new Vector2Int(xBoard + 1, yBoard + dir));
                }

                if (sc.PositionOnBoard(xBoard - 1, yBoard + dir))
                {
                    var cp = sc.GetPosition(xBoard - 1, yBoard + dir);
                    if (cp != null && cp.GetComponent<Chessman>().player != player)
                        moves.Add(new Vector2Int(xBoard - 1, yBoard + dir));
                }
                break;
        }

        return moves;
    }

    private List<Vector2Int> GetLineMoves(int xDir, int yDir)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        Game sc = Controller.GetComponent<Game>();
        int x = xBoard + xDir;
        int y = yBoard + yDir;

        while (sc.PositionOnBoard(x, y))
        {
            GameObject cp = sc.GetPosition(x, y);
            if (cp == null)
            {
                result.Add(new Vector2Int(x, y));
            }
            else
            {
                if (cp.GetComponent<Chessman>().player != player)
                    result.Add(new Vector2Int(x, y));
                break;
            }
            x += xDir;
            y += yDir;
        }

        return result;
    }

    private void TryAddMove(int dx, int dy, List<Vector2Int> moves, bool oneStep = false)
    {
        Game sc = Controller.GetComponent<Game>();
        int x = xBoard + dx;
        int y = yBoard + dy;

        if (sc.PositionOnBoard(x, y))
        {
            GameObject cp = sc.GetPosition(x, y);
            if (cp == null || cp.GetComponent<Chessman>().player != player)
            {
                moves.Add(new Vector2Int(x, y));
            }
        }
    }

}

