using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Lab5
{
    public enum Marker : byte
    {
        N, X, O
    }

    public class GridChangedArgs : EventArgs
    {
        public int Row { get; set; } // horizontal coordinate base

        public int Col { get; set; } // vertical coordinate base
    }

    public delegate void ChangedGrid(object sender, GridChangedArgs e); // check update event handler
    public delegate void ChangedPlayer(object sender, EventArgs e); // check which player going first handler
    public delegate void StartedGame(object sender, EventArgs e); // check start of game reset handler
    public delegate void GameOver(object sender, EventArgs e); // check if game completed handler

    public class GameEngine
    {
        private Marker[,] Grid;
        private List<WinPath> WinPaths; // private members for gameplay grid/best routes to win

        // Public events
        public event ChangedGrid GridChanged;
        public event ChangedPlayer PlayerChanged; // variables to track events with handlers above
        public event StartedGame GameStarted;
        public event GameOver GameOver;

        public bool GameIsOver { get { return (CurrentPlayer == Marker.N); } } 
        public int GridSize { get; private set; } // 3x3 by default, tested larger grid with algorithm to working success
        public Marker CurrentPlayer { get; private set; }
        public Marker WinningPlayer { get; private set; } // check who won for print string in UI
        public WinPath WinningPath { get; private set; } // allow storage of the best next path

        public GameEngine(Marker initialPlayer = Marker.X)
        {
            GridSize = 3; // default ttt grid
            InitGrid();
            Reset(initialPlayer); // initialization step
        }

        public void Reset(Marker initialPlayer = Marker.X)
        {
            Grid.UpdateEach((p, r, c) => Marker.N); // update all with empty 
            WinningPlayer = Marker.N;
            WinningPath = null;

            CurrentPlayer = initialPlayer;
            FGameStarted(); // player1 as human or bot
            FGridChanged(-1, -1);
            FPlayerChanged();
        }

        public bool CanMove(int row, int col)
        {
            return (!GameIsOver && Grid[row, col] == Marker.N);
        }

        public bool Move(int row, int col)
        {
            if (!CanMove(row, col)) // check if move is legal
                return false;

            Grid[row, col] = CurrentPlayer; // move for best conditions to follow
            FGridChanged(row, col);

            UpdateState();
            if (!GameIsOver) // see if game completed to continue testing
            {
                Debug.Assert(CurrentPlayer != Marker.N);
                CurrentPlayer = CurrentPlayer.GetOtherPlayer(); // switch which player goes first if possible
                FPlayerChanged();
            }
            return true;
        }

        public void AutoMove()
        {
            if (GameIsOver) return;

            Random random = new Random();
            WinPath pathToWin = WinPaths.Where(p => p.MovesToWin(Grid, CurrentPlayer) == 1).Random(random);
            if (pathToWin != null)
            {
                var move = pathToWin.First(m => Grid[m.Row, m.Column] == Marker.N);
                Move(move.Row, move.Column);
                return;
            }

            Marker otherPlayer = CurrentPlayer.GetOtherPlayer(); // check if the win by user is blockable
            WinPath pathToLose = WinPaths.Where(p => p.MovesToWin(Grid, otherPlayer) == 1).Random(random);
            if (pathToLose != null)
            {
                var move = pathToLose.First(m => Grid[m.Row, m.Column] == Marker.N);
                Move(move.Row, move.Column);
                return;
            }

            var movesAvailable = GetMoves(Marker.N);
            Debug.Assert(movesAvailable.Count > 0); // after checking which moves are legal

            foreach (var move in movesAvailable) // make the list for the best moves 
            {
                var grid = (Marker[,])Grid.Clone(); // makes duplicate grid for testing
                grid[move.Row, move.Column] = CurrentPlayer;
                move.UserData = (int)WinPaths.Sum(p => {
                    int count = p.MovesToWin(grid, CurrentPlayer);
                    return (count < 0) ? count : (GridSize - count); // ranks the best paths in best-worst ordering
                });
            }

            int maxScore = movesAvailable.Max(m => m.UserData);
            var mov = movesAvailable.Where(m => m.UserData == maxScore).Random(random); // select the top best move out of those ranked
            Move(mov.Row, mov.Column);
            return;
        }

        public Marker GetLoc(int row, int col)
        {
            return Grid[row, col]; // return the state of the grid markers
        }
        
        private WinPath FindWin(out Marker winningPlayer)
        {
            winningPlayer = Marker.N;
            foreach (WinPath path in WinPaths)
            {
                winningPlayer = path.GetWinningPlayer(Grid);
                if (winningPlayer != Marker.N)
                    return path;
            }
            return null;
        }

        public void SetGridSize(int gridSize, Marker initialPlayer = Marker.X)
        {
            GridSize = gridSize;
            InitGrid();
            Reset(initialPlayer);
        }

        private void InitGrid()
        {
            Grid = new Marker[GridSize, GridSize];

            WinPaths = new List<WinPath>(); // list the ranked grid paths for computer win
            var DownPath = new WinPath();
            var UpPath = new WinPath();
            for (int i = 0; i < GridSize; i++)
            {
                DownPath.Add(new TicTac(i, i));
                UpPath.Add(new TicTac(i, GridSize - i - 1));
            }
            WinPaths.Add(DownPath);
            WinPaths.Add(UpPath);

            for (int row = 0; row < GridSize; row++)
            {
                var path = new WinPath();
                for (int col = 0; col < GridSize; col++)
                    path.Add(new TicTac(row, col));
                WinPaths.Add(path);
            }
            for (int col = 0; col < GridSize; col++)
            {
                var path = new WinPath();
                for (int row = 0; row < GridSize; row++)
                    path.Add(new TicTac(row, col));
                WinPaths.Add(path);
            }
        }

        private void UpdateState()
        {
            if (!GameIsOver)
            {
                Marker winningPlayer;
                WinningPath = FindWin(out winningPlayer);
                if (WinningPath != null)
                {
                    WinningPlayer = winningPlayer; //endgame, winner
                    CurrentPlayer = Marker.N;
                }
                else if (!IsLegal)
                {
                    WinningPlayer = Marker.N;
                    CurrentPlayer = Marker.N; // tie
                }
                if (GameIsOver)
                {
                    FPlayerChanged();
                    FGameOver();
                }
            }
        }

        private List<TicTac> GetMoves(Marker player)
        {
            List<TicTac> moves = new List<TicTac>();
            Grid.ForEach((p, r, c) => { if (p == player) moves.Add(new TicTac(r, c)); });
            return moves;
        }

        private bool IsLegal
        {
            get { return Grid.Any((p, r, c) => (p == Marker.N)); }
        }

        private void FGameStarted()
        {
            if (GameStarted != null)
            {
                var args = new EventArgs();
                GameStarted(this, args);
            }
        }

        private void FGridChanged(int row = -1, int col = -1)
        {
            if (GridChanged != null)
            {
                var args = new GridChangedArgs();
                args.Row = row;
                args.Col = col;
                GridChanged(this, args);
            }
        }

        private void FGameOver()
        {
            if (GameOver != null)
            {
                var args = new EventArgs();
                GameOver(this, args);
            }
        }

        private void FPlayerChanged()
        {
            if (PlayerChanged != null)
            {
                var args = new EventArgs();
                PlayerChanged(this, args);
            }
        }
    }

    public class TicTac
    {
        public int Row { get; set; }
        public int Column { get; set; }
        public int UserData { get; set; }

        public TicTac(int row, int col)
        {
            Row = row; Column = col; UserData = 0;
        }

        public bool IsCorner(int gridSize)
        {
            return (Row == 0 || Row == (gridSize - 1)) && (Column == 0 || Column == (gridSize - 1));
        }
    }

    public class WinPath : List<TicTac>
    {
        public int MovesToWin(Marker[,] grid, Marker player)
        {
            Debug.Assert(player != Marker.N);
            Marker otherPlayer = player.GetOtherPlayer();
            if (this.Any(p => grid[p.Row, p.Column] == otherPlayer))
                return -1;
            return this.Count(p => grid[p.Row, p.Column] == Marker.N); // returns empty cells
        }

        public Marker GetWinningPlayer(Marker[,] grid)
        {
            Debug.Assert(Count > 0);
            Marker player = grid[this[0].Row, this[0].Column];
            if (player != Marker.N && this.All(p => grid[p.Row, p.Column] == player))
                return player;
            return Marker.N;
        }
    }

    public static class GridHelper
    {
        public static int Count<T>(this T[,] grid, Func<T, int, int, bool> predicate) where T : struct
        {
            int count = 0;
            for (int row = grid.GetLowerBound(0); row <= grid.GetUpperBound(0); row++)
                for (int col = grid.GetLowerBound(1); col <= grid.GetUpperBound(1); col++)
                    if (predicate(grid[row, col], row, col))
                        count++;
            return count;
        }
        public static bool Any<T>(this T[,] grid, Func<T, int, int, bool> predicate) where T : struct
        {
            for (int row = grid.GetLowerBound(0); row <= grid.GetUpperBound(0); row++)
                for (int col = grid.GetLowerBound(1); col <= grid.GetUpperBound(1); col++)
                    if (predicate(grid[row, col], row, col))
                        return true;
            return false;
        }

        public static T First<T>(this T[,] grid, Func<T, int, int, bool> predicate) where T : struct
        {
            for (int row = grid.GetLowerBound(0); row <= grid.GetUpperBound(0); row++)
                for (int col = grid.GetLowerBound(1); col <= grid.GetUpperBound(1); col++)
                    if (predicate(grid[row, col], row, col))
                        return grid[row, col];
            return default(T);
        }

        public static T Random<T>(this IEnumerable<T> list, Random random)
        {
            if (list == null || list.Count() == 0)
                return default(T);
            return list.ElementAt(random.Next(list.Count()));
        }

        public static Marker GetOtherPlayer(this Marker player)
        {
            Debug.Assert(player != Marker.N);
            return (player == Marker.X) ? Marker.O : Marker.X;
        }

        public static void ForEach<T>(this T[,] grid, Action<T, int, int> action) where T : struct
        {
            for (int row = grid.GetLowerBound(0); row <= grid.GetUpperBound(0); row++)
                for (int col = grid.GetLowerBound(1); col <= grid.GetUpperBound(1); col++)
                    action(grid[row, col], row, col);
        }

        public static void UpdateEach<T>(this T[,] grid, Func<T, int, int, T> func) where T : struct
        {
            for (int row = grid.GetLowerBound(0); row <= grid.GetUpperBound(0); row++)
                for (int col = grid.GetLowerBound(1); col <= grid.GetUpperBound(1); col++)
                    grid[row, col] = func(grid[row, col], row, col);
        }
    }
}
