using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Iamp.Desktop;
using Iamp.Protocol;
using Iamp.Transport.Windows;

internal static class UnsavedPresetChecks
{
    public static void Run()
    {
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        void Require(bool passed,string message){if(!passed)throw new InvalidOperationException(message);}
        using var fixture=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"fixtures","f15-preset-save-observations.json")));
        var raw=fixture.RootElement.GetProperty("baselineDetailHex").GetString()!;var state=fixture.RootElement.GetProperty("baselineStateHex").GetString()!;
        var decoded=PresetDataParser.Parse(Convert.FromHexString(raw));var original=new CurrentPresetSnapshot(decoded.WireIndex,decoded.Name,state,raw,decoded.Blocks);
        var window=new MainWindow();
        void Set(string name,object? value)=>typeof(MainWindow).GetField(name,flags)!.SetValue(window,value);
        T Field<T>(string name)=>(T)typeof(MainWindow).GetField(name,flags)!.GetValue(window)!;
        void Call(string name,params object[] values)=>typeof(MainWindow).GetMethod(name,flags)!.Invoke(window,values);
        CurrentPresetSnapshot Snapshot(byte[] detail){var p=PresetDataParser.Parse(detail);return original with {WireIndex=p.WireIndex,Name=p.Name,RawDetailPayloadHex=Convert.ToHexString(detail),Blocks=p.Blocks};}
        void Show(CurrentPresetSnapshot current){Set("currentSnapshot",current);Call("SetEffects",(object)current.Blocks);Call("UpdateSaveState");}
        CurrentPresetSnapshot Chain(IEnumerable<PresetEffectBlock> effects){
            var active=effects.ToArray();var bytes=new List<byte>(Convert.FromHexString(raw).Take(22));
            for(var i=0;i<20;i++){
                var block=i<active.Length?active[i]:null;bytes.Add((byte)(i+1));bytes.Add(block?.TypeCode??0);bytes.Add((byte)((block?.Values.Count??0)*2));
                if(block is not null)foreach(var value in block.Values){bytes.Add((byte)value);bytes.Add((byte)(value>>8));}
            }
            bytes.AddRange(decoded.UnknownTrailer);return Snapshot(bytes.ToArray());
        }
        try{
            Set("currentSnapshot",original);Set("snapshotAddress",(ulong)1);Set("snapshotFirmware","V1.1.9");Set("editSnapshotValid",true);Set("originalSnapshotDetail",raw);
            Call("SetEffects",(object)decoded.Blocks);Call("UpdateMixerState",true);Call("UpdateSaveState");
            var warning=Field<Border>("unsavedChangesNotice");
            Require(warning.Visibility==Visibility.Collapsed,"Clean preset shows an unsaved warning");
            Field<TextBox>("thresholdInput").Text="26";
            Require(warning.Visibility==Visibility.Visible&&warning.HasAnimatedProperties,"Pending preset edit does not show a flashing warning");
            Require(warning.Child is TextBlock text&&text.Text=="Unsaved changes exist"&&text.FontWeight==FontWeights.Bold&&text.FontSize>=16,"Warning is not prominent or has wrong wording");
            Field<TextBox>("thresholdInput").Text="25";
            Require(warning.Visibility==Visibility.Collapsed&&!warning.HasAnimatedProperties,"Cleared pending value leaves a flashing warning");
            var changed=Snapshot(PresetDataParser.WithParameterValue(Convert.FromHexString(raw),1,2,26));
            Set("liveEditUnsaved",false);Show(changed);
            Require(warning.Visibility==Visibility.Visible&&warning.HasAnimatedProperties,"Live preset edit does not show warning");
            // Changing effect selection must not clear the preset's unsaved state.
            ((Button)Field<WrapPanel>("effectChain").Children[1]).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(warning.Visibility==Visibility.Visible,"Browsing effects cleared the unsaved warning");
            Field<TextBlock>("currentPreset").Text=$"{original.SlotLabel} · {original.Name}";
            Set("names",new PresetName[]{new(original.WireIndex,original.Name)});Call("Filter");
            var root=(FrameworkElement)window.Content;
            foreach(var size in new[]{new Size(1280,900),new Size(620,500)}){
                root.Width=size.Width;root.Height=size.Height;root.Measure(size);root.Arrange(new Rect(size));root.UpdateLayout();
                var heading=(FrameworkElement)warning.Parent;var point=warning.TranslatePoint(new Point(),heading);
                Require(point.X>=0&&point.X+warning.ActualWidth<=heading.ActualWidth+1,"Warning overflows narrow heading");
                var title=(TextBlock)((WrapPanel)heading).Children[0];
                var titlePoint=title.TranslatePoint(new Point(),heading);
                double InkCentre(TextBlock text){
                    var formatted=new FormattedText(text.Text,System.Globalization.CultureInfo.CurrentUICulture,text.FlowDirection,
                        new Typeface(text.FontFamily,text.FontStyle,text.FontWeight,text.FontStretch),text.FontSize,text.Foreground,VisualTreeHelper.GetDpi(text).PixelsPerDip);
                    var bounds=formatted.BuildGeometry(new Point()).Bounds;return bounds.Top+bounds.Height/2;
                }
                var label=(TextBlock)warning.Child;
                var labelPoint=label.TranslatePoint(new Point(),heading);
                Require(Math.Abs(labelPoint.Y+InkCentre(label)-point.Y-warning.ActualHeight/2)<0.5,"Warning text is not centred inside its box");
                if(point.X>titlePoint.X)
                    Require(Math.Abs(labelPoint.Y+InkCentre(label)-titlePoint.Y-InkCentre(title))<0.5,"Visible warning text is not vertically centred with Presets text");
            }
            var full=new Size(1280,900);root.Width=full.Width;root.Height=full.Height;root.Measure(full);root.Arrange(new Rect(full));root.UpdateLayout();
            var bitmap=new RenderTargetBitmap(1280,900,96,96,PixelFormats.Pbgra32);bitmap.Render(root);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));Directory.CreateDirectory("analysis/private");using(var output=File.Create("analysis/private/unsaved-preset-1.50.6.png"))encoder.Save(output);
            Set("liveEditUnsaved",false);Set("originalSnapshotDetail",changed.RawDetailPayloadHex);Call("ResetPresetEntryBaseline",changed);Call("UpdateSaveState");
            Require(warning.Visibility==Visibility.Collapsed&&!warning.HasAnimatedProperties,"Saving does not clear the warning and animation");
            // Mixer levels are amplifier-wide, not preset contents.
            var editors=Field<System.Collections.IDictionary>("mixerEditors");var mixer=editors[MixerChannel.Bluetooth]!;
            ((Slider)mixer.GetType().GetProperty("Fader",flags)!.GetValue(mixer)!).Value=36;Call("UpdateSaveState");
            Require(warning.Visibility==Visibility.Collapsed,"Mixer-only input incorrectly marks the preset unsaved");Call("UpdateMixerState",true);
            Set("liveEditUnsaved",true);Call("UpdateSaveState");
            var other=PresetSaveCommand.ExpectedDetail(Convert.FromHexString(raw),1,"Other preset");var next=PresetDataParser.Parse(other);
            Set("currentSnapshot",new CurrentPresetSnapshot(1,next.Name,state,Convert.ToHexString(other),next.Blocks));Call("SetEffects",(object)next.Blocks);Call("UpdateSaveState");
            Require(warning.Visibility==Visibility.Collapsed,"Switching to a different preset retains the old warning");
            Set("liveEditUnsaved",false);Set("originalSnapshotDetail",Convert.ToHexString(other));Call("UpdateSaveState");
            Require(warning.Visibility==Visibility.Collapsed,"New preset does not remain clean");
            Show(original);Call("ResetPresetEntryBaseline",original);Call("UpdateSaveState");
            var active=decoded.Blocks.Where(b=>b.TypeCode!=0).ToArray();
            void DirtyThenUndo(CurrentPresetSnapshot altered,string description){
                Set("liveEditUnsaved",false);Show(altered);Require(warning.Visibility==Visibility.Visible,description+" did not show warning");
                Require((bool)typeof(MainWindow).GetMethod("HasPresetStateChanges",flags)!.Invoke(window,null)!,description+" is not reflected in preset navigation guards");
                Set("liveEditUnsaved",true);Show(original);Require(warning.Visibility==Visibility.Collapsed&&!warning.HasAnimatedProperties,description+" undo left a stale warning");
                Require(!(bool)typeof(MainWindow).GetMethod("HasPresetStateChanges",flags)!.Invoke(window,null)!,description+" undo retains a discard warning");
            }
            DirtyThenUndo(changed,"Parameter change");
            var compound=Snapshot(PresetDataParser.WithParameterValue(Convert.FromHexString(changed.RawDetailPayloadHex),1,3,51));
            Show(compound);Show(changed);Require(warning.Visibility==Visibility.Visible,"Partially undoing several edits cleared warning");Show(original);
            DirtyThenUndo(Chain(active.Append(new PresetEffectBlock(6,4,new ushort[]{1,0,20,70,0,0}))),"Add effect");
            DirtyThenUndo(Chain(active.Skip(1)),"Remove effect");
            DirtyThenUndo(Chain(new[]{active[1],active[0]}.Concat(active.Skip(2))),"Reorder effects");
            var disabled=active[0].Values.ToArray();disabled[0]=0;
            DirtyThenUndo(Chain(new[]{active[0] with {Values=disabled}}.Concat(active.Skip(1))),"Disable effect");
            var model=active[0].Values.ToArray();model[1]=0;
            DirtyThenUndo(Chain(new[]{active[0] with {Values=model}}.Concat(active.Skip(1))),"Replace model");
            DirtyThenUndo(Snapshot(PresetSaveCommand.ExpectedDetail(Convert.FromHexString(raw),(byte)original.WireIndex,"Changed name")),"Rename");
            // Re-adding the same effect with different defaults is still a real difference.
            var defaults=active[0].Values.ToArray();defaults[2]=15;
            Show(Chain(new[]{active[0] with {Values=defaults}}.Concat(active.Skip(1))));Require(warning.Visibility==Visibility.Visible,"Re-added effect with different values was treated as restored");Show(original);
            var amp=active.Single(b=>b.TypeCode==2);var bookkeeping=PresetDataParser.WithParameterValue(Convert.FromHexString(raw),amp.Position,8,0);
            Show(Snapshot(bookkeeping));Require(warning.Visibility==Visibility.Collapsed,"Derived Amp bookkeeping flag leaves a false unsaved warning");
            Show(changed);Set("originalSnapshotDetail",changed.RawDetailPayloadHex);Set("liveEditUnsaved",false);Call("UpdateSaveState");
            Require(warning.Visibility==Visibility.Visible,"Refreshing changed data overwrote the entry baseline");
            Call("ResetPresetEntryBaseline",changed);Call("UpdateSaveState");Require(warning.Visibility==Visibility.Collapsed,"Confirmed save did not establish a new clean baseline");
            Show(original);Require(warning.Visibility==Visibility.Visible,"Reverting to pre-save values incorrectly matches the newly saved baseline");
            Call("ResetPresetEntryBaseline",original);Show(original with {RawStatePayloadHex="global mixer state differs"});Require(warning.Visibility==Visibility.Collapsed,"Global state marked the preset dirty");
            var mutable=original with {Blocks=original.Blocks.Select(b=>b with {Values=b.Values.ToArray()}).ToArray()};
            Show(mutable);Call("ResetPresetEntryBaseline",mutable);((ushort[])mutable.Blocks[0].Values)[2]=99;Call("UpdateSaveState");
            Require((bool)typeof(MainWindow).GetMethod("HasPresetStateChanges",flags)!.Invoke(window,null)!,"Entry baseline shares mutable parameter storage with current state");
            Show(original);Show(changed);Set("snapshotAddress",(ulong)2);Call("UpdateSaveState");Require(warning.Visibility==Visibility.Collapsed,"Different amplifier inherits the previous amplifier's baseline");
            Require(Field<F15ReadSession?>("connectedSession") is null&&Field<CancellationTokenSource?>("operation") is null,"Unsaved notice check opened hardware");
            Console.WriteLine("PASS: complete preset-state comparison, parameter/model/on-off/order/add/remove undo, default-value differences, derived-flag exclusion, retained entry and saved baselines, pending/mixer separation and flashing layout. No hardware commands.");
        }finally{window.Close();}
    }
}
