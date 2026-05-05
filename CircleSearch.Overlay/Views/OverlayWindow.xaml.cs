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
    private bool _isSelecting = false;
    private System.Windows.Point _dragStart;
    private Bitmap? _fullScreenshot;
    private OverlayConfig _config;

    // DPI scale factors
    private double _dpiScaleX = 1.0;
    private double _dpiScaleY = 1.0;
    private ActionPopup? _currentPopup;
    private bool _isClosingPopupProgrammatically = false;

    // ── Constructor ────────────────────────────────────────────────────────────
    public OverlayWindow()
    {
        _config = AppRuntime.overlayConfig;
        InitializeComponent();

        TryApplyAccentColor(_config.AccentColor);

        Loaded += OnLoaded;
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

        _dragStart = e.GetPosition(DrawingCanvas);
        _isSelecting = true;

        HintPanel.Visibility = Visibility.Collapsed;

        // Hide crosshairs while dragging
        CrosshairH.Visibility = Visibility.Collapsed;
        CrosshairV.Visibility = Visibility.Collapsed;

        // Show dim masks
        ShowMasks(new Rect(_dragStart, _dragStart));

        SelectionRect.Visibility = Visibility.Visible;
        SizeLabel.Visibility = Visibility.Visible;

        DrawingCanvas.CaptureMouse();
        e.Handled = true;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(DrawingCanvas);

        if (!_isSelecting)
        {
            // Show crosshairs before drag starts
            CrosshairH.Visibility = Visibility.Visible;
            CrosshairV.Visibility = Visibility.Visible;
            CrosshairHT.Y = pos.Y;
            CrosshairVT.X = pos.X;
            return;
        }

        var rect = GetSelectionRect(pos);
        UpdateSelectionVisual(rect, pos);
    }

    private async void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isSelecting) return;
        _isSelecting = false;
        DrawingCanvas.ReleaseMouseCapture();

        var pos = e.GetPosition(DrawingCanvas);
        var rect = GetSelectionRect(pos);

        // Too small — cancel
        if (rect.Width < 10 || rect.Height < 10)
        {
            ResetSelectionUI();
            HintPanel.Visibility = Visibility.Visible;
            CrosshairH.Visibility = Visibility.Visible;
            CrosshairV.Visibility = Visibility.Visible;
            return;
        }

        SizeLabel.Visibility = Visibility.Collapsed;
        ProcessingPanel.Visibility = Visibility.Visible;
        AnimateProcessingDots();

        double left = this.Left;
        double top = this.Top;
        var screenshot = _fullScreenshot;

        try
        {
            var result = await Task.Run(() =>
                CaptureRegion(rect, left, top, screenshot));

            ProcessingPanel.Visibility = Visibility.Collapsed;

            if (result == null)
            {
                CloseOverlay();
                return;
            }

            ShowActionPopup(result, rect);
        }
        catch
        {
            ProcessingPanel.Visibility = Visibility.Collapsed;
            CloseOverlay();
        }
    }

    // ── Selection Visual ───────────────────────────────────────────────────────
    private Rect GetSelectionRect(System.Windows.Point current)
    {
        double x = Math.Min(_dragStart.X, current.X);
        double y = Math.Min(_dragStart.Y, current.Y);
        double w = Math.Abs(current.X - _dragStart.X);
        double h = Math.Abs(current.Y - _dragStart.Y);
        return new Rect(x, y, w, h);
    }

    private void UpdateSelectionVisual(Rect rect, System.Windows.Point cursor)
    {
        // Position the clear selection rectangle
        Canvas.SetLeft(SelectionRect, rect.X);
        Canvas.SetTop(SelectionRect, rect.Y);
        SelectionRect.Width = Math.Max(1, rect.Width);
        SelectionRect.Height = Math.Max(1, rect.Height);

        // Update dark masks around selection
        ShowMasks(rect);

        // Update size label
        SizeLabelText.Text = $"{(int)rect.Width} × {(int)rect.Height}";

        // Position size label below the selection, or above if near bottom
        double labelX = rect.X + rect.Width / 2 - 40;
        double labelY = rect.Bottom + 6;
        double canvasH = DrawingCanvas.ActualHeight;
        if (labelY + 24 > canvasH) labelY = rect.Y - 28;
        Canvas.SetLeft(SizeLabel, Math.Max(0, labelX));
        Canvas.SetTop(SizeLabel, Math.Max(0, labelY));
    }

    private void ShowMasks(Rect sel)
    {
        double W = DrawingCanvas.ActualWidth;
        double H = DrawingCanvas.ActualHeight;

        // Top mask
        Canvas.SetLeft(MaskTop, 0); Canvas.SetTop(MaskTop, 0);
        MaskTop.Width = W; MaskTop.Height = Math.Max(0, sel.Y);
        MaskTop.Visibility = Visibility.Visible;

        // Bottom mask
        Canvas.SetLeft(MaskBottom, 0); Canvas.SetTop(MaskBottom, sel.Bottom);
        MaskBottom.Width = W; MaskBottom.Height = Math.Max(0, H - sel.Bottom);
        MaskBottom.Visibility = Visibility.Visible;

        // Left mask (between top and bottom)
        Canvas.SetLeft(MaskLeft, 0); Canvas.SetTop(MaskLeft, sel.Y);
        MaskLeft.Width = Math.Max(0, sel.X); MaskLeft.Height = sel.Height;
        MaskLeft.Visibility = Visibility.Visible;

        // Right mask
        Canvas.SetLeft(MaskRight, sel.Right); Canvas.SetTop(MaskRight, sel.Y);
        MaskRight.Width = Math.Max(0, W - sel.Right); MaskRight.Height = sel.Height;
        MaskRight.Visibility = Visibility.Visible;
    }

    private void ResetSelectionUI()
    {
        SelectionRect.Visibility = Visibility.Collapsed;
        SizeLabel.Visibility = Visibility.Collapsed;
        MaskTop.Visibility = Visibility.Collapsed;
        MaskBottom.Visibility = Visibility.Collapsed;
        MaskLeft.Visibility = Visibility.Collapsed;
        MaskRight.Visibility = Visibility.Collapsed;
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
        double popupY = Top + bounds.Y + bounds.Height + 12;

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
