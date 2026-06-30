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
using static System.Net.Mime.MediaTypeNames;

namespace ShiftPlanner.Subwidgets
{
    /// <summary>
    /// Interaction logic for EmployeeCreater.xaml
    /// </summary>
    public partial class EmployeeCreater : Window
    {
        public EmployeeCreater()
        {
            InitializeComponent();
        }


        //Basicly a Delegate Event
        public event Action<EmployeeData, List<String>>? Erstellt;

        private Dictionary<String, int> OrtCache = new();

        public void SetStandortSelection(Dictionary<String, int> Orte)
        {
            OrtCache = Orte;
            foreach (var item in Orte)
            {
                Locations.Items.Add(item.Key);
            }
        }
        private void Add_Click(object sender, RoutedEventArgs e)
        {
           
            if (string.IsNullOrWhiteSpace(NameInput.Text))
            {
                MessageBox.Show("Bitte gib einen Namen ein.");
                return;
            }

            if (Locations.SelectedItems.Count == 0)
            {
                MessageBox.Show("Bitte wähle mindestens einen Standort aus.");
                return;
            }

            double Stunden = 160;
            if (double.TryParse(HoursInput.Text, out Stunden) && Stunden <= 0)
            {
                MessageBox.Show("Bitte gib eine gültige Stundenzahl an.");
                return;
            }

            List<string> Standorte = new();
            List<int> StandorteID = new();
            foreach (var item in Locations.SelectedItems)
            {
                Standorte.Add(item.ToString()!);
                StandorteID.Add(OrtCache[item.ToString()!]);
            }
 


            EmployeeData NewPloyee = new EmployeeData();
            NewPloyee.SetZielStunden(Stunden);
            NewPloyee.SetzeNeuenNamen(NameInput.Text);
            NewPloyee.SetzeStandorte(StandorteID);
            Erstellt?.Invoke(NewPloyee, Standorte);
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
