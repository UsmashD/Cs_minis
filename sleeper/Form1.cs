using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace sleeper
{
    public partial class Form1 : Form
    {
        Label chargeLabel = new Label(); 
        Label percentageValue = new Label();
        Label percentageLabel = new Label();
        Label chargeStatus = new Label();
        Label shutDownTimerLabel = new Label();
        CheckBox shutDownTimerCheck = new CheckBox();
        Label shutDownLabel = new Label();
        CheckBox shutDownCheck = new CheckBox();
        Label hibernateLabel = new Label();
        CheckBox hibernateCheck = new CheckBox();
        NumericUpDown shutDownHour = new NumericUpDown();
        NumericUpDown shutDownMinute = new NumericUpDown();
        Label featureShutDownTimeLabel = new Label();

        DateTime shutDownTime = new DateTime();
        
        public Form1()
        {
            shutDownTime = DateTime.MinValue;
            InitializeComponent();
            this.Resize += new EventHandler(_Resize);
            
            
            chargeLabel.Text = "Charge status:";
            chargeLabel.Location = new Point(1,2);
            this.Controls.Add(chargeLabel);
           
            chargeStatus.Location = new Point(110,2);
            this.Controls.Add(chargeStatus);
            
            percentageLabel.Text = "Battery:";
            percentageLabel.Location = new Point(1,30);
            this.Controls.Add(percentageLabel);
            
            percentageValue.Location = new Point(110,30);
            this.Controls.Add(percentageValue);

            shutDownTimerLabel.Text = "Shutdown timer:";
            shutDownTimerLabel.Width = 105;
            shutDownTimerLabel.Location = new Point(1,60);
            this.Controls.Add(shutDownTimerLabel);

            shutDownTimerCheck.Location = new Point(110,58);
            this.Controls.Add(shutDownTimerCheck);

            shutDownHour.Location = new Point(1, 90);
            shutDownHour.Width = 50;
            shutDownHour.Minimum = 0;
            shutDownHour.Maximum = 23;
            shutDownHour.Visible = true;
            this.Controls.Add(shutDownHour);

            shutDownMinute.Location = new Point(60, 90);
            shutDownMinute.Width = 50;
            shutDownMinute.Minimum = 0;
            shutDownMinute.Maximum = 59;
            shutDownMinute.Value = 30;
            shutDownMinute.Visible = true;
            this.Controls.Add(shutDownMinute);

            featureShutDownTimeLabel.Location = new Point(1,120);
            featureShutDownTimeLabel.Width = 250;
            this.Controls.Add(featureShutDownTimeLabel);            

            //shutDownCheck.Location = new Point(1,140);
            //shutDownCheck.Width = 15;
            //this.Controls.Add(shutDownCheck);
            
            //shutDownLabel.Text = "Shutdown";
            //shutDownLabel.Location = new Point(20,145);
            //this.Controls.Add(shutDownLabel);
            
            hibernateCheck.Location = new Point(1,140);
            hibernateCheck.Width = 20;
            this.Controls.Add(hibernateCheck);

            hibernateLabel.Text = "Hibernate";
            hibernateLabel.Location = new Point(20,145);
            this.Controls.Add(hibernateLabel);
            

            
        }
                
        private void RefreshFeatureShutDownTimerLabel()
        {
            featureShutDownTimeLabel.Text = "Shutdown at: " + GetShutDownTime().ToString("yyyy-MM-dd HH':'mm");
            featureShutDownTimeLabel.Refresh();
        }
        private DateTime GetShutDownTime()
        {
            int hours = Decimal.ToInt32(shutDownHour.Value);
            int minutes = Decimal.ToInt32(shutDownMinute.Value);
            if (shutDownTime == DateTime.MinValue){
                DateTime currentTime = DateTime.Now;
                shutDownTime = currentTime;
                shutDownTime = shutDownTime.AddHours(hours);
                shutDownTime = shutDownTime.AddMinutes(minutes);
            }    
            return shutDownTime;
        }
        
         private void ShutDownTimerCheck(string batteryPercentInString)
        {
            int batteryPercent = Int32.Parse(batteryPercentInString.Substring(0,batteryPercentInString.Length - 1));            
            int shutDownValue = 15;
            
            if ((batteryPercent <= shutDownValue || DateTime.Now == GetShutDownTime()) && hibernateCheck.Checked == false ){
                ShotDownActivate();
            }else if((batteryPercent <= shutDownValue || DateTime.Now == GetShutDownTime()) && hibernateCheck.Checked == true){
                HibernateActivate();
            }
        } 

        private void ShotDownActivate()
        {
            System.Diagnostics.Process.Start("shutdown","/s /t 0");  
        }

        private void HibernateActivate()
        {
            //MessageBox.Show("Hibernate");
            Application.SetSuspendState(PowerState.Hibernate, true, true);  
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

        public void backgroundWorker_doWork(object sender, DoWorkEventArgs e){
            BackgroundWorker bw = sender as BackgroundWorker;

            // Extract the argument.
            int sleepPeriod = (int)e.Argument;
                while (!bw.CancellationPending){
                    PowerStatus status = SystemInformation.PowerStatus;
                    string chargeStatusText = GetChargeStatus(status);
                    string batteryPercentInString = GetBatteryPercent(status);
                    
                    RefreshBatteryPercentage(batteryPercentInString);
                    RefreshChargeStatus(chargeStatusText);

                    if(shutDownTimerCheck.Checked == true){
                        RefreshFeatureShutDownTimerLabel();
                    }else{
                        shutDownTime = DateTime.MinValue;
                        featureShutDownTimeLabel.Text = "";
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
        public void backgroundWorker_forShutDown(object sender, DoWorkEventArgs e){
            BackgroundWorker bw = sender as BackgroundWorker;

            int sleepPeriod = (int)e.Argument;
                while (!bw.CancellationPending){
                    PowerStatus status = SystemInformation.PowerStatus;
                    string batteryPercentInString = GetBatteryPercent(status);
                    
                    ShutDownTimerCheck(batteryPercentInString);
                    
                    Thread.Sleep(sleepPeriod);
                }

            e.Result = 1;
            if (bw.CancellationPending)
            {
                e.Cancel = true;
            }
           
        }  

        private void _Resize(object sender, EventArgs e)
        {
            if(this.WindowState == FormWindowState.Minimized ){
                Hide();
            }
        }

        public void notifyIconClick(object Sender, EventArgs e)    
        {
            Show();
            this.WindowState = FormWindowState.Normal; 
        }  
          

    }

    
}
