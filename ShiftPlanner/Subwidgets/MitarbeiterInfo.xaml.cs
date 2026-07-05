using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for MitarbeiterInfo.xaml
    /// </summary>
    public partial class MitarbeiterInfo : Window
    {
        public MitarbeiterInfo()
        {
            InitializeComponent();
        }

        public int MAID { get; set; }

        public Dictionary<String, int> OrtCache = new();

        public Dictionary<int, int> SchichtTracker = new();

        private List<string> LocationsBeForeEdit = new List<string>();

        public event Action<List< int>, int>? EntferneSchichten;

        public event Action<int>? ErstelleExport;

        public event Action<MASaveChanges>? SaveMAChanges;

        public bool ShouldRedrawCalendar = false;

        private Color? ColorCache = new Color();


        private void DeleteSchiftsEdit(object sender, RoutedEventArgs e)
        {
            BestätigungsWidget BSW = new BestätigungsWidget();
            BSW.Owner = this;
            string InfoText = $"Alle Schichten für {MAName.Text} löschen?";
            BSW.SetInfoText(InfoText);
            bool? result = BSW.ShowDialog();
            if (result != null && result == true)
            {
                List<int> IDsToRemove = new List<int>();
                foreach (UIElement Thingi in RolePanel.Children)
                {
                    if (Thingi != null && Thingi is SchichtLabel)
                    {
                        SchichtLabel Label = (SchichtLabel)Thingi;


                        IDsToRemove.Add(Label._LinkedSchichtID);

                    }
                }

                RolePanel.Children.Clear();

                EntferneSchichten?.Invoke(IDsToRemove, MAID);

                ShouldRedrawCalendar = true;
            }
        }
      
        private void SaveEdit(object sender, RoutedEventArgs e)
        {

            if (Locations.SelectedItems.Count <= 0)
            {
                MessageBox.Show("Es wird mindestens ein Standort pro Mitarbeiter vorausgesetzt.");
                return;
            }

            if (string.IsNullOrWhiteSpace(MANameEdit.Text))
            {
                MessageBox.Show("Bitte gib einen gültigen Namen ein.");
                return;
            }

            double Stunden = 160;
            if (double.TryParse(ZielStundenBox.Text, out Stunden) && Stunden <= 0)
            {
                MessageBox.Show("Bitte gib eine gültige Stundenzahl an.");
                return;
            }

            if(MAName.Text.Trim() != MANameEdit.Text.Trim()) ShouldRedrawCalendar = true;
            MASaveChanges NewSave = new MASaveChanges();
            NewSave.MAToChange = MAID;
            NewSave.NewName = MANameEdit.Text.Trim();
            foreach (var item in RoleSelection.SelectedItems)
            {
                NewSave.NewRoles.Add(item.ToString()!);
            }
            
            NewSave.NeueZielStunden = Stunden;
            foreach (var item in Locations.SelectedItems)
            {
                NewSave.NeueStandorte.Add(item.ToString()!);
                NewSave.NeueStandortIDs.Add(OrtCache[item.ToString()!]);
            }

            if(ColorCache != MyColorPicker.SelectedColor) ShouldRedrawCalendar = true;
            if(MyColorPicker.SelectedColor != null) NewSave.NewColorHex = MyColorPicker.SelectedColor.Value.ToString();


            foreach (var Litem in LocationsBeForeEdit)
            {

                int LID = OrtCache[Litem.ToString()!];
                if (!NewSave.NeueStandortIDs.Contains(LID)) //Ein Ort wurde entfernt
                {
                    if (SchichtTracker.TryGetValue(LID, out int SchichtCount)) //Der Ort hatte noch schichten
                    {
                        BestätigungsWidget BSW = new BestätigungsWidget();
                        BSW.Owner = this;
                        string InfoText = $"Bist du dir sicher, dass du den Standort {Litem.ToString()!} entfernen möchtest? \n" +
                           $" Durch die Aktion werden {SchichtCount} Schichten gelöscht.";
                        BSW.SetInfoText(InfoText);
                        bool? result = BSW.ShowDialog();
                        if (result != null && result == true)
                        {

                            List<int> IDsToRemove = new List<int>();

                            RemoveSchichten(LID, IDsToRemove, RolePanel);
                    

                            SchichtTracker.Remove(LID);
                            ShouldRedrawCalendar = true;
                        }
                        else //Add it back againe to locations
                        {
                            NewSave.NeueStandorte.Add(Litem.ToString()!);
                            NewSave.NeueStandortIDs.Add(OrtCache[Litem.ToString()!]);
                        }
                    }

                } 
            }


            List<string>? NewSchliessTage = abwesendBox.Text.Split(',', ';').ToList();
            if (NewSchliessTage != null && NewSchliessTage.Count > 0)
            {
                string DayStrings = "mo,di,mi,do,fr,sa,so";
                foreach (string Day in NewSchliessTage)
                {
                    if (string.IsNullOrWhiteSpace(Day)) continue;
                    var match = Regex.Match(Day, @"^([^(]+)(?:\((.*)\))?$");
                    var Rangematch = Regex.Match(Day, @"(\d+)-(\d+)(?:\s*[\(\[\{](.*?)[\)\]\}])?$");

                    if (Rangematch.Success)
                    {

                        var AbrreviationMatch = Regex.Match(Rangematch.Groups[3].Value, @"^(.+?)\s*[\(\[\{](.*?)[\)\]\}]");
                        string type = "";
                        string typeAB = "";
                        if (AbrreviationMatch.Success)
                        {

                            type = AbrreviationMatch.Groups[1].Value.ToString().Trim();
                            typeAB = AbrreviationMatch.Groups[2].Value.ToString().Trim();
                        }
                        else
                        {
                            type = Rangematch.Groups[3].Value.ToString().Trim();
                        }
                        int start = int.Parse(Rangematch.Groups[1].Value.ToString().Trim());
                        int end = int.Parse(Rangematch.Groups[2].Value.ToString().Trim());

                        if (start > end)
                        {
                            System.Windows.MessageBox.Show("Bitte gib eine gültige Abwesenheitsspanne ein.");

                            return;
                        }

                        foreach (int day in Enumerable.Range(start, end - start + 1))
                        {
                            Abwesenheit Neueabwesenheit = new();
                            Neueabwesenheit.Typ = type;
                            Neueabwesenheit.Tag = day.ToString();
                            Neueabwesenheit.TypAbbreviation = typeAB;
                            NewSave.AbwesendListe.Add(Neueabwesenheit);
                        }
                    }
                    else if (match.Success)
                    {
                        string DayString = match.Groups[1].Value.Trim().ToLower();
                        var AbrreviationMatch = Regex.Match(match.Groups[2].Value.Trim(), @"^(.+?)\s*[\(\[\{](.*?)[\)\]\}]");
                        Abwesenheit Neueabwesenheit = new();
                        if (AbrreviationMatch.Success)
                        {
                            Neueabwesenheit.Typ = AbrreviationMatch.Groups[1].Value.Trim();
                            Neueabwesenheit.TypAbbreviation = AbrreviationMatch.Groups[2].Value.Trim();
                        }
                        else
                        {
                            Neueabwesenheit.Typ = match.Groups[2].Value.Trim(); // "UR"
                        }

                        if (DayStrings.Contains(DayString))
                        {
                            Neueabwesenheit.Tag = DayString;
                        }
                        else if (int.TryParse(DayString, out int OutDayInt) && OutDayInt > 0 && OutDayInt <= 31)
                        {
                            Neueabwesenheit.Tag = DayString;
                        }
                        else
                        {
                            System.Windows.MessageBox.Show("Bitte gib einen gültigen Wert an.");
                            return;
                        }
                        NewSave.AbwesendListe.Add(Neueabwesenheit);
                    }
                    
                    else
                    {
                        System.Windows.MessageBox.Show("Bitte gib einen gültigen Wert an.");

                        return;
                    }
               
                }

                AbwesendeTage.Text = abwesendBox.Text;
                NewSave.AbwesendString = abwesendBox.Text;
            }

            SaveMAChanges?.Invoke(NewSave); 

            EditButton.Visibility = Visibility.Visible;
            SpeicherButton.Visibility = Visibility.Collapsed;

            SchichtStandorte.Visibility = Visibility.Visible;
            Locations.Visibility = Visibility.Collapsed;

            ZielStundenText.Visibility = Visibility.Visible;
            ZielStundenBox.Visibility = Visibility.Collapsed;

            MAName.Visibility = Visibility.Visible;
            MANameEdit.Visibility = Visibility.Collapsed;

            RText.Visibility = Visibility.Visible;
            RoleSelection.Visibility = Visibility.Collapsed;
            AbwesendeTage.Visibility = Visibility.Visible;
            abwesendBox.Visibility = Visibility.Collapsed;
            UrlaubsTage.Visibility = Visibility.Visible;
            UrlaubsBox.Visibility = Visibility.Collapsed;
            AbwesenheitsTooltip.Visibility = Visibility.Collapsed;
            DeleteShifts.Visibility = Visibility.Collapsed;
            ExportMAPDF.Visibility = Visibility.Visible;

            MyColorPicker.IsEnabled = false;
        }
        private void ExportMA(object sender, RoutedEventArgs e)
        {
            ErstelleExport?.Invoke(MAID);   
        }
        private void RemoveSchichten(int LID, List<int> IDsToRemove, WrapPanel ShiftBox)
        {
            foreach (UIElement Thingi in ShiftBox.Children)
            {
                if (Thingi != null && Thingi is SchichtLabel)
                {
                    SchichtLabel Label = (SchichtLabel)Thingi;
                    if (Label._LinkedOrtID == LID)
                    {
                        IDsToRemove.Add(Label._LinkedSchichtID);
                    }
                }
            }

            foreach (int ID in IDsToRemove)
            {
                foreach (UIElement Thingi in ShiftBox.Children)
                {
                    if (Thingi != null && Thingi is SchichtLabel)
                    {
                        SchichtLabel Label = (SchichtLabel)Thingi;
                        if (ID == Label._LinkedSchichtID)
                        {
                            ShiftBox.Children.Remove(Thingi);
                            break;
                        }
                    }
                }
            }

            EntferneSchichten?.Invoke(IDsToRemove, MAID);
        }
        private void StartEdit(object sender, RoutedEventArgs e)
        {
            EditButton.Visibility = Visibility.Collapsed;
            SpeicherButton.Visibility = Visibility.Visible;

            SchichtStandorte.Visibility = Visibility.Collapsed;
            Locations.Visibility = Visibility.Visible;

            ZielStundenBox.Visibility = Visibility.Visible;
            ZielStundenText.Visibility = Visibility.Collapsed;

            MAName.Visibility = Visibility.Collapsed;
            MANameEdit.Visibility = Visibility.Visible;

            RText.Visibility = Visibility.Collapsed;
            RoleSelection.Visibility = Visibility.Visible;

            AbwesendeTage.Visibility = Visibility.Collapsed;
            abwesendBox.Visibility = Visibility.Visible;

            UrlaubsTage.Visibility = Visibility.Collapsed;
            UrlaubsBox.Visibility = Visibility.Visible;

            DeleteShifts.Visibility = Visibility.Visible;
            ExportMAPDF.Visibility = Visibility.Collapsed;

            AbwesenheitsTooltip.Visibility = Visibility.Visible;

            MyColorPicker.IsEnabled = true;

            ColorCache = MyColorPicker.SelectedColor;
            

            foreach (var item in Locations.SelectedItems)
             {
                    LocationsBeForeEdit.Add(item.ToString()!);
             }
        }
    }


    public class MASaveChanges
    {

        public int MAToChange = 0;
        public List<string> NeueStandorte = new();
        public List<int> NeueStandortIDs = new();

        public string NewColorHex { get; set; } = "#3A3A3A";
        public string NewName { get; set; } = string.Empty;
        public List<string> NewRoles { get; set; } = new();

        public double NeueZielStunden = 0;

        public string AbwesendString { get; set; } = "";
        public List<Abwesenheit> AbwesendListe { get; set; } = new();


    }

    public class Abwesenheit
    {
        public string Tag { get; set; } = "";
        public string Typ { get; set; } = "";
        public string TypAbbreviation { get; set; } = "";
    }
}
