using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Iamp.Desktop;
using Iamp.Transport.Windows;

internal static class SettingsChecks
{
    public static void Run()
    {
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        var directory=Path.GetFullPath("analysis/private/settings-check-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        var path=Path.Combine(directory,"settings.json");
        var storeType=typeof(MainWindow).Assembly.GetType("Iamp.Desktop.DesktopSettingsStore")!;
        var nameType=typeof(MainWindow).Assembly.GetType("Iamp.Desktop.PresetNameCacheStore")!;
        object Store(string p)=>Activator.CreateInstance(storeType,flags,null,[p],null)!;
        object? Call(object o,string name,params object?[] args)=>o.GetType().GetMethod(name,flags)!.Invoke(o,args);
        T Field<T>(object o,string name)=>(T)o.GetType().GetField(name,flags)!.GetValue(o)!;
        T Property<T>(object o,string name)=>(T)o.GetType().GetProperty(name,flags)!.GetValue(o)!;
        MainWindow Create(bool preview=false,string? file=null)=>(MainWindow)Activator.CreateInstance(typeof(MainWindow),flags,null,[preview,Activator.CreateInstance(nameType,flags,null,[Path.Combine(directory,"names.json")],null)!,Store(file??path)],null)!;
        MainWindow? window=null;
        try
        {
            Require(Property<bool>(Call(Store(path),"Load")!,"PreserveWindowSize"),"Default is not enabled like the reference app");
            window=Create();Require(Field<CheckBox>(window,"preserveWindowSize").IsChecked==true,"Initial setting not checked");
            var defaults=new Size(window.Width,window.Height);
            window.Width=980;window.Height=650;Require(!File.Exists(path),"Offline construction/render saved window settings");
            window.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            window.Width=1000;window.Height=660;
            Require(Field<DispatcherTimer>(window,"windowSizeSaveTimer").Interval==TimeSpan.FromMilliseconds(350),"Reference debounce interval differs");
            window.Close();window=null;
            var saved=Call(Store(path),"Load")!;Require(Property<double?>(saved,"WindowWidth")==1000&&Property<double?>(saved,"WindowHeight")==660,"Close did not flush final dimensions");
            window=Create();Require(window.Width==Math.Clamp(1000,window.MinWidth,Math.Max(window.MinWidth,SystemParameters.WorkArea.Width))&&window.Height==Math.Clamp(660,window.MinHeight,Math.Max(window.MinHeight,SystemParameters.WorkArea.Height)),"Reopen did not restore dimensions");
            window.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));window.WindowState=WindowState.Maximized;Call(window,"SaveCurrentWindowSize");window.Close();window=null;
            window=Create();Require(window.WindowState==WindowState.Maximized,"Maximized state was not restored");
            window.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));window.WindowState=WindowState.Minimized;Call(window,"SaveCurrentWindowSize");
            Require(Property<bool>(Call(Store(path),"Load")!,"WindowMaximized"),"Minimizing discarded saved maximized state");
            window.WindowState=WindowState.Normal;
            var checkbox=Field<CheckBox>(window,"preserveWindowSize");checkbox.IsChecked=false;
            var disabled=File.ReadAllText(path);window.Width=1100;window.Height=700;Call(window,"SaveCurrentWindowSize");window.Close();window=null;
            Require(File.ReadAllText(path)==disabled,"Disabled setting continued recording sizes");
            window=Create();Require(window.Width==defaults.Width&&window.Height==defaults.Height&&window.WindowState==WindowState.Normal,"Disabled setting restored old geometry");
            Require(Field<CheckBox>(window,"preserveWindowSize").IsChecked==false,"Disabled preference not retained");
            var side=(Panel)Field<Button>(window,"settingsNavigation").Parent;
            Require(side.Children.IndexOf(Field<Button>(window,"settingsNavigation"))==side.Children.IndexOf(Field<Button>(window,"exportDiagnostics"))+1,"Settings is not immediately below Export diagnostics");
            Field<Button>(window,"settingsNavigation").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(Field<ScrollViewer>(window,"settingsScroll").Visibility==Visibility.Visible&&Field<FrameworkElement>(window,"sharedHeader").Visibility==Visibility.Collapsed,"Settings navigation/header differs");
            var root=(Grid)window.Content;
            IEnumerable<DependencyObject> Walk(DependencyObject node){yield return node;foreach(var child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>())foreach(var item in Walk(child))yield return item;}
            Require(!Walk(root).OfType<TextBlock>().Any(t=>t.Text=="EXPERIMENTAL EDIT"),"Preset badge is still in the UI");
            foreach(var size in new[]{new Size(1280,900),new Size(911,512),new Size(620,320)})
            {
                root.Width=size.Width;root.Height=size.Height;for(var i=0;i<4;i++){root.Measure(size);root.Arrange(new Rect(size));root.UpdateLayout();}
                var scroll=Field<ScrollViewer>(window,"settingsScroll");var toggle=Field<CheckBox>(window,"preserveWindowSize");var point=toggle.TranslatePoint(new Point(),scroll);
                Require(point.X>=0&&point.X+toggle.ActualWidth<=scroll.ViewportWidth+1&&scroll.ActualHeight>0,"Settings exceeds viewport");
                var label=Walk(root).OfType<TextBlock>().Single(t=>t.Name=="PreserveWindowSizeLabel");var labelPoint=label.TranslatePoint(new Point(),scroll);
                Require(point.X>=labelPoint.X+label.ActualWidth&&toggle.HorizontalAlignment==HorizontalAlignment.Right,"Toggle is not to the right of its label");
                toggle.ApplyTemplate();var thumb=(System.Windows.Shapes.Ellipse)toggle.Template.FindName("Thumb",toggle);
                Require(thumb.HorizontalAlignment==HorizontalAlignment.Left,"Off thumb is not on the left");
                toggle.IsChecked=true;Require(thumb.HorizontalAlignment==HorizontalAlignment.Right,"On thumb is not on the right");toggle.IsChecked=false;
            }
            var render=new Size(1280,900);root.Width=render.Width;root.Height=render.Height;root.Measure(render);root.Arrange(new Rect(render));root.UpdateLayout();
            var bitmap=new RenderTargetBitmap(1280,900,96,96,PixelFormats.Pbgra32);bitmap.Render(root);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var output=File.Create("analysis/private/settings-1.49.1-off.png"))encoder.Save(output);
            Field<CheckBox>(window,"preserveWindowSize").IsChecked=true;root.UpdateLayout();bitmap=new RenderTargetBitmap(1280,900,96,96,PixelFormats.Pbgra32);bitmap.Render(root);encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var output=File.Create("analysis/private/settings-1.49.1-on.png"))encoder.Save(output);
            window.Close();window=null;
            File.WriteAllText(path,"{\"SchemaVersion\":1,\"PreserveWindowSize\":true,\"WindowWidth\":1,\"WindowHeight\":1}");window=Create();Require(window.Width==window.MinWidth&&window.Height==window.MinHeight,"Restored sizes violate application minimums");window.Close();window=null;
            File.WriteAllText(path,"{\"SchemaVersion\":1,\"PreserveWindowSize\":true,\"WindowWidth\":5000,\"WindowHeight\":5000}");window=Create();Require(window.Width<=Math.Max(window.MinWidth,SystemParameters.WorkArea.Width)&&window.Height<=Math.Max(window.MinHeight,SystemParameters.WorkArea.Height),"Restored size exceeds available work area");window.Close();window=null;
            foreach(var invalid in new[]{"{","{\"SchemaVersion\":2}","{\"SchemaVersion\":1,\"WindowWidth\":-1,\"WindowHeight\":12}",new string('x',4097)})
            {File.WriteAllText(path,invalid);Require(Property<bool>(Call(Store(path),"Load")!,"PreserveWindowSize")&&Property<double?>(Call(Store(path),"Load")!,"WindowWidth") is null,"Malformed settings did not use safe defaults");}
            var blocked=Path.Combine(directory,"blocked");File.WriteAllText(blocked,"file");window=Create(file:Path.Combine(blocked,"settings.json"));Field<CheckBox>(window,"preserveWindowSize").IsChecked=false;
            Require(Field<TextBlock>(window,"windowSettingsMessage").Text.Contains("Could not save"),"Persistence failure not explained");
            Require(Field<F15ReadSession?>(window,"connectedSession") is null,"Settings initiated device work");window.Close();window=null;
            window=Create(preview:true);Require(!Field<CheckBox>(window,"preserveWindowSize").IsEnabled,"Preview can change user preferences");window.Close();window=null;
            Console.WriteLine("PASS: default-on preserve size, close/reopen dimensions, maximized/minimized handling, disabled default size and no geometry writes, window bounds, malformed-file fallback, failure feedback, Settings placement/layout and removed badge. Isolated settings files; no hardware or real profile writes.");
        }
        finally{window?.Close();Directory.Delete(directory,true);}
    }
}
