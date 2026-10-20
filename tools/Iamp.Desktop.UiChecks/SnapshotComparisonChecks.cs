using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using Iamp.Desktop;
using Iamp.Protocol;
using Iamp.Transport.Windows;

internal static class SnapshotComparisonChecks
{
    public static void Run()
    {
        const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
        void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        var window=new MainWindow();
        try
        {
            var type=typeof(MainWindow).Assembly.GetType("Iamp.Desktop.ReadableSnapshotComparison")!;
            var amp=new PresetEffectBlock(1,2,new ushort[]{1,31,0,47,44,35,51,75,1});
            var dyna=new PresetEffectBlock(2,4,new ushort[]{1,2,25,50,0,0});
            var movedAmp=amp with {Position=2};
            var secondAmp=amp with {Position=3};
            var otherSecond=amp with {Position=4};
            var own=new[]{amp,dyna,secondAmp};var other=new[]{dyna with {Position=1},movedAmp,otherSecond};
            object? Match(PresetEffectBlock b)=>type.GetMethod("FindMatchingEffect",flags)!.Invoke(null,[b,own,other]);
            Require(ReferenceEquals(Match(amp),movedAmp)&&ReferenceEquals(Match(secondAmp),otherSecond),"Reordered/duplicate model matching differs");
            var parameterType=typeof(MainWindow).Assembly.GetType("Iamp.Desktop.ReadOnlyParameter")!;
            object Parameter(string name,decimal value,string? unit=null,bool off=false)=>Activator.CreateInstance(parameterType,[name,value,2,unit,null,off])!;
            string Format(object p)=>(string)type.GetMethod("Format",flags)!.Invoke(null,[p])!;
            Require(Format(Parameter("Q",0.8m))=="0.8"&&Format(Parameter("Gain",-3.5m,"dB"))=="-3.5 dB"&&Format(Parameter("Cut",0,"Hz",true))=="Off"&&Format(Parameter("Pre Delay (ms)",25))=="25 ms","Units, signed decimals or Off formatting differs");
            var snapshot=new CurrentPresetSnapshot(1,"British J800 Clean","00","00",own);
            var fileName=(string)typeof(MainWindow).GetMethod("CreateSnapshotFileName",flags)!.Invoke(null,[snapshot])!;
            Require(fileName=="Preset-0A-British-J800-Clean-snapshot.json","Default snapshot filename differs");
            var badName=snapshot with {Name="Bad / name:*?"};
            Require((string)typeof(MainWindow).GetMethod("CreateSnapshotFileName",flags)!.Invoke(null,[badName])! == "Preset-0A-Bad-name-snapshot.json","Invalid filename characters retained");
            var preset=new DecodedPreset(1,"British J800 Clean",own,Array.Empty<byte>());
            var file=new SnapshotArchive("V1.1.9",DateTimeOffset.UtcNow,[],[],preset);
            var changedValues=amp.Values.ToArray();changedValues[3]=48;
            var current=file with {Preset=new DecodedPreset(4,"British Lead Solo",new[]{dyna with {Position=1},movedAmp with {Values=changedValues}},Array.Empty<byte>())};
            var result=Activator.CreateInstance(type,flags|BindingFlags.Public,null,[new SnapshotComparisonResult(false,true,Array.Empty<SnapshotDifference>()),file,current,"Preset-0A-British-J800-Clean-snapshot.json",false],null)!;
            var dialog=(Window)typeof(MainWindow).GetMethod("CreateComparisonWindow",flags)!.Invoke(window,[result])!;
            try
            {
                IEnumerable<DependencyObject> Walk(DependencyObject node)
                {
                    yield return node;
                    foreach(var child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>())foreach(var desc in Walk(child))yield return desc;
                }
                var nodes=Walk((DependencyObject)dialog.Content).ToArray();var texts=nodes.OfType<TextBlock>().ToArray();
                foreach(var expected in new[]{"SAVED SNAPSHOT","CURRENT PRESET · LAST READ","0A · British J800 Clean","0D · British Lead Solo","Gain:","Bass:","Threshold:","Position 2 in the other preset"})
                    Require(texts.Any(t=>t.Text==expected),"Missing readable information: "+expected);
                Require(texts.Any(t=>t.Text=="48"&&t.FontWeight==FontWeights.Bold),"Different parameter value not highlighted");
                Require(nodes.OfType<ScrollViewer>().Count()==2,"Each preset must scroll independently");
                Require(nodes.OfType<Expander>().Single().IsExpanded==false,"Technical fields exposed by default");
                var eq=new PresetEffectBlock(1,9,new ushort[]{1,1,32,100,8,32,500,8,32,1600,8,32,6000,8,0,0,50});
                var grouped=file with {Preset=new DecodedPreset(1,"Custom EQ",new[]{eq},Array.Empty<byte>())};
                var column=(DependencyObject)typeof(MainWindow).GetMethod("CreateComparisonPresetColumn",flags)!.Invoke(window,["EQ",grouped,file,null!])!;
                var groupedTexts=Walk(column).OfType<TextBlock>().Select(t=>t.Text).ToArray();
                foreach(var group in new[]{"Low","Low Mid","High Mid","High"})Require(groupedTexts.Contains(group),"Missing EQ section "+group);
                var root=(FrameworkElement)dialog.Content;
                foreach(var size in new[]{new Size(1080,780),new Size(620,400)})
                {
                    root.Measure(size);root.Arrange(new Rect(size));root.UpdateLayout();
                    foreach(var scroll in nodes.OfType<ScrollViewer>())Require(scroll.ActualWidth>200&&scroll.ActualHeight>0,"Comparison columns collapsed");
                }
                var renderSize=new Size(1080,780);root.Measure(renderSize);root.Arrange(new Rect(renderSize));root.UpdateLayout();
                var bitmap=new RenderTargetBitmap(1080,780,96,96,PixelFormats.Pbgra32);bitmap.Render(root);
                Directory.CreateDirectory("analysis/private");using var output=File.Create("analysis/private/snapshot-comparison-1.47.0.png");
                var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));encoder.Save(output);
            }
            finally{dialog.Close();}
            Console.WriteLine("PASS: readable independent chains, reordered duplicate model matching, changed-value highlighting, scrolling, collapsed technical details, two viewport sizes and safe preset filenames. Own dialog rendered offline; no device commands.");
        }
        finally{window.Close();}
    }
}
