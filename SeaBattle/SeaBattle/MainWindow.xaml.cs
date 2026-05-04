using System;
using System.Linq;
using System.Windows;

namespace SeaBattle
{
    public partial class MainWindow : Window
    {
        private UdpServer _server;
        private UdpClient _client;
        private GameState _state = new GameState();

        private bool _placementMode = true;
        private bool _placementHorizontal = true;
        private bool _opponentReady = false;

        public MainWindow()
        {
            InitializeComponent();

            // подписываемся на клики досок
            boardPlayer.OnCellClick += BoardPlayer_OnCellClick;
            boardPlayer.OnCellRightClick += BoardPlayer_OnCellRightClick;
            boardEnemy.OnCellClick += BoardEnemy_OnCellClick;

            InitializePlacementUI();
        }

        private void InitializePlacementUI()
        {
            UpdateShipList();
            statusPanel.AddLog("Разместите корабли. ЛКМ — поставить, ПКМ — удалить.");
        }

        private void UpdateShipList()
        {
            lstShips.Items.Clear();
            var remaining = _state.GetRemainingShipSizes();
            foreach (var s in remaining)
                lstShips.Items.Add($"{s}-палубный");
            if (lstShips.Items.Count > 0) lstShips.SelectedIndex = 0;
        }

        private int GetSelectedShipSize()
        {
            if (lstShips.SelectedItem != null)
            {
                var text = lstShips.SelectedItem.ToString();
                var digits = new string(text.Where(char.IsDigit).ToArray());
                if (int.TryParse(digits, out int size)) return size;
            }

            var rem = _state.GetRemainingShipSizes();
            return rem.Count > 0 ? rem[0] : 1;
        }

        private void BoardPlayer_OnCellClick(int row, int col)
        {
            if (!_placementMode)
            {
                statusPanel.AddLog("Размещение завершено.");
                return;
            }

            int size = GetSelectedShipSize();
            if (_state.TryPlaceShip(size, row, col, _placementHorizontal))
            {
                RenderPlayerBoard();
                UpdateShipList();
                statusPanel.AddLog($"Поставлен {size}-палубный корабль в ({row},{col}) {(_placementHorizontal ? "гор." : "вер.")}");

                if (_state.IsPlacementComplete())
                    statusPanel.AddLog("Все корабли размещены. Нажмите 'Готов'."); 
            }
            else
            {
                statusPanel.AddLog("Нельзя разместить корабль в этой позиции.");
            }
        }

        private void BoardPlayer_OnCellRightClick(int row, int col)
        {
            if (_state.RemoveShipAt(row, col))
            {
                RenderPlayerBoard();
                UpdateShipList();
                statusPanel.AddLog($"Удалён корабль в ({row},{col})");
            }
            else
            {
                statusPanel.AddLog("В этой клетке нет вашего корабля.");
            }
        }

        private void BtnRotate_Click(object sender, RoutedEventArgs e)
        {
            _placementHorizontal = !_placementHorizontal;
            btnRotate.Content = _placementHorizontal ? "Ориентация: Гор." : "Ориентация: Вер.";
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            _state.ClearPlacement();
            RenderPlayerBoard();
            UpdateShipList();
            statusPanel.AddLog("Размещение очищено.");
        }

        private void BtnReady_Click(object sender, RoutedEventArgs e)
        {
            if (!_state.IsPlacementComplete())
            {
                statusPanel.AddLog("Разместите все корабли перед тем как нажать 'Готов'.");
                return;
            }

            _placementMode = false;
            _state.IsReady = true;
            statusPanel.AddLog("Вы готовы. Ожидание противника...");
            Send(GameProtocol.READY);
        }

        private void CreateServer_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new RunServer();
            if (dlg.ShowDialog() != true) return;

            _state.MyName = dlg.UserName;

            _server = new UdpServer();
            _server.OnMessageReceived += ProcessMessage;
            _server.Start(dlg.Port);

            _state.IsMyTurn = true;
            statusPanel.SetTurn(true);
            statusPanel.AddLog("Сервер запущен. Ожидание подключения...");
        }

        private void ConnectToServer_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new RunClient();
            if (dlg.ShowDialog() != true) return;
            _state.MyName = dlg.NickName;
            _client = new UdpClient();
            _client.OnMessageReceived += ProcessMessage;
            _client.Connect(dlg.ServerIP, dlg.Port);

