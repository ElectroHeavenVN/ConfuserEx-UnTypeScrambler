using ConfuserExUnTypeScrambler;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

internal class Program
{
    public static int unscrambledTimes;

    public static ModuleDef module;

    [DllImport("msvcrt.dll")]
    static extern bool system(string msg);
    private static void pause()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        system("pause");
    }

    /// <summary>
    /// The entry point of the application.
    /// </summary>
    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        PrintLogo();
        Console.WriteLine("                                     v" + Assembly.GetExecutingAssembly().GetName().Version);
        Console.WriteLine("                             Created by ElectroHeavenVN");
        Console.ResetColor();
        if (args.Length == 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Argument empty!");
            pause();
            return;
        }
        foreach (string arg in args)
        {
            Console.WriteLine("Untypescrambling assembly: " + Path.GetFileName(arg) + "...");
            Console.WriteLine("Resolving dependencies...");
            module = AssemblyDef.Load(arg).ManifestModule;
            ModuleContext moduleContext = ModuleDef.CreateModuleContext();
            AssemblyResolver assemblyResolver = (AssemblyResolver)moduleContext.AssemblyResolver;
            assemblyResolver.EnableTypeDefCache = true;
            module.Context = moduleContext;
            ((AssemblyResolver)module.Context.AssemblyResolver).AddToCache(module);
            foreach (AssemblyRef assemblyRef in module.GetAssemblyRefs())
            {
                assemblyResolver.ResolveThrow(assemblyRef, module);
            }
            Console.WriteLine("Scanning scrambled methods...");
            UnTypeScrambler.GetScrambledMethods(module.Types);
            Console.WriteLine("Found " + UnTypeScrambler.scrambledMethods.Count + " scrambled methods!");
            Console.WriteLine("Untypescrambling...");
            do
            {
                unscrambledTimes = 0;
                UnTypeScrambler.UnTypeScramble(module.Types);
                //UnTypeScrambler.excludedMethods.Clear();
                //UnTypeScrambler.scrambledMethods.Clear();
                UnTypeScrambler.GetScrambledMethods(module.Types);
            }
            while (unscrambledTimes > 0);
            if (UnTypeScrambler.excludedMethods.Count > 0) Console.WriteLine("Excluded methods: ");
            foreach (MethodDef method in UnTypeScrambler.excludedMethods)
            {
                Console.WriteLine(method.FullName + " [" + method.MDToken + "]");
            }
            Console.WriteLine("Untypescrambled successfully!");
            Console.WriteLine("Removing generic parameters...");
            UnTypeScrambler.RemoveGenericParameters(module.Types);
            Console.WriteLine("Fixing calls to typescrambled methods...");
            UnTypeScrambler.FixTypeScrambleCalls(module.Types);
            Console.WriteLine("Fixing Activator.CreateInstance...");
            UnTypeScrambler.FixActivatorCreateInstance(module.Types);
            SaveModule(arg);
            UnTypeScrambler.excludedMethods.Clear();
            UnTypeScrambler.scrambledMethods.Clear();
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

    private static void SaveModule(string arg)
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

