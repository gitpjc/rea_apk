using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Iamp.Transport.Windows;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    private CurrentPresetSnapshot? presetEntryBaseline;
    private ulong? presetEntryAddress;
    private string? presetEntryFirmware;
    private readonly Border unsavedChangesNotice=new(){Visibility=Visibility.Collapsed,VerticalAlignment=VerticalAlignment.Center,
        Background=Brush("#3A2818"),BorderBrush=Brush("#FFB454"),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(4),
        Padding=new Thickness(10,5,10,5),Margin=new Thickness(0,4,0,4)};

    private void InitializeUnsavedChangesNotice(WrapPanel heading)
    {
        var label=Text("Unsaved changes exist",16,Brush("#FFB454"));label.FontWeight=FontWeights.Bold;
        unsavedChangesNotice.Child=label;
        var title=(TextBlock)heading.Children[0];
        var labelOffset=new TranslateTransform();label.RenderTransform=labelOffset;
        var badgeOffset=new TranslateTransform();unsavedChangesNotice.RenderTransform=badgeOffset;
        // Font line boxes include unequal empty space above and below the visible glyphs.
        // Align the ink centres, moving the whole badge independently of its text.
        void AlignTextCentres()
        {
            if(title.ActualHeight<=0||label.ActualHeight<=0||unsavedChangesNotice.ActualHeight<=0)return;
            double InkCentre(TextBlock text)
            {
                var formatted=new FormattedText(text.Text,System.Globalization.CultureInfo.CurrentUICulture,text.FlowDirection,
                    new Typeface(text.FontFamily,text.FontStyle,text.FontWeight,text.FontStretch),text.FontSize,text.Foreground,
                    VisualTreeHelper.GetDpi(text).PixelsPerDip);
                var bounds=formatted.BuildGeometry(new Point()).Bounds;
                return bounds.Top+bounds.Height/2;
            }
            labelOffset.Y=label.ActualHeight/2-InkCentre(label);
            var titleOrigin=VisualTreeHelper.GetOffset(title);
            var badgeOrigin=VisualTreeHelper.GetOffset(unsavedChangesNotice);
            badgeOffset.Y=badgeOrigin.X>titleOrigin.X
                ? titleOrigin.Y+InkCentre(title)-badgeOrigin.Y-unsavedChangesNotice.ActualHeight/2 : 0;
        }
        heading.LayoutUpdated+=(_,_)=>AlignTextCentres();
        System.Windows.Automation.AutomationProperties.SetName(unsavedChangesNotice,"Unsaved changes exist");
        heading.Children.Add(unsavedChangesNotice);
        Closed+=(_,_)=>unsavedChangesNotice.BeginAnimation(OpacityProperty,null);
    }

    private void UpdateUnsavedChangesNotice()
    {
        if(!preview&&!localSnapshotSource&&currentSnapshot is {} current&&
            (presetEntryBaseline is null||presetEntryBaseline.WireIndex!=current.WireIndex||presetEntryAddress!=snapshotAddress||presetEntryFirmware!=snapshotFirmware))
            ResetPresetEntryBaseline(current);
        var show=!preview&&!localSnapshotSource&&currentSnapshot is not null&&
            (HasPresetStateChanges()||HasPendingPresetInputs());
        unsavedChangesNotice.Visibility=show?Visibility.Visible:Visibility.Collapsed;
        if(show){
            if(!unsavedChangesNotice.HasAnimatedProperties)
                unsavedChangesNotice.BeginAnimation(OpacityProperty,new DoubleAnimation(1,0.35,TimeSpan.FromMilliseconds(650)){
                    AutoReverse=true,RepeatBehavior=RepeatBehavior.Forever});
        }else{
            unsavedChangesNotice.BeginAnimation(OpacityProperty,null);unsavedChangesNotice.Opacity=1;
        }
    }

    private void ResetPresetEntryBaseline(CurrentPresetSnapshot preset)
    {
        presetEntryBaseline=PresetEditState.Freeze(preset);presetEntryAddress=snapshotAddress;presetEntryFirmware=snapshotFirmware;
    }

    private bool HasPresetStateChanges()=>presetEntryBaseline is not null&&currentSnapshot is not null&&
        presetEntryBaseline.WireIndex==currentSnapshot.WireIndex&&presetEntryAddress==snapshotAddress&&presetEntryFirmware==snapshotFirmware&&
        !PresetEditState.Equivalent(presetEntryBaseline,currentSnapshot);
}
