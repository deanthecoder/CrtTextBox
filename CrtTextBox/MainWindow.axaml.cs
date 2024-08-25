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
        // Preload the text box with the shader code.
        using var skslStream = new StreamReader(AssetLoader.Open(new Uri("avares://CrtTextBox/Assets/crt.sksl")));
        ((TextBox)sender).Text = skslStream.ReadToEnd();
        ((TextBox)sender).Focus();
    }
    
    private void OnShaderControlLoaded(object sender, RoutedEventArgs _)
    {
        // Set ShaderControl's source control (I.e. The TextBox).
        ((ShaderControl)sender).ControlSource = Source;
    }
}