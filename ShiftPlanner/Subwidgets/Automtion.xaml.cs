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

namespace ShiftPlanner.Subwidgets
{
    /// <summary>
    /// Interaction logic for Automtion.xaml
    /// </summary>
    public partial class Automtion : Window
    {
        public Automtion()
        { 
            InitializeComponent();
        }

        public void MakeNewTemplate()
        {
            DailyShiftsHandle BaseHandle = new DailyShiftsHandle();
            foreach (string Rol in Rollen)
            {
                BaseHandle.RoleSelection.Items.Add(Rol);
            }
            TemplateBorder.Child = BaseHandle;
        }

        private void AddTemplate(object sender, RoutedEventArgs e)
        {
            UIElement TempElement = TemplateBorder.Child;
          
            if (TempElement is DailyShiftsHandle handle)
            {
                if (handle.MoveValues())
                {
                    Templates.Add(handle.dayTemplateData);
                }
                else return;
            }

            MakeNewTemplate();
            ActiveHandles.Children.Add(TempElement);
         
        }

        private void Bestaetigung_Click(object sender, RoutedEventArgs e)
        {
            Automate?.Invoke(Templates);
            DialogResult = true;
            Close();
        }

        public event Action<List<DayTemplateData>>? Automate;

        public List<string> Rollen = new List<string>();

        public List<DayTemplateData> Templates = new List<DayTemplateData>();
    }

   


}
