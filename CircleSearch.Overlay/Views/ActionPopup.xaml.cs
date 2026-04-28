using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using CircleSearch.Overlay.Helpers;

namespace CircleSearch.Overlay.Views;

public partial class ActionPopup : Window
{
    private Bitmap? _capturedRegion;
    private string _ocrText = "";
    private readonly string _ocrLanguage;
    private readonly OverlayConfig _config;

    public ActionPopup(Bitmap capturedRegion, OverlayConfig config)
    {
        InitializeComponent();
        _capturedRegion = capturedRegion;
        _config         = config;
        _ocrLanguage    = config.OcrLanguage;

        BtnSearch.Click      += OnSearch;
        BtnSearchImage.Click += OnSearchImage;
        BtnOcr.Click         += OnCopyOcr;
        BtnTranslate.Click   += OnTranslate;
        BtnClose.Click       += (_, _) => Close();

        RunOcrAsync();
    }

    private void RunOcrAsync()
    {
        ShowStatus("Đang nhận dạng văn bản...");

        Task.Run<string>(() =>
        {
            if (_capturedRegion == null) return string.Empty;
            return TesseractOcr.RecognizeText(_capturedRegion, _ocrLanguage);
        }).ContinueWith(t =>
        {
            _ocrText = t.Result ?? string.Empty;
            HideStatus();

            if (!string.IsNullOrWhiteSpace(_ocrText) && !_ocrText.StartsWith("[Lỗi"))
            {
                OcrPreviewText.Text = _ocrText;
                OcrPreviewBorder.Visibility = Visibility.Visible;
            }
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    private void OnSearch(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_ocrText))
        {
            ShowStatus("Chưa nhận dạng được văn bản. Đang tìm bằng ảnh...");
            OnSearchImage(sender, e);
            return;
        }
        OpenBrowser(_config.GetSearchUrl(_ocrText));
        Close();
    }

    private void OnSearchImage(object sender, RoutedEventArgs e)
    {
        if (_capturedRegion == null) { Close(); return; }
        ShowStatus("Đang chuẩn bị tìm kiếm ảnh...");

        // Capture reference trước khi vào Task
        var bitmap = _capturedRegion;

        Task.Run(() => ScreenCapture.ToBitmapSource(bitmap))
            .ContinueWith(t =>
            {
                Clipboard.SetImage(t.Result);
                OpenBrowser("https://lens.google.com/");
                ShowStatus("Ảnh đã sao chép — dán vào Google Lens (Ctrl+V)");
            }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    private void OnCopyOcr(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_ocrText))
        {
            ShowStatus("Chưa có văn bản để sao chép");
            return;
        }
        Clipboard.SetText(_ocrText);
        ShowStatus("✓ Đã sao chép vào clipboard!");

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        timer.Tick += (_, _) => { timer.Stop(); HideStatus(); };
        timer.Start();
    }

    private void OnTranslate(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_ocrText))
        {
            ShowStatus("Chưa nhận dạng được văn bản");
            return;
        }
        OpenBrowser($"https://translate.google.com/?text={Uri.EscapeDataString(_ocrText)}&op=translate");
        Close();
    }

    private static void OpenBrowser(string url)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName        = url,
            UseShellExecute = true
        });
    }

    private void ShowStatus(string msg)
    {
        StatusText.Text       = msg;
        StatusText.Visibility = Visibility.Visible;
    }

    private void HideStatus() => StatusText.Visibility = Visibility.Collapsed;

    protected override void OnClosed(EventArgs e)
    {
        _capturedRegion?.Dispose();
        base.OnClosed(e);
    }
}
