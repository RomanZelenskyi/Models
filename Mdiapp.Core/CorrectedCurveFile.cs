using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Mdiapp.Core
{
    // Unit27: y * (1 + a*(x-r1)/r2 + b*(x-r3)/r4 + c).
    public sealed class CorrectedCurveFile
    {
        public InputCurveFile Curve { get; private set; } = new InputCurveFile { Type=1 };
        public double[] References { get; } = {1,1,1,1};
        public double A { get; set; }
        public double B { get; set; }
        public double C { get; set; }
        public bool IsSimple { get; private set; }
        public static CorrectedCurveFile Load(string path,CultureInfo culture=null,bool simple=false)
        {
            culture=culture??CultureInfo.CurrentCulture;var curve=InputCurveFile.Load(path,culture);if(simple?!new[]{11,16,20}.Contains(curve.Type):curve.Type!=1)throw new InvalidDataException(simple?"Unit29 поддерживает типы 11, 16 и 20.":"Unit27 поддерживает файл типа 1.");
            var model=new CorrectedCurveFile{Curve=curve,IsSimple=simple};var values=curve.CountSuffix.Split(new[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries);
            if(values.Length!=(simple?2:4))throw new InvalidDataException("Недостаточно опорных значений во второй строке.");
            for(int i=0;i<values.Length;i++)model.References[i]=InputFiles3132.Parse(values[i],culture);if(simple){model.References[2]=0;model.References[3]=1;}return model;
        }
        public double Correct(double x,double y)
        {
            return Evaluate(x,y,References,A,IsSimple?0:B,C);
        }
        public static double Evaluate(double x,double y,double[] references,double a,double b,double c)
        {
            if(references==null||references.Length!=4)throw new InvalidDataException("Нужны четыре опорных значения.");
            a=a==-1000?0:a;b=b==-1000?0:b;
            double result=y*(1+a*(x-references[0])/(references[1]==0?1:references[1])+b*(x-references[2])/(references[3]==0?1:references[3])+c);
            if(double.IsNaN(result)||double.IsInfinity(result))throw new InvalidDataException("Коррекция дала неконечное значение.");return result;
        }
        public void Save(string path,bool applyCorrection,CultureInfo culture=null,string correctionCaption="Коррекция применена")
        {
            culture=culture??CultureInfo.CurrentCulture;if(References.Concat(new[]{A,B,C}).Any(v=>double.IsNaN(v)||double.IsInfinity(v)))throw new InvalidDataException("Коэффициенты должны быть конечными числами.");
            var output=new InputCurveFile{Type=Curve.Type,Comment=Curve.Comment,Count=Curve.Count,Parameter=Curve.Parameter,CountCaption=Curve.CountCaption,ParameterCaption=Curve.ParameterCaption,XCaption=Curve.XCaption,YCaption=Curve.YCaption};
            if(applyCorrection){if(!output.Comment.EndsWith(" ("+correctionCaption+")",StringComparison.Ordinal))output.Comment+=" ("+correctionCaption+")";output.CountSuffix=IsSimple?"  0  1":"  0  1  0  1";}
            else output.CountSuffix=string.Concat(References.Take(IsSimple?2:4).Select(v=>"  "+v.ToString("F6",culture)));
            for(int i=0;i<Curve.Count;i++){output.Values[0,i]=Curve.Values[0,i];output.Values[1,i]=applyCorrection?Correct(Curve.Values[0,i],Curve.Values[1,i]):Curve.Values[1,i];}
            output.Save(path,culture);
        }
    }
}
