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


            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { });
            Button button = new Button
            {
                Content = "Entfernen",
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0,0,10,0)


            };
            Grid.SetColumn(button, 1);
            button.Click += Loesch_Click;
            grid.Children.Add(button);
            grid.Children.Add(TempElement);
            ActiveHandles.Children.Add(grid);
         
        }

        private void Bestaetigung_Click(object sender, RoutedEventArgs e)
        {
            Automate?.Invoke(Templates);
            DialogResult = true;
            Close();
        }

        private void Loesch_Click(object sender, RoutedEventArgs e)
        {

            Button button = (Button)sender;
            Grid grid = button.Parent as Grid;
            foreach (UIElement child in grid.Children)
            {
                    if (child is DailyShiftsHandle handle)
                    {
                        Templates.Remove(handle.dayTemplateData);
                    }
            }
            button.Click -= Loesch_Click;
            ActiveHandles.Children.Remove(grid);
        }


        public event Action<List<DayTemplateData>>? Automate;

        public List<string> Rollen = new List<string>();

        public List<DayTemplateData> Templates = new List<DayTemplateData>();
    }

   



}
