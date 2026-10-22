using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using Iamp.Desktop;
using Iamp.Protocol;
using Iamp.Transport.Windows;

internal static class F4PresetPanelChecks
{
    public static void Run()
    {
        var panel=new F4PresetPanel();
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
        T Field<T>(string name)=>(T)typeof(F4PresetPanel).GetField(name,flags)!.GetValue(panel)!;
        void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        CurrentPresetSnapshot Current(int wire,string name)=>new(wire,name,"00","00",Array.Empty<PresetEffectBlock>());
        var names=Enumerable.Range(1,80).Select(i=>new PresetName(i,$"Preset {PresetAddress.FromWireIndex(i).Label}")).ToArray();
        for(var wire=1;wire<=4;wire++){
            panel.Update(names,Current(wire,$"Active {wire}"),true);
            Require(Field<TextBlock>("bankValue").Text=="0","First-bank number differs");
            Require(Field<TextBlock[]>("slotLabels").Select(t=>t.Text).SequenceEqual(new[]{"0A","0B","0C","0D"}),"First bank mappings differ");
            var cards=Field<Border[]>("switchCards");
            Require(cards.Count(c=>AutomationProperties.GetName(c).Contains(", active preset"))==1&&AutomationProperties.GetName(cards[wire-1]).Contains(", active preset"),"Active switch marker differs");
        }
        panel.Update(names,Current(79,"Renamed active preset"),true);
        Require(Field<TextBlock>("bankValue").Text=="19"&&Field<TextBlock[]>("slotLabels").Select(t=>t.Text).SequenceEqual(new[]{"19A","19B","19C","19D"}),"Final bank mappings differ");
        Require(Field<TextBlock[]>("presetLabels")[2].Text=="Renamed active preset"&&Field<TextBlock[]>("presetLabels")[0].Text=="Preset 19A","Authoritative active name or adjacent mappings differ");
        panel.Update(Array.Empty<PresetName>(),Current(79,"Renamed active preset"),false);
        Require(Field<TextBlock>("bankLabel").Text=="Last read bank"&&Field<TextBlock>("activePreset").Text.StartsWith("Last read:"),"Retained data claims live state");
        Require(Field<TextBlock[]>("presetLabels")[0].Text=="Name not loaded","Missing preset name was fabricated");
        panel.Update(names,null,false);
        Require(Field<TextBlock>("bankValue").Text=="—"&&Field<TextBlock[]>("slotLabels").All(t=>t.Text=="—"),"Unknown current preset fabricates a bank");
        var requested=new List<int>();panel.PresetRequested+=requested.Add;
        panel.Update(names,Current(1,"Active A"),true,true);
        var previous=Field<Button>("previousBank");var next=Field<Button>("nextBank");var buttons=Field<Button[]>("switchButtons");
        void Click(Button button)=>button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(!previous.IsEnabled&&next.IsEnabled&&!buttons[0].IsEnabled&&buttons[1].IsEnabled,"First bank action guards differ");
        Click(next);
        Require(requested.Count==0&&Field<TextBlock>("bankValue").Text=="1"&&Field<TextBlock>("bankLabel").Text=="Selected bank","Bank browsing recalls a patch or claims active bank");
        Require(Field<TextBlock[]>("slotLabels").Select(t=>t.Text).SequenceEqual(new[]{"1A","1B","1C","1D"}),"Browsed bank mappings differ");
        Click(buttons[3]);Require(requested.SequenceEqual(new[]{8}),"Clicking D does not request the browsed bank slot");
        panel.Update(names,Current(1,"Active A"),true,true);
        Require(Field<TextBlock>("bankValue").Text=="1","Polling the same patch resets bank browsing");
        panel.Update(names,Current(79,"External C"),true,true);
        Require(Field<TextBlock>("bankValue").Text=="19"&&!next.IsEnabled&&!buttons[2].IsEnabled,"External patch does not synchronize bank or bound next action");
        panel.Update(names,Current(79,"External C"),true,false);Click(buttons[0]);Click(previous);
        Require(requested.Count==1&&Field<TextBlock>("bankValue").Text=="19"&&buttons.All(b=>!b.IsEnabled),"Busy/disconnected synthetic clicks escape action guards");
        panel.Update(Array.Empty<PresetName>(),Current(79,"External C"),true,true);
        Require(buttons.All(b=>!b.IsEnabled),"Unknown preset names enable recall");
        foreach(var width in new[]{316d,500d,900d}){
            panel.Update(names,Current(79,"A long active preset name"),true);panel.Width=width;panel.Measure(new Size(width,double.PositiveInfinity));panel.Arrange(new Rect(0,0,width,panel.DesiredSize.Height));panel.UpdateLayout();
            foreach(var card in Field<Border[]>("switchCards")){
                var point=card.TranslatePoint(new Point(),panel);Require(point.X>=0&&point.X+card.ActualWidth<=width+0.1,"F4 switch overflows compact panel");
            }
            Require(panel.ActualHeight>0,"F4 diagram has no height");
            foreach(var button in new[]{previous,next}){var point=button.TranslatePoint(new Point(),panel);Require(point.X>=0&&point.X+button.ActualWidth<=width+0.1,"Bank action overflows compact panel");}
            if(width==900){
                var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap(900,(int)Math.Ceiling(panel.ActualHeight),96,96,PixelFormats.Pbgra32);bitmap.Render(panel);
                var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
                System.IO.Directory.CreateDirectory("analysis/private");using var file=System.IO.File.Create("analysis/private/f4-interactive-1.53.0.png");encoder.Save(file);
            }
        }
        Console.WriteLine("PASS: F4 clickable switches, correct browsed-bank targets, bank bounds, stable polling, external synchronization, busy/offline guards, active selection and compact bounds. No hardware commands.");
    }
}
