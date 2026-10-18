using System.Globalization;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Input;
using System.Windows.Media;
using Iamp.Protocol;

namespace Iamp.Desktop;

public sealed class DrumTempoDial : FrameworkElement
{
    private int tempo = 100;
    public event Action<int>? TempoChanged;
    public event Action? Tapped;
    public bool IsDragging => IsMouseCaptured;
    public int Tempo
    {
        get => tempo;
        set { tempo = Math.Clamp(value, DrumSettings.MinimumTempo, DrumSettings.MaximumTempo); InvalidateVisual(); }
    }
    public DrumTempoDial()
    {
        Width = Height = 320; Focusable = true;
        Cursor = Cursors.Hand;
        System.Windows.Automation.AutomationProperties.SetName(this, "Drum tempo in BPM; arrows adjust; Enter taps tempo");
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new DialPeer(this);
    protected override void OnRender(DrawingContext dc)
    {
        var center = new Point(160,160);
        var blue = new SolidColorBrush(Color.FromRgb(81,169,255));
        var gray = new SolidColorBrush(Color.FromRgb(128,132,140));
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(38,42,45)), new Pen(new SolidColorBrush(Color.FromRgb(48,53,58)),8),center,146,146);
        var fraction = (Tempo - 40) / 220d;
        for (var i=0;i<60;i++)
        {
            var a=Math.PI/2+i*Math.PI*2/60;
            Point At(double r)=>new(160+Math.Cos(a)*r,160+Math.Sin(a)*r);
            dc.DrawLine(new Pen(i/60d<fraction?blue:gray,3){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round},At(116),At(131));
        }
        var angle=Math.PI/2+fraction*Math.PI*2;
        var thumb=new Point(160+Math.Cos(angle)*124,160+Math.Sin(angle)*124);
        dc.DrawEllipse(Brushes.White,null,thumb,18,18);dc.DrawEllipse(blue,null,thumb,5,5);
        void Label(string text,int size,double y,Brush color)
        {
            var formatted=new FormattedText(text,CultureInfo.CurrentCulture,FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),size,color,VisualTreeHelper.GetDpi(this).PixelsPerDip);
            dc.DrawText(formatted,new Point(160-formatted.Width/2,y));
        }
        Label("TAP",22,99,Brushes.White);Label(Tempo.ToString(CultureInfo.InvariantCulture),40,142,Brushes.White);Label("BPM",18,197,gray);
    }
    private void SetFromPoint(Point p)
    {
        var a=Math.Atan2(p.Y-160,p.X-160)-Math.PI/2;
        if(a<0)a+=Math.PI*2;
        SetUserTempo(40+(int)Math.Round(a/(Math.PI*2)*220));
    }
    private void SetUserTempo(int value)
    {
        var next=Math.Clamp(value,40,260);if(next==Tempo)return;
        Tempo=next;TempoChanged?.Invoke(next);
    }
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);if(!IsEnabled)return;Focus();
        var p=e.GetPosition(this);var distance=(p-new Point(160,160)).Length;
        if(distance<90){Tapped?.Invoke();e.Handled=true;return;}
        if(distance is < 105 or > 155)return;
        CaptureMouse();SetFromPoint(p);e.Handled=true;
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {base.OnMouseMove(e);if(IsMouseCaptured)SetFromPoint(e.GetPosition(this));}
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {base.OnMouseLeftButtonUp(e);if(IsMouseCaptured){SetFromPoint(e.GetPosition(this));ReleaseMouseCapture();e.Handled=true;}}
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if(e.Key is Key.Up or Key.Right)SetUserTempo(Tempo+1);
        else if(e.Key is Key.Down or Key.Left)SetUserTempo(Tempo-1);
        else if(e.Key==Key.Home)SetUserTempo(40);
        else if(e.Key==Key.End)SetUserTempo(260);
        else if(e.Key is Key.Enter or Key.Space)Tapped?.Invoke();
        else return;
        e.Handled=true;
    }
    private sealed class DialPeer(DrumTempoDial dial) : FrameworkElementAutomationPeer(dial), IRangeValueProvider
    {
        protected override string GetClassNameCore()=>nameof(DrumTempoDial);
        protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.Slider;
        public override object? GetPattern(PatternInterface p)=>p==PatternInterface.RangeValue?this:base.GetPattern(p);
        bool IRangeValueProvider.IsReadOnly=>!dial.IsEnabled;
        double IRangeValueProvider.LargeChange=>10;
        double IRangeValueProvider.SmallChange=>1;
        double IRangeValueProvider.Maximum=>260;
        double IRangeValueProvider.Minimum=>40;
        double IRangeValueProvider.Value=>dial.Tempo;
        void IRangeValueProvider.SetValue(double value)
        {
            if(!dial.IsEnabled)throw new InvalidOperationException("Drum is unavailable.");
            if(!double.IsFinite(value)||value<40||value>260)throw new ArgumentOutOfRangeException(nameof(value));
            dial.SetUserTempo((int)Math.Round(value));
        }
    }
}
