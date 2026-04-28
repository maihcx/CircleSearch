using System;
using System.Drawing;
using System.IO;
using Tesseract;

namespace CircleSearch.Overlay.Helpers;

public static class TesseractOcr
{
    private static readonly string TessDataPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "tessdata");

    public static string RecognizeText(Bitmap image, string language = "vie")
    {
        if (!Directory.Exists(TessDataPath))
            return "[Lỗi OCR: Thiếu thư mục tessdata/]";

        var trainedFile = Path.Combine(TessDataPath, language.Split('+')[0] + ".traineddata");
        if (!File.Exists(trainedFile))
            return $"[Lỗi OCR: Thiếu file {language}.traineddata]";

        try
        {
            using var engine = new TesseractEngine(TessDataPath, language, EngineMode.Default);

            // Convert Bitmap → Pix (format chuẩn của Tesseract)
            using var pix = PixConverter.ToPix(image);

            using var page = engine.Process(pix);

            return page.GetText()?.Trim() ?? "";
        }
        catch (Exception ex)
        {
            return $"[Lỗi OCR: {ex.Message}]";
        }
    }
}