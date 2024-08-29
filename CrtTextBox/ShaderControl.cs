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
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Skia;
using Avalonia.Threading;
using SkiaSharp;

namespace RenderTest;

/// <summary>
/// A custom Avalonia control that applies a shader effect to a visual element.
/// </summary>
public class ShaderControl : UserControl
{
    /// <summary>
    /// Defines the Uri property for the shader source.
    /// </summary>
    public static readonly StyledProperty<Uri> ShaderUriProperty =
        AvaloniaProperty.Register<ShaderControl, Uri>(nameof(ShaderUri));

    /// <summary>
    /// Defines the sampling frame rate of the source control.
    /// </summary>
    public static readonly StyledProperty<int> FpsProperty =
        AvaloniaProperty.Register<ShaderControl, int>(
            nameof(Fps),
            30);

    private CompositionCustomVisual m_customVisual;
    private Control m_controlSource;
    private SKBitmap m_sourceControlBitmap;
    private ShaderVisualHandler m_visualHandler;

    static ShaderControl()
    {
        AffectsRender<ShaderControl>(ShaderUriProperty);
        AffectsMeasure<ShaderControl>(ShaderUriProperty);
    }

    /// <summary>
    /// Gets or sets the frames per second (FPS) at which the source control is sampled.
    /// </summary>
    public int Fps
    {
        get => GetValue(FpsProperty);
        set => SetValue(FpsProperty, value);
    }

    /// <summary>
    /// Gets or sets the URI of the shader to be applied.
    /// </summary>
    public Uri ShaderUri
    {
        get => GetValue(ShaderUriProperty);
        set => SetValue(ShaderUriProperty, value);
    }
        
    /// <summary>
    /// Gets or sets the control source to which the shader effect will be applied.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the control source is already set.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the control source is set to null.
    /// </exception>
    public Control ControlSource
    {
        get => m_controlSource;
        set
        {
            if (m_controlSource != null)
                throw new InvalidOperationException($"{nameof(ControlSource)} has already been set.");
            m_controlSource = value ?? throw new ArgumentNullException();

            DispatcherTimer.Run(() =>
            {
                RenderSourceAsImage();
                return true;
            }, TimeSpan.FromSeconds(1.0 / Fps));
        }
    }

