using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Iamp.Desktop;

public sealed partial class MainWindow
{
    private readonly HashSet<Control> steadyControls=new();
    private string? steadyOperationMessage;
    private double? previousSaveMessageMinHeight;
    private void PrepareSteadyAppearance()
    {
        if(steadyControls.Count>0)return;
        var seen=new HashSet<DependencyObject>();
        void Visit(DependencyObject node)
        {
            if(!seen.Add(node))return;
            if(node is Control control&&control.IsEnabled){steadyControls.Add(control);SteadyControlAppearance.SetKeepEnabledAppearance(control,true);}
            if(node is Visual)for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)Visit(VisualTreeHelper.GetChild(node,i));
            foreach(var child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>())Visit(child);
        }
        Visit(this);
    }
    private void FinishSteadyAppearance()
    {
        foreach(var control in steadyControls)SteadyControlAppearance.SetKeepEnabledAppearance(control,false);
        steadyControls.Clear();
    }
    private void BeginSteadyOperation(string message,bool loadingPreset=false)
    {
        PrepareSteadyAppearance();steadyOperationMessage=loadingPreset?"Loading preset…":"Applying…";
        previousSaveMessageMinHeight=saveMessage.MinHeight;
        saveMessage.MinHeight=Math.Max(saveMessage.MinHeight,saveMessage.ActualHeight);
        saveMessage.Foreground=BlueBrush;
        Begin(message);
    }
    private void FinishSteadyOperation()
    {
        steadyOperationMessage=null;
        if(previousSaveMessageMinHeight is {} height){saveMessage.MinHeight=height;previousSaveMessageMinHeight=null;}
        saveMessage.Foreground=MutedBrush;FinishSteadyAppearance();
    }
}
