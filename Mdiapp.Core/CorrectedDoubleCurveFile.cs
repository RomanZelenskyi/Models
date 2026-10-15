using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Mdiapp.Core
{
    public sealed class CorrectedDoubleCurveFile
    {
        public InputDoubleCurveFile Data { get; private set; }
        public double[] References { get; } = {1,1,1,1};
        public double[,] Coefficients { get; } = new double[2,3];
        public static CorrectedDoubleCurveFile Load(string path,CultureInfo culture=null)
        {
            culture=culture??CultureInfo.CurrentCulture;var model=new CorrectedDoubleCurveFile{Data=InputDoubleCurveFile.Load(path,culture)};
            var fields=model.Data.Curves[0].CountSuffix.Split(new[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries);if(fields.Length!=4)throw new InvalidDataException("Для Unit28 нужны четыре опорных значения первой характеристики.");
            for(int i=0;i<4;i++)model.References[i]=InputFiles3132.Parse(fields[i],culture);return model;
        }
        public double Correct(int curve,double x,double y)=>CorrectedCurveFile.Evaluate(x,y,References,Coefficients[curve,0],Coefficients[curve,1],Coefficients[curve,2]);
        public void Save(string path,bool applyCorrection,CultureInfo culture=null,string caption="Коррекция применена")
        {
            culture=culture??CultureInfo.CurrentCulture;if(References.Concat(Coefficients.Cast<double>()).Any(v=>double.IsNaN(v)||double.IsInfinity(v)))throw new InvalidDataException("Коэффициенты должны быть конечными числами.");
            var output=new InputDoubleCurveFile{Comment=Data.Comment,ConstantsCaption=Data.ConstantsCaption};Array.Copy(Data.Constants,output.Constants,3);
            if(applyCorrection&&!output.Comment.EndsWith(" ("+caption+")",StringComparison.Ordinal))output.Comment+=" ("+caption+")";
            for(int curve=0;curve<2;curve++)
            {
                var source=Data.Curves[curve];var target=output.Curves[curve];target.Count=source.Count;target.Parameter=source.Parameter;target.CountCaption=source.CountCaption;target.ParameterCaption=source.ParameterCaption;target.XCaption=source.XCaption;target.YCaption=source.YCaption;target.CountSuffix=source.CountSuffix;
                if(curve==0)target.CountSuffix=applyCorrection?"  0  1  0  1":string.Concat(References.Select(v=>"  "+v.ToString("F6",culture)));
                for(int point=0;point<source.Count;point++){target.Values[0,point]=source.Values[0,point];target.Values[1,point]=applyCorrection?Correct(curve,source.Values[0,point],source.Values[1,point]):source.Values[1,point];}
            }
            output.Save(path,culture);
        }
    }
}
