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
    public int subathonElapsedSeconds;
	public string subathonSecondsLeftFile;
	public string subathonTotalTimeInSecondsFile;
    public string subathonElapsedSecondsFile;
    public string countdownString;
    public string countdownStringCap;
    public bool timerOn;
    public bool limitReached;
    public bool messageOnce;
    public bool newSubathonConfirm; 
    public bool subathonCancelConfirm;


    public void Init()
    {
        countdownTimer = new System.Timers.Timer(1000);
        countdownTimer.Elapsed += OnTimedEvent;
        countdownTimer.AutoReset = true;
        countdownTimer.Enabled = true;
        countdownTimer.Stop();
    }

    public void Dispose()
    {
        countdownTimer.Dispose();
    }

    private void StartSubathonTimer()
    {
        limitReached = false;
		messageOnce = false;
        subathonCancelConfirm = false;
        subathonElapsedSeconds = 0;

        // Import arguments from UI
		double maxHourValue = Convert.ToDouble(CPH.GetGlobalVar<double>("maxHourValueGlobal"));
        double hourValue = Convert.ToDouble(args["hourValue"]);

        // Calculate the total length of the subathon in seconds
        TimeSpan maxDuration = TimeSpan.FromHours(maxHourValue);
        subathonCapInSeconds = (int)maxDuration.TotalSeconds;

        // Calculate the time remaining in seconds
        TimeSpan remainingDuration = TimeSpan.FromHours(hourValue);
        subathonSecondsLeft = (int)remainingDuration.TotalSeconds + 1;

        subathonTotalTimeInSeconds = subathonSecondsLeft; // This is used to calculate when the subathon limit has been reached.
        BackupWriteToFile();
        StartTimer();
    }

    private void StartTimer()
    {
        int timeLeft = subathonSecondsLeft - 1;
        GetCountdownString(timeLeft);
        CPH.SendMessage($"A subathon has started at {countdownString} with a max limit of {countdownStringCap}",true);
        CPH.RunAction("Subathon Action Group Enable");
        countdownTimer.Start();
		timerOn = true;
        newSubathonConfirm = false;
        subathonCancelConfirm = false;
    }


    private void StopSubathon(string message)
    {
        // Stop timer
        countdownTimer.Stop();
        CPH.RunAction("Subathon Action Group Disable");
        timerOn = false;
        subathonCancelConfirm = false;

        // Set to Scene and Source of your text source
        string subathonScene = CPH.GetGlobalVar<string>("subathonScene", true);
        string subathonSource = CPH.GetGlobalVar<string>("subathonSource", true);
        CPH.ObsSetGdiText(subathonScene, subathonSource, message);
    }

    private void AddTime(int timeToAdd)
    {
        subathonCancelConfirm = false;
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

    private void OnTimedEvent(Object source, ElapsedEventArgs e)
    {
        subathonSecondsLeft--;
        subathonElapsedSeconds++;
        if (subathonSecondsLeft % 300 == 0)
        {
            BackupWriteToFile();
        }

        int timeLeft = subathonSecondsLeft;
        GetCountdownString(timeLeft);

        if (subathonSecondsLeft == 0)
        {
            GetDirectory();
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

	private void GetDirectory()
	{
		string secondsLeftBackupDirectory = CPH.GetGlobalVar<string>("secondsLeftBackupDirectory", true);
        subathonSecondsLeftFile = (secondsLeftBackupDirectory + "subathonSecondsLeft.txt").ToString();
        subathonTotalTimeInSecondsFile = (secondsLeftBackupDirectory + "subathonTotalTimeInSeconds.txt").ToString();
        subathonElapsedSecondsFile = (secondsLeftBackupDirectory + "subathonElapsedSeconds.txt").ToString();
        // Check if the directory exists, create it if it doesn't
        if (!Directory.Exists(secondsLeftBackupDirectory))
        {
            Directory.CreateDirectory(secondsLeftBackupDirectory);
            CPH.LogDebug($"Created the directory '{secondsLeftBackupDirectory}'");
        }
        if (!File.Exists(subathonSecondsLeftFile)) 
        {
            File.Create(subathonSecondsLeftFile).Dispose();
            CPH.LogDebug($"Created the file '{subathonSecondsLeftFile}'");
        }
        if (!File.Exists(subathonTotalTimeInSecondsFile)) 
        {
            File.Create(subathonTotalTimeInSecondsFile).Dispose();
            CPH.LogDebug($"Created the file '{subathonTotalTimeInSecondsFile}'");
        }
        if (!File.Exists(subathonElapsedSecondsFile)) 
        {
            File.Create(subathonElapsedSecondsFile).Dispose();
            CPH.LogDebug($"Created the file '{subathonElapsedSecondsFile}'");
        }
	}

    private void BackupWriteToFile()
    {
        GetDirectory();
        CPH.Wait(100);
        CPH.LogDebug(subathonSecondsLeftFile);
        CPH.LogDebug(subathonTotalTimeInSecondsFile);
        CPH.LogDebug(subathonElapsedSecondsFile);
        using (StreamWriter writer = new StreamWriter(subathonSecondsLeftFile))
        {
            writer.WriteLine(subathonSecondsLeft);
        }
        using (StreamWriter writer = new StreamWriter(subathonTotalTimeInSecondsFile))
        {
            writer.WriteLine(subathonTotalTimeInSeconds);
        }
        using (StreamWriter writer = new StreamWriter(subathonElapsedSecondsFile))
        {
            writer.WriteLine(subathonElapsedSeconds);
        }
    }

    private void GetCountdownString(int timeLeft)
    {
        TimeSpan time = TimeSpan.FromSeconds(timeLeft);
        countdownString = "";
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
        TimeSpan timeCap = TimeSpan.FromSeconds(subathonCapInSeconds);
        countdownStringCap = "";
        if (timeCap.Days > 1)
        {
            countdownStringCap = timeCap.ToString(@"dd' days 'hh\:mm\:ss");
        }
        else if (timeCap.Days == 1)
        {
            countdownStringCap = timeCap.ToString(@"dd' day 'hh\:mm\:ss");
        }
        else
        {
            countdownStringCap = timeCap.ToString(@"hh\:mm\:ss");
        }
    }

    public bool StartSubathon()
    {
        // Check if timer is currently running
		if (timerOn)
        {
            CPH.SendMessage("Error: Subathon countdown timer is currently running.",true);
            return false;
        }

        // Check if the subathon backup exists
		GetDirectory();
		if (!string.IsNullOrEmpty(File.ReadAllText(subathonSecondsLeftFile)))
		{
            if (!newSubathonConfirm)
            {
                CPH.SendMessage("A previous subathon exists. To overwrite the previous subathon, run !subathonstart again. Otherwise use !subathonresume", true);
                newSubathonConfirm = true;
                return false;
            }
            else
            {
                // If backup doesn't exist, start a new countdown
                newSubathonConfirm = false;
                StartSubathonTimer();
            }
		}
		else
		{
			StartSubathonTimer();
		}
        return true;
    }

    public bool ResumeSubathon()
    {
        // Check if timer is currently running
        if (timerOn)
        {
            CPH.SendMessage("Error: Subathon countdown timer is currently running.",true);
            return false;
        }
		else
		{
			GetDirectory();
			if (!string.IsNullOrEmpty(File.ReadAllText(subathonSecondsLeftFile)))
			{
                double maxHourValue = Convert.ToDouble(CPH.GetGlobalVar<double>("maxHourValueGlobal"));
			    TimeSpan maxDuration = TimeSpan.FromHours(maxHourValue);
			    subathonCapInSeconds = (int)maxDuration.TotalSeconds; // Calculate the total length of the subathon in seconds
				
                subathonSecondsLeft = Int32.Parse(File.ReadAllText(subathonSecondsLeftFile)) + 1; // Resuming seconds left from backup
				subathonTotalTimeInSeconds = Int32.Parse(File.ReadAllText(subathonTotalTimeInSecondsFile)); // Recalling seconds left with time added
				subathonElapsedSeconds = Int32.Parse(File.ReadAllText(subathonElapsedSecondsFile)); // Recall subathon elapsed
                StartTimer();
			}
			else
			{
				CPH.SendMessage("Cannot resume subathon because previous subathon doesn't exist", true);
			}
		}
        return true;
    }

    public bool PauseSubathon()
    {
        if (!timerOn)
        {
            CPH.SendMessage("Error: Subathon countdown timer is not currently running.",true);
            return false;
        }
        // Backup remaining time to file
        BackupWriteToFile();

        // Message to chat that timer is paused
        int timeLeft = subathonSecondsLeft;
        GetCountdownString(timeLeft);
        CPH.SendMessage($"Pausing subathon countdown timer with {countdownString} left", true);

        StopSubathon("Subathon paused...");
        return true;
    }
    public bool CancelSubathon()
    {
        if (!timerOn)
        {
            CPH.SendMessage("Error: Subathon countdown timer is not currently running.",true);
            return false;
        }
        else
        {
            if (!subathonCancelConfirm)
            {
                CPH.SendMessage("Are you sure you want to cancel the subathon? Type !subathoncancel again to cancel the subathon",true);
                subathonCancelConfirm = true;
                return false;
            }
            else
            {
                GetDirectory();
                File.WriteAllText(subathonSecondsLeftFile, "");
                CPH.SendMessage("The subathon has been cancelled!",true);
                StopSubathon("Subathon cancelled!");
            }
        }

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

    public bool CheckElapsed()
    {
        if (!timerOn)
        {
            CPH.SendMessage("Error: Subathon countdown timer is not currently running.",true);
            return false;
        }
        int timeElapsed = subathonElapsedSeconds;
        GetCountdownString(timeElapsed);
        CPH.SendMessage($"The subathon has been going for {countdownString}",true);
		return true;
    }

    public bool CheckRemaining()
    {
        if (!timerOn)
        {
            CPH.SendMessage("Error: Subathon countdown timer is not currently running.", true);
            return false;
        }

        int timeLeft = subathonSecondsLeft;
        GetCountdownString(timeLeft);
        CPH.SendMessage($"Time remaining left on the subathon is {countdownString}", true);
        return true;
    }

}
