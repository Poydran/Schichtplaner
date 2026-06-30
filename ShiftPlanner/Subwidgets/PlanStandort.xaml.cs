using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaction logic for PlanStandort.xaml
    /// </summary>
    public partial class PlanStandort : UserControl
    {
        public PlanStandort()
        {
            InitializeComponent();
        }
        public int LinkedStandortID { get; set; }

        //Basicly a Delegate Event
        public event Action<PlanStandort>? ClickedLeft;
        public event Action<PlanStandort>? ClickedRight;

        private void RootBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //wenn gelickt lass Main Window subscribe too
            ClickedLeft?.Invoke(this);
        }

        private void RootBorder_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ClickedRight?.Invoke(this);

        }

        public void SetOrtName(String InName)
        {
            NameText.Text = InName;
        }

        public void SetSelected(bool selected)
        {
            RootBorder.Background = selected
                ? Brushes.DarkSlateBlue
                : new SolidColorBrush(Color.FromRgb(58, 58, 58));
        }
    }


    public class PlanStandortData
    {
        public int PlanStandortId { get; set; }
        public string StandortName { get; set; } = "Dokuzentrum";
        public string StandortKuerzel{ get; set; } = "";
        public string SchliessTage { get; set; } = "";

        public List<string> SchliessTageList { get; set; } = new();

        public List<int> MAIDs { get; set; } = new List<int>();

        public List<string> Roles { get; set; } = new List<string>();

        public bool IstSelektiert {  get; set; } = false;

        public bool bIsClosedOnHoliday { get; set; } = false;

        public EBundesland Bundesland { get; set; } = EBundesland.Bayern;


    }


   public enum EBundesland
   {
        Bayern,
        Berlin,
        Brandenburg,
        Bremen,
        [Description("Baden-Württemberg")]
        BadenWuerttemberg,
        Hamburg,
        Hessen,
        [Description("Mecklenburg-Vorpommern")]
        MecklenburgVorpommern,
        Niedersachsen,
        [Description("Nordrhein-Westfalen")]
        NordrheinWestfalen,
        [Description("Rheinland-Pfalz")]
        RheinlandPfalz,
        Saarland,
        Sachsen,
        [Description("Sachsen-Anhalt")]
        SachsenAnhalt,
        [Description("Schleswig-Holstein")]
        SchleswigHolstein,
        [Description("Thüringen")]
        Thueringen
    }
}
