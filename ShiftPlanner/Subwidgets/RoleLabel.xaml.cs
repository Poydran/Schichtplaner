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
    /// Interaction logic for RoleLabel.xaml
    /// </summary>
    public partial class RoleLabel : UserControl
    {
        public RoleLabel()
        {
            InitializeComponent();
        }

        //Basicly a Delegate Event
        public event Action<RoleLabel>? ClickedLeft;

        public RoleData RoleData = new();

        private void RootBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //wenn gelickt lass Main Window subscribe too
            ClickedLeft?.Invoke(this);
        }

        public void SetRole(string role, string? KZ)
        {
            RoleData.RoleName = role;
            RoleData.RoleKuerzel = KZ;
            NameText.Text = role;
            if(KZ != null)
            {
                NameText.Text += $"({KZ})";
            }
        }

    }

    public class RoleData
    {
        public string RoleName { get; set; } = "";
        public string? RoleKuerzel { get; set; } = "";
    }

}
