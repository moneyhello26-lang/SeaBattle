using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaBattle
{
    public enum CellState
    {
        Empty,
        Ship,
        Hit,
        Miss
    }

    internal class GameState
    {
        public CellState[,] MyField = new CellState[GameProtocol.GRID_SIZE, GameProtocol.GRID_SIZE];
        public CellState[,] OpponentField = new CellState[GameProtocol.GRID_SIZE, GameProtocol.GRID_SIZE];

        public bool IsMyTurn = false;
        public bool IsGameOver = false;
        public bool IsReady = false;

        public string OpponentName = "Opponent";
        public string MyName = "Player";

        public int HitsOnOpponent = 0;
        public const int TotalShipCells = 20;

        private int[] shipSizes = { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };
        private List<(int row, int col)>[] ships = new List<(int, int)>[10];

        public GameState()
        {
            InitializeField();
            InitializeShips();
        }

        private void InitializeField()
        {
            for (int i = 0; i < GameProtocol.GRID_SIZE; i++)
            {
                for (int j = 0; j < GameProtocol.GRID_SIZE; j++)
                {
                    MyField[i, j] = CellState.Empty;
                    OpponentField[i, j] = CellState.Empty;
                }
            }
        }

        private void InitializeShips()
        {
            for (int i = 0; i < ships.Length; i++)
            {
                ships[i] = new List<(int, int)>();
            }
        }

        public bool PlaceShipsAutomatically()
        {
            InitializeField();
            InitializeShips();
            Random random = new Random();

            for (int shipIndex = 0; shipIndex < shipSizes.Length; shipIndex++)
            {
                int size = shipSizes[shipIndex];
                bool placed = false;

                for (int attempts = 0; attempts < 100; attempts++)
                {
                    bool isHorizontal = random.Next(2) == 0;
                    int row = random.Next(GameProtocol.GRID_SIZE);
                    int col = random.Next(GameProtocol.GRID_SIZE);

                    if (CanPlaceShip(row, col, size, isHorizontal))
                    {
                        PlaceShip(shipIndex, row, col, size, isHorizontal);
                        placed = true;
                        break;
                    }
                }

                if (!placed)
                    return false;
            }

            return true;
        }

        private bool CanPlaceShip(int row, int col, int size, bool isHorizontal)
        {
            if (isHorizontal)
            {
                if (col + size > GameProtocol.GRID_SIZE)
                    return false;

                for (int c = col; c < col + size; c++)
                {
                    if (MyField[row, c] == CellState.Ship)
                        return false;
                }

                for (int r = Math.Max(0, row - 1); r <= Math.Min(GameProtocol.GRID_SIZE - 1, row + 1); r++)
                {
                    for (int c = Math.Max(0, col - 1); c <= Math.Min(GameProtocol.GRID_SIZE - 1, col + size); c++)
                    {
                        if (MyField[r, c] == CellState.Ship && (r != row || c < col || c >= col + size))
                            return false;
                    }
                }
            }
            else
            {
                if (row + size > GameProtocol.GRID_SIZE)
                    return false;

                for (int r = row; r < row + size; r++)
                {
                    if (MyField[r, col] == CellState.Ship)
                        return false;
                }

                for (int r = Math.Max(0, row - 1); r <= Math.Min(GameProtocol.GRID_SIZE - 1, row + size); r++)
                {
                    for (int c = Math.Max(0, col - 1); c <= Math.Min(GameProtocol.GRID_SIZE - 1, col + 1); c++)
                    {
                        if (MyField[r, c] == CellState.Ship && (c != col || r < row || r >= row + size))
                            return false;
                    }
                }
            }

            return true;
        }

        private void PlaceShip(int shipIndex, int row, int col, int size, bool isHorizontal)
        {
            if (isHorizontal)
            {
                for (int c = col; c < col + size; c++)
                {
                    MyField[row, c] = CellState.Ship;
                    ships[shipIndex].Add((row, c));
                }
            }
            else
            {
                for (int r = row; r < row + size; r++)
                {
                    MyField[r, col] = CellState.Ship;
                    ships[shipIndex].Add((r, col));
                }
            }
        }

        public bool TryPlaceShip(int size, int row, int col, bool isHorizontal)
        {
            if (!CanPlaceShip(row, col, size, isHorizontal))
                return false;

            for (int i = 0; i < shipSizes.Length; i++)
            {
                if (shipSizes[i] == size && ships[i].Count == 0)
                {
                    PlaceShip(i, row, col, size, isHorizontal);
                    return true;
                }
            }

            return false;
        }

        public bool RemoveShipAt(int row, int col)
        {
            for (int i = 0; i < ships.Length; i++)
            {
                var list = ships[i];
                if (list.Count == 0) continue;
                foreach (var cell in list)
                {
                    if (cell.row == row && cell.col == col)
                    {
                        foreach (var c in list)
                            MyField[c.row, c.col] = CellState.Empty;
                        list.Clear();
                        return true;
                    }
                }
            }
            return false;
        }

        public List<int> GetRemainingShipSizes()
        {
            var res = new List<int>();
            for (int i = 0; i < shipSizes.Length; i++)
            {
                if (ships[i].Count == 0)
                    res.Add(shipSizes[i]);
            }
            return res;
        }

        public bool IsPlacementComplete()
        {
            for (int i = 0; i < ships.Length; i++)
                if (ships[i].Count == 0) return false;
            return true;
        }

        public void ClearPlacement()
        {
            InitializeField();
            InitializeShips();
            HitsOnOpponent = 0;
        }

        public void ApplyOpponentResult(int row, int col, string result)
        {
            if (row < 0 || row >= GameProtocol.GRID_SIZE || col < 0 || col >= GameProtocol.GRID_SIZE)
                return;

            if (result == "hit" || result == "kill")
            {
                if (OpponentField[row, col] != CellState.Hit)
                {
                    OpponentField[row, col] = CellState.Hit;
                    HitsOnOpponent++;
                }
            }
            else if (result == "miss")
            {
                OpponentField[row, col] = CellState.Miss;
            }
        }

        public bool IsOpponentDefeated()
        {
            return HitsOnOpponent >= TotalShipCells;
        }

        public (bool isHit, bool isKill) Shoot(int row, int col)
        {
            if (row < 0 || row >= GameProtocol.GRID_SIZE || col < 0 || col >= GameProtocol.GRID_SIZE)
                return (false, false);

            if (OpponentField[row, col] == CellState.Hit || OpponentField[row, col] == CellState.Miss)
                return (false, false);

            bool isHit = OpponentField[row, col] == CellState.Ship;

            if (isHit)
                OpponentField[row, col] = CellState.Hit;
            else
                OpponentField[row, col] = CellState.Miss;

            bool isKill = isHit && IsShipDestroyed(row, col);

            return (isHit, isKill);
        }

        private bool IsShipDestroyed(int row, int col)
        {
            int[][] directions = new int[][] {
                new int[] { -1, 0 },
                new int[] { 1, 0 },
                new int[] { 0, -1 },
                new int[] { 0, 1 }
            };

            List<(int, int)> shipCells = new List<(int, int)> { (row, col) };

            foreach (var dir in directions)
            {
                int newRow = row + dir[0];
                int newCol = col + dir[1];

                while (newRow >= 0 && newRow < GameProtocol.GRID_SIZE &&
                       newCol >= 0 && newCol < GameProtocol.GRID_SIZE)
                {
                    if (OpponentField[newRow, newCol] == CellState.Ship)
                        return false;

                    if (OpponentField[newRow, newCol] == CellState.Hit)
                        shipCells.Add((newRow, newCol));
                    else
                        break;

                    newRow += dir[0];
                    newCol += dir[1];
                }
            }

            return true;
        }

        public (bool isHit, bool isKill) ProcessOpponentShot(int row, int col)
        {
            if (row < 0 || row >= GameProtocol.GRID_SIZE || col < 0 || col >= GameProtocol.GRID_SIZE)
                return (false, false);

            bool isHit = MyField[row, col] == CellState.Ship;

            if (isHit)
                MyField[row, col] = CellState.Hit;
            else if (MyField[row, col] == CellState.Empty)
                MyField[row, col] = CellState.Miss;
            else
                return (false, false);

            bool isKill = isHit && IsOwnShipDestroyed(row, col);

            return (isHit, isKill);
        }

        private bool IsOwnShipDestroyed(int row, int col)
        {
            int[][] directions = new int[][] {
                new int[] { -1, 0 },
                new int[] { 1, 0 },
                new int[] { 0, -1 },
                new int[] { 0, 1 }
            };

            foreach (var dir in directions)
            {
                int newRow = row + dir[0];
                int newCol = col + dir[1];

                while (newRow >= 0 && newRow < GameProtocol.GRID_SIZE &&
                       newCol >= 0 && newCol < GameProtocol.GRID_SIZE)
                {
                    if (MyField[newRow, newCol] == CellState.Ship)
                        return false;

                    if (MyField[newRow, newCol] != CellState.Hit)
                        break;

                    newRow += dir[0];
                    newCol += dir[1];
                }
            }

            return true;
        }
    }
}