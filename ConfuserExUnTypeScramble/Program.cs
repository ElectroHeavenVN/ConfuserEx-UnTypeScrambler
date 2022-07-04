using dnlib.DotNet.Writer;
using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace ConfuserExUnTypeScramble
{
    internal class Program
    {
        public static int unscrambledTimes;

        public static ModuleDef module;

        [DllImport("msvcrt.dll")]
        public static extern bool system(string msg);
        public static void pause()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            system("pause");
        }
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Argument empty!");
                pause();
                return;
            }
            foreach (string arg in args)
            {
                PrintLogo();
                Console.WriteLine("                                     v" + Assembly.GetExecutingAssembly().GetName().Version);
                Console.WriteLine("                             Created by ElectroHeavenVN");
                Console.ResetColor();
                Console.WriteLine("Untypescrambling assembly: " + Path.GetFileName(arg) + "...");
                module = AssemblyDef.Load(arg).ManifestModule;
                UnTypeScrambler.GetScrambledMethods(module.Types);
                do
                {
                    unscrambledTimes = 0;
                    UnTypeScrambler.UnTypeScramble(module.Types);
                    //UnTypeScrambler.excludedMethods.Clear();
                    //UnTypeScrambler.scrambledMethods.Clear();
                    UnTypeScrambler.GetScrambledMethods(module.Types);
                }
                while (unscrambledTimes > 0);
                Console.WriteLine("Untypescrambled successfully!");
                Console.WriteLine("Excluded methods: ");
                foreach (MethodDef method in UnTypeScrambler.excludedMethods)
                {
                    Console.WriteLine(method.FullName + " [" + method.MDToken + "]");
                }
                Console.WriteLine("Removing generic parameters...");
                UnTypeScrambler.RemoveGenericParameters(module.Types);
                Console.WriteLine("Fixing calls to typescrambled methods...");
                UnTypeScrambler.FixTypeScrambleCall(module.Types);
                Console.WriteLine("Fixing Activator.CreateInstance...");
                UnTypeScrambler.FixActivatorCreateInstance(module.Types);
                SaveModule(arg);
            }
            pause();
        }

        private static void PrintLogo()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("              ______            ____                     ______                     ");
            Console.WriteLine("             / ____/___  ____  / __/_  __________  _____/ ____/  __                 ");
            Console.WriteLine("            / /   / __ \\/ __ \\/ /_/ / / / ___/ _ \\/ ___/ __/ | |/_/                 ");
            Console.WriteLine("           / /___/ /_/ / / / / __/ /_/ (__  )  __/ /  / /____>  <                   ");
            Console.WriteLine("   __  __  \\____/\\____/_/_/_/_/  \\__,_/____/\\___/_/  /_____/_/|_|  __    __       ");
            Console.WriteLine("  / / / /___/_  __/_  ______  ___ / ___/______________ _____ ___  / /_  / /__  _____");
            Console.WriteLine(" / / / / __ \\/ / / / / / __ \\/ _ \\\\__ \\/ ___/ ___/ __ `/ __ `__ \\/ __ \\/ / _ \\/ ___/");
            Console.WriteLine("/ /_/ / / / / / / /_/ / /_/ /  __/__/ / /__/ /  / /_/ / / / / / / /_/ / /  __/ /    ");
            Console.WriteLine("\\____/_/ /_/_/  \\__, / .___/\\___/____/\\___/_/   \\__,_/_/ /_/ /_/_.___/_/\\___/_/     ");
            Console.WriteLine("               /____/_/                                                             ");
            Console.ForegroundColor = ConsoleColor.Yellow;
        }

        public static void SaveModule(string arg)
        {
            ModuleWriterOptions moduleWriterOptions = new ModuleWriterOptions(module);
            string path = Path.GetFileNameWithoutExtension(arg) + "-unTypeScrambled" + Path.GetExtension(arg);
            try
            {
                module.Write(path, moduleWriterOptions);
            }
            catch (ModuleWriterException ex)
            {
                Console.WriteLine("Handled exception:" + ex);
                moduleWriterOptions.MetadataOptions.Flags |= MetadataFlags.PreserveAll;
                moduleWriterOptions.MetadataLogger = DummyLogger.NoThrowInstance;
                module.Write(path, moduleWriterOptions);
            }
            Console.WriteLine("Output file: " + path);
        }
    }
}
