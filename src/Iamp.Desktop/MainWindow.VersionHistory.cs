using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    private readonly Button libraryNavigation=Button("Amplifier library"),historyNavigation=Button("Version History");
    private readonly ScrollViewer historyScroll=new() {VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Visibility=Visibility.Collapsed};
    private IReadOnlyList<VersionHistoryEntry> versionEntries=Array.Empty<VersionHistoryEntry>();

    private void ConfigureNavigation()
    {
        foreach(var button in new[]{libraryNavigation,historyNavigation}) {
            button.HorizontalContentAlignment=HorizontalAlignment.Left;
            button.HorizontalAlignment=HorizontalAlignment.Stretch;
            button.Padding=new Thickness(14,12,14,12);button.Margin=new Thickness(0,0,0,5);
        }
        libraryNavigation.Click+=(_,_)=>NavigateToHistory(false);
        historyNavigation.Click+=(_,_)=>NavigateToHistory(true);
        SetNavigationColors(false);
    }

    private void SetNavigationColors(bool history)
    {
        libraryNavigation.Background=history?System.Windows.Media.Brushes.Transparent:Brush("#203B5C");
        libraryNavigation.Foreground=history?MutedBrush:BlueBrush;
        historyNavigation.Background=history?Brush("#203B5C"):System.Windows.Media.Brushes.Transparent;
        historyNavigation.Foreground=history?BlueBrush:MutedBrush;
    }

    private void NavigateToHistory(bool history)
    {
        if(history && historyScroll.Content is null)BuildVersionHistory();
        libraryScroll.Visibility=history?Visibility.Collapsed:Visibility.Visible;
        historyScroll.Visibility=history?Visibility.Visible:Visibility.Collapsed;
        SetNavigationColors(history);
        Title=history?"iAMP Desktop · Version History":"iAMP Desktop · Amplifier library";
    }

    private void BuildVersionHistory()
    {
        var page=new StackPanel {Margin=new Thickness(32,20,32,24)};
        page.Children.Add(Text("Version History",30));
        page.Children.Add(Text($"Current version · {DesktopVersion.Current}",14,MutedBrush,new Thickness(0,7,0,24)));
        historyScroll.Content=page;
        try {
            versionEntries=VersionHistoryStore.Load();
            foreach(var entry in versionEntries) {
                var content=new StackPanel();
                content.Children.Add(Text($"Version {entry.Version}",20));
                content.Children.Add(Text(entry.ChangedAt.ToLocalTime().ToString("dd MMM yyyy · HH:mm",CultureInfo.CurrentCulture),12,MutedBrush,new Thickness(0,6,0,0)));
                content.Children.Add(Text(entry.Summary,14,null,new Thickness(0,12,0,0)));
                content.Children.Add(Text("View details",12,BlueBrush,new Thickness(0,14,0,0)));
                var card=Button("");card.Content=content;card.Background=PanelBrush;
                card.HorizontalContentAlignment=HorizontalAlignment.Stretch;
                card.Padding=new Thickness(20);card.Margin=new Thickness(0,0,0,12);
                System.Windows.Automation.AutomationProperties.SetName(card,$"Version {entry.Version}. {entry.Summary}. View details.");
                card.Click+=(_,_)=>CreateVersionDetailsWindow(entry).ShowDialog();
                page.Children.Add(card);
            }
        }
        catch(Exception ex) when(ex is System.IO.IOException or UnauthorizedAccessException or System.Text.Json.JsonException or FormatException) {
            page.Children.Add(Text("Unable to load version history. Check that assets/version_history.json is included with the application.",14,MutedBrush));
        }
    }

    private Window CreateVersionDetailsWindow(VersionHistoryEntry entry)
    {
        var dialog=new Window {Title=$"Version {entry.Version}",Owner=IsVisible?this:null,Width=660,Height=400,MinWidth=420,MinHeight=280,
            WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=BackgroundBrush,Foreground=System.Windows.Media.Brushes.White,
            FontFamily=FontFamily,FontSize=14};
        var grid=new Grid();
        grid.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});
        grid.RowDefinitions.Add(new RowDefinition());grid.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});
        grid.Children.Add(Text($"Version {entry.Version}",24,null,new Thickness(0,0,0,18)));
        var body=new StackPanel();body.Children.Add(Text(entry.Summary,17));
        body.Children.Add(Text(entry.Details,14,MutedBrush,new Thickness(0,14,0,0)));
        var scroll=new ScrollViewer {Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};Grid.SetRow(scroll,1);grid.Children.Add(scroll);
        var close=Button("Close");close.IsCancel=true;close.IsDefault=true;close.HorizontalAlignment=HorizontalAlignment.Right;close.Margin=new Thickness(0,20,0,0);
        close.Click+=(_,_)=>dialog.Close();Grid.SetRow(close,2);grid.Children.Add(close);
        dialog.Content=new Border {Background=BackgroundBrush,Padding=new Thickness(24),Child=grid};
        return dialog;
    }

    internal Window PrepareVersionHistoryPreview()
    {
        if(!preview)throw new InvalidOperationException("Version history layout checks require offline demo mode.");
        thresholdInput.Text="26";presets.SelectedIndex=1;
        var root=(FrameworkElement)Content;root.Width=1280;root.Height=900;
        root.Measure(new Size(1280,900));root.Arrange(new Rect(0,0,1280,900));root.UpdateLayout();
        libraryScroll.ScrollToBottom();root.UpdateLayout();var originalOffset=libraryScroll.VerticalOffset;
        var original=currentSnapshot;var originalPreset=currentPreset.Text;var originalInput=thresholdInput.Text;
        var originalSelection=presets.SelectedItem;
        historyNavigation.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
        root.UpdateLayout();
        if(versionEntries.Count==0 || historyScroll.Visibility!=Visibility.Visible)throw new InvalidOperationException("Version history did not load.");
        libraryNavigation.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
        root.UpdateLayout();
        if(libraryScroll.Visibility!=Visibility.Visible || currentSnapshot!=original || currentPreset.Text!=originalPreset || thresholdInput.Text!=originalInput ||
            presets.SelectedItem!=originalSelection || Math.Abs(libraryScroll.VerticalOffset-originalOffset)>0.1)
            throw new InvalidOperationException("Navigation changed the amplifier snapshot or pending input.");
        historyNavigation.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
        Console.WriteLine($"Version History QA passed: {DesktopVersion.Current}, {versionEntries.Count} entries; library state preserved.");
        return CreateVersionDetailsWindow(versionEntries[0]);
    }
}
