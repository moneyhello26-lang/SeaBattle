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
    public partial class BoardControl : UserControl
    {

        public event Action<int, int> OnCellClick;

        public BoardControl()
        {
            InitializeComponent();
        }

        private void Grid_Click(object sender, MouseButtonEventArgs e)
        {
            var element = e.OriginalSource as FrameworkElement;
            if (element == null) return;

            int row = Grid.GetRow(element);
            int col = Grid.GetColumn(element);

            OnCellClick?.Invoke(row, col);
        }
    }
}
