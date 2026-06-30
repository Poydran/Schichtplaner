using QuestPDF.Infrastructure;
using System.Configuration;
using System.Data;
using System.Windows;

namespace ShiftPlanner
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }
    }

}
