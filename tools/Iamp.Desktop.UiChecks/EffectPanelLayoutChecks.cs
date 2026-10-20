using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using Iamp.Desktop;
using Iamp.Protocol;
using Iamp.Transport.Windows;

internal static class EffectPanelLayoutChecks
{
    public static void Run()
    {
        const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
        void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        var window=new MainWindow();
        T Field<T>(string n)=>(T)typeof(MainWindow).GetField(n,flags)!.GetValue(window)!;
        void Set(string n,object? v)=>typeof(MainWindow).GetField(n,flags)!.SetValue(window,v);
        object? Call(string n,params object[] a)=>typeof(MainWindow).GetMethod(n,flags)!.Invoke(window,a);
        try
        {
            var blocks=new PresetEffectBlock[]{new(1,4,new ushort[]{1,2,25,50,0,0}),new(2,2,new ushort[]{1,31,0,47,44,35,51,75,1}),new(3,9,new ushort[]{1,1,32,100,8,32,500,8,32,1600,8,32,6000,8,0,0,62})};
            var original=new CurrentPresetSnapshot(1,"Offline effect panel","00","00",blocks);
            Set("currentSnapshot",original);Set("snapshotAddress",(ulong)1);Set("snapshotFirmware","V1.1.9");Set("editSnapshotValid",true);Call("SetEffects",(object)blocks);
            var detail=Field<StackPanel>("effectDetails");var area=Field<StackPanel>("effectSelectionDetails");var scroll=Field<ScrollViewer>("effectParameterScroll");var heading=Field<WrapPanel>("effectHeading");
            Require(ReferenceEquals(scroll.Content,detail)&&area.Children.Contains(scroll),"Parameter section is not in its own scroll viewer");
            Require(Field<Button>("effectPowerButton").Parent is WrapPanel powerRow&&ReferenceEquals(powerRow.Parent,heading),"Power state/control are not beside the effect title");
            Require((string)Field<Button>("changeEffectModel").Content=="Change"&&(string)Field<Button>("moveEffectLeft").Content=="← Move left"&&(string)Field<Button>("moveEffectRight").Content=="Move right →"&&(string)Field<Button>("removeChainEffect").Content=="Remove"&&(string)Field<Button>("effectPowerButton").Content=="Disable","Effect action labels differ");
            foreach(var name in new[]{"changeEffectModel","moveEffectLeft","moveEffectRight","removeChainEffect"})Require(ReferenceEquals(Field<Button>(name).Parent,heading),"Effect action is not beside the power control");
            TextBlock PowerState()=>((WrapPanel)Field<Button>("effectPowerButton").Parent).Children.OfType<TextBlock>().Single();
            Require(PowerState().Text=="Enabled"&&((SolidColorBrush)PowerState().Foreground).Color.ToString()=="#FF4C9EFF","Enabled state is not blue");
            var offValues=blocks[0].Values.ToArray();offValues[0]=0;
            var offBlocks=blocks.ToArray();offBlocks[0]=offBlocks[0] with {Values=offValues};
            Call("SetEffects",(object)offBlocks);
            Require((string)Field<Button>("effectPowerButton").Content=="Enable"&&PowerState().Text=="Disabled"&&((SolidColorBrush)PowerState().Foreground).Color.ToString()=="#FFFF5C5C","Disabled effect does not offer Enable with red Disabled status");
            Call("SetEffects",(object)blocks);
            Require((string)Field<Button>("effectPowerButton").Content=="Disable"&&PowerState().Text=="Enabled","Restoring an enabled effect did not restore Disable action and Enabled status");
            Field<TextBox>("thresholdInput").Text="26";
            var buttons=Field<WrapPanel>("effectChain");((Button)buttons.Children[1]).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var root=(FrameworkElement)window.Content;
            foreach(var size in new[]{new Size(1280,900),new Size(911,512),new Size(620,320)})
            {
                root.Width=size.Width;root.Height=size.Height;
                for(var pass=0;pass<8;pass++){root.Measure(size);root.Arrange(new Rect(size));root.UpdateLayout();}
                if(size.Width==1280){
                    var title=(TextBlock)heading.Children[0];
                    var titleCentre=title.TranslatePoint(new Point(0,title.ActualHeight/2),heading).Y;
                    foreach(var name in new[]{"effectPowerButton","changeEffectModel","moveEffectLeft","moveEffectRight","removeChainEffect"}){
                        var control=Field<Button>(name);var centre=control.TranslatePoint(new Point(0,control.ActualHeight/2),heading).Y;
                        Require(Math.Abs(titleCentre-centre)<1,"Effect name is not vertically centred with its controls");
                    }
                }
                Require(scroll.ScrollableHeight>0&&scroll.ViewportHeight>0&&scroll.ViewportHeight<=scroll.MaxHeight+1,"Long parameter list is not independently scrollable");
                var before=heading.TranslatePoint(new Point(),area);scroll.ScrollToEnd();root.UpdateLayout();
                Require(scroll.VerticalOffset>0&&heading.TranslatePoint(new Point(),area)==before,"Scrolling parameters moved the effect heading");
                foreach(var control in new FrameworkElement[]{Field<Button>("effectPowerButton"),Field<Button>("changeEffectModel"),Field<Button>("moveEffectLeft"),Field<Button>("moveEffectRight"),Field<Button>("removeChainEffect")})
                {
                    var point=control.TranslatePoint(new Point(),area);Require(point.X>=0&&point.X+control.ActualWidth<=area.ActualWidth+1,"Effect action overflows compact layout");
                }
            }
            ((Button)buttons.Children[2]).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var compactSize=new Size(911,512);root.Width=compactSize.Width;root.Height=compactSize.Height;
            for(var pass=0;pass<4;pass++){root.Measure(compactSize);root.Arrange(new Rect(compactSize));root.UpdateLayout();}
            var outer=Field<ScrollViewer>("libraryScroll");outer.ScrollToEnd();scroll.ScrollToEnd();root.UpdateLayout();var outerBeforeWheel=outer.VerticalOffset;
            var wheel=new MouseWheelEventArgs(Mouse.PrimaryDevice,Environment.TickCount,-120){RoutedEvent=Mouse.PreviewMouseWheelEvent};scroll.RaiseEvent(wheel);root.UpdateLayout();
            Require(wheel.Handled&&outer.VerticalOffset==outerBeforeWheel,"Wheel scrolling at the inner bottom moved the outer page");
            outer.ScrollToEnd();scroll.ScrollToEnd();root.UpdateLayout();
            IEnumerable<DependencyObject> Walk(DependencyObject node){yield return node;foreach(var child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>())foreach(var item in Walk(child))yield return item;}
            var lastInput=Walk(detail).OfType<TextBox>().Single(t=>t.Text=="62");var lastPoint=lastInput.TranslatePoint(new Point(),scroll);var outerPoint=lastInput.TranslatePoint(new Point(),outer);
            Require(lastPoint.Y>=0&&lastPoint.Y+lastInput.ActualHeight<=scroll.ViewportHeight+1&&outerPoint.Y>=0&&outerPoint.Y+lastInput.ActualHeight<=outer.ViewportHeight+1,"Final Custom EQ Level input remains clipped at the bottom");
            IEnumerable<DependencyObject> Visuals(DependencyObject node){yield return node;for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var item in Visuals(VisualTreeHelper.GetChild(node,i)))yield return item;}
            var scrollbar=Visuals(scroll).OfType<ScrollBar>().Single(b=>b.Orientation==Orientation.Vertical&&b.Visibility==Visibility.Visible);
            Require(scrollbar.ActualWidth==14&&((SolidColorBrush)scrollbar.Background).Color.ToString()=="#FF20242B","Parameter scrollbar does not use the compact dark theme");
            scroll.ScrollToTop();root.UpdateLayout();var track=(Track)scrollbar.Template.FindName("PART_Track",scrollbar);
            track.Thumb.RaiseEvent(new DragStartedEventArgs(0,0){RoutedEvent=Thumb.DragStartedEvent});
            track.Thumb.RaiseEvent(new DragDeltaEventArgs(0,25){RoutedEvent=Thumb.DragDeltaEvent});
            track.Thumb.RaiseEvent(new DragCompletedEventArgs(0,25,false){RoutedEvent=Thumb.DragCompletedEvent});root.UpdateLayout();
            Require(scroll.VerticalOffset>0,"Styled scrollbar thumb cannot scroll the parameter list");
            var horizontal=new ScrollBar{Orientation=Orientation.Horizontal,Minimum=0,Maximum=100,ViewportSize=20,Style=(Style)Application.Current.FindResource(typeof(ScrollBar))};horizontal.ApplyTemplate();horizontal.Measure(new Size(200,14));horizontal.Arrange(new Rect(0,0,200,14));horizontal.UpdateLayout();
            var horizontalTrack=(Track)horizontal.Template.FindName("PART_Track",horizontal);
            Require(horizontal.ActualHeight==14&&!horizontalTrack.IsDirectionReversed&&horizontalTrack.DecreaseRepeatButton.Command==ScrollBar.PageLeftCommand&&horizontalTrack.IncreaseRepeatButton.Command==ScrollBar.PageRightCommand,"Horizontal dark scrollbar layout/commands differ");
            outer.ScrollToEnd();scroll.ScrollToEnd();root.UpdateLayout();
            var eqBitmap=new RenderTargetBitmap(911,512,96,96,PixelFormats.Pbgra32);eqBitmap.Render(root);var eqEncoder=new PngBitmapEncoder();eqEncoder.Frames.Add(BitmapFrame.Create(eqBitmap));Directory.CreateDirectory("analysis/private");using(var output=File.Create("analysis/private/custom-eq-scroll-end-1.49.16.png"))eqEncoder.Save(output);
            scroll.ScrollToTop();root.UpdateLayout();var previousOuterOffset=outer.VerticalOffset;
            scroll.RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice,Environment.TickCount,120){RoutedEvent=Mouse.PreviewMouseWheelEvent});root.UpdateLayout();
            Require(outer.VerticalOffset==previousOuterOffset,"Wheel scrolling at the inner top moved the outer page");
            var fullSize=new Size(1280,900);root.Width=fullSize.Width;root.Height=fullSize.Height;
            for(var pass=0;pass<8;pass++){root.Measure(fullSize);root.Arrange(new Rect(fullSize));root.UpdateLayout();}
            outer.ScrollToTop();scroll.ScrollToEnd();root.UpdateLayout();
            Require(outer.ScrollableHeight<1,"Normal window needs outer scrolling to reach effect parameters");
            var finalPoint=lastInput.TranslatePoint(new Point(),outer);var finalMessage=detail.Children.OfType<TextBlock>().Last();var messagePoint=finalMessage.TranslatePoint(new Point(),outer);
            Require(finalPoint.Y>=0&&finalPoint.Y+lastInput.ActualHeight<=outer.ViewportHeight+1&&messagePoint.Y+finalMessage.ActualHeight<=outer.ViewportHeight+1,"Last parameter/helper text is clipped without scrolling the outer page");
            var fittedBitmap=new RenderTargetBitmap(1280,900,96,96,PixelFormats.Pbgra32);fittedBitmap.Render(root);var fittedEncoder=new PngBitmapEncoder();fittedEncoder.Frames.Add(BitmapFrame.Create(fittedBitmap));using(var output=File.Create("analysis/private/custom-eq-fitted-1.49.21.png"))fittedEncoder.Save(output);
            ((Button)buttons.Children[0]).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));root.UpdateLayout();
            Require(Field<TextBox>("thresholdInput").Text=="26"&&ReferenceEquals(original,Field<CurrentPresetSnapshot>("currentSnapshot")),"Changing effect panels discarded pending inputs or changed the preset");
            Field<TextBox>("thresholdInput").Text="25";
            var renderSize=new Size(1280,900);root.Width=renderSize.Width;root.Height=renderSize.Height;
            for(var pass=0;pass<4;pass++){root.Measure(renderSize);root.Arrange(new Rect(renderSize));root.UpdateLayout();}
            Field<ScrollViewer>("libraryScroll").ScrollToBottom();root.UpdateLayout();
            var bitmap=new RenderTargetBitmap(1280,900,96,96,PixelFormats.Pbgra32);bitmap.Render(root);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));Directory.CreateDirectory("analysis/private");using(var output=File.Create("analysis/private/effect-panel-1.49.15.png"))encoder.Save(output);
            Require(Field<F15ReadSession?>("connectedSession") is null&&Field<CancellationTokenSource?>("operation") is null,"Layout check started device work");
            Console.WriteLine("PASS: fixed effect controls, independently sized parameters, Custom EQ final Level/helper visible without outer scrolling at normal size, no outer wheel movement at either boundary, dark scrollbar dragging, compact layouts and pending-input preservation. No hardware commands.");
        }
        finally{window.Close();}
    }
}
