using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Media;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;

namespace batteryWatcher
{
    public partial class Form1 : Form
    {
        Label chargeLabel = new Label(); 
        Label chargeStatus = new Label();
        Label percentageLabel = new Label();
        Label percentageValue = new Label();
        Label recommendedActionLabel = new Label();
        Label recommendedActionValue = new Label();
        Label boundariesLabel = new Label();
        Label boundariesValue = new Label();
        Label ownerLabel = new Label();
        int recommendedActionCode = -1;
        Settings settings;
        string JSONFile = "settings.json";
        
        public Form1()
        {
            readJson(JSONFile);
            InitializeComponent();
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            
            this.Resize += new EventHandler(_Resize);
            setNotifyText("");

            chargeLabel.Text = "Charge status:";
            chargeLabel.Location = new Point(1,2);
            this.Controls.Add(chargeLabel);
           
            chargeStatus.Location = new Point(150,2);
            this.Controls.Add(chargeStatus);
            
            percentageLabel.Text = "Battery:";
            percentageLabel.Location = new Point(1,30);
            this.Controls.Add(percentageLabel);
            
            percentageValue.Location = new Point(150,30);
            this.Controls.Add(percentageValue);

            recommendedActionLabel.Text = "Recommended action: ";
            recommendedActionLabel.Location = new Point(1, 60);
            recommendedActionLabel.Width = 150;
            this.Controls.Add(recommendedActionLabel);

            recommendedActionValue.Location = new Point(150, 60);
            recommendedActionValue.Width = 200;
            this.Controls.Add(recommendedActionValue);

            boundariesLabel.Text = "Boundaries (U/L): ";
            boundariesLabel.Location = new Point(1, 90);
            boundariesLabel.Width = 150;
            this.Controls.Add(boundariesLabel);

            boundariesValue.Location = new Point(150, 90);
            boundariesValue.Width = 200;
            this.Controls.Add(boundariesValue);

            ownerLabel.Text = "lgydevelop";
            ownerLabel.Font = new Font("Arial", 7,FontStyle.Italic);
            ownerLabel.Dock = DockStyle.Bottom;
            this.Controls.Add(ownerLabel);
            
        }

        private void readJson(string fileName){
            string jsonString = File.ReadAllText(fileName);
            settings = JsonSerializer.Deserialize<Settings>(jsonString);
        }

        private async void readJsonAsync(string fileName){          
            using (FileStream fs = File.OpenRead(fileName))
            {
                settings = await JsonSerializer.DeserializeAsync<Settings>(fs);
            }
             
        }
        private static string GetChargeStatus(PowerStatus status)
        {
            string chargeStatus = "";             
            chargeStatus = status.PowerLineStatus.ToString();  
            return chargeStatus;
        }
          private void RefreshChargeStatus(string statusText)
        {
            chargeStatus.Text = statusText;
            chargeStatus.Refresh();
        }

        private static string GetBatteryPercent(PowerStatus status)
        {
            string batteryPercent = "";
            batteryPercent = status.BatteryLifePercent.ToString("P0");
            return batteryPercent;
        }
        private void RefreshBatteryPercentage(string batteryPercentInString)
        {       
            percentageValue.Text = batteryPercentInString;
            percentageValue.Refresh();
        }

        private int CalculateRecommendedAction(string batteryStatus, string batteryPercentInString){
            int actionCode = -1;
            int batteryPercent = Int32.Parse(batteryPercentInString.Substring(0, batteryPercentInString.Length -1));
            
            if(batteryStatus == "Online"){
                if(batteryPercent >= settings.upperPercent){
                    actionCode = 1;
                }

            }else if(batteryStatus == "Offline"){

                if(batteryPercent <= settings.lowerPercent){
                    actionCode = 0;
                }else if(settings.isHibernateActive){
                    if(batteryPercent <= settings.hibernateWarningPercent){
                        actionCode = 2;
                    }
                }

            }else{
                // Do some exception handle if needed
            }
        return actionCode;
        }

