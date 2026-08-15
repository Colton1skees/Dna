using Dna.Binary.Windows;
using Dna.BinaryTranslator.VMProtect.Rewrite;
using Dna.LLVMInterop.API.Remill.Arch;
using LLVMSharp.Interop;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Shell;

namespace Dna.Example
{
    public record VmpFunc(string Name, ulong Addr);

    public record VmpBinary(string Path, List<VmpFunc> Funcs);

    public record VmpTest(string Path, List<VmpFunc> Funcs);

    public static class VmpTests
    {
        public static void Run()
        {
            var test = GetVmp35PrivateTlb();
            var vmpBin = WindowsBinary.From(test.Path);
            var vmpDna = new Dna(vmpBin);


            string name = "";
            name = "des_encrypt_main"; // works
            name = "aes_target_function"; // works at least for the first massive stub..
            name = "rc4_target_function"; // breaking somewhere
            name = "complex_add_two_vars_64";
            var vmpAddr = test.Funcs.Single(x => x.Name == name).Addr;

            var vmpCtx = LLVMContextRef.Global;
            var vmpArch = new RemillArch(vmpCtx, RemillOsId.kOSLinux, RemillArchId.kArchAMD64_AVX512);
            //var translator = new IterativeVmpTranslator(vmpDna, vmpArch, vmpCtx, vmpAddr);
            var translator = new VmpFunctionExplorer(vmpDna, vmpArch, vmpCtx);
            var sw = Stopwatch.StartNew();
            var devirtedFunc = translator.Run(vmpAddr);
            sw.Stop();

            Debugger.Break();
        }

        public static VmpTest GetVmptest()
            => new(
                @"C:\Users\user\Downloads\DNA ASSETS\vmptest.vmp.bin",
                new List<VmpFunc>
                {
                    new("vm_entry", 0x140001030),
                });

        public static VmpTest GetVmp3PrivateTlb()
            => new(
                @"C:\Users\user\Downloads\PRIV_BINARIES\vmp3_private_tlb.vmp.exe",
                new List<VmpFunc>
                {
                    new("add_two_vars", 0x1400032FA),
                    new("conditional_additions", 0x14000335E),
                    new("simple_loop_64", 0x140003710),
                    new("complex_loop_32", 0x1400037AF),
                    new("complex_loop_64", 0x140003862),
                    new("unknown_140003BC2", 0x140003BC2),
                    new("nested_simple_loop_32", 0x140003971),
                    new("vm_stub", 0x140130E2C),
                });

        public static VmpTest GetBattleEyeClient()
            => new(
                @"C:\Users\user\Downloads\BEClient2_020924_2.dll",
                new List<VmpFunc>
                {
                    new("vm_entry", 0x180002120),
                });

        public static VmpTest GetVmp35PrivateTlb()
            => new(
                @"C:\Users\user\Downloads\PRIV_BINARIES\vmp35_private_tlb\virt.exe",
                new List<VmpFunc>
                {
                    new("add_two_vars32", 0x140003990),
                    new("add_two_vars64", 0x1400039C0),
                    new("aes_encrypt_main", 0x140001980),
                    new("aes_target_function", 0x14000102B),
                    new("complex_add_two_vars_32", 0x140003AC0),
                    new("complex_add_two_vars_64", 0x140003AF0),
                    new("complex_loop_32", 0x140003DF0),
                    new("complex_loop_64", 0x140003F20),
                    new("cond_add_two_vars32", 0x1400039F0),
                    new("cond_add_two_vars64", 0x140003A50),
                    new("des_encrypt_main", 0x14000268C),
                    new("des_encrypt_target_function", 0x140001D30),
                    new("des_key_schedule_main", 0x140002B80),
                    new("des_key_schedule_target_function", 0x1400027A0),
                    new("md5_main", 0x1400030B0),
                    new("md5_target_function", 0x140002CB0),
                    new("nested_simple_loop_32", 0x140003FE0),
                    new("nested_simple_loop_64", 0x140004350),
                    new("rc4_main", 0x1400034E0),
                    new("rc4_target_function", 0x140003226),
                    new("sha1_main", 0x140003850),
                    new("sha1_target_function", 0x140003620),
                    new("simple_loop_32", 0x140003B20),
                    new("simple_loop_64", 0x140003C80),
                });
    }
}
