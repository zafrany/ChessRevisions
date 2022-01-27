class Game
{
    int turn;
    int winner;
    bool draw;
    int movesWithNoCaptureOrPawnMovement;

    ChessBoard board;
    public Game()
    {
        this.movesWithNoCaptureOrPawnMovement = 0;
        this.draw = false;
        this.winner = 0;
        this.turn = 1;
        this.board = new ChessBoard();
        this.board.setBoard();
        updateAllLegalMoves();
        board.legalMovesUnion();
    }
    public Game(string boardString, int turn)
    {
        this.movesWithNoCaptureOrPawnMovement = 0;
        this.draw = false;
        this.winner = 0;
        this.turn = turn;
        this.board = new ChessBoard();
        this.board.setBoard(boardString);
        updateAllLegalMoves();
        board.legalMovesUnion();
    }
    public void startGame()
    {
        string command = "";
        int[] locations;
        string message;
        board.updateBoardState(0);
        board.printBoardSymbols();

        while (this.winner == 0 && !draw)
        {
            locations = new int[0];
            message = string.Format("{0} player please enter a move, \"ff\" to surrender, or \"od\" to offer draw:", this.turn == -1 ? "Black" : "White");
            while (locations.Length == 0 && winner == 0 && !draw)
            {
                Console.WriteLine(message);
                command = Console.ReadLine().ToLower().Trim();
                if (command == "ff")
                    winner = this.turn * -1;
                else if (command == "od")
                {
                    Console.WriteLine("{0} player offers a draw, accept?: (y/n)", this.turn == -1 ? "Black" : "White");
                    command = Console.ReadLine().ToLower().Trim();
                    if (command == "y")
                        draw = true;
                }
                else
                    locations = parseCommand(command);
            }
            if (command != "ff" && !draw)
                playTurn(locations[0], locations[1]);
        }
        if (!draw)
            Console.WriteLine("{0} player is the winner!", winner == -1 ? "Black" : "White");
        else
            Console.WriteLine("The match is a draw!");
    }
    public ChessBoard GetBoard()
    {
        return this.board;
    }
    public int[] parseCommand(string command)
    {
        int[] result = new int[2];
        command = (command.Replace(" ", "")).Trim().ToUpper();
        if (command.Length != 4)
            return new int[0];
        else
        {
            for (int i = 0; i < 2; i++)
            {
                if (command[i * 2] > 'H' || command[i * 2] < 'A')
                    return new int[0];
                else if (command[i * 2 + 1] > '8' || command[i * 2 + 1] < '1')
                    return new int[0];

                result[i] = (command[i * 2] - 'A') + 8 * (command[i * 2 + 1] - '1');
            }
        }
        return result;
    }
    public void updateAllLegalMoves()
    {
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                this.board.pieceAtLocation(i * 8 + j).setLegalMoves();
            }
        }
    }
    void updateGameState()
    {
        updateAllLegalMoves();
        board.legalMovesUnion();
        updateAllLegalMoves();
    } //for every piece updates all attacked squares and then updates all attacked squares per player
    public void playTurn(int startingLocation, int endLocation)
    {
        string errorMessage = "";
        bool legalKingMovement = true;
        int flag; //used to determine if we need to clear game stats(game log) and 50 move counter
        ChessPiece startingLocationPiece = this.board.pieceAtLocation(startingLocation);
        ChessPiece endLocationPiece = this.board.pieceAtLocation(endLocation);

        if (board.pieceAtLocation(startingLocation).getColor() == turn)
        {
            if (board.pieceAtLocation(endLocation).getColor() != 0 || board.pieceAtLocation(startingLocation).getType() == "P")
                flag = 1;
            else
                flag = 0;

            if (this.board.pieceAtLocation(startingLocation).getType() == "K")
                legalKingMovement = verifyMove(startingLocation, endLocation, turn);

            if (!draw && legalKingMovement && board.pieceAtLocation(startingLocation).move(startingLocation, endLocation))
            {
                updateGameState();
                if (this.board.getLegalMovesUnion(turn * -1).Contains(this.board.getKingLocation(turn))) //check for pins - not the prettiest of things but less computationaly heavy
                {
                    board.setPieceAtLocation(startingLocation, startingLocationPiece, true);
                    board.setPieceAtLocation(endLocation, endLocationPiece, true);
                    updateGameState();
                    Console.WriteLine("Illegal move, doing that puts king in check or dose not resolve existing check!");
                }

                else
                {
                    if (this.board.pieceAtLocation(endLocation).getType() == "P")
                        if ((endLocation < 8 && this.board.pieceAtLocation(endLocation).getColor() == 1) || (endLocation > 55 && this.board.pieceAtLocation(endLocation).getColor() == -1))
                            doPromotion(endLocation, this.turn);

                    updateMoveCount(flag);
                    this.turn *= -1;

                    if (this.board.getLegalMovesUnion(turn * -1).Contains(this.board.getKingLocation(turn)))
                    {
                        if (isMateOrStalemate(this.turn))
                        {
                            winner = turn * -1;
                            Console.WriteLine("Checkmate on {0} player!", winner == 1 ? "Black" : "White");
                        }
                        else
                            Console.WriteLine("{0} player is checked!", this.turn == -1 ? "Black" : "White");
                    }
                    else
                        this.draw = isMateOrStalemate(this.turn); //check for stalemate
                }
                Console.WriteLine();
                board.printBoardSymbols();
                if (!draw) //check for other possibles draw states
                    this.draw = isDraw();
            }
            else
                errorMessage = "Illegal move!";
        }

        else if (board.pieceAtLocation(startingLocation).getColor() == 0)
            errorMessage = "Illegal move, cant move an empty space!";
        else
            errorMessage = "Illegal move, cant move oponnents pieces";
        Console.WriteLine(errorMessage);
    }
    bool isDraw()
    {
        int repetitions;
        string[] gameLog = this.board.getBoardStates();

        for (int i = 0; i < gameLog.Length; i++)
        {
            repetitions = 0;
            for (int j = 0; j < gameLog.Length; j++)
            {
                if (i != j && gameLog[i] == gameLog[j])
                {
                    repetitions++;
                    if (repetitions == 2)
                        return true;
                }
            }
        }
        if (board.isDeadState() || returnMoveCount() == 50)
            return true;
        return false;
    }
    public bool isMateOrStalemate(int player)
    {
        int[] legalMovesArray;
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (board.pieceAtLocation(i * 8 + j).getColor() == player)
                {
                    legalMovesArray = board.pieceAtLocation(i * 8 + j).getLegalMoves();
                    for (int k = 0; k < legalMovesArray.Length; k++)
                    {
                        if (verifyMove(i * 8 + j, legalMovesArray[k], turn))
                            return false;
                    }
                }
            }
        }
        return true;
    }
    public bool verifyMove(int startingLocation, int endLocation, int color) //costly function, only use for moving king, checking mate condition
    {
        ChessPiece startingLocationPiece = board.pieceAtLocation(startingLocation);
        ChessPiece endLocationPiece = board.pieceAtLocation(endLocation);

        board.setPieceAtLocation(endLocation, startingLocationPiece, true);
        board.setPieceAtLocation(startingLocation, new EmptySquare(startingLocation, board), true);
        if (startingLocationPiece.getType() == "K")
            board.setKingLocation(color, endLocation);
        updateGameState();

        bool result = !this.board.getLegalMovesUnion(color * -1).Contains(this.board.getKingLocation(color));

        board.setPieceAtLocation(startingLocation, startingLocationPiece, true);
        board.setPieceAtLocation(endLocation, endLocationPiece, true);
        updateGameState();
        if (startingLocationPiece.getType() == "K")
            board.setKingLocation(color, startingLocation);
        return result;
    }
    void doPromotion(int location, int color)
    {
        bool legalInput = false;
        string promotionType;

        while (!legalInput)
        {
            Console.WriteLine("Choose piece to promote to: (Q/N/R/B)");
            promotionType = Console.ReadLine().Trim().ToUpper();
            if (promotionType.Length != 1)
            {
                Console.WriteLine("Illegal input, type only 1 letter to represent desired piece.");
            }

            else
            {
                switch (promotionType)
                {
                    case "Q":
                        board.setPieceAtLocation(location, new Queen(color, location, this.board), false);
                        legalInput = true;
                        break;
                    case "N":
                        board.setPieceAtLocation(location, new Knight(color, location, this.board), false);
                        legalInput = true;
                        break;
                    case "R":
                        board.setPieceAtLocation(location, new Rook(color, location, this.board), false);
                        legalInput = true;
                        break;
                    case "B":
                        board.setPieceAtLocation(location, new Bishop(color, location, this.board), false);
                        legalInput = true;
                        break;
                    default:
                        Console.WriteLine("Illegal input, type only 1 letter to represent desired piece.");
                        break;
                }
            }
        }
        updateGameState();
    }
    public void updateMoveCount(int flag) // flag 1 = zero counter, flag 0 = increment;
    {
        if (flag == 1)
            movesWithNoCaptureOrPawnMovement = 0;
        else
            movesWithNoCaptureOrPawnMovement++;
    }
    public int returnMoveCount()
    {
        return this.movesWithNoCaptureOrPawnMovement;
    }
}
class ChessBoard
{
    int whiteKingLocation;
    int blackKingLocation;
    ChessPiece[,] chessBoard;
    int[][] piecesAttackingLocations; //index 0 = black, index 1 = white
    string[] gameStates;
    int[] bishopCount = { 0, 0 }; //[0] = black, [1] = white
    int[] pieceCount = { 0, 0 };  //[0] = black, [1] = white - counts how many pieces there are that are not bishops

