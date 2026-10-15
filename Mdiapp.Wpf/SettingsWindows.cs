using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Mdiapp.Core;

namespace Mdiapp.Wpf
{
    public static class SettingsWindows
    {
        public static void Language(ApplicationSettings settings,Action<Window> present=null,Action<string> notify=null)
        {
            var panel=new StackPanel();var combo=Ui.Choices(panel,"Язык интерфейса",settings.Languages);combo.SelectedItem=settings.Languages.FirstOrDefault(l=>l.Code==settings.PreferredLanguage);
            var window=Ui.Dialog("Unit7 — язык",panel,500,250);
            panel.Children.Add(Ui.Button("OK",()=>{if(settings.ChangeLanguage(combo.SelectedItem as LanguageOption)){string message=settings.Menu.Get("constants","n12","Перезапустите программу для применения языка.");if(notify!=null)notify(message);else MessageBox.Show(window,message);}window.DialogResult=true;}));if(present!=null)present(window);else window.ShowDialog();
        }
        public static void Engine(ApplicationSettings settings,Action<Window> present=null)
        {
            var panel=new StackPanel();var list=settings.GetEngines();var combo=Ui.Choices(panel,"Двигатель",list);combo.SelectedItem=list.FirstOrDefault(x=>x.Code==settings.EngineCode);if(combo.SelectedIndex<0&&list.Count>0)combo.SelectedIndex=0;
            var label=new TextBlock{Margin=new Thickness(8),TextWrapping=TextWrapping.Wrap};panel.Children.Add(label);combo.SelectionChanged+=(s,e)=>label.Text=(combo.SelectedItem as EngineOption)?.Description??"";label.Text=(combo.SelectedItem as EngineOption)?.Description??"";
            var window=Ui.Dialog("Unit10 — выбор двигателя",panel,600,300);panel.Children.Add(Ui.Button("OK",()=>{settings.ApplyEngine(combo.SelectedItem as EngineOption);window.DialogResult=true;}));if(present!=null)present(window);else window.ShowDialog();
        }
        public static void Export(ApplicationSettings settings,Action<Window> present=null)
        {
            string path=Path.Combine(settings.DataDirectory,settings.Language,"menu.ini");var model=ResultExportSettings.Load(path);var tabs=new TabControl();var trans=new CheckBox[3];var input=new CheckBox[3];var digits=new TextBox[3];CheckBox graphs=null;ComboBox format=null;
            for(int i=0;i<3;i++){var panel=new StackPanel();trans[i]=Ui.Check(panel,"Транспонировать",model.Formats[i].Transpose);input[i]=Ui.Check(panel,"Включить исходные данные",model.Formats[i].IncludeInput);digits[i]=Ui.Text(panel,"Знаков после запятой",model.Formats[i].Digits.ToString());if(i==0){format=Ui.Choices(panel,"Формат текста",new[]{"Пробел","Табуляция","Фиксированная ширина"});format.SelectedIndex=model.Formats[0].FormatIndex;}if(i==2)graphs=Ui.Check(panel,"Включить графики",model.Formats[2].IncludeGraphs);Ui.Tab(tabs,new[]{"Текст","HTML","Отчёт"}[i],panel);}
            Window window=null;var editor=Ui.Editor(tabs,()=>{for(int i=0;i<3;i++){model.Formats[i].Transpose=trans[i].IsChecked==true;model.Formats[i].IncludeInput=input[i].IsChecked==true;model.Formats[i].Digits=int.TryParse(digits[i].Text,out int n)?n:0;}model.Formats[0].FormatIndex=Math.Max(0,format.SelectedIndex);model.Formats[2].IncludeGraphs=graphs.IsChecked==true;model.Save(path);window.DialogResult=true;});window=Ui.Dialog("Unit8 — сохранение результатов",editor);if(present!=null)present(window);else window.ShowDialog();
        }
        public static void ResultPath(ApplicationSettings settings,string project,Action<Window> present=null)
        {
            var panel=new StackPanel();var format=Ui.Choices(panel,"Формат результатов",new[]{"Текст","HTML","Файл результатов"});var name=Ui.Text(panel,"Файл");format.SelectionChanged+=(s,e)=>name.Text=ResultFileSelection.Suggest(project,settings.DataDirectory,settings.Engine,(ResultFileFormat)(format.SelectedIndex+1));format.SelectedIndex=0;
            panel.Children.Add(Ui.Button("Выбрать файл…",()=>{var ext=ResultFileSelection.Extension((ResultFileFormat)(format.SelectedIndex+1));var selected=Ui.Save(name.Text,"Результаты|*"+ext,ext.TrimStart('.'));if(selected!=null)name.Text=selected;}));
            Window window=Ui.Dialog("Unit9 — файл результатов",panel,700,350);panel.Children.Add(Ui.Button("OK",()=>{var selection=new ResultFileSelection((ResultFileFormat)(format.SelectedIndex+1),name.Text);MessageBox.Show(window,"Выбрано: "+selection.FileName+"\nФайл создаётся командой «Экспорт результатов» в открытом проекте.");window.DialogResult=true;}));if(present!=null)present(window);else window.ShowDialog();
        }
        public static void ImportEngine(ApplicationSettings settings,Action<Window> present=null)
        {
            var panel=new StackPanel();var projects=LegacyEngineRegistry.ListProjects();var choice=Ui.Choices(panel,"Проект старой программы (Unit6)",projects);if(projects.Count>0)choice.SelectedIndex=0;var root=Ui.Text(panel,"Папка старых проектов",@"C:\Models");
            var window=Ui.Dialog("Unit5/6 — импорт двигателя",panel,650,350);
            panel.Children.Add(Ui.Button("Выбрать папку проектов",()=>{var folder=Ui.Folder("Папка старых проектов");if(folder!=null)root.Text=folder;}));
            var next=Ui.Button("Далее",()=>{if(choice.SelectedItem==null)throw new InvalidOperationException("ProjectList не найден или пуст в реестре старой программы.");var profile=LegacyEngineRegistry.Read(choice.SelectedItem.ToString());if(CreateEngine(settings,profile,root.Text))window.DialogResult=true;});next.IsEnabled=choice.SelectedItem!=null;panel.Children.Add(next);
            if(projects.Count==0)panel.Children.Add(new TextBlock{Text="В реестре старой программы нет списка ProjectList.",Margin=new Thickness(8)});
            if(present!=null)present(window);else window.ShowDialog();
        }
        public static bool CreateEngine(ApplicationSettings settings,LegacyEngineProfile profile=null,string sourceRoot=null,Action<Window> present=null)
        {
            var panel=new DockPanel{Margin=new Thickness(2,6,2,2)};
            var fields=Enumerable.Range(0,7).Select(i=>new TextBox{Name="EngineField"+i,MinWidth=0,Margin=new Thickness(0),Padding=new Thickness(2),Height=21,VerticalContentAlignment=VerticalAlignment.Center}).ToArray();
            if(profile!=null){fields[0].Text=profile.Name;fields[1].Text=profile.Get("Key");fields[2].Text=profile.Get("Project description");fields[3].Text=profile.SourcePath(sourceRoot,profile.Get("Executable files directiory"));fields[4].Text=profile.SourcePath(sourceRoot,profile.Get("Characteristic files"));fields[5].Text=profile.SourcePath(sourceRoot,profile.Get("DopParFileName"));fields[6].Text=profile.SourcePath(sourceRoot,profile.Get("BmpFileName"));}
            var window=Ui.Dialog(profile==null?"Создание проекта двигателя":"Импорт проекта двигателя",panel,524,300);window.ResizeMode=ResizeMode.NoResize;bool busy=false;window.Closing+=(s,e)=>e.Cancel=busy;
            var footer=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,8,2,4)};DockPanel.SetDock(footer,Dock.Bottom);panel.Children.Add(footer);
            var create=new Button{Content=profile==null?"OK":"Импортировать",IsDefault=true,Width=74,Height=24,Padding=new Thickness(4,2,4,2),Margin=new Thickness(4)};footer.Children.Add(create);
            var cancel=Ui.Button("Отмена",()=>window.DialogResult=false);cancel.Width=74;cancel.Height=24;cancel.Padding=new Thickness(4,2,4,2);cancel.IsCancel=true;footer.Children.Add(cancel);
            var body=new StackPanel();panel.Children.Add(body);
            Func<Action,Button> browse=action=>{var button=Ui.Button("",action);button.Width=27;button.Height=23;button.Padding=new Thickness(3);button.Margin=new Thickness(4,0,0,0);button.Background=System.Windows.Media.Brushes.Transparent;button.BorderThickness=new Thickness(0);button.ToolTip=InterfaceText.Translate("Выбрать файл или папку");var canvas=new Canvas{Width=16,Height=16};canvas.Children.Add(new System.Windows.Shapes.Path{Data=System.Windows.Media.Geometry.Parse("M1,5 L6,5 8,7 15,7 15,14 1,14 Z M1,14 L4,9 16,9 13,14 Z"),Fill=System.Windows.Media.Brushes.Khaki,Stroke=System.Windows.Media.Brushes.Black,StrokeThickness=1});button.Content=canvas;return button;};
            var header=new Grid{Margin=new Thickness(0,0,0,6)};foreach(double width in new[]{88d,48d,204d,1d})header.ColumnDefinitions.Add(new ColumnDefinition{Width=width==1?new GridLength(1,GridUnitType.Star):new GridLength(width)});body.Children.Add(header);
            int[] topFields={0,1,2,6};string[] labels={"Наименование","Номер","Описание","Изображение"};
            for(int i=0;i<4;i++){int field=topFields[i];var column=new StackPanel{Margin=new Thickness(0,0,6,0)};Grid.SetColumn(column,i);header.Children.Add(column);column.Children.Add(new TextBlock{Text=InterfaceText.Translate(labels[i]),Margin=new Thickness(0,0,0,4)});if(field==6){var row=new DockPanel();var button=browse(()=>{var path=Ui.Open("Изображение","Изображения|*.bmp;*.png;*.jpg;*.jpeg");if(path!=null)fields[6].Text=path;});DockPanel.SetDock(button,Dock.Right);row.Children.Add(button);row.Children.Add(fields[6]);column.Children.Add(row);}else column.Children.Add(fields[field]);}
            string[] importLabels={"Импортировать расчетные файлы из папки…","Импортировать подключаемые файлы из папки…","Импортировать параметры двигателя из файла…"};
            for(int i=0;i<3;i++){int field=i+3;body.Children.Add(new TextBlock{Text=InterfaceText.Translate(importLabels[i]),Margin=new Thickness(0,2,0,4)});var row=new DockPanel{Margin=new Thickness(0,0,0,6)};var button=browse(()=>{var path=field==5?Ui.Open("Параметры","Текст|*.txt|Все файлы|*.*"):Ui.Folder(field==3?"Расчётные файлы":"Входные данные");if(path!=null)fields[field].Text=path;});DockPanel.SetDock(button,Dock.Right);row.Children.Add(button);row.Children.Add(fields[field]);body.Children.Add(row);}
            var progress=new ProgressBar{Minimum=0,Maximum=100,Height=6,Visibility=Visibility.Collapsed};body.Children.Add(progress);
            create.Click+=async(s,e)=>
            {
                try
                {
                    if(!int.TryParse(fields[1].Text,out int code))throw new InvalidOperationException("Укажите целочисленный код.");
                    byte[] bytes=null;string picturePath=fields[6].Text.Trim();if(picturePath.Length>0){if(!File.Exists(picturePath))throw new FileNotFoundException("Не найден файл изображения. Выберите существующий файл или оставьте поле пустым.",picturePath);var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.UriSource=new Uri(Path.GetFullPath(picturePath));image.EndInit();var encoder=new BmpBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using(var stream=new MemoryStream()){encoder.Save(stream);bytes=stream.ToArray();}}
                    var request=new EngineCreationRequest{Name=fields[0].Text.Trim(),Code=code,Description=fields[2].Text,CalculationDirectory=fields[3].Text.Trim(),DataDirectory=fields[4].Text.Trim(),ParameterFile=fields[5].Text.Trim(),PictureBmp=bytes};
                    busy=true;panel.IsEnabled=false;progress.Visibility=Visibility.Visible;var report=new Progress<int>(v=>progress.Value=v);
                    await Task.Run(()=>profile==null?EngineCreation.Create(settings.DataDirectory,settings.Language,request,report):EngineImport.Import(settings.DataDirectory,settings.Language,profile,request,sourceRoot,report));
                    busy=false;window.DialogResult=true;
                }
                catch(Exception error){MessageBox.Show(window,error.Message,"Ошибка двигателя");}
                finally{busy=false;panel.IsEnabled=true;}
            };
            if(present!=null){present(window);return window.DialogResult==true;}return window.ShowDialog()==true;
        }
    }
}




