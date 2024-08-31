// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any non-commercial
// purpose.
//
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.
using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using RenderTest.Skins;

namespace RenderTest;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    
    private void OnTextBoxLoaded(object sender, RoutedEventArgs _)
    {
        var textBox = (TextBox)sender;
        textBox.ContextMenu = null;

        // Preload the text box with the shader code.
        using var skslStream = new StreamReader(AssetLoader.Open(new Uri("avares://CrtTextBox/Assets/crt.sksl")));
        textBox.Text = skslStream.ReadToEnd();
        textBox.Focus();

        // Allow real-time SKSL updates using 'F5'.
        textBox.KeyDown += (_, args) =>
        {
            if (args.PhysicalKey == PhysicalKey.F5)
                Shader.SetSksl(textBox.Text);
        };
        
        // Configure the default CRT shader.
        ApplyRetroSkin(new RetroPlasma());
    }

    private void ApplyRetroSkin(SkinBase skin)
    {
        // Apply font and color settings.
        Source.Foreground = SolidColorBrush.Parse(skin.ForegroundColor);
        Source.Background = SolidColorBrush.Parse(skin.BackgroundColor);
        Source.SelectionBrush = SolidColorBrush.Parse(skin.SelectionColor);
        Source.FontSize = skin.FontSize;

        // Set the focused background color in the resources.
        Source.Resources["TextControlBackgroundFocused"] = Source.Background;
        Source.Resources["TextControlBorderThemeThicknessFocused"] = new Thickness(0);

        // Configure the CRT shader using the provided skin.
        Shader.AddUniform("brightnessBoost", (float)skin.BrightnessBoost);
        Shader.AddUniform("enableScanlines", skin.EnableScanlines);
        Shader.AddUniform("enableSurround", skin.EnableSurround);
        Shader.AddUniform("enableSignalDistortion", skin.EnableSignalDistortion);
        Shader.AddUniform("enableShadows", skin.EnableShadows);
        
        // Alignment.
        MarginPanel.Margin = new Thickness(skin.EnableSurround ? 20 : 0);
    }
    
    private void OnShaderControlLoaded(object sender, RoutedEventArgs _)
    {
        // Set ShaderControl's source control.
        ((ShaderControl)sender).ControlSource = Source;
    }

    public void SetTheme(string name)
    {
        switch (name)
        {
            case "RetroPlasma":
                ApplyRetroSkin(new RetroPlasma());
                return;
            case "RetroGreenDos":
                ApplyRetroSkin(new RetroGreenDos());
                return;
            case "RetroMonoDos":
                ApplyRetroSkin(new RetroMonoDos());
                return;
            case "SimpleSkin":
                ApplyRetroSkin(new SimpleSkin());
                return;
        }
    }
}