using HarmonyLib;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RuntimePatches
{
    public static class LazyLLVMFixes
    {
        private static bool modulePatchInstalled;

        private static bool valuePatchInstalled;

        private static Func<LLVMModuleRef, string> moduleToStr;

        private static Func<LLVMValueRef, string> valueToStr;

        // Fixes a bug where the LLVM-C API's print function crashes.
        public static void InstallModuleToStringBugFix(Func<LLVMModuleRef, string> llvmModuleToString)
        {
            if (modulePatchInstalled)
                return;

            modulePatchInstalled = true;
            moduleToStr = llvmModuleToString;

            var mOriginal = AccessTools.Method(typeof(LLVMModuleRef), "PrintToString");
            var mPrefix = AccessTools.Method(typeof(LazyLLVMFixes), "HookedPrintModuleToString");

            Harmony.DEBUG = true;
            var harmony = new Harmony("Dna.LazyLLVMFixes");

            var result = harmony.Patch(mOriginal, new HarmonyMethod(mPrefix));
            Console.WriteLine("");
        }

        // Fixes a bug where the LLVM-C API's print function crashes.
        public static void InstallModuleToFileBugFix()
        {
            var mOriginal = typeof(LLVMModuleRef).GetMethods().Single(x => x.Name == "PrintToFile" && x.GetParameters().Single().ParameterType.ToString().ToLower().Contains("string"));
            var mPrefix = AccessTools.Method(typeof(LazyLLVMFixes), "HookedPrintModuleToFile");

            Harmony.DEBUG = true;
            var harmony = new Harmony("Dna.LazyLLVMFixes");

            var result = harmony.Patch(mOriginal, new HarmonyMethod(mPrefix));
            Console.WriteLine("");
        }

        // Fixes a bug where the LLVM-C API's print function crashes.
        public static void InstallValueToStringBugFix(Func<LLVMValueRef, string> llvmValueToString)
        {
            if (valuePatchInstalled)
                return;

            valuePatchInstalled = true;
            valueToStr = llvmValueToString;

            var mOriginal = AccessTools.Method(typeof(LLVMValueRef), "PrintToString");
            var mPrefix = AccessTools.Method(typeof(LazyLLVMFixes), "HookedPrintValueToString");

            Harmony.DEBUG = true;
            var harmony = new Harmony("Dna.LazyLLVMFixes");

            var result = harmony.Patch(mOriginal, new HarmonyMethod(mPrefix));
            Console.WriteLine("");
        }

        private static bool HookedPrintModuleToString(ref string __result, LLVMModuleRef __instance)
        {
            __result = moduleToStr(__instance);
            return false;
        }

        private static bool HookedPrintModuleToFile(LLVMModuleRef __instance, string Filename)
        {
            File.WriteAllText(Filename, moduleToStr(__instance));
            return false;
        }

        private static bool HookedPrintValueToString(ref string __result, LLVMValueRef __instance)
        {
            __result = valueToStr(__instance);
            return false;
        }
    }
}