    public ChessBoard()
    {
        this.chessBoard = new ChessPiece[8, 8];
        this.blackKingLocation = 4;
        this.whiteKingLocation = 60;
        piecesAttackingLocations = new int[2][];
        piecesAttackingLocations[0] = new int[0];
        piecesAttackingLocations[1] = new int[0];
        gameStates = new string[0];
    }
    public void setKingLocation(int color, int newKingLocation)
    {
        if (color == -1)
            this.blackKingLocation = newKingLocation;
        else
            this.whiteKingLocation = newKingLocation;
    }
    public int getKingLocation(int color)
    {
        if (color == -1)
            return this.blackKingLocation;
        else
            return this.whiteKingLocation;
    }
    public void legalMovesUnion() //to calculate all attacked squares by each player we must do a union of all legal moves of same player (informatio also needed for king movement/check/mate)
    {
        this.piecesAttackingLocations[0] = new int[0];
        this.piecesAttackingLocations[1] = new int[0];
        int index;
        for (int i = 0; i < 8; ++i)
        {
            for (int j = 0; j < 8; ++j)
            {
                if (chessBoard[i, j].getColor() == 0)
                    continue;
                else
                {
                    index = (chessBoard[i, j].getColor()) == -1 ? 0 : 1;
                    if (chessBoard[i, j].getType() != "P")
                        piecesAttackingLocations[index] = piecesAttackingLocations[index].Union(chessBoard[i, j].getLegalMoves()).ToArray();
                    else
                    {
                        Pawn currentPawn = (Pawn)chessBoard[i, j];
                        piecesAttackingLocations[index] = piecesAttackingLocations[index].Union(currentPawn.getPawnAttacks()).ToArray();
                    }
                }
            }
        }
    }
    public int[] getLegalMovesUnion(int color)
    {
        int index = color == -1 ? 0 : 1;
        return piecesAttackingLocations[index];
    }
    public void setBoard()
    {
        int color;
        int location = 0;

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++, location++)
            {
                if (i == 0 || i == 1)
                    color = -1;
                else if (i == 6 || i == 7)
                    color = 1;
                else
                    color = 0;

                if (color != 0)
                {
                    if (i < 1 || i > 6)
                    {
                        if (j == 0 || j == 7)
                        {
                            chessBoard[i, j] = new Rook(color, location, this);
                        }

                        else if (j == 1 || j == 6)
                        {
                            chessBoard[i, j] = new Knight(color, location, this);
                        }

                        else if (j == 2 || j == 5)
                        {
                            chessBoard[i, j] = new Bishop(color, location, this);
                        }

                        else if (j == 3)
                        {
                            chessBoard[i, j] = new Queen(color, location, this);
                        }

                        else if (j == 4)
                        {
                            chessBoard[i, j] = new King(color, location, this);
                        }
                    }

                    else
                        chessBoard[i, j] = new Pawn(color, location, this);
                }

                else
                    chessBoard[i, j] = new EmptySquare(location, this);
            }
        }
    }
    public void printBoardSymbols()
    {
        string space;
        string boardTop = "   A  B  C  D  E  F  G  H \n" +
                          "  -------------------------";
        string boardBot = "  -------------------------";

        Console.WriteLine(boardTop);

        for (int i = 0; i < 8; i++)
        {
            Console.Write((i + 1) + " |");

            for (int j = 0; j < 8; j++)
            {
                space = " ";
                if (chessBoard[i, j].getColor() == -1)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    if (chessBoard[i, j].getType() == "P")
                        space = "";
                }
                if (chessBoard[i, j].ToString() != "  ")
                    Console.Write(chessBoard[i, j].ToStringSymbol() + space);
                else Console.Write(chessBoard[i, j]);

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("|");
            }
            Console.WriteLine();
            Console.WriteLine(boardBot);
        }
        Console.WriteLine();
    }
    public ChessPiece pieceAtLocation(int location)
    {
        return chessBoard[location / 8, location % 8];
    }
    public void setPieceAtLocation(int location, ChessPiece piece, bool verifyFlag)
    {
        piece.setLocation(location);
        if (!verifyFlag) //in case were only verifying moves we dont want to set that a piece have moved
            piece.setFirstMove();
        chessBoard[location / 8, location % 8] = piece;
    }
    public void updateBoardState(int flag) //flag 0 = dont clear array and add new state, flag 1 = clear array before adding new state - used for tracking 3-fold repetitions
    {
        string[] currentState = { this.ToString() };
        if (flag == 0)
            gameStates = gameStates.Append(currentState[0]).ToArray();
        else
            gameStates = currentState;
    }
    public string[] getBoardStates()
    {
        return this.gameStates;
    }
    public override string ToString()
    {
        string result = "";
        string castlingString = "";
        string enPassantString = "";
        bool[] castlingStatus;
        bishopCount[0] = 0;
        bishopCount[1] = 0;
        pieceCount[0] = 0;
        pieceCount[1] = 0;

        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                if (chessBoard[i, j].getType() == "K")
                {
                    castlingString += chessBoard[i, j].getColor() == -1 ? "BB" : "WW";
                    castlingStatus = ((King)chessBoard[i, j]).getCasling();
                    for (int k = 0; k < 2; k++)
                    {
                        castlingString += castlingStatus[k] == false ? "NC" : "CP";
                    }
                }

                if (chessBoard[i, j].getType() == "P")
                {
                    if (((Pawn)chessBoard[i, j]).getEnPassant() != -1)
                        enPassantString += chessBoard[i, j].getLocation() + "" + ((Pawn)chessBoard[i, j]).getEnPassant();
                }

                if (chessBoard[i, j].getType() != "K")
                {
                    if (chessBoard[i, j].getType() == "B")
                        if (chessBoard[i, j].getColor() == -1)
                            bishopCount[0]++;
                        else
                            bishopCount[1]++;

                    else
                    {
                        if (chessBoard[i, j].getColor() == -1)
                            pieceCount[0]++;
                        else
                            pieceCount[1]++;
                    }
                }
                result += chessBoard[i, j].getColor() == 0 ? "EE," : chessBoard[i, j] + ",";
            }
        result += castlingString + enPassantString;
        return result;
    }
    public bool isDeadState()
    {
        if (bishopCount[0] < 2 && bishopCount[1] < 2 && pieceCount[0] == 1 && pieceCount[1] == 1)
            return true;
        else return false;
    }
    public void setBoard(string gameString)
    {
        int color;
        string[] tokens = gameString.Trim().Split(',');
        for (int i = 0; i < 64; i++)
        {
            if (tokens[i][0] == 'B')
                color = -1;
            else if (tokens[i][0] == 'W')
                color = 1;
            else color = 0;

            switch (tokens[i][1])
            {
                case 'R':
                    this.chessBoard[i / 8, i % 8] = new Rook(color, i, this);
                    break;
                case 'N':
                    this.chessBoard[i / 8, i % 8] = new Knight(color, i, this);
                    break;
                case 'B':
                    this.chessBoard[i / 8, i % 8] = new Bishop(color, i, this);
                    break;
                case 'Q':
                    this.chessBoard[i / 8, i % 8] = new Queen(color, i, this);
                    break;
                case 'K':
                    this.chessBoard[i / 8, i % 8] = new King(color, i, this);
                    this.setKingLocation(color, i);
                    break;
                case 'P':
                    this.chessBoard[i / 8, i % 8] = new Pawn(color, i, this);
                    this.chessBoard[i / 8, i % 8].setFirstMove();
                    break;
                case 'E':
                    this.chessBoard[i / 8, i % 8] = new EmptySquare(i, this);
                    break;
            }
        }
    }
}
abstract class ChessPiece
{
    string type;/*R/Q/B/N/P/K/E(mpty)*/
    int color; /*B = -1/W = 1/N = 0*/
    int location; /*coded location*/
    int[] possibleMoveDirections;
    int[] legalMoves;
    bool firstMove = true;
    ChessBoard board;

