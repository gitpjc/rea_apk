using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Iamp.Protocol;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    private readonly Button presetEffectDetails=Button("Preset/Effect Details");

    private string CreatePresetEffectReport()
    {
        var report=new StringBuilder();report.AppendLine("Presets");
        if(names.Count==0)report.AppendLine("  No preset names loaded. Connect the amplifier to load its presets.");
        else if(presetNamesAreCached)report.AppendLine("  Last-read preset names (offline cache). Connect to refresh.");
        foreach(var bank in names.OrderBy(p=>p.WireIndex).GroupBy(p=>PresetAddress.FromWireIndex(p.WireIndex).Bank))
        {
            report.AppendLine();report.AppendLine($"  {bank.Key}A-{bank.Key}D");
            foreach(var preset in bank)report.AppendLine($"    - {preset.SlotLabel} {preset.Name}");
        }
        report.AppendLine();report.AppendLine("Effects");
        foreach(var category in ChainCategories.Where(c=>c.TypeCode.HasValue))
        {
            var models=PresetEffectSearch.Catalog.Where(e=>e.TypeCode==category.TypeCode).ToArray();
            if(models.Length==0)continue;
            report.AppendLine();report.AppendLine("  "+category.Name);
            foreach(var model in models)report.AppendLine("    - "+model.Name);
        }
        return report.ToString().TrimEnd();
    }

    private Window CreatePresetEffectDetailsDialog(string report)
    {
        var dialog=new Window {Title="Preset/Effect Details",Width=760,Height=650,MinWidth=440,MinHeight=300,
            Background=PanelBrush,Foreground=Brushes.White,WindowStartupLocation=WindowStartupLocation.CenterOwner};
        var root=new DockPanel {Margin=new Thickness(20)};
        var header=new StackPanel();header.Children.Add(Text("Preset/Effect Details",24));
        header.Children.Add(Text("All loaded preset names and available built-in effect models. Preset search and effect filters do not limit this report.",13,MutedBrush,new Thickness(0,8,0,14)));
        DockPanel.SetDock(header,Dock.Top);root.Children.Add(header);
        var footer=new StackPanel {Margin=new Thickness(0,14,0,0)};
        var actions=new StackPanel {Orientation=Orientation.Horizontal};
        var feedback=Text("",12,MutedBrush,new Thickness(0,8,0,0));feedback.Name="PresetEffectReportFeedback";
        var copy=Button("");copy.Name="CopyPresetEffectReport";copy.ToolTip="Copy the complete preset/effect report as Markdown";
        var content=new StackPanel {Orientation=Orientation.Horizontal};
        var icon=new Grid {Width=18,Height=18,Margin=new Thickness(0,0,8,0)};
        icon.Children.Add(new Border {BorderBrush=Brushes.White,BorderThickness=new Thickness(1.5),Margin=new Thickness(0,0,5,5)});
        icon.Children.Add(new Border {BorderBrush=Brushes.White,BorderThickness=new Thickness(1.5),Background=PanelBrush,Margin=new Thickness(5,5,0,0)});
        content.Children.Add(icon);content.Children.Add(Text("Copy report",13));copy.Content=content;
        AutomationProperties.SetName(copy,"Copy complete preset/effect report as Markdown to clipboard");
        copy.Click+=(_,_)=>CopyPresetEffectReport(report,Clipboard.SetText,feedback);
        var close=Button("Close");close.IsCancel=true;close.Margin=new Thickness(10,0,0,0);close.Click+=(_,_)=>dialog.Close();
        actions.Children.Add(copy);actions.Children.Add(close);footer.Children.Add(actions);footer.Children.Add(feedback);
        DockPanel.SetDock(footer,Dock.Bottom);root.Children.Add(footer);
        var document=new FlowDocument {PagePadding=new Thickness(0),FontFamily=new FontFamily("Consolas"),FontSize=13};
        var sectionHeaders=ChainCategories.Select(c=>"  "+c.Name)
            .Concat(Enumerable.Range(0,20).Select(bank=>$"  {bank}A-{bank}D")).ToHashSet(StringComparer.Ordinal);
        foreach(var line in report.Replace("\r\n","\n").Split('\n'))
        {
            var major=line is "Presets" or "Effects";var heading=major||sectionHeaders.Contains(line);
            document.Blocks.Add(new Paragraph(new Run(line)) {Margin=new Thickness(0),
                FontSize=major?16:heading?15:13,FontWeight=heading?FontWeights.Bold:FontWeights.Normal});
        }
        var body=new RichTextBox {Name="PresetEffectReportText",Document=document,IsReadOnly=true,
            Background=BackgroundBrush,Foreground=Brushes.White,BorderBrush=Brush("#46505E"),Padding=new Thickness(12),
            FontFamily=new FontFamily("Consolas"),FontSize=13,
            VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto};
        AutomationProperties.SetName(body,"Preset and effect report");root.Children.Add(body);dialog.Content=root;
        return dialog;
    }

    private static void CopyPresetEffectReport(string report,Action<string> writeClipboard,TextBlock feedback)
    {
        try {writeClipboard(CreatePresetEffectMarkdown(report));feedback.Text="Report copied as Markdown.";}
        catch(ExternalException){feedback.Text="Clipboard is busy. Try copying again.";}
    }

    private static string CreatePresetEffectMarkdown(string report)
    {
        static string Escape(string text)
        {
            var escaped=new StringBuilder();
            foreach(var character in text)
            {
                if("\\`*_{}[]<>#|~".Contains(character))escaped.Append('\\');
                escaped.Append(character);
            }
            return escaped.ToString();
        }
        var headers=ChainCategories.Select(c=>"  "+c.Name)
            .Concat(Enumerable.Range(0,20).Select(bank=>$"  {bank}A-{bank}D")).ToHashSet(StringComparer.Ordinal);
        var markdown=new StringBuilder();
        foreach(var line in report.Replace("\r\n","\n").Split('\n'))
        {
            if(line is "Presets" or "Effects")markdown.Append("## ").AppendLine(line).AppendLine();
            else if(headers.Contains(line))markdown.Append("### ").AppendLine(Escape(line.Trim())).AppendLine();
            else if(line.StartsWith("    - ",StringComparison.Ordinal))markdown.Append("- ").AppendLine(Escape(line[6..]));
            else markdown.AppendLine(Escape(line.Trim()));
        }
        return markdown.ToString().TrimEnd();
    }
}
