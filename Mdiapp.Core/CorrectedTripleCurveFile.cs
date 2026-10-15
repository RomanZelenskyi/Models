using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Mdiapp.Core
{
    public sealed class CorrectedTripleCurveFile
    {
        public InputTripleCurveFile Data { get; private set; }
        public double[][] References { get; } = {new double[]{1,1},new double[]{1,1},new double[]{1,1,1,1}};
        public double[,] Coefficients { get; } = new double[3,3];
        private static readonly int[] BlockForCurve={0,2,1};
        public static CorrectedTripleCurveFile Load(string path,CultureInfo culture=null)
        {
            culture=culture??CultureInfo.CurrentCulture;var model=new CorrectedTripleCurveFile{Data=InputTripleCurveFile.Load(path,culture)};if(model.Data.Type!=21&&model.Data.Type!=22)throw new InvalidDataException("Unit33 поддерживает типы 21 и 22.");
            for(int curve=0;curve<3;curve++)
            {
                var fields=model.Data.Curves[BlockForCurve[curve]].CountSuffix.Split(new[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries);if(fields.Length<model.References[curve].Length)throw new InvalidDataException("Недостаточно опорных значений Unit33.");for(int i=0;i<model.References[curve].Length;i++)model.References[curve][i]=InputFiles3132.Parse(fields[i],culture);
            }
            return model;
        }
        public double Correct(int curve,double x,double y)
        {
            var refs=curve<2?new[]{References[curve][0],References[curve][1],0,1}:References[curve];return CorrectedCurveFile.Evaluate(x,y,refs,Coefficients[curve,0],curve==2?Coefficients[curve,1]:0,Coefficients[curve,2]);
        }
        public void Save(string path,bool applyCorrection,CultureInfo culture=null,string caption="Коррекция применена")
        {
            culture=culture??CultureInfo.CurrentCulture;if(References.SelectMany(r=>r).Concat(Coefficients.Cast<double>()).Any(v=>double.IsNaN(v)||double.IsInfinity(v)))throw new InvalidDataException("Коэффициенты должны быть конечными числами.");
            var output=new InputTripleCurveFile{Type=Data.Type,Comment=Data.Comment};if(applyCorrection&&!output.Comment.EndsWith(" ("+caption+")",StringComparison.Ordinal))output.Comment+=" ("+caption+")";
            for(int curve=0;curve<3;curve++)
            {
                var source=Data.Curves[curve];var target=output.Curves[curve];target.Count=source.Count;target.Parameter=source.Parameter;target.CountCaption=source.CountCaption;target.ParameterCaption=source.ParameterCaption;target.XCaption=source.XCaption;target.YCaption=source.YCaption;
                int referenceCurve=Array.IndexOf(BlockForCurve,curve);var values=applyCorrection?(referenceCurve==2?new double[]{0,1,0,1}:new double[]{0,1}):References[referenceCurve];
                target.CountSuffix=string.Concat(values.Select(v=>"  "+v.ToString("F6",culture)));
                var extras=source.CountSuffix.Split(new[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries).Skip(References[referenceCurve].Length);if(extras.Any())target.CountSuffix+="  "+string.Join("  ",extras);
                for(int point=0;point<source.Count;point++){target.Values[0,point]=source.Values[0,point];target.Values[1,point]=applyCorrection?Correct(curve,source.Values[0,point],source.Values[1,point]):source.Values[1,point];}
            }
            output.Save(path,culture);
        }
    }
}
