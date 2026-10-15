using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Iamp.Desktop;
using Iamp.Protocol;
using Iamp.Transport.Windows;

internal static class EffectChainUiChecks
{
    private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    private const string Detail="220442726974697368204C65616420536F6C6F00000001040C01000200190032000000000002010A0100030027006400640003021201001F00640023002B0028002D004B0001000407100100010011001900F40100000000000005081401000100140018002300000000000000000000000600000700000800000900000A00000B00000C00000D00000E00000F00001000001100001200001300001400000000";
    private static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,Flags)!.GetValue(target)!;
    private static T Property<T>(object target,string name)=>(T)target.GetType().GetProperty(name,Flags)!.GetValue(target)!;
    private static void Set(object target,string name,object? value)=>target.GetType().GetField(name,Flags)!.SetValue(target,value);
    private static object? Call(object target,string name,params object?[] args)=>target.GetType().GetMethod(name,Flags)!.Invoke(target,args);
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}

    internal static void Run()
    {
        var window=new MainWindow();
        try {
            var raw=Convert.FromHexString(Detail);var blocks=PresetDataParser.Parse(raw).Blocks;
            var snapshot=new CurrentPresetSnapshot(4,"British Lead Solo","00",Detail,blocks);
            void Load(CurrentPresetSnapshot value) {
                Set(window,"currentSnapshot",value);Set(window,"snapshotAddress",(ulong)1);Set(window,"snapshotFirmware","V1.1.9");Set(window,"editSnapshotValid",true);
                Set(window,"operation",null);Set(window,"polling",false);Call(window,"SetEffects",(object)value.Blocks);
            }
            void Select(int position)=>((Button)Field<WrapPanel>(window,"effectChain").Children[position-1]).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            void Update()=>Call(window,"UpdateChainControls");
            Button Add()=>Field<Button>(window,"addChainEffect");
            Button Left()=>Field<Button>(window,"moveEffectLeft");
            Button Right()=>Field<Button>(window,"moveEffectRight");
            Button Remove()=>Field<Button>(window,"removeChainEffect");
            void NoWork(Task task) {
                Require(task.IsCompletedSuccessfully,"A guarded chain action started asynchronous device work.");
                Require(Field<F15ReadSession?>(window,"connectedSession") is null,"A guarded chain action acquired a device session.");
            }
            void DisabledGuard(string label) {
                Update();Require(!Add().IsEnabled && !Left().IsEnabled && !Right().IsEnabled && !Remove().IsEnabled,$"Chain controls enabled during {label}.");
                var operation=Field<CancellationTokenSource?>(window,"operation");
                NoWork((Task)Call(window,"MoveDisplayedEffectAsync",1)!);
                NoWork((Task)Call(window,"ChangeEffectChainAsync","Offline guard",(byte)1,(byte?)2,null)!);
                NoWork((Task)Call(window,"ChangeEffectChainAsync","Offline guard",(byte)1,null,null)!);
                NoWork((Task)Call(window,"ChangeEffectChainAsync","Offline guard",(byte)0,null,PresetEffectSearch.Catalog.First(e=>e.TypeCode==5))!);
                Call(window,"ShowAddEffectPicker"); // A disabled guard must return before constructing a modal window.
                Require(ReferenceEquals(operation,Field<CancellationTokenSource?>(window,"operation")),"A guarded chain action changed operation state.");
            }

            Load(snapshot);Select(1);
            CheckPicker(window,snapshot);
            CheckInsertion(window,snapshot);
            Require(Add().IsEnabled && !Left().IsEnabled && Right().IsEnabled && Remove().IsEnabled,"First effect move boundaries or + availability differ.");
            Require(Field<WrapPanel>(window,"effectChain").Children.Count==6,"The + button must follow the five active effects.");
            NoWork((Task)Call(window,"MoveDisplayedEffectAsync",-1)!);
            Select(5);Require(Left().IsEnabled && !Right().IsEnabled,"Last effect move boundaries differ.");NoWork((Task)Call(window,"MoveDisplayedEffectAsync",1)!);
            Select(3);Require(Left().IsEnabled && Right().IsEnabled,"A middle effect cannot move both ways.");
            NoWork((Task)Call(window,"ChangeEffectChainAsync","No-op",(byte)3,(byte?)3,null)!);

            Set(window,"snapshotAddress",null);DisabledGuard("offline snapshot");Set(window,"snapshotAddress",(ulong)1);
            Set(window,"snapshotFirmware","V1.1.2");DisabledGuard("unsupported firmware");Set(window,"snapshotFirmware","V1.1.9");
            Set(window,"editSnapshotValid",false);DisabledGuard("invalid snapshot");Set(window,"editSnapshotValid",true);
            using(var busy=new CancellationTokenSource()){Set(window,"operation",busy);DisabledGuard("busy operation");Set(window,"operation",null);}
            Update();var stable=new[]{Add().IsEnabled,Left().IsEnabled,Right().IsEnabled,Remove().IsEnabled};
            for(var cycle=0;cycle<20;cycle++) {
                Set(window,"polling",true);Update();
                Require(new[]{Add().IsEnabled,Left().IsEnabled,Right().IsEnabled,Remove().IsEnabled}.SequenceEqual(stable),"Background read flickers chain controls.");
                Require(Property<bool>(window,"CanEditChain"),"Background read swallows an otherwise valid chain interaction.");
                Set(window,"polling",false);Update();
                Require(new[]{Add().IsEnabled,Left().IsEnabled,Right().IsEnabled,Remove().IsEnabled}.SequenceEqual(stable),"Background completion changes chain availability.");
            }
            Select(1);Field<TextBox>(window,"thresholdInput").Text="26";Select(3);DisabledGuard("hidden pending input");
            Select(1);Field<TextBox>(window,"thresholdInput").Text="";Select(3);DisabledGuard("hidden invalid pending input");
            Select(1);Field<TextBox>(window,"thresholdInput").Text="25";Update();Require(Add().IsEnabled,"Clearing pending edits did not restore chain actions.");
            var disabled=blocks.Select(b=>b.TypeCode==4?b with {Values=new ushort[]{0,2,25,50,0,0}}:b).ToArray();
            Load(snapshot with {Blocks=disabled});Require(Add().IsEnabled,"A disabled existing effect should not prevent adding another category.");
            CheckPicker(window,Field<CurrentPresetSnapshot>(window,"currentSnapshot"));

            for(byte from=1;from<=5;from++)for(byte to=1;to<=5;to++) {
                var moved=EffectChainCommands.ExpectedMove(raw,from,to);var movedBlocks=PresetDataParser.Parse(moved).Blocks;
                Load(snapshot with {Blocks=movedBlocks,RawDetailPayloadHex=Convert.ToHexString(moved)});
                Call(window,"SelectChangedChainEffect",from,(byte?)to,null);
                var selected=Property<PresetEffectBlock>(Field<object>(window,"displayedEffect"),"Source");
                Require(selected.Position==to && selected.TypeCode==blocks[from-1].TypeCode,"Reordered effect did not remain selected at its destination.");
                Require(Left().IsEnabled==(to>1) && Right().IsEnabled==(to<5),"Move button boundaries did not follow the reordered selection.");
            }
            foreach(byte position in new byte[]{1,3,5}) {
                var removed=EffectChainCommands.ExpectedRemove(raw,position);var remaining=PresetDataParser.Parse(removed).Blocks;
                Load(snapshot with {Blocks=remaining,RawDetailPayloadHex=Convert.ToHexString(removed)});
                Call(window,"SelectChangedChainEffect",position,null,null);
                Require(Property<PresetEffectBlock>(Field<object>(window,"displayedEffect"),"Source").Position==Math.Min((int)position,4),"Removal failed to select the following or final remaining effect.");
            }
            var added=blocks.ToArray();added[5]=new(6,5,new ushort[]{1,0,60,45,55,70});Load(snapshot with {Blocks=added});
            Call(window,"SelectChangedChainEffect",(byte)0,null,PresetEffectSearch.Catalog.First(e=>e.TypeCode==5));
            Require(Property<PresetEffectBlock>(Field<object>(window,"displayedEffect"),"Source").Position==6,"Newly added effect is not selected.");

            var allCategories=PresetEffectSearch.Catalog.DistinctBy(e=>e.TypeCode).Select((effect,index)=>new PresetEffectBlock((byte)(index+1),effect.TypeCode,new ushort[]{1,effect.Model})).ToList();
            for(var i=allCategories.Count;i<20;i++)allCategories.Add(new((byte)(i+1),0,Array.Empty<ushort>()));
            Load(snapshot with {Blocks=allCategories});Require(Add().IsEnabled,"The picker must remain browsable when every built-in category is already present.");
            var empty=Enumerable.Range(1,20).Select(i=>new PresetEffectBlock((byte)i,0,Array.Empty<ushort>())).ToArray();Load(snapshot with {Blocks=empty});
            Require(Add().IsEnabled && !Left().IsEnabled && !Right().IsEnabled && !Remove().IsEnabled,"An empty chain should allow adding but not moving or removing.");
            var duplicate=blocks.ToArray();duplicate[5]=duplicate[0] with {Position=6,Values=new ushort[]{1,2,15,50,0,0}};Load(snapshot with {Blocks=duplicate});
            Require(Add().IsEnabled && Remove().IsEnabled,"Repeated categories must remain editable.");
            Select(6);Require(Field<TextBox>(window,"thresholdInput").Text=="15","Second Noise Killer shows the first module's value.");
            Field<TextBox>(window,"thresholdInput").Text="16";Select(1);Require(Field<TextBox>(window,"thresholdInput").Text=="25","Pending values leaked into another identical model.");
            Require((bool)Call(window,"HasPendingInputs")!,"Hidden duplicate-module pending edit was lost.");
            Select(6);Require(Field<TextBox>(window,"thresholdInput").Text=="16","Second module's pending value was not retained.");
            Field<TextBox>(window,"thresholdInput").Text="15";Select(1);Require(!(bool)Call(window,"HasPendingInputs")!,"Restoring the second module's input left phantom pending state.");
            Field<TextBox>(window,"thresholdInput").Text="26";Select(6);Field<TextBox>(window,"thresholdInput").Text="16";
            Call(window,"SetEffectsPreservingPending",(object)duplicate);Select(1);Require(Field<TextBox>(window,"thresholdInput").Text=="26","Refresh lost first identical model's pending value.");
            Select(6);Require(Field<TextBox>(window,"thresholdInput").Text=="16","Refresh mixed identical model values.");
            Field<TextBox>(window,"thresholdInput").Text="15";Select(1);Field<TextBox>(window,"thresholdInput").Text="25";
            CheckPicker(window,Field<CurrentPresetSnapshot>(window,"currentSnapshot"));
            Load(snapshot with {Blocks=blocks.Take(5).ToArray()});DisabledGuard("incomplete structural snapshot");
            var authoritative=blocks.ToArray();authoritative[5]=new(6,2,new ushort[]{1,0,75,50,50,50,50,55,1});
            Call(window,"ShowUnconfirmedChain",snapshot with {Blocks=authoritative},(byte)0,null,PresetEffectSearch.Catalog.First(e=>e.TypeCode==2));
            Require(Field<WrapPanel>(window,"effectChain").Children.Count==7,"Unconfirmed readback left the old chain displayed.");
            Require(!Remove().IsEnabled && !Add().IsEnabled,"An unexpected outcome incorrectly claims editing is safe.");
            Call(window,"ShowUnconfirmedChain",snapshot,(byte)6,null,null);
            Require(Field<WrapPanel>(window,"effectChain").Children.Count==6,"Unconfirmed removal left a deleted effect displayed.");
            Console.WriteLine("PASS: all nine picker categories and 124 models, repeated-category/model addition and isolated pending values, GIR&IR explanation, insertion-gap mapping and marker geometry, move/add/remove selection and offline/busy/pending guards without device work.");
        }
        finally {Set(window,"operation",null);window.Close();}

        var preview=new MainWindow(true);
        try {
            Call(preview,"UpdateChainControls");
            Require(!Field<Button>(preview,"addChainEffect").IsEnabled,"Preview allows adding effects.");
            Require(((Task)Call(preview,"ChangeEffectChainAsync","Preview guard",(byte)1,(byte?)2,null)!).IsCompletedSuccessfully,"Preview chain action started device work.");
        }
        finally {preview.Close();}
    }
    private static T Named<T>(DependencyObject root,string name) where T:FrameworkElement
    {
        if(root is T match && match.Name==name)return match;
        foreach(var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>()) {
            try {return Named<T>(child,name);}catch(KeyNotFoundException){}
        }
        throw new KeyNotFoundException(name);
    }
    private static void CheckPicker(MainWindow window,CurrentPresetSnapshot snapshot)
    {
        var picker=(Window)Call(window,"CreateAddEffectPicker",snapshot)!;
        try {
            var categories=Named<ListBox>(picker,"EffectCategories");var models=Named<ListBox>(picker,"EffectModels");
            var add=Named<Button>(picker,"ConfirmAddEffect");var message=Named<TextBlock>(picker,"EffectPickerMessage");var search=Named<TextBox>(picker,"EffectModelSearch");
            Require(categories.Items.Cast<object>().Select(c=>Property<string>(c,"Name")).SequenceEqual(new[]{"Dyna","Filter","OD","Amp","GIR&IR","EQ","Mod","Delay","Reverb"}),"Mobile category order/list differs.");
            var counts=new[]{4,3,22,55,0,2,17,12,9};var total=0;
            for(var i=0;i<counts.Length;i++) {
                categories.SelectedIndex=i;Require(models.Items.Count==counts[i],"A category hides its models.");total+=models.Items.Count;
                if(models.Items.Count>0) {
                    models.SelectedIndex=0;var choice=(EffectDefinition)models.SelectedItem;
                    Require(add.IsEnabled,"Mobile-addable category is incorrectly disabled because it is already present.");
                    Require(!message.Text.Contains("already in this chain"),"Unverified one-category restriction remains visible.");
                }else Require(!add.IsEnabled && message.Text.Contains("not available"),"GIR&IR lacks an honest unavailable state.");
            }
            Require(total==124,"Picker must retain the complete built-in catalog.");
            categories.SelectedIndex=1;search.Text="Touch";Require(models.Items.Count==1 && ((EffectDefinition)models.Items[0]).Name=="Touch Wah","Picker search failed.");
            search.Text="no-such-model";Require(models.Items.Count==0 && !add.IsEnabled,"No-match search enables Add.");
            search.Text="";Require(models.Items.Count==3,"Clearing model search did not restore results.");
            var root=(FrameworkElement)picker.Content;
            foreach(var size in new[]{new Size(640,550),new Size(420,350)}) {root.Measure(size);root.Arrange(new Rect(size));root.UpdateLayout();Require(categories.ActualWidth>100 && models.ActualWidth>140,"Picker columns collapse at minimum size.");}
        }finally {picker.Close();}
    }
    private static void CheckInsertion(MainWindow window,CurrentPresetSnapshot snapshot)
    {
        var find=typeof(MainWindow).GetMethod("FindChainInsertion",BindingFlags.Static|BindingFlags.NonPublic)!;
        var rects=Enumerable.Range(0,5).Select(i=>new Rect(i*110,0,100,70)).ToArray();
        for(byte source=1;source<=5;source++)for(var gap=0;gap<=5;gap++) {
            var x=gap==5?550:gap*110;
            var actual=((byte Destination,double X,double Top,double Height))find.Invoke(null,new object[]{rects,new Point(x,30),source})!;
            var expected=gap>source-1?gap:gap+1;
            Require(actual.Destination==expected,"Insertion gap resolves to the wrong final position.");
        }
        var wrapped=new[]{new Rect(0,0,100,70),new Rect(110,0,100,70),new Rect(0,80,100,70),new Rect(110,80,100,70),new Rect(0,160,100,70)};
        var rowEnd=((byte Destination,double X,double Top,double Height))find.Invoke(null,new object[]{wrapped,new Point(300,100),(byte)1})!;
        Require(rowEnd.Destination==4 && rowEnd.Top==80 && rowEnd.X==214,"Wrapped row insertion/marker differs.");
        var root=(FrameworkElement)window.Content;
        foreach(var size in new[]{new Size(1366,768),new Size(683,384)}) {
            root.Measure(size);root.Arrange(new Rect(size));root.UpdateLayout();
            var dragType=typeof(MainWindow).GetNestedType("EffectDrag",BindingFlags.NonPublic)!;
            Set(window,"chainDrag",Activator.CreateInstance(dragType,"test",snapshot,(byte)1));
            var chain=Field<WrapPanel>(window,"effectChain");var target=(Button)chain.Children[2];
            var point=target.TranslatePoint(new Point(1,target.ActualHeight/2),chain);
            var destination=(byte?)Call(window,"ShowChainInsertion",point);
            var line=Field<Border>(window,"chainInsertionLine");
            Require(destination==2 && line.Visibility==Visibility.Visible && line.Height>target.ActualHeight,"Insertion line missing or inconsistent with destination.");
            Require(((System.Windows.Media.SolidColorBrush)line.Background).Color==System.Windows.Media.Color.FromRgb(0x43,0x85,0xff),"Insertion colour differs.");
            Set(window,"polling",true);Require(Call(window,"ShowChainInsertion",point) is not null && line.Visibility==Visibility.Visible,"Background read interrupts drag feedback.");Set(window,"polling",false);
            using(var busy=new CancellationTokenSource()) {
                Set(window,"operation",busy);Require(Call(window,"ShowChainInsertion",point) is null && line.Visibility==Visibility.Collapsed,"Busy operation leaves insertion marker visible.");Set(window,"operation",null);
            }
            Set(window,"chainDrag",null);Call(window,"HideChainInsertion");Require(line.Visibility==Visibility.Collapsed,"Drag cleanup left its marker visible.");
        }
    }
}
