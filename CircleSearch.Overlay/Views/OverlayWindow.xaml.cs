using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CircleSearch.Overlay.Helpers;
using IOPath = System.IO.Path;
using WpfPath = System.Windows.Shapes.Path;

namespace CircleSearch.Overlay.Views;

public partial class OverlayWindow : Window
{
    // ── State ──────────────────────────────────────────────────────────────────
    private bool _isDrawing = false;
    private readonly List<System.Windows.Point> _strokePoints = new();
    private Bitmap? _fullScreenshot;
    private OverlayConfig _config;

    // DPI scale factors (for multi-monitor / scaling awareness)
    private double _dpiScaleX = 1.0;
    private double _dpiScaleY = 1.0;
    private ActionPopup? _currentPopup;
    private bool _isClosingPopupProgrammatically = false;

    // ── Constructor ────────────────────────────────────────────────────────────
    public OverlayWindow()
    {
        _config = AppRuntime.overlayConfig;
        InitializeComponent();

        // Apply accent color from config
        TryApplyAccentColor(_config.AccentColor);

        Loaded += OnLoaded;
        //Deactivated += (_, _) => CloseOverlay();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget != null)
        {
            _dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
            _dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
        }

        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;

        _fullScreenshot = await Task.Run(CaptureScreen);

        DrawingCanvas.MouseEnter += (_, _) => MagnifierBorder.Visibility = Visibility.Visible;
        DrawingCanvas.MouseLeave += (_, _) => MagnifierBorder.Visibility = Visibility.Collapsed;

        Focus();
        Activate();
    }

    private Bitmap? CaptureScreen()
    {
        try { return ScreenCapture.CaptureScreen(); }
        catch { return null; }
    }

    // ── Mouse Events ───────────────────────────────────────────────────────────
    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        _isClosingPopupProgrammatically = true;
        _currentPopup?.Close();
        _currentPopup = null;
        _isClosingPopupProgrammatically = false;

        _isDrawing = true;
        _strokePoints.Clear();
        _strokePoints.Add(e.GetPosition(DrawingCanvas));

