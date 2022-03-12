using Dna.Binary;
using Dna.Binary.Windows;
using Dna.ControlFlow;

var bytes = File.ReadAllBytes(@"C:\Users\colton\Downloads\driver.sys");
IBinary binary = new WindowsBinary(64, bytes, 0x140000000);
var dna = new Dna.Dna(binary);
var cfg = dna.RecursiveDescent.ReconstructCfg(0x14034E74A);
Console.WriteLine(GraphFormatter.FormatGraph(cfg));
Console.ReadLine();
