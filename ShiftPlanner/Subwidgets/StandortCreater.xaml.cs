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

namespace ShiftPlanner.Subwidgets
{
    /// <summary>
    /// Interaction logic for StandortCreater.xaml
    /// </summary>
    public partial class StandortCreater : Window
    {
        public StandortCreater()
        {
            InitializeComponent();
        }

        //Basicly a Delegate Event
        public event Action<string>? Erstellt;

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameInput.Text))
            {
                MessageBox.Show("Bitte gib einen Namen ein.");
                return;
            }
            Erstellt?.Invoke(NameInput.Text);
            DialogResult = true;
            Close();

        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