    public ChessPiece(int color, int location, int[] possibleMoveDirections, ChessBoard board)
    {
        this.color = color;
        this.location = location;
        this.possibleMoveDirections = possibleMoveDirections;
        this.board = board;
    }
    public void setType(string type)
    {
        this.type = type;
    }
    public void setLegalMoves() //calculate all possible moves for said piece and set them to the legalMoves array.
    {
        this.legalMoves = legalMove(this.location);
    }
    public int[] getLegalMoves()
    {
        return this.legalMoves;
    } //return all legal moves piece can make
    public abstract int[] legalMove(int location);
    public bool getFirstMove()
    {
        return firstMove;
    }
    public void setFirstMove()
    {
        firstMove = false;
    }
    public string getType()
    {
        return this.type;
    }
    public int getColor()
    {
        return this.color;
    }
    public int getLocation()
    {
        return this.location;
    }
    public void setLocation(int location)
    {
        this.location = location;
    }
    public int[] getPossibleMoveDirections()
    {
        return this.possibleMoveDirections;
    }
    public ChessBoard getBoard()
    {
        return this.board;
    }
    public override string ToString()
    {
        if (color == 0)
            return "  ";
        else if (color == -1)
            return (String.Format("B" + type));
        else
            return ("W" + type);
    }
    public string ToStringSymbol()
    {
        switch (color)
        {
            case 1:
                switch (type)
                {
                    case "K":
                        return ("\u2654");
                    case "Q":
                        return ("\u2655");
                    case "R":
                        return ("\u2656");
                    case "B":
                        return ("\u2657");
                    case "N":
                        return ("\u2658");
                    case "P":
                        return ("\u2659");
                }
                break;

            case -1:

                switch (type)
                {
                    case "K":
                        return ("\u265A");
                    case "Q":
                        return ("\u265B");
                    case "R":
                        return ("\u265C");
                    case "B":
                        return ("\u265D");
                    case "N":
                        return ("\u265E");
                    case "P":
                        return ("\u265F");
                }
                break;
        }
        return "";
    }
    void performCastling(int location, int destination)
    {
        if (destination - location == 2) //castling right
        {
            board.setPieceAtLocation(destination - 1, board.pieceAtLocation(location + 3), false);
            board.setPieceAtLocation(location + 3, new EmptySquare(location + 3, board), false);
        }

        if (destination - location == -2) //castling left
        {
            board.setPieceAtLocation(destination + 1, board.pieceAtLocation(location - 4), false);
            board.setPieceAtLocation(location - 4, new EmptySquare(location - 4, board), false);
        }
    }
    void removeEnPassantOptions(int destination) //remove all current en passant options (besides those set this turn) as they are no longer possible
    {
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
                if (board.pieceAtLocation(i * 8 + j).getType() == "P")
                    if (((Pawn)board.pieceAtLocation(i * 8 + j)).getEnPassant() != destination)
                        ((Pawn)board.pieceAtLocation(i * 8 + j)).setEnPassant(-1);
    }
    void setEnPassantOption(int destination, bool west, bool east)
    {
        if (west && board.pieceAtLocation(destination - 1).getType() == "P" && board.pieceAtLocation(destination - 1).getColor() != this.getColor())
            ((Pawn)board.pieceAtLocation(destination - 1)).setEnPassant(destination);
        if (east && board.pieceAtLocation(destination + 1).getType() == "P" && board.pieceAtLocation(destination + 1).getColor() != this.getColor())
            ((Pawn)board.pieceAtLocation(destination + 1)).setEnPassant(destination);
    }
    public bool move(int location, int destination)
    {
        int flag = 0; //flag to determine if we need to clear game logs(threefold repetition) or reset 50 move counter, 0 = dont clear, 1 = clear
        if (this.legalMoves.Contains(destination))
        {
            if (board.pieceAtLocation(destination).getColor() != 0 || board.pieceAtLocation(location).getType() == "P")
                flag = 1;

            if (this.type == "K")
            {
                this.board.setKingLocation(this.getColor(), destination);
                if (Math.Abs(destination - location) == 2)
                    performCastling(location, destination);
            }

            if (this.type == "P")
            {
                if (board.pieceAtLocation(destination).getType() == "E" && Math.Abs(destination - location) != 16 && Math.Abs(destination - location) != 8)
                {
                    int enPassantLocation = ((Pawn)board.pieceAtLocation(location)).getEnPassant();
                    board.setPieceAtLocation(enPassantLocation, new EmptySquare(enPassantLocation, board), false);
                }

                if (Math.Abs(destination - location) == 16)
                {
                    int west = destination % 8; //west
                    int east = 7 - destination % 8; //east
                    //set new en passant options
                    setEnPassantOption(destination, west > 0, east > 0);
                }
            }
            removeEnPassantOptions(destination);
            board.setPieceAtLocation(destination, this, false);
            board.setPieceAtLocation(location, new EmptySquare(location, board), false);
            //update game state to help check for 3-fold repetition and 50 move draw
            board.updateBoardState(flag);
            return true;
        }
        else return false;
    }
    public string advance(int startLocation, int direction, int maxAllowedInDirection)
    {
        string result = "";
        bool stop = false;
        int color = this.getColor();
        int muiltiplier = 1;

        while (!stop)
        {
            if (startLocation + direction > 63 || startLocation + direction < 0 || maxAllowedInDirection == 0)
                stop = true;
            else
            {
                if (this.getBoard().pieceAtLocation(startLocation + direction * muiltiplier).getColor() != color)
                {
                    result += (startLocation + direction * muiltiplier) + " ";
                    if (this.getBoard().pieceAtLocation(startLocation + direction * muiltiplier).getColor() != 0)
                        stop = true;
                    maxAllowedInDirection -= 1;
                    muiltiplier++;
                }
                else
                    stop = true;
            }
        }

        return result;
    }
}
class EmptySquare : ChessPiece
{
    public EmptySquare(int location, ChessBoard board) : base(0, location, new int[] { 0 }, board)
    {
        setType("E");
    }
    public override int[] legalMove(int location)
    {
        return new int[0];
    }
}
class Rook : ChessPiece
{
    public Rook(int color, int location, ChessBoard board) : base(color, location, new int[] { -8, -1, 1, 8 }, board)
    {
        setType("R");
    }
    public override int[] legalMove(int location)
    {
        int[] possibleDirections = this.getPossibleMoveDirections();
        int color = this.getColor();
        ChessBoard board = this.getBoard();
        int[] enemyAttackedSquares = board.getLegalMovesUnion(color * -1);
        string legalMovesString = "";

        int[] possibleMaxDirections = new int[4];
        int north = location / 8; //north
        int west = location % 8; //west
        int south = 7 - location / 8; //south
        int east = 7 - location % 8; //east
        possibleMaxDirections[0] = north;
        possibleMaxDirections[1] = west;
        possibleMaxDirections[2] = east;
        possibleMaxDirections[3] = south;


        for (int i = 0; i < possibleDirections.Length; i++)
            legalMovesString += advance(location, possibleDirections[i], possibleMaxDirections[i]);

        if (legalMovesString.Trim() == "")
            return new int[0];

        string[] legalMovesArray = (legalMovesString.Trim()).Split(" ");
        int[] legalMovesLocations = new int[legalMovesArray.Length];

        for (int i = 0; i < legalMovesArray.Length; i++)
        {
            legalMovesLocations[i] = int.Parse(legalMovesArray[i]);
        }
        return legalMovesLocations;
    }
}
class Knight : ChessPiece
{
    public Knight(int color, int location, ChessBoard board) : base(color, location, new int[] { -17, -15, -10, -6, 6, 10, 15, 17 }, board)
    {
        setType("N");
    }
    public override int[] legalMove(int location)
    {
        string legalMovesString = "";
        int[] possibleDirections = this.getPossibleMoveDirections();
        int color = this.getColor();
        ChessBoard board = this.getBoard();
        int[][] forbiddenMoves = new int[8][];

        forbiddenMoves[0] = new int[] { -17, -15, -6 - 10 }; //row 1
        forbiddenMoves[1] = new int[] { -17, -15 };          //row 2
        forbiddenMoves[2] = new int[] { 17, 15 };            //row 7
        forbiddenMoves[3] = new int[] { 17, 15, 6, 10 };     //row 8

        forbiddenMoves[4] = new int[] { -17, 15, 6, -10 };   //col A
        forbiddenMoves[5] = new int[] { -10, 6 };            //col B
        forbiddenMoves[6] = new int[] { -6, 10 };            //col G
        forbiddenMoves[7] = new int[] { 17, -15, -6, 10 };   //col H

        int col = (location % 8) + 1;
        int row = (location / 8) + 1;

        switch (row)
        {
            case 1:
                possibleDirections = possibleDirections.Where(item => !(forbiddenMoves[0].Contains(item))).ToArray();
                break;
            case 2:
                possibleDirections = possibleDirections.Where(item => !(forbiddenMoves[1].Contains(item))).ToArray();
                break;
            case 7:
                possibleDirections = possibleDirections.Where(item => !(forbiddenMoves[2].Contains(item))).ToArray();
                break;
            case 8:
                possibleDirections = possibleDirections.Where(item => !(forbiddenMoves[3].Contains(item))).ToArray();
                break;
        }

        switch (col)
        {
            case 1:
                possibleDirections = possibleDirections.Where(item => !(forbiddenMoves[4].Contains(item))).ToArray();
                break;
            case 2:
                possibleDirections = possibleDirections.Where(item => !(forbiddenMoves[5].Contains(item))).ToArray();
                break;
            case 7:
                possibleDirections = possibleDirections.Where(item => !(forbiddenMoves[6].Contains(item))).ToArray();
                break;
            case 8:
                possibleDirections = possibleDirections.Where(item => !(forbiddenMoves[7].Contains(item))).ToArray();
                break;
        }

        for (int i = 0; i < possibleDirections.Length; i++)
        {
            if ((location + possibleDirections[i]) < 64 && (location + possibleDirections[i] > -1))
            {
                if (board.pieceAtLocation(location + possibleDirections[i]).getType() == "E" || board.pieceAtLocation(location + possibleDirections[i]).getColor() != color)
                    legalMovesString += (location + possibleDirections[i]) + " ";
            }
        }

        if (legalMovesString.Trim() == "")
            return new int[0];

        string[] legalMovesArray = (legalMovesString.Trim()).Split(" ");
        int[] legalMovesLocations = new int[legalMovesArray.Length];

        for (int i = 0; i < legalMovesArray.Length; i++)
        {
            legalMovesLocations[i] = int.Parse(legalMovesArray[i]);
        }
        return legalMovesLocations;
    }
}
class Bishop : ChessPiece
{
    public Bishop(int color, int location, ChessBoard board) : base(color, location, new int[] { -9, -7, 7, 9 }, board)
    {
        setType("B");
    }
    public override int[] legalMove(int location)
    {
        int[] possibleDirections = this.getPossibleMoveDirections();
        int color = this.getColor();
        ChessBoard board = this.getBoard();
        int[] enemyAttackedSquares = board.getLegalMovesUnion(color * -1);
        string legalMovesString = "";

        int[] possibleMaxDirections = new int[4];
        int north = location / 8; //north
        int west = location % 8; //west
        int south = 7 - location / 8; //south
        int east = 7 - location % 8; //east
        possibleMaxDirections[0] = Math.Min(north, west); //northwest
        possibleMaxDirections[1] = Math.Min(north, east); //northeast;        
        possibleMaxDirections[2] = Math.Min(south, west); //southwest
        possibleMaxDirections[3] = Math.Min(south, east); //southeast


        for (int i = 0; i < possibleDirections.Length; i++)
            legalMovesString += advance(location, possibleDirections[i], possibleMaxDirections[i]);

        if (legalMovesString.Trim() == "")
            return new int[0];

        string[] legalMovesArray = (legalMovesString.Trim()).Split(" ");
        int[] legalMovesLocations = new int[legalMovesArray.Length];

        for (int i = 0; i < legalMovesArray.Length; i++)
        {
            legalMovesLocations[i] = int.Parse(legalMovesArray[i]);
        }
        return legalMovesLocations;
    }
}
class Queen : ChessPiece
{
    public Queen(int color, int location, ChessBoard board) : base(color, location, new int[] { -9, -8, -7, -1, 1, 7, 8, 9 }, board)
    {
        setType("Q");
    }
    public override int[] legalMove(int location)
    {
        int[] possibleDirections = this.getPossibleMoveDirections();
        int color = this.getColor();
        ChessBoard board = this.getBoard();
        int[] enemyAttackedSquares = board.getLegalMovesUnion(color * -1);
        string legalMovesString = "";
        int[] possibleMaxDirections = new int[8];
        possibleMaxDirections[1] = location / 8; //north
        possibleMaxDirections[3] = location % 8; //west
        possibleMaxDirections[6] = 7 - location / 8; //south
        possibleMaxDirections[4] = 7 - location % 8; //east
        possibleMaxDirections[0] = Math.Min(possibleMaxDirections[1], possibleMaxDirections[3]); //northwest
        possibleMaxDirections[2] = Math.Min(possibleMaxDirections[1], possibleMaxDirections[4]); //northeast;        
        possibleMaxDirections[5] = Math.Min(possibleMaxDirections[6], possibleMaxDirections[3]); //southwest
        possibleMaxDirections[7] = Math.Min(possibleMaxDirections[6], possibleMaxDirections[4]); //southeast

        for (int i = 0; i < possibleDirections.Length; i++)
            legalMovesString += advance(location, possibleDirections[i], possibleMaxDirections[i]);

        if (legalMovesString.Trim() == "")
            return new int[0];

        string[] legalMovesArray = (legalMovesString.Trim()).Split(" ");
        int[] legalMovesLocations = new int[legalMovesArray.Length];

        for (int i = 0; i < legalMovesArray.Length; i++)
            legalMovesLocations[i] = int.Parse(legalMovesArray[i]);
        return legalMovesLocations;
    }
}
class King : ChessPiece
{
    bool[] castlingPossible; //index 0 - from left, index 1 - from right
    public King(int color, int location, ChessBoard board) : base(color, location, new int[] { -9, -8, -7, -1, 1, 7, 8, 9 }, board)
    {
        castlingPossible = new bool[] { true, true };
        setType("K");
    }

