using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SeaBattle
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private UdpServer _server;
        private UdpClient _client;
        private GameState _state = new GameState();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void boardPlayer_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void boardEnemy_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void statusPanel_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CreateServer_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new RunServer();
            if (dlg.ShowDialog() != true) return;

            _state.MyName = dlg.UserName;

            _server = new UdpServer();
            statusPanel.SetTurn(true);
            statusPanel.AddLog("Сервер запущен. Ожидание противника...");
        }
        private void ConnectToServer_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new RunClient();
            if (dlg.ShowDialog() != true) return;
            _state.MyName = dlg.NickName;
            _client = new UdpClient();
            _client.Connect(dlg.ServerIP, dlg.Port);

            _state.IsMyTurn = false;
            statusPanel.SetTurn(false);
            statusPanel.AddLog("Подключение к серверу...");
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

        private void HandleReady()
        {
            statusPanel.AddLog($"{_state.OpponentName} готов!");
        }

        private void HandleShot(string[] parts)
        {
            int row = int.Parse(parts[1]);
            int col = int.Parse(parts[2]);

            statusPanel.AddLog($"{_state.OpponentName} стреляет в ({row}, {col})");
        }

        private void HandleResult(string[] parts)
        {
            int row = int.Parse(parts[1]);
            int col = int.Parse(parts[2]);
            string result = parts[3];

            statusPanel.AddLog($"Результат выстрела в ({row}, {col}): {result}");

            if (result == "miss")
            {
                _state.IsMyTurn = false;
                statusPanel.SetTurn(false);
            }
        }

        private void HandleWin()
        {
            statusPanel.AddLog($"Игра окончена!");
            MessageBox.Show("Победа!", "SeaBattle");
        }
    }
}
