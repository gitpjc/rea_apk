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
        if (args.Length == 2 && args[0] == "--preview")
        {
            var window = new MainWindow(preview: true) { Width = 1280, Height = 900 };
            RenderPreview(window, args[1]);
            window.Close(); app.Shutdown(); return 0;
        }
        app.Run(new MainWindow());
        return 0;
    }

    private static void RenderPreview(MainWindow window, string outputPath)
    {
        var content = (FrameworkElement)window.Content;
        if (content is System.Windows.Controls.Panel panel) panel.Background = window.Background;
        content.Width = 1280; content.Height = 900;
        content.Measure(new Size(1280, 900)); content.Arrange(new Rect(0, 0, 1280, 900)); content.UpdateLayout();
        var bitmap = new RenderTargetBitmap(1280, 900, 96, 96, PixelFormats.Pbgra32); bitmap.Render(content);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        var path = Path.GetFullPath(outputPath); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var output = File.Create(path); encoder.Save(output);
    }
}
