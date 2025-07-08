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

        x *= 1.1f;
        y *= 1.1f;

        x += -3.8f;
        y += -3.8f;

        this.transform.position = new Vector3(x, y, -1.0f); // Posisi Z bidak
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

    private void OnMouseUp()
    {
        Game gameController = Controller.GetComponent<Game>();

        if (player == gameController.GetCurrentPlayer())
        {
            // --- Tambahan: Cek apakah bidak ini beku ---
            if (gameController.IsPieceFrozen(this.gameObject))
            {
                Debug.Log($"{this.name} dibekukan! Tidak bisa bergerak.");
                return; // Jangan lakukan apa-apa jika beku
            }
            // --- Akhir Tambahan ---
            if (gameController.inSkillPlacementMode)
            {
                Debug.Log("Game dalam mode penempatan skill. Tidak bisa memilih bidak catur.");
                return;
            }

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

    public void InitiateMovePlates()
    {
        List<Vector2Int> legalMoves = GetLegalMoves();

        foreach (Vector2Int move in legalMoves)
        {
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
            // Periksa jika ada Firewall atau EarthStun obstacle yang menghalangi
            if (sc.IsCustomObstacle(x, y))
            {
                // Jika itu Firewall, periksa pemiliknya
                string obstaclePlayerColor = sc.GetObstacleOwnerColor(x, y);
                if (obstaclePlayerColor != null && obstaclePlayerColor != player) // Ini adalah Firewall musuh
                {
                    break; // Bidak lurus tidak bisa melewati firewall musuh
                }
                else if (obstaclePlayerColor == null && sc.IsTemporarilyImpassable(x, y)) // Ini EarthStun obstacle
                {
                    break; // EarthStun obstacle menghalangi semua bidak lurus
                }
                // Jika firewall milik sendiri, bidak bisa terus bergerak melewatinya (tidak ada break)
            }

            GameObject cp = sc.GetPosition(x, y);
            if (cp == null)
            {
                result.Add(new Vector2Int(x, y));
            }
            else
            {
                // Cek apakah target serangan berada di atas firewall musuh atau EarthStun obstacle
                if (cp.GetComponent<Chessman>().player != player) // Jika target adalah bidak musuh
                {
                    if (sc.IsCustomObstacle(x, y))
                    {
                        string obstacleOwner = sc.GetObstacleOwnerColor(x, y);
                        if (obstacleOwner != null && obstacleOwner != player) // Firewall musuh
                        {
                            break; // Tidak bisa menyerang bidak di atas firewall musuh
                        }
                        else if (obstacleOwner == null && sc.IsTemporarilyImpassable(x, y)) // EarthStun obstacle
                        {
                            break; // Tidak bisa menyerang bidak di atas EarthStun obstacle
                        }
                    }
                    result.Add(new Vector2Int(x, y)); // Bisa serang jika tidak ada obstacle penghalang
                }
                break; // Bidak lurus berhenti jika ada bidak lain (sendiri atau musuh)
            }

            x += xDir;
            y += yDir;
        }

        return result;
    }


    // Helper method to add a single potential move (for King, Knight, and Pawn diagonal captures)
    private void TryAddRawMove(int dx, int dy, List<Vector2Int> rawMovesList)
    {
        Game sc = Controller.GetComponent<Game>();
        int x = xBoard + dx;
        int y = yBoard + dy;

        if (sc.PositionOnBoard(x, y))
        {
            bool isTargetObstacle = sc.IsCustomObstacle(x, y);
            string obstacleOwnerColor = isTargetObstacle ? sc.GetObstacleOwnerColor(x, y) : null;
            bool isEarthObstacle = sc.IsTemporarilyImpassable(x, y); // Periksa EarthStun obstacle

            if (isTargetObstacle) // Jika posisi target adalah Firewall atau EarthStun obstacle
            {
                if (this.name == player + "_knight")
                {
                    // Kuda bisa melompati kedua jenis obstacle, tapi TIDAK BISA menempati keduanya
                    return;
                }
                else // Bidak lain (Raja, Pion)
                {
                    if (obstacleOwnerColor != null && obstacleOwnerColor != player) // Ini adalah Firewall musuh
                    {
                        return; // TIDAK BISA bergerak ke/menempati firewall musuh
                    }
                    else if (isEarthObstacle) // Ini EarthStun obstacle (selalu menghalangi)
                    {
                        return; // TIDAK BISA bergerak ke/menempati EarthStun obstacle
                    }
                    // Jika itu Firewall milik sendiri, bisa menempati (logika berlanjut)
                }
            }

            GameObject cp = sc.GetPosition(x, y);
            // Perkuat logika untuk menyerang bidak di atas obstacle musuh
            if (cp != null && cp.GetComponent<Chessman>().player != player) // Jika target adalah bidak musuh
            {
                if (isTargetObstacle)
                {
                    if (obstacleOwnerColor != null && obstacleOwnerColor != player) // Firewall musuh
                    {
                        return; // Tidak bisa menyerang bidak di atas firewall musuh
                    }
                    else if (isEarthObstacle) // EarthStun obstacle
                    {
                        return; // Tidak bisa menyerang bidak di atas EarthStun obstacle
                    }
                }
                rawMovesList.Add(new Vector2Int(x, y)); // Bisa serang jika tidak ada obstacle penghalang
            }
            else if (cp == null && !isTargetObstacle) // Jika petak kosong dan bukan obstacle (Firewall/EarthStun)
            {
                rawMovesList.Add(new Vector2Int(x, y));
            }
            else if (cp == null && isTargetObstacle && obstacleOwnerColor == player) // Jika petak kosong dan ada Firewall sendiri
            {
                rawMovesList.Add(new Vector2Int(x, y)); // Bisa menempati firewall sendiri
            }
            // else if (cp == null && isTargetObstacle && isEarthObstacle) // Jika petak kosong dan ada EarthStun obstacle
            // {
            //     // Tidak ditambahkan karena EarthStun obstacle menghalangi semua bidak
            // }
        }
    }

    public void MovePlateSpawn(int matrixX, int matrixY)
    {
        float x = matrixX;
        float y = matrixY;

        x *= 1.1f;
        y *= 1.1f;

        x += -3.85f;
        y += -3.85f;

        GameObject mp = Instantiate(movePlate, new Vector3(x, y, -3.0f), Quaternion.identity);

        MovePlate mpScript = mp.GetComponent<MovePlate>();
        mpScript.SetReference(gameObject);
        mpScript.SetCoords(matrixX, matrixY);
    }

    public void MovePlateAttactSpawn(int matrixX, int matrixY)
    {
        float x = matrixX;
        float y = matrixY;

        x *= 1.1f;
        y *= 1.1f;

        x += -3.85f;
        y += -3.85f;

        GameObject mp = Instantiate(movePlate, new Vector3(x, y, -3.0f), Quaternion.identity);

        MovePlate mpScript = mp.GetComponent<MovePlate>();
        mpScript.attack = true;
        mpScript.SetReference(gameObject);
        mpScript.SetCoords(matrixX, matrixY);
    }

    private bool IsKingInCheckAfterMove(int oldX, int oldY, int newX, int newY)
    {
        Game gameController = Controller.GetComponent<Game>();

        GameObject originalPieceAtNewPos = gameController.GetPosition(newX, newY);
        bool originalPieceActive = false;
        if (originalPieceAtNewPos != null)
        {
            originalPieceActive = originalPieceAtNewPos.activeSelf;
            originalPieceAtNewPos.SetActive(false);
        }

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
            kingInCheck = true;
        }

        gameController.SetPositionEmpty(newX, newY);
        xBoard = originalXBoard;
        yBoard = originalYBoard;
        gameController.SetPosition(this.gameObject);

        if (originalPieceAtNewPos != null)
        {
            originalPieceAtNewPos.SetActive(originalPieceActive);
            gameController.SetPosition(originalPieceAtNewPos);
        }

        return kingInCheck;
    }

    public List<Vector2Int> GetPotentialMoves()
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
                    TryAddRawMove(dx, dy, rawMoves); // Kuda menggunakan TryAddRawMove
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
                            TryAddRawMove(dx, dy, rawMoves); // Raja menggunakan TryAddRawMove
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

                // Gerakan maju 1 petak
                int forwardOneX = xBoard;
                int forwardOneY = yBoard + dir;
                if (sc.PositionOnBoard(forwardOneX, forwardOneY) && sc.GetPosition(forwardOneX, forwardOneY) == null)
                {
                    if (sc.IsCustomObstacle(forwardOneX, forwardOneY))
                    {
                        string obstacleOwner = sc.GetObstacleOwnerColor(forwardOneX, forwardOneY);
                        if (obstacleOwner == player)
                        {
                            rawMoves.Add(new Vector2Int(forwardOneX, forwardOneY));
                        }
                    }
                    else
                    {
                        rawMoves.Add(new Vector2Int(forwardOneX, forwardOneY));
                    }

                    // Gerakan maju 2 petak (hanya dari baris awal)
                    int forwardTwoY = yBoard + dir * 2;
                    if (yBoard == startRow && sc.PositionOnBoard(forwardOneX, forwardTwoY) && sc.GetPosition(forwardOneX, forwardTwoY) == null)
                    {
                        // Pion tidak bisa melompati obstacle (meskipun milik sendiri)
                        // Periksa petak pertama dan petak kedua.
                        bool obstacleInFirstStep = sc.IsCustomObstacle(forwardOneX, forwardOneY) && sc.GetObstacleOwnerColor(forwardOneX, forwardOneY) != player;
                        bool obstacleInSecondStep = sc.IsCustomObstacle(forwardOneX, forwardTwoY) && sc.GetObstacleOwnerColor(forwardOneX, forwardTwoY) != player;

                        if (!obstacleInFirstStep && !obstacleInSecondStep)
                        {
                            rawMoves.Add(new Vector2Int(forwardOneX, forwardTwoY));
                        }
                    }
                }

                // Serangan diagonal (Pion hanya bisa menyerang bidak musuh)
                // Ini sekarang akan memeriksa apakah bidak musuh berada di atas firewall musuh.
                int[] attackDx = { 1, -1 };
                foreach (int dx in attackDx)
                {
                    int targetX = xBoard + dx;
                    int targetY = yBoard + dir;

                    if (sc.PositionOnBoard(targetX, targetY))
                    {
                        GameObject cp = sc.GetPosition(targetX, targetY);
                        if (cp != null && cp.GetComponent<Chessman>().player != player)
                        {
                            // --- PERBAIKAN: Pion tidak bisa menyerang bidak di atas firewall musuh ---
                            if (sc.IsCustomObstacle(targetX, targetY) && sc.GetObstacleOwnerColor(targetX, targetY) != player)
                            {
                                // Jika ada firewall musuh di bawah bidak yang diserang, tidak bisa serang
                                continue; // Skip serangan ini
                            }
                            // --- AKHIR PERBAIKAN ---
                            rawMoves.Add(new Vector2Int(targetX, targetY));
                        }
                    }
                }
                break;
        }
        return rawMoves;
    }


    public List<Vector2Int> GetLegalMoves()
    {
        List<Vector2Int> potentialMoves = GetPotentialMoves();
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