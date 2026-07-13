using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
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
using ShiftPlanner.Utility;

namespace ShiftPlanner.Subwidgets
{
    /// <summary>
    /// Interaction logic for DailyShiftsHandle.xaml
    /// </summary>
    public partial class DailyShiftsHandle : UserControl
    {
        public DailyShiftsHandle()
        {
            InitializeComponent();

            
        }

        public DayTemplateData dayTemplateData = new();

        public bool MoveValues()
        {


            if (int.TryParse(AnzahlBox.Text.Trim(), out int Count))
            {
                dayTemplateData.Anzahl = Count;
            }
            else return false;

           
            Anzahl.Text = AnzahlBox.Text.Trim();
            if(RoleSelection.SelectedItem != null)
            {
                string selectedrole = RoleSelection.SelectedItem.ToString()!;
                RText.Text = selectedrole;
                dayTemplateData.SchichtRolle = selectedrole!;
            }else return false;

            if (!UtilityClass.IsValidTimeString(Begin_Box.Text) || !UtilityClass.IsValidTimeString(Ende_Box.Text))
            {
                MessageBox.Show("Bitte gib eine Zugelassene Zeitspanne ein");
                return false;
            }

            dayTemplateData.SchichtSchlussText = Ende_Box.Text;
            dayTemplateData.SchichtStartText = Begin_Box.Text;

            List<string>? DayList = Tagesbox.Text.Split(',', ';').ToList();
            if (DayList != null && DayList.Count > 0)
            {
                string DayStrings = "mo,di,mi,do,fr,sa,so";
                foreach (string Day in DayList)
                {
                    if (string.IsNullOrWhiteSpace(Day)) continue;
                    if (DayStrings.Contains(Day.ToLower().Trim()))
                    {
                        dayTemplateData.TagesListe.Add(Day.ToLower().Trim());
                    }
                    else if (int.TryParse(Day.Trim(), out int OutDayInt) && OutDayInt > 0 && OutDayInt <= 31)
                    {
                        dayTemplateData.TagesListe.Add(Day);
                    }
                    else if (Day.Contains("-"))
                    {
                        string[] parts = Day.Split('-');

                        if (!int.TryParse(parts[0].Trim(), out int start)) return false;
                        if (!int.TryParse(parts[1].Trim(), out int end)) return false;

                        if (start > end)
                        {
                            MessageBox.Show("Bitte gib gültige Tage ein.");
                            Tage.Text = string.Empty;
                            return false;
                        }

                        foreach (int day in Enumerable.Range(start, end - start + 1))
                        {
                            dayTemplateData.TagesListe.Add(day.ToString().Trim());
                        }
                    }
                    else
                    {
                        MessageBox.Show("Bitte gib gültige Tage ein.");
                        Tage.Text = string.Empty;
                        return false;
                    }

                }
            }
            else return false;
            Tage.Text = Tagesbox.Text;

            dayTemplateData.DayString = Tage.Text;
            Tagesbox.Visibility = Visibility.Collapsed;
            Tage.Visibility = Visibility.Visible;
            Tooltip.Visibility = Visibility.Collapsed;
            AnzahlBox.Visibility = Visibility.Collapsed;
            RoleSelection.Visibility = Visibility.Collapsed;
            RText.Visibility = Visibility.Visible;
            Anzahl.Visibility = Visibility.Visible;
            Begin_Box.IsReadOnly = true;
            Ende_Box.IsReadOnly = true; 
            return true;
        }
     
    }


    public class DayTemplateData
    {
        public int Anzahl { get; set; }
        public string SchichtSchlussText { get; set; } = "";
        public string SchichtStartText { get; set; } = "";
        public string SchichtRolle { get; set; } = "";
        public string DayString { get; set; } = "";
        public List<string> TagesListe { get; set; } = new();
        public double DifficultyWeight { get; set; } //lesser is better heigher gets tried first
        public SchichtZeit Zeiten { get; set; } = new();

    }




}
