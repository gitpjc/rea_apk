using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Threading;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    private readonly Button settingsNavigation=Button("Settings");
    private readonly ScrollViewer settingsScroll=new(){VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Visibility=Visibility.Collapsed};
    private readonly CheckBox preserveWindowSize=new(){Name="PreserveWindowSizeToggle",HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Center,IsThreeState=false};
    private readonly TextBlock windowSettingsMessage=Text("",12,MutedBrush,new Thickness(0,12,0,0));
    private readonly DesktopSettingsStore desktopSettingsStore;
    private DesktopSettings desktopSettings=DesktopSettings.Default;
    private readonly DispatcherTimer windowSizeSaveTimer=new(){Interval=TimeSpan.FromMilliseconds(350)};
    private bool windowSessionStarted;
    private bool lastNonMinimizedMaximized;

    private void BuildSettingsPage()
    {
        var page=new StackPanel{Margin=new Thickness(32,20,32,24)};page.Children.Add(Text("Settings",30));
        page.Children.Add(Text("Preferences for iAMP Desktop on this computer.",14,MutedBrush,new Thickness(0,7,0,20)));
        preserveWindowSize.Template=(ControlTemplate)XamlReader.Parse("""
            <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="CheckBox">
              <Border x:Name="FocusRing" Width="64" Height="48" Background="Transparent" BorderBrush="Transparent" BorderThickness="2" CornerRadius="24">
                <Grid Width="52" Height="32">
                  <Border x:Name="Track" Background="#35414B" BorderBrush="#8995A3" BorderThickness="2" CornerRadius="16"/>
                  <Ellipse x:Name="Thumb" Width="16" Height="16" Fill="#8995A3" HorizontalAlignment="Left" VerticalAlignment="Center" Margin="8,0,0,0"/>
                </Grid>
              </Border>
              <ControlTemplate.Triggers>
                <Trigger Property="IsChecked" Value="True">
                  <Setter TargetName="Track" Property="Background" Value="#64B5F6"/><Setter TargetName="Track" Property="BorderBrush" Value="#64B5F6"/>
                  <Setter TargetName="Thumb" Property="Fill" Value="#062B45"/><Setter TargetName="Thumb" Property="Width" Value="24"/><Setter TargetName="Thumb" Property="Height" Value="24"/>
                  <Setter TargetName="Thumb" Property="HorizontalAlignment" Value="Right"/><Setter TargetName="Thumb" Property="Margin" Value="0,0,4,0"/>
                </Trigger>
                <Trigger Property="IsMouseOver" Value="True"><Setter TargetName="Track" Property="Opacity" Value="0.85"/></Trigger>
                <Trigger Property="IsKeyboardFocused" Value="True"><Setter TargetName="FocusRing" Property="BorderBrush" Value="#64B5F6"/></Trigger>
                <Trigger Property="IsEnabled" Value="False"><Setter TargetName="FocusRing" Property="Opacity" Value="0.4"/></Trigger>
              </ControlTemplate.Triggers>
            </ControlTemplate>
            """);
        var card=new StackPanel();var row=new Grid();row.ColumnDefinitions.Add(new());row.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
        var labels=new StackPanel {Margin=new Thickness(0,0,16,0),VerticalAlignment=VerticalAlignment.Center};
        var label=Text("Preserve window size",16);label.Name="PreserveWindowSizeLabel";label.FontWeight=FontWeights.SemiBold;labels.Children.Add(label);
        labels.Children.Add(Text("Reopen at the last saved window size, including maximized windows.",13,MutedBrush,new Thickness(0,6,0,0)));
        row.Children.Add(labels);Grid.SetColumn(preserveWindowSize,1);row.Children.Add(preserveWindowSize);card.Children.Add(row);
        card.Children.Add(Text("Turn this off to use the default window size next time. Your current window is not resized.",12,MutedBrush,new Thickness(0,12,0,0)));
        card.Children.Add(windowSettingsMessage);
        page.Children.Add(new Border{Background=PanelBrush,CornerRadius=new CornerRadius(8),Padding=new Thickness(18),Child=card});settingsScroll.Content=page;
        System.Windows.Automation.AutomationProperties.SetName(preserveWindowSize,"Preserve window size");
    }
    private void InitializeWindowSettings()
    {
        desktopSettings=desktopSettingsStore.Load();preserveWindowSize.IsChecked=desktopSettings.PreserveWindowSize;preserveWindowSize.IsEnabled=!preview;
        if(!preview&&desktopSettings.PreserveWindowSize)RestoreSavedWindowSize();
        lastNonMinimizedMaximized=WindowState==WindowState.Maximized;
        preserveWindowSize.Checked+=(_,_)=>ChangePreserveWindowSize(true);
        preserveWindowSize.Unchecked+=(_,_)=>ChangePreserveWindowSize(false);
        windowSizeSaveTimer.Tick+=(_,_)=>{windowSizeSaveTimer.Stop();SaveCurrentWindowSize();};
        Loaded+=(_,_)=>{if(!preview){windowSessionStarted=true;SaveCurrentWindowSize();}};
        SizeChanged+=(_,_)=>QueueWindowSizeSave();StateChanged+=(_,_)=>{if(WindowState!=WindowState.Minimized)lastNonMinimizedMaximized=WindowState==WindowState.Maximized;QueueWindowSizeSave();};
        Closed+=(_,_)=>{windowSizeSaveTimer.Stop();if(windowSessionStarted)SaveCurrentWindowSize();};
    }
    private void RestoreSavedWindowSize()
    {
        if(desktopSettings.WindowWidth is {} width&&desktopSettings.WindowHeight is {} height)
        {Width=Math.Clamp(width,MinWidth,Math.Max(MinWidth,SystemParameters.WorkArea.Width));Height=Math.Clamp(height,MinHeight,Math.Max(MinHeight,SystemParameters.WorkArea.Height));}
        if(desktopSettings.WindowMaximized)WindowState=WindowState.Maximized;
    }
    private void ChangePreserveWindowSize(bool enabled)
    {
        if(preview)return;windowSizeSaveTimer.Stop();desktopSettings=desktopSettings with{PreserveWindowSize=enabled};
        if(enabled&&windowSessionStarted)SaveCurrentWindowSize();else SaveDesktopSettings();
    }
    private void QueueWindowSizeSave()
    {
        if(preview||!windowSessionStarted||!desktopSettings.PreserveWindowSize||WindowState==WindowState.Minimized)return;
        windowSizeSaveTimer.Stop();windowSizeSaveTimer.Start();
    }
    private void SaveCurrentWindowSize()
    {
        if(preview||!windowSessionStarted||!desktopSettings.PreserveWindowSize)return;
        var bounds=RestoreBounds;
        var width=WindowState==WindowState.Normal||bounds.IsEmpty?Width:bounds.Width;
        var height=WindowState==WindowState.Normal||bounds.IsEmpty?Height:bounds.Height;
        desktopSettings=desktopSettings with{WindowWidth=Math.Clamp(width,MinWidth,5000),WindowHeight=Math.Clamp(height,MinHeight,5000),WindowMaximized=WindowState==WindowState.Minimized?lastNonMinimizedMaximized:WindowState==WindowState.Maximized};
        SaveDesktopSettings();
    }
    private void SaveDesktopSettings()=>windowSettingsMessage.Text=desktopSettingsStore.Save(desktopSettings)?"Saved automatically on this computer.":"Could not save settings. This change applies to this session only.";
}
