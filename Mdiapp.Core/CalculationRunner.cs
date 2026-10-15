using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mdiapp.Core
{
    public sealed class CalculationRunner
    {
        public static void CopyDirectory(string source,string target)
        {
            if(!Directory.Exists(source))throw new DirectoryNotFoundException(source);Directory.CreateDirectory(target);foreach(string file in Directory.EnumerateFiles(source))File.Copy(file,Path.Combine(target,Path.GetFileName(file)),true);foreach(string directory in Directory.EnumerateDirectories(source))if((File.GetAttributes(directory)&FileAttributes.ReparsePoint)==0)CopyDirectory(directory,Path.Combine(target,Path.GetFileName(directory)));
        }
        public static async Task<int> RunAsync(string executable,string workingDirectory,CancellationToken cancellation,string arguments="")
        {
            cancellation.ThrowIfCancellationRequested();
            if(!File.Exists(executable))throw new FileNotFoundException("Расчётный EXE не найден.",executable);var process=new Process{StartInfo=new ProcessStartInfo(executable,arguments){WorkingDirectory=workingDirectory,UseShellExecute=false,CreateNoWindow=true}};
            using(process){if(!process.Start())throw new InvalidOperationException("Не удалось запустить расчёт.");while(!process.HasExited){if(cancellation.IsCancellationRequested){process.Kill();await Task.Run(()=>process.WaitForExit()).ConfigureAwait(false);cancellation.ThrowIfCancellationRequested();}await Task.Delay(100).ConfigureAwait(false);}cancellation.ThrowIfCancellationRequested();return process.ExitCode;}
        }
        public static string Prepare(ProjectDocument project,string engineDirectory,string calculationDirectory)
        {
            string bin=Path.Combine(calculationDirectory,"bin");CopyDirectory(Path.Combine(engineDirectory,"bin"),bin);Directory.CreateDirectory(Path.Combine(calculationDirectory,"data"));
            foreach(string file in Directory.EnumerateFiles(engineDirectory))File.Copy(file,Path.Combine(calculationDirectory,Path.GetFileName(file)),true);
            foreach(string language in new[]{"Ru","En"}){string names=Path.Combine(engineDirectory,language,"Namepar1.txt");if(File.Exists(names)){File.Copy(names,Path.Combine(calculationDirectory,"Namepar1.txt"),true);break;}}
            foreach(string name in project.IncludedFiles){string source=Path.GetFullPath(Path.Combine(engineDirectory,"datar",name));string destination=Path.GetFullPath(Path.Combine(bin,name));string root=Path.GetFullPath(calculationDirectory)+Path.DirectorySeparatorChar;if(!destination.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Подключённый файл выходит за папку расчёта.");Directory.CreateDirectory(Path.GetDirectoryName(destination));File.Copy(source,destination,true);}
            File.WriteAllText(Path.Combine(bin,"data.dat"),project.Serialize(false),LegacyCodec.TextEncoding);foreach(string file in new[]{"rez.dat","param.dat"}){string path=Path.Combine(bin,file);if(File.Exists(path))File.Delete(path);}return bin;
        }
        public static CalculationResults Read(ProjectDocument project,string bin,IniFile definitions)
        {
            if(definitions.GetInt("rez","rez",0)==1){string path=Path.Combine(bin,"rez.dat");if(!File.Exists(path)||ProjectDocument.Integers(File.ReadAllLines(path,LegacyCodec.TextEncoding).FirstOrDefault()??"0").FirstOrDefault()!=1)throw new InvalidDataException("Расчёт не сообщил об успешном завершении в rez.dat.");}
            return CalculationResults.LoadParameters(Path.Combine(bin,"param.dat"),new[]{project.ArgumentIds[0]}.Concat(project.ResultIds).ToArray());
        }
    }
}
