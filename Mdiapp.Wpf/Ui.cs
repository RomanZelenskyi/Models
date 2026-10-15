using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Win32;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Mdiapp.Wpf
{
    public static class Ui
    {
        public static void Run(Action action) { try { action(); } catch (Exception error) { MessageBox.Show(error.Message, "MDIAPP", MessageBoxButton.OK, MessageBoxImage.Error); } }
        public static Button Button(string title, Action action) { var button = new Button { Content = InterfaceText.Translate(title) }; button.Click += (s,e) => Run(action); return button; }
        public static TextBox Text(Panel panel, string label, string value = "") { panel.Children.Add(new TextBlock { Text = InterfaceText.Translate(label), Margin = new Thickness(8,5,8,0) }); var text = new TextBox { Text = value, MinWidth = 120 }; panel.Children.Add(text); return text; }
        public static CheckBox Check(Panel panel, string label, bool value) { var check = new CheckBox { Content = InterfaceText.Translate(label), IsChecked = value, Margin = new Thickness(8) }; panel.Children.Add(check); return check; }
        public static ComboBox Choices(Panel panel, string label, System.Collections.IEnumerable items) { panel.Children.Add(new TextBlock { Text = InterfaceText.Translate(label), Margin = new Thickness(8) }); var combo = new ComboBox { ItemsSource = items }; panel.Children.Add(combo); return combo; }
        public static string Open(string title, string filter = "DAT|*.dat|Все файлы|*.*") { var dialog = new OpenFileDialog { Title = InterfaceText.Translate(title), Filter = filter }; return dialog.ShowDialog() == true ? dialog.FileName : null; }
        public static string Save(string path, string filter = "DAT|*.dat", string extension = "dat") { var dialog = new SaveFileDialog { FileName = System.IO.Path.GetFileName(path), InitialDirectory = System.IO.Path.GetDirectoryName(path), Filter = filter, DefaultExt = extension, AddExtension = true }; return dialog.ShowDialog() == true ? dialog.FileName : null; }
        public static string Folder(string title)
        {
            object shell=null,folder=null,self=null;
            try
            {
                shell=Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));
                int owner=Application.Current?.MainWindow?.IsVisible==true?new System.Windows.Interop.WindowInteropHelper(Application.Current.MainWindow).Handle.ToInt32():0;
                folder=shell.GetType().InvokeMember("BrowseForFolder",BindingFlags.InvokeMethod,null,shell,new object[]{owner,title,0x41,0});
                if(folder==null)return null;
                self=folder.GetType().InvokeMember("Self",BindingFlags.GetProperty,null,folder,null);
                string path=Convert.ToString(self.GetType().InvokeMember("Path",BindingFlags.GetProperty,null,self,null));
                return System.IO.Directory.Exists(path)?path:null;
            }
            finally {foreach(object obj in new[]{self,folder,shell})if(obj!=null&&Marshal.IsComObject(obj))Marshal.FinalReleaseComObject(obj);}
        }
        public static Window Dialog(string title, UIElement content, double width = 600, double height = 500) => new Window { Title = InterfaceText.Translate(title), Content = content, Width = width, Height = height, Owner = Application.Current?.MainWindow?.IsVisible == true ? Application.Current.MainWindow : null, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        public static DockPanel Editor(UIElement content, Action save, Action saveAs = null)
        {
            var dock = new DockPanel { Tag=save }; var bar = new StackPanel { Orientation = Orientation.Horizontal }; DockPanel.SetDock(bar, Dock.Bottom);
            bar.Children.Add(Button("Сохранить", save)); if (saveAs != null) bar.Children.Add(Button("Сохранить как…", saveAs)); dock.Children.Add(bar); dock.Children.Add(content); InterfaceText.Apply(dock); return dock;
        }
        public static void Commit(DependencyObject root)
        {
            if (root is DataGrid grid && (!grid.CommitEdit(DataGridEditingUnit.Cell, true) || !grid.CommitEdit(DataGridEditingUnit.Row, true))) throw new InvalidOperationException("Исправьте значение в таблице.");
            if (Validation.GetHasError(root)) throw new InvalidOperationException("Исправьте некорректное значение.");
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) Commit(VisualTreeHelper.GetChild(root, i));
        }
        public static void SaveEditors(DependencyObject root)
        {
            if(root is FrameworkElement element&&element.Tag is Action save){save();return;}
            if(root is TabControl tabs){foreach(var item in tabs.Items.OfType<TabItem>())if(item.Content is DependencyObject content)SaveEditors(content);return;}
            for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)SaveEditors(VisualTreeHelper.GetChild(root,i));
        }
        public static DataGrid Grid<T>(IEnumerable<T> values, params string[] readOnly)
        {
            var grid = new DataGrid { ItemsSource = values, AutoGenerateColumns = true, CanUserAddRows = false, CanUserDeleteRows = false };
            grid.AutoGeneratingColumn += (s,e) =>
            {
                e.Column.IsReadOnly = readOnly.Contains(e.PropertyName); e.Column.CanUserSort = false;
                var captions=new Dictionary<string,string>{{"Enabled","Разрешено"},{"Number","Номер"},{"Symbol","Символ"},{"Name","Название"},{"Node","Узел"},{"Value","Значение"},{"Minimum","Минимум"},{"Maximum","Максимум"},{"Code","Код"},{"Description","Описание"},{"Executable","EXE-файл"},{"Preg","PREG"},{"Graphic","График"},{"Key","Ключ"},{"Section","Секция"},{"Parameter","Параметр"}};
                if(captions.TryGetValue(e.PropertyName,out string caption))e.Column.Header=caption;
            };
            return grid;
        }
        public static void Tab(TabControl tabs, string title, UIElement content) => tabs.Items.Add(new TabItem { Header = InterfaceText.Translate(title), Content = content });
    }
}
