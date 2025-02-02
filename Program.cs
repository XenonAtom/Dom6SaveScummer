using System.Text;
using Newtonsoft.Json;

namespace Dom6SaveScummer;

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
