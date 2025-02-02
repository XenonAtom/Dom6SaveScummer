using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Dom6SaveScummer;

public class Scummer
{
    ScummerSettings _settings;
    private FileSystemWatcher _watcher;
    private Regex _trnFileParseRegex;

    public Scummer()
    {
        if (!ReadSettings())
        {
            return;
        }

        string separator = Path.DirectorySeparatorChar == '\\' ? @"\\" : $"{Path.DirectorySeparatorChar}";
        _trnFileParseRegex = new Regex($@"^.*\\(?<GameName>[^{separator}]+){separator}(?<TrnFile>(early|mid|late)_[a-z]+\.trn)$");

        _watcher = new FileSystemWatcher(_settings.SavedGamesDirectory);
        _watcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.FileName | NotifyFilters.LastWrite;
        _watcher.IncludeSubdirectories = true;
        _watcher.Changed += OnChanged;
        _watcher.Created += OnCreated;
        _watcher.Filter = "*.trn";
        _watcher.EnableRaisingEvents = true;
    }

    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        if (e.ChangeType != WatcherChangeTypes.Created)
        {
            return;
        }

        CopyGameSave(e.FullPath);
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        if (e.ChangeType != WatcherChangeTypes.Changed)
        {
            return;
        }

        CopyGameSave(e.FullPath);
    }

    private void CopyGameSave(string pathToGameSave)
    {
        var match = _trnFileParseRegex.Match(pathToGameSave);
        
        if (!Directory.Exists(Path.Combine(_settings.BackupDirectory, match.Groups["GameName"].Value)))
        {
            Directory.CreateDirectory(Path.Combine(_settings.BackupDirectory, match.Groups["GameName"].Value));
            if (_settings.CopyMapFiles)
            {
                var toCopy = Directory.GetFiles(Path.Combine(_settings.SavedGamesDirectory, match.Groups["GameName"].Value))
                                                .Where(x => !x.ToLower().EndsWith(".trn") && !x.ToLower().EndsWith(".2h"))
                                                .Select(y => new FileInfo(y)).ToList();
                foreach (var f in toCopy)
                {
                    File.Copy(f.FullName, Path.Combine(_settings.BackupDirectory, match.Groups["GameName"].Value, f.Name));
                }
            }
            Console.WriteLine($"Backup directory created for game '{match.Groups["GameName"].Value}'");
        }

        var latestBackupNum = HighestBackupNumberForTurnFile(match.Groups["GameName"].Value, match.Groups["TrnFile"].Value);
        
        // FileSystemWatcher triggers multiple times on a new turn so we want to ignore if this is not the first event for a new turn
        // Adding a second to the last write time of the backup because I was seeing inconsistent results on whether it thought the copied backup
        //      was newer. Seems like some sort of precision issue with GetLastWriteTime? 
        if (latestBackupNum.HasValue && 
            File.GetLastWriteTime(pathToGameSave) <=
            File.GetLastWriteTime(Path.Combine(_settings.BackupDirectory,
                match.Groups["GameName"].Value,
                $"{latestBackupNum}",
                match.Groups["TrnFile"].Value)).AddSeconds(1))
        { 
            return;
        }

        latestBackupNum = latestBackupNum.HasValue ? ++latestBackupNum : 0;
        Directory.CreateDirectory(Path.Combine(_settings.BackupDirectory, match.Groups["GameName"].Value, $"{latestBackupNum}"));
        File.Copy(pathToGameSave, Path.Combine(_settings.BackupDirectory,
                                                                        match.Groups["GameName"].Value,
                                                                        $"{latestBackupNum}",
                                                                        match.Groups["TrnFile"].Value));
        Console.WriteLine($"Backup #{latestBackupNum.Value} created for game '{match.Groups["GameName"]}");
    }

    private int? HighestBackupNumberForTurnFile(string gameName, string trnFile)
    {
        if (!Directory.Exists(Path.Combine(_settings.BackupDirectory, gameName)))
        {
            return null;
        }
        var subDirs = Directory.GetDirectories(Path.Combine(_settings.BackupDirectory, gameName)).ToList();
        if (!subDirs.Any())
        {
            return null;
        }

        int highest = -1;
        int newNum = -2;
        foreach (var dir in subDirs)
        {
            if (Int32.TryParse(new DirectoryInfo(dir).Name, out newNum) && 
                newNum > highest && 
                File.Exists(Path.Combine(_settings.BackupDirectory, gameName, $"{newNum}", trnFile)))
            {
                highest = newNum;
            }
        }

        return highest > -1 ? highest : null;
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
}
