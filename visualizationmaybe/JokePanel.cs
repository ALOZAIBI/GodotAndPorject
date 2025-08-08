using Godot;
using System;

public partial class JokePanel : Panel
{
    private Timer _initialTimer;
    private TextureRect _textureRect;
    private Tween _shrinkTween;
    private Vector2 _originalSize;
    private Vector2 _originalPosition;
    private bool _hasShownInitially = false;
    private StyleBoxFlat _panelStyle;
    
    public override void _Ready()
    {
        // Get the TextureRect child
        _textureRect = GetNode<TextureRect>("TextureRect");
        
        // Store original sizes and positions
        _originalSize = Size;
        _originalPosition = Position;
        
        // Get or create the panel's StyleBoxFlat for background control
        var theme = GetThemeStylebox("panel");
        if (theme is StyleBoxFlat styleBox)
        {
            // Create a copy so we don't modify the original theme
            _panelStyle = (StyleBoxFlat)styleBox.Duplicate();
            AddThemeStyleboxOverride("panel", _panelStyle);
        }
        
        // Set pivot to center for proper scaling
        _textureRect.PivotOffset = _textureRect.Size / 2.0f;
        
        // Start extremely zoomed in (10x scale)
        _textureRect.Scale = Vector2.One * 10.0f;
        
        // Start the zoom out animation immediately
        StartZoomOutAnimation();
    }
    
    private void StartZoomOutAnimation()
    {
        // Create tween for zoom out animation
        var zoomTween = CreateTween();
        
        // Zoom out from 10x to normal scale over 5 seconds
        zoomTween.TweenProperty(_textureRect, "scale", Vector2.One, 5.0f).SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
        
        // When zoom out completes, start the timer for shrinking
        zoomTween.TweenCallback(Callable.From(StartShrinkTimer));
    }
    
    private void StartShrinkTimer()
    {
        // Create and configure the timer to wait 1 second before shrinking
        _initialTimer = new Timer();
        _initialTimer.WaitTime = 1.0f; // 1 second
        _initialTimer.OneShot = true;
        _initialTimer.Timeout += OnInitialTimeout;
        AddChild(_initialTimer);
        
        // Start the timer to shrink after 1 second
        _initialTimer.Start();
    }
    
    private void OnInitialTimeout()
    {
        if (!_hasShownInitially)
        {
            ShrinkToCorner();
            _hasShownInitially = true;
        }
    }
    
    private void ShrinkToCorner()
    {
        // Calculate target size (10% of original)
        Vector2 targetSize = _originalSize * 0.1f;
        
        // Calculate target position (lower left corner with some margin)
        Vector2 targetPosition = new Vector2(20, GetViewport().GetVisibleRect().Size.Y - targetSize.Y - 20);
        
        // Kill any existing tween and create a new one
        if (_shrinkTween != null && _shrinkTween.IsValid())
        {
            _shrinkTween.Kill();
        }
        _shrinkTween = CreateTween();
        _shrinkTween.SetParallel(true); // Allow multiple properties to animate simultaneously
        
        // Animate the panel
        _shrinkTween.TweenProperty(this, "size", targetSize, 0.5f).SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
        _shrinkTween.TweenProperty(this, "position", targetPosition, 0.5f).SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
        
        // Fade out only the panel background by animating the StyleBox background color alpha
        if (_panelStyle != null)
        {
            Color currentColor = _panelStyle.BgColor;
            Color targetColor = new Color(currentColor.R, currentColor.G, currentColor.B, 0.1f);
            _shrinkTween.TweenProperty(_panelStyle, "bg_color", targetColor, 0.5f).SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
        }
        
        // Reset TextureRect to fit perfectly within the shrunk panel
        // Clear all offsets and make it fill the entire panel
        _shrinkTween.TweenProperty(_textureRect, "offset_left", 0.0f, 0.5f).SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
        _shrinkTween.TweenProperty(_textureRect, "offset_top", 0.0f, 0.5f).SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
        _shrinkTween.TweenProperty(_textureRect, "offset_right", 0.0f, 0.5f).SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
        _shrinkTween.TweenProperty(_textureRect, "offset_bottom", 0.0f, 0.5f).SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
    }
}
