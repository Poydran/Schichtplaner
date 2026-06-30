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

namespace ShiftPlanner.Subwidgets
{
    /// <summary>
    /// Interaction logic for WochenTag.xaml
    /// </summary>
    public partial class WochenTag : UserControl
    {
        public WochenTag()
        {
            InitializeComponent();

        }

        public TagesDaten DayData { get; set; } = new();

        public event Action<WochenTag>? ClickedWeekDay;

        private void CellClick(object sender, MouseButtonEventArgs e)
        {
            //wenn gelickt lass Main Window subscribe too
            ClickedWeekDay?.Invoke(this);
        }

        public void SetDayToHoliday(string holidaystring)
        {
            HolidayText.Text = holidaystring;
            RootBorder.BorderBrush = Brushes.Violet;
            HolidayText.Visibility = Visibility.Visible;
            DayData.bIsHoliday = true;

        }


    }

  
}
