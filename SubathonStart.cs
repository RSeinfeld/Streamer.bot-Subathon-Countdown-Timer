//In the references tab, right click under mscorlib.dll and "Add reference from file..."
//Add System.dll as a reference
using System;
using System.Timers;

class CPHInline
{
    public System.Timers.Timer countdownTimer;
    public int subathonSecondsLeft;
    public int subathontotalTimeInSeconds;
    public int subathonCapInSeconds;
    public void Init()
    {
        countdownTimer = new System.Timers.Timer(1000);
        countdownTimer.Elapsed += OnTimedEvent;
        countdownTimer.AutoReset = true;
        countdownTimer.Enabled = true;
        countdownTimer.Stop();
    }

    public bool StartSubathon()
    {
        double maxHourValue = Convert.ToDouble(args["maxHourValue"]);
        double hourValue = Convert.ToDouble(args["hourValue"]);
        subathonCapInSeconds = (int)(maxHourValue * 3600);
        subathonSecondsLeft = (int)(hourValue * 3600 + 1);
        subathontotalTimeInSeconds = subathonSecondsLeft;
        countdownTimer.Start();
        return true;
    }

    public void OnTimedEvent(Object source, ElapsedEventArgs e)
    {
        subathonSecondsLeft--;
        TimeSpan time = TimeSpan.FromSeconds(subathonSecondsLeft);
        string countdownString = time.ToString(@"d' days 'hh\:mm\:ss");
        if (subathonSecondsLeft == 0)
        {
            StopSubathon("Subathon Complete!");
            CPH.RunAction("Subathon Done Action");
        }
        else
        {
            string subathonScene = CPH.GetGlobalVar<string>("subathonScene", true);
            string subathonSource = CPH.GetGlobalVar<string>("subathonSource", true);
            CPH.ObsSetGdiText(subathonScene, subathonSource, countdownString);
        }
    }

    public void Dispose()
    {
        countdownTimer.Dispose();
    }

    private void StopSubathon(string message)
    {
        // Set to Scene and Source of your text source
        string subathonScene = CPH.GetGlobalVar<string>("subathonScene", true);
        string subathonSource = CPH.GetGlobalVar<string>("subathonSource", true);
        CPH.ObsSetGdiText(subathonScene, subathonSource, message);
        countdownTimer.Stop();
    }

    private void AddTime(int timeToAdd)
    {
        int secondsToAdd = Convert.ToInt32(timeToAdd);
        if ((subathontotalTimeInSeconds + secondsToAdd) < subathonCapInSeconds)
        {
            subathontotalTimeInSeconds = subathontotalTimeInSeconds + secondsToAdd;
            subathonSecondsLeft = subathonSecondsLeft + secondsToAdd;
            if (secondsToAdd < 60)
            {
                string message = secondsToAdd + " seconds has been added to the Subathon Timer";
                CPH.SendMessage(message, true);
            }
            else
            {
                double secondsDouble = Convert.ToDouble(secondsToAdd);
                int timeMinutes = Convert.ToInt32(Math.Floor(secondsDouble / 60));
                int timeSeconds = Convert.ToInt32(Math.Floor(secondsDouble % 60));
                if (timeMinutes == 1 && timeSeconds == 0)
                {
                    string message = timeMinutes + " minute has been added to the Subathon Timer";
                    CPH.SendMessage(message, true);
                }
                else if (timeMinutes > 1 && timeSeconds == 0)
                {
                    string message = timeMinutes + " minutes has been added to the Subathon Timer";
                    CPH.SendMessage(message, true);
                }
                else if (timeMinutes == 1 && timeSeconds > 0)
                {
                    string message = timeMinutes + " minute and " + timeSeconds + " seconds has been added to the Subathon Timer";
                    CPH.SendMessage(message, true);
                }
                else if (timeMinutes > 1 && timeSeconds > 0)
                {
                    string message = timeMinutes + " minutes and " + timeSeconds + " seconds has been added to the Subathon Timer";
                    CPH.SendMessage(message, true);
                }
            }
        }
        else
        {
            subathonSecondsLeft = subathonSecondsLeft + (subathonCapInSeconds - subathontotalTimeInSeconds);
            subathontotalTimeInSeconds = subathonCapInSeconds;
            CPH.SendMessage("We've reached the sub-a-thon limit! No more time will be added.", true);
        }
    }

    public bool Stop()
    {
        StopSubathon("Sub-a-thon cancelled!");
        return true;
    }

    public bool SubAddTime()
    {
        double timeValue = Convert.ToDouble(args["minutesToAdd"]);
        int secondsToAdd = Convert.ToInt32(Math.Floor(timeValue * 60));
        AddTime(secondsToAdd);
        return true;
    }

    public bool MoneyAddTime()
    {
        double amountReceived = Convert.ToDouble(args["amountReceived"]);
        int secondsMultiplier = Convert.ToInt32(args["secondsMultiplier"]);
        int moneyDivide = Convert.ToInt32(args["moneyDivide"]);
        int moneyHundred = Convert.ToInt32(Math.Floor(amountReceived / moneyDivide));
        int secondsToAdd = moneyHundred * secondsMultiplier;
        AddTime(secondsToAdd);
        return true;
    }
}
