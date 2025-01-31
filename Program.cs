using System.Text;
using Newtonsoft.Json;

namespace Dom6SaveScummer;

// TODO: Handle games where there are more than one human player

// TODO: Handle 2h file properly 

// TODO: Read appsettings from scummersettings.json instead of hardcoded values

// TODO: Make it not break if a backup folder has folders in it with names that don't parse to ints

// TODO: Make it not break if a backup folder has a folder in it without a save

// TODO: On startup check whether there's a more recent backup for found games that have a backup created instead of just assuming it's up to date 

// TODO: Handle folders in the savedgames directory that DON'T have all the files inside (such as when you've connected to a MP game that hasn't started yet)

class Program
{

    
    static void Main(string[] args)
    {
        var scummer = new Scummer();
        
        Console.WriteLine($"Scummer is scumming it up. Type 'exit' to end the program.");
        string exitCheck = "";
        while (exitCheck.ToLower() != "exit")
        {
            exitCheck = Console.ReadLine();
        }
    }
}
