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

    }
}
