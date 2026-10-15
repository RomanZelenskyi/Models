using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Mdiapp.Core
{
    public sealed class CorrectedCharacteristicFile
    {
        public CharacteristicFile Data { get; private set; }
        public double[] References { get; private set; } = {1,1,1};
        public bool IsTurbine { get; private set; }
        public double[,] Coefficients { get; } = new double[2,3];
        public double XCorrection { get; set; }
        public static CorrectedCharacteristicFile Load(string path,CultureInfo culture=null,bool turbine=false)
        {
            culture=culture??CultureInfo.CurrentCulture;var model=new CorrectedCharacteristicFile{Data=CharacteristicFile.Load(path,culture,true),IsTurbine=turbine};
            if(turbine?!new[]{15,17,19}.Contains(model.Data.Type):!new[]{3,4,1210,1220,1230,1240,1250}.Contains(model.Data.Type))throw new InvalidDataException(turbine?"Unit31 поддерживает типы 15, 17 и 19.":"Unit30 поддерживает типы 3, 4 и 1210–1250.");
            if(turbine)model.References=new double[]{1,1,1,1};
            var values=(turbine?model.Data.SpecialSuffix:model.Data.ControlSuffix).Split(new[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries);for(int i=0;i<Math.Min(model.References.Length,values.Length);i++)model.References[i]=InputFiles3132.Parse(values[i],culture);return model;
        }
        public double[,] Correct(CharacteristicBranch branch)
        {
            int count=branch.Points.GetLength(1);var result=(double[,])branch.Points.Clone();int row=Math.Abs(Data.Interpolation)==5?1:0;
            if(IsTurbine)
            {
                for(int i=0;i<count;i++)for(int curve=0;curve<2;curve++)
                {
                    double a=Coefficients[curve,0]==-1000?0:Coefficients[curve,0],b=Coefficients[curve,1]==-1000?0:Coefficients[curve,1];int target=curve==0?0:2;
                    result[target,i]=branch.Points[target,i]*(1+a*(branch.Points[1,i]-References[0])/(References[1]==0?1:References[1])+b*(branch.Value-References[2])/(References[3]==0?1:References[3])+Coefficients[curve,2]);
                }
                if(result.Cast<double>().Any(v=>double.IsNaN(v)||double.IsInfinity(v)))throw new InvalidDataException("Коррекция дала неконечные значения.");return result;
            }
            double min=Enumerable.Range(0,count).Min(i=>branch.Points[row,i]),max=Enumerable.Range(0,count).Max(i=>branch.Points[row,i]);
            for(int i=0;i<count;i++)
            {
                double normalized=max==min?0:(branch.Points[row,i]-min)/(max-min);result[0,i]=branch.Points[0,i]*(1+XCorrection);
                for(int curve=0;curve<2;curve++)
                {
                    double a=Coefficients[curve,0]==-1000?0:Coefficients[curve,0],b=Coefficients[curve,1]==-1000?0:Coefficients[curve,1];
                    result[curve+1,i]=branch.Points[curve+1,i]*(1+a*(branch.Value-References[0])/(References[1]==0?1:References[1])+b*(normalized-References[2])+Coefficients[curve,2]);
                }
            }
            if(result.Cast<double>().Any(v=>double.IsNaN(v)||double.IsInfinity(v)))throw new InvalidDataException("Коррекция дала неконечные значения.");return result;
        }
        public void Save(string path,bool applyCorrection,CultureInfo culture=null,string caption="Коррекция применена")
        {
            culture=culture??CultureInfo.CurrentCulture;if(References.Concat(Coefficients.Cast<double>()).Concat(new[]{XCorrection}).Any(v=>double.IsNaN(v)||double.IsInfinity(v)))throw new InvalidDataException("Коэффициенты должны быть конечными числами.");
            var output=new CharacteristicFile{Type=Data.Type,Comment=Data.Comment,Interpolation=Data.Interpolation,AdditionalValue=Data.AdditionalValue,ControlSuffix=Data.ControlSuffix,SpecialSuffix=Data.SpecialSuffix};Array.Copy(Data.Coefficients,output.Coefficients,4);
            if(applyCorrection){if(!output.Comment.EndsWith(" ("+caption+")",StringComparison.Ordinal))output.Comment+=" ("+caption+")";if(IsTurbine)output.SpecialSuffix="  0  1  0  1";else{for(int i=0;i<4;i++)output.Coefficients[i]=1;output.ControlSuffix="  0  1  0";}}
            else if(IsTurbine)output.SpecialSuffix=string.Concat(References.Select(v=>"  "+v.ToString("F6",culture)));
            else output.ControlSuffix=string.Concat(References.Select(v=>"  "+v.ToString("F6",culture)));
            foreach(var branch in Data.Branches)output.Branches.Add(new CharacteristicBranch{Value=branch.Value,Points=applyCorrection?Correct(branch):(double[,])branch.Points.Clone()});
            output.Save(path,culture,true);
        }
    }
}