    void setCastling()
    {
        if (this.getLocation() == 4 || this.getLocation() == 60)
        {
            castlingPossible[0] = this.getFirstMove() && this.getBoard().pieceAtLocation(this.getLocation() - 4).getFirstMove(); //left side castling
            castlingPossible[1] = this.getFirstMove() && this.getBoard().pieceAtLocation(this.getLocation() + 3).getFirstMove(); //right side castling
        }
        else
        {
            castlingPossible[0] = false;
            castlingPossible[1] = false;
        }
    }

    public bool[] getCasling()
    {
        return castlingPossible;
    }

    public override int[] legalMove(int location)
    {
        int[] possibleDirections = this.getPossibleMoveDirections();
        int color = this.getColor();
        ChessBoard board = this.getBoard();
        int[] enemyAttackedSquares = board.getLegalMovesUnion(color * -1);
        string legalMovesString = "";
        bool inCheck = board.getLegalMovesUnion(color * -1).Contains(board.getKingLocation(color));

        setCastling();
        int north = location / 8; //north
        int west = location % 8; //west
        int south = 7 - location / 8; //south
        int east = 7 - location % 8; //east

        int[] maxAllowedDiagonal = new int[4];
        maxAllowedDiagonal[0] = Math.Min(north, west); //northwest
        maxAllowedDiagonal[1] = Math.Min(north, east); //northeast
        maxAllowedDiagonal[2] = Math.Min(south, west); //southwest
        maxAllowedDiagonal[3] = Math.Min(south, east); //southeast

        int[] maxAllowedHorizontal = new int[2];
        maxAllowedHorizontal[0] = west;
        maxAllowedHorizontal[1] = east;


        if (this.castlingPossible[0] || castlingPossible[1])
        {
            if (castlingPossible[0] && !inCheck &&
               board.pieceAtLocation(this.getLocation() - 1).getType() == "E" &&
               board.pieceAtLocation(this.getLocation() - 2).getType() == "E" &&
               board.pieceAtLocation(this.getLocation() - 3).getType() == "E" &&
               !board.getLegalMovesUnion(color * -1).Contains((this.getLocation() - 1)) &&
               !board.getLegalMovesUnion(color * -1).Contains((this.getLocation() - 2))
              )
                legalMovesString += (this.getLocation() - 2) + " ";

            if (castlingPossible[1] && !inCheck &&
               board.pieceAtLocation(this.getLocation() + 1).getType() == "E" &&
               board.pieceAtLocation(this.getLocation() + 2).getType() == "E" &&
               !board.getLegalMovesUnion(color * -1).Contains((this.getLocation() + 1)) &&
               !board.getLegalMovesUnion(color * -1).Contains((this.getLocation() + 2))
              )
                legalMovesString += (this.getLocation() + 2) + " ";

        }


        for (int i = 0; i < possibleDirections.Length; i++)
        {
            {
                if (
                    (possibleDirections[i] == -9 && maxAllowedDiagonal[0] > 0) ||
                    (possibleDirections[i] == -7 && maxAllowedDiagonal[1] > 0) ||
                    (possibleDirections[i] == -1 && maxAllowedHorizontal[0] > 0) ||
                    (possibleDirections[i] == 1 && maxAllowedHorizontal[1] > 0) ||
                    (possibleDirections[i] == 7 && maxAllowedDiagonal[2] > 0) ||
                    (possibleDirections[i] == 9 && maxAllowedDiagonal[3] > 0) ||
                    (possibleDirections[i] == 8 && 8 + location < 64) ||
                    (possibleDirections[i] == -8 && -8 + location > -1)
                  )
                {
                    if (
                       (board.pieceAtLocation(location + possibleDirections[i]).getType() == "E" || board.pieceAtLocation(location + possibleDirections[i]).getColor() != color)
                       && (!enemyAttackedSquares.Contains(location + possibleDirections[i]))
                       )
                    {
                        legalMovesString += (location + possibleDirections[i]) + " ";
                    }
                }
            }
        }

        if (legalMovesString.Trim() == "")
            return new int[0];

        string[] legalMovesArray = (legalMovesString.Trim()).Split(" ");
        int[] legalMovesLocations = new int[legalMovesArray.Length];

        for (int i = 0; i < legalMovesArray.Length; i++)
        {
            legalMovesLocations[i] = int.Parse(legalMovesArray[i]);
        }
        return legalMovesLocations;
    }
}
class Pawn : ChessPiece
{
    int[] attackingSpaces; //pawn threath zone different from movement
    int enPassant; //can capture a pawn en passant on this location

