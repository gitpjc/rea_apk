using System.Reflection;
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
            Console.WriteLine("Preset synchronization UI checks passed: external changes, active marker, filtering, popup browsing and return.");
        }finally{window.Close();}
    }
}
