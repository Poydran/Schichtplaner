using Microsoft.VisualBasic;
using Microsoft.Win32;
using QuestPDF.Elements;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ShiftPlanner.Subwidgets;
using ShiftPlanner.Utility;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using static QuestPDF.Helpers.Colors;
using static System.Formats.Asn1.AsnWriter;
using static System.Runtime.InteropServices.JavaScript.JSType;





namespace ShiftPlanner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Sortierungen.Items.Add("Name");
            Sortierungen.Items.Add("Zielstunden");
            Sortierungen.Items.Add("Zugeteilte Stunden");
            Sortierungen.Items.Add("Stunden Differenz");

            WeekDayList.DataContext = this;
            DayController.DataContext = this;
            RoleController.DataContext = this;

            _DayAbbreviations.Add(0, "Mo");
            _DayAbbreviations.Add(1, "Di");
            _DayAbbreviations.Add(2, "Mi");
            _DayAbbreviations.Add(3, "Do");
            _DayAbbreviations.Add(4, "Fr");
            _DayAbbreviations.Add(5, "Sa");
            _DayAbbreviations.Add(6, "So");

            _MonthMapping.Add(1, "Januar");
            _MonthMapping.Add(2, "Februar");
            _MonthMapping.Add(3, "März");
            _MonthMapping.Add(4, "April");
            _MonthMapping.Add(5, "Mai");
            _MonthMapping.Add(6, "Juni");
            _MonthMapping.Add(7, "Juli");
            _MonthMapping.Add(8, "August");
            _MonthMapping.Add(9, "September");
            _MonthMapping.Add(10, "Oktober");
            _MonthMapping.Add(11, "November");
            _MonthMapping.Add(12, "Dezember");

            MonthTitleText.Text = _MonthMapping[_currentMonth.Month] + " " + _currentMonth.Year.ToString();

            CommandBindings.Add(
            new CommandBinding(
                ApplicationCommands.Save,
                    JustSave));

            InputBindings.Add(
            new KeyBinding(
                ApplicationCommands.Save,
                Key.S,
                ModifierKeys.Control));

            
        }

        //Settings Variablen

        private bool _unsavedchanges = false;

        private bool _WeekViewActive = false;

        private bool _KeineWEs = false;

        private bool _UseRoleKuerzel = false;

        private bool _UseStandortKuerzel = false;

        private bool _AufundAb = false;

        private bool _ShowOutOfOfficeReason = false;

        private bool _UseBreakTimes = false;

        private bool _UseColorForPDF = false;

        public float _ExportSizePDFST = 5;
        public float _ExportSizePDFPersonal= 12;

        private Brush _ClosedDayBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(140,33,49));

        private double _ClosedDayOpacity = .5f;

        public ObservableCollection<WochenTag> _WochenTage { get; } = new();

        public ObservableCollection<KalenderTag> _KalenderTage { get; } = new();

        public ObservableCollection<RoleLabel> _Rollen { get; } = new();

        public bool UnsavedChanges
        {
            get => _unsavedchanges;

            set
            {
                if(_unsavedchanges == value) return;
                _unsavedchanges= value;

                UpdateUI();
            }
        }
   
        private MitarbeiterInfo? _MitarbeiterInfoCache;

        private SchichtEditor? _SchichtEditorCache;

        private DateTime _currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

        private int _SchichtIDCounter = 0;

        public string SelectedRole = "";

        private List<SchichtInfo> _Schichten = new();

        private EmployeeData? _ActiveEmployee;

        private int _MitarbeiterIDCounter = 0;

        private List<EmployeeData> _Mitartbeiter = new();

        private Dictionary<int, Employee> _MitarbeiterWidgetMap = new();

        private int _StandortIDCounter = 0;

        private List<PlanStandortData> _Standorte = new();

        private int _ActiveStandortID = -1;

        private string _savefile = "";

        private Dictionary<int, PlanStandort> _StandortWidgetMap = new();

        private Dictionary<int, string> _DayAbbreviations = new();

        private Dictionary<int, string> _DayMapping = new();

        private Dictionary<int, string> _MonthMapping = new();



        //Settings

        private void ChangeFontSize_Click(object sender, RoutedEventArgs e)
        {
            string result = Interaction.InputBox(
                "Gib eine neue Export größe für die Font ein.",
                "Fontänderung",
                "");

            float Size = 5;
            if (!float.TryParse(result, out Size) || Size <= 0)
            {
                MessageBox.Show("Bitte gib eine gültige Größe an.");
                return;
            }

            _ExportSizePDFST = Size;

            ExportFont.Header = "Aktuelle Größe: " + result;
            UnsavedChanges = true;

        }

        private void ChangeFontSizeMA_Click(object sender, RoutedEventArgs e)
        {
            string result = Interaction.InputBox(
                "Gib eine neue Export größe für die Font ein.",
                "Fontänderung",
                "");

            float Size = 12;
            if (!float.TryParse(result, out Size) || Size <= 0)
            {
                MessageBox.Show("Bitte gib eine gültige Größe an.");
                return;
            }

            _ExportSizePDFPersonal = Size;

            ExportFontMA.Header = "Aktuelle Größe: " + result;
            UnsavedChanges = true;

        }
        private static Dictionary<string, string?> GetFullNameAndKuerzel(string InChanges)
        {
            Dictionary<string, string?> result = new();
            foreach (string part in InChanges.Split(';', ','))
            {
                Match match = Regex.Match(
                    part.Trim(),
                    @"^(.*?)\((.*?)\)$");

                if (match.Success)
                {
                    result.Add(
                        match.Groups[1].Value.Trim(),
                        match.Groups[2].Value.Trim());
                }
                else
                {
                    result.Add(
                        part.Trim(),
                        null);
                }
            }

            return result;
        }
        private void ChangeWEVisibility(object sender, RoutedEventArgs e)
        {
            _KeineWEs = !_KeineWEs;
            SwitchGenerator();
        }
        private void ChangeKZOn_Click(object sender, RoutedEventArgs e)
        {
            _UseRoleKuerzel = !_UseRoleKuerzel;
            SwitchGenerator();
        }
        private void ChangeSTKZOn_Click(object sender, RoutedEventArgs e)
        {
            _UseStandortKuerzel = !_UseStandortKuerzel;

            if(_ActiveEmployee != null)
            {
                AddMAChangesToDays(_ActiveEmployee);
            }

        }
        private void ChangeColorforPDF(object sender, RoutedEventArgs e)
        {
            _UseColorForPDF = !_UseColorForPDF;

        }
        private void ChangePausenSettings(object sender, RoutedEventArgs e)
        {
            _UseBreakTimes = !_UseBreakTimes;
            NeuberechneArbeitszeitNachPausen();
            UnsavedChanges = true;
        }
        private void ChangeAbwesenheitsSettings(object sender, RoutedEventArgs e)
        {
            _ShowOutOfOfficeReason = !_ShowOutOfOfficeReason;
            UnsavedChanges = true;
        }
        private void RecalcPlannedHours()
        {
            foreach (EmployeeData employeeData in _Mitartbeiter)
            {

                employeeData._VerplanteStunden = 0;
                foreach (int ID in employeeData._ZugeteilteSchichten)
                {
                    if (GetSchicht(ID) is SchichtInfo SInfo && SInfo.Date.Month == _currentMonth.Month && SInfo.Date.Year == _currentMonth.Year)
                    {
                        if(!IsDayOff(employeeData.AbwesendListeNew,SInfo.Date.Day))
                        {
                            employeeData._VerplanteStunden += SInfo.Zeiten.SchichtStunden;
                        }
                        
                    }
                }

                if (_MitarbeiterWidgetMap.TryGetValue(employeeData._MitarbeiterID, out Employee? OutEmployee))
                {
                    _MitarbeiterWidgetMap[employeeData._MitarbeiterID].SetHours(employeeData._VerplanteStunden, employeeData._ZielStunden);
                }
            }
        }
        private void NeuberechneArbeitszeitNachPausen()
        {
            foreach (SchichtInfo SI in _Schichten)
                {
                    double StundenZahl = Math.Abs(SI.Zeiten.SchichtEnde - SI.Zeiten.SchichtStart) / 60;
                    int PausenZeit = 0;
                    if (_UseBreakTimes)
                    {
                        if (StundenZahl > 9)
                        {
                            StundenZahl -= .75f;
                            PausenZeit = 45;
                        }
                        else if (StundenZahl > 6)
                        {
                            StundenZahl -= .5f;
                            PausenZeit = 30;
                        }
                    }
                    SI.Zeiten.SchichtStunden = Math.Round(StundenZahl, 2);
                    SI.Zeiten.PausenZeit = PausenZeit;
                }

            RecalcPlannedHours();
        }
        private void UpdateUI()
        {

            if (_unsavedchanges)
            {
                SaveButton.BorderBrush = Brushes.LightSkyBlue;
                SaveButton.Foreground = Brushes.LightSkyBlue;
            }
            else
            {
                SaveButton.BorderBrush = Brushes.LightGray;
                SaveButton.Foreground = Brushes.White;
            }


        }


    

        //Tools

        private void OpenAutomator(object sender, RoutedEventArgs e)
        {

            if (_ActiveStandortID < 0)
            {
                MessageBox.Show("Bitte wähle vorher einen Standort aus.");
                return;
            }

            Automtion Automation = new Automtion();
            Automation.Owner = this;

            foreach(RoleLabel Rolle in _Rollen)
            {
                Automation.Rollen.Add(Rolle.RoleData.RoleName);
            }

            Automation.MakeNewTemplate();
            Automation.Automate += TriggerAutomation;

            bool? result = Automation.ShowDialog();
            if (result != null)
            {
                Automation.Automate -= TriggerAutomation;
            }

        }

        private void StandortSchichtenLoeschen_Click(object sender, RoutedEventArgs e)
        {

            if (_ActiveStandortID < 0)
            {
                MessageBox.Show("Bitte wähle vorher einen Standort aus.");
                return;
            }

            LoescheAlleStandortSchichten(_ActiveStandortID);

        }



        private void TriggerAutomation(List<DayTemplateData> DTDList)
        {


            if (DTDList.Count <= 0) return;
            string OutputText = "Ausgabe Log:" + Environment.NewLine + Environment.NewLine;
            string FehlendeSchichten = "";
            string UberarbeiteteMitarbeiter = "";
            string ZuvieleTage = "";

            List<EmployeeData> EmployeesInQuestion = new List<EmployeeData>();
            Dictionary<string, int> EmployeesPerRole = new();
            foreach (EmployeeData ED in _Mitartbeiter)
            {
                if (ED._Standorte.Contains(_ActiveStandortID))
                {

                    foreach( string role in  ED._VorgeseheneRollen)
                    {

                        if (EmployeesPerRole.TryGetValue(role, out int count))
                        {
                            EmployeesPerRole[role]++;
                        }
                        else
                        {
                            EmployeesPerRole.Add(role, 1);
                        }
                    }


                    EmployeesInQuestion.Add(ED);
                }
            }

            foreach (DayTemplateData DTD in DTDList)
            {
                if (EmployeesPerRole.TryGetValue(DTD.SchichtRolle, out int count))
                {
                  DTD.DifficultyWeight =  DTD.Anzahl - count;

                }
                SchichtZeit schichtZeit = new SchichtZeit();
                TimeOnly OutTime = new();
                schichtZeit.SchichtStart = UtilityClass.GetTimeFromString(DTD.SchichtStartText, out OutTime);
                schichtZeit.SchichtStartText = OutTime.ToShortTimeString();
                schichtZeit.SchichtEnde = UtilityClass.GetTimeFromString(DTD.SchichtSchlussText, out OutTime);
                schichtZeit.SchichtSchlussText = OutTime.ToShortTimeString();
                DateTime StartTime = _currentMonth.Date;
                StartTime = StartTime.AddHours((int)(schichtZeit.SchichtStart / 60));
                StartTime = StartTime.AddMinutes((int)(schichtZeit.SchichtStart % 60));
                DateTime EndTime = _currentMonth.Date;
                EndTime = EndTime.AddHours((int)(schichtZeit.SchichtEnde / 60));
                EndTime = EndTime.AddMinutes((int)(schichtZeit.SchichtEnde % 60));
                if (schichtZeit.SchichtEnde < schichtZeit.SchichtStart)
                {
                    EndTime = EndTime.AddDays(1);
                    schichtZeit.bPlusOneDay = true;
                }
                //OutShiftTime.SchichtEndDate = EndTime;
               // OutShiftTime.SchichtStartDate = StartTime;
               double StundenZahl = (EndTime - StartTime).TotalHours;

                if (_UseBreakTimes)
                {
                    if (StundenZahl > 9)
                    {
                        StundenZahl -= .75f;
                        schichtZeit.PausenZeit = 45;
                    }
                    else if (StundenZahl > 6)
                    {
                        StundenZahl -= .5f;
                        schichtZeit.PausenZeit = 30;
                    }
                }
                schichtZeit.SchichtStunden = Math.Round(StundenZahl, 2);

                DTD.DifficultyWeight += schichtZeit.SchichtStunden / 8;
                DTD.Zeiten = schichtZeit;
            }
            DTDList = DTDList.OrderByDescending(Template => Template.DifficultyWeight).ToList();

            List<SchichtInfo> StandortschichtenAtStart = GetShiftsForLocation(_ActiveStandortID);
            List<DayAutomationScore> DaysToSet = new();
            foreach (KeyValuePair<int, string> kvp in _DayMapping)
            {
                DateTime dateTime = new DateTime(_currentMonth.Year, _currentMonth.Month, kvp.Key);
                bool TagIsHoliiday = false;
                if (GetStandort(_ActiveStandortID) is PlanStandortData PSD)
                {
                    if (PSD.SchliessTageList.Contains(kvp.Key.ToString()) || PSD.SchliessTageList.Contains(kvp.Value.ToLower())) continue;

                    TagIsHoliiday = HolidayService.IsHoliday(dateTime, PSD.Bundesland);
                    if (TagIsHoliiday && PSD.bIsClosedOnHoliday) continue;
                }

                DayAutomationScore NewDay = new();
                NewDay.Datum = dateTime;
                NewDay.bIsHoliday = TagIsHoliiday;
                foreach (DayTemplateData DTD in DTDList)
                {
                    if (DTD.TagesListe.Contains(kvp.Key.ToString()) || DTD.TagesListe.Contains(kvp.Value.ToLower()))
                    {
                        NewDay.weight += DTD.DifficultyWeight;
                        NewDay.MatchingdayTemplateDatas.Add(DTD);
                    }
                }

                foreach (EmployeeData Arbeiter in EmployeesInQuestion)
                {
                    if (IsDayOff(Arbeiter.FreitagsWuensche, dateTime.Day)) NewDay.weight += 25;
                    if (IsDayOff(Arbeiter.Einsatzwuensche, dateTime.Day)) NewDay.weight += 25;

                }

                if (NewDay.MatchingdayTemplateDatas.Count > 0) DaysToSet.Add(NewDay);

            }
            DaysToSet = DaysToSet.OrderByDescending(Day => Day.weight).ToList();


            foreach(DayAutomationScore DAS in DaysToSet)
            {

                foreach(DayTemplateData DTD in DAS.MatchingdayTemplateDatas)
                {
                    int LoopAnzahl = DTD.Anzahl;
                    foreach (SchichtInfo DayShift in StandortschichtenAtStart)
                    {
                        if (DayShift.Date.Date == DAS.Datum.Date && DayShift.SchichtRolle == DTD.SchichtRolle)
                        {
                            LoopAnzahl = Math.Max(0, LoopAnzahl - 1);
                        }
                    }

                    List<EmployeeData> AvailableEmployees = GatherAvailableEmployees(EmployeesInQuestion, DTD, DAS.Datum);
                    List<EmployeeAutomation> WeighedEmployees = WeighEmployeesForAutomation(AvailableEmployees, DTD, DAS.Datum);

                    for (int i = 0; i < LoopAnzahl; i++)
                    {
                        SchichtInfo schichtInfo = new SchichtInfo();
                        schichtInfo.SchichtRolle = DTD.SchichtRolle;
                        schichtInfo.Zeiten = DTD.Zeiten;
                        schichtInfo.SLinkedID = _ActiveStandortID;
                        schichtInfo.SchichtID = _SchichtIDCounter;
                        schichtInfo.bIsHoliday = DAS.bIsHoliday;
                        schichtInfo.Date = DAS.Datum;
                        schichtInfo.Notiz = "";

                        if (WeighedEmployees.Count <= 0)
                        {
                            FehlendeSchichten+= $"Für den {DAS.Datum.ToShortDateString()} konnten nicht alle {DTD.SchichtRolle} besetzt werden." + Environment.NewLine;
                            break;
                        }
                        EmployeeAutomation EAD = WeighedEmployees[0];
                        WeighedEmployees.RemoveAt(0);


                        schichtInfo.ELinkedID = EAD.Employee._MitarbeiterID;

                        EAD.Employee._VerplanteStunden += schichtInfo.Zeiten.SchichtStunden;
                        _MitarbeiterWidgetMap.TryGetValue(EAD.Employee._MitarbeiterID, out Employee? OutEmployee);
                        if (OutEmployee != null)
                        {
                            OutEmployee.SetHours(EAD.Employee._VerplanteStunden, EAD.Employee._ZielStunden);
                        }

                        EAD.Employee._ZugeteilteSchichten.Add(schichtInfo.SchichtID);
                        EAD.Employee.TageImEinsatz.Add(DAS.Datum);
                      
                        _SchichtIDCounter++;

                        _Schichten.Add(schichtInfo);

                    }
                }

            }

            foreach(EmployeeData ED in EmployeesInQuestion)
            {
                if(ED._ZielStunden < ED._VerplanteStunden)
                {

                    UberarbeiteteMitarbeiter += $"Der Mitarbeiter {ED._MitarbeiterName} arbeitet { ED._VerplanteStunden - ED._ZielStunden} Stunden mehr, als für ihn festgelegt." + Environment.NewLine;
                }

                List<int> ZuVieleTageInFolge = new List<int>();
                int ConsecutiveDays = 0;
                DateTime StartDate = _currentMonth.AddDays(-10);
                for (int i = 1; i <= 51; i++)
                {
                    if (HasMAShiftThisDay(StartDate.AddDays(i), ED))
                    {
                        ConsecutiveDays++;
                    }
                    else
                    {
                        if (ConsecutiveDays > ED.MaxArbeitsTageAmStueck)
                        {
                            ZuvieleTage += $"Der Mitarbeiter {ED._MitarbeiterName} arbeitet {ConsecutiveDays - ED.MaxArbeitsTageAmStueck} Tage länger in Folge als für das Maximum festgelegt." + Environment.NewLine;
                        }
                        ConsecutiveDays = 0;
                        if (StartDate.Month > _currentMonth.Month) break;
                    }
                }
                
            }

            SwitchGenerator();
            OutputText += FehlendeSchichten + UberarbeiteteMitarbeiter + ZuvieleTage;
            MessageBox.Show(OutputText);
            UnsavedChanges = true;
        }

        private bool HasMAShiftThisDay(DateTime dateTime, EmployeeData ED)
        {
            return ED.TageImEinsatz.Contains(dateTime);
        }

        private bool HasMAShiftThisDay(DateTime dateTime, EmployeeData ED, out SchichtInfo? OutInfo)
        {
            foreach (int MASchicht in ED._ZugeteilteSchichten)
            {
                if (GetSchicht(MASchicht) is SchichtInfo MAShift)
                {
                    if (MAShift.Date == dateTime)
                    {
                        OutInfo = MAShift;
                        return true;
                    }
                }
            }
            OutInfo = null;
            return false;
        }

        private List<EmployeeData> GatherAvailableEmployees(List<EmployeeData> BaseEmployeeList, DayTemplateData InShiftTemplate, DateTime DateToGatherFor)
        {
            List<EmployeeData> ElegibleEmployees = new List<EmployeeData>();

            foreach (EmployeeData ED in BaseEmployeeList)
            {
                if (HasMAShiftThisDay(DateToGatherFor, ED) || IsDayOff(ED.AbwesendListeNew, DateToGatherFor.Day)) continue;
                if (!ED._VorgeseheneRollen.Contains(InShiftTemplate.SchichtRolle)) continue;

                ElegibleEmployees.Add(ED);  
            }

            return ElegibleEmployees;
        }

        private List<EmployeeAutomation> WeighEmployeesForAutomation(List<EmployeeData> BaseEmployeeList, DayTemplateData InShiftTemplate, DateTime DateToGatherFor)
        {
            List<EmployeeAutomation> WeighedEmployees = new List<EmployeeAutomation>();
            double veryLargeNumber = 1000000;

            foreach (EmployeeData ED in BaseEmployeeList)
            {
                EmployeeAutomation NewAutoData = new EmployeeAutomation();
                bool bAlreadyWorksToday =   HasMAShiftThisDay(DateToGatherFor, ED);
                NewAutoData.Employee = ED;
                int DaysWorkedBefore = 0;
                int DaysWorkingAfter = 0;
                
                for (int i = 1; i <= 15;  i++)
                {
                    if(HasMAShiftThisDay(DateToGatherFor.AddDays(-i), ED) )
                    {
                        DaysWorkedBefore++;
                    }
                    else break;

                }
                for (int i = 1; i <= 15; i++)
                {
                    if (HasMAShiftThisDay(DateToGatherFor.AddDays(i), ED))
                    {
                        DaysWorkingAfter++;
                    }
                    else break;
                }
                if(DaysWorkingAfter > 0)
                {
                    SchichtInfo? NextShift;
                    HasMAShiftThisDay(DateToGatherFor.AddDays(1), ED, out NextShift);
                    if (NextShift != null)
                    {
                        DateTime DayAfter = DateToGatherFor.AddDays(1);
                        DayAfter=DayAfter.AddHours((int)(NextShift.Zeiten.SchichtEnde / 60));
                        DayAfter=DayAfter.AddMinutes((int)(NextShift.Zeiten.SchichtEnde % 60));
                        DateTime CurrentDay = DateToGatherFor;
                        CurrentDay = CurrentDay.AddHours((int)(InShiftTemplate.Zeiten.SchichtStart / 60));
                        CurrentDay = CurrentDay.AddMinutes((int)(InShiftTemplate.Zeiten.SchichtStart % 60));
                        double hours = (DayAfter - CurrentDay).TotalHours;
                        if (hours < 11) continue; //unterschreitet ruhezeit
                    }
                }
                if(DaysWorkedBefore > 0)
                {
                    SchichtInfo? LastShift;
                    HasMAShiftThisDay(DateToGatherFor.AddDays(-1), ED, out LastShift);
                    if (LastShift != null)
                    {
                        DateTime DayBefore = DateToGatherFor.AddDays(-1);
                        DayBefore=DayBefore.AddHours((int)(LastShift.Zeiten.SchichtEnde / 60));
                        DayBefore=DayBefore.AddMinutes((int)(LastShift.Zeiten.SchichtEnde % 60));
                        DateTime CurrentDay = DateToGatherFor;
                        CurrentDay =  CurrentDay.AddHours((int)(InShiftTemplate.Zeiten.SchichtStart / 60));
                        CurrentDay = CurrentDay.AddMinutes((int)(InShiftTemplate.Zeiten.SchichtStart % 60));
                        double hours = (CurrentDay - DayBefore).TotalHours;
                        if (hours < 11) continue; //unterschreitet ruhezeit
                    }
                }

             

                double workloadRatio = ED._VerplanteStunden / ED._ZielStunden;
                if (bAlreadyWorksToday) continue;
                if(ED._ZielStunden > 0)
                {
                    NewAutoData.Weight += (int)(workloadRatio * 130);
                }
                else NewAutoData.Weight += veryLargeNumber;

                int totalWorkingDays = (DaysWorkedBefore + DaysWorkingAfter + 1);
                if (totalWorkingDays > ED.MaxArbeitsTageAmStueck)
                {
                    NewAutoData.Weight += ((totalWorkingDays - ED.MaxArbeitsTageAmStueck) * 10);
                }else if(totalWorkingDays == 1)
                {
                    NewAutoData.Weight += 5;
                }
                else if(totalWorkingDays <= 3)
                {
                    NewAutoData.Weight -= 10;
                }
                else { NewAutoData.Weight -= 5; }

                if (ED._VorgeseheneRollen.Count > 1) NewAutoData.Weight += 5;

                if (IsDayOff(ED.FreitagsWuensche, DateToGatherFor.Day)) NewAutoData.Weight += 25;
                else NewAutoData.Weight -= ED.FreitagsWuensche.Count * 5;
                if (IsDayOff(ED.Einsatzwuensche, DateToGatherFor.Day)) NewAutoData.Weight -= 15;
                NewAutoData.Weight -= ED.AbwesendListeNew.Count * 2;
                NewAutoData.Weight += ED._Standorte.Count * 7;


                NewAutoData.Weight += Random.Shared.NextDouble();

                WeighedEmployees.Add(NewAutoData);
            }


            WeighedEmployees = WeighedEmployees.OrderBy(EAD => EAD.Weight).ToList();
            return WeighedEmployees;
        }

        //Layout
        private bool LeftBorderOpen = true;
        private bool RightBorderOpen = true;
        private void ClapInLeft(object sender, RoutedEventArgs e)
        {
            if (LeftBorderOpen)
            {
                LeftBorderOpen = false;
                LeftBorder.Visibility = Visibility.Collapsed;
                LeftRowB.Content = ">";
            }
            else
            {
                LeftBorderOpen = true;
                LeftBorder.Visibility = Visibility.Visible;
                LeftRowB.Content = "<";
            }
        }
        private void ClapInRight(object sender, RoutedEventArgs e)
        {
            if (RightBorderOpen)
            {
                RightBorderOpen = false;
                RightBorder.Visibility = Visibility.Collapsed;
                RightRowB.Content = "<";
            }
            else
            {
                RightBorderOpen = true;
                RightBorder.Visibility = Visibility.Visible;
                RightRowB.Content = ">";
            }
        }
        private void Calendar_Click(object sender, RoutedEventArgs e)
        {

            CalendarBorder.Visibility = Visibility.Visible;
            WeekDayList.Visibility = Visibility.Collapsed;
            _WeekViewActive = false;
            _WochenTage.Clear();
            if (_ActiveStandortID != -1) SwitchGenerator();
        }
        private void WeekView_Click(object sender, RoutedEventArgs e)
        {

            CalendarBorder.Visibility = Visibility.Collapsed;
            WeekDayList.Visibility = Visibility.Visible;
            _WeekViewActive = true;
            _KalenderTage.Clear();
            if (_ActiveStandortID != -1) SwitchGenerator();
        }
        private void PreviousMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(-1);
            MonthTitleText.Text = _MonthMapping[_currentMonth.Month] + " " + _currentMonth.Year.ToString();
            foreach (EmployeeData Arbeitstier in _Mitartbeiter)
            {
                Arbeitstier.AbwesendDaten.TryGetValue(_currentMonth, out List<TagesWunsch>? OutDayList);
                if (OutDayList != null)
                {

                    Arbeitstier.AbwesendListeNew = new List<TagesWunsch>(OutDayList);
                    Arbeitstier.AbwesendString = BuildNewDayString(Arbeitstier.AbwesendListeNew);

                }
                else
                {
                        Arbeitstier.AbwesendListeNew.Clear();
                        Arbeitstier.AbwesendString = "";
                    
                }

                Arbeitstier.EinsatzDaten.TryGetValue(_currentMonth, out List<TagesWunsch>? OutFWDayList);
                if (OutFWDayList != null)
                {
                  

                        Arbeitstier.Einsatzwuensche = new List<TagesWunsch>(OutFWDayList);
                    Arbeitstier.EinsatzwunschString = BuildNewDayString( Arbeitstier.Einsatzwuensche);


                }
                else
                {
                  
                        Arbeitstier.Einsatzwuensche.Clear();
                        Arbeitstier.EinsatzwunschString = "";
                    
                }

                Arbeitstier.FreitagsDaten.TryGetValue(_currentMonth, out List<TagesWunsch>? OutEWDayList);
                if (OutEWDayList != null)
                {
                 
                        Arbeitstier.FreitagsWuensche = new List<TagesWunsch>(OutEWDayList);
                    Arbeitstier.FreitagWunschString=  BuildNewDayString( Arbeitstier.FreitagsWuensche);

                }
                else
                {
                  
                        Arbeitstier.FreitagsWuensche.Clear();
                        Arbeitstier.FreitagWunschString = "";
                    
                }


            }
            if (_ActiveStandortID >= 0) SwitchGenerator();
            RecalcPlannedHours();
        }

        private static string BuildNewDayString( List<TagesWunsch> DayListToGet)
        {
            string DayStrings = "mo,di,mi,do,fr,sa,so";
            string InStringToBuild = "";
            bool AddDescription = false;
            int StartDay = 0;
            for (int i = 0; i < DayListToGet.Count(); i++)
            {
                TagesWunsch TW = DayListToGet[i];
                if (DayStrings.Contains(TW.Tag.ToLower()))
                {
                    InStringToBuild += $" {TW.Tag}";
                    AddDescription = true;
                }
                else
                {

                    if (StartDay == 0)
                    {
                        int Start = int.Parse(TW.Tag.Trim());

                        if (i + 1 < DayListToGet.Count())
                        {
                            TagesWunsch NTW = DayListToGet[i + 1];
                            int end = int.Parse(NTW.Tag.Trim());

                            if (Start + 1 == end)
                            {
                                StartDay = Start;
                            }
                            else
                            {
                                InStringToBuild += $" {TW.Tag}";
                                AddDescription = true;
                            }

                        }
                        else
                        {
                            InStringToBuild += $" {TW.Tag}";
                            AddDescription = true;
                        }


                    }
                    else
                    {
                        int Followday = int.Parse(TW.Tag.Trim());

                        if (i + 1 < DayListToGet.Count())
                        {
                            TagesWunsch NTW = DayListToGet[i + 1];
                            int end = int.Parse(NTW.Tag.Trim());

                            if (Followday + 1 == end)
                            {
                                continue;
                            }
                            else
                            {
                                InStringToBuild += $" {StartDay.ToString()}-{TW.Tag}";
                                AddDescription = true;
                            }

                        }
                        else
                        {
                            InStringToBuild += $" {StartDay.ToString()}-{TW.Tag}";
                            AddDescription = true;
                        }


                    }
                }


                if (AddDescription)
                {
                    if (!string.IsNullOrEmpty(TW.Typ))
                    {

                        InStringToBuild += $"({TW.Typ}";
                        if (!string.IsNullOrEmpty(TW.TypAbbreviation))
                        {
                            InStringToBuild += $"[{TW.TypAbbreviation}]";
                        }

                        InStringToBuild += ")";

                    }

                    InStringToBuild += ",";
                    StartDay = 0;
                    AddDescription = false;
                }

            }
            char Komma = ',';
            InStringToBuild = InStringToBuild.Trim(Komma);
            return InStringToBuild;
        }

        private void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(1);
            MonthTitleText.Text = _MonthMapping[_currentMonth.Month] + " " + _currentMonth.Year.ToString();


            foreach (EmployeeData Arbeitstier in _Mitartbeiter)
            {
                Arbeitstier.AbwesendDaten.TryGetValue(_currentMonth, out List<TagesWunsch>? OutDayList);
                if (OutDayList != null)
                {
                    if(Arbeitstier.TransferAbwesenheitOverMonths && OutDayList.Count == 0)
                    {
                        Arbeitstier.AbwesendDaten[_currentMonth] = new List<TagesWunsch>(Arbeitstier.AbwesendListeNew);
                    }
                    else
                    {
                        Arbeitstier.AbwesendListeNew = new List<TagesWunsch>(OutDayList);
                        Arbeitstier.AbwesendString = BuildNewDayString(Arbeitstier.AbwesendListeNew);
                    }
                }
                else
                {
                    if (Arbeitstier.TransferAbwesenheitOverMonths)
                    {

                        Arbeitstier.AbwesendDaten.Add(_currentMonth, new List<TagesWunsch>(Arbeitstier.AbwesendListeNew));

                    }
                    else
                    {
                        Arbeitstier.AbwesendListeNew.Clear();
                        Arbeitstier.AbwesendString = "";
                    }
                }

                Arbeitstier.EinsatzDaten.TryGetValue(_currentMonth, out List<TagesWunsch>? OutFWDayList);
                if (OutFWDayList != null)
                {
                    if (Arbeitstier.TransferEinsatzwuenscheOverMonths && OutFWDayList.Count == 0)
                    {
                        Arbeitstier.EinsatzDaten[_currentMonth] = new List<TagesWunsch>(Arbeitstier.Einsatzwuensche);
                    }
                    else
                    {
                      
                        Arbeitstier.Einsatzwuensche = new List<TagesWunsch>(OutFWDayList);
                        Arbeitstier.EinsatzwunschString = BuildNewDayString( Arbeitstier.Einsatzwuensche);

                    }
                }
                else
                { 
                    if (Arbeitstier.TransferEinsatzwuenscheOverMonths)
                    {

                        Arbeitstier.EinsatzDaten.Add(_currentMonth, new List<TagesWunsch>(Arbeitstier.Einsatzwuensche));
                    }
                    else
                    {
                        Arbeitstier.Einsatzwuensche.Clear();
                        Arbeitstier.EinsatzwunschString = "";
                    }
                }

                Arbeitstier.FreitagsDaten.TryGetValue(_currentMonth, out List<TagesWunsch>? OutEWDayList);
                if (OutEWDayList != null)
                {
                    if (Arbeitstier.TransferFreitagWunschOverMonths && OutEWDayList.Count == 0)
                    {
                        Arbeitstier.FreitagsDaten[_currentMonth] = new List<TagesWunsch>(Arbeitstier.FreitagsWuensche);
                    }
                    else
                    {
                        Arbeitstier.FreitagsWuensche = new List<TagesWunsch>(OutEWDayList);
                        Arbeitstier.FreitagWunschString = BuildNewDayString( Arbeitstier.FreitagsWuensche);
                    }
                }
                else
                {
                    if (Arbeitstier.TransferFreitagWunschOverMonths)
                    {

                        Arbeitstier.FreitagsDaten.Add(_currentMonth, new List<TagesWunsch>(Arbeitstier.FreitagsWuensche));

                    }
                    else
                    {
                        Arbeitstier.FreitagsWuensche.Clear();
                        Arbeitstier.FreitagWunschString = "";
                    }
                }
   

            }

            if (_ActiveStandortID >= 0) SwitchGenerator();
            RecalcPlannedHours();
        }

        //Standort

        private void AddStandort_Click(object sender, RoutedEventArgs e)
        {
            StandortCreater window = new StandortCreater();
            window.Owner = this;
            window.Erstellt += ReceiveStandort;
            bool? result = window.ShowDialog();
        }
        private void ReceiveStandort(string? InStandort)
        {
            if (InStandort == null) return;

            PlanStandortData NewLocation = new PlanStandortData();

            Dictionary<string, string?> NameTokuerzel = GetFullNameAndKuerzel(InStandort);

            foreach (var pair in NameTokuerzel)
            {
                NewLocation.StandortName = pair.Key;
                if (string.IsNullOrWhiteSpace(pair.Value))
                {
                    NewLocation.StandortKuerzel = string.Empty;
                }
                else
                {
                    NewLocation.StandortKuerzel = pair.Value;
                }

                foreach (PlanStandortData LStandort in _Standorte)
                {
                    if (NewLocation.StandortName.ToLower() == LStandort.StandortName.ToLower())
                    {
                        MessageBox.Show($"Ein Standort mit dem Namen {NewLocation.StandortName} existiert schon.");
                        return;
                    }
                }

                PlanStandort StandortWidget = new PlanStandort();
                StandortWidget.LinkedStandortID = _StandortIDCounter;
                StandortWidget.SetOrtName(NewLocation.StandortName);
                StandortWidget.ClickedLeft += SetStandortActive;
                StandortWidget.ClickedRight += OpenStandortInfo;

                NewLocation.PlanStandortId = _StandortIDCounter;
                _Standorte.Add(NewLocation);
                _StandortWidgetMap.Add(_StandortIDCounter, StandortWidget);
                StandortBox.Children.Add(StandortWidget);

                _StandortIDCounter++;

                SetStandortActive(StandortWidget);

                UnsavedChanges = true;
            }
        }
        private void RemoveStandort_Click(object sender, RoutedEventArgs e)
        {

            if(_ActiveStandortID == -1)
            {
                MessageBox.Show($"Bitte wähle den Standort der gelöscht werden soll.");
                return;
            }

            foreach (EmployeeData ED in _Mitartbeiter)
            {
                if(ED._Standorte.Contains(_ActiveStandortID))
                {
                    MessageBox.Show($"Bitte entferne zuerst alle Mitarbeiter aus dem Standort.");
                    return;
                }
            }

            if (GetStandort(_ActiveStandortID) is PlanStandortData PSD) { _Standorte.Remove(PSD); }
            StandortBox.Children.Remove(_StandortWidgetMap[_ActiveStandortID]);
            _StandortWidgetMap.Remove(_ActiveStandortID);
            _KalenderTage.Clear();
            LocationText.Text = string.Empty;

            UnsavedChanges = true;
        }
        private void SetStandortActive(PlanStandort? InStandortWidget)
        {
            if (InStandortWidget == null) return;

            if (_ActiveStandortID >= 0)
            {
                if (GetStandort(_ActiveStandortID) is PlanStandortData LocalStandortInfo)
                {
                    LocalStandortInfo.IstSelektiert = false;
                }
                _StandortWidgetMap.TryGetValue(_ActiveStandortID, out PlanStandort? OutStandort);
                if (OutStandort != null)
                {
                    OutStandort.SetSelected(false);
                }
            }

            _ActiveStandortID = InStandortWidget.LinkedStandortID;
            InStandortWidget.SetSelected(true);
            if (GetStandort(_ActiveStandortID) is PlanStandortData LocalStandortInfoSecond)
            {
                LocalStandortInfoSecond.IstSelektiert = true;
                LocationText.Text = LocalStandortInfoSecond.StandortName;
                SwitchGenerator();
                RecalcMitarberiterView(MAHaken);
            }
        }
        private void OpenStandortInfo(PlanStandort? InStandortWidget)
        {
            if (InStandortWidget == null) return;

            if(GetStandort(InStandortWidget.LinkedStandortID) is PlanStandortData STInfo)
            {
                Standorteditor NewEditor = new Standorteditor();
                NewEditor.Owner = this;
                NewEditor.LinkedID = InStandortWidget.LinkedStandortID;
                NewEditor.StandortName.Text = STInfo.StandortName;
                NewEditor.StandortNameEdit.Text = STInfo.StandortName;
                NewEditor.StandortKuerzel.Text = STInfo.StandortKuerzel;
                NewEditor.StandortKuerzelEdit.Text = STInfo.StandortKuerzel;
                NewEditor.SchliesstagBox.Text = STInfo.SchliessTage;
                NewEditor.Schliesstage.Text = STInfo.SchliessTage;
                NewEditor._Bundesland = STInfo.Bundesland;
                NewEditor.CBClosedOnHoliday.IsChecked = STInfo.bIsClosedOnHoliday;
                NewEditor.BundeslaenderWahl.SelectedItem = STInfo.Bundesland;
                NewEditor.SaveSTChanges += SaveStandortInfo;
                NewEditor.DeleteAllShifts += LoescheAlleStandortSchichtenDesMonat;
                bool? result = NewEditor.ShowDialog();

                if (result != null)
                {
                    NewEditor.SaveSTChanges -= SaveStandortInfo;
                    NewEditor.DeleteAllShifts -= LoescheAlleStandortSchichtenDesMonat;
                }
            }
        }
        private void SaveStandortInfo(StandortSave STSave, int StandortID)
        {
            if (GetStandort(StandortID) is PlanStandortData STInfo)
            {

                STInfo.StandortName = STSave.NewName;
                STInfo.SchliessTage = STSave.SchliesstagString;
                STInfo.SchliessTageList = STSave.NewSchliessTage;
                STInfo.StandortKuerzel = STSave.NewKuerzel;
                STInfo.Bundesland = STSave.STBundesland;
                STInfo.bIsClosedOnHoliday = STSave.bClosedOnHoliday;
                _StandortWidgetMap[StandortID].NameText.Text = STSave.NewName;

                if (_ActiveStandortID == StandortID)
                {
                    LocationText.Text = STInfo.StandortName;
                    SwitchGenerator();
                }
            }
        }
        public string GetStandortKuerzel(int SID)
        {

            if (GetStandort(SID) is PlanStandortData PSD)
            {
                return string.IsNullOrWhiteSpace(PSD.StandortKuerzel) ? PSD.StandortName : PSD.StandortKuerzel;
            }


            return string.Empty;
        }
        public void LoescheAlleStandortSchichten(int SID)
        {
            if (GetStandort(SID) is PlanStandortData STInfo)
            {
                foreach(int  MID in STInfo.MAIDs)
                {
                    if (GetMitarbeiter(MID) is EmployeeData Arbeiter)
                    {
                        List<int> ShiftsToDelete = new List<int>();
                        foreach (int SchiftID in Arbeiter._ZugeteilteSchichten) 
                        {
                            if (GetSchicht(SchiftID) is SchichtInfo SelectedSchicht)
                            {
                                if(SelectedSchicht.SLinkedID == SID) ShiftsToDelete.Add(SchiftID);
                            }
                        }
                        ClearSelectedShiftsFromEmployee(ShiftsToDelete,MID);    
                    }
                }

            }

            SwitchGenerator();
        }

        public void LoescheAlleStandortSchichtenDesMonat(int SID)
        {
            if (GetStandort(SID) is PlanStandortData STInfo)
            {
                foreach (int MID in STInfo.MAIDs)
                {
                    if (GetMitarbeiter(MID) is EmployeeData Arbeiter)
                    {
                        List<int> ShiftsToDelete = new List<int>();
                        foreach (int SchiftID in Arbeiter._ZugeteilteSchichten)
                        {
                            if (GetSchicht(SchiftID) is SchichtInfo SelectedSchicht)
                            {
                                if (SelectedSchicht.SLinkedID == SID && SelectedSchicht.Date.Year == _currentMonth.Year && SelectedSchicht.Date.Month == _currentMonth.Month) ShiftsToDelete.Add(SchiftID);
                            }
                        }
                        ClearSelectedShiftsFromEmployee(ShiftsToDelete, MID);
                    }
                }

            }

            SwitchGenerator();
        }
        public PlanStandortData? GetStandort(int InStandortID)
        {

            foreach (var item in _Standorte)
            {
                if (item.PlanStandortId == InStandortID) return item;
            }
            return null;
        }


        public List<SchichtInfo> GetShiftsForLocation(int InStandortID)
        {
            List < SchichtInfo > Standortschichten = new List<SchichtInfo >();
            foreach (SchichtInfo schicht in _Schichten)
            {
                if (schicht.SLinkedID == InStandortID && schicht.Date.Year == _currentMonth.Year && schicht.Date.Month == _currentMonth.Month) Standortschichten.Add(schicht);
            }


            return Standortschichten;
        }


        //Mitarbeiter 
        private void AddEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (_Standorte.Count < 1)
            {
                MessageBox.Show("Bitte lege mindestens einen Standort an bevor du einen Mitarbeiter anlegst.");
                return;
            }

            EmployeeCreater window = new EmployeeCreater();
            window.Owner = this;

            Dictionary<string, int> Orte = new();
            foreach (PlanStandortData PSD in _Standorte)
            {
                Orte.Add(PSD.StandortName, PSD.PlanStandortId);
                if (PSD.PlanStandortId == _ActiveStandortID)
                {
                    window.Locations.SelectedItems.Add(PSD.StandortName);
                }
            }

            window.SetStandortSelection(Orte);

            window.Erstellt += ReceiveEmployee;
            bool? result = window.ShowDialog();
            if (result != null)
            {
                window.Erstellt -= ReceiveEmployee;
            }


        }
        private void ReceiveEmployee(EmployeeData? InEmployee, List<string> OrtList)
        {
            if (InEmployee == null) return;

            foreach (EmployeeData Arbeiter in _Mitartbeiter)
            {
                if (InEmployee._MitarbeiterName.ToLower() == Arbeiter._MitarbeiterName.ToLower())
                {
                    MessageBox.Show($"Ein Mitarbeiter mit dem Namen {InEmployee._MitarbeiterName} existiert schon.");
                    return;
                }
            }
            Employee employeWidget = new Employee();
            employeWidget.SetMitarbeiterName(InEmployee._MitarbeiterName);
            employeWidget.SetRolls(InEmployee._VorgeseheneRollen);
            employeWidget.SetHours(InEmployee._VerplanteStunden, InEmployee._ZielStunden);
            employeWidget._LinkedMitarbeiterID = _MitarbeiterIDCounter;
            employeWidget.Clicked += SetEmployeeActive;
            employeWidget.RightClicked += OpenMAInfo;

            InEmployee._MitarbeiterID = _MitarbeiterIDCounter;
            _Mitartbeiter.Add(InEmployee);

            foreach (var item in InEmployee._Standorte)
            {
                if (GetStandort(item) is PlanStandortData PlanStandort)
                {
                    PlanStandort.MAIDs.Add(InEmployee._MitarbeiterID);
                }
            }

            _MitarbeiterWidgetMap.Add(_MitarbeiterIDCounter, employeWidget);
            SortierMitarbeiter(Sortierungen);

            _MitarbeiterIDCounter++;
            UnsavedChanges = true;
        }
        private void RemoveEmployee_Click(object sender, RoutedEventArgs e)
        {

            if (_ActiveEmployee == null)
            {
                MessageBox.Show("Du musst einen Mitarbeiter ausgewählt haben um ihn zu entfernen.");
                return;
            }

            BestätigungsWidget window = new BestätigungsWidget();
            window.Owner = this;
            string InfoText = $"Bist du dir sicher, dass du den Mitarbeiter {_ActiveEmployee._MitarbeiterName} löschen möchtest?";
            window.SetInfoText(InfoText);
            bool? result = window.ShowDialog();


            if (result != null && (bool)result)
            {

                _MitarbeiterWidgetMap.TryGetValue(_ActiveEmployee._MitarbeiterID, out Employee? OutEmployee);
                if (OutEmployee != null)
                {
                    OutEmployee.Clicked -= SetEmployeeActive;
                    OutEmployee.RightClicked -= OpenMAInfo;
                    EmployeeBox.Children.Remove(OutEmployee);
                    _MitarbeiterWidgetMap.Remove(_ActiveEmployee._MitarbeiterID);
                }

                string LocalName = _ActiveEmployee._MitarbeiterName;

                foreach (int LSchichtID in _ActiveEmployee._ZugeteilteSchichten)
                {
                    if (GetSchicht(LSchichtID) is SchichtInfo LocalSchichtInfo)
                    {
                        _Schichten.Remove(LocalSchichtInfo!);
                    }
                }

                foreach (var item in _ActiveEmployee._Standorte)
                {
                    if (GetStandort(item) is PlanStandortData PlanStandort)
                    {
                        PlanStandort.MAIDs.Remove(_ActiveEmployee._MitarbeiterID);
                    }
                }

                _Mitartbeiter.Remove(_ActiveEmployee);

                if (_ActiveEmployee._ZugeteilteSchichten.Count > 0) //Wenn der Mitarbeiter min eine Schicht hatte mach den Kalender neu
                {
                    SwitchGenerator();
                }

                _ActiveEmployee = null;

                MessageBox.Show($"Der Mitarbeiter {LocalName} wurde entfernt.");
                UnsavedChanges = true;
            }

        }
        private void SetEmployeeActive(Employee? InEmployeeWidget)
        {
            if (InEmployeeWidget == null) return;

            if (_ActiveEmployee != null)
            {
                _ActiveEmployee.IstSelektiert = false;
                _MitarbeiterWidgetMap.TryGetValue(_ActiveEmployee._MitarbeiterID, out Employee? OutEmployee);
                if (OutEmployee != null)
                {
                    OutEmployee.SetSelected(false);
                }
                RemoveMAChangesFromDays(_ActiveEmployee);

                if (_ActiveEmployee._MitarbeiterID == InEmployeeWidget._LinkedMitarbeiterID)
                {
                   
                    _ActiveEmployee = null;
                    return;
                }
            }

            foreach (EmployeeData Arbeitstier in _Mitartbeiter)
            {
                if (InEmployeeWidget._LinkedMitarbeiterID == Arbeitstier._MitarbeiterID)
                {
                    _ActiveEmployee = Arbeitstier;
                    AddMAChangesToDays(_ActiveEmployee);
                    _ActiveEmployee.IstSelektiert = true;
                    InEmployeeWidget.SetSelected(true);
                    InEmployeeWidget.BringIntoView();
                    break;
                }
            }
        }
        public void AddMAChangesToDays(EmployeeData InEmployee)
        {
            foreach(KalenderTag KT in _KalenderTage)
            {

                KT.ActiveInfoBorder.Background = Brushes.Black;
                KT.ActiveMAInfo.Foreground = Brushes.White;
                string? Type;
                string? TypeAB;
                bool bFoundDay = IsDayOff(InEmployee.AbwesendListeNew, KT.DayData._KalenderDatum.Day,out Type,out TypeAB);
                if (bFoundDay)
                {
                    KT.DayData.NotAvailableForSelectedMA = true;
                    KT.RootBorder.Background = _ClosedDayBrush;
                    KT.RootBorder.Opacity = _ClosedDayOpacity;
                    KT.ActiveInfoBorder.Visibility = Visibility.Visible;

                    if(Type == null || string.IsNullOrWhiteSpace(Type))
                    {
                        KT.ActiveMAInfo.Text = "Abwesend";
                    }
                    else
                    {
                        KT.ActiveMAInfo.Text = Type;
                    }

                        continue;
                }

                if (IsDayOff(InEmployee.FreitagsWuensche, KT.DayData._KalenderDatum.Day))
                {
                    KT.ActiveInfoBorder.Visibility = Visibility.Visible;
                    KT.ActiveInfoBorder.Background = Brushes.Red;
                    KT.ActiveMAInfo.Text = "Freizeitwunsch";
                }

                if (IsDayOff(InEmployee.Einsatzwuensche, KT.DayData._KalenderDatum.Day))
                {
                    KT.ActiveInfoBorder.Visibility = Visibility.Visible;
                    KT.ActiveInfoBorder.Background = Brushes.GreenYellow;
                    KT.ActiveMAInfo.Foreground = Brushes.Black;
                    KT.ActiveMAInfo.Text = "Einsatzwunsch";

                }

                foreach (int SchichtID in InEmployee._ZugeteilteSchichten)
                {
                    if (GetSchicht(SchichtID) is SchichtInfo SInfo && SInfo.Date == KT.DayData._KalenderDatum)
                    {
                        KT.DayData.NotAvailableForSelectedMA = true;
                        KT.RootBorder.Background = Brushes.BlueViolet;
                        KT.RootBorder.Opacity = 0.4;
                        KT.ActiveInfoBorder.Visibility = Visibility.Visible;

                        string AInfo = "";
                        if(_UseStandortKuerzel)
                        {
                            AInfo += GetStandortKuerzel(SInfo.SLinkedID);
                        }
                        else
                        { 
                            if(GetStandort(SInfo.SLinkedID) is PlanStandortData PSD)
                            {
                                AInfo += PSD.StandortName;
                            }

                        }

                        if (_UseRoleKuerzel)
                        {
                            AInfo += "/" + GetRoleKuerzel(SInfo.SchichtRolle);
                        }
                        else
                        {

                            AInfo += "/" + SInfo.SchichtRolle;
                        }

                        if (SInfo.Zeiten.bPlusOneDay) AInfo += " +1";

                        KT.ActiveMAInfo.Text = AInfo;

                    }
                }
            }
            foreach (WochenTag WT in _WochenTage)
            {

                bool bFoundDay = IsDayOff(InEmployee.AbwesendListeNew, WT.DayData._KalenderDatum.Day);
                if (bFoundDay)
                {
                    WT.DayData.NotAvailableForSelectedMA = true;
                    WT.RootBorder.Background = _ClosedDayBrush;
                    WT.RootBorder.Opacity = _ClosedDayOpacity;
                    continue;
                }

                foreach (int SchichtID in InEmployee._ZugeteilteSchichten)
                {
                    if (GetSchicht(SchichtID) is SchichtInfo SInfo && SInfo.Date == WT.DayData._KalenderDatum)
                    {
                        WT.DayData.NotAvailableForSelectedMA = true;
                        WT.RootBorder.Background = Brushes.BlueViolet;
                        WT.RootBorder.Opacity = _ClosedDayOpacity;
                    }
                }

           
            }

        }

        private bool IsDayOff(List<TagesWunsch> Abwesenheiten, int Date)
        {

            string Day = Date.ToString();
            string DayAbbreviation = _DayMapping[Date].ToLower();
            bool bFoundDay = false;
            foreach (TagesWunsch EPDaysOff in Abwesenheiten)
            {
                if (EPDaysOff.Tag == Day || EPDaysOff.Tag == DayAbbreviation)
                {
                    bFoundDay = true;
                    break;
                }
            }

            return bFoundDay;
        }

        private bool IsDayOff(List<TagesWunsch> Abwesenheiten, int Date, out string? Type, out string? TypeAbbreviation)
        {

            string Day = Date.ToString();
            string DayAbbreviation = _DayMapping[Date].ToLower();
            bool bFoundDay = false;
            Type = null;
            TypeAbbreviation = null;
            foreach (TagesWunsch EPDaysOff in Abwesenheiten)
            {
                if (EPDaysOff.Tag == Day || EPDaysOff.Tag == DayAbbreviation)
                {
                    bFoundDay = true;
                    Type = EPDaysOff.Typ;
                    TypeAbbreviation = EPDaysOff.TypAbbreviation;
                    break;
                }
            }
            
            return bFoundDay;
        }

        public void RemoveMAChangesFromDays(EmployeeData InEmployee)
        {

            foreach (KalenderTag KT in _KalenderTage)
            {
               
                KT.DayData.NotAvailableForSelectedMA = false;
                if (KT.DayData.NotAvailableForSelection)
                {
                    KT.RootBorder.Background = _ClosedDayBrush;
                }
                else
                {
                    KT.RootBorder.Opacity = 1;
                    KT.RootBorder.Background = Brushes.Transparent;
                }
                KT.ActiveInfoBorder.Visibility = Visibility.Collapsed;
                KT.ActiveInfoBorder.Background = Brushes.Black;
                KT.ActiveMAInfo.Foreground = Brushes.White;


            }
            foreach (WochenTag WT in _WochenTage)
            {
                WT.DayData.NotAvailableForSelectedMA = false;
                if (WT.DayData.NotAvailableForSelection)
                {
                    WT.RootBorder.Background = _ClosedDayBrush;
                }
                else
                {
                    WT.RootBorder.Opacity = 1;
                    WT.RootBorder.Background = Brushes.Transparent;
                }
            }
        }
        int GetMASchichtZahlen(List<int> Schichten, out Dictionary<string, int> RtoS)
        {
            int OutCount = 0;
            RtoS = new Dictionary<string, int>();
            foreach (int i in Schichten)
            {
                if(GetSchicht(i) is SchichtInfo SI)
                {
                    if (SI.Date.Month == _currentMonth.Month && SI.Date.Year == _currentMonth.Year)
                    {
                        OutCount++;

                        if (RtoS.TryGetValue(SI.SchichtRolle, out int count))
                        {
                            RtoS[SI.SchichtRolle]++;
                        }
                        else
                        {
                            RtoS.Add(SI.SchichtRolle, 1);
                        }

                    }
                }
            }

            return OutCount;
        }
        private void OpenMAInfo(Employee? InEmployeeWidget)
        {
            if (InEmployeeWidget == null) return;

            if (GetMitarbeiter(InEmployeeWidget._LinkedMitarbeiterID) is EmployeeData Arbeiter)
            {
                _MitarbeiterInfoCache = new MitarbeiterInfo();
                _MitarbeiterInfoCache.Owner = this;

                _MitarbeiterInfoCache.MAName.Text = $"{Arbeiter._MitarbeiterName}";
                _MitarbeiterInfoCache.MANameEdit.Text = $"{Arbeiter._MitarbeiterName}";
                _MitarbeiterInfoCache.MAID = Arbeiter._MitarbeiterID;
                _MitarbeiterInfoCache.ZielStundenText.Text = $"{Arbeiter._ZielStunden.ToString()}";
                _MitarbeiterInfoCache.ZielStundenBox.Text = $"{Arbeiter._ZielStunden.ToString()}";
                _MitarbeiterInfoCache.PlannedStundenText.Text = $"Verplante Stunden:  {Arbeiter._VerplanteStunden.ToString()}";
                _MitarbeiterInfoCache.MyColorPicker.SelectedColor = (System.Windows.Media.Color)ColorConverter.ConvertFromString(Arbeiter.ColorHex);
                _MitarbeiterInfoCache.AbwesendeTage.Text = Arbeiter.AbwesendString;
                _MitarbeiterInfoCache.abwesendBox.Text = Arbeiter.AbwesendString;
                _MitarbeiterInfoCache.FreizeitBox.Text = Arbeiter.FreitagWunschString;
                _MitarbeiterInfoCache.FreizeitTage.Text = Arbeiter.FreitagWunschString;
                _MitarbeiterInfoCache.EinsatzBox.Text = Arbeiter.EinsatzwunschString;
                _MitarbeiterInfoCache.EinsatzTage.Text = Arbeiter.EinsatzwunschString;
                _MitarbeiterInfoCache.TageAmStueck.Text = Arbeiter.MaxArbeitsTageAmStueck.ToString();
                _MitarbeiterInfoCache.TageAmStueckBox.Text = Arbeiter.MaxArbeitsTageAmStueck.ToString();
                _MitarbeiterInfoCache.CarryOverAbwesenheiten.IsChecked = Arbeiter.TransferAbwesenheitOverMonths;
                _MitarbeiterInfoCache.CarryOverEinsaetze.IsChecked = Arbeiter.TransferEinsatzwuenscheOverMonths;
                _MitarbeiterInfoCache.CarryOverFreizeit.IsChecked = Arbeiter.TransferFreitagWunschOverMonths;
                string STText = "";
                int Index = 0;
                foreach (PlanStandortData Ort in _Standorte)
                {
                    _MitarbeiterInfoCache.Locations.Items.Add(Ort.StandortName);
                    _MitarbeiterInfoCache.OrtCache.Add(Ort.StandortName, Ort.PlanStandortId);
                    if (Arbeiter._Standorte.Contains(Ort.PlanStandortId))
                    {
                        if (Index != 0) STText += ", ";
                        Index++;
                        STText += $"{Ort.StandortName}";
                        _MitarbeiterInfoCache.Locations.SelectedItems.Add(Ort.StandortName);
                    }
                }
                _MitarbeiterInfoCache.SchichtStandorte.Text = STText;

                string RText = "";
                Index = 0;
                foreach (RoleLabel RL in _Rollen)
                {

                    _MitarbeiterInfoCache.RoleSelection.Items.Add(RL.RoleData.RoleName);
                    if (Arbeiter._VorgeseheneRollen.Contains(RL.RoleData.RoleName))
                    {
                        if (Index != 0) RText += ", ";
                        Index++;
                        RText += $"{RL.RoleData.RoleName}";
                        _MitarbeiterInfoCache.RoleSelection.SelectedItems.Add(RL.RoleData.RoleName);
                    }
                }

            
                _MitarbeiterInfoCache.RText.Text = RText;
                _MitarbeiterInfoCache.EntferneSchichten += ClearSelectedShiftsFromEmployee;
                _MitarbeiterInfoCache.SaveMAChanges += SaveChangesToMA;
                _MitarbeiterInfoCache.ErstelleExport += Export_PersonalPDF;

                List<SchichtInfo> SortedList = _Schichten.OrderBy(Schicht => Schicht.Date).ToList();
                List<SchichtLabel> SubscribedLabels = new List<SchichtLabel>();
                bool BoxSwitch = false;

                int TotalCount = GetMASchichtZahlen(Arbeiter._ZugeteilteSchichten, out Dictionary<string, int>? RoleTocount);
                UpdateMASchichtInfos(TotalCount, RoleTocount);

                foreach (SchichtInfo assignment in SortedList)
                {
                    if (assignment.ELinkedID == Arbeiter._MitarbeiterID && assignment.Date.Month == _currentMonth.Month && assignment.Date.Year == _currentMonth.Year)
                    {
                        SchichtLabel Label = new SchichtLabel();
                        
                        Label.NameText.Text = $"{_Standorte[assignment.SLinkedID].StandortName} am {_DayMapping[assignment.Date.Day].ToString()}, {assignment.Date.Day.ToString("00")}.{assignment.Date.Month.ToString("00")}.{assignment.Date.Year}";
                        Label.StartZeit.Text = assignment.Zeiten.SchichtStartText;
                        Label.SchlussZeit.Text = assignment.Zeiten.SchichtSchlussText;
                        Label.RollenText.Text = assignment.SchichtRolle;
                        Label._LinkedSchichtID = assignment.SchichtID;
                        Label._LinkedOrtID = assignment.SLinkedID;
                        if(assignment.Zeiten.bPlusOneDay) Label.PlusDayText.Visibility = Visibility.Visible;
                        Label.RootBorder.Width = 350.0f;
                        Label.StartSchichtEdit += OpenSchichtEditor;
                        SubscribedLabels.Add(Label);
                      
                            _MitarbeiterInfoCache.RolePanel.Children.Add(Label);
                        
                     
                        BoxSwitch = !BoxSwitch;

                        if (_MitarbeiterInfoCache.SchichtTracker.TryGetValue(assignment.SLinkedID, out int SchichtCount))
                        {
                            _MitarbeiterInfoCache.SchichtTracker[assignment.SLinkedID] = SchichtCount + 1;

                        }
                        else { _MitarbeiterInfoCache.SchichtTracker.Add(assignment.SLinkedID, 1); }
                    }
                }
             
                bool? result = _MitarbeiterInfoCache.ShowDialog();
                if (result != null)
                {
                    foreach (SchichtLabel info in SubscribedLabels)
                    {
                        info.StartSchichtEdit -= OpenSchichtEditor;
                    }
                    _MitarbeiterInfoCache.EntferneSchichten -= ClearSelectedShiftsFromEmployee;
                    _MitarbeiterInfoCache.SaveMAChanges -= SaveChangesToMA;
                    _MitarbeiterInfoCache.ErstelleExport -= Export_PersonalPDF;

                    if (_MitarbeiterInfoCache.ShouldRedrawCalendar == true)
                    {
                        SwitchGenerator();
                    }

                    _MitarbeiterInfoCache = null;
                }
            }
        }

        private void SaveChangesToMA(MASaveChanges InChanges)
        {

            if (GetMitarbeiter(InChanges.MAToChange) is EmployeeData Arbeiter)
            {
                string STText = "";
                int index = 0;
                foreach (string Loc in InChanges.NeueStandorte)
                {
                    if (index != 0) STText += ", ";
                    STText += $"{Loc}";
                    index++;

                }

                string RText = "";
                index = 0;
                foreach (string R in InChanges.NewRoles)
                {
                    if (index != 0) RText += ", ";
                    index++;
                    RText += $"{R}";
                }

                if (_MitarbeiterInfoCache != null)
                {
                    _MitarbeiterInfoCache.SchichtStandorte.Text = STText;
                    _MitarbeiterInfoCache.RText.Text = RText;
                    _MitarbeiterInfoCache.MAName.Text = InChanges.NewName;
                    _MitarbeiterInfoCache.ZielStundenText.Text = InChanges.NeueZielStunden.ToString();
                    _MitarbeiterInfoCache.TageAmStueck.Text = InChanges.FolgeTage.ToString();

                }

                foreach (var item in Arbeiter._Standorte)
                {
                    if (GetStandort(item) is PlanStandortData PlanStandort)
                    {
                        PlanStandort.MAIDs.Remove(Arbeiter._MitarbeiterID);
                    }
                }
                Arbeiter._Standorte.Clear();
                Arbeiter._Standorte = InChanges.NeueStandortIDs;
                foreach (var item in Arbeiter._Standorte)
                {
                    if (GetStandort(item) is PlanStandortData PlanStandort)
                    {
                        PlanStandort.MAIDs.Add(Arbeiter._MitarbeiterID);
                    }
                }

                Arbeiter.SetZielStunden(InChanges.NeueZielStunden);
                Arbeiter.SetzeRollen(InChanges.NewRoles);
                Arbeiter.SetzeNeuenNamen(InChanges.NewName);
                Arbeiter.ColorHex = InChanges.NewColorHex;
                if (_MitarbeiterWidgetMap.TryGetValue(Arbeiter._MitarbeiterID, out Employee? OutEmployee))
                {
                    _MitarbeiterWidgetMap[Arbeiter._MitarbeiterID].SetHours(Arbeiter._VerplanteStunden, Arbeiter._ZielStunden);
                    _MitarbeiterWidgetMap[Arbeiter._MitarbeiterID].SetMitarbeiterName(InChanges.NewName);
                    _MitarbeiterWidgetMap[Arbeiter._MitarbeiterID].SetRolls(Arbeiter._VorgeseheneRollen);
                    System.Windows.Media.Color color = (System.Windows.Media.Color)ColorConverter.ConvertFromString(Arbeiter.ColorHex);
                    _MitarbeiterWidgetMap[Arbeiter._MitarbeiterID].RootBorder.BorderBrush = new SolidColorBrush(color);
                }


                Arbeiter.TransferAbwesenheitOverMonths = InChanges.TransferAbwesenheitOverMonths;
                Arbeiter.TransferEinsatzwuenscheOverMonths = InChanges.TransferEinsatzwuenscheOverMonths;
                Arbeiter.TransferFreitagWunschOverMonths = InChanges.TransferFreitagWunschOverMonths;

                Arbeiter.AbwesendString = InChanges.AbwesendString;
                Arbeiter.EinsatzwunschString = InChanges.EinsatzwunschString;
                Arbeiter.FreitagWunschString = InChanges.FreitagWunschString;
                Arbeiter.AbwesendListeNew = InChanges.AbwesendListe;
                Arbeiter.Einsatzwuensche = InChanges.Einsatzwuensche;
                Arbeiter.FreitagsWuensche = InChanges.FreitagsWuensche;
                Arbeiter.AbwesendDaten.TryGetValue(_currentMonth, out List<TagesWunsch>? OutDayList);
                if (OutDayList != null)
                {

                    Arbeiter.AbwesendDaten[_currentMonth] = new List<TagesWunsch>(InChanges.AbwesendListe);
                }
                else
                {
                    Arbeiter.AbwesendDaten.Add(_currentMonth, new List<TagesWunsch>(InChanges.AbwesendListe));
                }
                Arbeiter.FreitagsDaten.TryGetValue(_currentMonth, out List<TagesWunsch>? OutFWDayList);
                if (OutFWDayList != null)
                {

                    Arbeiter.FreitagsDaten[_currentMonth] = new List<TagesWunsch>(InChanges.FreitagsWuensche);
                }
                else
                {
                    Arbeiter.FreitagsDaten.Add(_currentMonth, new List<TagesWunsch>(InChanges.FreitagsWuensche));
                }
                Arbeiter.EinsatzDaten.TryGetValue(_currentMonth, out List<TagesWunsch>? OutEWDayList);
                if (OutEWDayList != null)
                {

                    Arbeiter.EinsatzDaten[_currentMonth] = new List<TagesWunsch>(InChanges.Einsatzwuensche);
                }
                else
                {
                    Arbeiter.EinsatzDaten.Add(_currentMonth, new List<TagesWunsch>(InChanges.Einsatzwuensche));
                }

                Arbeiter.MaxArbeitsTageAmStueck = InChanges.FolgeTage;


                List<int> SchichtIDsToRemove = new();


                foreach (int ID in Arbeiter._ZugeteilteSchichten)
                {
                    if (GetSchicht(ID) is SchichtInfo SInfo && SInfo.Date.Month == _currentMonth.Month && SInfo.Date.Year == _currentMonth.Year)
                    {
                        if (IsDayOff(Arbeiter.AbwesendListeNew, SInfo.Date.Day))
                        {
                            SchichtIDsToRemove.Add(ID);
                        }

                    }
                }

                ClearSelectedShiftsFromEmployee(SchichtIDsToRemove, InChanges.MAToChange);

                if (_ActiveEmployee != null && Arbeiter._MitarbeiterID == _ActiveEmployee._MitarbeiterID)
                {
                    SwitchGenerator();
                }
                UnsavedChanges = true;
            }
        }
        private void UpdateMASchichtInfos(int TotalCount, Dictionary<string, int> RoleTocount)
        {

            if (_MitarbeiterInfoCache == null) return;
            _MitarbeiterInfoCache.SchichtZahl.Text = TotalCount.ToString();
            string NewText = string.Empty;
            int Index = 0;
            foreach (var Pair in RoleTocount)
            {
                if (Index != 0) NewText += ", ";
                Index++;
                NewText += $"{Pair.Value.ToString()} x {Pair.Key}";
            }
            _MitarbeiterInfoCache.SchichtAls.Text = NewText;
        }
   
        public EmployeeData? GetMitarbeiter(int InMAID)
        {

            foreach (var item in _Mitartbeiter)
            {
                if (item._MitarbeiterID == InMAID) return item;
            }
            return null;
        }
        private void RepopulateTageImEinsatz()
        {
            foreach (EmployeeData ED in _Mitartbeiter)
            {
                ED.TageImEinsatz.Clear();
                foreach (int id in ED._ZugeteilteSchichten)
                {
                    if (GetSchicht(id) is SchichtInfo SI)
                    {
                      
                            ED.TageImEinsatz.Add(SI.Date);
                        
                    }
                }
            }
        }


        //Mitarbeiter Sortieren
        private void FilterMAbyStandort(object sender, RoutedEventArgs e)
        {
            CheckBox ClickBox = (CheckBox)sender;
            RecalcMitarberiterView(ClickBox);
        }
        private void ReloadSort(object sender, RoutedEventArgs e)
        {
            SortierMitarbeiter(Sortierungen);
        }
        private void ChangeOrder(object sender, RoutedEventArgs e)
        {
            _AufundAb = !_AufundAb;
            if(_AufundAb)
            {
                Orientierung.Content = "↑";
            }
            else
            {
                Orientierung.Content = "↓";
            }
            SortierMitarbeiter(Sortierungen);

        }
        private void SortierMitarbeiter(ComboBox Selector)
        {
            switch (Selector.SelectedIndex)
            {
                case 0: //Name
                    Sortierung_Name();
                    break;
                case 1: //ZielStunden
                    Sortierung_ZielStunden();
                    break;
                case 2: //Stunden
                    Sortierung_Stunden();
                    break;
                case 3:
                    Sortierung_TimeDiff();
                    break;
            }
        }
        private void Sortierung_Name()
        {
            List<EmployeeData> SortedList = !_AufundAb ? _Mitartbeiter.OrderBy(Employee => Employee._MitarbeiterName.ToLower()).ToList() : _Mitartbeiter.OrderByDescending(Employee => Employee._MitarbeiterName.ToLower()).ToList();
            _Mitartbeiter = SortedList;
            RecalcMitarberiterView(MAHaken);
        }
        private void Sortierung_ZielStunden()
        {

            List<EmployeeData> SortedList = !_AufundAb ? _Mitartbeiter.OrderBy(Employee => Employee._ZielStunden).ToList()
                : _Mitartbeiter.OrderByDescending(Employee => Employee._ZielStunden).ToList();
            _Mitartbeiter = SortedList;
            RecalcMitarberiterView(MAHaken);
        }
        private void Sortierung_Stunden()
        {

            List<EmployeeData> SortedList = !_AufundAb ? _Mitartbeiter.OrderBy(Employee => Employee._VerplanteStunden).ToList()
                : _Mitartbeiter.OrderByDescending(Employee => Employee._VerplanteStunden).ToList();
            _Mitartbeiter = SortedList;
            RecalcMitarberiterView(MAHaken);
        }
        private void Sortierung_TimeDiff()
        {

            List<EmployeeData> SortedList = !_AufundAb ? _Mitartbeiter.OrderBy(Employee =>  Employee._ZielStunden - Employee._VerplanteStunden).ToList()
                : _Mitartbeiter.OrderByDescending(Employee => Employee._ZielStunden - Employee._VerplanteStunden).ToList();
            _Mitartbeiter = SortedList;
            RecalcMitarberiterView(MAHaken);
        }

        private void RecalcMitarberiterView(CheckBox ClickBox)
        {
            if (ClickBox.IsChecked == true)
            {
                if (GetStandort(_ActiveStandortID) is PlanStandortData PSD)
                {
                    List<EmployeeData> OrtEmployees = new List<EmployeeData>();
                    foreach (EmployeeData ED in _Mitartbeiter)
                    {
                        if (ED._Standorte.Contains(_ActiveStandortID)) OrtEmployees.Add(ED);
                    }
                    AddEmployeeList(OrtEmployees);

                    if (_ActiveEmployee != null && !_ActiveEmployee._Standorte.Contains(_ActiveStandortID))
                    {
                        _ActiveEmployee.IstSelektiert = false;
                        _MitarbeiterWidgetMap.TryGetValue(_ActiveEmployee._MitarbeiterID, out Employee? OutEmployee);
                        if (OutEmployee != null)
                        {
                            OutEmployee.SetSelected(false);
                        }
                        _ActiveEmployee = null;
                    }
                }
            }
            else
            {
                AddEmployeeList(_Mitartbeiter);
            }
        }
        private void AddEmployeeList(List<EmployeeData> InEmployeeList)
        {
            EmployeeBox.Children.Clear();
            foreach (EmployeeData EP in InEmployeeList)
            {
                _MitarbeiterWidgetMap.TryGetValue(EP._MitarbeiterID, out Employee? OutEmployee);
                if (OutEmployee != null)
                {
                    EmployeeBox.Children.Add(OutEmployee);
                }
            }
        }

        //Rollen
        private void AddRole_Click(object sender, RoutedEventArgs e)
        {

            string result = Interaction.InputBox(
                "Gib einen Namen für die neue Rolle/n ein:" + Environment.NewLine + "Eingabeformat: Name oder Name(Kürzel)",
                "Rollenerstellung",
                "");


            Dictionary<string, string?> Roles = GetFullNameAndKuerzel(result);

            FügeNeueRollenHinzu(Roles);
        }
        private void RenameRole_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SelectedRole))
            {
                MessageBox.Show("Bitte wähle zuerst eine Rolle zum umbenennen aus.");
                return;
            }

            string roleNameCache = SelectedRole;
            string result = Interaction.InputBox(
                "Gib den neuen Namen für die Rolle ein:",
                "Rollenerstellung",
                "");


            Dictionary<string, string?> Roles = GetFullNameAndKuerzel(result);

            if(Roles.Count != 1)
            {
                MessageBox.Show("Bitte gib nur einen Namen für die rolle ein.");
                return;
            }
            foreach (var Rolle in Roles)
            {
                if (string.IsNullOrWhiteSpace(Rolle.Key))
                {
                    MessageBox.Show("Bitte gib für den Namen mindestens ein Zeichen an.");
                    return;
                }

                if(Rolle.Key.ToLower() == SelectedRole.ToLower())
                {
                    foreach(RoleLabel RL in _Rollen)
                    {
                        if(RL.RoleData.RoleName.ToLower() == Rolle.Key.ToLower())
                        {
                            if(!string.IsNullOrWhiteSpace(Rolle.Value))
                            {
                                RL.SetRole(Rolle.Key, Rolle.Value);
                            }
                            else
                            {
                                RL.SetRole(Rolle.Key, RL.RoleData.RoleKuerzel);
                            }
                        }
                    }
                }
                else
                {
                    RoleLabel? SelectedLabel = null;
                    foreach (RoleLabel RL in _Rollen)
                    {
                        if (RL.RoleData.RoleName.ToLower() == Rolle.Key.ToLower())
                        {
                            MessageBox.Show("Es ist schon eine weitere Rolle unter diesem Namen vorhanden.");
                            return;
                        }
                        if(RL.RoleData.RoleName.ToLower() == roleNameCache.ToLower()) SelectedLabel = RL;
                    }

                    if(SelectedLabel != null)
                    {
                        SelectedLabel.SetRole(Rolle.Key,Rolle.Value);
                    }


                }

                foreach (SchichtInfo SI in _Schichten)
                {
                    if (SI.SchichtRolle.ToLower() == roleNameCache.ToLower())
                    {
                        SI.SchichtRolle = Rolle.Key;
                    }
                }

                foreach(EmployeeData ED in _Mitartbeiter)
                {
                    if(ED._VorgeseheneRollen.Contains(roleNameCache))
                    {
                        ED._VorgeseheneRollen.Remove(roleNameCache);
                        ED._VorgeseheneRollen.Add(Rolle.Key);
                        if (_MitarbeiterWidgetMap.TryGetValue(ED._MitarbeiterID, out Employee? OutEmployee))
                        {
                            _MitarbeiterWidgetMap[ED._MitarbeiterID].SetRolls(ED._VorgeseheneRollen);
                        }
                    }
                }

                SelectedRole = Rolle.Key;
                break;
            }

            SwitchGenerator();
            UnsavedChanges = true;
        }
        private void RemoveRole_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SelectedRole)) return;

            foreach (RoleLabel RL in _Rollen)
            {
                if (RL.RoleData.RoleName.ToLower() == SelectedRole.ToLower())
                {
                    _Rollen.Remove(RL);
                    break;
                }
            }

            SelectedRole = string.Empty;
            UnsavedChanges = true;
        }
        private void FügeNeueRollenHinzu(Dictionary<string, string?> Rollen)
        {
            foreach (var Rolle in Rollen)
            {
                if (string.IsNullOrWhiteSpace(Rolle.Key)) continue;
                bool AddLabel = true;  
              
                 foreach (RoleLabel RL in _Rollen)
                 {
                        if (RL.RoleData.RoleName.ToLower() == Rolle.Key.ToLower())
                        {
                            AddLabel = false;
                            
                            if(string.IsNullOrWhiteSpace( RL.RoleData.RoleKuerzel) && !string.IsNullOrWhiteSpace(Rolle.Value))
                            {
                                    RL.SetRole(RL.RoleData.RoleName,Rolle.Value);
                                    UnsavedChanges = true;
                            }

                        }
                 }
                
                   
                 if(AddLabel)
                 {
                    RoleLabel NewLabel = new RoleLabel();
                    NewLabel.SetRole(Rolle.Key,Rolle.Value);
                    NewLabel.ClickedLeft += SetRoleActive;
                    _Rollen.Add(NewLabel);
                    UnsavedChanges = true;
                 }
       
            }
        }
        private void SetRoleActive(RoleLabel InLabel)
        {
            if (!string.IsNullOrWhiteSpace(SelectedRole))
            {
                foreach (RoleLabel RL in _Rollen)
                {
                    if (RL.RoleData.RoleName.ToLower() == SelectedRole.ToLower())
                    {
                        RL.RootBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(58, 58, 58));
                    }
                }
            }

            SelectedRole = InLabel.RoleData.RoleName;
            InLabel.RootBorder.Background = Brushes.DarkSlateBlue;

        }
        public string GetRoleKuerzel(string RoleName)
        {

            foreach (RoleLabel RL in _Rollen)
            {
                if (RL.RoleData.RoleName.ToLower() == RoleName.ToLower() && !string.IsNullOrWhiteSpace(RL.RoleData.RoleKuerzel))
                {
                    return RL.RoleData.RoleKuerzel;
                }
            }

            return RoleName;
        }


        //Kalender und Tage
        private void RedrawCalendar(object sender, RoutedEventArgs e)
        {
            SwitchGenerator();
        }
        private void SwitchGenerator()
        {
            if (_ActiveStandortID >= 0)
            {
                if (_WeekViewActive)
                {
                    GenerateWeekView(_currentMonth.Year, _currentMonth.Month);
                }
                else
                {
                    GenerateCalendar(_currentMonth.Year, _currentMonth.Month);
                }
            }

        }
        private void GenerateCalendar(int year, int month)
        {
            _KalenderTage.Clear();
            _DayMapping.Clear();

            int DaysInMonth = DateTime.DaysInMonth(year, month);

            DateTime FirstDayOfMonth = new DateTime(year, month, 1);
            Int32 DayofMonth = ((int)FirstDayOfMonth.DayOfWeek + 6) % 7;
            PlanStandortData? PSD = GetStandort(_ActiveStandortID);
            List<Holiday> holidays = new();
            if (PSD != null)
            {
                holidays = HolidayService.GetGermanHolidays(year, PSD.Bundesland);
            }
            else
            {
                holidays = HolidayService.GetGermanHolidays(year, null);
            }

            int day = 1;

            for (int row = 0; row < 6; row++)
            {
                for (int col = 0; col < 7; col++)
                {
                    KalenderTag ThisDay = new KalenderTag();
                    if (row == 0 && col < DayofMonth)
                    {
                     
                        ThisDay.Visibility= Visibility.Hidden;

                        _KalenderTage.Add(ThisDay);
                        continue;
                    }
                    DateTime currentDate = new DateTime(year, month, day);
                    Holiday? holiday = holidays.FirstOrDefault(h => h.Date.Date == currentDate.Date);

                    ThisDay.DayData._KalenderDatum = currentDate;
                    ThisDay.ClickedDay += DayCell_Click;
                    ThisDay.SwitchToWeek += RoleButton_ClickedButton;
                    ThisDay.DateText.Text = day.ToString();
                    _DayMapping.Add(day,_DayAbbreviations[col]);
        
                    if (holiday != null)
                    {
                        ThisDay.SetDayToHoliday(holiday.Name);
                    }

                    if (PSD != null)
                    {
                        if (PSD.SchliessTageList.Contains(_DayAbbreviations[col].ToLower()) || PSD.SchliessTageList.Contains(day.ToString()) || (PSD.bIsClosedOnHoliday && ThisDay.DayData.bIsHoliday))
                        {
                            ThisDay.DayData.NotAvailableForSelection = true;
                            ThisDay.RootBorder.Background = _ClosedDayBrush;
                            ThisDay.RootBorder.Opacity = _ClosedDayOpacity;
                        }

                    }
                    foreach (SchichtInfo assignment in _Schichten)
                    {
                        if (assignment.Date.Date == currentDate.Date && assignment.SLinkedID == _ActiveStandortID)
                        {
                            ThisDay.DayData.TotalShifts++;
                            ThisDay.DayData.LinkedShifts.Add(assignment.SchichtID);
                            if (ThisDay.DayData.SchiftMapping.TryGetValue(assignment.SchichtRolle, out int count))
                            {
                                ThisDay.DayData.SchiftMapping[assignment.SchichtRolle]++;
                            }
                            else
                            {
                                ThisDay.DayData.SchiftMapping.Add(assignment.SchichtRolle, 1);
                            }
                        }
                    }

                   
                    if(_UseRoleKuerzel)
                    {
                      
                        List<string> RoleStrings = ThisDay.DayData.SchiftMapping.Keys.ToList();

                        RoleStrings = RoleStrings.OrderBy(x => x).ToList();

                        string NewText = string.Empty;
                        int Index = 0;

                        foreach (string Role in RoleStrings)
                        {
                            if (Index != 0) NewText += Environment.NewLine;
                            Index++;
                            string rtext = _UseRoleKuerzel ? GetRoleKuerzel(Role) : Role;
                            NewText += $"{ThisDay.DayData.SchiftMapping[Role]} x {rtext}";
                        }

                        ThisDay.SchichtText.Text = NewText;
                    }
                    else
                    {
                        ThisDay.SetShiftInfo();
                    }
                        
                    _KalenderTage.Add(ThisDay);

                    day++;

                    if (day > DaysInMonth)
                    {
                        if (_ActiveEmployee != null)
                        {
                            AddMAChangesToDays(_ActiveEmployee);
                        }
                        return;
                    }
                        
                }
            }
        }
        private void GenerateWeekView(int year, int month)
        {
            _WochenTage.Clear();
            _DayMapping.Clear();
            

            int DaysInMonth = DateTime.DaysInMonth(year, month);

            DateTime FirstDayOfMonth = new DateTime(year, month, 1);
            Int32 DayofMonth = ((int)FirstDayOfMonth.DayOfWeek + 6) % 7;

            int day = 1;
            PlanStandortData? PSD = GetStandort(_ActiveStandortID);
            List<Holiday> holidays = new();
            if (PSD != null)
            {
                holidays = HolidayService.GetGermanHolidays(year,PSD.Bundesland);
            }
            else
            {
                holidays = HolidayService.GetGermanHolidays(year, null);
            }

           

            for (int row = 0; row < 6; row++)
            {
                for (int col = 0; col < 7; col++)
                {
                    if (row == 0 && col < DayofMonth)
                    {
                        continue;
                    }


                    DateTime currentDate = new DateTime(year, month, day);
                    Holiday? holiday = holidays.FirstOrDefault(h => h.Date.Date == currentDate.Date);

                    WochenTag ThisDay = new WochenTag();
                    ThisDay.DayData._KalenderDatum = currentDate;
                    ThisDay.ClickedWeekDay += WeekDay_Click;
                    ThisDay.DateText.Text = day.ToString();
                    ThisDay.TagesText.Text = _DayAbbreviations[col];
                    _DayMapping.Add(day, _DayAbbreviations[col]);
                    if (col == 0)
                    {

                        ThisDay.RootBorder.BorderThickness = new Thickness(1, 5, 1, 1); ThisDay.RootBorder.CornerRadius = new CornerRadius(0, 0, 5, 5);
                        int week = ISOWeek.GetWeekOfYear(currentDate);
                        ThisDay.KalenderWoche.Text = $"KW {week}";
                        ThisDay.KalenderWoche.Visibility = Visibility.Visible;
                    }
                    else if ((col == 5 || col == 6) && _KeineWEs) { day++; if (day > DaysInMonth)
                        {
                            if (_ActiveEmployee != null)
                            {
                                AddMAChangesToDays(_ActiveEmployee);
                            }
                            return;
                        }
                        continue;  }

                    if (holiday != null)
                    {
                        ThisDay.SetDayToHoliday(holiday.Name);

                    }

                    if (PSD != null)
                    {
                        if(PSD.SchliessTageList.Contains(ThisDay.TagesText.Text.ToLower()) || PSD.SchliessTageList.Contains(day.ToString()) || (PSD.bIsClosedOnHoliday && ThisDay.DayData.bIsHoliday))
                        {
                            ThisDay.DayData.NotAvailableForSelection = true;
                            ThisDay.RootBorder.Background = _ClosedDayBrush;
                            ThisDay.RootBorder.Opacity = _ClosedDayOpacity;
                        }

                    }

                    Dictionary<string,List<SchichtLabel>> SortedByRole = new Dictionary<string, List<SchichtLabel>>();
                    List<string> SortedRoles = new List<string>();
                    foreach (SchichtInfo assignment in _Schichten)
                    {
                        if (assignment.Date.Date == currentDate.Date && assignment.SLinkedID == _ActiveStandortID)
                        {
                            ThisDay.DayData.TotalShifts++;

                            if (ThisDay.DayData.SchiftMapping.TryGetValue(assignment.SchichtRolle, out int count))
                            {
                                ThisDay.DayData.SchiftMapping[assignment.SchichtRolle]++;
                            }
                            else
                            {
                                ThisDay.DayData.SchiftMapping.Add(assignment.SchichtRolle, 1);
                            }

                            if (GetMitarbeiter(assignment.ELinkedID) is EmployeeData Arbeiter)
                            {
                                SchichtLabel Label = new SchichtLabel();
                                Label.NameText.Text = Arbeiter._MitarbeiterName;
                                System.Windows.Media.Color color = (System.Windows.Media.Color)ColorConverter.ConvertFromString(Arbeiter.ColorHex);
                                Label.RootBorder.BorderBrush = new SolidColorBrush(color);
                                if (FarbSettings.IsChecked == false) { Label.RootBorder.Background = new SolidColorBrush(color); }
                                Label.StartZeit.Text = assignment.Zeiten.SchichtStartText;
                                Label.SchlussZeit.Text = assignment.Zeiten.SchichtSchlussText;
                                Label.RollenText.Text =  _UseRoleKuerzel ?GetRoleKuerzel(assignment.SchichtRolle.ToString()) :  assignment.SchichtRolle.ToString();
                                Label._LinkedSchichtID = assignment.SchichtID;
                                if (assignment.Zeiten.bPlusOneDay) Label.PlusDayText.Visibility = Visibility.Visible;
                                Label._LinkedOrtID = assignment.SLinkedID;
                                Label.StartSchichtEdit += OpenSchichtEditor;
                               


                                if (SortedByRole.TryGetValue(assignment.SchichtRolle, out List<SchichtLabel>? Labels))
                                {
                                    SortedByRole[assignment.SchichtRolle].Add(Label);
                                }
                                else
                                {
                                    SortedRoles.Add(assignment.SchichtRolle);
                                    SortedByRole.Add(assignment.SchichtRolle, new List<SchichtLabel> {Label} );
                                }
                            }
                        }
                    }
                    SortedRoles = SortedRoles.OrderBy(x => x).ToList();
                    foreach ( string Role in SortedRoles)
                    {
                        foreach (SchichtLabel AddLabel in SortedByRole[Role])
                        {
                            ThisDay.DayData.LinkedShifts.Add(AddLabel._LinkedSchichtID);
                            ThisDay.ContentBox.Children.Add(AddLabel);
                        }
                    }

               
                    _WochenTage.Add(ThisDay);
                    day++;

                    if (day > DaysInMonth)
                    {
                        if (_ActiveEmployee != null) 
                        {
                            AddMAChangesToDays(_ActiveEmployee);
                        }
                        return;
                    }

                }
            }
        }
        private void DayCell_Click(KalenderTag InDay)
        {
            bool flowControl = EvaluateDayClickability(InDay.DayData.NotAvailableForSelectedMA, InDay.DayData.NotAvailableForSelection);
            if (!flowControl)
            {

                if(InDay.DayData.NotAvailableForSelectedMA)
                {
                    DateTime Date = InDay.DayData._KalenderDatum.Date;
                    if (_ActiveEmployee
                        != null) {

                        foreach (int SchichtID in _ActiveEmployee!._ZugeteilteSchichten)
                        {
                            if (GetSchicht(SchichtID) is SchichtInfo SI && SI.Date == Date)
                            {
                                OpenSchichtEditor(SchichtID);
                                return;
                            }
                        }

                    }
                }
                return;
            }
            if(!IsRoleAvailableByEmployee()) return;
            if (_ActiveEmployee == null) return;
            DateTime clickedDate = InDay.DayData._KalenderDatum;
            int concurrentDays = 1;
            for(int i = 1; i <_ActiveEmployee.MaxArbeitsTageAmStueck+1;i++)
            {
                if (_ActiveEmployee.TageImEinsatz.Contains(clickedDate.AddDays(-i)))
                {
                    ++concurrentDays;
                }
                else break;
            }
            MessageBox.Show($"Tage nach hinten: {concurrentDays}");
            for (int u = 1; u < _ActiveEmployee.MaxArbeitsTageAmStueck+1; u++)
            {
                    if (_ActiveEmployee.TageImEinsatz.Contains(clickedDate.AddDays(u)))
                    {
                        ++concurrentDays;
                    }
                    else break;
            }
            MessageBox.Show($"Tage nach vorne: {concurrentDays}");
            if (concurrentDays > _ActiveEmployee.MaxArbeitsTageAmStueck)
                {
                    BestätigungsWidget window = new BestätigungsWidget();
                    window.Owner = this;
                    string InfoText = $"Der Mitarbeiter überschreitet die maximale Anzahl an Einsätzen am Stück." + Environment.NewLine + "Soll die Schicht trotzdem zugeteilt werden?";
                    window.SetInfoText(InfoText);
                    bool? result = window.ShowDialog();

                    if (result != true)
                    {
                        return;
                    }

                }



            SchichtInfo Schicht = new SchichtInfo();
            Schicht.Zeiten = BuildShiftTime(clickedDate, Begin_Box.Text, Ende_Box.Text);

            if (HasMAShiftThisDay(clickedDate.AddDays(1), _ActiveEmployee, out SchichtInfo? NextShift))
            {
                if (NextShift != null)
                {
                    DateTime DayAfter = clickedDate.AddDays(1);
                    DayAfter = DayAfter.AddHours((int)(NextShift.Zeiten.SchichtEnde / 60));
                    DayAfter = DayAfter.AddMinutes((int)(NextShift.Zeiten.SchichtEnde % 60));
                    DateTime CurrentDay = clickedDate;
                    CurrentDay = CurrentDay.AddHours((int)(Schicht.Zeiten.SchichtStart / 60));
                    CurrentDay = CurrentDay.AddMinutes((int)(Schicht.Zeiten.SchichtStart % 60));
                    double hours = (DayAfter - CurrentDay).TotalHours;
                    if (hours < 11)
                    {
                        BestätigungsWidget window = new BestätigungsWidget();
                        window.Owner = this;
                        string InfoText = $"Mit der Einteilung dieser Schicht wird die gesetzliche Ruhezeit des Arbeitnehmer verletzt." + Environment.NewLine + "Soll die Schicht trotzdem zugeteilt werden?";
                        window.SetInfoText(InfoText);
                        bool? result = window.ShowDialog();

                        if (result != true)
                        {
                            return;
                        }
                    }
                    //unterschreitet ruhezeit
                }
            } else if (HasMAShiftThisDay(clickedDate.AddDays(-1), _ActiveEmployee, out SchichtInfo? LastShift))
            {
                if (LastShift != null)
                {
                    DateTime DayBefore = clickedDate.AddDays(-1);
                    DayBefore = DayBefore.AddHours((int)(LastShift.Zeiten.SchichtEnde / 60));
                    DayBefore = DayBefore.AddMinutes((int)(LastShift.Zeiten.SchichtEnde % 60));
                    DateTime CurrentDay = clickedDate;
                    CurrentDay = CurrentDay.AddHours((int)(Schicht.Zeiten.SchichtStart / 60));
                    CurrentDay = CurrentDay.AddMinutes((int)(Schicht.Zeiten.SchichtStart % 60));
                    double hours = (CurrentDay - DayBefore).TotalHours;
                    if (hours < 11)
                    {
                        BestätigungsWidget window = new BestätigungsWidget();
                        window.Owner = this;
                        string InfoText = $"Mit der Einteilung dieser Schicht wird die gesetzliche Ruhezeit des Arbeitnehmer verletzt." + Environment.NewLine + "Soll die Schicht trotzdem zugeteilt werden?";
                        window.SetInfoText(InfoText);
                        bool? result = window.ShowDialog();

                        if (result != true)
                        {
                            return;
                        }
                    }
                }
            }



          
            AssigneNewShift(clickedDate, Schicht);
            SwitchGenerator();
            UnsavedChanges = true;
        }
        private void WeekDay_Click(WochenTag InDay)
        {
            bool flowControl = EvaluateDayClickability(InDay.DayData.NotAvailableForSelectedMA,InDay.DayData.NotAvailableForSelection);
            if (!flowControl)
            {
                return;
            }

            if (!IsRoleAvailableByEmployee()) return;
            DateTime clickedDate = InDay.DayData._KalenderDatum;
            int concurrentDays = 1;
            if (_ActiveEmployee == null) return;
            for (int i = 1; i < _ActiveEmployee.MaxArbeitsTageAmStueck + 1; i++)
            {
                if (_ActiveEmployee.TageImEinsatz.Contains(clickedDate.AddDays(-i)))
                {
                    ++concurrentDays;
                }
                else break;
            }
            for (int u = 1; u < _ActiveEmployee.MaxArbeitsTageAmStueck + 1; u++)
            {
                if (_ActiveEmployee.TageImEinsatz.Contains(clickedDate.AddDays(u)))
                {
                    ++concurrentDays;
                }
                else break;
            }

            if (concurrentDays > _ActiveEmployee.MaxArbeitsTageAmStueck)
            {
                BestätigungsWidget window = new BestätigungsWidget();
                window.Owner = this;
                string InfoText = $"Der Mitarbeiter überschreitet die maximale Anzahl an Einsätzen am Stück." + Environment.NewLine + "Soll die Schicht trotzdem zugeteilt werden?";
                window.SetInfoText(InfoText);
                bool? result = window.ShowDialog();

                if (result != true)
                {
                    return;
                }

            }
            SchichtInfo Schicht = new SchichtInfo();
            Schicht.Zeiten = BuildShiftTime(clickedDate, Begin_Box.Text, Ende_Box.Text);

            if (HasMAShiftThisDay(clickedDate.AddDays(1), _ActiveEmployee, out SchichtInfo? NextShift))
            {
                if (NextShift != null)
                {
                    DateTime DayAfter = clickedDate.AddDays(1);
                    DayAfter = DayAfter.AddHours((int)(NextShift.Zeiten.SchichtEnde / 60));
                    DayAfter = DayAfter.AddMinutes((int)(NextShift.Zeiten.SchichtEnde % 60));
                    DateTime CurrentDay = clickedDate;
                    CurrentDay = CurrentDay.AddHours((int)(Schicht.Zeiten.SchichtStart / 60));
                    CurrentDay = CurrentDay.AddMinutes((int)(Schicht.Zeiten.SchichtStart % 60));
                    double hours = (DayAfter - CurrentDay).TotalHours;
                    if (hours < 11)
                    {
                        BestätigungsWidget window = new BestätigungsWidget();
                        window.Owner = this;
                        string InfoText = $"Mit der Einteilung dieser Schicht wird die gesetzliche Ruhezeit des Arbeitnehmer verletzt." + Environment.NewLine + "Soll die Schicht trotzdem zugeteilt werden?";
                        window.SetInfoText(InfoText);
                        bool? result = window.ShowDialog();

                        if (result != true)
                        {
                            return;
                        }
                    }
                    //unterschreitet ruhezeit
                }
            }
            else if (HasMAShiftThisDay(clickedDate.AddDays(-1), _ActiveEmployee, out SchichtInfo? LastShift))
            {
                if (LastShift != null)
                {
                    DateTime DayBefore = clickedDate.AddDays(-1);
                    DayBefore = DayBefore.AddHours((int)(LastShift.Zeiten.SchichtEnde / 60));
                    DayBefore = DayBefore.AddMinutes((int)(LastShift.Zeiten.SchichtEnde % 60));
                    DateTime CurrentDay = clickedDate;
                    CurrentDay = CurrentDay.AddHours((int)(Schicht.Zeiten.SchichtStart / 60));
                    CurrentDay = CurrentDay.AddMinutes((int)(Schicht.Zeiten.SchichtStart % 60));
                    double hours = (CurrentDay - DayBefore).TotalHours;
                    if (hours < 11)
                    {
                        BestätigungsWidget window = new BestätigungsWidget();
                        window.Owner = this;
                        string InfoText = $"Mit der Einteilung dieser Schicht wird die gesetzliche Ruhezeit des Arbeitnehmer verletzt." + Environment.NewLine + "Soll die Schicht trotzdem zugeteilt werden?";
                        window.SetInfoText(InfoText);
                        bool? result = window.ShowDialog();

                        if (result != true)
                        {
                            return;
                        }
                    }
                }
            }
            AssigneNewShift(clickedDate, Schicht);
    
            int ClickedDay = InDay.DayData._KalenderDatum.Day;

            Dictionary<string, List<SchichtLabel>> SortedByRole = new Dictionary<string, List<SchichtLabel>>();
            InDay.DayData.TotalShifts++;

            if (InDay.DayData.SchiftMapping.TryGetValue(Schicht.SchichtRolle, out int count))
            {
                InDay.DayData.SchiftMapping[Schicht.SchichtRolle]++;
            }
            else
            {
                InDay.DayData.SchiftMapping.Add(Schicht.SchichtRolle, 1);
            }
            InDay.DayData.LinkedShifts.Add(Schicht.SchichtID);


            foreach (SchichtInfo assignment in _Schichten)
            {
                if (assignment.Date.Date == clickedDate.Date && assignment.SLinkedID == _ActiveStandortID)
                {
                    if (GetMitarbeiter(assignment.ELinkedID) is EmployeeData Arbeiter)
                    {
                        SchichtLabel Label = new SchichtLabel();
                        Label.NameText.Text = Arbeiter._MitarbeiterName;
                        System.Windows.Media.Color color = (System.Windows.Media.Color)ColorConverter.ConvertFromString(Arbeiter.ColorHex);
                        Label.RootBorder.BorderBrush = new SolidColorBrush(color);
                        if (FarbSettings.IsChecked == false) { Label.RootBorder.Background = new SolidColorBrush(color); }
                        Label.StartZeit.Text = assignment.Zeiten.SchichtStartText;
                        Label.SchlussZeit.Text = assignment.Zeiten.SchichtSchlussText;
                        Label.RollenText.Text = _UseRoleKuerzel ? GetRoleKuerzel(assignment.SchichtRolle.ToString()) : assignment.SchichtRolle.ToString();
                        Label._LinkedSchichtID = assignment.SchichtID;
                        if (assignment.Zeiten.bPlusOneDay) Label.PlusDayText.Visibility = Visibility.Visible;
                        Label._LinkedOrtID = assignment.SLinkedID;
                        Label.StartSchichtEdit += OpenSchichtEditor;


                        if (SortedByRole.TryGetValue(assignment.SchichtRolle, out List<SchichtLabel>? Labels))
                        {
                            SortedByRole[assignment.SchichtRolle].Add(Label);
                        }
                        else
                        {

                            SortedByRole.Add(assignment.SchichtRolle, new List<SchichtLabel> { Label });
                        }
                    }
                }
            }

            InDay.ContentBox.Children.Clear();
            foreach (var Pair in SortedByRole)
            {
                foreach (SchichtLabel AddLabel in Pair.Value)
                {

                    InDay.ContentBox.Children.Add(AddLabel);
                }
            }

            AddMAChangesToDays(_ActiveEmployee!);
            UnsavedChanges = true;
        }
        private bool EvaluateDayClickability(bool AvailableForMA, bool AvailableByStandort)
        {
            if (AvailableByStandort)
            {

                MessageBox.Show("Der Tag ist aktuell gesperrt für die Schichteinteilung.");
                return false;
            }
            if (_ActiveEmployee == null)
            {
                MessageBox.Show("Bitte wähle einen Mitarbeiter für diese Schicht.");
                return false;
            }
            if (!_ActiveEmployee._Standorte.Contains(_ActiveStandortID))
            {
                MessageBox.Show("Der Mitarbeiter ist nicht zugelassen für diesen Standort.");
                return false;
            }
            if (AvailableForMA)
            {
                return false;
            }
            if (string.IsNullOrWhiteSpace(SelectedRole))
            {

                MessageBox.Show("Bitte wähle eine Rolle für diese Schicht aus.");
                return false;
            }

            if (!UtilityClass.IsValidTimeString(Begin_Box.Text) || !UtilityClass.IsValidTimeString(Ende_Box.Text))
            {
                MessageBox.Show("Bitte gib eine Zugelassene Zeitspanne ein");
                return false;
            }

            return true;
        }
        private bool IsRoleAvailableByEmployee()
        {
            //Ist die Rolle richtig für den Mitarbeiter?
            bool bIsRolleDa = false;
            foreach (string Rolle in _ActiveEmployee!._VorgeseheneRollen)
            {
                if (Rolle.ToLower() == SelectedRole.ToLower())
                {
                    bIsRolleDa = true;
                    break;
                }
            }
            if (!bIsRolleDa)
            {
                string InfoText = $"Die Rolle {SelectedRole} ist nicht für den Mitarbeiter vorgesehen."
               + Environment.NewLine + $"Möchtest du ihm die Rolle zuweisen?";
                BestätigungsWidget window = new BestätigungsWidget();
                window.SetInfoText(InfoText);
                window.Owner = this;
                bool? result = window.ShowDialog();
                if (result != null & result == true)
                {
                    _ActiveEmployee._VorgeseheneRollen.Add(SelectedRole);
                    _MitarbeiterWidgetMap.TryGetValue(_ActiveEmployee._MitarbeiterID, out Employee? OutMA);
                    if (OutMA != null)
                    {
                        OutMA.SetRolls(_ActiveEmployee._VorgeseheneRollen);
                    }
                    
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
        private void AssigneNewShift(DateTime clickedDate, SchichtInfo Schicht)
        {
            if (_ActiveEmployee == null) return;
            Schicht.SchichtID = _SchichtIDCounter;
            Schicht.Date = clickedDate;

            if (GetStandort(_ActiveStandortID) is PlanStandortData PSD)
            {
                Schicht.bIsHoliday = HolidayService.IsHoliday(clickedDate, PSD.Bundesland);
            }
            else
            {
                Schicht.bIsHoliday = HolidayService.IsHoliday(clickedDate, null);
            }


            Schicht.SchichtRolle = SelectedRole;
            Schicht.Notiz = Notiz_Box.Text;

            Schicht.ELinkedID = _ActiveEmployee._MitarbeiterID;
            Schicht.SLinkedID = _ActiveStandortID;
            _SchichtIDCounter++;

            _ActiveEmployee._VerplanteStunden += Schicht.Zeiten.SchichtStunden;
            _MitarbeiterWidgetMap.TryGetValue(_ActiveEmployee._MitarbeiterID, out Employee? OutEmployee);
            if (OutEmployee != null)
            {
                OutEmployee.SetHours(_ActiveEmployee._VerplanteStunden, _ActiveEmployee._ZielStunden);
            }
            _ActiveEmployee.TageImEinsatz.Add(clickedDate);
            _ActiveEmployee._ZugeteilteSchichten.Add(Schicht.SchichtID);
            _Schichten.Add(Schicht);
        }

        private SchichtZeit BuildShiftTime(DateTime CurrentDate, string StartText, string EndText)
        {
            SchichtZeit OutShiftTime = new SchichtZeit();
            TimeOnly OutTime = new();
            OutShiftTime.SchichtStart = UtilityClass.GetTimeFromString(StartText, out OutTime);
            OutShiftTime.SchichtStartText = OutTime.ToShortTimeString();
            OutShiftTime.SchichtEnde = UtilityClass.GetTimeFromString(EndText, out OutTime);
            OutShiftTime.SchichtSchlussText = OutTime.ToShortTimeString();
            DateTime StartTime = CurrentDate.Date;
            StartTime = StartTime.AddHours((int)(OutShiftTime.SchichtStart / 60));
            StartTime = StartTime.AddMinutes((int)(OutShiftTime.SchichtStart % 60));
            DateTime EndTime = CurrentDate.Date;
            EndTime = EndTime.AddHours((int)(OutShiftTime.SchichtEnde / 60));
            EndTime = EndTime.AddMinutes((int)(OutShiftTime.SchichtEnde % 60));
            if (OutShiftTime.SchichtEnde < OutShiftTime.SchichtStart)
            {
                EndTime = EndTime.AddDays(1);
                OutShiftTime.bPlusOneDay = true;
            }
            OutShiftTime.SchichtEndDate = EndTime;
            OutShiftTime.SchichtStartDate = StartTime;
           // MessageBox.Show($"{OutShiftTime.SchichtStart}" + Environment.NewLine + $"{OutShiftTime.SchichtEnde}" + Environment.NewLine + $"{StartTime}" + Environment.NewLine+$"{EndTime}" );
            double StundenZahl = (OutShiftTime.SchichtEndDate - OutShiftTime.SchichtStartDate).TotalHours;
            if (_UseBreakTimes)
            {
                if (StundenZahl > 9)
                {
                    StundenZahl -= .75f;
                    OutShiftTime.PausenZeit = 45;
                }
                else if (StundenZahl > 6)
                {
                    StundenZahl -= .5f;
                   OutShiftTime.PausenZeit = 30;
                }
            }
            OutShiftTime.SchichtStunden = Math.Round(StundenZahl, 2);
            return OutShiftTime;
        }

        private void RoleButton_ClickedButton(int InDay)
        {
            CalendarBorder.Visibility = Visibility.Collapsed;
            WeekDayList.Visibility = Visibility.Visible;
            _WeekViewActive = true;
            _KalenderTage.Clear();
            SwitchGenerator();
            WeekDayList.ScrollIntoView(_WochenTage[InDay - 1]);
        }

        //Schichten Managen

        public SchichtInfo? GetSchicht(int InSchichtID)
        {

            foreach (var item in _Schichten)
            {
                if (item.SchichtID == InSchichtID) return item;
            }
            return null;
        }
        void OpenSchichtEditor(int _InSchichtID)
        {
            if (GetSchicht(_InSchichtID) is SchichtInfo SelectedSchicht)
            {
                if (GetMitarbeiter(SelectedSchicht.ELinkedID) is EmployeeData Arbeiter)
                {
                    if (GetStandort(SelectedSchicht.SLinkedID) is PlanStandortData Ort)
                    {
                        SchichtEditor schichtEditor = new SchichtEditor();
                        schichtEditor.Owner = this;
                        schichtEditor.SaveSchichtInfo += SaveNewSchichtInfo;
                        schichtEditor.SetSchichtInfo(SelectedSchicht, Arbeiter, Ort.StandortName);
                        schichtEditor.SchichtID = _InSchichtID;
                        _SchichtEditorCache = schichtEditor;
                        bool? result = schichtEditor.ShowDialog();

                        if (result != null && result == true) //if true shift should get deleted
                        {
                            Arbeiter._ZugeteilteSchichten.Remove(_InSchichtID);
                            int ScrollDay = SelectedSchicht.Date.Day;
                           
                            Arbeiter._VerplanteStunden -= SelectedSchicht.Zeiten.SchichtStunden;
                            _MitarbeiterWidgetMap.TryGetValue(Arbeiter._MitarbeiterID, out Employee? OutEmployee);
                            if (OutEmployee != null)
                            {
                                OutEmployee.SetHours(Arbeiter._VerplanteStunden, Arbeiter._ZielStunden);
                            }
                            if (_MitarbeiterInfoCache != null)
                            {
                                RemoveSingleShiftFromMAInfo(_InSchichtID, SelectedSchicht, Arbeiter, _MitarbeiterInfoCache.RolePanel);
                                int TotalCount = GetMASchichtZahlen(Arbeiter._ZugeteilteSchichten, out Dictionary<string, int>? RoleTocount);
                                UpdateMASchichtInfos(TotalCount, RoleTocount);
                            }

                            if (_WeekViewActive)
                            {
                                WochenTag WT = _WochenTage[ScrollDay - 1];
                                WT.DayData.TotalShifts--;
                                WT.DayData.LinkedShifts.Remove(_InSchichtID);
                                WT.DayData.SchiftMapping[SelectedSchicht.SchichtRolle]--;
                                foreach (UIElement Thingi in WT.ContentBox.Children)
                                {
                                    if (Thingi != null && Thingi is SchichtLabel)
                                    {
                                        SchichtLabel Label = (SchichtLabel)Thingi;
                                        if (Label._LinkedSchichtID == _InSchichtID)
                                        {
                                            WT.ContentBox.Children.Remove(Label);
                                            break;
                                        }
                                    }
                                }
                                RemoveMAChangesFromDays(Arbeiter);
                                AddMAChangesToDays(Arbeiter);

                                _Schichten.Remove(SelectedSchicht);
                                _SchichtEditorCache = null;
                            }
                            else
                            {
                                _SchichtEditorCache = null;
                                _Schichten.Remove(SelectedSchicht);
                                SwitchGenerator();
                            }

                            Arbeiter.TageImEinsatz.Remove(SelectedSchicht.Date);
                            UnsavedChanges = true;
                        }
                        schichtEditor.SaveSchichtInfo -= SaveNewSchichtInfo;
                    }
                }
            }
        }
        private void SaveNewSchichtInfo(SchichtSaveChanges InChanges)
        {

            if (GetSchicht(InChanges.LinkedSchichtID) is SchichtInfo SelectedSchicht)
            {

               if(!UtilityClass.IsValidTimeString(InChanges.NewStartTime) || !UtilityClass.IsValidTimeString(InChanges.NewEndTime))
               {

                    MessageBox.Show("Bitte gib eine gültige Zeitspanne ein.");
                    return;
               }

                SelectedSchicht.Notiz = InChanges.Notiz;

                double BeforeHours = SelectedSchicht.Zeiten.SchichtStunden;

                SelectedSchicht.Zeiten = BuildShiftTime(SelectedSchicht.Date, InChanges.NewStartTime, InChanges.NewEndTime);



                if (GetMitarbeiter(SelectedSchicht.ELinkedID) is EmployeeData Arbeiter)
                {
                    Arbeiter._VerplanteStunden -= BeforeHours;
                    Arbeiter._VerplanteStunden += SelectedSchicht.Zeiten.SchichtStunden;
                    _MitarbeiterWidgetMap.TryGetValue(SelectedSchicht.ELinkedID, out Employee? OutEmployee);
                    if (OutEmployee != null)
                    {
                        OutEmployee.SetHours(Arbeiter._VerplanteStunden, Arbeiter._ZielStunden);
                    }

                    if (_MitarbeiterInfoCache != null)
                    {
                        foreach (UIElement element in _MitarbeiterInfoCache.RolePanel.Children)
                        {
                            if (element is SchichtLabel schichtLabel && schichtLabel._LinkedSchichtID == SelectedSchicht.SchichtID)
                            {
                                schichtLabel.NameText.Text = $"{_Standorte[SelectedSchicht.SLinkedID].StandortName} am {_DayMapping[SelectedSchicht.Date.Day].ToString()}, {SelectedSchicht.Date.Day.ToString("00")}.{SelectedSchicht.Date.Month.ToString("00")}.{SelectedSchicht.Date.Year}";
                                schichtLabel.StartZeit.Text = SelectedSchicht.Zeiten.SchichtStartText;
                                schichtLabel.SchlussZeit.Text = SelectedSchicht.Zeiten.SchichtSchlussText;
                                if (SelectedSchicht.Zeiten.bPlusOneDay) schichtLabel.PlusDayText.Visibility = Visibility.Visible; else schichtLabel.PlusDayText.Visibility = Visibility.Collapsed;
                                break;
                            }

                        }

                        _MitarbeiterInfoCache.PlannedStundenText.Text = $"Verplante Stunden:  {Arbeiter._VerplanteStunden.ToString()}";

                    }
                }

                if (_SchichtEditorCache != null)
                {

                    string DatumsString = $"Datum:      {SelectedSchicht.Date.Day.ToString("00")}.{SelectedSchicht.Date.Month.ToString("00")}.{SelectedSchicht.Date.Year}";
                    if (SelectedSchicht.Zeiten.bPlusOneDay) DatumsString += " +1";
                    _SchichtEditorCache.SchichtDatum.Text = DatumsString;
                    _SchichtEditorCache.StartZeit.Text = SelectedSchicht.Zeiten.SchichtStartText;
                    _SchichtEditorCache.SchlussZeit.Text = SelectedSchicht.Zeiten.SchichtSchlussText;
                    string StundenText = $"{SelectedSchicht.Zeiten.SchichtStunden} std";
                    _SchichtEditorCache.StundenZahl.Text = StundenText;
                }

                if (_WeekViewActive)
                {
                    SwitchGenerator();
                }

                UnsavedChanges = true;
            }
        }
        private bool RemoveSingleShiftFromMAInfo(int _InSchichtID, SchichtInfo SelectedSchicht, EmployeeData Arbeiter, WrapPanel PanelToFilter)
        {
            if (_MitarbeiterInfoCache == null) return false;
            foreach (UIElement Thingi in PanelToFilter.Children)
            {
                if (Thingi != null && Thingi is SchichtLabel)
                {
                    SchichtLabel Label = (SchichtLabel)Thingi;
                    if (Label._LinkedSchichtID == _InSchichtID)
                    {
                        PanelToFilter.Children.Remove(Label);
                        if (_MitarbeiterInfoCache.SchichtTracker.TryGetValue(SelectedSchicht.SLinkedID, out int SchichtCount))
                        {
                            _MitarbeiterInfoCache.SchichtTracker[SelectedSchicht.SLinkedID]--;
                        }
                        _MitarbeiterInfoCache.PlannedStundenText.Text = $"Verplante Stunden:  {Arbeiter._VerplanteStunden.ToString()}";
                        Arbeiter._ZugeteilteSchichten.Remove(_InSchichtID);
                        return true;
                    }
                }
            }
            return false;
        }
        void ClearSelectedShiftsFromEmployee(List<int> _InSchichtIDs,int MAID)
        {
            if (GetMitarbeiter(MAID) is EmployeeData Arbeiter)
            {
                foreach (int LSchichtID in _InSchichtIDs)
                {
                    if (GetSchicht(LSchichtID) is SchichtInfo SelectedSchicht)
                    {
                  
                        Arbeiter._ZugeteilteSchichten.Remove(LSchichtID);
                        Arbeiter.TageImEinsatz.Remove(SelectedSchicht.Date);
                          

                        Arbeiter._VerplanteStunden -= SelectedSchicht.Zeiten.SchichtStunden;
                        _Schichten.Remove(SelectedSchicht);

                    }
                }

                _MitarbeiterWidgetMap.TryGetValue(Arbeiter._MitarbeiterID, out Employee? OutEmployee);
                if (OutEmployee != null)
                {
                    OutEmployee.SetHours(Arbeiter._VerplanteStunden, Arbeiter._ZielStunden);
                }
                if (_MitarbeiterInfoCache != null)
                {
                    _MitarbeiterInfoCache.PlannedStundenText.Text = $"Verplante Stunden:  {Arbeiter._VerplanteStunden.ToString()}";
                }

                int TotalCount = GetMASchichtZahlen(Arbeiter._ZugeteilteSchichten, out Dictionary<string, int>? RoleTocount);
                UpdateMASchichtInfos(TotalCount, RoleTocount);
                UnsavedChanges = true;
            }
        }

        //Save
        private void Click_SavePlan(object sender, RoutedEventArgs e)
        {
            CommitSave("");
        }
        private void JustSave(object sender, ExecutedRoutedEventArgs e)
        {
            CommitSave(_savefile);
        }
        private string Execute_SaveToDisk(ShiftPlanSaveData insaveData)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "JSON Files (*.json)|*.json";
            dialog.DefaultExt = ".json";
            dialog.FileName = "Schichtplan";
            if (dialog.ShowDialog() == true)
            {
                return dialog.FileName;
            }
            return string.Empty;
        }
        private void CommitSave(string InSaveFile)
        {
         
            ShiftPlanSaveData saveData = new ShiftPlanSaveData();

            saveData.Year = _currentMonth.Year;

            saveData.Month = _currentMonth.Month;

            saveData.SchichtIDCounter = _SchichtIDCounter;
            saveData.MitarbeiterIDCounter = _MitarbeiterIDCounter;
            saveData.StandortIDCounter = _StandortIDCounter;

            saveData.SD_Standort = _Standorte;
            saveData.SD_Employees = _Mitartbeiter;
            saveData.SD_Assignments = _Schichten;
            saveData.SD_UseRollenKuerzel = _UseRoleKuerzel;
            saveData.SD_UseStandortKuerzel = _UseStandortKuerzel;
            saveData.SD_ShowAbwesenheit = _ShowOutOfOfficeReason;
            saveData.SD_UsegesPause = _UseBreakTimes;
            saveData.SD_FontSize = _ExportSizePDFST;
            saveData.SD_FontSizeMA = _ExportSizePDFPersonal;
            saveData.SD_UsePDFColor = _UseColorForPDF;

            foreach (RoleLabel RL in _Rollen) 
            {
                saveData.SD_Roles.Add(RL.RoleData);
            }

            string LocalSaveFile = InSaveFile;
            if (string.IsNullOrWhiteSpace(InSaveFile) || !File.Exists(InSaveFile))
            {
                 LocalSaveFile = Execute_SaveToDisk(saveData);
                if(string.IsNullOrWhiteSpace(LocalSaveFile))
                {
                    MessageBox.Show(
                   "Speichern fehlgeschlagen.",
                   "Der Dateiname is nicht gültig.",
                   MessageBoxButton.OK,
                   MessageBoxImage.Warning);
                   return;
                }
            }

            try
            {
                string json = JsonSerializer.Serialize(
                  saveData,
                  new JsonSerializerOptions
                  {
                      WriteIndented = true
                  });
                File.WriteAllText(LocalSaveFile, json);
                _savefile = LocalSaveFile;
                UnsavedChanges = false;
            }
            catch (IOException)
            {
                MessageBox.Show(
                    "Speichern fehlgeschlagen.",
                    "Die Datei ist in einem anderen Programm geöffnet.",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        private void Click_LoadPlan(object sender, RoutedEventArgs e)
        {

            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Filter = "JSON Files (*.json)|*.json";
            if (dialog.ShowDialog() == true)
            {
                string json = File.ReadAllText(dialog.FileName);
                
                ShiftPlanSaveData ? saveData =
                    JsonSerializer.Deserialize<ShiftPlanSaveData>(json);

                if (saveData == null)
                {
                    MessageBox.Show("Could not load file.");
                    return;
                }
                _savefile = dialog.FileName;
                _currentMonth = new DateTime(saveData.Year, saveData.Month, 1);
                MonthTitleText.Text = _MonthMapping[_currentMonth.Month] + " " + _currentMonth.Year.ToString();

               
             
                //Rollen 
                _Rollen.Clear();
                SelectedRole = string.Empty;
                foreach(RoleData RD in saveData.SD_Roles)
                {
                    RoleLabel NewLabel = new RoleLabel();
                    NewLabel.SetRole(RD.RoleName, RD.RoleKuerzel);
                    NewLabel.ClickedLeft += SetRoleActive;
                    _Rollen.Add(NewLabel);
                }

                //Restore Schichten
                _SchichtIDCounter = saveData.SchichtIDCounter;
                _Schichten = saveData.SD_Assignments;


                //Restore Standorte
                _Standorte = saveData.SD_Standort;
                _StandortIDCounter = saveData.StandortIDCounter;

                StandortBox.Children.Clear();
                _StandortWidgetMap.Clear();
                _ActiveStandortID = -1;
                foreach (PlanStandortData LStandort in _Standorte)
                {
                    PlanStandort StandortWidget = new PlanStandort();
                    StandortWidget.LinkedStandortID = LStandort.PlanStandortId;
                    StandortWidget.SetOrtName(LStandort.StandortName);
                    StandortWidget.ClickedLeft += SetStandortActive;
                    StandortWidget.ClickedRight += OpenStandortInfo;
                    _StandortWidgetMap.Add(LStandort.PlanStandortId, StandortWidget);
                    StandortBox.Children.Add(StandortWidget);
                    if (LStandort.IstSelektiert == true) _ActiveStandortID = LStandort.PlanStandortId;
                }

                //Restore Mitarbeiter
                _MitarbeiterIDCounter = saveData.MitarbeiterIDCounter;

                _Mitartbeiter = saveData.SD_Employees;
                _MitarbeiterWidgetMap.Clear();
                _ActiveEmployee = null;

                foreach (EmployeeData Arbeiter in _Mitartbeiter)
                {
                    Employee employeWidget = new Employee();
                    Arbeiter.IstSelektiert = false;

                    employeWidget.SetMitarbeiterName(Arbeiter._MitarbeiterName);
                    employeWidget.SetRolls(Arbeiter._VorgeseheneRollen);
                    employeWidget._LinkedMitarbeiterID = Arbeiter._MitarbeiterID;
                    employeWidget.Clicked += SetEmployeeActive;
                    employeWidget.RightClicked += OpenMAInfo;
                    System.Windows.Media.Color color = (System.Windows.Media.Color)ColorConverter.ConvertFromString(Arbeiter.ColorHex);
                    employeWidget.RootBorder.BorderBrush = new SolidColorBrush(color);
                    _MitarbeiterWidgetMap.Add(Arbeiter._MitarbeiterID, employeWidget);



                    //Compatability Load for 1.0.2 and before
                    Arbeiter.AbwesendDaten.TryGetValue(_currentMonth, out List<TagesWunsch>? OutDayList);
                    if (OutDayList == null)
                    {
                        Arbeiter.AbwesendDaten.Add(_currentMonth, new List<TagesWunsch>(Arbeiter.AbwesendListeNew));
                    }
                    Arbeiter.FreitagsDaten.TryGetValue(_currentMonth, out List<TagesWunsch>? OutFWDayList);
                    if (OutFWDayList == null)
                    {
                        Arbeiter.FreitagsDaten.Add(_currentMonth, new List<TagesWunsch>(Arbeiter.FreitagsWuensche));
                    }
                    Arbeiter.EinsatzDaten.TryGetValue(_currentMonth, out List<TagesWunsch>? OutEWDayList);
                    if (OutEWDayList == null)
                    {
                        Arbeiter.EinsatzDaten.Add(_currentMonth, new List<TagesWunsch>(Arbeiter.Einsatzwuensche));
                    }


                }
                SortierMitarbeiter(Sortierungen);

                //Compatability Load for 1.0.1
                RepopulateTageImEinsatz();
                //Set Active Standort
                if (_StandortWidgetMap.TryGetValue(_ActiveStandortID,out PlanStandort? PST))
                {
                    SetStandortActive(_StandortWidgetMap[_ActiveStandortID]);
                }

                Kuerzelsettings.IsChecked = saveData.SD_UseRollenKuerzel;
                KuerzelsettingsST.IsChecked = saveData.SD_UseStandortKuerzel;
                VerwendePausenzeiten.IsChecked = saveData.SD_UsegesPause;
                ZeigeAbwesenheitsGrund.IsChecked = saveData.SD_ShowAbwesenheit;
                FarbExport.IsChecked = saveData.SD_UsePDFColor;
                _ExportSizePDFST = saveData.SD_FontSize;
                ExportFont.Header = $"Aktuelle Größe: {_ExportSizePDFST}";
                _ExportSizePDFPersonal = saveData.SD_FontSizeMA;
                ExportFontMA.Header = $"Aktuelle Größe: {_ExportSizePDFPersonal}";
                RecalcPlannedHours();
                UnsavedChanges = false;
            }
        }
        private void SoftwareClosing(object? sender, CancelEventArgs e)
        {
            if (!_unsavedchanges)
            {
                return;
            }

            BestätigungsWidget window = new BestätigungsWidget();
            window.Owner = this;
            string InfoText = $"Es sind noch nicht gespeicherte Änderungen vorhanden, möchtest du diese Speichern?";
            window.SetInfoText(InfoText);
            bool? result = window.ShowDialog();

            if (result != null && result == true)
            {
                CommitSave(_savefile);
            }

        }

        //export
        private void Click_ExportCSV(object sender, RoutedEventArgs e)
        {

            if (_ActiveStandortID < 0) return;

            StringBuilder csv = new StringBuilder();
            string HeaderLine = "Datum;";
            List<EmployeeData> LocalEPCache = new List<EmployeeData>();
            if (GetStandort(_ActiveStandortID) is PlanStandortData PlanStandort)
            {
                foreach( int MID in PlanStandort.MAIDs)
                { 
                    if(GetMitarbeiter(MID) is EmployeeData ED)
                    {
                        HeaderLine += ED._MitarbeiterName + ";";
                        LocalEPCache.Add(ED);
                    }
                }
            
                csv.AppendLine(HeaderLine);

                List<DayData> Days = new List<DayData>();
                int DaysInMonth = DateTime.DaysInMonth(_currentMonth.Year, _currentMonth.Month);
                DateTime FirstDayOfMonth = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
                Int32 DayofMonth = ((int)FirstDayOfMonth.DayOfWeek + 6) % 7;

                int day = 1;
                for (int row = 0; row < 6; row++)
                {
                    for (int col = 0; col < 7; col++)
                    {
                        if ((row == 0 && col < DayofMonth) || day > DaysInMonth)
                        {
                            continue;
                        } else if((col == 5 || col == 6) && _KeineWEs)
                        {
                            day++;
                            continue;
                        }
                        DateTime currentDate = new DateTime(_currentMonth.Year, _currentMonth.Month, day);

                        DayData NewDay = new DayData();
                        NewDay._TagesDatum = currentDate;
                        NewDay.daystring = _DayAbbreviations[col];
                        foreach (SchichtInfo assignment in _Schichten)
                        {
                            if (assignment.Date.Date == currentDate.Date && assignment.SLinkedID == _ActiveStandortID)
                            {
                               NewDay.schichtIDs.Add(assignment.SchichtID);
                            }
                        }
                        Days.Add(NewDay);   
                        day++;
                    }
                }
                List<string> UsedRoles = new List<string>();
                foreach (DayData Day in Days)
                {

                    string DayLine = $"{ Day.daystring } " + Day._TagesDatum.Date.ToString("dd-MM-yyyy") + ";";
                    if (PlanStandort.SchliessTageList.Count > 0)
                    {
                        if (PlanStandort.SchliessTageList.Contains(Day._TagesDatum.Day.ToString()) || PlanStandort.SchliessTageList.Contains(Day.daystring.ToLower()))
                        {
                            foreach (EmployeeData EP in LocalEPCache)
                            {
                                DayLine += "geschlossen;";
                            }
                            csv.AppendLine(DayLine);
                            continue;
                        }

                    }
                    foreach (EmployeeData EP in LocalEPCache)
                    {
                        int commonValue = EP._ZugeteilteSchichten.Intersect(Day.schichtIDs).FirstOrDefault();

                        if (EP._ZugeteilteSchichten.Intersect(Day.schichtIDs).Any())
                        {
                            // commonValue contains the first shared int
                            if(GetSchicht(commonValue) is SchichtInfo CommonShift)
                            {
                                if (!UsedRoles.Contains(CommonShift.SchichtRolle)) UsedRoles.Add(CommonShift.SchichtRolle);
                                string RString = _UseRoleKuerzel ? GetRoleKuerzel(CommonShift.SchichtRolle) : CommonShift.SchichtRolle;
                                DayLine += "\"" + RString + Environment.NewLine + CommonShift.Zeiten.SchichtStartText + " - " + CommonShift.Zeiten.SchichtSchlussText + "\"" + ";";
                            }
                            else
                            {
                                DayLine += ";";
                            }

                        }
                        else
                        {
                            DayLine += ";";
                        }
                    }

                    csv.AppendLine(DayLine);
                }

                if (_UseRoleKuerzel)
                {

                    csv.AppendLine();
                    csv.AppendLine("Legend");
                    csv.AppendLine();

                    csv.AppendLine("Abkuerzung;Rolle");

                    foreach (string UR in UsedRoles)
                    {
                        if (GetRoleKuerzel(UR).ToLower() == UR.ToLower()) continue;

                        csv.AppendLine($"{GetRoleKuerzel(UR)};{UR}");
                    }
                }
            }
            //Save CSV
            string DefaultFileName = string.Empty;
            DefaultFileName = "Schichtplan_" + LocationText.Text + "_" + _currentMonth.Month.ToString() +"_" + _currentMonth.Year.ToString();

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "CSV Dateien (*.csv)|*.csv|Alle Dateien (*.*)|*.*";
            dialog.DefaultExt = ".csv";
            dialog.FileName = DefaultFileName;

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(dialog.FileName, csv.ToString());

                    MessageBox.Show("Erfolgreich exportiert.");
                }
                catch (IOException)
                {
                    MessageBox.Show(
                        "Export fehlgeschlagen.",
                        "Die Datei ist in einem anderen Programm geöffnet.",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
          
               
            }
        }
        private void Click_ExportPDFPerPerson(object sender, RoutedEventArgs e)
        {

            var dialog = new OpenFolderDialog();

            if (dialog.ShowDialog() == true)
            {
                string path = dialog.FolderName;
                foreach (EmployeeData ED in _Mitartbeiter)
                {
                    if(ED._VerplanteStunden <= 0) continue;
                    string DefaultFileName = "Schichtplan_" + ED._MitarbeiterName + "_" + _currentMonth.Month.ToString() + "_" + _currentMonth.Year.ToString();

                    ExportTheMAPDF(ED, path + "\\" + DefaultFileName +".pdf");
                }
            }
        }
        private void Export_PersonalPDF(int InMAID)
        {

            if (InMAID < 0 || _ExportSizePDFPersonal <= 0) return;

            if (GetMitarbeiter(InMAID) is EmployeeData Arbeiter)
            {
                string DefaultFileName = "Schichtplan_" + Arbeiter._MitarbeiterName + "_" + _currentMonth.Month.ToString() + "_" + _currentMonth.Year.ToString();

          
                    SaveFileDialog dialog = new SaveFileDialog();
                    dialog.Filter = "PDF Dateien (*.pdf)|*.pdf|Alle Dateien (*.*)|*.*";
                    dialog.DefaultExt = ".pdf";
                    dialog.FileName = DefaultFileName;

                    if (dialog.ShowDialog() == true)
                    {
                        ExportTheMAPDF(Arbeiter, dialog.FileName);
                    }
            
            }
        }

        private void ExportTheMAPDF(EmployeeData Arbeiter, string FileName)
        {
            List<SchichtInfo> SchichtList = new List<SchichtInfo>();
            foreach (int SID in Arbeiter._ZugeteilteSchichten)
            {
                if (GetSchicht(SID) is SchichtInfo Schicht)
                {
                    SchichtList.Add(Schicht);
                }
            }
            List<SchichtInfo> SortedList = SchichtList.OrderBy(Schicht => Schicht.Date).ToList();

            List<DayData> Days = new List<DayData>();
            int DaysInMonth = DateTime.DaysInMonth(_currentMonth.Year, _currentMonth.Month);
            DateTime FirstDayOfMonth = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
            Int32 DayofMonth = ((int)FirstDayOfMonth.DayOfWeek + 6) % 7;

            int day = 1;
            for (int row = 0; row < 6; row++)
            {
                for (int col = 0; col < 7; col++)
                {
                    if ((row == 0 && col < DayofMonth) || day > DaysInMonth)
                    {
                        continue;
                    }
                    else if ((col == 5 || col == 6) && _KeineWEs)
                    {
                        day++;
                        continue;
                    }
                    DateTime currentDate = new DateTime(_currentMonth.Year, _currentMonth.Month, day);

                    DayData NewDay = new DayData();
                    NewDay._TagesDatum = currentDate;
                    if (col == 0) NewDay.KW = ISOWeek.GetWeekOfYear(currentDate);
                    NewDay.daystring = _DayAbbreviations[col];
                    Days.Add(NewDay);
                    day++;
                }
            }

            bool bUseLegend = false;
            List<string> UsedRoles = new List<string>();
            List<PlanStandortData> UsedLocations = new List<PlanStandortData>();
            Document.Create(container =>
            {
                container.Page(page =>
                {

                    page.Size(PageSizes.A4);

                    page.Margin(20);

                    page.Header().Column(column =>
                    {
                        column.Item()
                            .Text("Schichtplan " + Arbeiter._MitarbeiterName + " " + _MonthMapping[_currentMonth.Month] + " " + _currentMonth.Year.ToString())
                            .FontSize(20)
                            .Bold();
                        column.Item().PaddingBottom(10);
                    });

                    page.Content().Column(column =>
                    {

                        column.Item()
                            .Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(); //Datum
                                    columns.RelativeColumn(); //Location
                                    columns.RelativeColumn(); //Rolle
                                    columns.RelativeColumn(); //Uhrzeit
                                    columns.RelativeColumn(); //Stunden
                                    columns.RelativeColumn(); //Notiz

                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(x => HeaderStyle(x)).PreventPageBreak()
                                    .Text("Datum").FontSize(_ExportSizePDFPersonal).AlignLeft();
                                    header.Cell().Element(x => HeaderStyle(x)).PreventPageBreak()
                                         .Text("Standort").FontSize(_ExportSizePDFPersonal).AlignCenter();
                                    header.Cell().Element(x => HeaderStyle(x)).PreventPageBreak()
                                         .Text("Position").FontSize(_ExportSizePDFPersonal).AlignCenter();
                                    header.Cell().Element(x => HeaderStyle(x)).PreventPageBreak()
                                         .Text("Schichtdauer").FontSize(_ExportSizePDFPersonal).AlignCenter();
                                    header.Cell().Element(x => HeaderStyle(x)).PreventPageBreak()
                                         .Text("Arbeitszeit").FontSize(_ExportSizePDFPersonal).AlignCenter();
                                    header.Cell().Element(x => HeaderStyle(x)).PreventPageBreak()
                                        .Text("Notiz").FontSize(_ExportSizePDFPersonal).AlignCenter();

                                });



                                foreach (DayData Day in Days)
                                {

                                    SchichtInfo? Schicht = null;
                                    foreach (SchichtInfo SI in SortedList)
                                    {
                                        if (SI.Date == Day._TagesDatum)
                                        {
                                            Schicht = SI;
                                            break;
                                        }
                                    }
                                    if (Schicht != null) if (AddShiftRow(table, Day, Schicht, UsedLocations, UsedRoles)) bUseLegend = true; 
                                }

                            });

                        column.Item().PaddingTop(10).Text("Monatsinfo:").Bold();
                        int TotalCount = GetMASchichtZahlen(Arbeiter._ZugeteilteSchichten, out Dictionary<string, int>? RoleTocount);
                        string InfoString = string.Empty;
                        InfoString += $"Schichtzahl: {TotalCount}";
                        if (RoleTocount != null)
                        {
                            foreach (var pair in RoleTocount)
                            {
                                string SchichtText = "";
                                if (_UseRoleKuerzel)
                                {
                                    SchichtText = GetRoleKuerzel(pair.Key);
                                    bUseLegend = true;
                                }
                                else
                                {
                                    SchichtText = pair.Key;
                                }
                                InfoString += $" | {pair.Value} x {SchichtText}";
                            }
                        }
                        column.Item().PaddingTop(5).Text(InfoString);

                        string StundenString = $"Stunden des Monats: {Arbeiter._VerplanteStunden} von {Arbeiter._ZielStunden} ";
                        column.Item().PaddingTop(5).Text(StundenString);

                        if (_UseBreakTimes) column.Item().PaddingTop(15).Text("Die Arbeitszeiten wurden unter Berücksichtigung der gesetzlichen Mindestpausen gemäß § 4 ArbZG automatisch berechnet.").SemiBold();


                       if(bUseLegend)
                       {
                            Dictionary<string, string>? abwesenheitsKuerzel = null;
                            AddLegend(column, UsedRoles, UsedLocations, abwesenheitsKuerzel);
                       }
                      
                        

                    });

                    page.Footer().Row(Row =>
                    {
                        Row.RelativeItem()
                            .Text($"Erstellt am {DateTime.Now:dd.MM.yyyy}")
                            .AlignLeft()

                            .FontSize(10);

                        Row.ConstantItem(20)
                            .AlignRight()

                            .Text(x => { x.CurrentPageNumber().FontSize(10); });

                    });
                });
            })
              .GeneratePdf(FileName);
        }

        private void AddLegend(ColumnDescriptor column, List<string>? UsedRoles, List<PlanStandortData>? UsedLocations, Dictionary<string, string>? AbwesenheitsKuerzel)
        {
            column.Item()
                       .ShowEntire() // adjust value
                       .PaddingTop(20)
                       .Column(legend =>
                       {
                           legend.Item().Text("Legende:").Bold();

                           if (_UseRoleKuerzel && UsedRoles != null && UsedRoles.Count > 0)
                           {
                               legend.Item().PaddingTop(5).Text("Positionen:");

                               foreach (string UR in UsedRoles)
                               {
                                   if (GetRoleKuerzel(UR).ToLower() == UR.ToLower())
                                       continue;

                                   string legendString = $"{GetRoleKuerzel(UR)} => {UR}";

                                   legend.Item()
                                       .PaddingTop(5)
                                       .PaddingLeft(5)
                                       .Text(legendString);
                               }
                           }

                           if (_UseStandortKuerzel && UsedLocations != null && UsedLocations.Count > 0)
                           {

                               legend.Item().PaddingTop(5).Text("Standorte:");

                               foreach (PlanStandortData UL in UsedLocations)
                               {

                                   if (GetStandortKuerzel(UL.PlanStandortId).ToLower() == UL.StandortName.ToLower()) continue;

                                   string LegendString = $"{UL.StandortKuerzel} => {UL.StandortName}";

                                   column.Item().PaddingTop(5).PaddingLeft(5).Text(LegendString);
                               }
                           }

                           if (_ShowOutOfOfficeReason && AbwesenheitsKuerzel != null && AbwesenheitsKuerzel.Count > 0)
                           {
                               legend.Item().PaddingTop(5).Text("Abwesenheiten:");

                               foreach (var pair in AbwesenheitsKuerzel)
                               {
                                   string legendString = $"{pair.Value} => {pair.Key}";

                                   legend.Item()
                                       .PaddingTop(5)
                                       .PaddingLeft(5)
                                       .Text(legendString);
                               }
                           }
                       });
        }

        bool AddShiftRow(TableDescriptor table, DayData Day, SchichtInfo Schicht, List<PlanStandortData> UsedLocations, List<string> UsedRoles)
        {
            string datum = $"{Day.daystring} {Day._TagesDatum:dd.MM}";
            string standort = "";
            bool bUseLegend = false;
            if (GetStandort(Schicht.SLinkedID) is PlanStandortData PSD)
            {
                if (!UsedLocations.Contains(PSD))
                    UsedLocations.Add(PSD);

                if(_UseStandortKuerzel)
                {
                    standort = GetStandortKuerzel(PSD.PlanStandortId);
                    bUseLegend = true;
                }
                else
                {
                    standort = PSD.StandortName;
                }
            }

            if (!UsedRoles.Contains(Schicht.SchichtRolle))
                UsedRoles.Add(Schicht.SchichtRolle);

            string rolle = "";
            if (_UseRoleKuerzel)
            {
                rolle = GetRoleKuerzel(Schicht.SchichtRolle);
                bUseLegend = true;
            }
            else
            {
                rolle = Schicht.SchichtRolle;
            }
        
            table.Cell()
                .ColumnSpan(6)
                .PreventPageBreak()
                .Table(row =>
                {
                    row.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    row.Cell().Element(x =>
                                                            CellStyle(x))
                        .Text(datum)
                        .FontSize(_ExportSizePDFPersonal);

                    row.Cell().Element(x =>
                                                            CellStyle(x))
                        .Text(standort)
                        .FontSize(_ExportSizePDFPersonal)
                        .AlignCenter();

                    row.Cell().Element(x =>
                                                            CellStyle(x))
                        .Text(rolle)
                        .FontSize(_ExportSizePDFPersonal)
                        .AlignCenter();

                    row.Cell().Element(x =>
                                                            CellStyle(x))
                        .Text($"{Schicht.Zeiten.SchichtStartText}-{Schicht.Zeiten.SchichtSchlussText}")
                        .FontSize(_ExportSizePDFPersonal)
                        .AlignCenter();

                    row.Cell().Element(x =>
                                                            CellStyle(x))
                        .Text(Schicht.Zeiten.SchichtStunden.ToString())
                        .FontSize(_ExportSizePDFPersonal)
                        .AlignRight();

                    row.Cell().Element(x =>
                                                            CellStyle(x))
                        .Text(Schicht.Notiz ?? "")
                        .FontSize(_ExportSizePDFPersonal)
                        .AlignLeft();
                });

            return bUseLegend;
        }

        private void Click_ExportPDF_MADay(object sender, RoutedEventArgs e)
        {
            if (_ActiveStandortID < 0 || _ExportSizePDFST <= 0) return;

           string DefaultFileName = "Schichtplan_" + LocationText.Text + "_" + _currentMonth.Month.ToString() + "_" + _currentMonth.Year.ToString();

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "PDF Dateien (*.pdf)|*.pdf|Alle Dateien (*.*)|*.*";
            dialog.DefaultExt = ".pdf";
            dialog.FileName = DefaultFileName;


            if (dialog.ShowDialog() == true)
            {

                List<EmployeeData> LocalEPCache = new List<EmployeeData>();
                if (GetStandort(_ActiveStandortID) is PlanStandortData PlanStandort)
                {
                    foreach (int MID in PlanStandort.MAIDs)
                    {
                        if (GetMitarbeiter(MID) is EmployeeData ED)
                        {
                            LocalEPCache.Add(ED);
                        }
                    }

                    List<DayData> Days = new List<DayData>();
                    int DaysInMonth = DateTime.DaysInMonth(_currentMonth.Year, _currentMonth.Month);
                    DateTime FirstDayOfMonth = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
                    Int32 DayofMonth = ((int)FirstDayOfMonth.DayOfWeek + 6) % 7;

                    int day = 1;
                    for (int row = 0; row < 6; row++)
                    {
                        for (int col = 0; col < 7; col++)
                        {
                            if ((row == 0 && col < DayofMonth) || day > DaysInMonth)
                            {
                                continue;
                            }
                            else if ((col == 5 || col == 6) && _KeineWEs)
                            {
                                day++;
                                continue;
                            }
                            DateTime currentDate = new DateTime(_currentMonth.Year, _currentMonth.Month, day);

                            DayData NewDay = new DayData();
                            NewDay._TagesDatum = currentDate;
                            if (col == 0) NewDay.KW = ISOWeek.GetWeekOfYear(currentDate);
                            NewDay.daystring = _DayAbbreviations[col];

                            foreach (SchichtInfo assignment in _Schichten)
                            {
                                if (assignment.Date.Date == currentDate.Date && assignment.SLinkedID == _ActiveStandortID)
                                {
                                    NewDay.schichtIDs.Add(assignment.SchichtID);
                                }
                            }
                            Days.Add(NewDay);
                            day++;
                        }
                    }

                    List<string> UsedRoles = new List<string>();

                    Document.Create(container =>
                    {
                        container.Page(page =>
                        {

                            page.Size(PageSizes.A4.Landscape());

                            page.Margin(20);

                            page.Header().Column(column =>
                            {
                                column.Item()
                                    .Text("Schichtplan " + LocationText.Text + " " + _MonthMapping[_currentMonth.Month] + " " + _currentMonth.Year.ToString())
                                    .FontSize(20)
                                    .Bold();


                                column.Item().PaddingBottom(10);
                            });

                            page.Content().Column(column =>
                            {

                                column.Item()
                                    .Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.ConstantColumn(55);
                                            foreach (EmployeeData ED in LocalEPCache)
                                            {
                                                columns.RelativeColumn();
                                            }
                                        });

                                        table.Header(header =>
                                        {
                                            header.Cell().Element(x => HeaderStyle(x)).PreventPageBreak()
                                            .Text("Datum").FontSize(_ExportSizePDFST);
                                            foreach (EmployeeData ED in LocalEPCache)
                                            {
                                                header.Cell().Element(x => HeaderStyle(x)).PreventPageBreak()
                                                .Text(ED._MitarbeiterName).FontSize(_ExportSizePDFST);
                                            }


                                        });

                                        foreach (DayData Day in Days)
                                        {
                                                table.Cell().Element(x =>
                                                            CellStyle(x))
                                                .PreventPageBreak()
                                                .Text(Day.daystring + " " + Day._TagesDatum.Date.ToString("dd")).FontSize(_ExportSizePDFST); ;
                                        

                                            if (PlanStandort.SchliessTageList.Count > 0)
                                            {

                                                if (PlanStandort.SchliessTageList.Contains(Day._TagesDatum.Day.ToString()) || PlanStandort.SchliessTageList.Contains(Day.daystring.ToLower()))
                                                {
                                                    foreach (EmployeeData EP in LocalEPCache)
                                                    {
                                                        table.Cell().Element(x =>
                                                            CellStyle(x)).PreventPageBreak().Text("geschlossen").FontSize(_ExportSizePDFST);
                                                    }
                                                    continue;
                                                }
                                              
                                            }

                                            foreach (EmployeeData EP in LocalEPCache)
                                            {
                                                int commonValue = EP._ZugeteilteSchichten.Intersect(Day.schichtIDs).FirstOrDefault();

                                                if (EP._ZugeteilteSchichten.Intersect(Day.schichtIDs).Any())
                                                {
                                                    // commonValue contains the first shared int
                                                    if (GetSchicht(commonValue) is SchichtInfo CommonShift)
                                                    {
                                                        if (!UsedRoles.Contains(CommonShift.SchichtRolle)) UsedRoles.Add(CommonShift.SchichtRolle);
                                                        string RString = _UseRoleKuerzel ? GetRoleKuerzel(CommonShift.SchichtRolle) : CommonShift.SchichtRolle;
                                                        table.Cell().Element(x =>
                                                            CellStyle(x))
                                                        .PreventPageBreak()
                                                        .Text(RString + Environment.NewLine + CommonShift.Zeiten.SchichtStartText + "-" + CommonShift.Zeiten.SchichtSchlussText).FontSize(_ExportSizePDFST);
                                                    }
                                                    else
                                                    {
                                                        table.Cell().Element(x =>
                                                            CellStyle(x)).Text("").FontSize(_ExportSizePDFST); ;
                                                    }

                                                }
                                                else
                                                {
                                                    table.Cell().Element(x =>
                                                            CellStyle(x)).Text("").FontSize(_ExportSizePDFST); ;
                                                }
                                            }

                                        }

                                    });

                                if (_UseRoleKuerzel)
                                {
                                    column.Item().PaddingTop(20).Text("Legende:").Bold();

                                    foreach (string UR in UsedRoles)
                                    {

                                        if (GetRoleKuerzel(UR).ToLower() == UR.ToLower()) continue;

                                        string LegendString = $"{GetRoleKuerzel(UR)} => {UR}";

                                        column.Item().PaddingTop(5).Text(LegendString);
                                    }
                                }

                            });

                            page.Footer().Row(Row =>
                            {
                                Row.RelativeItem()
                                    .Text($"Erstellt am {DateTime.Now:dd.MM.yyyy}")
                                    .AlignLeft()

                                    .FontSize(10);

                                Row.ConstantItem(20)
                                    .AlignRight()

                                    .Text(x => { x.CurrentPageNumber().FontSize(10); });

                            });
                        });
                    })
                      .GeneratePdf(dialog.FileName);
                }
            }
        }

        private void Click_ExportPDF_DayMA(object sender, RoutedEventArgs e)
        {
            if (_ActiveStandortID < 0 || _ExportSizePDFST <= 0) return;

            string DefaultFileName = "Schichtplan_" + LocationText.Text + "_" + _currentMonth.Month.ToString() + "_" + _currentMonth.Year.ToString();

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "PDF Dateien (*.pdf)|*.pdf|Alle Dateien (*.*)|*.*";
            dialog.DefaultExt = ".pdf";
            dialog.FileName = DefaultFileName;


            if (dialog.ShowDialog() == true)
            {

                List<EmployeeData> LocalEPCache = new List<EmployeeData>();
                if (GetStandort(_ActiveStandortID) is PlanStandortData PlanStandort)
                {
                    foreach (int MID in PlanStandort.MAIDs)
                    {
                        if (GetMitarbeiter(MID) is EmployeeData ED)
                        {
                            LocalEPCache.Add(ED);
                        }
                    }

                    List<DayData> Days = new List<DayData>();
                    int DaysInMonth = DateTime.DaysInMonth(_currentMonth.Year, _currentMonth.Month);
                    DateTime FirstDayOfMonth = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
                    Int32 DayofMonth = ((int)FirstDayOfMonth.DayOfWeek + 6) % 7;

                    int day = 1;
                    for (int row = 0; row < 6; row++)
                    {
                        for (int col = 0; col < 7; col++)
                        {
                            if ((row == 0 && col < DayofMonth) || day > DaysInMonth)
                            {
                                continue;
                            }
                            else if ((col == 5 || col == 6) && _KeineWEs)
                            {
                                day++;
                                continue;
                            }
                            DateTime currentDate = new DateTime(_currentMonth.Year, _currentMonth.Month, day);

                            DayData NewDay = new DayData();
                            NewDay._TagesDatum = currentDate;
                            if (col == 0) NewDay.KW = ISOWeek.GetWeekOfYear(currentDate);
                            NewDay.daystring = _DayAbbreviations[col];

                            foreach (SchichtInfo assignment in _Schichten)
                            {
                                if (assignment.Date.Date == currentDate.Date && assignment.SLinkedID == _ActiveStandortID)
                                {
                                    NewDay.schichtIDs.Add(assignment.SchichtID);
                                }
                            }
                            Days.Add(NewDay);
                            day++;
                        }
                    }

                    List<string> UsedRoles = new List<string>();

                    Document.Create(container =>
                    {
                        container.Page(page =>
                        {

                            page.Size(PageSizes.A4.Landscape());

                            page.Margin(20);

                            page.Header().Column(column =>
                            {
                                column.Item()
                                    .Text("Schichtplan " + LocationText.Text + " " + _MonthMapping[_currentMonth.Month] + " " + _currentMonth.Year.ToString())
                                    .FontSize(20)
                                    .Bold();


                                column.Item().PaddingBottom(10);
                            });

                            bool bUseLegend = false;
                            Dictionary<string, string> abwesenheitsKuerzel = new();
                            page.Content().Column(column =>
                            {

                                column.Item().Border(1).BorderColor(Black)
                                    .Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.ConstantColumn(100);
                                            foreach (DayData Day in Days)
                                            {
                                                columns.RelativeColumn();
                                            }
                                        });

                                        table.Header(header =>
                                        {
                                            header.Cell().Element(x => HeaderStyle(x)).PreventPageBreak()
                                            .Text("").FontSize(_ExportSizePDFST + 2);
                                            foreach (DayData Day in Days)
                                            {
                                                string HexValue = (Day.daystring == "Sa" || Day.daystring == "So") ? "#F0F0F0" : "#FFFFFF";
                                                float CellLeftThickness = 1;
                                                float CellRightThickness = 1;
                                                if (Day.daystring.ToLower() == "so")
                                                {
                                                    CellRightThickness = 3f;
                                                }
                                                else if (Day.daystring.ToLower() == "mo")
                                                {
                                                    CellLeftThickness = 3f;
                                                }
                                                header.Cell().Background(HexValue).Element(x =>
                                                            CellStyle(x, CellRightThickness,CellLeftThickness))
                                                .PreventPageBreak()
                                                .Text(Day.daystring + Environment.NewLine + Day._TagesDatum.Date.ToString("dd")).FontSize(_ExportSizePDFST +2 ).AlignCenter();

                                            }
                                        });

                                        foreach (EmployeeData EP in LocalEPCache)
                                        {
                                            bool bEmployeeIsInST = false;
                                            foreach (int SLID in EP._ZugeteilteSchichten)
                                            {
                                                if(GetSchicht(SLID) is SchichtInfo SIInfoLocal)
                                                {
                                                    if ( SIInfoLocal.Date.Month == _currentMonth.Date.Month && SIInfoLocal.Date.Year == _currentMonth.Date.Year &&   SIInfoLocal.SLinkedID == _ActiveStandortID)
                                                    {
                                                        bEmployeeIsInST = true;
                                                        break;
                                                    }
                                                }
                                            }
                                            if (!bEmployeeIsInST) continue;

                                            table.Cell()
                                                   .ColumnSpan((uint)(Days.Count + 1))
                                                   .PreventPageBreak()
                                                   .Table(row =>
                                                   {
                                                       row.ColumnsDefinition(columns =>
                                                       {
                                                           columns.ConstantColumn(100);
                                                           foreach (DayData Day in Days)
                                                           {
                                                               columns.RelativeColumn();
                                                           }
                                                       });

                                                       row.Cell().Element(x => HeaderStyle(x))
                                                        .Text(text => {
                                                            text.Span(EP._MitarbeiterName).FontSize(_ExportSizePDFST + 3); text.Line(""); text.Span($"Arbeitszeit: {EP._VerplanteStunden} std").FontSize(_ExportSizePDFST);
                                                        });

                                                       foreach (DayData Day in Days)
                                                       {
                                                           float CellLeftThickness = 1;
                                                           float CellRightThickness = 1;
                                                           if (Day.daystring.ToLower() == "so")
                                                           {
                                                               CellRightThickness = 3f;
                                                           }
                                                           else if (Day.daystring.ToLower() == "mo")
                                                           {
                                                               CellLeftThickness = 3f;
                                                           }

                                                           if (PlanStandort.SchliessTageList.Count > 0)
                                                           {

                                                               if (PlanStandort.SchliessTageList.Contains(Day._TagesDatum.Day.ToString()) || PlanStandort.SchliessTageList.Contains(Day.daystring.ToLower()))
                                                               {

                                                                   row.Cell().Background(QuestPDF.Helpers.Colors.Red.Darken1).Element(x =>
                                                                   CellStyle(x, CellRightThickness, CellLeftThickness)).AlignMiddle().AlignCenter().PreventPageBreak().Text("X").FontSize(10);

                                                                   continue;
                                                               }

                                                           }

                                                           if (EP._ZugeteilteSchichten.Intersect(Day.schichtIDs).Any())
                                                           {
                                                               int commonValue = EP._ZugeteilteSchichten.Intersect(Day.schichtIDs).FirstOrDefault();

                                                               // commonValue contains the first shared int
                                                               if (GetSchicht(commonValue) is SchichtInfo CommonShift)
                                                               {
                                                                   if (!UsedRoles.Contains(CommonShift.SchichtRolle)) UsedRoles.Add(CommonShift.SchichtRolle);

                                                                   string RString = "";
                                                                   if (_UseRoleKuerzel)
                                                                   {
                                                                       RString = GetRoleKuerzel(CommonShift.SchichtRolle);
                                                                       bUseLegend = true;
                                                                   }
                                                                   else
                                                                   {
                                                                       RString = CommonShift.SchichtRolle;
                                                                   }

                                                                   string ColorHex = "#FFFFFF";
                                                                   if (_UseColorForPDF && EP.ColorHex != "#3A3A3A") ColorHex = EP.ColorHex;

                                                                   QuestPDF.Infrastructure.Color c = QuestPDF.Infrastructure.Color.FromHex(ColorHex);

                                                                   // Perceived brightness
                                                                   double brightness =
                                                                           (0.299 * c.Red +
                                                                            0.587 * c.Green +
                                                                            0.114 * c.Blue);

                                                                   QuestPDF.Infrastructure.Color TextBrush = brightness > 128
                                                                           ? QuestPDF.Infrastructure.Color.FromHex("#000000")
                                                                           : QuestPDF.Infrastructure.Color.FromHex("#FFFFFF");

                                                                   if (!string.IsNullOrWhiteSpace(CommonShift.Notiz))
                                                                   {
                                                                       if (CommonShift.Notiz.Length < 4)
                                                                       {
                                                                           RString += $"({CommonShift.Notiz})";
                                                                       }
                                                                   }

                                                                   row.Cell().Background(ColorHex).Element(x =>
                                                                       CellStyle(x, CellRightThickness, CellLeftThickness))
                                                                   .PreventPageBreak()
                                                                   .Text(RString + Environment.NewLine + CommonShift.Zeiten.SchichtStartText + Environment.NewLine + CommonShift.Zeiten.SchichtSchlussText).FontSize(_ExportSizePDFST)
                                                                   .FontColor(TextBrush).AlignCenter();
                                                               }
                                                               else
                                                               {
                                                                   row.Cell().Element(x =>
                                                                       CellStyle(x, CellRightThickness, CellLeftThickness)).Text("").FontSize(_ExportSizePDFST);
                                                               }

                                                           }
                                                           else if (_ShowOutOfOfficeReason)
                                                           {
                                                               string SetString = Environment.NewLine;

                                                               if (IsDayOff(EP.AbwesendListeNew, Day._TagesDatum.Day, out string? Type, out string? TypeAB))
                                                               {
                                                                   if (!string.IsNullOrWhiteSpace(Type))
                                                                   {
                                                                       if (!string.IsNullOrWhiteSpace(TypeAB))
                                                                       {
                                                                           SetString += TypeAB.ToString();
                                                                           bUseLegend = true;
                                                                           if (!abwesenheitsKuerzel.ContainsKey(Type))
                                                                           { abwesenheitsKuerzel.Add(Type, TypeAB); }


                                                                       }
                                                                       else
                                                                       {
                                                                           SetString += Type.ToString();
                                                                       }
                                                                   }

                                                               }
                                                               else if (IsDayOff(EP.FreitagsWuensche, Day._TagesDatum.Day, out string? TypeFW, out string? TypeFWAB))
                                                               {
                                                                   if (!string.IsNullOrWhiteSpace(TypeFW))
                                                                   {

                                                                       if (!string.IsNullOrWhiteSpace(TypeFWAB))
                                                                       {
                                                                           SetString += TypeFWAB.ToString();
                                                                           bUseLegend = true;
                                                                           if (!abwesenheitsKuerzel.ContainsKey(TypeFW))
                                                                           { abwesenheitsKuerzel.Add(TypeFW, TypeFWAB); }


                                                                       }
                                                                       else
                                                                       {
                                                                           SetString += TypeFW.ToString();
                                                                       }

                                                                   }
                                                                
                                                               }

                                                               row.Cell().Element(x =>
                                                                       CellStyle(x, CellRightThickness, CellLeftThickness)).Text(SetString).FontSize(_ExportSizePDFST).AlignCenter().Bold();
                                                           }
                                                           else
                                                           {
                                                               row.Cell().Element(x =>
                                                                       CellStyle(x, CellRightThickness, CellLeftThickness)).Text("").FontSize(_ExportSizePDFST).AlignCenter();
                                                           }
                                                       }
                                                   });
                                        }


                                        //Add Schichtübersicht at End
                                        table.Cell().Element(x => HeaderStyle(x)).Text("Schichtübersicht:").FontSize(_ExportSizePDFST + 3);

                                        foreach (DayData Day in Days)
                                        {
                                            float CellLeftThickness = 1;
                                            float CellRightThickness = 1;
                                            if (Day.daystring.ToLower() == "so")
                                            {
                                                CellRightThickness = 3f;
                                            }
                                            else if (Day.daystring.ToLower() == "mo")
                                            {
                                                CellLeftThickness = 3f;
                                            }
                                            Dictionary<string, int> SchiftMapping = new();
                                            List<string> RoleStrings = new();
                                            foreach (int assignmentIDs in Day.schichtIDs)
                                            {
                                                if (GetSchicht(assignmentIDs) is SchichtInfo SIInfoLocal)
                                                {
                                                    if (SchiftMapping.TryGetValue(SIInfoLocal.SchichtRolle, out int count))
                                                    {
                                                        SchiftMapping[SIInfoLocal.SchichtRolle]++;
                                                    }
                                                    else
                                                    {
                                                        RoleStrings.Add(SIInfoLocal.SchichtRolle);
                                                        SchiftMapping.Add(SIInfoLocal.SchichtRolle, 1);
                                                    }

                                                }
                                            }
                                            RoleStrings = RoleStrings.OrderBy(x => x).ToList();

                                            string NewText = string.Empty;
                                            int Index = 0;

                                            foreach (string Role in RoleStrings)
                                            {
                                                if (Index != 0) NewText += Environment.NewLine;
                                                Index++;
                                                string rtext = _UseRoleKuerzel ? GetRoleKuerzel(Role) : Role;
                                                NewText += $"{SchiftMapping[Role]}x{rtext}";
                                            }

                                            table.Cell().Element(x =>
                                                            CellStyle(x, CellRightThickness, CellLeftThickness))
                                                       .PreventPageBreak()
                                                       .Text(NewText).FontSize(_ExportSizePDFST).AlignCenter();

                                        }

                                    });

                                if (_UseBreakTimes) column.Item().PaddingTop(15).Text("Die Arbeitszeiten wurden unter Berücksichtigung der gesetzlichen Mindestpausen gemäß § 4 ArbZG automatisch berechnet.").SemiBold();
                                if (bUseLegend)
                                {
                                    List<PlanStandortData>? PSDL = null;
                                    AddLegend(column, UsedRoles, PSDL, abwesenheitsKuerzel);
                                }

                            });

                            page.Footer().Row(Row =>
                            {
                                Row.RelativeItem()
                                    .Text($"Erstellt am {DateTime.Now:dd.MM.yyyy}")
                                    .AlignLeft()

                                    .FontSize(10);

                                Row.ConstantItem(20)
                                    .AlignRight()

                                    .Text(x => { x.CurrentPageNumber().FontSize(10); });

                            });
                        });
                    })
                      .GeneratePdf(dialog.FileName);
                }
            }
        }



        static QuestPDF.Infrastructure.IContainer HeaderStyle(QuestPDF.Infrastructure.IContainer container, float borderThicknessRight = 1, float borderThicknessLeft = 1)
        {
            return container
                .BorderBottom(1)
                .BorderTop(1)
                .BorderLeft(borderThicknessLeft)
                .BorderRight(borderThicknessRight)
                .Background(QuestPDF.Helpers.Colors.Grey.Lighten3)
                .Padding(4);
        }

        static QuestPDF.Infrastructure.IContainer CellStyle(QuestPDF.Infrastructure.IContainer container, float borderThicknessRight = 1, float borderThicknessLeft = 1)
        {
            return container
                .BorderBottom(1)
                .BorderTop(1)
                .BorderLeft(borderThicknessLeft)
                .BorderRight(borderThicknessRight)
                .BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2)
                .Padding(4);
        }
    }

    public class DayData
    {
        public DayData() { }

        public DateTime _TagesDatum { get; set; }

        public List<int> schichtIDs = new List<int>();

        public int KW { get; set; } = -1;

        public string daystring { get; set; } = string.Empty; // Mo,Di etc.
    }

    public class ShiftPlanSaveData
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int SchichtIDCounter { get; set; }

        public int MitarbeiterIDCounter { get; set; }

        public int StandortIDCounter { get; set; }

        public List<EmployeeData> SD_Employees { get; set; } = new();
        public List<SchichtInfo> SD_Assignments { get; set; } = new();

        public List<RoleData> SD_Roles { get; set; } = new();
        public List<PlanStandortData> SD_Standort { get; set; } = new();
        public bool SD_UseStandortKuerzel { get; set; }
        public bool SD_UseRollenKuerzel { get; set; }

        public bool SD_ShowAbwesenheit { get; set; }
        public bool SD_UsegesPause { get; set; }
        public float SD_FontSize { get; set; }

        public float SD_FontSizeMA { get; set; }
        public bool SD_UsePDFColor { get; set; }
    }


    //Schicht Stuff
    public class SchichtZeit
    {
        public SchichtZeit() { }
        public double SchichtStart { get; set; } = 480; //8Uhr in Minuten
        public DateTime SchichtStartDate { get; set; } //8Uhr in Minuten
        public string SchichtStartText { get; set; } = "";
        public double SchichtEnde { get; set; } = 960; //16Uhr in Minute
        public DateTime SchichtEndDate { get; set; }
        public string SchichtSchlussText { get; set; } = "";
        public double SchichtStunden { get; set; } = 8.0f; //16Uhr in Minuten
        public bool bPlusOneDay { get; set; } = false;
        public int PausenZeit { get; set; } = 0; //30 Minuten ab 6std, 45 Minuten ab 9std
    }

    public class TagesDaten
    {

        public DateTime _KalenderDatum { get; set; }

        public int TotalShifts { get; set; } = 0;

        public List<int> LinkedShifts { get; set; } = new List<int>();

        public Dictionary<string, int> SchiftMapping { get; set; } = new Dictionary<string, int>();

        public bool NotAvailableForSelectedMA { get; set; } = false;

        public bool NotAvailableForSelection { get; set; } = false;

        public bool bIsHoliday { get; set; } = false;

    }

    public class DayAutomationScore
    {

        public DateTime Datum { get; set; }
        public double weight { get; set; } = 0.0f;
        public bool bIsHoliday { get; set; } = false;
        public List<DayTemplateData> MatchingdayTemplateDatas { get; set; } = new();

    }

    public class SchichtInfo
    {
        public SchichtInfo() { }
        public int SchichtID { get; set; }
        public DateTime Date { get; set; }
        public bool bIsHoliday { get; set; } = false;
        public SchichtZeit Zeiten { get; set; } = new();
        public string SchichtRolle { get; set; } = "";
        public string Notiz { get; set; } = "";
        public int SLinkedID { get; set; } = -1;

        public int ELinkedID { get; set; } = -1;
    }

    public class EmployeeAutomation
    {
        public double Weight { get; set; }
        public int TageAmStueckBeiSchicht { get; set; }
        public EmployeeData Employee { get; set; } = new();
    }

    //Feiertags Stuff
    public class Holiday
    {
        public DateTime Date { get; set; }
        public string Name { get; set; } = "";
    }
    public static class HolidayService
    {
        public static List<Holiday> GetGermanHolidays(int year, EBundesland? InBundesLand)
        {
            List<Holiday> holidays = new();

            DateTime easter = GetEasterSunday(year);

            holidays.Add(new Holiday { Date = new DateTime(year, 1, 1), Name = "Neujahr" });
            holidays.Add(new Holiday { Date = new DateTime(year, 5, 1), Name = "Tag der Arbeit" });
            holidays.Add(new Holiday { Date = new DateTime(year, 10, 3), Name = "Tag der Deutschen Einheit" });
            holidays.Add(new Holiday { Date = new DateTime(year, 12, 25), Name = "1. Weihnachtstag" });
            holidays.Add(new Holiday { Date = new DateTime(year, 12, 26), Name = "2. Weihnachtstag" });
            holidays.Add(new Holiday { Date = easter.AddDays(-2), Name = "Karfreitag" });
            holidays.Add(new Holiday { Date = easter.AddDays(1), Name = "Ostermontag" });
            holidays.Add(new Holiday { Date = easter.AddDays(39), Name = "Christi Himmelfahrt" });
            holidays.Add(new Holiday { Date = easter.AddDays(50), Name = "Pfingstmontag" });

            if (InBundesLand != null)
            {
                switch (InBundesLand)
                {
                    case EBundesland.Bayern:
                        holidays.Add(new Holiday { Date = new DateTime(year, 1, 6), Name = "Heilige Drei Könige" });

                        holidays.Add(new Holiday { Date = new DateTime(year, 8, 15), Name = "Mariä Himmelfahrt" });

                        holidays.Add(new Holiday { Date = new DateTime(year, 11, 1), Name = "Allerheiligen" });

                        holidays.Add(new Holiday { Date = easter.AddDays(60), Name = "Fronleichnam" });
                        break;
                    case EBundesland.Brandenburg:
                        holidays.Add(new Holiday { Date = new DateTime(year, 10, 31), Name = "Reformationstag" });
                        break;
                    case EBundesland.Berlin:
                        holidays.Add(new Holiday { Date = new DateTime(year, 3, 8), Name = "Internationaler Frauentag" });
                        break;
                    case EBundesland.BadenWuerttemberg:
                        holidays.Add(new Holiday { Date = easter.AddDays(60), Name = "Fronleichnam" });
                        holidays.Add(new Holiday { Date = new DateTime(year, 1, 6), Name = "Heilige Drei Könige" });
                        holidays.Add(new Holiday { Date = new DateTime(year, 11, 1), Name = "Allerheiligen" });
                        break;
                    case EBundesland.Bremen:
                        holidays.Add(new Holiday { Date = new DateTime(year, 10, 31), Name = "Reformationstag" });
                        break;
                    case EBundesland.Hamburg:
                        holidays.Add(new Holiday { Date = new DateTime(year, 10, 31), Name = "Reformationstag" });
                        break;
                    case EBundesland.Hessen:
                        holidays.Add(new Holiday { Date = easter.AddDays(60), Name = "Fronleichnam" });
                        break;
                    case EBundesland.MecklenburgVorpommern:
                        holidays.Add(new Holiday { Date = new DateTime(year, 3, 8), Name = "Internationaler Frauentag" });
                        holidays.Add(new Holiday { Date = new DateTime(year, 10, 31), Name = "Reformationstag" });
                        break;
                    case EBundesland.Niedersachsen:
                        holidays.Add(new Holiday { Date = new DateTime(year, 10, 31), Name = "Reformationstag" });
                        break;
                    case EBundesland.NordrheinWestfalen:
                        holidays.Add(new Holiday { Date = easter.AddDays(60), Name = "Fronleichnam" });
                        holidays.Add(new Holiday { Date = new DateTime(year, 11, 1), Name = "Allerheiligen" });
                        break;
                    case EBundesland.RheinlandPfalz:
                        holidays.Add(new Holiday { Date = easter.AddDays(60), Name = "Fronleichnam" });
                        holidays.Add(new Holiday { Date = new DateTime(year, 11, 1), Name = "Allerheiligen" });
                        break;
                    case EBundesland.Saarland:
                        holidays.Add(new Holiday { Date = easter.AddDays(60), Name = "Fronleichnam" });
                        holidays.Add(new Holiday { Date = new DateTime(year, 1, 6), Name = "Heilige Drei Könige" });
                        holidays.Add(new Holiday { Date = new DateTime(year, 11, 1), Name = "Allerheiligen" });
                        break;
                    case EBundesland.Sachsen:
                        holidays.Add(new Holiday { Date = easter.AddDays(60), Name = "Fronleichnam" });
                        holidays.Add(new Holiday { Date = new DateTime(year, 10, 31), Name = "Reformationstag" });
                        holidays.Add(new Holiday { Date = GetBußUndBettag(year), Name = "Buß- und Bettag" });
                        break;
                    case EBundesland.SachsenAnhalt:
                        holidays.Add(new Holiday { Date = new DateTime(year, 10, 31), Name = "Reformationstag" });
                        holidays.Add(new Holiday { Date = new DateTime(year, 1, 6), Name = "Heilige Drei Könige" });
                        break;
                    case EBundesland.SchleswigHolstein:
                        holidays.Add(new Holiday { Date = new DateTime(year, 10, 31), Name = "Reformationstag" });
                        break;
                    case EBundesland.Thueringen:
                        holidays.Add(new Holiday { Date = new DateTime(year, 9, 20), Name = "Weltkindertag" });
                        holidays.Add(new Holiday { Date = new DateTime(year, 10, 31), Name = "Reformationstag" });
                        holidays.Add(new Holiday { Date = easter.AddDays(60), Name = "Fronleichnam" });
                        break;

                }
            }
           
            return holidays;
        }

        private static DateTime GetEasterSunday(int year)
        {
            int a = year % 19;
            int b = year / 100;
            int c = year % 100;
            int d = b / 4;
            int e = b % 4;
            int f = (b + 8) / 25;
            int g = (b - f + 1) / 3;
            int h = (19 * a + b - d - g + 15) % 30;
            int i = c / 4;
            int k = c % 4;
            int l = (32 + 2 * e + 2 * i - h - k) % 7;
            int m = (a + 11 * h + 22 * l) / 451;
            int month = (h + l - 7 * m + 114) / 31;
            int day = ((h + l - 7 * m + 114) % 31) + 1;

            return new DateTime(year, month, day);
        }

        public static bool IsHoliday(DateTime Datum,EBundesland? bundesland) 
        {
           List<Holiday> HL = GetGermanHolidays(Datum.Year,bundesland);

            foreach (Holiday H in HL)
            {
                if(H.Date == Datum) return true;
            }
            return false;
        }

        static DateTime GetBußUndBettag(int year)
        {
            var date = new DateTime(year, 11, 23);

            while (date.DayOfWeek != DayOfWeek.Wednesday)
                date = date.AddDays(-1);

            return date;
        }
    }

    //Converter
    public class DivideBySevenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double height)
                return height / 7.0;

            return 50;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    public class EnumDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Enum e)
            {
                var field = e.GetType().GetField(e.ToString());
                var attr = field?.GetCustomAttribute<DescriptionAttribute>();

                return attr?.Description ?? e.ToString();
            }

            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}