using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Mdiapp.Core
{
    // Unit32: Unit13 matrix plus a trailing row of three coefficients.
    public sealed class InputExtendedFile28
    {
        public InputFile28 Data { get; private set; }
        public double[] Coefficients { get; } = new double[3];
        public static InputExtendedFile28 Load(string path,IniFile definitions,CultureInfo culture=null)
        {
            culture=culture??CultureInfo.CurrentCulture;var model=new InputExtendedFile28{Data=InputFile28.Load(path,definitions,culture)};
            if(model.Data.TrailingLines.Count==0)throw new InvalidDataException("Для Unit32 нужна строка трёх коэффициентов после отборов мощности.");
            var values=model.Data.TrailingLines[0].Split('!')[0].Split(new[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries);if(values.Length!=3)throw new InvalidDataException("В строке коэффициентов должно быть три значения.");
            for(int i=0;i<3;i++)model.Coefficients[i]=InputFiles3132.Parse(values[i],culture);return model;
        }
        public void Save(string path,CultureInfo culture=null)
        {
            culture=culture??CultureInfo.CurrentCulture;if(Coefficients.Any(v=>double.IsNaN(v)||double.IsInfinity(v)))throw new InvalidDataException("Коэффициенты должны быть конечными числами.");
            int comment=Data.TrailingLines[0].IndexOf('!');string caption=comment<0?"":" !"+Data.TrailingLines[0].Substring(comment+1);
            string line=string.Join(" ",Coefficients.Select(v=>v.ToString("G",culture)))+caption;
            Data.Save(path,culture,new[]{line}.Concat(Data.TrailingLines.Skip(1)).ToArray());
        }
    }
}
