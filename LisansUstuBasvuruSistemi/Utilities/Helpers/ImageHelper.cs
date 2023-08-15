using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using QRCoder;

namespace LisansUstuBasvuruSistemi.Utilities.Helpers
{
    public static  class ImageHelper
    {
        public static Image CreateQrCode(this string kod)
        {
           
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(kod, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(10); // QR kod boyutu belirleme (10 ile çarpılarak boyut arttırılabilir)
            return qrCodeImage;

             
        }

        public static Image ResizeImage(this Image imgToResize, Size size)
        {
            var sourceWidth = imgToResize.Width;
            var sourceHeight = imgToResize.Height;

            var nPercentW = ((float)sourceWidth / (float)size.Width);
            var nPercentH = ((float)sourceHeight / (float)size.Height);

            var nPercent = nPercentH > nPercentW ? nPercentH : nPercentW;

            var destWidth = (int)(sourceWidth / nPercent);
            var destHeight = (int)(sourceHeight / nPercent);

            Bitmap b = new Bitmap(destWidth, destHeight);
            Graphics g = Graphics.FromImage((Image)b);
            g.InterpolationMode = InterpolationMode.Bicubic;
            b.SetResolution(200, 200);
            g.DrawImage(imgToResize, 0, 0, destWidth, destHeight);
            g.Dispose();

            return (Image)b;
        }

        public static ImageCodecInfo GetImageCodecInfo(ImageFormat format)
        {

            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
    }
}
