using System.IO;using System.Reflection;using System.Text.Json;using System.Windows;using System.Windows.Controls;using Iamp.Desktop;using Iamp.Protocol;using Iamp.Transport.Windows;

internal static class PresetSaveChecks
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static T Field<T>(object o,string n)=>(T)o.GetType().GetField(n,Flags)!.GetValue(o)!;
    static void Set(object o,string n,object? v)=>o.GetType().GetField(n,Flags)!.SetValue(o,v);
    static object? Call(object o,string n,params object?[] args)=>o.GetType().GetMethod(n,Flags)!.Invoke(o,args);
    static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    internal static void Run()
    {
        using var fixture=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"fixtures","f15-preset-save-observations.json")));
        var root=fixture.RootElement;var state=root.GetProperty("baselineStateHex").GetString()!;var detail=root.GetProperty("baselineDetailHex").GetString()!;
        var decoded=PresetDataParser.Parse(Convert.FromHexString(detail));var current=new CurrentPresetSnapshot(decoded.WireIndex,decoded.Name,state,detail,decoded.Blocks);
        var window=new MainWindow();Window? dialog=null;
        try {
            Set(window,"names",Enumerable.Range(1,80).Select(i=>new PresetName(i,i==4?current.Name:$"Preset {i}")).ToArray());
            void Load(CurrentPresetSnapshot snapshot) {
                Set(window,"currentSnapshot",snapshot);Set(window,"snapshotAddress",(ulong)1);Set(window,"snapshotFirmware","V1.1.9");Set(window,"editSnapshotValid",true);Set(window,"localSnapshotSource",false);Set(window,"originalSnapshotDetail",detail);Set(window,"liveEditUnsaved",snapshot.RawDetailPayloadHex!=detail);
                Call(window,"UpdateMixerState",true);Call(window,"SetEffects",(object)snapshot.Blocks);Call(window,"UpdateSaveState");
            }
            var save=Field<Button>(window,"savePreset");Load(current);Require(save.IsEnabled,"Unchanged preset must allow rename and save-as.");
            foreach(ushort threshold in new ushort[]{0,26,27,100}) {
                var bytes=PresetDataParser.WithParameterValue(Convert.FromHexString(detail),1,2,threshold);var preset=PresetDataParser.Parse(bytes);
                Load(current with {RawDetailPayloadHex=Convert.ToHexString(bytes),Blocks=preset.Blocks});Require(save.IsEnabled,"Broader verified edits remain save-restricted.");
            }
            Load(current);Field<TextBox>(window,"thresholdInput").Text="26";Require(!save.IsEnabled,"Pending effect input enabled Save.");Load(current);
            var editors=Field<System.Collections.IDictionary>(window,"mixerEditors");var mixer=editors[MixerChannel.Bluetooth]!;
            var fader=(Slider)mixer.GetType().GetProperty("Fader",Flags)!.GetValue(mixer)!;fader.Value=36;Require(!save.IsEnabled,"Pending mixer input did not immediately disable Save.");Load(current);
            void Blocked(string reason) {Call(window,"UpdateSaveState");Require(!save.IsEnabled,reason);Require(((Task)Call(window,"SavePresetToAsync",(byte)4,"Test",true)!).IsCompletedSuccessfully,"Blocked save began asynchronous device work.");Require(Field<F15ReadSession?>(window,"connectedSession") is null,"Offline save check opened a device.");}
            Set(window,"localSnapshotSource",true);Blocked("Imported snapshot enabled Save");Set(window,"localSnapshotSource",false);
            Set(window,"editSnapshotValid",false);Blocked("Invalid snapshot enabled Save");
            Require(Field<TextBlock>(window,"saveMessage").Text.Contains("Refresh amplifier"),"Invalid snapshot misleadingly asked for supported firmware.");
            Set(window,"presetSaveNotice","Save stopped before writing · Nothing was saved.");Call(window,"UpdateSaveState");
            Require(Field<TextBlock>(window,"saveMessage").Text.Contains("Nothing was saved"),"Save outcome was hidden while invalid.");
            Set(window,"editSnapshotValid",true);Call(window,"UpdateSaveState");Require(save.IsEnabled,"Fresh verified snapshot failed to restore Save availability.");
            Set(window,"presetSaveNotice",null);
            Set(window,"snapshotFirmware","V1.1.2");Blocked("Unsupported firmware enabled Save");Set(window,"snapshotFirmware","V1.1.9");
            Set(window,"snapshotAddress",null);Blocked("Missing target enabled Save");Set(window,"snapshotAddress",(ulong)1);
            Set(window,"presetSaveDialogOpen",true);Blocked("Open save dialog enabled another save");Set(window,"presetSaveDialogOpen",false);
            Call(window,"Begin","Offline save busy check");Blocked("Busy operation enabled Save");Call(window,"End");Load(current);
            dialog=(Window)Call(window,"CreatePresetSaveDialog",current)!;
            var name=(TextBox)dialog.FindName("PresetSaveName");var destination=(ComboBox)dialog.FindName("PresetSaveDestination");var proceed=(Button)dialog.FindName("PresetSaveContinue");
            Require((string)proceed.Content=="Save","Save dialog action must be labelled Save.");
            Require(name.Text==current.Name && name.MaxLength==20 && destination.Items.Count==80 && destination.SelectedIndex==3 && proceed.IsEnabled,"Save dialog defaults/destination count differ.");
            foreach(var bad in new[]{""," ","café","bad\nname"}){name.Text=bad;Require(!proceed.IsEnabled,"Invalid name enabled continue.");}
            foreach(var valid in new[]{"A","12345678901234567890"," My tone-1.0_ "}){name.Text=valid;Require(proceed.IsEnabled && name.Text==valid,"Valid name changed or was rejected.");}
            destination.SelectedIndex=79;Require(proceed.IsEnabled && ReferenceEquals(current,Field<CurrentPresetSnapshot>(window,"currentSnapshot")),"Dialog selected/recalled a preset before confirmation.");
            foreach(var size in new[]{new Size(420,340),new Size(580,400),new Size(740,500)}) {
                var content=(FrameworkElement)dialog.Content;content.Measure(size);content.Arrange(new Rect(size));content.UpdateLayout();
                foreach(var control in new FrameworkElement[]{name,destination,proceed}){var p=control.TransformToAncestor(content).Transform(new Point());Require(control.ActualWidth>0 && p.X>=0 && p.X+control.ActualWidth<=size.Width+1,"Save dialog control exceeds width.");}
            }
            Require(Field<F15ReadSession?>(window,"connectedSession") is null && Field<CancellationTokenSource?>(window,"operation") is null,"Dialog caused device work.");
            var proof=new PresetSaveDestination(80,"New Patch",detail);
            var confirmation=(Window)Call(window,"CreatePresetOverwriteDialog",current,proof,current.Name)!;
            try {
                var confirm=(Button)confirmation.FindName("PresetOverwriteSave");var cancel=(Button)confirmation.FindName("PresetOverwriteCancel");
                Require((string)confirm.Content=="Save" && (string)cancel.Content=="Cancel" && cancel.IsDefault && cancel.IsCancel && !confirm.IsDefault,"Overwrite dialog must use styled Save/Cancel with Cancel as default.");
                Require(Equals(confirmation.Background,window.Background),"Overwrite dialog does not match the app background.");
                foreach(var size in new[]{new Size(420,340),new Size(580,390),new Size(740,500)}) {
                    var content=(FrameworkElement)confirmation.Content;content.Measure(size);content.Arrange(new Rect(size));content.UpdateLayout();
                    foreach(var control in new[]{confirm,cancel}){var p=control.TransformToAncestor(content).Transform(new Point());Require(p.X>=0 && p.X+control.ActualWidth<=size.Width+1 && p.Y+control.ActualHeight<=size.Height+1,"Confirmation actions exceed dialog bounds.");}
                }
                confirmation.Loaded+=(_,_)=>confirm.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Require(confirmation.ShowDialog()==true,"Styled Save did not return a confirmed result.");
            }finally {confirmation.Close();}
            Call(window,"Begin","Confirmation deadline check");
            var deadline=Field<CancellationTokenSource>(window,"operation");deadline.CancelAfter(TimeSpan.FromMilliseconds(60));
            var reviewTimer=new System.Windows.Threading.DispatcherTimer {Interval=TimeSpan.FromMilliseconds(150)};
            reviewTimer.Tick+=(_,_)=> {
                var review=Application.Current.Windows.OfType<Window>().SingleOrDefault(w=>w.Title=="Confirm preset overwrite");
                if(review is null)return;
                reviewTimer.Stop();((Button)review.FindName("PresetOverwriteCancel")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            };
            try {
                reviewTimer.Start();Require(!(bool)Call(window,"ConfirmPresetOverwrite",current,proof,current.Name)!,"Cancel confirmed a save.");
                Require(!deadline.IsCancellationRequested,"Time spent reviewing confirmation expired the device operation.");
            }finally {reviewTimer.Stop();Call(window,"End");}
            var liveCopy=decoded.Blocks.Select(b=>b.Position==6?new PresetEffectBlock(6,4,new ushort[]{1,0,20,30,0,0}):b).ToArray();
            Set(window,"presetEffectIndex",new Dictionary<int,IReadOnlyList<PresetEffectBlock>>{{4,liveCopy},{80,Array.Empty<PresetEffectBlock>()}});
            var savedCopy=current with {WireIndex=80,Name="Saved copy",Blocks=liveCopy};
            Call(window,"UpdateSavedPresetLibrary",savedCopy,decoded,false);
            var index=Field<IReadOnlyDictionary<int,IReadOnlyList<PresetEffectBlock>>>(window,"presetEffectIndex");
            Require(index[4].Count(b=>b.TypeCode!=0)==5 && index[80].Count(b=>b.TypeCode!=0)==6,"Save As cached the unsaved source chain as if it were stored.");
            Require(Field<IReadOnlyList<PresetName>>(window,"names").Single(n=>n.WireIndex==80).Name=="Saved copy","Saved destination name did not update.");
            Console.WriteLine("PASS: general save/rename guards; pending effect/mixer inputs; all80 destinations; name limits; dialog sizes and no device work.");
        }
        finally {dialog?.Close();window.Close();}
    }
}
