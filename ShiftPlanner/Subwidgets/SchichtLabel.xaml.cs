using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace ShiftPlanner.Subwidgets
{
    /// <summary>
    /// Interaction logic for SchichtLabel.xaml
    /// </summary>
    public partial class SchichtLabel : UserControl
    {
        public SchichtLabel()
        {
            InitializeComponent();
        }

        public int _LinkedSchichtID = 0;

        public int _LinkedOrtID = 0;

        public event Action<int>? StartSchichtEdit;

        private void EditSchicht(object sender, RoutedEventArgs e)
        {
            StartSchichtEdit?.Invoke(_LinkedSchichtID);
        }
    }


    public class ContrastForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                Color c = brush.Color;

                // Perceived brightness
                double brightness =
                    (0.299 * c.R +
                     0.587 * c.G +
                     0.114 * c.B);

                return brightness > 128
                    ? Brushes.Black
                    : Brushes.White;
            }

            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        
            => Brushes.White;
    }



}
