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

internal static class SteadyOperationChecks
{
    public static void Run()
    {
        const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
        var window=new MainWindow();
        T Field<T>(string n)=>(T)typeof(MainWindow).GetField(n,flags)!.GetValue(window)!;
        void Set(string n,object value)=>typeof(MainWindow).GetField(n,flags)!.SetValue(window,value);
        object? Call(string n,params object[] args)=>typeof(MainWindow).GetMethod(n,flags)!.Invoke(window,args);
        void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        try {
            using var fixture=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"fixtures","f15-preset-save-observations.json")));
            var state=fixture.RootElement.GetProperty("baselineStateHex").GetString()!;var detail=fixture.RootElement.GetProperty("baselineDetailHex").GetString()!;
            var decoded=PresetDataParser.Parse(Convert.FromHexString(detail));
            var preset=new CurrentPresetSnapshot(decoded.WireIndex,decoded.Name,state,detail,decoded.Blocks);
            Field<TextBox>("address").Text="000000000001";
            Set("currentSnapshot",preset);Set("snapshotAddress",(ulong)1);Set("snapshotFirmware","V1.1.9");Set("editSnapshotValid",true);
            Set("names",new PresetName[]{new(decoded.WireIndex,decoded.Name)});Call("SetEffects",(object)preset.Blocks);Call("UpdateMixerState",true);Call("Filter");Call("UpdateSaveState");
            Field<TextBlock>("currentPreset").Text=$"{preset.SlotLabel} · {preset.Name}";
            Call("UpdateConnectionControls");
            var root=(FrameworkElement)window.Content;var size=new Size(1280,900);root.Width=size.Width;root.Height=size.Height;
            void Layout(){root.Measure(size);root.Arrange(new Rect(size));root.UpdateLayout();}Layout();
            var slider=Field<StackPanel>("effectDetails").Children.OfType<Slider>().First();
            var save=Field<Button>("savePreset");var unavailable=Field<Button>("moveEffectLeft");var selector=Field<ComboBox>("presets");
            double ButtonOpacity(Button b)=>((Border)b.Template.FindName("Surface",b)).Opacity;
            Require(slider.IsEnabled&&save.IsEnabled&&selector.IsEnabled&&!unavailable.IsEnabled,$"Starting state: slider={slider.IsEnabled}, save={save.IsEnabled}, selector={selector.IsEnabled}, moveLeft={unavailable.IsEnabled}; {Field<TextBlock>("saveMessage").Text}");
            var colour=slider.Foreground;var unavailableOpacity=ButtonOpacity(unavailable);var value=slider.Value;
            Call("PrepareSteadyAppearance");slider.Value=value==slider.Maximum?value-1:value+1;Layout();
            Require(!save.IsEnabled&&ButtonOpacity(save)==1,"Pending slider adjustment visibly dims Save before applying");
            Call("BeginSteadyOperation","Synthetic live edit",false);Layout();
            Require(!slider.IsEnabled&&!selector.IsEnabled&&!save.IsEnabled,"Conflicting actions are not locked during the write");
            Require(slider.Opacity==1&&Equals(slider.Foreground,colour)&&selector.Opacity==1&&ButtonOpacity(save)==1,"Live edit visually dims available controls");
            Require(ButtonOpacity(unavailable)==unavailableOpacity&&!SteadyControlAppearance.GetKeepEnabledAppearance(unavailable),"Originally unavailable action was made to look available");
            Require(Field<TextBlock>("saveMessage").Text=="Applying…"&&Field<Button>("cancel").Visibility==Visibility.Collapsed,"Applying feedback or Cancel behavior differs");
            var bitmap=new RenderTargetBitmap(1280,900,96,96,PixelFormats.Pbgra32);bitmap.Render(root);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));Directory.CreateDirectory("analysis/private");using(var output=File.Create("analysis/private/steady-operation-1.51.1.png"))encoder.Save(output);
            slider.Value=value;Call("End");Layout();
            Require(slider.IsEnabled&&save.IsEnabled&&!SteadyControlAppearance.GetKeepEnabledAppearance(slider),"Completed write did not restore normal interaction/styling");
            Call("BeginSteadyOperation","Synthetic preset recall",true);Layout();
            Require(Field<TextBlock>("saveMessage").Text=="Loading preset…"&&selector.Opacity==1,"Preset recall feedback or styling differs");
            Set("editSnapshotValid",false);Call("End");Layout();
            Require(!slider.IsEnabled&&slider.Opacity<1&&!SteadyControlAppearance.GetKeepEnabledAppearance(slider),"Unconfirmed operation hides its unavailable state after ending");
            Console.WriteLine("PASS: steady live-edit/preset-recall visuals, pending-input phase, Applying/Loading feedback, conflict locks, existing disabled states and completion/failure cleanup. No hardware commands.");
        }
        finally {Call("End");window.Close();}
    }
}
