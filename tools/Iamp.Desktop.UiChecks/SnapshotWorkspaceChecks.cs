using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Iamp.Desktop;
using Iamp.Protocol;
using Iamp.Transport.Windows;

internal static class SnapshotWorkspaceChecks
{
    public static void Run()
    {
        var previousContext=SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
        const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
        void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        var window=new MainWindow();
        object? Call(string n,params object?[] a)=>typeof(MainWindow).GetMethod(n,flags)!.Invoke(window,a);
        T Field<T>(string n)=>(T)typeof(MainWindow).GetField(n,flags)!.GetValue(window)!;
        void Set(string n,object? value)=>typeof(MainWindow).GetField(n,flags)!.SetValue(window,value);
        void Finish(Task task)
        {
            if(!task.IsCompleted){var dispatcher=Dispatcher.CurrentDispatcher;var frame=new DispatcherFrame();_ =task.ContinueWith(_=>dispatcher.BeginInvoke(()=>frame.Continue=false));Dispatcher.PushFrame(frame);}
            task.GetAwaiter().GetResult();
        }
        var directory=Path.GetFullPath("analysis/private");Directory.CreateDirectory(directory);
        var invalid=Path.Combine(directory,".snapshot-workspace-invalid.json");var exported=Path.Combine(directory,".snapshot-workspace-export.json");
        try
        {
            var archive=SnapshotArchiveCodec.Decode(File.ReadAllText("tests/fixtures/f15-snapshot-v1.example.json"));
            var editor=new CurrentPresetSnapshot(archive.Preset.WireIndex,archive.Preset.Name,Convert.ToHexString(archive.StatePayload),Convert.ToHexString(archive.DetailPayload),archive.Preset.Blocks);
            Set("currentSnapshot",editor);Set("snapshotFirmware",archive.Firmware);Set("snapshotTime",archive.CapturedAt);Set("snapshotAddress",(ulong)1);Set("editSnapshotValid",true);
            var names=new[]{new PresetName(editor.WireIndex,editor.Name)};Set("names",names);
            Field<TextBlock>("currentPreset").Text=editor.SlotLabel+" · "+editor.Name;
            Field<TextBox>("thresholdInput").Text="26";Call("UpdateThresholdState");
            Require(!Field<Button>("exportSnapshot").IsEnabled&&!Field<Button>("compareSnapshot").IsEnabled,"Empty viewer uses editor state implicitly");
            Require(!Field<Button>("captureSnapshot").IsEnabled,"Disconnected capture enabled");
            var open=(Task<bool>)Call("LoadSnapshotFromPathAsync","tests/fixtures/f15-snapshot-v1.example.json")!;Finish(open);Require(open.Result,"Valid archive failed");
            var viewed=Field<SnapshotArchive>("viewedSnapshot");
            void Preserved()
            {
                Require(ReferenceEquals(Field<CurrentPresetSnapshot>("currentSnapshot"),editor)&&ReferenceEquals(Field<IReadOnlyList<PresetName>>("names"),names),"Opening/comparing changed editor or names");
                Require(Field<TextBox>("thresholdInput").Text=="26"&&Field<ulong?>("snapshotAddress")==1&&Field<bool>("editSnapshotValid"),"Opening/comparing changed pending values or edit target");
                Require(!Field<bool>("localSnapshotSource"),"Viewer replaced the editor source");
            }
            Preserved();Require(Field<Button>("exportSnapshot").IsEnabled&&Field<Button>("compareSnapshot").IsEnabled,"Offline viewing/export/compare blocked");
            File.WriteAllText(invalid,"{}");var rejected=(Task<bool>)Call("LoadSnapshotFromPathAsync",invalid)!;Finish(rejected);
            Require(!rejected.Result&&ReferenceEquals(viewed,Field<SnapshotArchive>("viewedSnapshot")),"Malformed file replaced viewer");Preserved();
            Finish((Task)Call("ExportArchiveToPathAsync",viewed,exported)!);
            var roundtrip=SnapshotArchiveCodec.Decode(File.ReadAllText(exported));Require(roundtrip.CapturedAt==viewed.CapturedAt&&roundtrip.DetailPayload.SequenceEqual(viewed.DetailPayload)&&roundtrip.StatePayload.SequenceEqual(viewed.StatePayload),"Export drifted from fixed viewer");
            var compared=(Task)Call("CompareSnapshotFromPathAsync",exported)!;Finish(compared);Preserved();
            var different=editor with {WireIndex=1,Name="Different preset"};
            Set("observedAmplifierPreset",different);Call("SetViewedSnapshot",archive,null,(ulong)1);Set("editSnapshotValid",false);Call("UpdateSharedHeader");
            Require(Field<TextBlock>("snapshotContext").Text.Contains("has changed")&&ReferenceEquals(archive,Field<SnapshotArchive>("viewedSnapshot")),"Capture relabelled after amplifier changed");
            Set("editSnapshotValid",true);Call("SetViewedSnapshot",archive,"Example-snapshot.json",null);
            Require(Field<TextBlock>("snapshotContext").Text.Contains("File · Example-snapshot.json"),"File source missing");
            Require(Field<TextBlock>("amplifierHeading").Text.Contains("Disconnected")&&Field<TextBlock>("amplifierPreset").Text.StartsWith("Last read preset"),"Offline header claims live connection");
            var buttonRow=(Panel)Field<Button>("scan").Parent;
            Require(buttonRow.Children.IndexOf(Field<Button>("disconnect"))==buttonRow.Children.IndexOf(Field<Button>("scan"))+1&&buttonRow.Children.IndexOf(Field<Button>("read"))==buttonRow.Children.IndexOf(Field<Button>("disconnect"))+1,"Connection action order differs");
            Require(buttonRow is Grid&&buttonRow.Children.IndexOf(Field<Button>("cancel"))==buttonRow.Children.IndexOf(Field<Button>("read"))+1,"Cancel is not immediately after Refresh amplifier in a single row");
            var root=(Grid)window.Content;
            var progress=Field<ProgressBar>("connectionProgress");
            var progressNotice=Field<StackPanel>("connectionProgressNotice");
            Require(progressNotice.Visibility==Visibility.Collapsed&&!progress.IsIndeterminate,"Idle connection shows progress");
            Call("BeginConnectionScan");
            Require(progressNotice.Visibility==Visibility.Visible&&progress.IsIndeterminate&&Field<TextBlock>("connectionProgressLabel").Text=="Scanning for Bluetooth devices…","Scan does not immediately show progress");
            Call("Begin","Scanning Bluetooth…");Call("End");
            Require(progressNotice.Visibility==Visibility.Visible&&progress.IsIndeterminate,"Scan-to-connect transition hides progress");
            Call("BeginConnectionRead");
            Require(Field<TextBlock>("connectionProgressLabel").Text=="Connecting to amp…","Scanning message does not change when connecting");
            Call("End");Call("FinishConnectionScan");
            Require(progressNotice.Visibility==Visibility.Collapsed&&!progress.IsIndeterminate,"Scan and automatic connection retain progress after completion");
            foreach(var outcome in new[]{"No amplifier found","Scan cancelled","Scan failed"}){
                Call("BeginConnectionScan");Call("Begin","Scanning Bluetooth…");Field<TextBlock>("status").Text=outcome;Call("End");Call("FinishConnectionScan");
                Require(progressNotice.Visibility==Visibility.Collapsed&&!progress.IsIndeterminate,outcome+" retains progress");
            }
            Call("BeginConnectionRead");
            Require(progressNotice.Visibility==Visibility.Visible&&progress.IsIndeterminate,"Connection read has no animated progress");
            Require(Field<Button>("cancel").Visibility==Visibility.Visible,"Connection read cannot be cancelled");
            Field<TextBlock>("status").Text="Reading current preset…";
            Require(progressNotice.Visibility==Visibility.Visible,"Progress disappeared before amplifier finished loading");
            Call("End");
            Require(progressNotice.Visibility==Visibility.Collapsed&&!progress.IsIndeterminate,"Completed connection retains progress");
            Call("BeginConnectionRead");Field<TextBlock>("status").Text="Read cancelled";Call("End");
            Require(progressNotice.Visibility==Visibility.Collapsed&&!progress.IsIndeterminate,"Cancelled connection retains progress");
            Call("BeginConnectionRead");Field<TextBlock>("status").Text="Read failed";Call("End");
            Require(progressNotice.Visibility==Visibility.Collapsed&&!progress.IsIndeterminate,"Failed connection retains progress");
            Call("Begin","Unrelated operation");
            Require(progressNotice.Visibility==Visibility.Collapsed&&Field<Button>("cancel").Visibility==Visibility.Collapsed,"Effect or preset operation shows connection progress/Cancel");Call("End");
            var status=Field<TextBlock>("status");status.Text="Tuner stopped";
            Require(status.Parent is null,"Dynamic status line is still part of the UI");
            foreach(var pageName in new[]{"libraryScroll","informationScroll","mixerScroll","tunerScroll","drumScroll","historyScroll","snapshotsScroll","settingsScroll"})
            {
                var page=Field<ScrollViewer>(pageName);Finish((Task)Call("NavigateToPageAsync",page)!);
                Require(page.Visibility==Visibility.Visible&&Grid.GetRow(page)==1,"Navigation hides wrong page");
                var history=pageName is "historyScroll" or "settingsScroll";
                Require(Field<FrameworkElement>("sharedHeader").Visibility==(history?Visibility.Collapsed:Visibility.Visible),"Header visibility differs for "+pageName);
                foreach(var size in new[]{new Size(1280,900),new Size(911,512),new Size(620,320)})
                {
                    root.Width=size.Width;root.Height=size.Height;
                    for(var pass=0;pass<4;pass++){root.Measure(size);root.Arrange(new Rect(size));root.UpdateLayout();}
                    Require(page.ActualHeight>0,"Shared header consumes entire page");
                    if(history){Require(root.RowDefinitions[0].ActualHeight==0,"Hidden header leaves an empty gap on Version History");continue;}
                    Call("BeginConnectionRead");root.Measure(size);root.Arrange(new Rect(size));root.UpdateLayout();
                    var progressPoint=progress.TranslatePoint(new Point(),root);
                    Require(progress.ActualWidth>0&&progressPoint.X>=212&&progressPoint.X+progress.ActualWidth<=size.Width+1,"Connection progress overflows shared header");
                    var rowHeight=buttonRow.ActualHeight;
                    var cancelPoint=Field<Button>("cancel").TranslatePoint(new Point(),buttonRow);
                    var readPoint=Field<Button>("read").TranslatePoint(new Point(),buttonRow);
                    Require(Math.Abs(cancelPoint.Y-readPoint.Y)<0.1&&cancelPoint.X>=readPoint.X+Field<Button>("read").ActualWidth,"Cancel wraps below Refresh amplifier");
                    Call("End");root.Measure(size);root.Arrange(new Rect(size));root.UpdateLayout();
                    Require(Math.Abs(buttonRow.ActualHeight-rowHeight)<0.1,"Cancel changes connection row height");
                    var headerHeight=Field<FrameworkElement>("sharedHeader").ActualHeight;
                    Call("Begin","Applying effect parameter");root.Measure(size);root.Arrange(new Rect(size));root.UpdateLayout();
                    Require(Field<Button>("cancel").Visibility==Visibility.Collapsed&&Math.Abs(Field<FrameworkElement>("sharedHeader").ActualHeight-headerHeight)<0.1,"Effect edit shifts header or shows Cancel");Call("End");
                    Call("BeginConnectionScan");root.Measure(size);root.Arrange(new Rect(size));root.UpdateLayout();
                    var scanPoint=progress.TranslatePoint(new Point(),root);
                    Require(scanPoint.X>=212&&scanPoint.X+progress.ActualWidth<=size.Width+1,"Scanning message/progress overflows shared header");
                    var progressLabel=Field<TextBlock>("connectionProgressLabel");
                    var labelPoint=progressLabel.TranslatePoint(new Point(),root);
                    Require(progressNotice.Children.IndexOf(progressLabel)>progressNotice.Children.IndexOf(progress)&&labelPoint.X>=scanPoint.X+progress.ActualWidth,"Progress message is not on the right of the bar");
                    Require(progressLabel.TextWrapping==TextWrapping.NoWrap&&labelPoint.X+progressLabel.ActualWidth<=size.Width+1,"Progress text wraps or overflows the header");
                    Call("FinishConnectionScan");root.Measure(size);root.Arrange(new Rect(size));root.UpdateLayout();
                    Require(Math.Abs(Field<ComboBox>("devices").ActualHeight-Field<Button>("scan").ActualHeight)<0.1,"Amplifier dropdown height differs from adjacent buttons");
                    var selector=Field<ComboBox>("devices");var selectedText=(ContentPresenter)selector.Template.FindName("SelectedAmplifierPresenter",selector);
                    Require(selectedText.VerticalAlignment==VerticalAlignment.Center,"Dropdown text is not vertically centered");
                    foreach(var control in new FrameworkElement[]{Field<Button>("scan"),Field<Button>("disconnect"),Field<Button>("read"),Field<ComboBox>("devices")})
                    {
                        var point=control.TranslatePoint(new Point(),root);
                        var connectionScroll=(ScrollViewer)buttonRow.Parent;
                        Require(point.X>=212&&(point.X+control.ActualWidth<=size.Width+1||connectionScroll.ScrollableWidth>0),"Connection action is inaccessible at compact width");
                    }
                    var rowScroll=(ScrollViewer)buttonRow.Parent;rowScroll.ScrollToRightEnd();root.UpdateLayout();
                    var rightmost=Field<Button>("read").TranslatePoint(new Point(),root);
                    Require(rightmost.X+Field<Button>("read").ActualWidth<=size.Width+1,"Compact connection actions cannot be reached by scrolling");rowScroll.ScrollToLeftEnd();root.UpdateLayout();
                }
            }
            Require((string)Field<Button>("libraryNavigation").Content=="Presets","Presets navigation not renamed");
            var selectorWindow=new MainWindow(preview:true);
            try
            {
                var selector=(ComboBox)typeof(MainWindow).GetField("devices",flags)!.GetValue(selectorWindow)!;
                var device=new F15DiscoveredDevice(0xDA8D4137B21B,"F15iF15i",-44);
                selector.ItemsSource=new[]{device};selector.SelectedIndex=0;
                var selectorRoot=(FrameworkElement)selectorWindow.Content;
                for(var pass=0;pass<4;pass++){selectorRoot.Measure(new Size(1280,900));selectorRoot.Arrange(new Rect(0,0,1280,900));selectorRoot.UpdateLayout();}
                Dispatcher.CurrentDispatcher.Invoke(()=>{},DispatcherPriority.DataBind);
                var presenter=(ContentPresenter)selector.Template.FindName("SelectedAmplifierPresenter",selector);
                Require(presenter.Content is string selected&&selected==device.DisplayName&&!selected.Contains("F15DiscoveredDevice"),"Selected amplifier renders internal record text instead of name/address/signal");
                Require(selector.DisplayMemberPath=="DisplayName","Device menu label differs from selected label");
            }
            finally{selectorWindow.Close();}
            var renderSize=new Size(1280,900);root.Width=renderSize.Width;root.Height=renderSize.Height;
            foreach(var (name,path) in new[]{("snapshotsScroll","snapshot-workspace-1.48.0.png"),("libraryScroll","shared-header-presets-1.48.0.png")})
            {
                Finish((Task)Call("NavigateToPageAsync",Field<ScrollViewer>(name))!);for(var pass=0;pass<4;pass++){root.Measure(renderSize);root.Arrange(new Rect(renderSize));root.UpdateLayout();}
                var bitmap=new RenderTargetBitmap(1280,900,96,96,PixelFormats.Pbgra32);bitmap.Render(root);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var stream=File.Create(Path.Combine(directory,path));encoder.Save(stream);
            }
            Call("BeginConnectionRead");root.Measure(renderSize);root.Arrange(new Rect(renderSize));root.UpdateLayout();
            var connectingBitmap=new RenderTargetBitmap(1280,900,96,96,PixelFormats.Pbgra32);connectingBitmap.Render(root);
            var connectingEncoder=new PngBitmapEncoder();connectingEncoder.Frames.Add(BitmapFrame.Create(connectingBitmap));
            using(var output=File.Create(Path.Combine(directory,"connection-progress-1.50.10.png")))connectingEncoder.Save(output);
            Call("End");
            Console.WriteLine("PASS: connection progress lifecycle/cleanup and header visibility/action order on all eight pages at three sizes; Presets rename; isolated offline snapshot open/export/compare; malformed-file preservation; fixed capture change notice; editor, target and pending inputs preserved. No device commands.");
        }
        finally{File.Delete(invalid);File.Delete(exported);window.Close();SynchronizationContext.SetSynchronizationContext(previousContext);}
    }
}
