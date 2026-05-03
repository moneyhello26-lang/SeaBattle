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
            BuildGrid();
        }

        private void BuildGrid()
        {
            for (int i = 0; i < GameProtocol.GRID_SIZE * GameProtocol.GRID_SIZE; i++)
            {
                var rect = new Rectangle
                {
                    Fill = Brushes.LightBlue,
                    Stroke = Brushes.Navy,
                    StrokeThickness = 1
                };
                grid.Children.Add(rect);
            }
        }

        public void UpdateCell(int row, int col, CellState state)
        {
            int index = row * GameProtocol.GRID_SIZE + col;
            if (index >= grid.Children.Count) return;

            var rect = grid.Children[index] as Rectangle;
            if (rect == null) return;

            if (state == CellState.Ship)
                rect.Fill = Brushes.Gray;
            else if (state == CellState.Hit)
                rect.Fill = Brushes.Red;
            else if (state == CellState.Miss)
                rect.Fill = Brushes.White;
            else
                rect.Fill = Brushes.LightBlue;
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
