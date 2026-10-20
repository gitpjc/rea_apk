using System.Windows;
using System.Windows.Markup;

namespace Iamp.Desktop;

internal static class DesktopScrollbars
{
    private static ResourceDictionary? theme;
    internal static void Install()
    {
        if(Application.Current is not {} app)return;
        theme??=(ResourceDictionary)XamlReader.Parse("""
            <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <Style x:Key="IampScrollThumb" TargetType="Thumb">
                <Setter Property="MinHeight" Value="28"/>
                <Setter Property="Template">
                  <Setter.Value><ControlTemplate TargetType="Thumb">
                    <Border x:Name="Grip" Margin="3" Background="#566170" CornerRadius="4"/>
                    <ControlTemplate.Triggers>
                      <Trigger Property="IsMouseOver" Value="True"><Setter TargetName="Grip" Property="Background" Value="#78899E"/></Trigger>
                      <Trigger Property="IsDragging" Value="True"><Setter TargetName="Grip" Property="Background" Value="#4C9EFF"/></Trigger>
                      <Trigger Property="IsEnabled" Value="False"><Setter TargetName="Grip" Property="Opacity" Value="0.4"/></Trigger>
                    </ControlTemplate.Triggers>
                  </ControlTemplate></Setter.Value>
                </Setter>
              </Style>
              <Style x:Key="IampScrollPage" TargetType="RepeatButton">
                <Setter Property="Focusable" Value="False"/>
                <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="RepeatButton"><Border Background="Transparent"/></ControlTemplate></Setter.Value></Setter>
              </Style>
              <Style TargetType="ScrollBar">
                <Setter Property="OverridesDefaultStyle" Value="True"/>
                <Setter Property="Focusable" Value="False"/>
                <Setter Property="Width" Value="14"/>
                <Setter Property="Background" Value="#20242B"/>
                <Setter Property="Template"><Setter.Value>
                  <ControlTemplate TargetType="ScrollBar">
                    <Border Background="{TemplateBinding Background}" CornerRadius="6" SnapsToDevicePixels="True">
                      <Track x:Name="PART_Track" Orientation="{TemplateBinding Orientation}" IsDirectionReversed="True" Minimum="{TemplateBinding Minimum}" Maximum="{TemplateBinding Maximum}" ViewportSize="{TemplateBinding ViewportSize}" Value="{Binding Value,RelativeSource={RelativeSource TemplatedParent},Mode=TwoWay}">
                        <Track.DecreaseRepeatButton><RepeatButton x:Name="BeforeThumb" Style="{StaticResource IampScrollPage}" Command="{x:Static ScrollBar.PageUpCommand}"/></Track.DecreaseRepeatButton>
                        <Track.Thumb><Thumb x:Name="ScrollThumb" Style="{StaticResource IampScrollThumb}"/></Track.Thumb>
                        <Track.IncreaseRepeatButton><RepeatButton x:Name="AfterThumb" Style="{StaticResource IampScrollPage}" Command="{x:Static ScrollBar.PageDownCommand}"/></Track.IncreaseRepeatButton>
                      </Track>
                    </Border>
                    <ControlTemplate.Triggers>
                      <Trigger Property="Orientation" Value="Horizontal">
                        <Setter TargetName="PART_Track" Property="IsDirectionReversed" Value="False"/>
                        <Setter TargetName="BeforeThumb" Property="Command" Value="{x:Static ScrollBar.PageLeftCommand}"/>
                        <Setter TargetName="AfterThumb" Property="Command" Value="{x:Static ScrollBar.PageRightCommand}"/>
                        <Setter TargetName="ScrollThumb" Property="MinHeight" Value="0"/><Setter TargetName="ScrollThumb" Property="MinWidth" Value="28"/>
                      </Trigger>
                    </ControlTemplate.Triggers>
                  </ControlTemplate>
                </Setter.Value></Setter>
                <Style.Triggers><Trigger Property="Orientation" Value="Horizontal"><Setter Property="Width" Value="Auto"/><Setter Property="Height" Value="14"/></Trigger></Style.Triggers>
              </Style>
            </ResourceDictionary>
            """);
        if(!app.Resources.MergedDictionaries.Contains(theme))app.Resources.MergedDictionaries.Add(theme);
    }
}
