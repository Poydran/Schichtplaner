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
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace ShiftPlanner.Subwidgets
{
    /// <summary>
    /// Interaction logic for SchichtEditor.xaml
    /// </summary>
    public partial class SchichtEditor : Window
    {
        public SchichtEditor()
        {
            InitializeComponent();
        }

        public event Action<SchichtSaveChanges>? SaveSchichtInfo;

        public int SchichtID { get; set; }

        public void SetSchichtInfo(SchichtInfo InInfo, EmployeeData ArbeiterInfo, string OrtName)
        {
            string DatumsString = $"Datum:      {InInfo.Date.Day.ToString("00")}.{InInfo.Date.Month.ToString("00")}.{InInfo.Date.Year}";
            SchichtDatum.Text = DatumsString;
            string EmployeeString = $"Mitarbeiter:      {ArbeiterInfo._MitarbeiterName}";
            MitarbeiterText.Text = EmployeeString;
            string RollString = $"Mitarbeiterrolle:     {InInfo.SchichtRolle}";
            MitarbeiterRolleText.Text = RollString;

            string StundenText = $"{InInfo.Zeiten.SchichtStunden} std";
            StundenZahl.Text = StundenText;
            StartZeit.Text = InInfo.Zeiten.SchichtStartText;
            StartZeitBox.Text = InInfo.Zeiten.SchichtStartText;
            SchlussZeit.Text = InInfo.Zeiten.SchichtSchlussText;
            SchlussZeitBox.Text = InInfo.Zeiten.SchichtSchlussText;
            Pausendauer.Text = $"{InInfo.Zeiten.PausenZeit} min";
            NoteBlock.Text = InInfo.Notiz;
            SchichtStandort.Text = $"Standort:      {OrtName}";

        }

        private void DeleteSchicht(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void StartEdit(object sender, RoutedEventArgs e)
        {
            EditButton.Visibility = Visibility.Collapsed;
            SaveButton.Visibility = Visibility.Visible;

            StartZeit.Visibility = Visibility.Collapsed;
            StartZeitBox.Visibility = Visibility.Visible;

            SchlussZeitBox.Visibility = Visibility.Visible;
            SchlussZeit.Visibility = Visibility.Collapsed;

            DeleteButton.Visibility = Visibility.Visible;
            NoteBlock.IsReadOnly = false;
        }
        private void SaveEdit(object sender, RoutedEventArgs e)
        {

            SchichtSaveChanges NewSchichtSave = new();

            NewSchichtSave.LinkedSchichtID = SchichtID;
            NewSchichtSave.NewStartTime = StartZeitBox.Text;
            NewSchichtSave.NewEndTime = SchlussZeitBox.Text;
            NewSchichtSave.Notiz = NoteBlock.Text;

            SaveSchichtInfo?.Invoke(NewSchichtSave);

            EditButton.Visibility = Visibility.Visible;
            SaveButton.Visibility = Visibility.Collapsed;

            StartZeit.Visibility = Visibility.Visible;
            StartZeitBox.Visibility = Visibility.Collapsed;

            SchlussZeit.Visibility = Visibility.Visible;
            SchlussZeitBox.Visibility = Visibility.Collapsed;
            NoteBlock.IsReadOnly = true;

            DeleteButton.Visibility = Visibility.Collapsed;
        }
    }


    public class SchichtSaveChanges
    {
        public int LinkedSchichtID { get; set; }
        public string NewStartTime { get; set; } = string.Empty;
        public string NewEndTime { get; set; } = string.Empty;

        public string Notiz { get; set; } = string.Empty;
    }



}
