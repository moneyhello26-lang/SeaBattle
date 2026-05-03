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
    /// Логика взаимодействия для StatusPanel.xaml
    /// </summary>
    public partial class StatusPanel : UserControl
    {
        public StatusPanel()
        {
            InitializeComponent();
        }

        public void AddLog(string msg)
        {
            lstLog.Items.Add(msg);
            lstLog.ScrollIntoView(lstLog.Items[lstLog.Items.Count - 1]);
        }

        public void SetTurn(bool isMyTurn)
        {
            lblTurn.Content = isMyTurn ? "Ваш ход" : "Ход противника";
        }

        public void ClearLog()
        {
            lstLog.Items.Clear();
        }
    }
}
