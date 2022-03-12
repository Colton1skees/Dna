using CommandLine;
using Dna.Binary;
using Dna.Binary.Windows;
using Dna.ControlFlow;

class Program
{
    public class Options
    {
        [Option("binary", Required = true, HelpText = "Filepath to binary")]
        public string Binary { get; set;  }
        
        [Option("base", Required = true, HelpText = "Base address")]
        public ulong Base { get; set;  }
        
        [Option("start", Required = true, HelpText = "Address to begin analysis from")]
        public ulong Start { get; set;  }
    }
    
    static void Main(string[] args)
    {
        var binaryFilepath = "";
        ulong baseAddr = 0x0;
        ulong startAddr = 0x0;
        
        Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(options =>
        {
            binaryFilepath = options.Binary;
            baseAddr = options.Base;
            startAddr = options.Start;
        });
        
        var bytes = File.ReadAllBytes(binaryFilepath);
        IBinary binary = new WindowsBinary(64, bytes, baseAddr);
        var dna = new Dna.Dna(binary);
        var cfg = dna.RecursiveDescent.ReconstructCfg(startAddr);
        Console.WriteLine(GraphFormatter.FormatGraph(cfg));
        Console.ReadLine();
    }
}
