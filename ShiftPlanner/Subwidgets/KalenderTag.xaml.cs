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
    /// Interaction logic for KalenderTag.xaml
    /// </summary>
    public partial class KalenderTag : UserControl
    {
        public KalenderTag()
        {
            InitializeComponent();
        }

        public TagesDaten DayData { get; set; } = new();


        public event Action<KalenderTag>? ClickedDay;

        public event Action<int>? SwitchToWeek;

        private Brush? _colorBrushCache;
        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
         
            if (ActualHeight < 75)
            {
                HolidayText.Visibility = Visibility.Collapsed;
                SchichtText.Visibility = Visibility.Collapsed;
            }
            else
            {
                HolidayText.Visibility = Visibility.Visible;
                SchichtText.Visibility = Visibility.Visible;
            }
        }

        public void SetShiftInfo()
        {

           List<string> RoleStrings = DayData.SchiftMapping.Keys.ToList();

           RoleStrings = RoleStrings.OrderBy(x => x).ToList();

            string NewText = string.Empty;
            int Index = 0;

            foreach (string Role in RoleStrings)
            {
                if (Index != 0) NewText +=  Environment.NewLine;
                Index++;
                NewText += $"{DayData.SchiftMapping[Role]} x {Role}";
            }
            SchichtText.Text = NewText;
        }

        private void CellClick(object sender, MouseButtonEventArgs e)
        {
            //wenn gelickt lass Main Window subscribe too
            ClickedDay?.Invoke(this);
        }
        private void CellRightClick(object sender, MouseButtonEventArgs e)
        {
            //wenn gelickt lass Main Window subscribe too
         
            SwitchToWeek?.Invoke(DayData._KalenderDatum.Day);
        }
        private void Highlight(object sender, MouseEventArgs e)
        {
            //wenn gelickt lass Main Window subscribe too
            if (DayData.NotAvailableForSelectedMA || DayData.NotAvailableForSelection) return;
            _colorBrushCache = RootBorder.Background;
            RootBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2A2A"));
        }

        private void Unhighlight(object sender, MouseEventArgs e)
        {
            //wenn gelickt lass Main Window subscribe too
            if (DayData.NotAvailableForSelectedMA || DayData.NotAvailableForSelection) return;
            RootBorder.Background = _colorBrushCache;
               
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
