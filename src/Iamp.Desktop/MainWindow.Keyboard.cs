using System.Windows;
using System.Windows.Input;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    private readonly RoutedCommand libraryShortcut=new(),historyShortcut=new(),searchShortcut=new(),mixerShortcut=new();
    private void InitializeKeyboardNavigation()
    {
        void Bind(RoutedCommand command, Key key, Action action)
        {
            InputBindings.Add(new KeyBinding(command,new KeyGesture(key,ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(command,(_,e)=>{action();e.Handled=true;},(_,e)=>{e.CanExecute=true;e.Handled=true;}));
        }
        Bind(libraryShortcut,Key.D1,()=>NavigateToHistory(false));
        Bind(historyShortcut,Key.D2,()=>NavigateToHistory(true));
        Bind(mixerShortcut,Key.D3,()=>NavigateToPage(mixerScroll));
        Bind(searchShortcut,Key.F,()=>{NavigateToHistory(false);search.BringIntoView();search.Focus();search.SelectAll();});
        libraryNavigation.ToolTip="Amplifier Settings · Ctrl+1";historyNavigation.ToolTip="Version History · Ctrl+2";
        search.ToolTip="Search preset names or indices · Ctrl+F";
        System.Windows.Automation.AutomationProperties.SetName(search,"Search preset names or indices");
        System.Windows.Automation.AutomationProperties.SetName(devices,"Discovered amplifiers");
        System.Windows.Automation.AutomationProperties.SetName(address,"Optional manual Bluetooth address");
        System.Windows.Automation.AutomationProperties.SetName(presets,"Preset browser");
    }

    internal void PrepareResponsivePreview(int width,int height)
    {
        var pending=thresholdInput.Text;var original=currentSnapshot;
        historyShortcut.Execute(null,this);
        if(historyScroll.Visibility!=Visibility.Visible)throw new InvalidOperationException("History shortcut failed.");
        libraryShortcut.Execute(null,this);
        searchShortcut.Execute(null,this);
        if(libraryScroll.Visibility!=Visibility.Visible || thresholdInput.Text!=pending || currentSnapshot!=original)
            throw new InvalidOperationException("Keyboard navigation changed the snapshot or pending input.");
        var root=(FrameworkElement)Content;root.Width=width;root.Height=height;
        for(var pass=0;pass<3;pass++) {root.Measure(new Size(width,height));root.Arrange(new Rect(0,0,width,height));root.UpdateLayout();}
        libraryScroll.ScrollToTop();root.UpdateLayout();
        if(devices.ActualWidth>width-212-64-36 || search.ActualWidth>width-212-64)
            throw new InvalidOperationException("Inputs exceed compact page bounds.");
        foreach(var control in new FrameworkElement[]{devices,read,scan,openSnapshot,compareSnapshot,search,presets}) {
            var left=control.TranslatePoint(new Point(0,0),root).X;
            if(left<212 || left+control.ActualWidth>width+1)throw new InvalidOperationException("Connection action extends outside the window.");
        }
        Console.WriteLine($"Responsive/keyboard QA passed at {width}x{height} logical pixels; snapshot and pending input preserved.");
    }
}
