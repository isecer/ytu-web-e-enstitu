using System;
using System.IO;
using System.Web;
using SkiaSharp;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace LisansUstuBasvuruSistemi.Business
{
    public class InviteRenderService
    {
        private readonly string _templatePath =
            HttpContext.Current.Server.MapPath("~/Content/templates/tez-daveti-bg.png");

        public string RenderToFile(ThesisInviteVm vm, int quality = 75)
        {
            var stamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            var safe = (vm.FullName ?? "noname").Replace(" ", "_");
            foreach (var ch in Path.GetInvalidFileNameChars()) safe = safe.Replace(ch, '_');
            var fileName = $"{safe}_{vm.Date}_invite_{vm.TableId}_.jpg";
            var outputPath = HttpContext.Current.Server.MapPath("~/Images/SinavDavetGaleri/" + fileName);

            Func<float, float> ToPx = pt => pt * 96f / 72f;

            using (var bg = SKBitmap.Decode(_templatePath))
            using (var surface = SKSurface.Create(new SKImageInfo(bg.Width, bg.Height)))
            {
                var canvas = surface.Canvas;
                canvas.DrawBitmap(bg, 0, 0);

                using (var tfRegular = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal))
                using (var tfBold = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold))
                {
                    float CX = bg.Width / 2f;

                    // === BOYUTLAR (pt) — her alan için ayrı ===
                    float fsFullNamePt = 22.9f;
                    float fsDeptPt = 22.9f;
                    float fsProgramPt = 20.4f;
                    float fsThesisPt = 17.4f;
                    float fsAdvisorPt = 21.4f;
                    float fsDatePt = 17.4f;
                    float fsTimePt = 17.4f;
                    float fsPlacePt = 17.4f;

                    // px karşılıkları
                    float fsFullName = ToPx(fsFullNamePt);
                    float fsDept = ToPx(fsDeptPt);
                    float fsProgram = ToPx(fsProgramPt);
                    float fsThesis = ToPx(fsThesisPt);
                    float fsAdvisor = ToPx(fsAdvisorPt);
                    float fsDate = ToPx(fsDatePt);
                    float fsTime = ToPx(fsTimePt);
                    float fsPlace = ToPx(fsPlacePt);

                    // === KOORDİNATLAR ===
                    float yAvatarCenter = 140f;
                    float avatarRadius = 70f;
                    float yFullName = 255f;
                    float yDept = 295f;
                    float yProgram = 335f;

                    float yThesisTitleAreaTop = 380f;
                    float yThesisTitleAreaBottom = 560f;

                    float yAdvisor = 610f;
                    float yDate = 705f;
                    float yTime = 735f;
                    float yPlaceTop = 745f;

                    // Avatar (değişmedi)
                    if (!string.IsNullOrEmpty(vm.AvatarPath))
                    {
                        SKBitmap avatarBitmap = null;
                        try
                        {
                            if (vm.AvatarPath.StartsWith("http://") || vm.AvatarPath.StartsWith("https://"))
                            {
                                using (var webClient = new System.Net.WebClient())
                                {
                                    var imageBytes = webClient.DownloadData(vm.AvatarPath);
                                    using (var ms = new MemoryStream(imageBytes))
                                        avatarBitmap = SKBitmap.Decode(ms);
                                }
                            }
                            else
                            {
                                var ap = HttpContext.Current.Server.MapPath(vm.AvatarPath);
                                if (File.Exists(ap)) avatarBitmap = SKBitmap.Decode(ap);
                            }

                            if (avatarBitmap != null)
                            {
                                using (avatarBitmap)
                                using (var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High })
                                {
                                    var cx = CX; 
                                    var cy = yAvatarCenter;
                                    var circle = new SKPath();
                                    circle.AddCircle(cx, cy, avatarRadius);

                                    canvas.Save();
                                    canvas.ClipPath(circle, SKClipOperation.Intersect, true);
                                    var dst = new SKRect(cx - avatarRadius, cy - avatarRadius, cx + avatarRadius, cy + avatarRadius);
                                    canvas.DrawBitmap(avatarBitmap, dst, paint);
                                    canvas.Restore();

                                    paint.Style = SKPaintStyle.Stroke;
                                    paint.StrokeWidth = 5f;
                                    paint.Color = new SKColor(249, 214, 137);
                                    canvas.DrawCircle(cx, cy, avatarRadius + 2f, paint);

                                    paint.Color = new SKColor(249, 214, 137, 70);
                                    paint.StrokeWidth = 12f;
                                    canvas.DrawCircle(cx, cy, avatarRadius + 10f, paint);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Avatar yüklenemedi: {ex.Message}");
                        }
                    }

                    // === METİNLER (artık her biri kendi boyutunu kullanıyor) ===
                    DrawCentered(canvas, tfBold, vm.FullName, CX, yFullName, fsFullName, SKColors.White, 1200);
                    DrawCentered(canvas, tfRegular, vm.Department, CX, yDept, fsDept, new SKColor(220, 225, 230), 1200);
                    DrawCentered(canvas, tfRegular, vm.Program, CX, yProgram, fsProgram, new SKColor(220, 225, 230), 1200);

                    DrawWrappedCenteredVertically(canvas, tfBold, vm.ThesisTitle, CX,
                        yThesisTitleAreaTop, yThesisTitleAreaBottom,
                        fsThesis, SKColors.White, 1000, 3, ToPx(12f));

                    DrawCentered(canvas, tfRegular, vm.Advisor, CX, yAdvisor, fsAdvisor, SKColors.White, 1200);
                    DrawCentered(canvas, tfRegular, "Tarih: " + (vm.Date ?? ""), CX, yDate, fsDate, SKColors.White, 1000);
                    DrawCentered(canvas, tfRegular, "Saat: " + (vm.Time ?? ""), CX, yTime, fsTime, SKColors.White, 1000);
                    DrawWrappedCentered(canvas, tfRegular, "Yer: " + (vm.Place ?? ""), CX, yPlaceTop, fsPlace, SKColors.White, 1300, 2, ToPx(12f));
                }

                canvas.Flush();

                var dir = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                if (File.Exists(outputPath)) File.Delete(outputPath);

                using (var image = surface.Snapshot())
                using (var data = image.Encode(SKEncodedImageFormat.Jpeg, quality))
                using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                    data.SaveTo(fs);
            }

            return "/Images/SinavDavetGaleri/" + fileName;
        }



        public byte[] RenderToPdf(ThesisInviteVm vm)
        {
            // PNG oluştur
            var pngUrl = RenderToFile(vm);
            var pngPath = HttpContext.Current.Server.MapPath(pngUrl);

            using (var ms = new MemoryStream())
            {
                // A4 yatay boyut (842 x 595 point)
                var document = new Document(PageSize.A4.Rotate(), 0, 0, 0, 0);
                var writer = PdfWriter.GetInstance(document, ms);

                // PDF açıldığında "Sayfaya Sığdır" görünümü
                writer.ViewerPreferences = PdfWriter.PageLayoutSinglePage | PdfWriter.FitWindow;

                document.Open();

                var pngImage = iTextSharp.text.Image.GetInstance(pngPath);
                float pageWidth = PageSize.A4.Rotate().Width;
                float pageHeight = PageSize.A4.Rotate().Height;
                float pngRatio = pngImage.Width / pngImage.Height;
                float pageRatio = pageWidth / pageHeight;
                if (pngRatio > pageRatio)
                {
                    // PNG daha geniş -> genişliğe göre ölçekle
                    pngImage.ScaleToFit(pageWidth, pageHeight);
                    float scaledHeight = pngImage.ScaledHeight;
                    float yOffset = (pageHeight - scaledHeight) / 2;
                    pngImage.SetAbsolutePosition(0, yOffset);
                }
                else
                {
                    // PNG daha dar -> yüksekliğe göre ölçekle
                    pngImage.ScaleToFit(pageWidth, pageHeight);
                    float scaledWidth = pngImage.ScaledWidth;
                    float xOffset = (pageWidth - scaledWidth) / 2;
                    pngImage.SetAbsolutePosition(xOffset, 0);
                }
                document.Add(pngImage);

                document.Close();
                return ms.ToArray();
            }
        }
        const float LINE_TIGHT = 0.86f;
        private static void DrawWrappedCenteredVertically(
            SKCanvas canvas, SKTypeface tf, string text,
            float cx, float areaTop, float areaBottom,
            float sizePx, SKColor color,
            float maxWidth, int maxLines,
            float autoShrinkMinPx)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            using (var paint = new SKPaint
                   {
                       Typeface = tf,
                       TextSize = sizePx,
                       IsAntialias = true,
                       Color = color,
                       TextAlign = SKTextAlign.Center
                   })
            {
                var lines = WrapLines(text, paint, maxWidth);
                while (lines.Count > maxLines && paint.TextSize > autoShrinkMinPx)
                {
                    paint.TextSize -= 1f;
                    lines = WrapLines(text, paint, maxWidth);
                }

                var fm = paint.FontMetrics;
                // Leading'i kullanmıyoruz, sadece Ascent/Descent ile sıkı satır yüksekliği
                float baseLineH = Math.Abs(fm.Ascent) + Math.Abs(fm.Descent);
                float lineH = baseLineH * LINE_TIGHT;

                int drawCount = Math.Min(lines.Count, maxLines);
                float totalH = baseLineH + (drawCount - 1) * lineH;

                float centerY = (areaTop + areaBottom) * 0.5f;
                // İlk satırın baseline'ını bul: baseline, üstten fm.Ascent kadar aşağıdadır
                float startBaselineY = centerY - totalH / 2f - fm.Ascent;

                for (int i = 0; i < drawCount; i++)
                {
                    float y = startBaselineY + i * lineH;
                    canvas.DrawText(lines[i], cx, y, paint);
                }
            }
        }


        private static void DrawCentered(SKCanvas canvas, SKTypeface tf, string text, float cx, float y, float sizePx, SKColor color, float maxWidthPx)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            using (var paint = new SKPaint { Typeface = tf, TextSize = sizePx, IsAntialias = true, Color = color, TextAlign = SKTextAlign.Center })
            {
                var w = paint.MeasureText(text);
                var minPx = 12f;
                while (w > maxWidthPx && paint.TextSize > minPx)
                {
                    paint.TextSize -= 1.0f;
                    w = paint.MeasureText(text);
                }
                canvas.DrawText(text, cx, y, paint);
            }
        }

        private static void DrawWrappedCentered(
            SKCanvas canvas, SKTypeface tf, string text,
            float cx, float topY, float sizePx, SKColor color,
            float maxWidth, int maxLines, float autoShrinkMinPx)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            using (var paint = new SKPaint
                   {
                       Typeface = tf,
                       TextSize = sizePx,
                       IsAntialias = true,
                       Color = color,
                       TextAlign = SKTextAlign.Center
                   })
            {
                var lines = WrapLines(text, paint, maxWidth);
                while (lines.Count > maxLines && paint.TextSize > autoShrinkMinPx)
                {
                    paint.TextSize -= 1f;
                    lines = WrapLines(text, paint, maxWidth);
                }

                var fm = paint.FontMetrics;
                float baseLineH = Math.Abs(fm.Ascent) + Math.Abs(fm.Descent);
                float lineH = baseLineH * LINE_TIGHT;

                int drawCount = Math.Min(lines.Count, maxLines);
                // Üstten başlayacağımız ilk baseline:
                float startBaselineY = topY - fm.Ascent;

                for (int i = 0; i < drawCount; i++)
                {
                    float y = startBaselineY + i * lineH;
                    canvas.DrawText(lines[i], cx, y, paint);
                }
            }
        }

        private static System.Collections.Generic.List<string> WrapLines(string text, SKPaint paint, float maxWidth)
        {
            var result = new System.Collections.Generic.List<string>();
            var words = (text ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var line = "";

            foreach (var w in words)
            {
                var test = string.IsNullOrEmpty(line) ? w : line + " " + w;
                if (paint.MeasureText(test) <= maxWidth)
                    line = test;
                else
                {
                    if (!string.IsNullOrEmpty(line)) result.Add(line);
                    line = w;
                }
            }
            if (!string.IsNullOrEmpty(line)) result.Add(line);
            return result;
        }
    }

    public class ThesisInviteVm
    {
        public string FullName { get; set; }
        public string Department { get; set; }
        public string Program { get; set; }
        public string ThesisTitle { get; set; }
        public string Advisor { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string Place { get; set; }
        public string AvatarPath { get; set; }
        public int TableId { get; set; }
    }
}