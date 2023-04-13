//In the references tab, right click under mscorlib.dll and "Add reference from file..."
//Add System.dll as a reference
using System;
using System.Timers;
using System.IO;
using Newtonsoft.Json;

class CPHInline
{
    public System.Timers.Timer countdownTimer;
    public int subathonSecondsLeft; //time left on timer
    public int subathonTotalTimeInSeconds; //total elapsed time after adding time
    public int subathonCapInSeconds;
    public int subathonElapsedSeconds;
	public int subathonCount;
    public string subathonSecondsLeftFile;
    public string subathonTotalTimeInSecondsFile;
    public string subathonElapsedSecondsFile;
	public string subathonCountFile;
    public string countdownString;
    public string countdownStringCap;
	public string countString;
    public bool timerOn;
    public bool limitReached;
    public bool messageOnce;
    public bool newSubathonConfirm;
    public bool subathonCancelConfirm;
    public class MediaInputStatus
	{
		public int mediaCursor { get; set; }
		public int mediaDuration { get; set; }
		public string mediaState { get; set; }
	}
	public enum PlayState
	{
		//https://wiki.streamer.bot/en/Sub-Actions/Code/CSharp/Available-Methods/OBS#media docs are wrong in current form
		/** Play is supposed to be 1 but only 0 works */
		Play = 0,
		/** Pause is supposed to be 2 but only 1 works */
		Pause = 1,
		/** Stop is supposed to be 3 and 3 works */
		Stop = 3,
		/** Restart is supposed to be 4 but only 2 works */
		Restart = 2
	}
    
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
		subathonCount = 0;
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
		
		ShowSubathon();
		
