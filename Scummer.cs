using Newtonsoft.Json;

namespace Dom6SaveScummer;

public class Scummer
{
    ScummerSettings _settings;
    static List<TrackedGame> _trackedGames = new List<TrackedGame>();
    private Timer _timer = null;

    public Scummer()
    {
        if (!ReadSettings())
        {
            return;
        }

        // newlords directory is where Dominions 6 stores pretenders created in the Game Tools rather than for a specific game - ignore the folder
        var gameSavesFound = Directory.GetDirectories(_settings.SavedGamesDirectory).Where(x => new DirectoryInfo(x).Name.ToLower() != "newlords").ToList();

        if (gameSavesFound.Any())
        {
            foreach (var dir in gameSavesFound)
            {
                var di = new DirectoryInfo(dir);
                _trackedGames.Add(new TrackedGame(di.Name, _settings.SavedGamesDirectory, _settings.BackupDirectory));
            }
        }
        

        _timer = new Timer(TimerCallback, null, 0, _settings.NewFileCheckFrequencyInSeconds * 1000);
    }

    bool ReadSettings()
    {
        try
        {
            using (StreamReader sr = new StreamReader("scummersettings.json"))
            {
                string text = sr.ReadToEnd();
                try
                {
                    _settings = JsonConvert.DeserializeObject<ScummerSettings>(text);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to deserialize scummersettings.json."); 
                    Console.WriteLine(@"If paths use the backslash '\' make sure to double them up ('\\')");
                    Console.WriteLine(e.ToString());
                    Console.ReadLine();
                    return false;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Error reading scummersettings.json. Ensure it is named 'scummersettings.json' and located in the directory this program runs in.");
            Console.WriteLine(e);
            return false;
        }

        return true;
    }

    void TimerCallback(Object o)
    {
        Console.WriteLine($"Checking for new saves...");

        var foundGames = Directory.GetDirectories(_settings.SavedGamesDirectory).Select(x => new DirectoryInfo(x).Name).Where(y => y.ToLower() != "newlords");
        var newGames = foundGames.Where(x => _trackedGames.All(y => y.GameName != x)).ToList();
        var lostGames = _trackedGames.Where(x => !foundGames.Contains(x.GameName)).ToList();
        
        if (lostGames.Any())
        {
            foreach (var lost in lostGames)
            {
                Console.WriteLine($"Game '{lost.GameName}' no longer exists in directory of Dominions 6 saved games - deleting backups");
                Directory.Delete(Path.Combine(_settings.BackupDirectory, lost.GameName), true);
                _trackedGames.Remove(lost);
            }
        }

        foreach (var game in _trackedGames)
        {
            game.CheckForNewSave(_settings.SavedGamesDirectory, _settings.BackupDirectory);
        }

        if (newGames.Any())
        {
            foreach (var ng in newGames)
            {
                _trackedGames.Add(new TrackedGame(ng, _settings.SavedGamesDirectory, _settings.BackupDirectory));
            }
        }
    }
}
