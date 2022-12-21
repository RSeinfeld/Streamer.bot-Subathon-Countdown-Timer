//In the references tab, right click under mscorlib.dll and "Add reference from file..."
//Add System.dll as a reference
using System;
using System.Timers;
using System.IO;

class CPHInline
{
    public System.Timers.Timer countdownTimer;
    public int subathonSecondsLeft;     //time left on timer
    public int subathonTotalTimeInSeconds;  //total elapsed time after adding time
    public int subathonCapInSeconds;
	public string subathonSecondsLeftFile;
	public string subathonTotalTimeInSecondsFile;
    public bool limitReached;
    public bool messageOnce;


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
		limitReached = false;
		messageOnce = false;
		double maxHourValue = Convert.ToDouble(CPH.GetGlobalVar<double>("maxHourValueGlobal"));
        double hourValue = Convert.ToDouble(args["hourValue"]); // Import arguments from UI

        TimeSpan maxDuration = TimeSpan.FromHours(maxHourValue);
        subathonCapInSeconds = (int)maxDuration.TotalSeconds; // Calculate the total length of the subathon in seconds

        TimeSpan remainingDuration = TimeSpan.FromHours(hourValue);
        subathonSecondsLeft = (int)remainingDuration.TotalSeconds; // Calculate the time remaining in seconds

        subathonTotalTimeInSeconds = subathonSecondsLeft;
        countdownTimer.Start();
        return true;
    }

    public void OnTimedEvent(Object source, ElapsedEventArgs e)
    {
        subathonSecondsLeft--;
        if (subathonSecondsLeft % 300 == 0)
        {
            Backup();
        }

        TimeSpan time = TimeSpan.FromSeconds(subathonSecondsLeft);
        string countdownString = "";
        if (time.Days > 1)
        {
            countdownString = time.ToString(@"dd' days 'hh\:mm\:ss");
        }
        else if (time.Days == 1)
        {
            countdownString = time.ToString(@"dd' day 'hh\:mm\:ss");
        }
        else
        {
            countdownString = time.ToString(@"hh\:mm\:ss");
        }

        if (subathonSecondsLeft == 0)
        {
            File.WriteAllText(subathonSecondsLeftFile, "");
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

    public void Backup()
    {
        string secondsLeftBackupDirectory = CPH.GetGlobalVar<string>("secondsLeftBackupDirectory", true);
        subathonSecondsLeftFile = (secondsLeftBackupDirectory + "subathonSecondsLeft.txt").ToString();
        subathonTotalTimeInSecondsFile = (secondsLeftBackupDirectory + "subathonTotalTimeInSeconds.txt").ToString();
        File.WriteAllText(subathonSecondsLeftFile, subathonSecondsLeft.ToString());
        File.WriteAllText(subathonTotalTimeInSecondsFile, subathonTotalTimeInSeconds.ToString());
    }

    public bool PauseSubathon()
    {
        countdownTimer.Stop();
        Backup();
        TimeSpan time = TimeSpan.FromSeconds(subathonSecondsLeft);
        string countdownString = "";
        if (time.Days > 1)
        {
            countdownString = time.ToString(@"dd' days 'hh\:mm\:ss");
        }
        else if (time.Days == 1)
        {
            countdownString = time.ToString(@"dd' day 'hh\:mm\:ss");
        }
        else
        {
            countdownString = time.ToString(@"hh\:mm\:ss");
        }

        CPH.SendMessage($"Pausing subathon timer with {countdownString} left", true);
        StopSubathon("Subathon paused...");
        return true;
    }

    public bool ResumeSubathon()
    {
		double maxHourValue = Convert.ToDouble(CPH.GetGlobalVar<double>("maxHourValueGlobal"));
        TimeSpan maxDuration = TimeSpan.FromHours(maxHourValue);
        subathonCapInSeconds = (int)maxDuration.TotalSeconds; // Calculate the total length of the subathon in seconds
        if (!string.IsNullOrEmpty(subathonSecondsLeftFile))
        {
            subathonSecondsLeft = Int32.Parse(subathonSecondsLeftFile) + 1; // Resuming seconds left from backup
            subathonTotalTimeInSeconds = Int32.Parse(subathonTotalTimeInSecondsFile); //Recalling total time elapsed
            TimeSpan time = TimeSpan.FromSeconds(subathonSecondsLeft -1);
            string countdownString = "";
            if (time.Days > 1)
            {
                countdownString = time.ToString(@"dd' days 'hh\:mm\:ss");
            }
            else if (time.Days == 1)
            {
                countdownString = time.ToString(@"dd' day 'hh\:mm\:ss");
            }
            else
            {
                countdownString = time.ToString(@"hh\:mm\:ss");
            }
            CPH.SendMessage($"Resuming subathon timer with {countdownString} left", true);
			countdownTimer.Start();
        }
        else
        {
            CPH.SendMessage("Cannot resume subathon because there is no evidence of a previous subathon.", true);
        }

        return true;
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
        if ((subathonCapInSeconds - (subathonTotalTimeInSeconds + secondsToAdd)) > 0 )
        {
            subathonTotalTimeInSeconds = subathonTotalTimeInSeconds + secondsToAdd;
            subathonSecondsLeft = subathonSecondsLeft + secondsToAdd;
        }
        else
        {
            subathonSecondsLeft = subathonSecondsLeft + (subathonCapInSeconds - subathonTotalTimeInSeconds);
			secondsToAdd = subathonCapInSeconds - subathonTotalTimeInSeconds;
            subathonTotalTimeInSeconds = subathonCapInSeconds;
			limitReached = true;
        }
        // Calculate the number of hours, minutes, and seconds added
            double secondsDouble = Convert.ToDouble(secondsToAdd);
            int timeHours = Convert.ToInt32(Math.Floor(secondsDouble / 3600));
            int timeMinutes = Convert.ToInt32(Math.Floor((secondsDouble % 3600) / 60));
            int timeSeconds = Convert.ToInt32(Math.Floor(secondsDouble % 60));
            // Build the message string
            string message = "";
            if (timeHours > 0)
            {
                message += $"{timeHours} hour{(timeHours > 1 ? "s" : "")}";
            }

            if (timeMinutes > 0)
            {
                if (!string.IsNullOrEmpty(message))
                {
                    message += " ";
                }

                message += $"{timeMinutes} minute{(timeMinutes > 1 ? "s" : "")}";
            }

            if (timeSeconds > 0)
            {
                if (!string.IsNullOrEmpty(message))
                {
                    message += " ";
                }

                message += $"{timeSeconds} second{(timeSeconds > 1 ? "s" : "")}";
            }

            if (secondsToAdd == 0)
            {
                message += "No time";
            }

            message += " has been added to the subathon countdown timer";
            CPH.SendMessage(message, true);
			if (limitReached == true && messageOnce == false)
            {
                CPH.SendMessage("Congratulations! The subathon limit has been reached!",true);
                messageOnce = true;
            }
    }

    public bool Stop()
    {
        StopSubathon("Subathon cancelled!");
        string secondsLeftBackupDirectory = CPH.GetGlobalVar<string>("secondsLeftBackupDirectory", true);
        File.WriteAllText(secondsLeftBackupDirectory, "");
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
