using System.Windows;
using System.Globalization;
using System.Windows.Markup;
namespace Mdiapp.Wpf
{
    public partial class App : Application
    {
        public bool LaunchMainWindow { get; set; } = true;
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            if(LaunchMainWindow)new MainWindow(null, IsEngineMode(e.Args)).Show();
        }
        public static bool IsEngineMode(string[] args)=>args!=null&&args.Length>0&&string.Equals(args[0],"engine",System.StringComparison.OrdinalIgnoreCase);
        public App()
        {
            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
        }
    }
}
