using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Mdiapp.Core;

namespace Mdiapp.Wpf
{
    public static class CorrectedCharacteristicEditor
    {
        public static UIElement Create(ApplicationSettings settings,string path,bool unit31=false)
        {
            var model=CorrectedCharacteristicFile.Load(path,turbine:unit31);var tabs=new TabControl();var correction=new StackPanel();Ui.Tab(tabs,"Коррекция",new ScrollViewer{Content=correction});
            correction.Children.Add(new TextBlock{Text=unit31?"Первый и третий ряды умножаются на 1+a·(ряд2−r1)/r2+b·(ветвь−r3)/r4+c. Второй ряд сохраняется.":"Y1/Y2 умножаются на 1+a·(ветвь−r1)/r2+b·(u−r3)+c; u нормируется по выбранному ряду. Первый ряд умножается на 1+Cx.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(8)});
            var references=Enumerable.Range(0,model.References.Length).Select(i=>Ui.Text(correction,"r"+(i+1),model.References[i].ToString(CultureInfo.CurrentCulture))).ToArray();for(int i=0;i<references.Length;i++)references[i].Name="Reference"+(unit31?31:30)+"_"+i;var coefficients=new TextBox[2,3];
            for(int curve=0;curve<2;curve++){var row=new StackPanel{Orientation=Orientation.Horizontal};correction.Children.Add(row);for(int i=0;i<3;i++){coefficients[curve,i]=Ui.Text(row,new[]{"a","b","c"}[i]+" (ряд "+(unit31?(curve==0?1:3):curve+2)+")",settings.CorrectionPreset(model.Data.Type)[curve*3+i].ToString(CultureInfo.CurrentCulture));coefficients[curve,i].Name="Coefficient"+(unit31?31:30)+"_"+curve+"_"+i;}}
            var xPanel=new StackPanel();correction.Children.Add(xPanel);var x=Ui.Text(xPanel,"Cx — поправка первого ряда",settings.XCorrectionPreset(model.Data.Type).ToString(CultureInfo.CurrentCulture));x.Name="XCorrection30";xPanel.Visibility=unit31?Visibility.Collapsed:Visibility.Visible;var bake=Ui.Check(correction,"Сохранить с применённой коррекцией",false);bake.Name="BakeCorrection"+(unit31?31:30);
            Ui.Tab(tabs,"Исходные данные",CharacteristicEditor.CreateContent(settings,model.Data,out Action commit,100));
            Action<string> save=target=>{commit();for(int i=0;i<references.Length;i++)model.References[i]=InputFiles3132.Parse(references[i].Text,CultureInfo.CurrentCulture);for(int curve=0;curve<2;curve++)for(int i=0;i<3;i++)model.Coefficients[curve,i]=InputFiles3132.Parse(coefficients[curve,i].Text,CultureInfo.CurrentCulture);model.XCorrection=InputFiles3132.Parse(x.Text,CultureInfo.CurrentCulture);model.Save(target,bake.IsChecked==true,CultureInfo.CurrentCulture,settings.Menu.Get("constants","n52","Коррекция применена"));path=target;};
            return Ui.Editor(tabs,()=>save(path),()=>{var target=Ui.Save(path);if(target!=null)save(target);});
        }
    }
}