            _state.IsMyTurn = false;
            statusPanel.SetTurn(false);
            statusPanel.AddLog("Подключено. Разместите корабли и нажмите 'Готов'.");

        }

        private void ProcessMessage(string[] parts)
        {
            Dispatcher.Invoke(() =>
            {
                switch (parts[0])
                {
                    case GameProtocol.READY: HandleReady(); break;
                    case GameProtocol.SHOT: HandleShot(parts); break;
                    case GameProtocol.RESULT: HandleResult(parts); break;
                    case GameProtocol.WIN: HandleWin(); break;
                }
            });
        }

        private void Send(string msg)
        {
            _server?.Send(msg);
            _client?.Send(msg);
        }

        private void HandleReady()
        {
            _opponentReady = true;
            statusPanel.AddLog($"{_state.OpponentName} готов.");

            if (_state.IsReady && _opponentReady)
            {
                statusPanel.AddLog("Оба игрока готовы! Игра начинается.");
            }
            else
            {
                statusPanel.AddLog("Ожидание готовности другой стороны...");
            }
        }

        private void HandleShot(string[] parts)
        {
            int row = int.Parse(parts[1]);
            int col = int.Parse(parts[2]);

            statusPanel.AddLog($"{_state.OpponentName} стреляет в ({row}, {col})");

            var (isHit, isKill) = _state.ProcessOpponentShot(row, col);

            boardPlayer.UpdateCell(row, col, _state.MyField[row, col]);

            string result = isKill ? "kill" : isHit ? "hit" : "miss";
            statusPanel.AddLog($"Результат: {result}");

            Send($"{GameProtocol.RESULT};{row};{col};{result}");

            if (!isHit)
            {
                _state.IsMyTurn = true;
                statusPanel.SetTurn(true);
                statusPanel.AddLog("Ваш ход!");
            }

            if (AllShipsDestroyed(_state.MyField))
            {
                Send(GameProtocol.WIN);
                HandleWin();
            }
        }

        private void HandleResult(string[] parts)
        {
            int row = int.Parse(parts[1]);
            int col = int.Parse(parts[2]);
            string res = parts[3];

            statusPanel.AddLog($"Результат выстрела в ({row}, {col}): {res}");

            if (res == "hit" || res == "kill")
            {
                boardEnemy.UpdateCell(row, col, CellState.Hit);
                _state.IsMyTurn = true;
                statusPanel.SetTurn(true);
            }
                
            else if (res == "miss")
            {
                boardEnemy.UpdateCell(row, col, CellState.Miss);
                _state.IsMyTurn = false;
                statusPanel.SetTurn(false);
                statusPanel.AddLog("Ход противника");
            }

            if (AllShipsDestroyed(_state.OpponentField))
            {
                statusPanel.AddLog("Вы победили!");
                MessageBox.Show("Вы победили!", "SeaBattle");
                _state.IsGameOver = true;
            }
        }

        private void HandleWin()
        {
            statusPanel.AddLog($"Игра окончена! Противник победил.");
            MessageBox.Show("Вы проиграли!", "SeaBattle");
            _state.IsGameOver = true;
        }

        private void BoardEnemy_OnCellClick(int row, int col)
        {
            if (!_state.IsReady || !_opponentReady)
            {
                statusPanel.AddLog("Ожидается готовность обоих игроков.");
                return;
            }

            if (!_state.IsMyTurn || _state.IsGameOver)
            {
                statusPanel.AddLog("Это не ваш ход!");
                return;
            }

            var (isHit, isKill) = _state.Shoot(row, col);

            boardEnemy.UpdateCell(row, col, _state.OpponentField[row, col]);

            statusPanel.AddLog($"Выстрел в ({row}, {col})");

            Send($"{GameProtocol.SHOT};{row};{col}");

            if (!isHit)
            {
                _state.IsMyTurn = false;
                statusPanel.SetTurn(false);
            }
        }

        private void RenderPlayerBoard()
        {
            for (int i = 0; i < GameProtocol.GRID_SIZE; i++)
            {
                for (int j = 0; j < GameProtocol.GRID_SIZE; j++)
                {
                    boardPlayer.UpdateCell(i, j, _state.MyField[i, j]);
                }
            }
        }

        private bool AllShipsDestroyed(CellState[,] field)
        {
            for (int i = 0; i < GameProtocol.GRID_SIZE; i++)
            {
                for (int j = 0; j < GameProtocol.GRID_SIZE; j++)
                {
                    if (field[i, j] == CellState.Ship)
                        return false;
                }
            }
            return true;
        }
    }
}