        // Fade in the stroke path
        StrokePath.Opacity = 0;
        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150));
        StrokePath.BeginAnimation(UIElement.OpacityProperty, fadeIn);

        HintPanel.Visibility = Visibility.Collapsed;
        SelectionRect.Visibility = Visibility.Collapsed;

        DrawingCanvas.CaptureMouse();
        e.Handled = true;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(DrawingCanvas);

        // Move magnifier ring with cursor
        MagnifierTranslate.X = pos.X - 40;
        MagnifierTranslate.Y = pos.Y - 40;

        if (!_isDrawing) return;

        _strokePoints.Add(pos);

        // Redraw stroke path
        if (_strokePoints.Count >= 2)
        {
            var geometry = CircleHelper.CreateSmoothPath(_strokePoints);
            StrokePath.Data = geometry;
        }
    }

    private async void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDrawing) return;
        _isDrawing = false;
        DrawingCanvas.ReleaseMouseCapture();

        _strokePoints.Add(e.GetPosition(DrawingCanvas));

        if (_strokePoints.Count < 5)
        {
            CloseOverlay();
            return;
        }

        AnimateStrokeComplete();

        var bounds = CircleHelper.GetBoundingRect(_strokePoints, padding: 16);

        ShowSelectionRect(bounds);

        ProcessingPanel.Visibility = Visibility.Visible;
        AnimateProcessingDots();

        // ✅ Snapshot toàn bộ dữ liệu UI
        double left = this.Left;
        double top = this.Top;

        var safeBounds = new Rect(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        var screenshot = _fullScreenshot;

        try
        {
            var result = await Task.Run(() =>
                CaptureRegion(safeBounds, left, top, screenshot));

            ProcessingPanel.Visibility = Visibility.Collapsed;

            if (result == null)
            {
                CloseOverlay();
                return;
            }

            ShowActionPopup(result, safeBounds);
        }
        catch
        {
            ProcessingPanel.Visibility = Visibility.Collapsed;
            CloseOverlay();
        }
    }

    // ── Capture & Popup ────────────────────────────────────────────────────────
    private Bitmap? CaptureRegion(Rect bounds, double left, double top, Bitmap? screenshot)
    {
        if (screenshot == null) return null;

        var physRect = new System.Drawing.Rectangle(
            (int)((left + bounds.X) * _dpiScaleX),
            (int)((top + bounds.Y) * _dpiScaleY),
            (int)(bounds.Width * _dpiScaleX),
            (int)(bounds.Height * _dpiScaleY));

        physRect.Intersect(new System.Drawing.Rectangle(
            0, 0, screenshot.Width, screenshot.Height));

        if (physRect.Width <= 0 || physRect.Height <= 0)
            return null;

        return ScreenCapture.CropRegion(screenshot, physRect);
    }

    private void ShowActionPopup(Bitmap? region, Rect bounds)
    {
        if (region == null)
        {
            CloseOverlay();
            return;
        }

        _currentPopup?.Close();
        _currentPopup = null;

        var popup = new ActionPopup(region, _config);
        _currentPopup = popup;

        double popupX = Left + bounds.X + bounds.Width / 2 - 170;
        double popupY = Top + bounds.Y + bounds.Height + 12; // ⚠️ fix bug Top

        var screenH = SystemParameters.VirtualScreenHeight + SystemParameters.VirtualScreenTop;
        if (popupY + 260 > screenH)
            popupY = Top + bounds.Y - 260;

        popup.Left = Math.Max(SystemParameters.VirtualScreenLeft, popupX);
        popup.Top = Math.Max(SystemParameters.VirtualScreenTop, popupY);

        popup.Closed += (_, _) =>
        {
            _currentPopup = null;

            if (!_isClosingPopupProgrammatically)
            {
                CloseOverlay();
            }
        };

        popup.Show();
        popup.Activate();
    }

    // ── Animations ─────────────────────────────────────────────────────────────
    private void AnimateStrokeComplete()
    {
        // Pulse the stroke — glow effect
        var thicknessAnim = new DoubleAnimationUsingKeyFrames();
        thicknessAnim.KeyFrames.Add(new LinearDoubleKeyFrame(5, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
        thicknessAnim.KeyFrames.Add(new LinearDoubleKeyFrame(2, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300))));
        StrokePath.BeginAnimation(Shape.StrokeThicknessProperty, thicknessAnim);

        // Briefly flash the stroke to accent color
        var colorStroke = TryParseColor(_config.AccentColor) ?? Colors.LightBlue;
        StrokePath.Stroke = new SolidColorBrush(colorStroke);
    }

    private void ShowSelectionRect(Rect bounds)
    {
        Canvas.SetLeft(SelectionRect, bounds.X);
        Canvas.SetTop(SelectionRect, bounds.Y);
        SelectionRect.Width = Math.Max(1, bounds.Width);
        SelectionRect.Height = Math.Max(1, bounds.Height);
        SelectionRect.Visibility = Visibility.Visible;

        // Animate in
        SelectionRect.Opacity = 0;
        SelectionRect.BeginAnimation(UIElement.OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200)));
    }

    private System.Windows.Threading.DispatcherTimer? _dotTimer;
    private int _dotPhase = 0;

    private void AnimateProcessingDots()
    {
        _dotTimer?.Stop();
        _dotTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _dotTimer.Tick += (_, _) =>
        {
            _dotPhase = (_dotPhase + 1) % 3;
            Dot1.Opacity = _dotPhase == 0 ? 1 : 0.3;
            Dot2.Opacity = _dotPhase == 1 ? 1 : 0.3;
            Dot3.Opacity = _dotPhase == 2 ? 1 : 0.3;
        };
        _dotTimer.Start();
    }

    // ── Keyboard ───────────────────────────────────────────────────────────────
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            CloseOverlay();
    }

    // ── Close ──────────────────────────────────────────────────────────────────
    private void CloseOverlay()
    {
        _dotTimer?.Stop();
        _fullScreenshot?.Dispose();

        // Fade out
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
        fadeOut.Completed += (_, _) =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Close();
                Environment.Exit(0);
            });
        };
        BeginAnimation(UIElement.OpacityProperty, fadeOut);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────
    private void TryApplyAccentColor(string hex)
    {
        try
        {
            var color = TryParseColor(hex);
            if (color.HasValue)
            {
                var brush = new SolidColorBrush(color.Value);
                SelectionRect.Stroke = brush;
                SelectionRect.Fill = new SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(34,
                        color.Value.R, color.Value.G, color.Value.B));

                // Apply to glow effect of stroke
                if (StrokePath.Effect is System.Windows.Media.Effects.DropShadowEffect glow)
                    glow.Color = color.Value;
            }
        }
        catch { /* non-critical */ }
    }

    private static System.Windows.Media.Color? TryParseColor(string hex)
    {
        try { return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex); }
        catch { return null; }
    }
}
