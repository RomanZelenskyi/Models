using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Mdiapp.Core;

namespace Mdiapp.Wpf
{
    public static class File300Editor
    {
        public static UIElement Create(ApplicationSettings settings,string path)
        {
            var model=InputFile300.Load(path);var tabs=new TabControl();var values=new StackPanel{Margin=new Thickness(8)};Ui.Tab(tabs,"Исходные значения и коэффициенты",new ScrollViewer{Content=values,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
            var comment=Ui.Text(values,"Комментарий",model.Comment);var commits=new List<Action>();
            Action<string,double[],string[]> fields=(group,data,labels)=>
            {
                values.Children.Add(new TextBlock{Text=group,FontWeight=FontWeights.Bold,Margin=new Thickness(0,12,0,3)});
                for(int i=0;i<data.Length;i++){int index=i;var field=Ui.Text(values,labels[i],data[i].ToString(CultureInfo.CurrentCulture));field.Name="Value300_"+commits.Count;commits.Add(()=>data[index]=InputFiles3132.Parse(field.Text,CultureInfo.CurrentCulture));}
            };
            Func<string,string,string> label=(key,fallback)=>settings.Menu.Get("files",key,fallback);
            for(int i=0;i<2;i++)fields(label("n"+(i+1),"Исходные значения "+(i+1)),model.InitialValues[i],new[]{label("n11","1"),label("n12","2"),label("n13","3")});
            for(int group=0;group<2;group++)
            {
                var labels=new string[8];var side=new[]{14,15,15,14,17,18,18,17};for(int i=0;i<8;i++)labels[i]=label("f300l"+(group==0?10+i%4:23+i%4),"Коэффициент "+(i+1))+" / "+label("f300l"+side[i],"Граница "+(i+1));
                fields("Ограничения "+(group+1),model.Limits[group],labels);
            }
            fields("Константы",model.Constants,new[]{label("f300l22","Константа 1"),label("f300l22","Константа 2")});
            for(int row=0;row<4;row++){var labels=new string[model.AdditionalValues[row].Length];for(int i=0;i<labels.Length;i++)labels[i]=row<2?label("n"+(11+i),"Ротор "+(i+1)):label("f300l"+(42+(row-2)*12+i*3),"Значение "+(i+1))+" / "+label("f300l"+(43+(row-2)*12+i*3),"Граница");fields(row<2?label("f300l"+(40+row),"Дополнительные значения "+(row+1)):"Дополнительные ограничения "+(row-1),model.AdditionalValues[row],labels);}
            for(int i=0;i<14;i++)
            {
                string title=label("f300t"+(i+1),"Характеристика "+(i+1));if(string.IsNullOrWhiteSpace(title))title="Характеристика "+(i+1);
                Ui.Tab(tabs,(i+1)+". "+title,CurveEditor.CreateContent(settings,model.Curves[i],"f300l33","f300l34","f300t"+(i+1),out Action commit,false));commits.Add(commit);
            }
            Action<string> save=target=>{Ui.Commit(tabs);foreach(var commit in commits)commit();model.Comment=comment.Text;model.Save(target);path=target;};
            return Ui.Editor(tabs,()=>save(path),()=>{var target=Ui.Save(path);if(target!=null)save(target);});
        }
    }
}
