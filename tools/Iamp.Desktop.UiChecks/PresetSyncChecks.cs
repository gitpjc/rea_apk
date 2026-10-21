using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Iamp.Desktop;
using Iamp.Protocol;
using Iamp.Transport.Windows;

internal static class PresetSyncChecks
{
    private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    public static void Run()
    {
        var window=new MainWindow();
        try {
            foreach(var action in new[]{"Cancel","Recall","Close"}){
                var dialog=(Window)typeof(MainWindow).GetMethod("CreatePresetRecallDialog",Flags)!.Invoke(window,[new PresetName(4,"British Lead Solo")])!;
                var cancel=(Button)dialog.FindName("PresetRecallCancel");var recall=(Button)dialog.FindName("PresetRecallConfirm");
                if(dialog.Background!=window.Background||!cancel.IsCancel||!cancel.IsDefault||recall.IsDefault)
                    throw new InvalidOperationException("Recall confirmation styling or safe default differs");
                dialog.Loaded+=(_,_)=>dialog.Dispatcher.BeginInvoke(()=>{
                    if(action=="Cancel"){
                        var content=(FrameworkElement)dialog.Content;content.UpdateLayout();
                        var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(content.ActualWidth),(int)Math.Ceiling(content.ActualHeight),96,96,System.Windows.Media.PixelFormats.Pbgra32);bitmap.Render(content);
                        var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
                        System.IO.Directory.CreateDirectory("analysis/private");using var output=System.IO.File.Create("analysis/private/recall-dialog-1.50.13.png");encoder.Save(output);
                    }
                    if(action=="Close")dialog.Close();
                    else (action=="Recall"?recall:cancel).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                });
                if((dialog.ShowDialog()==true)!=(action=="Recall"))throw new InvalidOperationException("Recall confirmation result differs for "+action);
            }
            void Set(string n,object value)=>typeof(MainWindow).GetField(n,Flags)!.SetValue(window,value);
            T Field<T>(string n)=>(T)typeof(MainWindow).GetField(n,Flags)!.GetValue(window)!;
            void Filter()=>typeof(MainWindow).GetMethod("Filter",Flags)!.Invoke(window,null);
            void Require(bool passed,string error){if(!passed)throw new InvalidOperationException(error);}
            CurrentPresetSnapshot Current(byte index,string name)=>new(index,name,"00","00",Array.Empty<PresetEffectBlock>());
            Set("names",new PresetName[]{new(3,"British Rhythm"),new(4,"British Lead Solo")});
            Set("currentSnapshot",Current(4,"British Lead Solo"));Filter();
            var dropdown=Field<ComboBox>("presets");
            Require((dropdown.SelectedItem as PresetName)?.WireIndex==4,"Initial current preset differs");
            Set("currentSnapshot",Current(3,"British Rhythm"));Filter();
            Require((dropdown.SelectedItem as PresetName)?.WireIndex==3,"Closed dropdown retained old preset after external change/refresh");
            Require(dropdown.Items.OfType<PresetName>().Single(x=>x.IsCurrent).WireIndex==3,"Active check differs from current field");
            var search=Field<TextBox>("search");search.Text="Lead";
            Require(dropdown.SelectedItem is null,"Filtering out the active preset must not show a different preset as current");
            search.Text="";
            Require((dropdown.SelectedItem as PresetName)?.WireIndex==3,"Clearing search did not restore the current preset");
            window.Show();window.UpdateLayout();dropdown.IsDropDownOpen=true;
            Require(dropdown.IsDropDownOpen,"Popup did not open for the browsing regression");
            dropdown.SelectedItem=dropdown.Items.OfType<PresetName>().Single(x=>x.WireIndex==4);
            Filter();
            Require((dropdown.SelectedItem as PresetName)?.WireIndex==4,"Popup browsing choice was replaced by polling");
            // Cancel the popup before closing it so the offline check cannot request a recall.
            Set("cancelPresetDropdown",true);dropdown.IsDropDownOpen=false;
            Set("currentSnapshot",Current(4,"British Lead Solo"));Filter();
            Require((dropdown.SelectedItem as PresetName)?.WireIndex==4,"Return to original preset did not update the closed field");
            Require(dropdown.Items.OfType<PresetName>().Single(x=>x.IsCurrent).WireIndex==4,"Return check marker differs");
            Console.WriteLine("Preset synchronization UI checks passed: styled recall confirm/cancel/close and safe default; external changes, active marker, filtering, popup browsing and return.");
        }finally{window.Close();}
    }
}
