// // Code authored by Dean Edis (DeanTheCoder).
// // Anyone is free to copy, modify, use, compile, or distribute this software,
// // either in source code form or as a compiled binary, for any non-commercial
// // purpose.
// //
// // If you modify the code, please retain this copyright header,
// // and consider contributing back to the repository or letting us know
// // about your modifications. Your contributions are valued!
// //
// // THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.
namespace RenderTest.Skins;

public class SimpleSkin : SkinBase
{
    // Font properties
    public override string ForegroundColor => "#e0e000";
    public override string BackgroundColor => "#000080";
    public override string SelectionColor => "#004080";
    public override double FontSize => 16.0;
    public override double BrightnessBoost => 1.0;

    // Shader uniform properties
    public override bool EnableScanlines => false;
    public override bool EnableSurround => false;
    public override bool EnableSignalDistortion => false;
    public override bool EnableShadows => false;
}