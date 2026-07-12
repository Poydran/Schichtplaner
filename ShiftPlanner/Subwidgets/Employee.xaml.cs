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
    /// Interaction logic for Employee.xaml
    /// </summary>
    public partial class Employee : UserControl
    {
        public Employee()
        {
            InitializeComponent();
        }

        public int _LinkedMitarbeiterID { get; set; } = 0;

        //Basicly a Delegate Event
        public event Action<Employee>? Clicked;

        public event Action<Employee>? RightClicked;

        private void RootBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //wenn gelickt lass Main Window subscribe too
            Clicked?.Invoke(this);
        }

        private void RootBorder_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            //wenn gelickt lass Main Window subscribe too
            RightClicked?.Invoke(this);
        }

        public void SetMitarbeiterName(String InName)
        {
            NameText.Text = InName;
        }

        public void SetRolls(List<String> InRolls)
        {
            Rollen.Text = "Rollen: ";
            int index = 0;
            foreach (String Roll in InRolls)
            {

                if (index != 0) { Rollen.Text += ", "; }
                Rollen.Text += $"{Roll}";
                index++;
            }
        }

        public void SetSelected(bool selected)
        {
            RootBorder.Background = selected
                ? Brushes.DarkSlateBlue
                : new SolidColorBrush(Color.FromRgb(58, 58, 58));
        }

        public void SetHours(double plannedHours, double TargetHours)
        {
            if(plannedHours >= TargetHours * 1.05)
            {
                VerplanteStunden.Foreground = Brushes.Pink;
            }else if(plannedHours > TargetHours * 0.9 && plannedHours < TargetHours * 1.1)
            {
                VerplanteStunden.Foreground = Brushes.LightGreen;
            }
            else
            {
                VerplanteStunden.Foreground = Brushes.LightGray;
            }
            VerplanteStunden.Text = $"{plannedHours}";
            MaxStunden.Text = $" / {TargetHours} h";
        }


    }


    public class EmployeeData
    {

        public int _MitarbeiterID { get; set; }
        public string _MitarbeiterName { get; set; } = "";
        public double _ZielStunden { get; set; } = 160.0f;
        public double _VerplanteStunden { get; set; } = 0.0f;

        public string ColorHex { get; set; } = "#3A3A3A";

        public List<String> _VorgeseheneRollen { get; set; } = new();

        public List<int> _Standorte { get; set; } = new();

        public List<int> _ZugeteilteSchichten { get; set; } = new();

        public bool IstSelektiert { get; set; } = false;

        public string AbwesendString { get; set; } = "";
        public List<TagesWunsch> AbwesendListeNew { get; set; } = new();
        public string EinsatzwunschString { get; set; } = "";
        public List<TagesWunsch> Einsatzwuensche { get; set; } = new();
        public string FreitagWunschString { get; set; } = "";
        public List<TagesWunsch> FreitagsWuensche { get; set; } = new();

        public List<DateTime> TageImEinsatz = new();
        public int MaxArbeitsTageAmStueck { get; set; } = 5;

        public void SetzeNeuenNamen(String InName)
        {
            _MitarbeiterName = InName;
        }
        public void SetZielStunden(double InZielStunden)
        {
            _ZielStunden = InZielStunden;

        }

        public void SetzeStandorte(List<int> Orte)
        {
          _Standorte = Orte;
        }

        public void SetzeRollen(List<string> InRollen)
        {
       
            _VorgeseheneRollen = InRollen;
         

        }

    }
}
