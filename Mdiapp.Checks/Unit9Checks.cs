using System;
using System.IO;
using System.Linq;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class Unit9Checks
    {
        public static void Run()
        {
            Check(ResultFileSelection.Suggest(@"C:\Models\sp-12\datar\test.inm", @"C:\Models", "sp-12", ResultFileFormat.Text) == @"C:\Models\sp-12\Result\test.txt", "Unit9 Delphi default result path");
            Check(ResultFileSelection.Suggest(@"C:\Models\sp-12\DATAR\nested\test.INM", @"C:\Models", "sp-12", ResultFileFormat.Html) == @"C:\Models\sp-12\Result\nested\test.htm", "Unit9 case insensitive nested datar path");
            Check(ResultFileSelection.Suggest("NONAME1", @"C:\Models", "sp-12", ResultFileFormat.Native) == @"C:\Models\sp-12\Result\NONAME.res", "Unit9 unnamed project default");
            Check(ResultFileSelection.Suggest(@"C:\datar_backup\project.inm", @"C:\Models", "sp-12", ResultFileFormat.Text) == @"C:\datar_backup\Result\project.txt", "Unit9 datar substring does not alter unrelated directories");
        }
        private static void Check(bool condition, string label) { if (!condition) throw new Exception(label); Console.WriteLine("PASS " + label); }
    }
}
