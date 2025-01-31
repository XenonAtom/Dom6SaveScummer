using System.Text.RegularExpressions;

namespace Dom6SaveScummer;

public class TrackedGame
{
    public string GameName;
    public string Age;
    public string Nation;
    public DateTime? LastWriteTime;
    public int LastBackupNumber;

    public string HFile => $"{Age}_{Nation}.2h";
    public string TrnFile => $"{Age}_{Nation}.trn";

    /// <summary>
    /// Constructor for TrackedGame which ensures that the initial backup save is created
    /// </summary>
    /// <param name="gameName">Name of the game (should match the folder for the game)</param>
    /// <param name="pathToGameSaves">Full path to the directory where Dominions is writing saves</param>
    /// <param name="pathToBackups">Full path to the directory the scummer is writing backup saves to</param>
    public TrackedGame(string gameName, string pathToGameSaves, string pathToBackups)
    {
        GameName = gameName;
        
        if (Directory.Exists(Path.Combine(pathToBackups, gameName)))
        {
            InitializeFromExistingBackups(gameName, pathToGameSaves, pathToBackups);
            Console.WriteLine($"Game '{gameName}' - already found in backup directory");
        }
        else
        {
            CreateBackupForNewGame(gameName, pathToGameSaves, pathToBackups);
            Console.WriteLine($"New game {GameName} - Backup created");
        }
    }

    void InitializeFromExistingBackups(string gameName, string pathToGameSaves, string pathToBackups)
    {
        var pathToBackupsForGame = Path.Combine(pathToBackups, gameName);
        
        // WARNING: THIS ASSUMES THAT ALL SUBDIRECTORY NAMES CLEANLY PARSE TO INTS && THAT THE MOST RECENT FOUND FOLDER DOES HAVE A SAVE
        var subdirs = Directory.GetDirectories(pathToBackupsForGame).Select(x => int.Parse(new DirectoryInfo(x).Name)).ToList();
        subdirs.Sort();
        LastBackupNumber = subdirs.Last();

        var trnFile = Directory
            .GetFiles(Path.Combine(pathToBackupsForGame, $"{LastBackupNumber}")).First(x => x.ToLower().EndsWith(".trn"));
        
        var fileInfo = new FileInfo(trnFile);
        Regex ageAndNationRegex = new Regex(@"^(?<Age>early|mid|late+)_(?<Nation>[a-z]+)\.trn$");
        var match = ageAndNationRegex.Match(fileInfo.Name);
        Age = match.Groups["Age"].Value;
        Nation = match.Groups["Nation"].Value;

        LastWriteTime = fileInfo.LastWriteTime;
    }

    void CreateBackupForNewGame(string gameName, string pathToGameSaves, string pathToBackups)
    {
        var backupDirForGame = Path.Combine(pathToBackups, gameName);
        Directory.CreateDirectory(backupDirForGame);
        var filesToCopy = Directory.GetFiles(Path.Combine(pathToGameSaves, gameName))
            .Where(x => !x.ToLower().EndsWith("trn") && !x.ToLower().EndsWith("2h")).Select(y => new FileInfo(y));
        
        foreach (var f in filesToCopy)
        {
            File.Copy(f.FullName, Path.Combine(backupDirForGame, f.Name));
        }

        Directory.CreateDirectory(Path.Combine(pathToBackups, gameName, "0"));
        
        var trnFile = new FileInfo(Directory.GetFiles(Path.Combine(pathToGameSaves, gameName)).First(x => x.ToLower().EndsWith(".trn")));
        File.Copy(trnFile.FullName, Path.Combine(backupDirForGame, "0", trnFile.Name));
        File.Copy(Path.ChangeExtension(trnFile.FullName, ".2h"), Path.Combine(backupDirForGame, "0", Path.ChangeExtension(trnFile.Name, ".2h")));
        
        Regex ageAndNationRegex = new Regex(@"^(?<Age>early|mid|late+)_(?<Nation>[a-z]+)\.trn$");
        var match = ageAndNationRegex.Match(trnFile.Name.Remove(trnFile.Name.Length - trnFile.Extension.Length, trnFile.Extension.Length));
        Age = match.Groups["Age"].Value;
        Nation = match.Groups["Nation"].Value;

        LastWriteTime = trnFile.LastWriteTime;
    }

    public void CheckForNewSave(string pathToGameSaves, string pathToBackups)
    {
        FileInfo domSave = new FileInfo(Path.Combine(pathToGameSaves, GameName, TrnFile));
        
        if (domSave.LastWriteTime > LastWriteTime)
        {
            Console.WriteLine($"New save found for {GameName} - creating backup #{LastBackupNumber+1}");
            Directory.CreateDirectory(Path.Combine(pathToBackups, GameName, $"{LastBackupNumber + 1}"));
            File.Copy(domSave.FullName, Path.Combine(pathToBackups, GameName, $"{LastBackupNumber + 1}", domSave.Name));

            LastWriteTime = domSave.LastWriteTime;
            ++LastBackupNumber;
        }
    }
}
