using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class ProjectChecks
    {
        public static void Run(string sourceRoot)
        {
            string temp=Path.Combine(Path.GetTempPath(),"ProjectChecks-"+Guid.NewGuid());Directory.CreateDirectory(temp);
            try
            {
                foreach(string source in Directory.EnumerateFiles(Path.Combine(sourceRoot,"sp-12","datar"),"*.inm"))
                {
                    byte[] before=File.ReadAllBytes(source);var model=ProjectDocument.Load(source);string target=Path.Combine(temp,Path.GetFileName(source));model.Save(target);Check(before.SequenceEqual(File.ReadAllBytes(target)),"Project byte-exact round trip "+Path.GetFileName(source));model.Remark="C# check";model.OperatingPoints[0][0]=.125;model.Options[3]=1-model.Options[3];model.Save(target);var copy=ProjectDocument.Load(target);Check(copy.Remark=="C# check"&&copy.OperatingPoints[0][0]==.125&&copy.Options[3]==model.Options[3]&&copy.ResultIds.SequenceEqual(model.ResultIds)&&copy.TrailingLines.SequenceEqual(model.TrailingLines),"Project full editable sections "+Path.GetFileName(source));Check(before.SequenceEqual(File.ReadAllBytes(source)),"Project original unchanged "+Path.GetFileName(source));
                }
                var catalog=new Unit3Model(sourceRoot,"Ru","sp-12");var created=ProjectDocument.Create(catalog);created.Options[0]=7;created.ResultIds.Clear();created.ResultIds.Add(1);string file=Path.Combine(temp,"new.inm");created.Save(file);Check(ProjectDocument.Load(file).ControlIds.Length==0,"Project task7 with empty optional blocks");created.NodeIds=new[]{11,12};created.NodeValues=new[]{.125,.25};created.CorrectionIds=new[]{42};created.CorrectionValues=new[]{new[]{.5},new[]{.625},new[]{.75}};created.ControlIds=new[]{425};created.ControlValues=new[]{2000d};created.DynamicFactors=new[]{1,0,1};created.Save(file);var reread=ProjectDocument.Load(file);Check(reread.NodeValues.SequenceEqual(created.NodeValues)&&reread.CorrectionValues[2][0]==.75&&reread.ControlValues[0]==2000&&reread.DynamicFactors.SequenceEqual(new[]{1,0,1}),"Project node multi-parameter control and dynamic sections");Check(File.Exists(file+".bak"),"Project atomic save backup");
                var context=new ApplicationSettings(sourceRoot,Path.Combine(temp,"prefs.ini")){NodeCorrections=new System.Collections.Generic.Dictionary<int,double>{{11,.1},{14,.2},{17,.3}},ActiveParameterIds=new[]{421,425}};Check(context.CorrectionPreset(3)[0]==.1&&context.CorrectionPreset(3)[5]==.3&&context.XCorrectionPreset(3)==.2&&context.IsParameterActive(421)&&!context.IsParameterActive(1),"Project editor correction mapping and active parameters");
                string bin=Path.Combine(temp,"fixture");Directory.CreateDirectory(bin);string exe=typeof(ProjectChecks).Assembly.Location;int exit=CalculationRunner.RunAsync(exe,bin,CancellationToken.None,"--solver-fixture").GetAwaiter().GetResult();Check(exit==0&&File.Exists(Path.Combine(bin,"param.dat")),"Calculation process produces results in supplied directory");var results=CalculationResults.LoadParameters(Path.Combine(bin,"param.dat"),new[]{421,1});Check(results.Rows.Count==2&&results.Rows[1][1]==20,"Calculation param.dat record assembly");
                var options=new ResultExportOption{Digits=6,FormatIndex=1,IncludeInput=true,IncludeGraphs=true};string html=results.Export(ResultFileFormat.Html,options,id=>"<P"+id+">","<input>");Check(html.Contains("&lt;P421&gt;")&&html.Contains("&lt;input&gt;")&&html.Contains("<svg"),"Export HTML escapes labels and includes graph");options.Transpose=true;Check(results.Export(ResultFileFormat.Text,options,id=>id.ToString()).Contains("421\t0.000000\t1.000000"),"Export transposed numeric text");string native=Path.Combine(temp,"results.res");results.Save(native,ResultFileFormat.Native,options,id=>id.ToString());Check(CalculationResults.LoadNative(native).Rows[1][1]==20,"Export native RES round trip");
                using(var cancel=new CancellationTokenSource(300)){try{CalculationRunner.RunAsync(exe,bin,cancel.Token,"--solver-wait").GetAwaiter().GetResult();throw new Exception("Cancellation failed");}catch(OperationCanceledException){Check(true,"Calculation process cancellation");}}
                string sourceEngine=Path.Combine(sourceRoot,"sp-12"),run=Path.Combine(temp,"run");var project=ProjectDocument.Load(Path.Combine(sourceEngine,"datar","dross-sau.inm"));string prepared=CalculationRunner.Prepare(project,sourceEngine,run);Check(File.Exists(Path.Combine(prepared,"data.dat"))&&File.Exists(Path.Combine(run,"data","ReLPT.dat"))&&!File.Exists(Path.Combine(prepared,"param.dat")),"Calculation isolated input staging clears stale output");Check(ProjectDocument.Load(Path.Combine(prepared,"data.dat")).Options.SequenceEqual(project.Options),"Calculation staged data.dat preserves options");
            }
            finally{Directory.Delete(temp,true);}
        }
        private static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);}
    }
}
