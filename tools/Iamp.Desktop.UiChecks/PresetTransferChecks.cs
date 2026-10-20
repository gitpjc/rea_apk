using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Iamp.Desktop;
using Iamp.Protocol;
using Iamp.Transport.Windows;

internal static class PresetTransferChecks
{
    public static void Run()
    {
        const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
        const BindingFlags statics=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Static;
        var codec=typeof(MainWindow).Assembly.GetType("Iamp.Desktop.PresetJsonDocument")!;
        object? Encode(string n,params object?[] args)=>codec.GetMethod(n,statics)!.Invoke(null,args);
        void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        void Reject(string json){try{Encode("ParseImport",json);throw new InvalidOperationException("Invalid export was accepted.");}catch(TargetInvocationException e) when(e.InnerException is FormatException or NotSupportedException or ArgumentException){}}
        using var fixture=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"fixtures","f15-preset-save-observations.json")));
        var state=fixture.RootElement.GetProperty("baselineStateHex").GetString()!;var hex=fixture.RootElement.GetProperty("baselineDetailHex").GetString()!;
        var bytes=Convert.FromHexString(hex);var decoded=PresetTransferRecord.Validate("V1.1.9",bytes);
        var snapshot=new CurrentPresetSnapshot(decoded.WireIndex,decoded.Name,state,hex,decoded.Blocks);
        var json=(string)Encode("Create",snapshot,"V1.1.9",DateTimeOffset.Now,"amplifierSnapshot",true,false)!;
        Require(((byte[])Encode("ParseImport",json)!).SequenceEqual(bytes),"Export/import lost complete preset data.");
        foreach(var mutation in new Action<JsonObject>[] {
            root=>root["schemaVersion"]="3.0.0",root=>root["restore"]!["firmware"]="V1.1.2",
            root=>root["restore"]!["detailSha256"]=new string('0',64),root=>root["restore"]!["deviceFamily"]="Other",
            root=>root["preset"]!["name"]="Misleading",root=>root["preset"]!["wireIndex"]=80,
            root=>root["effects"]![0]!["enabled"]=false,root=>root["restore"]=null,
            root=>root["restore"]!["detailPayloadHex"]="22"
        }){var changed=JsonNode.Parse(json)!.AsObject();mutation(changed);Reject(changed.ToJsonString());}
        Reject(json.Replace("\"schemaVersion\": \"4.0.0\"","\"schemaVersion\": \"4.0.0\",\"schemaVersion\": \"4.0.0\""));Reject(new string(' ',1024*1024+1));
        var snapshotJson="{\"format\":\"iamp-desktop-snapshot\",\"schemaVersion\":1}";
        try{Encode("ParseImport",snapshotJson);throw new InvalidOperationException("Snapshot accepted as preset");}catch(TargetInvocationException e){Require(e.InnerException is FormatException&&e.InnerException.Message.Contains("Snapshot file")&&e.InnerException.Message.Contains("Preset export"),"Snapshot rejection does not explain which file to choose");}
        var window=new MainWindow();
        T Field<T>(string n)=>(T)typeof(MainWindow).GetField(n,flags)!.GetValue(window)!;
        void Set(string n,object? value)=>typeof(MainWindow).GetField(n,flags)!.SetValue(window,value);
        object? Call(string n,params object[] args)=>typeof(MainWindow).GetMethod(n,flags)!.Invoke(window,args);
        try {
            Set("currentSnapshot",snapshot);Set("snapshotAddress",(ulong)1);Set("snapshotFirmware","V1.1.9");Set("editSnapshotValid",true);Call("SetEffects",(object)decoded.Blocks);Call("UpdateMixerState",true);Call("UpdateSaveState");
            Require(Field<Button>("exportPreset").IsEnabled&&Field<Button>("importPreset").IsEnabled,"Valid preset does not offer transfer actions.");
            foreach(var name in new[]{"preview","localSnapshotSource","presetSaveDialogOpen","tunerActive","drumReady"}){
                Set(name,true);Call("UpdateSaveState");Require(!Field<Button>("importPreset").IsEnabled&&!Field<Button>("exportPreset").IsEnabled,"Unsafe state permits preset transfer: "+name);Set(name,false);
            }
            Set("editSnapshotValid",false);Call("UpdateSaveState");Require(!Field<Button>("importPreset").IsEnabled,"Stale snapshot permits import");Set("editSnapshotValid",true);
            Field<TextBox>("thresholdInput").Text="26";Call("UpdateSaveState");Require(!Field<Button>("importPreset").IsEnabled&&!Field<Button>("exportPreset").IsEnabled,"Pending input permits transfer");Call("SetEffects",(object)decoded.Blocks);
            var imported=PresetDataParser.Parse(PresetSaveCommand.ExpectedDetail(bytes,1,"Imported tone"));
            var proof=new PresetSaveDestination((byte)snapshot.WireIndex,snapshot.Name,hex);
            var dialog=(Window)Call("CreatePresetImportDialog",snapshot,proof,imported)!;
            try{
                var cancel=(Button)dialog.FindName("PresetImportCancel");var confirm=(Button)dialog.FindName("PresetImportConfirm");
                Require(cancel.IsDefault&&cancel.IsCancel&&!confirm.IsDefault,"Overwrite confirmation does not default to Cancel");
                IEnumerable<DependencyObject> Walk(DependencyObject n){yield return n;foreach(var child in LogicalTreeHelper.GetChildren(n).OfType<DependencyObject>())foreach(var item in Walk(child))yield return item;}
                var texts=Walk(dialog).OfType<TextBlock>().Select(t=>t.Text).ToArray();
                Require(texts.Contains($"Replace {snapshot.SlotLabel} · {snapshot.Name}")&&texts.Contains($"With {snapshot.SlotLabel} · Imported tone"),"Confirmation uses exported slot rather than active slot");
                foreach(var size in new[]{new Size(420,340),new Size(600,440)}){var view=(FrameworkElement)dialog.Content;view.Measure(size);view.Arrange(new Rect(size));view.UpdateLayout();var p=confirm.TranslatePoint(new Point(),view);Require(p.X>=0&&p.X+confirm.ActualWidth<=size.Width&&p.Y+confirm.ActualHeight<=size.Height,"Import confirmation overflows compact windows");}
            }finally{dialog.Close();}
            var previousContext=SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
            try{
                foreach(var closeWindow in new[]{false,true}){
                    var runningDialog=(Window)Call("CreatePresetImportDialog",snapshot,proof,imported)!;
                    try{
                        var controller=runningDialog.Tag;var controllerType=controller.GetType();
                        bool Running()=>(bool)controllerType.GetProperty("Running")!.GetValue(controller)!;
                        bool Completed()=>(bool)controllerType.GetProperty("Completed")!.GetValue(controller)!;
                        var finish=new TaskCompletionSource<string>();CancellationToken request=default;
                        var callback=(Func<IProgress<PresetImportProgress>,CancellationToken,Task<string>>)((report,token)=>{request=token;report.Report(new("Restoring effect settings…"));return finish.Task;});
                        controllerType.GetProperty("Import")!.SetValue(controller,callback);
                        var action=(Button)runningDialog.FindName("PresetImportConfirm");var cancelAction=(Button)runningDialog.FindName("PresetImportCancel");
                        var indicator=(ProgressBar)runningDialog.FindName("PresetImportProgress");var message=(TextBlock)runningDialog.FindName("PresetImportStatus");
                        var closed=false;runningDialog.Closed+=(_,_)=>closed=true;
                        action.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        Require(Running()&&indicator.Visibility==Visibility.Visible&&indicator.IsIndeterminate&&!action.IsEnabled,"Import does not stay open with visible progress");
                        var compact=new Size(420,340);var content=(FrameworkElement)runningDialog.Content;content.Measure(compact);content.Arrange(new Rect(compact));content.UpdateLayout();
                        var position=indicator.TranslatePoint(new Point(),content);Require(position.Y>=0&&position.Y+indicator.ActualHeight<=compact.Height,"Import progress is outside the visible dialog footer");
                        if(closeWindow)runningDialog.Close();else cancelAction.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        Require(request.IsCancellationRequested&&Running()&&!closed&&!cancelAction.IsEnabled,"Cancel/Close dismissed the dialog before restoration");
                        Require(message.Text.Contains("Cancelling"),"Cancellation/restoration is not visible in the dialog");
                        finish.SetResult("Import cancelled · Previous preset restored and verified.");
                        var frame=new DispatcherFrame();var deadline=DateTime.UtcNow.AddSeconds(3);var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(5)};
                        timer.Tick+=(_,_)=>{if(Completed()||DateTime.UtcNow>deadline)frame.Continue=false;};timer.Start();Dispatcher.PushFrame(frame);timer.Stop();
                        Require(Completed()&&!Running()&&(string)action.Content=="Close"&&message.Text.Contains("restored and verified")&&!closed,"Restoration result was not retained in the dialog");
                    }finally{runningDialog.Close();}
                }
            }finally{SynchronizationContext.SetSynchronizationContext(previousContext);}
            var failureDialog=(Window)Call("CreatePresetImportDialog",snapshot,proof,imported)!;
            try{
                var control=failureDialog.Tag;var type=control.GetType();
                type.GetProperty("NeedsRecovery")!.SetValue(control,true);
                type.GetProperty("Import")!.SetValue(control,(Func<IProgress<PresetImportProgress>,CancellationToken,Task<string>>)((_,_)=>Task.FromResult("Import stopped. No preset was saved.")));
                type.GetProperty("Recover")!.SetValue(control,(Func<Task<string>>)(()=>{type.GetProperty("NeedsRecovery")!.SetValue(control,false);return Task.FromResult("Current settings read. The import remains incomplete.");}));
                var action=(Button)failureDialog.FindName("PresetImportConfirm");action.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Require((string)action.Content=="Reconnect and inspect"&&failureDialog.Title=="Import stopped","Failed dialog does not show a recovery action");
                action.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Require((string)action.Content=="Close"&&((TextBlock)failureDialog.FindName("PresetImportStatus")).Text.Contains("import remains incomplete"),"Recovery claims import success or fails to show current-state result");
            }finally{failureDialog.Close();}
            Set("editSnapshotValid",true);Field<TextBox>("address").Text="000000000001";
            ((Task)Call("HandlePresetImportFailureAsync",new PresetImportException("Verification stopped",false,new IOException("Imported effect settings could not be restored exactly. Nothing was saved.")))!).GetAwaiter().GetResult();
            Require(Field<StackPanel>("importRecoveryPanel").Visibility==Visibility.Visible&&Field<Button>("inspectImport").IsEnabled,"Failed import has no usable recovery action");
            Require(!Field<Button>("importPreset").IsEnabled&&!Field<Button>("dismissImportWarning").IsEnabled,"Failed import permits writing or hides unresolved state");
            Require(Field<TextBlock>("importRecoverySummary").Text.Contains("does not save or repeat")&&Field<TextBlock>("importRecoveryDetails").Text.Contains("restored exactly"),"Failure loses its reason or safe next step");
            Set("editSnapshotValid",true);Call("UpdateImportRecoveryAfterRead",true,(ulong)1);Call("UpdateSaveState");
            Require(Field<StackPanel>("importRecoveryPanel").Visibility==Visibility.Visible&&Field<TextBlock>("importRecoverySummary").Text.Contains("import remains incomplete")&&Field<Button>("dismissImportWarning").IsEnabled,"Read recovery hides the incomplete import or blocks reviewing it");
            Require(Field<F15ReadSession?>("connectedSession") is null&&Field<CancellationTokenSource?>("operation") is null,"Offline transfer checks opened hardware");
        }finally{window.Close();}
        var clickWindow=new MainWindow();var file=Path.Combine(Path.GetTempPath(),"iamp-import-click-"+Guid.NewGuid().ToString("N")+".json");File.WriteAllText(file,json);
        var oldContext=SynchronizationContext.Current;SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
        try{
            void Put(string n,object v)=>typeof(MainWindow).GetField(n,flags)!.SetValue(clickWindow,v);
            T Get<T>(string n)=>(T)typeof(MainWindow).GetField(n,flags)!.GetValue(clickWindow)!;
            void Invoke(string n,params object[] values)=>typeof(MainWindow).GetMethod(n,flags)!.Invoke(clickWindow,values);
            Put("currentSnapshot",snapshot);Put("snapshotAddress",(ulong)1);Put("snapshotFirmware","V1.1.9");Put("editSnapshotValid",true);Invoke("SetEffects",(object)decoded.Blocks);Invoke("UpdateMixerState",true);Invoke("UpdateSaveState");
            Put("presetImportFilePicker",(Func<Microsoft.Win32.OpenFileDialog,bool?>)(picker=>{picker.FileName=file;return true;}));
            var shown=false;var done=new DispatcherFrame();var limit=DateTime.UtcNow.AddSeconds(4);var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(5)};
            timer.Tick+=(_,_)=>{
                var confirmation=Application.Current.Windows.OfType<Window>().FirstOrDefault(w=>w.Title=="Import preset"&&w.Tag?.GetType().Name=="PresetImportDialogController");
                if(confirmation is not null&&!shown){shown=true;((Button)confirmation.FindName("PresetImportCancel")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}
                if((shown&&Get<CancellationTokenSource?>("operation") is null)||DateTime.UtcNow>limit)done.Continue=false;
            };
            timer.Start();Get<Button>("importPreset").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Dispatcher.PushFrame(done);timer.Stop();
            Require(shown&&Get<F15ReadSession?>("connectedSession") is null&&Get<CancellationTokenSource?>("operation") is null,"Import click did not show confirmation before any Bluetooth work");
            var invalidDialog=(Window)typeof(MainWindow).GetMethod("CreatePresetImportFileErrorDialog",flags)!.Invoke(clickWindow,["Snapshot-example.json","This is a Snapshot file. Choose a Preset export."])!;
            try{Require(invalidDialog.Title=="Cannot import this file"&&invalidDialog.FindName("PresetFileErrorChoose") is Button&&invalidDialog.FindName("PresetFileErrorCancel") is Button,"Invalid file has no visible explanation/reselection dialog");}finally{invalidDialog.Close();}
        }finally{SynchronizationContext.SetSynchronizationContext(oldContext);clickWindow.Close();File.Delete(file);}
        Console.WriteLine("PASS: lossless preset JSON, invalid-file guards, active-slot confirmation, visible dialog progress, Cancel/Close await restoration and retained completion result. No hardware commands.");
    }
}