		StartTimer();
    }

    private void StartTimer()
    {
        int timeLeft = subathonSecondsLeft - 1;
        GetCountdownString(timeLeft);
        CPH.SendMessage($"A subathon has started at {countdownString} with a max limit of {countdownStringCap}", true);
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
        int obsConnection = CPH.GetGlobalVar<int>("subathonObsConnectionGlobal", true);
        CPH.ObsSetGdiText(subathonScene, subathonSource, message, obsConnection);

    }

    private void AddTime(int timeToAdd)
    {
        subathonCancelConfirm = false;
        int secondsToAdd = Convert.ToInt32(timeToAdd);
        if ((subathonCapInSeconds - (subathonTotalTimeInSeconds + secondsToAdd)) > 0)
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
		
		int songLengthSeconds = Convert.ToInt32(CPH.GetGlobalVar<int>("subathonEndAudioLength"));
		string subathonScene = CPH.GetGlobalVar<string>("subathonScene", true);
		string subathonSourceEndAudio = CPH.GetGlobalVar<string>("subathonEndAudio", true);
		int obsConnection = CPH.GetGlobalVar<int>("subathonObsConnectionGlobal", true);
		//if a sub comes in with a low timer value, change the song play postion so it will still end at 0 on the timer.
        if (subathonSecondsLeft < songLengthSeconds)
        {
        	int songTimer = songLengthSeconds - subathonSecondsLeft;
			int songCursor = (songTimer * 1000);
			CPH.ObsSendRaw("SetMediaInputCursor", "{\"inputName\":\"" + subathonSourceEndAudio + "\",\"mediaCursor\":" + songCursor + "}", 0);
		}
		// if a sub will push the timer over the length of the song, the song file is reset to the beginning.
		else
		{
			EndingSong(PlayState.Pause,false);
		}

        if (limitReached == true && messageOnce == false)
        {
            CPH.SendMessage("Congratulations! The subathon limit has been reached!", true);
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
		countString = subathonCount.ToString();
        if (subathonSecondsLeft <= 0)
        {
            GetDirectory();
            File.WriteAllText(subathonSecondsLeftFile, "");
            StopSubathon("Subathon Complete!");
            EndingSong(PlayState.Pause, false); //pause and hide
            CPH.RunAction("Subathon Done Action");
        }
        else
        {
            string subathonScene = CPH.GetGlobalVar<string>("subathonScene", true);
            string subathonSource = CPH.GetGlobalVar<string>("subathonSource", true);
			string subathonSourceCount = CPH.GetGlobalVar<string>("subathonSourceCount", true);
            int obsConnection = CPH.GetGlobalVar<int>("subathonObsConnectionGlobal", true);
            CPH.ObsSetGdiText(subathonScene, subathonSource, countdownString, obsConnection);
			CPH.ObsSetGdiText(subathonScene, subathonSourceCount, countString, obsConnection);
			
			int songLengthSeconds = Convert.ToInt32(CPH.GetGlobalVar<int>("subathonEndAudioLength"));
			if (subathonSecondsLeft <= songLengthSeconds)
			{
				
				EndingSong(PlayState.Play, true); //play and show
			}
			else
			{
				EndingSong(PlayState.Pause, false); //pause and hide
			}
			
        }
    }

    private void GetDirectory()
    {
        string secondsLeftBackupDirectory = CPH.GetGlobalVar<string>("secondsLeftBackupDirectory", true);
        subathonSecondsLeftFile = (secondsLeftBackupDirectory + "subathonSecondsLeft.txt").ToString();
        subathonTotalTimeInSecondsFile = (secondsLeftBackupDirectory + "subathonTotalTimeInSeconds.txt").ToString();
        subathonElapsedSecondsFile = (secondsLeftBackupDirectory + "subathonElapsedSeconds.txt").ToString();
		subathonCountFile = (secondsLeftBackupDirectory + "subathonTotalCount.txt").ToString();
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
		if (!File.Exists(subathonCountFile)) 
        {
            File.Create(subathonCountFile).Dispose();
            CPH.LogDebug($"Created the file '{subathonCountFile}'");
        }
    }

    private void BackupWriteToFile()
    {
        GetDirectory();
        CPH.Wait(100);
        CPH.LogDebug(subathonSecondsLeftFile);
        CPH.LogDebug(subathonTotalTimeInSecondsFile);
        CPH.LogDebug(subathonElapsedSecondsFile);
		CPH.LogDebug(subathonCountFile);
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
		using (StreamWriter writer = new StreamWriter(subathonCountFile))
        {
            writer.WriteLine(subathonCount);
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
	
	private void EndingSong(PlayState state, bool visible)
	{
		// DOCUMENTATION STATES NOT ACCURATE FOR MEDIASTATE 'fixed' with enum PlayState

		string subathonScene = CPH.GetGlobalVar<string>("subathonScene", true);
		string subathonSourceEndAudio = CPH.GetGlobalVar<string>("subathonEndAudio", true);
		int obsConnection = CPH.GetGlobalVar<int>("subathonObsConnectionGlobal", true);
		CPH.ObsSetSourceVisibility(subathonScene, subathonSourceEndAudio, visible, obsConnection);
		CPH.ObsSetMediaState(subathonScene, subathonSourceEndAudio, (int)state, obsConnection);
	}
	
    public bool StartSubathon()
    {
        // Check if timer is currently running
        if (timerOn)
        {
            CPH.SendMessage("Error: Subathon countdown timer is currently running.", true);
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
            CPH.SendMessage("Error: Subathon countdown timer is currently running.", true);
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
                subathonCount = Int32.Parse(File.ReadAllText(subathonCountFile)); // Recall added subs
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
            CPH.SendMessage("Error: Subathon countdown timer is not currently running.", true);
            return false;
        }

        // Backup remaining time to file
        BackupWriteToFile();
        // Message to chat that timer is paused
        int timeLeft = subathonSecondsLeft;
        GetCountdownString(timeLeft);
        int songLengthSeconds = Convert.ToInt32(CPH.GetGlobalVar<int>("subathonEndAudioLength"));
        if (timeLeft < songLengthSeconds)
        {
            EndingSong(PlayState.Pause, true); //pause without restarting if currently playing
        }
        else
        {
            EndingSong(PlayState.Pause, false); //pause and hide
        }
        CPH.SendMessage($"Pausing subathon countdown timer with {countdownString} left", true);
        StopSubathon("Subathon paused...");
        return true;
    }

    public bool CancelSubathon()
    {
        if (!timerOn)
        {
            CPH.SendMessage("Error: Subathon countdown timer is not currently running.", true);
            return false;
        }
        else
        {
            if (!subathonCancelConfirm)
            {
                CPH.SendMessage("Are you sure you want to cancel the subathon? Type !subathoncancel again to cancel the subathon", true);
                subathonCancelConfirm = true;
                return false;
            }
            else
            {
                GetDirectory();
                File.WriteAllText(subathonSecondsLeftFile, "");
                CPH.SendMessage("The subathon has been cancelled!", true);
                StopSubathon("Subathon cancelled!");
				EndingSong(PlayState.Pause,false);
            }
        }

        return true;
    }
	
	public bool ShowSubathon()
	{
		string subathonScene = CPH.GetGlobalVar<string>("subathonScene", true);
		string subathonSource = CPH.GetGlobalVar<string>("subathonSource", true);
		string subathonSourceCount = CPH.GetGlobalVar<string>("subathonSourceCount", true);
		int obsConnection = CPH.GetGlobalVar<int>("subathonObsConnectionGlobal", true);
		CPH.ObsSetSourceVisibility(subathonScene, subathonSource, true, obsConnection);
		CPH.ObsSetSourceVisibility(subathonScene, subathonSourceCount, true, obsConnection);
	
		return true;
	}
	
	public bool HideSubathon()
	{
		string subathonScene = CPH.GetGlobalVar<string>("subathonScene", true);
		string subathonSource = CPH.GetGlobalVar<string>("subathonSource", true);
		string subathonSourceCount = CPH.GetGlobalVar<string>("subathonSourceCount", true);
		string subathonSourceEndAudio = CPH.GetGlobalVar<string>("subathonEndAudio", true);
		int obsConnection = CPH.GetGlobalVar<int>("subathonObsConnectionGlobal", true);
		CPH.ObsSetSourceVisibility(subathonScene, subathonSource, false, obsConnection);
		CPH.ObsSetSourceVisibility(subathonScene, subathonSourceCount, false, obsConnection);
		CPH.ObsSetSourceVisibility(subathonScene, subathonSourceEndAudio, false, obsConnection);
		CPH.ObsSetGdiText(subathonScene, subathonSource, "00:00:00", obsConnection);
		CPH.ObsSetMediaSourceFile(subathonScene, subathonSourceEndAudio, "", obsConnection);
		
		return true;
	}

    public bool SubAddTime()
    {
        double timeValue = Convert.ToDouble(args["minutesToAdd"]);
        int secondsToAdd = Convert.ToInt32(Math.Floor(timeValue * 60));
        AddTime(secondsToAdd);
        return true;
    }
	
	public bool SubAddCount()
    {
		int pointsToAdd = Convert.ToInt32(args["pointsToAdd"]);
		subathonCount += pointsToAdd;
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
	
	public bool MoneyAddCount()
    {
		double amountReceived = Convert.ToDouble(args["amountReceived"]);
        int moneyDivide = Convert.ToInt32(args["moneyDivide"]);
        int moneyHundred = Convert.ToInt32(Math.Floor(amountReceived / moneyDivide));
		int pointValue = Convert.ToInt32(args["pointsToAdd"]);
		int pointsToAdd = moneyHundred * pointValue;
		subathonCount += pointsToAdd;
        return true;
    }

    public bool CheckElapsed()
    {
        if (!timerOn)
        {
            CPH.SendMessage("Error: Subathon countdown timer is not currently running.", true);
            return false;
        }

        int timeElapsed = subathonElapsedSeconds;
        GetCountdownString(timeElapsed);
        CPH.SendMessage($"The subathon has been going for {countdownString}", true);
        return true;
    }
}