    public Pawn(int color, int location, ChessBoard board) : base(color, location, new int[] { 8, 16 }, board)
    {
        setType("P");
        this.attackingSpaces = new int[0];
        this.enPassant = -1;
    }
    public int[] getPawnAttacks()
    {
        return this.attackingSpaces;
    }
    public void setEnPassant(int location)
    {
        this.enPassant = location;
    }
    public int getEnPassant()
    {
        return this.enPassant;
    }
    public override int[] legalMove(int location)
    {
        int north = location / 8; //north
        int west = location % 8; //west
        int south = 7 - location / 8; //south
        int east = 7 - location % 8; //east

        int maxAllowedDiagonalLeft = this.getColor() == -1 ? Math.Min(south, east) : Math.Min(north, west);
        int maxAllowedDiagonalRight = this.getColor() == -1 ? Math.Min(south, west) : Math.Min(north, east);
        string legalMovesString = "";
        string attackingSpacesString = "";

        int[] possibleDirections = this.getPossibleMoveDirections();
        int color = this.getColor();
        ChessBoard board = this.getBoard();

        if (location < 8 || location > 55)
        {
            return new int[0]; //promition to be done
        }

        if (this.getBoard().pieceAtLocation(location + (this.getColor() * -8)).getType() == "E")
        {
            legalMovesString += location + color * 8 * -1 + " ";
            if (this.getFirstMove())
                if (board.pieceAtLocation(location + (color * -1) * 16).getType() == "E")
                    legalMovesString += (location + color * 16 * -1) + " ";
        }

        if (this.getBoard().pieceAtLocation(location + 9 * -1 * color).getColor() == color * -1 && maxAllowedDiagonalLeft > 0) //check if opposing piece is diagonaly right (white prespective)
            legalMovesString += location + color * 9 * -1 + " ";

        if (this.getBoard().pieceAtLocation(location + 7 * -1 * color).getColor() == color * -1 && maxAllowedDiagonalRight > 0) //check if opposing piece is diagonaly left  (white prespective)
            legalMovesString += location + color * 7 * -1 + " ";

        if (maxAllowedDiagonalLeft > 0)
            attackingSpacesString += location + color * 7 * -1 + " ";
        if (maxAllowedDiagonalRight > 0)
            attackingSpacesString += location + color * 9 * -1 + " ";

        string[] attackingSpacesArray = (attackingSpacesString.Trim()).Split(" ");
        this.attackingSpaces = new int[attackingSpacesArray.Length];

        if (attackingSpacesString.Trim() == "") //no possible moves for this pawn
            this.attackingSpaces = new int[0];
        else
            for (int i = 0; i < this.attackingSpaces.Length; i++)
                attackingSpaces[i] = int.Parse(attackingSpacesArray[i]);

        //add an passant option to legal movement
        if (enPassant != -1)
            if (this.getColor() == 1)
                legalMovesString += (this.enPassant - 8);
            else
                legalMovesString += (this.enPassant + 8);

        if (legalMovesString.Trim() == "") //no possible moves for this pawn
            return new int[0];

        string[] legalMovesArray = (legalMovesString.Trim()).Split(" ");
        int[] legalMovesLocations = new int[legalMovesArray.Length];

        for (int i = 0; i < legalMovesArray.Length; i++)
        {
            legalMovesLocations[i] = int.Parse(legalMovesArray[i]);
        }
        return legalMovesLocations;
    }
}
class GameLauncher
{
    public static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.Unicode;

        string boardString =
        "EE,BK,EE,WK,EE,EE,EE,EE," +
        "EE,EE,EE,WQ,EE,EE,EE,EE," +
        "BB,EE,BR,BQ,EE,EE,EE,BR," +
        "EE,EE,EE,EE,EE,EE,EE,EE," +
        "EE,EE,EE,EE,EE,EE,EE,EE," +
        "EE,EE,EE,EE,EE,EE,EE,EE," +
        "EE,EE,EE,EE,EE,BP,EE,EE," +
        "EE,EE,EE,EE,EE,EE,EE,EE";

        Game game = new Game();
        //Game game = new Game(boardString, 1);
        game.startGame();
    }
}