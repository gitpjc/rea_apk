using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using Iamp.Desktop;
using Iamp.Protocol;
using Iamp.Transport.Windows;

internal static class PresetEffectReportChecks
{
    public static void Run()
    {
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var window=new MainWindow();
        T Field<T>(string n)=>(T)typeof(MainWindow).GetField(n,flags)!.GetValue(window)!;
        object? Call(string n,params object[] a)=>typeof(MainWindow).GetMethod(n,flags)!.Invoke(window,a);
        void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        T Find<T>(DependencyObject parent,string name) where T:FrameworkElement
        {
            if(parent is T control&&control.Name==name)return control;
            foreach(var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
                if(Find<T>(child,name) is {} found)return found;
            return null!;
        }
        try
        {
            typeof(MainWindow).GetField("names",flags)!.SetValue(window,Array.Empty<PresetName>());
            typeof(MainWindow).GetField("presetNamesAreCached",flags)!.SetValue(window,false);
            Require(((string)Call("CreatePresetEffectReport")!).Contains("No preset names loaded"),"Empty report invents presets");
            var names=new[]{new PresetName(80,"Last patch"),new PresetName(5,"Bank one"),new PresetName(4,"Lead"),new PresetName(1,"Clean"),new PresetName(41,"Bank ten")};
            typeof(MainWindow).GetField("names",flags)!.SetValue(window,names);
            Call("Filter");Field<TextBox>("search").Text="Clean";
            var report=(string)Call("CreatePresetEffectReport")!;
            Require(report.Contains("  0A-0D")&&report.Contains("    - 0A Clean")&&report.Contains("    - 0D Lead")&&report.Contains("    - 19D Last patch"),"Report omits presets hidden by search");
            Require(report.IndexOf("  1A-1D",StringComparison.Ordinal)<report.IndexOf("  10A-10D",StringComparison.Ordinal),"Banks sorted alphabetically");
            foreach(var model in PresetEffectSearch.Catalog)Require(report.Contains("    - "+model.Name),"Report omits model "+model.Name);
            foreach(var name in new[]{"Dyna","Filter","OD","Amp","EQ","Mod","Delay","Reverb"})Require(report.Contains("  "+name+Environment.NewLine),"Missing category "+name);
            var dialog=(Window)Call("CreatePresetEffectDetailsDialog",report)!;
            try
            {
                var body=Find<RichTextBox>((DependencyObject)dialog.Content,"PresetEffectReportText");
                var text=new System.Windows.Documents.TextRange(body.Document.ContentStart,body.Document.ContentEnd).Text.TrimEnd();
                Require(body.IsReadOnly&&text==report&&body.VerticalScrollBarVisibility==ScrollBarVisibility.Auto,"Dialog report/scrolling differs");
                foreach(var paragraph in body.Document.Blocks.OfType<System.Windows.Documents.Paragraph>())
                {
                    var line=new System.Windows.Documents.TextRange(paragraph.ContentStart,paragraph.ContentEnd).Text.TrimEnd();
                    if(line is "Presets" or "Effects" || line=="  Dyna" || line=="  0A-0D")
                        Require(paragraph.FontWeight==FontWeights.Bold&&paragraph.FontSize>13,"Section header is not bold/larger");
                    if(line.StartsWith("    - "))Require(paragraph.FontWeight==FontWeights.Normal&&paragraph.FontSize==13,"Report item styled as header");
                }
                Require(Find<Button>((DependencyObject)dialog.Content,"CopyPresetEffectReport") is not null,"Copy icon missing");
                var feedback=Find<TextBlock>((DependencyObject)dialog.Content,"PresetEffectReportFeedback");
                string? copied=null;
                var copy=typeof(MainWindow).GetMethod("CopyPresetEffectReport",BindingFlags.Static|BindingFlags.NonPublic)!;
                var markdown=(string)typeof(MainWindow).GetMethod("CreatePresetEffectMarkdown",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,[report])!;
                copy.Invoke(null,[report,(Action<string>)(value=>copied=value),feedback]);Require(copied==markdown,"Copy truncated Markdown report");
                Require(markdown.Contains("## Presets")&&markdown.Contains("### 0A-0D")&&markdown.Contains("- 0A Clean")&&markdown.Contains("## Effects")&&markdown.Contains("### Dyna"),"Markdown headings/lists differ");
                foreach(var preset in names)Require(markdown.Contains($"- {preset.SlotLabel} {preset.Name}"),"Markdown omitted a preset");
                foreach(var model in PresetEffectSearch.Catalog)
                {
                    var escaped=(string)typeof(MainWindow).GetMethod("CreatePresetEffectMarkdown",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,["    - "+model.Name])!;
                    Require(markdown.Contains(escaped),"Markdown omitted model "+model.Name);
                }
                var special=(string)typeof(MainWindow).GetMethod("CreatePresetEffectMarkdown",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,["    - 0A [Clean] *Lead*"])!;
                Require(special=="- 0A \\[Clean\\] \\*Lead\\*","Markdown name escaping differs");
                copy.Invoke(null,[report,(Action<string>)(_=>throw new ExternalException()),feedback]);Require(feedback.Text.Contains("busy"),"Clipboard error not handled");
            }
            finally{dialog.Close();}
            var root=(Grid)window.Content;
            foreach(var size in new[]{new Size(1366,768),new Size(911,512),new Size(620,320),new Size(1920,1080)})
            {
                root.Width=size.Width;root.Height=size.Height;
                for(var i=0;i<3;i++){root.Measure(size);root.Arrange(new Rect(size));root.UpdateLayout();}
                var chooser=Field<ComboBox>("presets");var button=Field<Button>("presetEffectDetails");
                var a=chooser.TranslatePoint(new Point(),root);var b=button.TranslatePoint(new Point(),root);
                Require(b.X>=a.X+chooser.ActualWidth&&b.X+button.ActualWidth<=size.Width+1,"Details button is not beside preset dropdown or overflows");
                Require(Math.Abs(a.Y+chooser.ActualHeight/2-b.Y-button.ActualHeight/2)<1,"Details button is on another line");
            }
            Require(Field<CancellationTokenSource?>("operation") is null&&Field<F15ReadSession?>("connectedSession") is null,"Report started device work");
            Console.WriteLine("PASS: complete unfiltered report, numeric bank grouping, all available models/categories, scrollable dialog, full clipboard copy/busy handling and adjacent button at four viewport sizes. No device/clipboard mutations.");
        }
        finally{window.Close();}
    }
}
