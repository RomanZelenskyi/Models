using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Mdiapp.Core;

namespace Mdiapp.Wpf
{
    public static class NoticeWindows
    {
        public static Window About(ApplicationSettings settings)
        {
            var panel=new StackPanel{Margin=new Thickness(24)};panel.Children.Add(new TextBlock{Text="Gas Turbine Engine Modeling",FontSize=24,Margin=new Thickness(0,0,0,16)});panel.Children.Add(new TextBlock{Text="Версия исходной модели: "+settings.Version,Margin=new Thickness(0,0,0,8)});panel.Children.Add(new TextBlock{Text="MDIAPP — C# / WPF / .NET Framework 4.8",Margin=new Thickness(0,0,0,16)});
            foreach(string text in new[]{"2012–"+DateTime.Now.Year+" ©","Исполнители и разработчики:","Sergey Yepifanov — scientific direction","Roman Zelenskyi — modelling, testing","Alexey Chigrin — design, programming","Felix Sirenko — documentation"})panel.Children.Add(new TextBlock{Text=text,Margin=new Thickness(0,4,0,4)});
            var window=new Window{Title="О программе",Width=620,Height=440,Content=panel,WindowStartupLocation=WindowStartupLocation.CenterOwner};panel.Children.Add(Ui.Button("OK",()=>window.Close()));return window;
        }
        public static Window Create(ApplicationSettings settings,Action<string> open=null)
        {
            string path=settings.UpdateNoticePath;bool exists=path!=null&&File.Exists(path);var panel=new DockPanel{Margin=new Thickness(12)};
            var window=new Window{Title="Что нового — Unit42",Width=920,Height=620,Content=panel,WindowStartupLocation=WindowStartupLocation.CenterOwner};var bottom=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,12,0,0)};DockPanel.SetDock(bottom,Dock.Bottom);panel.Children.Add(bottom);
            var show=Ui.Check(bottom,"Показывать при запуске",settings.ShowUpdateNotice);show.Name="ShowNotice42";bottom.Children.Add(Ui.Button("OK",()=>{settings.SetShowUpdateNotice(show.IsChecked==true);window.Close();}));
            var header=new StackPanel();DockPanel.SetDock(header,Dock.Top);panel.Children.Add(header);header.Children.Add(new TextBlock{Text=exists?Path.GetFileName(path):"Файл новостей не найден.",Margin=new Thickness(0,0,0,8),TextWrapping=TextWrapping.Wrap});
            var button=Ui.Button("Открыть документ",()=>{if(open!=null)open(path);else Process.Start(new ProcessStartInfo(path){UseShellExecute=true});});button.IsEnabled=exists;header.Children.Add(button);
            var reader=new RichTextBox{IsReadOnly=true,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Margin=new Thickness(0,12,0,0)};reader.Name="NoticeText42";panel.Children.Add(reader);
            if(exists)
            {
                string extension=Path.GetExtension(path).ToLowerInvariant();var range=new TextRange(reader.Document.ContentStart,reader.Document.ContentEnd);
                if(extension==".rtf"){using(var stream=File.OpenRead(path))range.Load(stream,DataFormats.Rtf);}
                else if(extension==".txt"){range.Text=File.ReadAllText(path,LegacyCodec.TextEncoding);}
                else reader.Document.Blocks.Add(new Paragraph(new Run("Документ "+Path.GetFileName(path)+" открывается кнопкой выше в установленном просмотрщике.")));
            }
            return window;
        }
        public static void Show(ApplicationSettings settings)
        {
            var window=Create(settings);if(Application.Current?.MainWindow?.IsVisible==true)window.Owner=Application.Current.MainWindow;window.ShowDialog();
        }
    }
}
