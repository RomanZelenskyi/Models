using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Mdiapp.Core;

namespace Mdiapp.Checks
{
    internal static class WpfChecks
    {
        public static void Run(string sourceRoot)
        {
            if(sourceRoot==null)return;
            var app=new Mdiapp.Wpf.App{LaunchMainWindow=false};app.InitializeComponent();app.ShutdownMode=ShutdownMode.OnExplicitShutdown;
            string temp=Path.Combine(Path.GetTempPath(),"WpfChecks-"+Guid.NewGuid());Directory.CreateDirectory(temp);
            try
            {
                var settings=new ApplicationSettings(sourceRoot,Path.Combine(temp,"models.ini"));
                using(var trace=new StringWriter())
                {
                    var main=new Mdiapp.Wpf.MainWindow(settings){ShowInTaskbar=false,ShowActivated=false,Left=-32000,Top=-32000};main.Show();Pump();
                    Check(main.Content is DockPanel,"WPF main XAML loads");
                    var preview=new RenderTargetBitmap((int)main.ActualWidth,(int)main.ActualHeight,96,96,PixelFormats.Pbgra32);preview.Render(main);var encoded=new PngBitmapEncoder();encoded.Frames.Add(BitmapFrame.Create(preview));using(var file=File.Create(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Wpf-main.png")))encoded.Save(file);main.Close();
                }
                Check(typeof(Mdiapp.Wpf.MainWindow).Assembly.GetReferencedAssemblies().All(a=>a.Name!="System.Windows.Forms"&&!a.Name.StartsWith("Telerik")),"WPF runtime has no WinForms or Telerik reference");
                Check(Mdiapp.Wpf.App.IsEngineMode(new[]{"engine"})&&Mdiapp.Wpf.App.IsEngineMode(new[]{"ENGINE"})&&!Mdiapp.Wpf.App.IsEngineMode(new string[0])&&!Mdiapp.Wpf.App.IsEngineMode(new[]{"other","engine"}),"WPF engine argument matches Delphi first argument");
                var engineMain=new Mdiapp.Wpf.MainWindow(settings,true){ShowInTaskbar=false,ShowActivated=false,Left=-32000,Top=-32000};engineMain.Show();Pump();Check(engineMain.EngineEditingMode&&Equals(((MenuItem)engineMain.FindName("EngineMenu")).Header,"Двигатели"),"WPF engine editing launch and menu");engineMain.Close();
                string editorRoot=Path.Combine(temp,"engine-editor");CatalogWorkspace.Initialize(sourceRoot,editorRoot);var editorSettings=new ApplicationSettings(editorRoot,Path.Combine(temp,"engine-editor-prefs.ini"));
                var changeEditor=Mdiapp.Wpf.CatalogEditor.Create(editorSettings,null,true);Check(!Mdiapp.Wpf.CatalogEditor.HasChanges(changeEditor),"WPF catalog initially unchanged");var changeHost=new Window{Content=changeEditor,Width=880,Height=646,ShowInTaskbar=false,ShowActivated=false,Left=-32000,Top=-32000};changeHost.Show();Pump();var changeGrid=Children(changeEditor).OfType<DataGrid>().Single(g=>g.Name=="ConfigurationsGrid");var changeRow=(Unit3Choice)changeGrid.Items[0];string originalName=changeRow.Name;changeGrid.SelectedIndex=1;Check(!Mdiapp.Wpf.CatalogEditor.HasChanges(changeEditor),"WPF catalog selection does not require saving");changeRow.Name="Changed";Check(Mdiapp.Wpf.CatalogEditor.HasChanges(changeEditor),"WPF catalog detects edited configuration");changeRow.Name=originalName;Check(!Mdiapp.Wpf.CatalogEditor.HasChanges(changeEditor),"WPF catalog reverting edit clears changes");changeHost.Close();
                var engineDialog=Mdiapp.Wpf.CatalogEditor.CreateWindow(editorSettings);var engineContent=(UIElement)engineDialog.Content;engineDialog.Content=null;Test("Wpf-engine-editor",engineContent);
                engineDialog=Mdiapp.Wpf.CatalogEditor.CreateWindow(editorSettings);
                Modal(engineDialog,()=>{Check(Children(engineDialog).OfType<Button>().Where(b=>Equals(b.Content,"Применить")).All(b=>!b.IsEnabled),"WPF Apply disabled before changes");var applyConfig=Children(engineDialog).OfType<DataGrid>().Single(g=>g.Name=="ConfigurationsGrid");applyConfig.SelectedIndex=0;applyConfig.CurrentCell=new DataGridCellInfo(applyConfig.Items[0],applyConfig.Columns[2]);applyConfig.BeginEdit();Pump();var editName=Children(applyConfig).OfType<TextBox>().Single();string previousName=editName.Text;editName.Text="Live change";Pump();Check(Children(engineDialog).OfType<Button>().Where(b=>Equals(b.Content,"Применить")).All(b=>b.IsEnabled),"WPF Apply enables while editing cell");editName.Text=previousName;Pump();Check(Children(engineDialog).OfType<Button>().Where(b=>Equals(b.Content,"Применить")).All(b=>!b.IsEnabled),"WPF Apply disables when cell restored");applyConfig.CommitEdit(DataGridEditingUnit.Cell,true);applyConfig.CommitEdit(DataGridEditingUnit.Row,true);engineDialog.Width=600;engineDialog.Height=400;Pump();var narrowGrid=Children(engineDialog).OfType<DataGrid>().Single(g=>g.Name=="EngineCatalog");Check(engineDialog.Content is Grid&&narrowGrid.Columns.Count==3&&Children(engineDialog).OfType<Button>().Where(b=>Equals(b.Content,"Применить")).All(b=>b.IsVisible),"WPF settings keep table scroll and buttons inside narrow panels");engineDialog.Width=880;engineDialog.Height=646;Pump();var panelGrid=(Grid)engineDialog.Content;panelGrid.ColumnDefinitions[0].Width=new GridLength(2000);Pump();Check(panelGrid.ColumnDefinitions[2].ActualWidth>=340&&panelGrid.ColumnDefinitions[0].ActualWidth<=panelGrid.ColumnDefinitions[0].MaxWidth,"WPF splitter cannot hide right panel");engineDialog.Width=600;Pump();Check(panelGrid.ColumnDefinitions[2].ActualWidth>=340,"WPF shrinking window keeps right panel visible after splitter move");engineDialog.Width=1200;Pump();var dragSplitter=Children(engineDialog).OfType<System.Windows.Controls.Primitives.Thumb>().Single(t=>t.Name=="SettingsSplitter");dragSplitter.RaiseEvent(new System.Windows.Controls.Primitives.DragDeltaEventArgs(800,0){RoutedEvent=System.Windows.Controls.Primitives.Thumb.DragDeltaEvent});Pump();engineDialog.Width=600;Pump();dragSplitter.RaiseEvent(new System.Windows.Controls.Primitives.DragDeltaEventArgs(300,0){RoutedEvent=System.Windows.Controls.Primitives.Thumb.DragDeltaEvent});Pump();Check(panelGrid.ColumnDefinitions[2].Width.IsStar&&panelGrid.ColumnDefinitions.Sum(c=>c.ActualWidth)<=panelGrid.ActualWidth+1&&panelGrid.ColumnDefinitions[2].ActualWidth>=340,"WPF actual splitter drag and window shrink keep both panels inside window");foreach(double resizedWidth in new[]{880d,550d,920d,550d}){engineDialog.Width=resizedWidth;Pump();dragSplitter.RaiseEvent(new System.Windows.Controls.Primitives.DragDeltaEventArgs(900,0){RoutedEvent=System.Windows.Controls.Primitives.Thumb.DragDeltaEvent});Pump();Check(panelGrid.ActualWidth<=engineDialog.ActualWidth&&panelGrid.ColumnDefinitions.Sum(c=>c.ActualWidth)<=panelGrid.ActualWidth+1&&panelGrid.ColumnDefinitions[2].ActualWidth>=340,"WPF panels stay within actual window client after repeated drag and resize "+resizedWidth);if(resizedWidth==550){var narrowBitmap=new RenderTargetBitmap((int)panelGrid.ActualWidth,(int)panelGrid.ActualHeight,96,96,PixelFormats.Pbgra32);narrowBitmap.Render(panelGrid);var narrowEncoder=new PngBitmapEncoder();narrowEncoder.Frames.Add(BitmapFrame.Create(narrowBitmap));using(var narrowFile=File.Create(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Wpf-settings-narrow.png")))narrowEncoder.Save(narrowFile);}}panelGrid.ColumnDefinitions[0].Width=new GridLength(185);engineDialog.Width=880;Pump();var config=Children(engineDialog).OfType<DataGrid>().Single(g=>g.Name=="ConfigurationsGrid");var first=(Unit3Choice)config.Items[0];first.Name="Test configuration";first.Enabled=true;((Unit3Engine)Children(engineDialog).OfType<DataGrid>().Single(g=>g.Name=="EngineCatalog").SelectedItem).Description="Test engine description";Children(engineDialog).OfType<Button>().Last(b=>Equals(b.Content,"Применить")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Check(new Unit3Model(editorRoot,editorSettings.Language,editorSettings.Engine).Configurations[0].Name=="Test configuration","WPF bottom Apply persists project settings without closing");Check(Children(engineDialog).OfType<Button>().Where(b=>Equals(b.Content,"Применить")).All(b=>!b.IsEnabled),"WPF Apply disabled after saving");Children(engineDialog).OfType<Button>().Single(b=>Equals(b.Content,"OK")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));});
                var savedCatalog=new Unit3Model(editorRoot,editorSettings.Language,editorSettings.Engine);Check(savedCatalog.Configurations[0].Name=="Test configuration"&&savedCatalog.Configurations[0].Enabled&&savedCatalog.Engines.Single(item=>item.Name==editorSettings.Engine).Description=="Test engine description","WPF engine editor saves configuration selection and description");
                Test("Wpf-Unit2",Mdiapp.Wpf.Editors.Series());
                string project=Path.Combine(sourceRoot,"sp-12","datar","dinam.inm");Test("Wpf-INM",Mdiapp.Wpf.Editors.Project(project));
                Test("Wpf-Unit3",Mdiapp.Wpf.CatalogEditor.Create(settings));
                Test("Wpf-project",new Mdiapp.Wpf.ProjectWorkspace(settings,project));
                var workspace=new Mdiapp.Wpf.ProjectWorkspace(settings,project);var projectWindow=new Window{Content=workspace,Width=1200,Height=900,Left=-32000,Top=-32000,ShowInTaskbar=false,ShowActivated=false};projectWindow.Show();Pump();
                Check(workspace.Pages.Items.Count==7,"WPF full project pages");Children(workspace).OfType<TextBox>().Single(t=>t.Name=="ProjectRemark").Text="WPF project change";workspace.Pages.SelectedIndex=1;Pump();var pointGrid=Children(workspace).OfType<DataGrid>().Single();((System.Data.DataRowView)pointGrid.Items[1])["Value0"]="0.375";
                workspace.Pages.SelectedIndex=3;Pump();Children(workspace).OfType<TextBox>().Single(t=>t.Name=="ProjectNodeIds").Text="1";Children(workspace).OfType<TextBox>().Single(t=>t.Name=="ProjectNodeValues").Text="0.125";workspace.OpenFile("..\\data\\XVHOD.dat");Pump();
                Check(InputFiles3132.Parse(Children(workspace).OfType<TextBox>().Single(t=>t.Name=="Correction0").Text,System.Globalization.CultureInfo.CurrentCulture)==.125,"WPF project passes node correction to Unit27");
                var editorGrid=Children(workspace).OfType<DataGrid>().Single();((System.Data.DataRowView)editorGrid.Items[1])["Point0"]="0.875";string savedProject=Path.Combine(temp,"saved-engine","datar","project.inm");workspace.SaveTo(savedProject);var full=ProjectDocument.Load(savedProject);
                Check(full.Remark=="WPF project change"&&full.OperatingPoints[0][0]==.375&&full.NodeValues[0]==.125,"WPF project saves hidden operating points and corrections");Check(InputCurveFile.Load(Path.Combine(temp,"saved-engine","data","XVHOD.dat")).Values[1,0]==.875,"WPF project saves editor DAT copies with INM");
                var sampleResults=new CalculationResults(new[]{421,16});sampleResults.Rows.Add(new[]{0d,10});sampleResults.Rows.Add(new[]{1d,20});workspace.SetResults(sampleResults);Pump();Check(Children(workspace).OfType<DataGrid>().Single().Items.Count==2,"WPF project displays result records");Children(workspace).OfType<TextBox>().Single(t=>t.Name=="SelectedResultRows").Text="2";Check(workspace.SelectedResults().Rows.Count==1&&workspace.SelectedResults().Rows[0][1]==20,"WPF results operating-point selection");Children(workspace).OfType<TextBox>().Single(t=>t.Name=="SelectedResultRows").Text="";workspace.Pages.SelectedIndex=6;Pump();var resultPlot=Children(workspace).OfType<Mdiapp.Wpf.CurvePlot>().Single();Check(resultPlot.Points.Length==2,"WPF project displays selectable result graph");resultPlot.ChangeZoom(2);Check(resultPlot.Zoom==2,"WPF result graph zoom");resultPlot.ResetView();Check(resultPlot.Zoom==1&&workspace.PrintableDocument().Blocks.Count>=2,"WPF result graph reset and printable document");projectWindow.Close();workspace.Dispose();
                var about=Mdiapp.Wpf.NoticeWindows.About(settings);var aboutContent=(UIElement)about.Content;about.Content=null;Test("Wpf-about",aboutContent);
                Test("Wpf-Unit11",Mdiapp.Wpf.Editors.Pair(settings,Path.Combine(sourceRoot,"sp-12","data","nezper0.DAT"),Path.Combine(sourceRoot,"sp-12","data","Prednp.dat")));
                var model=new InputFile30{Comment="WPF check"};string file30=Path.Combine(temp,"30.dat");File.WriteAllText(file30,model.Serialize(System.Globalization.CultureInfo.InvariantCulture),LegacyCodec.TextEncoding);Test("Wpf-Unit12",Mdiapp.Wpf.Editors.File30(settings,file30));
                Test("Wpf-Unit13",Mdiapp.Wpf.Editors.File28(settings,Path.Combine(sourceRoot,"sp-12","data","Xotbor1.dat")));
                string writable=Path.Combine(temp,"otbor.dat");File.Copy(Path.Combine(sourceRoot,"sp-12","data","Xotbor1.dat"),writable);
                var editor=Mdiapp.Wpf.Editors.File28(settings,writable);var editWindow=new Window{Content=editor,Width=1200,Height=800,Left=-32000,Top=-32000,ShowInTaskbar=false,ShowActivated=false};editWindow.Show();Pump();
                var controls=Children(editor).ToArray();var numeric=controls.OfType<DataGrid>().Single();((System.Data.DataRowView)numeric.Items[0])["Value0"]=0.125;
                controls.OfType<Button>().First(b=>(string)b.Content=="Сохранить").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var definitions=IniFile.Load(Path.Combine(sourceRoot,"sp-12","Ru","inputfiles.ini"));
                Check(InputFile28.Load(writable,definitions).Values[2,0]==0.125,"WPF table edit persists mapped physical value");editWindow.Close();
                Test("Wpf-Unit14",Mdiapp.Wpf.Editors.File24(settings,Path.Combine(sourceRoot,"sp-12","data","XKSM.dat")));
                string file24=Path.Combine(temp,"24.dat");File.Copy(Path.Combine(sourceRoot,"sp-12","data","XKSM.dat"),file24);
                var editor24=Mdiapp.Wpf.Editors.File24(settings,file24);
                var window24=new Window{Content=editor24,Width=1200,Height=800,Left=-32000,Top=-32000,ShowInTaskbar=false,ShowActivated=false};window24.Show();Pump();
                var fields24=Children(editor24).OfType<TextBox>().ToArray();
                Check(fields24.Length==13,"WPF Unit14 displays comment four base and eight additional fields");
                fields24[5].Text="0.12345678";
                Children(editor24).OfType<Button>().First(b=>(string)b.Content=="Сохранить").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Check(InputFile24.Load(file24,definitions,settings.Menu).AdditionalValues[0]==0.12345678,"WPF Unit14 edit saves to file");window24.Close();
                Test("Wpf-Unit15",Mdiapp.Wpf.Editors.File41(settings,Path.Combine(sourceRoot,"sp-12","data","TEPL.DAT")));
                string file41=Path.Combine(temp,"41.dat");File.Copy(Path.Combine(sourceRoot,"sp-12","data","TEPL.DAT"),file41);
                var editor41=Mdiapp.Wpf.Editors.File41(settings,file41);
                var window41=new Window{Content=editor41,Width=1200,Height=800,Left=-32000,Top=-32000,ShowInTaskbar=false,ShowActivated=false};window41.Show();Pump();
                var tabs41=Children(editor41).OfType<TabControl>().Single();
                Check(tabs41.Items.Count==7,"WPF Unit15 six value groups and parameter group");
                Children(editor41).OfType<TextBox>().First(t=>t.Text=="10,2"||t.Text=="10.2").Text="0.125";
                tabs41.SelectedIndex=6;Pump();var selectors41=Children(editor41).OfType<ComboBox>().ToArray();
                Check(selectors41.Length==9&&!selectors41[3].IsEnabled,"WPF Unit15 nine selectors and disabled channel");
                selectors41[0].SelectedItem=selectors41[0].Items.Cast<Mdiapp.Wpf.Editors.ParameterOption>().Single(p=>p.Number==0);
                Children(editor41).OfType<Button>().First(b=>(string)b.Content=="Сохранить").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var saved41=InputFile41.Load(file41);Check(saved41.Values[0,0]==0.125&&saved41.Parameters[0]==0&&saved41.Parameters[1]==67,"WPF Unit15 edits save values and IDs");window41.Close();
                Test("Wpf-Unit16",Mdiapp.Wpf.CharacteristicEditor.Create(settings,Path.Combine(sourceRoot,"sp-12","data","Xtvd.dat")));
                string characteristic=Path.Combine(temp,"characteristic.dat");File.Copy(Path.Combine(sourceRoot,"sp-12","data","Xknd1.dat"),characteristic);
                var editor16=Mdiapp.Wpf.CharacteristicEditor.Create(settings,characteristic);
                var window16=new Window{Content=editor16,Width=1200,Height=800,Left=-32000,Top=-32000,ShowInTaskbar=false,ShowActivated=false};window16.Show();Pump();
                var tabs16=Children(editor16).OfType<TabControl>().Single();tabs16.SelectedIndex=1;Pump();
                var grid16=Children(editor16).OfType<DataGrid>().Single();
                Check(grid16.Items.Count==42&&grid16.Columns.Count==23,"WPF Unit16 real branch table dimensions");
                ((System.Data.DataRowView)grid16.Items[0])["Point0"]="12.345";
                Children(editor16).OfType<TextBox>().Single(t=>t.Name=="PointCount").Text="21";
                Children(editor16).OfType<Button>().Single(b=>(string)b.Content=="Применить размер").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Check(grid16.Columns.Count==24&&Convert.ToString(((System.Data.DataRowView)grid16.Items[0])["Point0"])=="12.345","WPF Unit16 resizing retains edits");
                Children(editor16).OfType<Button>().First(b=>(string)b.Content=="Сохранить").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Check(CharacteristicFile.Load(characteristic).Branches[0].Points[0,0]==12.345,"WPF Unit16 edit saves characteristic");window16.Close();
                Test("Wpf-Unit17",Mdiapp.Wpf.CurveEditor.Create(settings,Path.Combine(sourceRoot,"sp-12","data","clearance.dat")));
                string curve=Path.Combine(temp,"curve.dat");File.Copy(Path.Combine(sourceRoot,"sp-12","data","clearance.dat"),curve);
                var editor17=Mdiapp.Wpf.CurveEditor.Create(settings,curve);
                var window17=new Window{Content=editor17,Width=1200,Height=800,Left=-32000,Top=-32000,ShowInTaskbar=false,ShowActivated=false};window17.Show();Pump();
                var grid17=Children(editor17).OfType<DataGrid>().Single();var plot17=Children(editor17).OfType<Mdiapp.Wpf.CurvePlot>().Single();
                Check(grid17.Items.Count==2&&grid17.Columns.Count==10&&plot17.Points.Length==9,"WPF Unit17 two rows and chart points");
                ((System.Data.DataRowView)grid17.Items[1])["Point0"]="0.125";
                Check(plot17.Points[0].Y==0.125,"WPF Unit17 chart updates after editing");
                Children(editor17).OfType<TextBox>().Single(t=>t.Name=="CurvePointCount").Text="10";
                Children(editor17).OfType<Button>().Single(b=>(string)b.Content=="Применить число точек").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Check(grid17.Columns.Count==11&&plot17.Points.Length==9,"WPF Unit17 resizing retains points and ignores empty chart cells");
                var parameter17=Children(editor17).OfType<ComboBox>().Single();parameter17.SelectedItem=parameter17.Items.Cast<Mdiapp.Wpf.Editors.ParameterOption>().Single(p=>p.Number==0);
                Children(editor17).OfType<Button>().First(b=>(string)b.Content=="Сохранить").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var saved17=InputCurveFile.Load(curve);Check(saved17.Count==10&&saved17.Parameter==0&&saved17.Values[1,0]==0.125&&saved17.Values[0,9]==0,"WPF Unit17 saves values parameter and empty cells as zero");window17.Close();
                Test("Wpf-Unit18",Mdiapp.Wpf.CurveEditor.Create(settings,Path.Combine(sourceRoot,"sp-12","data","XPER1.dat"),true));
                string curve18=Path.Combine(temp,"curve18.dat");File.Copy(Path.Combine(sourceRoot,"sp-12","data","XPER4.dat"),curve18);
                var editor18=Mdiapp.Wpf.CurveEditor.Create(settings,curve18,true);
                var window18=new Window{Content=editor18,Width=1200,Height=800,Left=-32000,Top=-32000,ShowInTaskbar=false,ShowActivated=false};window18.Show();Pump();
                var grid18=Children(editor18).OfType<DataGrid>().Single();var plot18=Children(editor18).OfType<Mdiapp.Wpf.CurvePlot>().Single();
                Check(grid18.Columns.Count==4&&plot18.Points.Length==3,"WPF Unit18 loads type 18 table and chart");
                ((System.Data.DataRowView)grid18.Items[1])["Point1"]="0.875";
                Check(plot18.Points[1].Y==0.875,"WPF Unit18 chart updates after editing");
                Children(editor18).OfType<Button>().First(b=>(string)b.Content=="Сохранить").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var saved18=InputCurveFile.Load(curve18);Check(saved18.Type==18&&saved18.Parameter==603&&saved18.Values[1,1]==0.875,"WPF Unit18 saves type parameter and edited value");window18.Close();
                try{Mdiapp.Wpf.CurveEditor.Create(settings,curve18);throw new Exception("Unit17 accepted Unit18 file");}catch(InvalidOperationException){Check(true,"WPF Unit17 rejects Unit18 file in wrong editor");}
                Test("Wpf-Unit19",Mdiapp.Wpf.DoubleCurveEditor.Create(settings,Path.Combine(sourceRoot,"sp-12","data","XKS.dat")));
                string curve19=Path.Combine(temp,"curve19.dat");File.Copy(Path.Combine(sourceRoot,"sp-12","data","XKS.dat"),curve19);
                var editor19=Mdiapp.Wpf.DoubleCurveEditor.Create(settings,curve19);
                var window19=new Window{Content=editor19,Width=1200,Height=850,Left=-32000,Top=-32000,ShowInTaskbar=false,ShowActivated=false};window19.Show();Pump();
                var tabs19=Children(editor19).OfType<TabControl>().Single();var grid19=Children(editor19).OfType<DataGrid>().Single();
                Check(tabs19.Items.Count==3&&grid19.Columns.Count==6,"WPF Unit19 two curves and constants tabs");
                ((System.Data.DataRowView)grid19.Items[1])["Point0"]="0.875";
                tabs19.SelectedIndex=1;Pump();var grid19second=Children(editor19).OfType<DataGrid>().Single();
                Check(grid19second.Columns.Count==7,"WPF Unit19 independent second curve size");
                ((System.Data.DataRowView)grid19second.Items[1])["Point1"]="0.625";
                Check(Children(editor19).OfType<Mdiapp.Wpf.CurvePlot>().Single().Points[1].Y==0.625,"WPF Unit19 second chart updates");
                tabs19.SelectedIndex=2;Pump();Children(editor19).OfType<TextBox>().Last().Text="15.5";
                Children(editor19).OfType<Button>().First(b=>(string)b.Content=="Сохранить").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var saved19=InputDoubleCurveFile.Load(curve19);Check(saved19.Curves[0].Values[1,0]==0.875&&saved19.Curves[1].Values[1,1]==0.625&&saved19.Constants[2]==15.5&&saved19.Curves[0].Parameter==546&&saved19.Curves[1].Parameter==547,"WPF Unit19 saves both hidden curves constants and IDs");window19.Close();
                Test("Wpf-Unit20",Mdiapp.Wpf.TripleCurveEditor.Create(settings,Path.Combine(sourceRoot,"sp-12","data","XSOPL1.dat")));
                string curve20=Path.Combine(temp,"curve20.dat");File.Copy(Path.Combine(sourceRoot,"sp-12","data","XSOPL2.dat"),curve20);
                var editor20=Mdiapp.Wpf.TripleCurveEditor.Create(settings,curve20);
                var window20=new Window{Content=editor20,Width=1200,Height=850,Left=-32000,Top=-32000,ShowInTaskbar=false,ShowActivated=false};window20.Show();Pump();
                var tabs20=Children(editor20).OfType<TabControl>().Single();Check(tabs20.Items.Count==3,"WPF Unit20 three independent curve tabs");
                for(int i=0;i<3;i++)
                {
                    tabs20.SelectedIndex=i;Pump();var grid20=Children(editor20).OfType<DataGrid>().Single();((System.Data.DataRowView)grid20.Items[1])["Point0"]=(0.5+i/10d).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    Check(Children(editor20).OfType<Mdiapp.Wpf.CurvePlot>().Single().Points[0].Y==0.5+i/10d,"WPF Unit20 chart "+(i+1)+" updates");
                }
                Children(editor20).OfType<TextBox>().Single(t=>t.Name=="CurvePointCount").Text="9";
                Children(editor20).OfType<Button>().First(b=>(string)b.Content=="Сохранить").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var saved20=InputTripleCurveFile.Load(curve20);Check(saved20.Type==22&&saved20.Curves[0].Values[1,0]==0.5&&saved20.Curves[1].Values[1,0]==0.6&&saved20.Curves[2].Values[1,0]==0.7&&saved20.Curves[0].Count==8&&saved20.Curves[2].Count==9,"WPF Unit20 saves all curves and independent resized count");window20.Close();
                Test("Wpf-Unit22",Mdiapp.Wpf.ThreeColumnEditor.Create(settings,Path.Combine(sourceRoot,"sp-12","data","ResultTND.DAT")));
                string file22=Path.Combine(temp,"three22.dat");File.Copy(Path.Combine(sourceRoot,"sp-12","data","ResultSTN.DAT"),file22);
                var editor22=Mdiapp.Wpf.ThreeColumnEditor.Create(settings,file22);var window22=new Window{Content=editor22,Width=1200,Height=850,Left=-32000,Top=-32000,ShowInTaskbar=false,ShowActivated=false};window22.Show();Pump();
                var grid22=Children(editor22).OfType<DataGrid>().Single();var plot22=Children(editor22).OfType<Mdiapp.Wpf.CurvePlot>().Single();
                Check(grid22.Items.Count==2&&plot22.Points.Length==2&&plot22.SecondaryPoints.Length==2,"WPF Unit22 three columns and two chart series");
                ((System.Data.DataRowView)grid22.Items[1])["Value2"]="1.25";
                Check(plot22.SecondaryPoints[1].Y==1.25,"WPF Unit22 red series updates");
                Children(editor22).OfType<CheckBox>().Single().IsChecked=true;Check(plot22.ShowMarkers,"WPF Unit22 marker toggle");
                Children(editor22).OfType<TextBox>().Single(t=>t.Name=="RowCount22").Text="3";
                Children(editor22).OfType<TextBox>().Single(t=>t.Name=="InitialValue22").Text="1400.5";
                Children(editor22).OfType<Button>().First(b=>(string)b.Content=="Сохранить").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var saved22=InputThreeColumnFile.Load(file22);Check(saved22.Type==48&&saved22.InitialValue==1400.5&&saved22.Rows.Count==3&&saved22.Rows[1][2]==1.25&&saved22.Rows[2].All(v=>v==0),"WPF Unit22 saves resize initial value and edited row");window22.Close();
                Test("Wpf-Unit23",Mdiapp.Wpf.ThreeColumnEditor.Create(settings,Path.Combine(sourceRoot,"sp-12","data","ResultTNDBL.dat"),true));
                string file23=Path.Combine(temp,"three23.dat");File.Copy(Path.Combine(sourceRoot,"sp-12","data","ResultTNDBL.dat"),file23);
                var editor23=Mdiapp.Wpf.ThreeColumnEditor.Create(settings,file23,true);var window23=new Window{Content=editor23,Width=1200,Height=900,Left=-32000,Top=-32000,ShowInTaskbar=false,ShowActivated=false};window23.Show();Pump();
                var fields23=Children(editor23).OfType<TextBox>().Where(t=>t.Name.StartsWith("InitialValue23_")).ToArray();Check(fields23.Length==3,"WPF Unit23 three initial fields");
                fields23[0].Text="1400.25";fields23[1].Text="2.5";fields23[2].Text="45";
                var grid23=Children(editor23).OfType<DataGrid>().Single();((System.Data.DataRowView)grid23.Items[0])["Value1"]="0.125";
                Check(Children(editor23).OfType<Mdiapp.Wpf.CurvePlot>().Single().Points[0].Y==0.125,"WPF Unit23 graph updates");
                Children(editor23).OfType<Button>().First(b=>(string)b.Content=="Сохранить").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var saved23=InputThreeColumnFile.Load(file23);Check(saved23.Type==49&&saved23.InitialValues.SequenceEqual(new[]{1400.25,2.5,45})&&saved23.Rows[0][1]==0.125,"WPF Unit23 saves initial fields and table");window23.Close();
                try{Mdiapp.Wpf.ThreeColumnEditor.Create(settings,file23);throw new Exception("Unit22 accepted Unit23");}catch(InvalidOperationException){Check(true,"WPF Unit22 rejects Unit23 file in wrong editor");}
                string file24quad=Path.Combine(temp,"quad36.dat");Unit24Checks.Example().Save(file24quad,System.Globalization.CultureInfo.InvariantCulture);
                Test("Wpf-Unit24",Mdiapp.Wpf.QuadCurveEditor.Create(settings,file24quad));
                var editor24quad=Mdiapp.Wpf.QuadCurveEditor.Create(settings,file24quad);var window24quad=new Window{Content=editor24quad,Width=1200,Height=850,Left=-32000,Top=-32000,ShowInTaskbar=false,ShowActivated=false};window24quad.Show();Pump();
                var tabs24quad=Children(editor24quad).OfType<TabControl>().Single();Check(tabs24quad.Items.Count==4,"WPF Unit24 four independent tabs");
                for(int i=0;i<4;i++)
                {
                    tabs24quad.SelectedIndex=i;Pump();var grid24quad=Children(editor24quad).OfType<DataGrid>().Single();((System.Data.DataRowView)grid24quad.Items[1])["Point0"]=(0.5+i/10d).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    Check(Children(editor24quad).OfType<Mdiapp.Wpf.CurvePlot>().Single().Points[0].Y==0.5+i/10d,"WPF Unit24 chart "+(i+1)+" updates");
                }
                Check(((Mdiapp.Wpf.Editors.ParameterOption)Children(editor24quad).OfType<ComboBox>().Single().SelectedItem).Number==1999,"WPF Unit24 unknown parameter remains selected");
                Children(editor24quad).OfType<Button>().First(b=>(string)b.Content=="Сохранить").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var saved24quad=InputQuadCurveFile.Load(file24quad);Check(saved24quad.Curves.Select(c=>c.Values[1,0]).SequenceEqual(new[]{0.5,0.6,0.7,0.8})&&saved24quad.Curves[3].Count==100&&saved24quad.Curves[3].Parameter==1999,"WPF Unit24 saves edits from all four tabs");window24quad.Close();
                Test("Wpf-Unit25",Mdiapp.Wpf.ControllersEditor.Create(settings,Path.Combine(sourceRoot,"sp-12","data","Controllers_1.dat")));
                string controllers25=Path.Combine(temp,"controllers25.dat");File.Copy(Path.Combine(sourceRoot,"sp-12","data","Controllers_1.dat"),controllers25);
                var editor25=Mdiapp.Wpf.ControllersEditor.Create(settings,controllers25);var window25=new Window{Content=editor25,Width=1280,Height=1000,Left=-32000,Top=-32000,ShowInTaskbar=false,ShowActivated=false};window25.Show();Pump();
                var group25=Children(editor25).OfType<ComboBox>().Single(c=>c.Name=="ControllerGroup");var category25=Children(editor25).OfType<ComboBox>().Single(c=>c.Name=="ControllerCategory");
                Check(group25.Items.Count==5&&category25.Items.Count==3,"WPF Unit25 five groups and three categories");
                Children(editor25).OfType<TextBox>().Single(t=>t.Name=="ControllerComment").Text="WPF saved first";
                var grid25=Children(editor25).OfType<DataGrid>().Single();((System.Data.DataRowView)grid25.Items[1])["Point0"]="1175.25";
                group25.SelectedIndex=1;Pump();Children(editor25).OfType<TextBox>().Single(t=>t.Name=="ControllerComment").Text="WPF saved second";
                Children(editor25).OfType<Button>().First(b=>(string)b.Content=="Сохранить").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var saved25=ControllersFile.Load(controllers25);Check(saved25.Groups[0,0][0].Comment=="WPF saved first"&&saved25.Groups[0,0][0].Components[0].Table.Values[1,0]==1175.25&&saved25.Groups[0,1][0].Comment=="WPF saved second","WPF Unit25 switching groups commits pending table and comment edits");
                category25.SelectedIndex=1;Pump();Check(!Children(editor25).OfType<ListBox>().Single(l=>l.Name=="ControllerList").IsEnabled,"WPF Unit25 respects disabled additional category");
                Children(editor25).OfType<Button>().Single(b=>(string)b.Content=="Дополнительные значения").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Pump();
                Children(editor25).OfType<TextBox>().Single(t=>t.Name=="ControllerExtra2").Text="0.125";
                Children(editor25).OfType<Button>().First(b=>(string)b.Content=="Сохранить").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Check(ControllersFile.Load(controllers25).AdditionalValues[2]==0.125,"WPF Unit25 saves additional values");window25.Close();
                string extended25=Path.Combine(temp,"extended25.dat");Unit25Checks.Example().Save(extended25,System.Globalization.CultureInfo.InvariantCulture);
                var extendedEditor25=Mdiapp.Wpf.ControllersEditor.Create(settings,extended25);var extendedWindow25=new Window{Content=extendedEditor25,Width=1280,Height=850,Left=-32000,Top=-32000,ShowInTaskbar=false,ShowActivated=false};extendedWindow25.Show();Pump();
                var extendedGroup25=Children(extendedEditor25).OfType<ComboBox>().Single(c=>c.Name=="ControllerGroup");extendedGroup25.SelectedIndex=3;Pump();
                var componentTabs25=Children(extendedEditor25).OfType<TabControl>().Single();Check(componentTabs25.Items.Count==5,"WPF Unit25 extended mode exposes five components");
                componentTabs25.SelectedIndex=3;Pump();Children(extendedEditor25).OfType<TextBox>().Single(t=>t.Name=="ControllerConstant3").Text="3.875";
                extendedGroup25.SelectedIndex=4;Pump();Children(extendedEditor25).OfType<Button>().First(b=>(string)b.Content=="Сохранить").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Check(ControllersFile.Load(extended25).Groups[0,3][0].Components[3].Constant==3.875,"WPF Unit25 fourth component survives group switch");extendedWindow25.Close();
                string file26=Path.Combine(temp,"multi1500.dat");Unit26Checks.Example().Save(file26,System.Globalization.CultureInfo.InvariantCulture);Test("Wpf-Unit26",Mdiapp.Wpf.MultiCharacteristicEditor.Create(settings,file26));
                var editor26=Mdiapp.Wpf.MultiCharacteristicEditor.Create(settings,file26);var window26=new Window{Content=editor26,Width=1200,Height=900,Left=-32000,Top=-32000,ShowInTaskbar=false,ShowActivated=false};window26.Show();Pump();
                var characteristics26=Children(editor26).OfType<ComboBox>().Single(c=>c.Name=="Characteristics26");var branches26=Children(editor26).OfType<ListBox>().Single(l=>l.Name=="Branches26");
                Check(characteristics26.Items.Count==2&&branches26.Items.Count==2,"WPF Unit26 characteristic and branch selectors");
                Children(editor26).OfType<TextBox>().Single(t=>t.Name=="Mach26").Text="0.75";Children(editor26).OfType<TextBox>().Single(t=>t.Name=="BranchValue26").Text="15.5";
                ((System.Data.DataRowView)Children(editor26).OfType<DataGrid>().Single().Items[1])["Point0"]="0.625";
                branches26.SelectedIndex=1;Pump();var familyTabs26=Children(editor26).OfType<TabControl>().Single();familyTabs26.SelectedIndex=1;Pump();
                Check(Children(editor26).OfType<Mdiapp.Wpf.CurveFamilyPlot>().Single().Series.Length==2,"WPF Unit26 family graph displays all branches");
                characteristics26.SelectedIndex=1;Pump();Children(editor26).OfType<TextBox>().Single(t=>t.Name=="Mach26").Text="1.5";
                Children(editor26).OfType<Button>().First(b=>(string)b.Content=="Сохранить").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var saved26=MultiCharacteristicFile.Load(file26);Check(saved26.Characteristics[0].Mach==0.75&&saved26.Characteristics[1].Mach==1.5&&saved26.Characteristics[0].Branches[0].Value==15.5&&saved26.Characteristics[0].Branches[0].Curve.Values[1,0]==0.625,"WPF Unit26 switching selections preserves Mach branch and table edits");window26.Close();
                Test("Wpf-Unit27",Mdiapp.Wpf.CorrectedCurveEditor.Create(settings,Path.Combine(sourceRoot,"sp-12","data","XVHOD.dat")));
                string file27=Path.Combine(temp,"corrected1.dat");File.Copy(Path.Combine(sourceRoot,"sp-12","data","XVHOD.dat"),file27);
                var editor27=Mdiapp.Wpf.CorrectedCurveEditor.Create(settings,file27);var window27=new Window{Content=editor27,Width=1200,Height=900,Left=-32000,Top=-32000,ShowInTaskbar=false,ShowActivated=false};window27.Show();Pump();
                var plot27=Children(editor27).OfType<Mdiapp.Wpf.CurvePlot>().Single();Children(editor27).OfType<TextBox>().Single(t=>t.Name=="Correction2").Text="0.125";
                Check(plot27.SecondaryPoints[0].Y==plot27.Points[0].Y*1.125,"WPF Unit27 corrected chart reacts to coefficient");
                Children(editor27).OfType<CheckBox>().Single(t=>t.Name=="BakeCorrection").IsChecked=true;Children(editor27).OfType<Button>().First(b=>(string)b.Content=="Сохранить").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var saved27=CorrectedCurveFile.Load(file27);Check(saved27.Curve.Count==9&&saved27.Curve.Parameter==158&&saved27.Curve.Values[1,0]==1.125&&saved27.References.SequenceEqual(new[]{0d,1,0,1}),"WPF Unit27 corrected save preserves valid file format");window27.Close();
                Test("Wpf-Unit28",Mdiapp.Wpf.CorrectedDoubleCurveEditor.Create(settings,Path.Combine(sourceRoot,"sp-12","data","XKS.dat")));
                string file28correction=Path.Combine(temp,"corrected14.dat");File.Copy(Path.Combine(sourceRoot,"sp-12","data","XKS.dat"),file28correction);
                var editor28correction=Mdiapp.Wpf.CorrectedDoubleCurveEditor.Create(settings,file28correction);var window28correction=new Window{Content=editor28correction,Width=1200,Height=900,Left=-32000,Top=-32000,ShowInTaskbar=false,ShowActivated=false};window28correction.Show();Pump();
                var tabs28correction=Children(editor28correction).OfType<TabControl>().Single();Children(editor28correction).OfType<TextBox>().Single(t=>t.Name=="Correction28_0_2").Text="0.125";
                var plot28first=Children(editor28correction).OfType<Mdiapp.Wpf.CurvePlot>().Single();Check(plot28first.SecondaryPoints[0].Y==plot28first.Points[0].Y*1.125,"WPF Unit28 first correction chart updates");
                tabs28correction.SelectedIndex=1;Pump();Children(editor28correction).OfType<TextBox>().Single(t=>t.Name=="Correction28_1_2").Text="0.25";
                var plot28second=Children(editor28correction).OfType<Mdiapp.Wpf.CurvePlot>().Single();Check(plot28second.SecondaryPoints[0].Y==plot28second.Points[0].Y*1.25,"WPF Unit28 second correction chart updates");
                Children(editor28correction).OfType<CheckBox>().Single(t=>t.Name=="BakeCorrection28").IsChecked=true;Children(editor28correction).OfType<Button>().First(b=>(string)b.Content=="Сохранить").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var saved28correction=CorrectedDoubleCurveFile.Load(file28correction);Check(saved28correction.Data.Curves[0].Count==5&&saved28correction.Data.Curves[1].Count==6&&Math.Abs(saved28correction.Data.Curves[0].Values[1,0]-0.9677*1.125)<=0.00000051&&saved28correction.Data.Curves[1].Values[1,0]==1.249125,"WPF Unit28 saves both corrected curves and valid counts");window28correction.Close();
                Test("Wpf-Unit29",Mdiapp.Wpf.CorrectedCurveEditor.Create(settings,Path.Combine(sourceRoot,"sp-12","data","XPER2.dat"),true));
                string file29=Path.Combine(temp,"correction11.dat");File.Copy(Path.Combine(sourceRoot,"sp-12","data","XPER2.dat"),file29);
                var editor29=Mdiapp.Wpf.CorrectedCurveEditor.Create(settings,file29,true);var window29=new Window{Content=editor29,Width=1200,Height=850,Left=-32000,Top=-32000,ShowInTaskbar=false,ShowActivated=false};window29.Show();Pump();
                Check(!Children(editor29).OfType<TextBox>().Single(t=>t.Name=="Correction1").IsVisible,"WPF Unit29 hides unused b coefficient");
                Children(editor29).OfType<TextBox>().Single(t=>t.Name=="Correction2").Text="0.125";
                var plot29=Children(editor29).OfType<Mdiapp.Wpf.CurvePlot>().Single();Check(plot29.SecondaryPoints[0].Y==plot29.Points[0].Y*1.125,"WPF Unit29 corrected graph updates");
                Children(editor29).OfType<CheckBox>().Single(t=>t.Name=="BakeCorrection").IsChecked=true;Children(editor29).OfType<Button>().First(b=>(string)b.Content=="Сохранить").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var saved29=CorrectedCurveFile.Load(file29,simple:true);Check(saved29.Curve.Type==11&&saved29.Curve.Count==9&&saved29.Curve.Parameter==545&&Math.Abs(saved29.Curve.Values[1,0]-0.999019*1.125)<=0.00000051,"WPF Unit29 corrected save preserves type count and parameter");window29.Close();
                Test("Wpf-Unit30",Mdiapp.Wpf.CorrectedCharacteristicEditor.Create(settings,Path.Combine(sourceRoot,"sp-12","data","Xknd1.dat")));
                string file30correction=Path.Combine(temp,"correctedBranches.dat");File.Copy(Path.Combine(sourceRoot,"sp-12","data","Xknd1.dat"),file30correction);
                var editor30correction=Mdiapp.Wpf.CorrectedCharacteristicEditor.Create(settings,file30correction);var window30correction=new Window{Content=editor30correction,Width=1200,Height=850,Left=-32000,Top=-32000,ShowInTaskbar=false,ShowActivated=false};window30correction.Show();Pump();
                Children(editor30correction).OfType<TextBox>().Single(t=>t.Name=="XCorrection30").Text="0.1";Children(editor30correction).OfType<CheckBox>().Single(t=>t.Name=="BakeCorrection30").IsChecked=true;
                Children(editor30correction).OfType<Button>().First(b=>(string)b.Content=="Сохранить").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var saved30correction=CorrectedCharacteristicFile.Load(file30correction);Check(Math.Abs(saved30correction.Data.Branches[0].Points[0,0]-13.697*1.1)<=0.00051&&saved30correction.Data.Coefficients.All(v=>v==1),"WPF Unit30 applies correction and keeps valid scale row");window30correction.Close();
                Test("Wpf-Unit31",Mdiapp.Wpf.CorrectedCharacteristicEditor.Create(settings,Path.Combine(sourceRoot,"sp-12","data","Xtvd.dat"),true));
                string file31correction=Path.Combine(temp,"correctedTurbine.dat");File.Copy(Path.Combine(sourceRoot,"sp-12","data","Xtvd.dat"),file31correction);
                var editor31=Mdiapp.Wpf.CorrectedCharacteristicEditor.Create(settings,file31correction,true);var window31=new Window{Content=editor31,Width=1200,Height=850,Left=-32000,Top=-32000,ShowInTaskbar=false,ShowActivated=false};window31.Show();Pump();
                Check(Children(editor31).OfType<TextBox>().Count(t=>t.Name.StartsWith("Reference31_"))==4,"WPF Unit31 four reference fields");
                Children(editor31).OfType<TextBox>().Single(t=>t.Name=="Coefficient31_0_2").Text="0.125";Children(editor31).OfType<CheckBox>().Single(t=>t.Name=="BakeCorrection31").IsChecked=true;
                Children(editor31).OfType<Button>().First(b=>(string)b.Content=="Сохранить").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var saved31correction=CorrectedCharacteristicFile.Load(file31correction,turbine:true);Check(Math.Abs(saved31correction.Data.Branches[0].Points[0,0]-0.789*1.125)<=0.00051&&saved31correction.Data.Branches[0].Points[1,0]==1.425&&saved31correction.Data.Interpolation==5,"WPF Unit31 corrected save retains second row and interpolation");window31.Close();
                Test("Wpf-Unit32",Mdiapp.Wpf.ExtendedFile28Editor.Create(settings,Path.Combine(sourceRoot,"sp-12","data","Xotbor1.dat")));
                string file32extended=Path.Combine(temp,"extended281.dat");File.Copy(Path.Combine(sourceRoot,"sp-12","data","Xotbor2.dat"),file32extended);
                var editor32=Mdiapp.Wpf.ExtendedFile28Editor.Create(settings,file32extended);var window32=new Window{Content=editor32,Width=1200,Height=850,Left=-32000,Top=-32000,ShowInTaskbar=false,ShowActivated=false};window32.Show();Pump();
                var tabs32=Children(editor32).OfType<TabControl>().Single();var grid32=Children(editor32).OfType<DataGrid>().Single();((System.Data.DataRowView)grid32.Items[0])["Value0"]=0.175;
                tabs32.SelectedIndex=1;Pump();Check(Children(editor32).OfType<TextBox>().Count(t=>t.Name.StartsWith("Coefficient32_"))==3,"WPF Unit32 three coefficient fields");Children(editor32).OfType<TextBox>().Single(t=>t.Name=="Coefficient32_2").Text="0.125";
                Children(editor32).OfType<Button>().First(b=>(string)b.Content=="Сохранить").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var saved32=InputExtendedFile28.Load(file32extended,definitions);Check(saved32.Data.Type==281&&saved32.Data.Values[2,0]==0.175&&saved32.Coefficients[2]==0.125,"WPF Unit32 saves hidden matrix and coefficient retaining type");window32.Close();
                Test("Wpf-Unit33",Mdiapp.Wpf.CorrectedTripleCurveEditor.Create(settings,Path.Combine(sourceRoot,"sp-12","data","XSOPL1.dat")));
                string file33=Path.Combine(temp,"correctedTriple.dat");File.Copy(Path.Combine(sourceRoot,"sp-12","data","XSOPL2.dat"),file33);
                var editor33=Mdiapp.Wpf.CorrectedTripleCurveEditor.Create(settings,file33);var window33=new Window{Content=editor33,Width=1200,Height=900,Left=-32000,Top=-32000,ShowInTaskbar=false,ShowActivated=false};window33.Show();Pump();
                var tabs33=Children(editor33).OfType<TabControl>().Single();Check(tabs33.Items.Count==3,"WPF Unit33 three correction tabs");
                for(int i=0;i<3;i++){tabs33.SelectedIndex=i;Pump();Children(editor33).OfType<TextBox>().Single(t=>t.Name=="Correction33_"+i+"_2").Text="0.125";var plot33=Children(editor33).OfType<Mdiapp.Wpf.CurvePlot>().Single();Check(plot33.SecondaryPoints[0].Y==plot33.Points[0].Y*1.125,"WPF Unit33 correction chart "+(i+1)+" updates");}
                Children(editor33).OfType<CheckBox>().Single(t=>t.Name=="BakeCorrection33").IsChecked=true;Children(editor33).OfType<Button>().First(b=>(string)b.Content=="Сохранить").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var saved33=CorrectedTripleCurveFile.Load(file33);var original33=CorrectedTripleCurveFile.Load(Path.Combine(sourceRoot,"sp-12","data","XSOPL2.dat"));Check(saved33.Data.Type==22&&saved33.Data.Curves.Zip(original33.Data.Curves,(a,b)=>a.Count==b.Count&&a.Parameter==b.Parameter&&Math.Abs(a.Values[1,0]-b.Values[1,0]*1.125)<=0.00000051).All(v=>v),"WPF Unit33 saves all corrected curves valid counts and IDs");window33.Close();
                Test("Wpf-Unit34",Mdiapp.Wpf.File300Editor.Create(settings,Path.Combine(sourceRoot,"sp-12","data","MEH.dat")));
                string file34=Path.Combine(temp,"mechanical.dat");File.Copy(Path.Combine(sourceRoot,"sp-12","data","MEH1.dat"),file34);
                var editor34=Mdiapp.Wpf.File300Editor.Create(settings,file34);var window34=new Window{Content=editor34,Width=1200,Height=850,Left=-32000,Top=-32000,ShowInTaskbar=false,ShowActivated=false};window34.Show();Pump();
                Children(editor34).OfType<TextBox>().Single(t=>t.Name=="Value300_0").Text="0.875";
                var tabs34=Children(editor34).OfType<TabControl>().Single();Check(tabs34.Items.Count==15,"WPF Unit34 all fourteen curve tabs");tabs34.SelectedIndex=14;Pump();
                var grid34=Children(editor34).OfType<DataGrid>().Single();var row34=(System.Data.DataRowView)grid34.Items[1];row34["Point0"]="123.5";
                Check(Children(editor34).OfType<Mdiapp.Wpf.CurvePlot>().Single().Points.Length>0,"WPF Unit34 last curve graph");
                Children(editor34).OfType<Button>().First(b=>(string)b.Content=="Сохранить").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var saved34=InputFile300.Load(file34);Check(saved34.InitialValues[0][0]==.875&&saved34.Curves[13].Values[1,0]==123.5&&File.Exists(file34+".bak"),"WPF Unit34 saves hidden scalar and last curve");window34.Close();
                Test("Wpf-Unit35",Mdiapp.Wpf.SurfaceEditor.Create(settings,Path.Combine(sourceRoot,"sp-12","data","ResultSTV.DAT")));
                string file35=Path.Combine(temp,"surface.dat");File.Copy(Path.Combine(sourceRoot,"sp-12","data","ResultSTV.DAT"),file35);
                var editor35=Mdiapp.Wpf.SurfaceEditor.Create(settings,file35);var window35=new Window{Content=editor35,Width=1200,Height=850,Left=-32000,Top=-32000,ShowInTaskbar=false,ShowActivated=false};window35.Show();Pump();
                Check(Children(editor35).OfType<Image>().Count()==6,"WPF Unit35 original scale formula images");Children(editor35).OfType<TextBox>().Single(t=>t.Name=="Scale35_5").Text="0.875";
                var tabs35=Children(editor35).OfType<TabControl>().Single();tabs35.SelectedIndex=1;Pump();
                var grid35=Children(editor35).OfType<DataGrid>().Single();((System.Data.DataRowView)grid35.Items[2])["Value12"]="12.5";
                Children(editor35).OfType<TextBox>().Single(t=>t.Name=="TemperatureValue35").Text="425";Children(editor35).OfType<TextBox>().Single(t=>t.Name=="BranchValue35").Text="0.375";
                Children(editor35).OfType<ComboBox>().Single(t=>t.Name=="Temperature35").SelectedIndex=5;Pump();Children(editor35).OfType<ComboBox>().Single(t=>t.Name=="Branch35").SelectedIndex=2;Pump();
                grid35=Children(editor35).OfType<DataGrid>().Single();((System.Data.DataRowView)grid35.Items[2])["Value12"]="99.25";
                tabs35.SelectedIndex=3;Pump();grid35=Children(editor35).OfType<DataGrid>().Single();((System.Data.DataRowView)grid35.Items[4])["Value3"]="123.125";
                Children(editor35).OfType<Button>().First(b=>(string)b.Content=="Сохранить").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));var saved35=InputSurfaceFile.Load(file35);
                Check(saved35.Values[0,0,2,12]==12.5&&saved35.Temperatures[0]==425&&saved35.BranchValues[0,0]==.375&&saved35.Values[5,2,2,12]==99.25,"WPF Unit35 commits on selection and saves current hidden surface");
                Check(saved35.Scales[5]==.875&&saved35.Corrections[1].Values[3,3]==123.125&&File.Exists(file35+".bak"),"WPF Unit35 saves hidden scale and second transition block");
                tabs35.SelectedIndex=0;Pump();Children(editor35).OfType<TextBox>().Single(t=>t.Name=="Size35_0").Text="14";Children(editor35).OfType<TextBox>().Single(t=>t.Name=="Size35_2").Text="7";
                Children(editor35).OfType<Button>().First(b=>(string)b.Content=="Применить размеры основной таблицы").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                tabs35.SelectedIndex=3;Pump();Children(editor35).OfType<TextBox>().Single(t=>t.Name=="CorrectionBranches35_1").Text="3";Children(editor35).OfType<TextBox>().Single(t=>t.Name=="CorrectionPoints35_1").Text="5";
                Children(editor35).OfType<Button>().First(b=>(string)b.Content=="Применить размеры").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Children(editor35).OfType<Button>().First(b=>(string)b.Content=="Сохранить").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));saved35=InputSurfaceFile.Load(file35);
                Check(saved35.X.Length==14&&saved35.Temperatures.Length==7&&saved35.Values[5,2,2,12]==99.25&&saved35.Values[6,2,2,13]==0,"WPF Unit35 applies main dimensions without losing existing surface");
                Check(saved35.Corrections[1].BranchCount==3&&saved35.Corrections[1].Arguments.Length==5&&saved35.Corrections[1].Values[3,3]==123.125,"WPF Unit35 applies transition dimensions");window35.Close();
                Test("Wpf-Unit36",Mdiapp.Wpf.TransitionEditor.Create(settings,Path.Combine(sourceRoot,"sp-12","data","ResultTVDBL.dat")));
                string file36=Path.Combine(temp,"transition.dat");File.Copy(Path.Combine(sourceRoot,"sp-12","data","ResultTVDBL.dat"),file36);
                var editor36=Mdiapp.Wpf.TransitionEditor.Create(settings,file36);var window36=new Window{Content=editor36,Width=1200,Height=850,Left=-32000,Top=-32000,ShowInTaskbar=false,ShowActivated=false};window36.Show();Pump();
                Check(Children(editor36).OfType<Image>().Count()==1&&Children(editor36).OfType<Mdiapp.Wpf.CurvePlot>().Single().Points.Length==9,"WPF Unit36 scale image and primary curve");
                Children(editor36).OfType<TextBox>().Single(t=>t.Name=="Scale36").Text="2.5";Children(editor36).OfType<TextBox>().Single(t=>t.Name=="CurvePointCount").Text="10";
                var grid36=Children(editor36).OfType<DataGrid>().Single();((System.Data.DataRowView)grid36.Items[1])["Point8"]="0.625";
                var tabs36=Children(editor36).OfType<TabControl>().Single();tabs36.SelectedIndex=1;Pump();grid36=Children(editor36).OfType<DataGrid>().Single();((System.Data.DataRowView)grid36.Items[2])["Value2"]="9.25";
                Children(editor36).OfType<TextBox>().Single(t=>t.Name=="Branches36").Text="2";Children(editor36).OfType<TextBox>().Single(t=>t.Name=="Points36").Text="4";
                Children(editor36).OfType<Button>().First(b=>(string)b.Content=="Применить размеры").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Children(editor36).OfType<Button>().First(b=>(string)b.Content=="Сохранить").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));var saved36=InputTransitionFile.Load(file36);
                Check(saved36.Scale==2.5&&saved36.Curve.Count==10&&saved36.Curve.Values[1,8]==.625&&saved36.Curve.Values[1,9]==0,"WPF Unit36 saves hidden primary curve and changed count");
                Check(saved36.Correction.BranchCount==2&&saved36.Correction.Arguments.Length==4&&saved36.Correction.Values[1,2]==9.25&&saved36.Correction.Values[3,3]==0,"WPF Unit36 transition resize and save preserve edited values");
                var original36=InputTransitionFile.Load(Path.Combine(sourceRoot,"sp-12","data","ResultTVDBL.dat"));Check(saved36.ScaleSuffix==original36.ScaleSuffix&&saved36.TrailingLines.SequenceEqual(original36.TrailingLines)&&File.Exists(file36+".bak"),"WPF Unit36 extra fields result tail and backup retained");window36.Close();
                Test("Wpf-Unit37",Mdiapp.Wpf.SurfaceEditor.Create(settings,Path.Combine(sourceRoot,"sp-12","data","ResultTVD.DAT"),true));
                string file37=Path.Combine(temp,"rotor.dat");File.Copy(Path.Combine(sourceRoot,"sp-12","data","ResultTVD.DAT"),file37);
                var editor37=Mdiapp.Wpf.SurfaceEditor.Create(settings,file37,true);var window37=new Window{Content=editor37,Width=1200,Height=850,Left=-32000,Top=-32000,ShowInTaskbar=false,ShowActivated=false};window37.Show();Pump();
                Check(Children(editor37).OfType<Image>().Count()==4&&Children(editor37).OfType<TextBox>().Count(t=>t.Name.StartsWith("Scale37_"))==4,"WPF Unit37 four scales and source images");
                Children(editor37).OfType<TextBox>().Single(t=>t.Name=="Scale37_3").Text="0.875";var axes37=Children(editor37).OfType<DataGrid>().First();((System.Data.DataRowView)axes37.Items[0])["Value0"]="0.225";
                var tabs37=Children(editor37).OfType<TabControl>().Single();Check(tabs37.Items.Count==3,"WPF Unit37 one transition block");tabs37.SelectedIndex=1;Pump();Check(Children(editor37).OfType<ComboBox>().Single(t=>t.Name=="Branch37").Visibility==Visibility.Collapsed,"WPF Unit37 no separate surface branch selector");
                var grid37=Children(editor37).OfType<DataGrid>().Single();((System.Data.DataRowView)grid37.Items[4])["Value5"]="77.75";Children(editor37).OfType<TextBox>().Single(t=>t.Name=="TemperatureValue37").Text="425";
                Children(editor37).OfType<ComboBox>().Single(t=>t.Name=="Temperature37").SelectedIndex=5;Pump();grid37=Children(editor37).OfType<DataGrid>().Single();((System.Data.DataRowView)grid37.Items[4])["Value5"]="88.25";
                tabs37.SelectedIndex=2;Pump();grid37=Children(editor37).OfType<DataGrid>().Single();((System.Data.DataRowView)grid37.Items[4])["Value3"]="12.875";
                Children(editor37).OfType<Button>().First(b=>(string)b.Content=="Сохранить").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));var saved37=InputSurfaceFile.Load(file37,unit37:true);
                Check(saved37.Values[0,0,4,5]==77.75&&saved37.Temperatures[0]==425&&saved37.Values[5,0,4,5]==88.25,"WPF Unit37 commits selected temperature and current hidden matrix");
                Check(saved37.X[0]==.225&&saved37.Scales[3]==.875&&saved37.Corrections[0].Values[3,3]==12.875,"WPF Unit37 saves hidden axis scale and transition values");
                tabs37.SelectedIndex=0;Pump();Children(editor37).OfType<TextBox>().Single(t=>t.Name=="Size37_0").Text="7";Children(editor37).OfType<TextBox>().Single(t=>t.Name=="Size37_2").Text="7";
                Children(editor37).OfType<Button>().First(b=>(string)b.Content=="Применить размеры основной таблицы").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                tabs37.SelectedIndex=2;Pump();Children(editor37).OfType<TextBox>().Single(t=>t.Name=="CorrectionBranches37_0").Text="3";Children(editor37).OfType<TextBox>().Single(t=>t.Name=="CorrectionPoints37_0").Text="5";
                Children(editor37).OfType<Button>().First(b=>(string)b.Content=="Применить размеры").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Children(editor37).OfType<Button>().First(b=>(string)b.Content=="Сохранить").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));saved37=InputSurfaceFile.Load(file37,unit37:true);
                Check(saved37.X.Length==7&&saved37.Temperatures.Length==7&&saved37.Values[5,0,4,5]==88.25&&saved37.Values[6,0,4,6]==0&&saved37.Corrections[0].BranchCount==3&&saved37.Corrections[0].Arguments.Length==5&&saved37.Corrections[0].Values[3,3]==12.875,"WPF Unit37 resizes both tables while preserving edits");
                Check(saved37.TrailingLines.SequenceEqual(InputSurfaceFile.Load(Path.Combine(sourceRoot,"sp-12","data","ResultTVD.DAT"),unit37:true).TrailingLines)&&File.Exists(file37+".bak"),"WPF Unit37 preserves result tail and backup");window37.Close();
                Test("Wpf-Unit38",Mdiapp.Wpf.File55Editor.Create(settings,Path.Combine(sourceRoot,"sp-12","data","otbor_dop.dat")));
                string file38=Path.Combine(temp,"55.dat");var input38=InputFile55.Load(Path.Combine(sourceRoot,"sp-12","data","otbor_dop.dat"));input38.Curves[14].Parameter=1999;input38.Save(file38);
                var editor38=Mdiapp.Wpf.File55Editor.Create(settings,file38);var window38=new Window{Content=editor38,Width=1200,Height=850,Left=-32000,Top=-32000,ShowInTaskbar=false,ShowActivated=false};window38.Show();Pump();
                var groups38=Children(editor38).OfType<TabControl>().First();Check(groups38.Items.Count==6&&groups38.Items.Cast<TabItem>().Sum(t=>((TabControl)t.Content).Items.Count)==15,"WPF Unit38 six groups and fifteen curves");
                var grid38=Children(editor38).OfType<DataGrid>().Single();((System.Data.DataRowView)grid38.Items[1])["Point0"]="0.375";groups38.SelectedIndex=4;Pump();
                var curves38=(TabControl)((TabItem)groups38.Items[4]).Content;curves38.SelectedIndex=3;Pump();grid38=Children(editor38).OfType<DataGrid>().Single();((System.Data.DataRowView)grid38.Items[1])["Point2"]="3.125";
                groups38.SelectedIndex=5;Pump();Check(Children(editor38).OfType<ComboBox>().Single().SelectedItem.ToString().Contains("1999")&&Children(editor38).OfType<Mdiapp.Wpf.CurvePlot>().Single().Points.Length==4,"WPF Unit38 unknown parameter and last curve graph");
                Children(editor38).OfType<TextBox>().Single(t=>t.Name=="CurvePointCount").Text="5";grid38=Children(editor38).OfType<DataGrid>().Single();((System.Data.DataRowView)grid38.Items[1])["Point3"]="2.625";
                Children(editor38).OfType<Button>().First(b=>(string)b.Content=="Сохранить").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));var saved38=InputFile55.Load(file38);
                Check(saved38.Curves[0].Values[1,0]==.375&&saved38.Curves[13].Values[1,2]==3.125&&saved38.Curves[14].Values[1,3]==2.625&&saved38.Curves[14].Count==5,"WPF Unit38 saves hidden first and extra curves and last count");
                Check(saved38.Curves[14].Parameter==1999&&saved38.Curves[13].Count==3&&File.Exists(file38+".bak"),"WPF Unit38 retains IDs physical order and backup");window38.Close();
                Test("Wpf-Unit39",Mdiapp.Wpf.File56Editor.Create(settings,Path.Combine(sourceRoot,"sp-12","data","ParTP.dat")));
                string file39=Path.Combine(temp,"56.dat");File.Copy(Path.Combine(sourceRoot,"sp-12","data","ParTP.dat"),file39);
                var editor39=Mdiapp.Wpf.File56Editor.Create(settings,file39);var window39=new Window{Content=editor39,Width=1200,Height=850,Left=-32000,Top=-32000,ShowInTaskbar=false,ShowActivated=false};window39.Show();Pump();
                Check(Children(editor39).OfType<Image>().Count()==10,"WPF Unit39 source mode and coefficient images");var mode39=Children(editor39).OfType<ComboBox>().Single(t=>t.Name=="Mode39_0");
                Children(editor39).OfType<TextBox>().Single(t=>t.Name=="Coefficient39_0_0").Text="123.5";mode39.SelectedIndex=1;Pump();Children(editor39).OfType<TextBox>().Single(t=>t.Name=="Coefficient39_0_0").Text="456.75";mode39.SelectedIndex=0;Pump();
                Check(InputFiles3132.Parse(Children(editor39).OfType<TextBox>().Single(t=>t.Name=="Coefficient39_0_0").Text,System.Globalization.CultureInfo.CurrentCulture)==123.5,"WPF Unit39 mode switch preserves first draft");mode39.SelectedIndex=1;Pump();Check(InputFiles3132.Parse(Children(editor39).OfType<TextBox>().Single(t=>t.Name=="Coefficient39_0_0").Text,System.Globalization.CultureInfo.CurrentCulture)==456.75,"WPF Unit39 mode switch preserves second draft");
                var tabs39=Children(editor39).OfType<TabControl>().Single();tabs39.SelectedIndex=1;Pump();Children(editor39).OfType<TextBox>().Single(t=>t.Name=="Extra39_1_3").Text="0.625";
                Children(editor39).OfType<Button>().First(b=>(string)b.Content=="Сохранить").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));var saved39=InputFile56.Load(file39);
                Check(saved39.Modes.SequenceEqual(new[]{2,1})&&saved39.Coefficients[0,1,0]==456.75&&saved39.Coefficients[1,0,0]==4250000&&saved39.AdditionalValues[1,3]==.625&&File.Exists(file39+".bak"),"WPF Unit39 saves hidden group selected modes and additional values");window39.Close();
                Test("Wpf-Unit40",Mdiapp.Wpf.File57Editor.Create(settings,Path.Combine(sourceRoot,"sp-12","data","ReLPT.dat")));
                string file40=Path.Combine(temp,"57.dat");var input40=InputFile57.Load(Path.Combine(sourceRoot,"sp-12","data","ReLPT.dat"));input40.Curves[1].Parameter=1999;input40.Save(file40);
                var editor40=Mdiapp.Wpf.File57Editor.Create(settings,file40);var window40=new Window{Content=editor40,Width=1200,Height=850,Left=-32000,Top=-32000,ShowInTaskbar=false,ShowActivated=false};window40.Show();Pump();
                Check(Children(editor40).OfType<Image>().Count()==4&&Children(editor40).OfType<Mdiapp.Wpf.CurvePlot>().Single().Points.Length==6,"WPF Unit40 constant images and first graph");
                Children(editor40).OfType<TextBox>().Single(t=>t.Name=="Constant40_0").Text="invalid";Children(editor40).OfType<TextBox>().Single(t=>t.Name=="Constant40_1").Text="-1000";Children(editor40).OfType<TextBox>().Single(t=>t.Name=="Constant40_2").Text="125.5";
                var grid40=Children(editor40).OfType<DataGrid>().Single();((System.Data.DataRowView)grid40.Items[1])["Point5"]="4.125";var tabs40=Children(editor40).OfType<TabControl>().Single();tabs40.SelectedIndex=1;Pump();
                Check(Children(editor40).OfType<ComboBox>().Single().SelectedItem.ToString().Contains("1999")&&Children(editor40).OfType<Mdiapp.Wpf.CurvePlot>().Single().Points.Length==6,"WPF Unit40 unknown second parameter and graph");
                grid40=Children(editor40).OfType<DataGrid>().Single();((System.Data.DataRowView)grid40.Items[1])["Point5"]="5.625";Children(editor40).OfType<TextBox>().Single(t=>t.Name=="CurvePointCount").Text="7";
                Children(editor40).OfType<Button>().First(b=>(string)b.Content=="Сохранить").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));var saved40=InputFile57.Load(file40);
                Check(saved40.Constants.SequenceEqual(new[]{1d,1,125.5}),"WPF Unit40 invalid constants and minus1000 normalize to one");Check(saved40.Curves[0].Values[1,5]==4.125&&saved40.Curves[1].Values[1,5]==5.625&&saved40.Curves[1].Parameter==1999&&saved40.Curves[1].Count==7&&File.Exists(file40+".bak"),"WPF Unit40 saves hidden curve second count unknown ID and backup");window40.Close();
                var noticeSettings=new ApplicationSettings(sourceRoot,Path.Combine(temp,"notice-preferences.ini"));string openedNotice=null;var notice=Mdiapp.Wpf.NoticeWindows.Create(noticeSettings,path=>openedNotice=path);var noticeContent=(UIElement)notice.Content;notice.Content=null;Test("Wpf-Unit42",noticeContent);
                notice=Mdiapp.Wpf.NoticeWindows.Create(noticeSettings,path=>openedNotice=path);Modal(notice,()=>{Children(notice).OfType<Button>().Single(b=>(string)b.Content=="Открыть документ").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Children(notice).OfType<CheckBox>().Single().IsChecked=false;Children(notice).OfType<Button>().Single(b=>(string)b.Content=="OK").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));});
                Check(openedNotice==noticeSettings.UpdateNoticePath&&!noticeSettings.ShowUpdateNotice,"WPF Unit42 PDF open action and startup preference");
                string noticeData=Path.Combine(temp,"notice-data");Directory.CreateDirectory(Path.Combine(noticeData,"Ru"));File.WriteAllText(Path.Combine(noticeData,"models.ini"),"[Interface]\r\nlang=Ru\r\nnew=notice.txt\r\n",LegacyCodec.TextEncoding);File.WriteAllText(Path.Combine(noticeData,"Ru","notice.txt"),"Новости программы",LegacyCodec.TextEncoding);var textSettings=new ApplicationSettings(noticeData,Path.Combine(temp,"text-notice.ini"));
                notice=Mdiapp.Wpf.NoticeWindows.Create(textSettings);notice.ShowInTaskbar=false;notice.Left=-32000;notice.Top=-32000;notice.WindowStartupLocation=WindowStartupLocation.Manual;notice.Show();Pump();Check(new System.Windows.Documents.TextRange(Children(notice).OfType<RichTextBox>().Single().Document.ContentStart,Children(notice).OfType<RichTextBox>().Single().Document.ContentEnd).Text.Contains("Новости программы"),"WPF Unit42 reads Windows1251 text");notice.Close();
                var noticeIni=IniFile.Load(Path.Combine(noticeData,"models.ini"));noticeIni.Set("Interface","new","notice.rtf");noticeIni.Save(Path.Combine(noticeData,"models.ini"));File.WriteAllText(Path.Combine(noticeData,"Ru","notice.rtf"),"{\\rtf1\\ansi Test notice}");notice=Mdiapp.Wpf.NoticeWindows.Create(textSettings);notice.ShowInTaskbar=false;notice.Left=-32000;notice.Top=-32000;notice.WindowStartupLocation=WindowStartupLocation.Manual;notice.Show();Pump();var rich=Children(notice).OfType<RichTextBox>().Single();Check(new System.Windows.Documents.TextRange(rich.Document.ContentStart,rich.Document.ContentEnd).Text.Contains("Test notice"),"WPF Unit42 reads RTF document");notice.Close();
                File.Delete(Path.Combine(noticeData,"Ru","notice.rtf"));notice=Mdiapp.Wpf.NoticeWindows.Create(textSettings);notice.ShowInTaskbar=false;notice.Left=-32000;notice.Top=-32000;notice.WindowStartupLocation=WindowStartupLocation.Manual;notice.Show();Pump();Check(!Children(notice).OfType<Button>().Single(b=>(string)b.Content=="Открыть документ").IsEnabled,"WPF Unit42 missing document disables open button");notice.Close();
                Test("Wpf-Unit21",Mdiapp.Wpf.Editors.File51(settings,Path.Combine(sourceRoot,"sp-12","data","volume.dat")));
                string copied=Path.Combine(temp,"SettingsData");Directory.CreateDirectory(copied);File.Copy(Path.Combine(sourceRoot,"models.ini"),Path.Combine(copied,"models.ini"));
                foreach(string lang in new[]{"Ru","En"}){Directory.CreateDirectory(Path.Combine(copied,lang));foreach(string file in new[]{"menu.ini","jobs.ini"})File.Copy(Path.Combine(sourceRoot,lang,file),Path.Combine(copied,lang,file));}
                var dialogSettings=new ApplicationSettings(copied,Path.Combine(temp,"ui-preferences.ini"));
                int notices=0;
                Mdiapp.Wpf.SettingsWindows.Language(dialogSettings,w=>Modal(w,()=>{Children(w).OfType<ComboBox>().Single().SelectedIndex=1;Children(w).OfType<Button>().First(b=>(string)b.Content=="OK").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}),message=>notices++);
                Check(dialogSettings.PreferredLanguage=="En"&&dialogSettings.Language=="Ru"&&notices==1,"WPF Unit7 saves pending language and restart notice");
                Mdiapp.Wpf.SettingsWindows.Engine(dialogSettings,w=>Modal(w,()=>Children(w).OfType<Button>().First(b=>(string)b.Content=="OK").RaiseEvent(new RoutedEventArgs(Button.ClickEvent))));
                Check(IniFile.Load(Path.Combine(temp,"ui-preferences.ini")).Get("Interface","lang")=="En"&&dialogSettings.EngineCode==3,"WPF Unit10 preserves pending language");
                Mdiapp.Wpf.SettingsWindows.Export(dialogSettings,w=>Modal(w,()=>{Children(w).OfType<CheckBox>().First().IsChecked=true;Children(w).OfType<Button>().First(b=>(string)b.Content=="Сохранить").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}));
                Check(ResultExportSettings.Load(Path.Combine(copied,"Ru","menu.ini")).Formats[0].Transpose,"WPF Unit8 saves export options");
                Mdiapp.Wpf.SettingsWindows.ResultPath(dialogSettings,"NONAME",w=>Modal(w,()=>{Check(Children(w).OfType<TextBox>().Single().Text.EndsWith("NONAME.txt"),"WPF Unit9 suggests result path");w.Close();}));
                string picture=Path.Combine(temp,"picture.png");var image=new RenderTargetBitmap(2,2,96,96,PixelFormats.Pbgra32);var pictureEncoder=new PngBitmapEncoder();pictureEncoder.Frames.Add(BitmapFrame.Create(image));using(var output=File.Create(picture))pictureEncoder.Save(output);
                bool created=Mdiapp.Wpf.SettingsWindows.CreateEngine(dialogSettings,present:w=>Modal(w,()=>
                {
                    var formBitmap=new RenderTargetBitmap((int)w.ActualWidth,(int)w.ActualHeight,96,96,PixelFormats.Pbgra32);formBitmap.Render(w);var formEncoder=new PngBitmapEncoder();formEncoder.Frames.Add(BitmapFrame.Create(formBitmap));using(var formOutput=File.Create(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Wpf-create-engine.png")))formEncoder.Save(formOutput);
                    var fields=Children(w).OfType<TextBox>().ToArray();fields[0].Text="wpf-created";fields[1].Text="19";fields[2].Text="WPF test";Children(w).OfType<TextBox>().Single(t=>t.Name=="EngineField6").Text=picture;
                    Children(w).OfType<Button>().First(b=>(string)b.Content=="OK").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }));
                Check(created&&File.Exists(Path.Combine(copied,"wpf-created","Ru","PROJECT.BMP")),"WPF Unit4 creates engine and converts PNG to BMP");
                bool blankCreated=Mdiapp.Wpf.SettingsWindows.CreateEngine(dialogSettings,present:w=>Modal(w,()=>{Children(w).OfType<TextBox>().Single(t=>t.Name=="EngineField0").Text="no-picture";Children(w).OfType<TextBox>().Single(t=>t.Name=="EngineField1").Text="18";Children(w).OfType<TextBox>().Single(t=>t.Name=="EngineField2").Text="Моя настройка";Children(w).OfType<Button>().Single(b=>Equals(b.Content,"OK")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}));
                Check(blankCreated&&Directory.Exists(Path.Combine(copied,"no-picture","bin"))&&File.Exists(Path.Combine(copied,"no-picture","Ru","jobs.ini"))&&!File.Exists(Path.Combine(copied,"no-picture","Ru","PROJECT.BMP")),"WPF Unit4 creates project with empty image and import paths");
                string oldRoot=Path.Combine(temp,"Legacy");Directory.CreateDirectory(Path.Combine(oldRoot,"old-source","datar"));File.Copy(picture,Path.Combine(oldRoot,"old-source","picture.png"));File.WriteAllText(Path.Combine(oldRoot,"old-source","datar","sample.inm"),"test");
                var profile=new LegacyEngineProfile{Name="old-source"};profile.Values["Key"]="20";profile.Values["BmpFileName"]="picture.png";profile.Values["Parameter numbers"]="385 1";profile.Jobs[1]="calc.exe/1/0";
                bool imported=Mdiapp.Wpf.SettingsWindows.CreateEngine(dialogSettings,profile,oldRoot,w=>Modal(w,()=>
                {
                    Children(w).OfType<TextBox>().First().Text="wpf-imported";Children(w).OfType<Button>().First(b=>(string)b.Content=="Импортировать").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }));
                Check(imported&&File.ReadAllText(Path.Combine(copied,"wpf-imported","datar","sample.inm"))=="test","WPF Unit5 imports snapshot and original project directory");
                var englishPrefs=IniFile.Load(Path.Combine(sourceRoot,"models.ini"));englishPrefs.Set("Interface","lang","En");string englishPath=Path.Combine(temp,"english.ini");englishPrefs.Save(englishPath);var englishSettings=new ApplicationSettings(sourceRoot,englishPath);var englishMain=new Mdiapp.Wpf.MainWindow(englishSettings){ShowInTaskbar=false,ShowActivated=false,Left=-32000,Top=-32000};englishMain.Show();Pump();Check(Children(englishMain).OfType<MenuItem>().Any(item=>Equals(item.Header,"File")),"WPF English main menu");englishMain.Close();Test("Wpf-project-en",new Mdiapp.Wpf.ProjectWorkspace(englishSettings,project));Mdiapp.Wpf.InterfaceText.Configure(settings);
            }
            finally{Directory.Delete(temp,true);app.Shutdown();}
        }
        private static void Test(string name,UIElement content)
        {
            var window=new Window{Content=content,Width=1200,Height=800,ShowInTaskbar=false,ShowActivated=false,Left=-32000,Top=-32000};window.Show();Pump();window.UpdateLayout();
            Check(window.Content!=null,name+" loads");
            var bitmap=new RenderTargetBitmap(1200,800,96,96,PixelFormats.Pbgra32);bitmap.Render(window);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var file=File.Create(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,name+".png")))encoder.Save(file);
            window.Close();
            if(content is IDisposable disposable)disposable.Dispose();
        }
        private static void Pump()=>Dispatcher.CurrentDispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
        private static void Modal(Window window,Action action){window.WindowStartupLocation=WindowStartupLocation.Manual;window.Left=-32000;window.Top=-32000;window.ShowInTaskbar=false;window.ShowActivated=false;window.Loaded+=(s,e)=>window.Dispatcher.BeginInvoke(action,DispatcherPriority.ApplicationIdle);window.ShowDialog();}
        private static System.Collections.Generic.IEnumerable<DependencyObject> Children(DependencyObject root){yield return root;for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)foreach(var child in Children(VisualTreeHelper.GetChild(root,i)))yield return child;}
        private static void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);}
    }
}















