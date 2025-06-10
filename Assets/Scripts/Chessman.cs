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
        // Get only truly legal moves (those that don't leave the king in check)
        List<Vector2Int> legalMoves = GetLegalMoves();

        foreach (Vector2Int move in legalMoves)
        {
            // Check if the target position contains an enemy piece
            Game sc = Controller.GetComponent<Game>();
            GameObject targetPiece = sc.GetPosition(move.x, move.y);

            if (targetPiece != null && targetPiece.GetComponent<Chessman>().player != player)
            {
                MovePlateAttactSpawn(move.x, move.y);
            }
            else
            {
                MovePlateSpawn(move.x, move.y);
            }
        }
    }

    // Helper method to get all potential moves for line-moving pieces (Queen, Rook, Bishop)
    private List<Vector2Int> GetLineMovesInternal(int xDir, int yDir)
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
                // Add the enemy piece's position as a potential capture, then stop
                if (cp.GetComponent<Chessman>().player != player)
                    result.Add(new Vector2Int(x, y));
                break; 
            }
            x += xDir;
            y += yDir;
        }
        return result;
    }

    // Helper method to add a single potential move (for Knight, King)
    private void TryAddRawMove(int dx, int dy, List<Vector2Int> rawMovesList)
    {
        Game sc = Controller.GetComponent<Game>();
        int x = xBoard + dx;
        int y = yBoard + dy;

        if (sc.PositionOnBoard(x, y))
        {
            GameObject cp = sc.GetPosition(x, y);
            if (cp == null || cp.GetComponent<Chessman>().player != player)
            {
                rawMovesList.Add(new Vector2Int(x, y));
            }
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

    // Helper cek king urip
    private bool IsKingInCheckAfterMove(int oldX, int oldY, int newX, int newY)
    {
        Game gameController = Controller.GetComponent<Game>();

        // Store state
        GameObject originalPieceAtNewPos = gameController.GetPosition(newX, newY);
        bool originalPieceActive = false;
        if (originalPieceAtNewPos != null)
        {
            originalPieceActive = originalPieceAtNewPos.activeSelf;
            originalPieceAtNewPos.SetActive(false); 
        }

        // Simulate move
        gameController.SetPositionEmpty(oldX, oldY);
        int originalXBoard = xBoard;
        int originalYBoard = yBoard;
        xBoard = newX;
        yBoard = newY;
        gameController.SetPosition(this.gameObject); 

        
        GameObject king = gameController.FindKing(player);
        bool kingInCheck = false;

        if (king != null)
        {

            kingInCheck = gameController.IsUnderAttack(king.GetComponent<Chessman>().GetXBoard(), king.GetComponent<Chessman>().GetYBoard(), player);
        }
        else
        {
            // If the king is null, it means the king itself was captured by this move, which is an illegal state.
            // So, this move is illegal as it leads to the king's capture.
            kingInCheck = true;
        }

        // Undo move
        gameController.SetPositionEmpty(newX, newY);
        xBoard = originalXBoard;
        yBoard = originalYBoard;
        gameController.SetPosition(this.gameObject); // Place this piece back at old position

        // Restore
        if (originalPieceAtNewPos != null)
        {
            originalPieceAtNewPos.SetActive(originalPieceActive);
            gameController.SetPosition(originalPieceAtNewPos);
        }

        return kingInCheck;
    }

    // ngitung move nggo illegal move
    public List<Vector2Int> GetPotentialMoves() // tak ganti publik
    {
        List<Vector2Int> rawMoves = new List<Vector2Int>();
        Game sc = Controller.GetComponent<Game>();

        switch (this.name)
        {
            case "black_queen":
            case "white_queen":
                rawMoves.AddRange(GetLineMovesInternal(1, 0));
                rawMoves.AddRange(GetLineMovesInternal(0, 1));
                rawMoves.AddRange(GetLineMovesInternal(1, 1));
                rawMoves.AddRange(GetLineMovesInternal(-1, 0));
                rawMoves.AddRange(GetLineMovesInternal(0, -1));
                rawMoves.AddRange(GetLineMovesInternal(-1, -1));
                rawMoves.AddRange(GetLineMovesInternal(-1, 1));
                rawMoves.AddRange(GetLineMovesInternal(1, -1));
                break;

            case "white_knight":
            case "black_knight":
                int[,] knightMoves = { { 1, 2 }, { -1, 2 }, { 2, 1 }, { 2, -1 }, { 1, -2 }, { -1, -2 }, { -2, 1 }, { -2, -1 } };
                for (int i = 0; i < knightMoves.GetLength(0); i++)
                {
                    int dx = knightMoves[i, 0];
                    int dy = knightMoves[i, 1];
                    TryAddRawMove(dx, dy, rawMoves);
                }
                break;

            case "black_bishop":
            case "white_bishop":
                rawMoves.AddRange(GetLineMovesInternal(1, 1));
                rawMoves.AddRange(GetLineMovesInternal(-1, 1));
                rawMoves.AddRange(GetLineMovesInternal(1, -1));
                rawMoves.AddRange(GetLineMovesInternal(-1, -1));
                break;

            case "black_king":
            case "white_king":
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        if (dx != 0 || dy != 0)
                            TryAddRawMove(dx, dy, rawMoves);
                break;

            case "black_rook":
            case "white_rook":
                rawMoves.AddRange(GetLineMovesInternal(1, 0));
                rawMoves.AddRange(GetLineMovesInternal(0, 1));
                rawMoves.AddRange(GetLineMovesInternal(-1, 0));
                rawMoves.AddRange(GetLineMovesInternal(0, -1));
                break;

            case "black_pawn":
            case "white_pawn":
                int dir = (player == "white") ? 1 : -1;
                int startRow = (player == "white") ? 1 : 6;

                // Forward 1
                if (sc.PositionOnBoard(xBoard, yBoard + dir) && sc.GetPosition(xBoard, yBoard + dir) == null)
                {
                    rawMoves.Add(new Vector2Int(xBoard, yBoard + dir));
                    // Forward 2
                    if (yBoard == startRow && sc.PositionOnBoard(xBoard, yBoard + dir * 2) && sc.GetPosition(xBoard, yBoard + dir * 2) == null)
                    {
                        rawMoves.Add(new Vector2Int(xBoard, yBoard + dir * 2));
                    }
                }

                // Diagonal captures
                if (sc.PositionOnBoard(xBoard + 1, yBoard + dir))
                {
                    var cp = sc.GetPosition(xBoard + 1, yBoard + dir);
                    if (cp != null && cp.GetComponent<Chessman>().player != player)
                        rawMoves.Add(new Vector2Int(xBoard + 1, yBoard + dir));
                }
                if (sc.PositionOnBoard(xBoard - 1, yBoard + dir))
                {
                    var cp = sc.GetPosition(xBoard - 1, yBoard + dir);
                    if (cp != null && cp.GetComponent<Chessman>().player != player)
                        rawMoves.Add(new Vector2Int(xBoard - 1, yBoard + dir));
                }
                break;
        }
        return rawMoves;
    }


    // This method returns only truly legal moves (those that do not leave the king in check).
    public List<Vector2Int> GetLegalMoves()
    {
        List<Vector2Int> potentialMoves = GetPotentialMoves(); // Get all potential moves
        List<Vector2Int> legalMoves = new List<Vector2Int>();
        foreach (Vector2Int move in potentialMoves)
        {
            if (!IsKingInCheckAfterMove(xBoard, yBoard, move.x, move.y))
            {
                legalMoves.Add(move);
            }
        }
        return legalMoves;
    }
}
