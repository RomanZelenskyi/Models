using System;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit6Checks
    {
        public static void Run()
        {
            Check(LegacyEngineRegistry.ParseProjectList("  first   last").SequenceEqual(new[] {"first", "last"}), "Unit6 last project without trailing space");
            Check(LegacyEngineRegistry.ParseProjectList("a\tb\r\na").SequenceEqual(new[] {"a", "b"}), "Unit6 whitespace and duplicate handling");
            Check(LegacyEngineRegistry.ParseProjectList(null).Count == 0, "Unit6 empty project list");
        }
        private static void Check(bool condition, string name) { if (!condition) throw new Exception(name); Console.WriteLine("PASS " + name); }
    }
}
