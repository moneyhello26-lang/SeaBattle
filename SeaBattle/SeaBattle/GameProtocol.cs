using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SeaBattle
{
    internal class GameProtocol
    {
        public const string READY = "ready";
        public const string SHOT = "shot";
        public const string RESULT = "result";
        public const string WIN = "win";
        

        public const int GRID_SIZE = 10;
        public const int PORT = 12345;
    }
}