        private string GetRecommendedActionText(int actionCode){
            string actionText = "";

            switch (actionCode){
                case 0:
                    actionText = "Charger plug in recommended!";
                    break;
                case 1:
                    actionText = "Charger unplug recommended!";
                    break;
                case 2:
                    actionText = $"At {settings.hibernatePercent}% the computer will be hibernated!";
                    break;
                
                default:
                    actionText = "No further action required!";
                    break;
            }
            return actionText;
        }

        private void setNotifyText(string notifyText){
            notifyIcon.Text = notifyText;
        }

        private void RefreshRecommendedActionText(int actionCode){
            recommendedActionValue.Text = GetRecommendedActionText(actionCode);
            recommendedActionValue.Refresh();
        }

        private void SendRecommendedActionNotification(int actionCode){
            string actionText = GetRecommendedActionText(actionCode);
            
            if (actionCode != -1 ){
                notifyIcon.BalloonTipIcon = ToolTipIcon.Warning;

            notifyIcon.BalloonTipText = actionText;
            notifyIcon.BalloonTipTitle ="Battery Watcher";
            notifyIcon.ShowBalloonTip(500);
            }
        }

        private void RefreshBoundariesText(){
            string boundariesText = $"{settings.upperPercent.ToString()} / {settings.lowerPercent.ToString()}";
            boundariesValue.Text = boundariesText;
        }

        
        private void Hibernate(string batteryPercentInString, string batteryStatus)
        {
            int batteryPercent = Int32.Parse(batteryPercentInString.Substring(0,batteryPercentInString.Length - 1));
            
            if(batteryPercent <= settings.hibernatePercent && batteryStatus == "Offline"){
                Application.SetSuspendState(PowerState.Hibernate, true, true);  
            }
        }

        private void _Resize(object sender, EventArgs e)
        {
            if(this.WindowState == FormWindowState.Minimized ){
                Hide();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e) {
                    
            if (e.CloseReason == CloseReason.UserClosing) {
                notifyIcon.Icon = null;
            }
            base.OnFormClosing(e);
        }

        public void  notifyIconClick(object Sender, EventArgs e)    
        {
            Show();
            this.WindowState = FormWindowState.Normal; 
        }  

         public void backgroundWorker_watchBatteryStatus(object sender, DoWorkEventArgs e){
            BackgroundWorker bw = sender as BackgroundWorker;

            // Extract the argument.
            int sleepPeriod = (int)e.Argument;
                while (!bw.CancellationPending){                    
                    readJsonAsync(JSONFile);

                    PowerStatus status = SystemInformation.PowerStatus;
                    string chargeStatusText = GetChargeStatus(status);
                    string batteryPercentInString = GetBatteryPercent(status);
                    int actionCode = CalculateRecommendedAction(chargeStatusText, batteryPercentInString);
                    string actionText = GetRecommendedActionText(actionCode);
                    
                    RefreshBatteryPercentage(batteryPercentInString);
                    RefreshChargeStatus(chargeStatusText);
                    RefreshRecommendedActionText(actionCode);
                    RefreshBoundariesText();
                    setNotifyText(actionText);
                    Hibernate(batteryPercentInString, chargeStatusText);
                    

                    if(recommendedActionCode != actionCode){    
                        
                        SystemSounds.Beep.Play();
                        Thread.Sleep(500);
                        SystemSounds.Beep.Play();
                        Thread.Sleep(500);
                        
                        SendRecommendedActionNotification(actionCode);
                        recommendedActionCode = actionCode;
                    }

                    
                    Thread.Sleep(sleepPeriod);
                }

            // Start the time-consuming operation.
            e.Result = 1;
            
            // If the operation was canceled by the user, 
            // set the DoWorkEventArgs.Cancel property to true.
            if (bw.CancellationPending)
            {
                e.Cancel = true;
            }       
        }    
    }
}
