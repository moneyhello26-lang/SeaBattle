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
using System.Windows.Shapes;

namespace SeaBattle
{
    /// <summary>
    /// Логика взаимодействия для RunClient.xaml
    /// </summary>
    public partial class RunClient : Window
    {
        public string NickName { get; private set; }
        public string ServerIP {  get; private set; }
        public int Port { get; private set; }
        public RunClient()
        {
            InitializeComponent();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void TextBox_TextChanged_1(object sender, TextChangedEventArgs e)
        {

        }

        private void TextBox_TextChanged_2(object sender, TextChangedEventArgs e)
        {

        }

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            NickName = txtName.Text;
            ServerIP = txtIP.Text;
            Port = int.Parse(txtPORT.Text);
            DialogResult = true;
        }
    }
}
