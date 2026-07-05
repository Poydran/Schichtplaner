using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
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
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace ShiftPlanner.Subwidgets
{
    /// <summary>
    /// Interaction logic for Standorteditor.xaml
    /// </summary>
    public partial class Standorteditor : Window, INotifyPropertyChanged
    {
        public Standorteditor()
        {
            InitializeComponent();

            BundeslaenderWahl.ItemsSource = Enum.GetValues<EBundesland>();

           DataContext = this;
        }

        public int LinkedID = -1;

        public event PropertyChangedEventHandler? PropertyChanged;

        public event Action<StandortSave, int>? SaveSTChanges;

        public event Action<int>? DeleteAllShifts;

        public EBundesland _Bundesland { get; set; }

        public EBundesland Bundesland
        {
            get => _Bundesland;
            set
            {
                if (_Bundesland == value)
                    return;

                _Bundesland = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Bundesland)));
            }
        }

        private void SaveEdit(object sender, RoutedEventArgs e)
        {

            if (string.IsNullOrWhiteSpace(StandortName.Text))
            {
                MessageBox.Show("Bitte gib einen gültigen Namen ein.");
                return;
            }

            if (string.IsNullOrWhiteSpace(StandortName.Text))
            {
                MessageBox.Show("Bitte gib einen gültigen Namen ein.");
                return;
            }

            StandortSave standortSave = new StandortSave();



            List<string>? NewSchliessTage = SchliesstagBox.Text.Split(',',';').ToList();

            if (NewSchliessTage != null)
            {
                string DayStrings = "mo,di,mi,do,fr,sa,so";
                Schliesstage.Text = string.Empty;
                int index = 0;
                foreach(string Day in NewSchliessTage)
                {
                    if (DayStrings.Contains(Day.ToLower()))
                    {
                        Schliesstage.Text += Day;
                        standortSave.NewSchliessTage.Add(Day.ToLower());
                    }
                    else if(int.TryParse(Day,out int OutDayInt) && OutDayInt > 0 && OutDayInt <= 31)
                    {
                        Schliesstage.Text += Day;
                        standortSave.NewSchliessTage.Add(Day);
                    }
                    else if(Day.Contains("-"))
                    {
                        string[] parts = Day.Split('-');

                        int start = int.Parse(parts[0].Trim());
                        int end = int.Parse(parts[1].Trim());

                        if(start > end)
                        {
                            MessageBox.Show("Bitte gib gültige Schließtage ein.");
                            Schliesstage.Text = string.Empty;
                            return;
                        }

                        List<int> numbers = Enumerable
                            .Range(start, end - start + 1)
                            .ToList();

                        foreach(int number in numbers)
                        {
                            Schliesstage.Text += Day;
                            standortSave.NewSchliessTage.Add(number.ToString());
                        }
                    }
                    else
                    {
                        MessageBox.Show("Bitte gib gültige Schließtage ein.");
                        Schliesstage.Text = string.Empty;
                        return;
                    }

                    if (index != 0) { Schliesstage.Text += ", "; }
                    index++;
                }
            }

            standortSave.SchliesstagString = SchliesstagBox.Text;

            standortSave.NewName = StandortNameEdit.Text.Trim();
            StandortName.Text = StandortNameEdit.Text.Trim();

            standortSave.STBundesland = (EBundesland)BundeslaenderWahl.SelectedItem;

            Bundesland = standortSave.STBundesland;
            standortSave.bClosedOnHoliday = CBClosedOnHoliday.IsChecked != null && CBClosedOnHoliday.IsChecked  == true ? true : false;
            standortSave.NewKuerzel = StandortKuerzelEdit.Text.Trim();
            StandortKuerzel.Text = StandortKuerzelEdit.Text.Trim();

            EditButton.Visibility = Visibility.Visible;
            SpeicherButton.Visibility = Visibility.Collapsed;

            StandortName.Visibility = Visibility.Visible;
            StandortNameEdit.Visibility = Visibility.Collapsed;

            Schliesstage.Visibility = Visibility.Visible;
            SchliesstagBox.Visibility = Visibility.Collapsed;

            StandortKuerzel.Visibility = Visibility.Visible;
            StandortKuerzelEdit.Visibility = Visibility.Collapsed;

            DeleteButton.Visibility = Visibility.Collapsed;


            BundeslaenderWahl.Visibility = Visibility.Collapsed;
            BundeslandText.Visibility = Visibility.Visible;
            SchliessTooltip.Visibility = Visibility.Collapsed;

            CBClosedOnHoliday.IsHitTestVisible = false;

            SaveSTChanges?.Invoke(standortSave,LinkedID);
        }

        private void StartEdit(object sender, RoutedEventArgs e)
        {
            EditButton.Visibility = Visibility.Collapsed;
            SpeicherButton.Visibility = Visibility.Visible;

            BundeslandText.Visibility = Visibility.Collapsed;
            BundeslaenderWahl.Visibility = Visibility.Visible;

            StandortName.Visibility = Visibility.Collapsed;
            StandortNameEdit.Visibility = Visibility.Visible;

            Schliesstage.Visibility = Visibility.Collapsed;
            SchliesstagBox.Visibility = Visibility.Visible;
            SchliessTooltip.Visibility = Visibility.Visible;
            StandortKuerzel.Visibility = Visibility.Collapsed;
            StandortKuerzelEdit.Visibility = Visibility.Visible;
            CBClosedOnHoliday.IsHitTestVisible = true;
            DeleteButton.Visibility = Visibility.Visible;
        }
        private void ClearAllShifts(object sender, RoutedEventArgs e)
        {
            DeleteAllShifts?.Invoke(LinkedID);  
        }
    }

    public class StandortSave
    {
        public string NewName { get; set; } = "";
        public string NewKuerzel { get; set; } = "";
        public string SchliesstagString { get; set; } = "";
        public EBundesland STBundesland {get; set;} = new EBundesland();
        public List<string> NewSchliessTage { get; set; } = new();
        public bool bClosedOnHoliday { get; set; } = false;

    }

   
}
