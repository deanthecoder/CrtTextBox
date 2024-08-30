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
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;

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
        
        // Configure the CRT shader.
        Shader.AddUniform("textColor", 1.0f, 0.5f, 0.0f);
        Shader.AddUniform("backgroundTint", 0.2f);
        Shader.AddUniform("enableScanlines", true);
        Shader.AddUniform("enableSurround", true);
        Shader.AddUniform("enableSignalDistortion", true);
        Shader.AddUniform("enableShadows", true);
    }
    
    private void OnShaderControlLoaded(object sender, RoutedEventArgs _)
    {
        // Set ShaderControl's source control.
        ((ShaderControl)sender).ControlSource = Source;
    }
}