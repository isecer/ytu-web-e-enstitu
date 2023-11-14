using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

namespace BiskaUtil
{
    [Serializable()]
    public class CaptchaImage
    {
        // Public properties (all read-only).
        public string Text { get; }

        public Bitmap Image { get; private set; }

        public int Width { get; private set; }

        public int Height { get; private set; }

        // Internal properties.
        private string _familyName;

        public float FontSize { get; set; }
        // For generating random numbers.
        private Random _random = new Random();

        public CaptchaImage(string s, int width, int height, int fontSize)
        {
            this.Text = string.IsNullOrWhiteSpace(s) == false ? s : GenerateRandomCode();
            this.FontSize = fontSize;
            this.SetDimensions(width, height);
            this.GenerateImage();
        }

        // ====================================================================
        // Initializes a new instance of the CaptchaImage class using the
        // specified text, width and height.
        // ====================================================================
        public CaptchaImage(string s, int width, int height)
        {
            this.Text = string.IsNullOrWhiteSpace(s) == false ? s : GenerateRandomCode();
            this.SetDimensions(width, height);
            this.GenerateImage();
        }

        // ====================================================================
        // Initializes a new instance of the CaptchaImage class using the
        // specified text, width, height and font family.
        // ====================================================================
        public CaptchaImage(string s, int width, int height, string familyName)
        {
            this.Text = s;
            this.SetDimensions(width, height);
            this.SetFamilyName(familyName);
            this.GenerateImage();
        }

        // ====================================================================
        // This member overrides Object.Finalize.
        // ====================================================================
        ~CaptchaImage()
        {
            Dispose(false);
        }

        // ====================================================================
        // Releases all resources used by this object.
        // ====================================================================
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            this.Dispose(true);
        }

        // ====================================================================
        // Custom Dispose method to clean up unmanaged resources.
        // ====================================================================
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                // Dispose of the bitmap.
                this.Image.Dispose();
        }

        // ====================================================================
        // Sets the image width and height.
        // ====================================================================
        private void SetDimensions(int width, int height)
        {
            // Check the width and height.
            if (width <= 0)
                throw new ArgumentOutOfRangeException("width", width, "Argument out of range, must be greater than zero.");
            if (height <= 0)
                throw new ArgumentOutOfRangeException("height", height, "Argument out of range, must be greater than zero.");
            Width = width;
            Height = height;
        }

        // ====================================================================
        // Sets the font used for the image text.
        // ====================================================================
        private void SetFamilyName(string familyName)
        {
            // If the named font is not installed, default to a system font.
            try
            {
                var font = new Font(this._familyName, 12F);
                _familyName = familyName;
                font.Dispose();
            }
            catch (Exception)
            {
                _familyName = System.Drawing.FontFamily.GenericSerif.Name;
            }
        }

        // ====================================================================
        // Creates the bitmap image.
        // ====================================================================
        private void GenerateImage()
        {
            // Create a new 32-bit bitmap image.
            var bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);

            // Create a graphics object for drawing.
            var g = Graphics.FromImage(bitmap);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, this.Width, this.Height);

            // Fill in the background.
            var hatchBrush = new HatchBrush(HatchStyle.SmallConfetti, Color.LightGray, Color.White);
            g.FillRectangle(hatchBrush, rect);

            // Set up the text font.
            float fontSize = rect.Height + 5;
            Font font;
            if (this.FontSize == 0)
            {
                // Adjust the font size until the text fits within the image.
                SizeF size;
                do
                {
                    fontSize--;
                    font = new Font(this._familyName, fontSize, FontStyle.Bold);
                    size = g.MeasureString(this.Text, font);
                } while (size.Width > rect.Width);
            }
            else
            {
                font = new Font(this._familyName, fontSize, FontStyle.Bold);
            }
            // Set up the text format.
            var format = new StringFormat();
            format.Alignment = StringAlignment.Near;
            format.LineAlignment = StringAlignment.Near;
            format.FormatFlags = StringFormatFlags.NoWrap;
            // Create a path using the text and warp it randomly.
            var path = new GraphicsPath();
            path.AddString(this.Text, font.FontFamily, (int)font.Style, font.Size, rect, format);
            const float v = 4F;
            PointF[] points =
            {
                new PointF(this._random.Next(rect.Width) / v, this._random.Next(rect.Height) / v),
                new PointF(rect.Width - this._random.Next(rect.Width) / v, this._random.Next(rect.Height) / v),
                new PointF(this._random.Next(rect.Width) / v, rect.Height - this._random.Next(rect.Height) / v),
                new PointF(rect.Width - this._random.Next(rect.Width) / v, rect.Height - this._random.Next(rect.Height) / v)
            };
            var matrix = new Matrix();
            matrix.Translate(0F, 0F);
            path.Warp(points, rect, matrix, WarpMode.Perspective, 0F);

            // Draw the text.
            hatchBrush = new HatchBrush(HatchStyle.LargeConfetti, Color.LightGray, Color.DarkGray);
            g.FillPath(hatchBrush, path);

            // Add some random noise.
            var m = Math.Max(rect.Width, rect.Height);
            for (var i = 0; i < (int)(rect.Width * rect.Height / 30F); i++)
            {
                var x = this._random.Next(rect.Width);
                var y = this._random.Next(rect.Height);
                var w = this._random.Next(m / 50);
                var h = this._random.Next(m / 50);
                g.FillEllipse(hatchBrush, x, y, w, h);
            }

            // Clean up.
            font.Dispose();
            hatchBrush.Dispose();
            g.Dispose();

            // Set the image.
            Image = bitmap;
        }

        private string GenerateRandomCode()
        {
            //var charArray = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
            //string str = "";
            //for (int i = 0; i < 4; i++)
            //{ 
            //    var ix=this.random.Next(charArray.Length);
            //    str = String.Concat(str, charArray[ix].ToString());
            //}
            //return str;

            var s = "";
            for (var i = 0; i < 4; i++)
                s = string.Concat(s, _random.Next(10).ToString());
            return s;
        }
        public string GetBase64ImageSrc()
        {
            var ms = new System.IO.MemoryStream();
            Image.Save(ms, ImageFormat.Jpeg);
            try
            {
                return "data:image/jpg;base64," + Convert.ToBase64String(ms.GetBuffer());
            }
            finally
            {
                ms.Dispose();
            }

            //String path = Commons.HhtmlDecode(item);
            //if (path.Contains('/')) path = "/StaticContent/" + path.Substring(path.LastIndexOf('/') + 1);
            //if (!File.Exists(HostingEnvironment.MapPath("~" + path))) continue;
            //String replacment = "data:image/" + item.Substring(item.Length - 3) + ";base64," + Convert.ToBase64String(File.ReadAllBytes(HostingEnvironment.MapPath("~" + path)));
            //BodyContent = BodyContent.Replace(item, replacment);// replace path with new Image-Identifier

        }
    }
}
