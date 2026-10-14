using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Iamp.Desktop;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        var app = new Application();
        if(args.Length==3 && args[0]=="--save-preview") {
            var savePreview=new MainWindow();
            try {savePreview.PrepareSavePreview(args[1]);RenderPreview(savePreview,args[2]);savePreview.Close();app.Shutdown();return 0;}
            catch(Exception ex) {Console.Error.WriteLine(ex);savePreview.Close();app.Shutdown();return 2;}
        }
        if(args.Length==3 && args[0]=="--save-roundtrip-preview") {
            app.ShutdownMode=ShutdownMode.OnExplicitShutdown;
            var saveWindow=new MainWindow();
            _=app.Dispatcher.InvokeAsync(async()=> {
                var exitCode=0;
                try {await saveWindow.RunSaveRoundtripPreviewAsync(args[1],args[2]);}
                catch(Exception ex) {Console.Error.WriteLine(ex);exitCode=2;}
                try {RenderPreview(saveWindow,args[2]);}
                catch(Exception ex) {Console.Error.WriteLine(ex);exitCode=2;}
                finally {saveWindow.Close();app.Shutdown(exitCode);}
            });
            return app.Run();
        }
        if(args.Length==2 && args[0]=="--version-history-preview") {
            var historyWindow=new MainWindow(preview:true);
            try {
                var details=historyWindow.PrepareVersionHistoryPreview();
                RenderPreview(historyWindow,args[1]);
                RenderPreview(details,Path.ChangeExtension(args[1],"details.png"),width:660,height:400);
                details.Close();historyWindow.Close();app.Shutdown();return 0;
            }
            catch(Exception ex) {Console.Error.WriteLine(ex);historyWindow.Close();app.Shutdown();return 2;}
        }
        if (args.Length == 3 && args[0] == "--snapshot-preview")
        {
            app.ShutdownMode=ShutdownMode.OnExplicitShutdown;
            var localWindow=new MainWindow();
            _=app.Dispatcher.InvokeAsync(async()=>
            {
                var exitCode=2;
                try {
                    var loaded=await localWindow.LoadSnapshotPreviewAsync(args[1],args[2]);
                    RenderPreview(localWindow,args[2]);exitCode=loaded?0:2;
                }
                catch(Exception ex) {Console.Error.WriteLine(ex);}
                finally {localWindow.Close();app.Shutdown(exitCode);}
            });
            return app.Run();
        }
        if (args.Length == 3 && args[0] == "--hardware-roundtrip-preview")
        {
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var testWindow = new MainWindow();
            _ = app.Dispatcher.InvokeAsync(async () =>
            {
                var exitCode=0;
                try {await testWindow.RunHardwareRoundtripPreviewAsync(args[1],args[2]);}
                catch(Exception ex) {Console.Error.WriteLine(ex);exitCode=2;}
                try {RenderPreview(testWindow,args[2]);}
                catch(Exception ex) {Console.Error.WriteLine(ex);exitCode=2;}
                finally {testWindow.Close();app.Shutdown(exitCode);}
            });
            return app.Run();
        }
        if (args.Length == 3 && args[0] == "--read-preview")
        {
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var liveWindow = new MainWindow();
            _ = app.Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    var success = await liveWindow.ReadSnapshotPreviewAsync(args[1]);
                    RenderPreview(liveWindow, args[2]);
                    liveWindow.Close(); app.Shutdown(success ? 0 : 2);
                }
                catch { liveWindow.Close(); app.Shutdown(2); }
            });
            return app.Run();
        }
        if (args.Length == 2 && args[0] is "--preview" or "--presets-preview")
        {
            var window = new MainWindow(preview: true) { Width = 1280, Height = 900 };
            if(args[0]=="--presets-preview")window.PreparePresetListPreview();
            RenderPreview(window, args[1],args[0]=="--presets-preview");
            window.Close(); app.Shutdown(); return 0;
        }
        app.Run(new MainWindow());
        return 0;
    }

    private static void RenderPreview(Window window, string outputPath, bool showPresets=false,int width=1280,int height=900)
    {
        var content = (FrameworkElement)window.Content;
        if (content is System.Windows.Controls.Panel panel) panel.Background = window.Background;
        content.Width = width; content.Height = height;
        content.Measure(new Size(width, height)); content.Arrange(new Rect(0, 0, width, height)); content.UpdateLayout();
        if(showPresets && content is System.Windows.Controls.Panel root) {
            var scroll=root.Children.OfType<System.Windows.Controls.ScrollViewer>().Single(s=>s.Visibility==Visibility.Visible);
            scroll.ScrollToBottom();content.UpdateLayout();
        }
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(content);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        var path = Path.GetFullPath(outputPath); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var output = File.Create(path); encoder.Save(output);
    }
}
