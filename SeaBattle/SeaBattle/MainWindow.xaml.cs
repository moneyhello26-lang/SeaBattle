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
        private bool _gameStarted = false;

        public MainWindow()
        {
            InitializeComponent();

            boardPlayer.OnCellClick += BoardPlayer_OnCellClick;
            boardEnemy.OnCellClick += BoardEnemy_OnCellClick;

            InitializePlacementUI();
        }

        private void InitializePlacementUI()
        {
            UpdateShipList();
            SetPlacementControlsEnabled(true);
            statusPanel.AddLog("Разместите корабли. ЛКМ — поставить");
        }

        private void SetPlacementControlsEnabled(bool enabled)
        {
            lstShips.IsEnabled = enabled;
            btnRotate.IsEnabled = enabled;
            btnClear.IsEnabled = enabled;
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
                statusPanel.AddLog($"Поставлен {size}-палубный корабль в ({row},{col}) {(_placementHorizontal ? "гор." : "верт.")}");

                if (_state.IsPlacementComplete())
                    statusPanel.AddLog("Все корабли размещены. Нажмите 'Готов'.");        
            }
            else
            {
                statusPanel.AddLog("Нельзя разместить корабль в этой позиции.");
            }
        }

        private void BtnRotate_Click(object sender, RoutedEventArgs e)
        {
            _placementHorizontal = !_placementHorizontal;
            btnRotate.Content = _placementHorizontal ? "Ориентация: Гор." : "Ориентация: Вер.";
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            if (!btnClear.IsEnabled) return;

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

            SetPlacementControlsEnabled(false);
            btnReady.IsEnabled = false;

            statusPanel.AddLog("Вы готовы. Ожидание противника...");

            Send(GameProtocol.FormatMessage(GameProtocol.READY, _state.MyName));
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
                try
                {
                    if (parts == null || parts.Length == 0) return;

                    var cmd = parts[0].Trim().ToLowerInvariant();

                    if (cmd == GameProtocol.READY)
                        HandleReady(parts);
                    else if (cmd == GameProtocol.START_GAME)
                        HandleStartGame();
                    else if (cmd == GameProtocol.SHOT)
                        HandleShot(parts);
                    else if (cmd == GameProtocol.RESULT)
                        HandleResult(parts);
                    else if (cmd == GameProtocol.WIN)
                        HandleWin();
                    else
                        statusPanel.AddLog($"Неизвестная команда: {parts[0]}");
                }
                catch (Exception ex)
                {
                    statusPanel.AddLog($"Ошибка при обработке сообщения: {ex.Message}");
                }
            });
        }

        private void Send(string msg)
        {
            try
            {
                _server?.Send(msg);
                _client?.Send(msg);
            }
            catch (Exception ex)
            {
                statusPanel.AddLog($"Ошибка при отправке: {ex.Message}");
            }
        }

        private void HandleReady(string[] parts)
        {
            if (_opponentReady) return;

            _opponentReady = true;

            if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]))
                _state.OpponentName = parts[1];

            statusPanel.AddLog($"{_state.OpponentName} готов.");

            if (_state.IsReady && _opponentReady && _server != null)
            {
                Send(GameProtocol.FormatMessage(GameProtocol.START_GAME));
                StartGameSession();
            }
            else
            {
                statusPanel.AddLog("Ожидание готовности другой стороны...");
            }
        }

        private void HandleStartGame()
        {
            if (_gameStarted) return;
            StartGameSession();
        }

        private void StartGameSession()
        {
            _gameStarted = true;
            _placementMode = false;

            SetPlacementControlsEnabled(false);
            btnReady.IsEnabled = false;

            statusPanel.AddLog("Оба игрока готовы! Игра начинается.");
            statusPanel.SetTurn(_state.IsMyTurn);
            statusPanel.AddLog(_state.IsMyTurn ? "Ваш ход!" : "Ход противника.");
        }

        private void HandleShot(string[] parts)
        {
            if (parts.Length < 3) return;

            int row = int.Parse(parts[1]);
            int col = int.Parse(parts[2]);

            statusPanel.AddLog($"{_state.OpponentName} стреляет в ({row}, {col})");

            var (isHit, isKill) = _state.ProcessOpponentShot(row, col);
            boardPlayer.UpdateCell(row, col, _state.MyField[row, col]);

            string result = isKill ? "kill" : isHit ? "hit" : "miss";
            statusPanel.AddLog($"Результат: {result}");

            Send(GameProtocol.FormatMessage(GameProtocol.RESULT, row.ToString(), col.ToString(), result));

            if (!isHit)
            {
                _state.IsMyTurn = true;
                statusPanel.SetTurn(true);
                statusPanel.AddLog("Ваш ход!");
            }

            if (AllShipsDestroyed(_state.MyField))
            {
                Send(GameProtocol.FormatMessage(GameProtocol.WIN));
                HandleWin();
            }
        }

        private void HandleResult(string[] parts)
        {
            if (parts.Length < 4) return;

            int row = int.Parse(parts[1]);
            int col = int.Parse(parts[2]);
            string res = parts[3];

            statusPanel.AddLog($"Результат выстрела в ({row}, {col}): {res}");

            _state.ApplyOpponentResult(row, col, res);

            if (res == "hit" || res == "kill")
            {
                boardEnemy.UpdateCell(row, col, CellState.Hit);
                statusPanel.AddLog("Попадание!");
                _state.IsMyTurn = true;
                statusPanel.SetTurn(true);
            }
            else
            {
                boardEnemy.UpdateCell(row, col, CellState.Miss);
                _state.IsMyTurn = false;
                statusPanel.SetTurn(false);
                statusPanel.AddLog("Промах. Ход противника.");
            }

            boardEnemy.IsEnabled = !_state.IsGameOver;

            if (_state.IsOpponentDefeated())
            {
                statusPanel.AddLog("Вы победили!");
                Send(GameProtocol.FormatMessage(GameProtocol.WIN));
                MessageBox.Show("Вы победили!", "SeaBattle");
                _state.IsGameOver = true;
                boardEnemy.IsEnabled = false;
            }
        }

        private void HandleWin()
        {
            statusPanel.AddLog($"Игра окончена! Противник победил.");
            MessageBox.Show("Вы проиграли!", "SeaBattle");
            _state.IsGameOver = true;
            boardEnemy.IsEnabled = false;
        }

        private void BoardEnemy_OnCellClick(int row, int col)
        {
            if (!_gameStarted)
            {
                statusPanel.AddLog("Игра ещё не началась.");
                return;
            }

            if (!_state.IsMyTurn || _state.IsGameOver)
            {
                statusPanel.AddLog("Это не ваш ход!");
                return;
            }

            if (_state.OpponentField[row, col] == CellState.Hit || _state.OpponentField[row, col] == CellState.Miss)
            {
                statusPanel.AddLog("Вы уже стреляли в эту клетку.");
                return;
            }

            Send(GameProtocol.FormatMessage(GameProtocol.SHOT, row.ToString(), col.ToString()));
            statusPanel.AddLog($"Выстрел отправлен в ({row}, {col}). Ожидаем результат...");

            boardEnemy.IsEnabled = false;
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