    /// <summary>
    /// Called in response to the source control changing state, this method renders the source
    /// control into a bitmap, allowing for its appearance to later be modified when displayed
    /// to the screen.
    /// </summary>
    private void RenderSourceAsImage()
    {
        if (Bounds.Width == 0 || Bounds.Height == 0)
            return;

        // Check if the control bitmap needs to be recreated (e.g., if size has changed).
        if (m_sourceControlBitmap == null || m_sourceControlBitmap.Width != (int)Bounds.Width || m_sourceControlBitmap.Height != (int)Bounds.Height)
        {
            m_sourceControlBitmap?.Dispose();
            m_sourceControlBitmap = new SKBitmap(new SKImageInfo((int)Bounds.Width, (int)Bounds.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
        }

        using var rtb = new RenderTargetBitmap(new PixelSize(m_sourceControlBitmap.Width, m_sourceControlBitmap.Height));
        rtb.Render(ControlSource);

        rtb.CopyPixels(
            new PixelRect(0, 0, m_sourceControlBitmap.Width, m_sourceControlBitmap.Height),
            m_sourceControlBitmap.GetPixels(),
            m_sourceControlBitmap.ByteCount,
            m_sourceControlBitmap.RowBytes);

        m_visualHandler.SourceBitmap = m_sourceControlBitmap;
    }

    private Size GetShaderSize() => m_controlSource?.Bounds.Size ?? new Size(512, 512);

    protected override Size MeasureOverride(Size availableSize) =>
        ShaderUri != null ? Stretch.Fill.CalculateSize(availableSize, GetShaderSize()) : new Size();

    protected override Size ArrangeOverride(Size finalSize)
    {
        var source = ShaderUri;
        if (source == null)
            return new Size();

        var sourceSize = GetShaderSize();
        return Stretch.Fill.CalculateSize(finalSize, sourceSize);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        IsHitTestVisible = false;

        var elemVisual = ElementComposition.GetElementVisual(this);
        var compositor = elemVisual?.Compositor;
        if (compositor is null)
            return;

        m_visualHandler = new ShaderVisualHandler();
        m_customVisual = compositor.CreateCustomVisual(m_visualHandler);
        ElementComposition.SetElementChildVisual(this, m_customVisual);

        LayoutUpdated += OnLayoutUpdated;

        m_customVisual.Size = new Vector2((float)Bounds.Size.Width, (float)Bounds.Size.Height);

        m_customVisual.SendHandlerMessage(
            new ShaderVisualHandler.DrawPayload(
                ShaderVisualHandler.Command.Update,
                null,
                GetShaderSize(),
                Bounds.Size));

        Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        LayoutUpdated -= OnLayoutUpdated;

        Stop();
        DisposeImpl();
    }

    private void OnLayoutUpdated(object sender, EventArgs e)
    {
        if (m_customVisual == null)
            return;

        m_customVisual.Size = new Vector2((float)Bounds.Size.Width, (float)Bounds.Size.Height);

        m_customVisual.SendHandlerMessage(
            new ShaderVisualHandler.DrawPayload(
                ShaderVisualHandler.Command.Update,
                null,
                GetShaderSize(),
                Bounds.Size));
    }

    private void Start()
    {
        m_customVisual?.SendHandlerMessage(
            new ShaderVisualHandler.DrawPayload(
                ShaderVisualHandler.Command.Start,
                ShaderUri,
                GetShaderSize(),
                Bounds.Size));

        InvalidateVisual();
    }

    private void Stop() =>
        m_customVisual?.SendHandlerMessage(new ShaderVisualHandler.DrawPayload(ShaderVisualHandler.Command.Stop));

    private void DisposeImpl() =>
        m_customVisual?.SendHandlerMessage(new ShaderVisualHandler.DrawPayload(ShaderVisualHandler.Command.Dispose));

    public void SetSksl(string sksl)
    {
        Stop();
        m_visualHandler.ShaderCode = sksl;
        Start();
    }

    /// <summary>
    /// Handles custom visual drawing for the shader effect.
    /// </summary>
    private class ShaderVisualHandler : CompositionCustomVisualHandler
    {
        private readonly object m_sync = new object();
        private Size? m_boundsSize;
        private SKRuntimeEffect m_effect;
        private bool m_isDisposed;
        private bool m_running;
        private Size? m_shaderSize;
        private SKRuntimeEffectUniforms m_uniforms;

        internal string ShaderCode { get; set; }

        /// <summary>
        /// Gets or sets the source bitmap that the shader effect is applied to.
        /// </summary>
        public SKBitmap SourceBitmap { get; set; }

        /// <summary>
        /// Handles messages sent to the visual handler, such as starting or stopping the shader effect.
        /// </summary>
        public override void OnMessage(object message)
        {
            if (message is not DrawPayload msg)
                return;

            switch (msg)
            {
                case { Command: Command.Start, ShaderCode: { } uri, ShaderSize: { } shaderSize, Size: { } size }:
                {
                    if (ShaderCode == null)
                    {
                        using var stream = AssetLoader.Open(uri);
                        using var txt = new StreamReader(stream);
                        ShaderCode = txt.ReadToEnd();
                    }

                    m_effect = SKRuntimeEffect.Create(ShaderCode, out var errorText);
                    if (m_effect == null)
                        Console.WriteLine($"Shader compilation error: {errorText}");

                    m_shaderSize = shaderSize;
                    m_running = true;
                    m_boundsSize = size;
                    RegisterForNextAnimationFrameUpdate();
                    break;
                }
                case { Command: Command.Update, ShaderSize: { } shaderSize, Size: { } size }:
                {
                    m_shaderSize = shaderSize;
                    m_boundsSize = size;
                    RegisterForNextAnimationFrameUpdate();
                    break;
                }
                case { Command: Command.Stop }:
                {
                    m_running = false;
                    break;
                }
                case { Command: Command.Dispose }:
                {
                    DisposeImpl();
                    break;
                }
            }
        }

        /// <summary>
        /// Updates the shader effect on each animation frame, and schedules the next frame to be processed.
        /// </summary>
        public override void OnAnimationFrameUpdate()
        {
            if (!m_running || m_isDisposed)
                return;

            Invalidate();
            RegisterForNextAnimationFrameUpdate();
        }

        /// <summary>
        /// Draws the shader effect onto the given SkiaSharp canvas.
        /// </summary>
        private void Draw(SKCanvas canvas)
        {
            if (m_isDisposed || m_effect is null)
                return;

            canvas.Save();

            var targetWidth = (float)(m_shaderSize?.Width ?? 0.0);
            var targetHeight = (float)(m_shaderSize?.Height ?? 0.0);

            m_uniforms ??= new SKRuntimeEffectUniforms(m_effect);

            m_uniforms["iTime"] = (float)CompositionNow.TotalSeconds;
            m_uniforms["iResolution"] = new[]
            {
                targetWidth, targetHeight
            };

            SKRuntimeEffectChildren children = null;
            using var imageShader = SourceBitmap?.ToShader();
            if (imageShader != null)
            {
                children = new SKRuntimeEffectChildren(m_effect)
                {
                    ["iImage1"] = imageShader
                };
                m_uniforms["iImageResolution"] = new float[]
                {
                    SourceBitmap.Width, SourceBitmap.Height
                };
            }

            using (var paint = new SKPaint())
            using (var shader = children != null ? m_effect.ToShader(false, m_uniforms, children) : m_effect.ToShader(false, m_uniforms))
            {
                paint.Shader = shader;
                paint.FilterQuality = SKFilterQuality.Low;
                canvas.DrawRect(SKRect.Create(targetWidth, targetHeight), paint);
            }

            canvas.Restore();
        }

        /// <summary>
        /// Renders the shader effect within the context of the Avalonia ImmediateDrawingContext.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        public override void OnRender(ImmediateDrawingContext context)
        {
            lock (m_sync)
            {
                if (m_isDisposed)
                    return;

                var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
                if (leaseFeature is null)
                    return;

                var rb = GetRenderBounds();
                var size = m_boundsSize ?? rb.Size;
                var viewPort = new Rect(rb.Size);
                var sourceSize = m_shaderSize!.Value;

                if (sourceSize.Width <= 0 || sourceSize.Height <= 0)
                    return;

                var scale = Stretch.Fill.CalculateScaling(rb.Size, sourceSize);
                var scaledSize = sourceSize * scale;
                var destRect = viewPort
                    .CenterRect(new Rect(scaledSize))
                    .Intersect(viewPort);
                var sourceRect = new Rect(sourceSize)
                    .CenterRect(new Rect(destRect.Size / scale));

                var bounds = SKRect.Create(new SKPoint(), new SKSize((float)size.Width, (float)size.Height));

                var scaleMatrix = Matrix.CreateScale(
                    destRect.Width / sourceRect.Width,
                    destRect.Height / sourceRect.Height);

                var translateMatrix = Matrix.CreateTranslation(
                    (-sourceRect.X + destRect.X) - bounds.Top,
                    (-sourceRect.Y + destRect.Y) - bounds.Left);

                using (context.PushClip(destRect))
                using (context.PushPostTransform(translateMatrix * scaleMatrix))
                {
                    using var lease = leaseFeature.Lease();
                    Draw(lease.SkCanvas);
                }
            }
        }

        private void DisposeImpl()
        {
            lock (m_sync)
            {
                if (m_isDisposed) return;
                m_isDisposed = true;
                m_effect?.Dispose();
                m_uniforms?.Reset();
                m_running = false;
            }
        }

        /// <summary>
        /// Enum representing commands sent to the shader visual handler.
        /// </summary>
        public enum Command
        {
            Start,
            Stop,
            Update,
            Dispose
        }

        /// <summary>
        /// A struct representing the payload for draw commands sent to the shader visual handler.
        /// </summary>
        public record struct DrawPayload(
            Command Command,
            Uri ShaderCode = default,
            Size? ShaderSize = default,
            Size? Size = default);
    }
}