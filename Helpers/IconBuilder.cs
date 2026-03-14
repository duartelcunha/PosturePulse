using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace PosturePulse.Helpers;

/// <summary>
/// Generates a modern, multi-resolution app icon at runtime.
/// Produces a clean geometric "P" mark in a rounded-square shape.
/// </summary>
public static class IconBuilder
{
    private static readonly Color AccentPrimary = Color.FromArgb(100, 160, 255);
    private static readonly Color AccentSecondary = Color.FromArgb(60, 110, 220);
    private static readonly Color GlyphColor = Color.FromArgb(8, 12, 20);

    /// <summary>
    /// Builds an ICO containing 16×16, 24×24, 32×32, and 48×48 sizes.
    /// </summary>
    public static Icon BuildMultiSizeIcon()
    {
        int[] sizes = [16, 24, 32, 48];
        var bitmaps = new List<Bitmap>();

        try
        {
            foreach (var size in sizes)
                bitmaps.Add(RenderIcon(size));

            return CreateMultiSizeIcon(bitmaps, sizes);
        }
        finally
        {
            foreach (var bmp in bitmaps)
                bmp.Dispose();
        }
    }

    /// <summary>Builds a single-size icon (e.g. for the system tray at 16×16).</summary>
    public static Icon BuildSingleIcon(int size = 16)
    {
        using var bmp = RenderIcon(size);
        var hIcon = bmp.GetHicon();
        var icon = (Icon)Icon.FromHandle(hIcon).Clone();
        NativeMethods.DestroyIcon(hIcon);
        return icon;
    }

    private static Bitmap RenderIcon(int size)
    {
        var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);

        g.SmoothingMode = SmoothingMode.HighQuality;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.Clear(Color.Transparent);

        float padding = size * 0.04f;
        var rect = new RectangleF(padding, padding, size - padding * 2, size - padding * 2);

        // Background: gradient rounded rectangle
        float cornerRadius = size * 0.25f;
        using var bgPath = CreateRoundedRect(rect, cornerRadius);

        using var gradBrush = new LinearGradientBrush(
            new PointF(0, 0),
            new PointF(size, size),
            AccentPrimary,
            AccentSecondary);

        g.FillPath(gradBrush, bgPath);

        // Subtle inner glow at top
        using var glowBrush = new LinearGradientBrush(
            new PointF(0, 0),
            new PointF(0, size * 0.5f),
            Color.FromArgb(60, 255, 255, 255),
            Color.FromArgb(0, 255, 255, 255));
        g.FillPath(glowBrush, bgPath);

        // "P" glyph — bold, centered
        float fontSize = size switch
        {
            <= 16 => 8.5f,
            <= 24 => 13f,
            <= 32 => 17f,
            _ => 26f
        };

        using var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
        using var textBrush = new SolidBrush(GlyphColor);

        var sf = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        // Slight downward offset for optical centering of "P"
        var textRect = new RectangleF(0, size * 0.02f, size, size);
        g.DrawString("P", font, textBrush, textRect, sf);

        return bmp;
    }

    private static GraphicsPath CreateRoundedRect(RectangleF rect, float radius)
    {
        var path = new GraphicsPath();
        float diameter = radius * 2;

        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }

    /// <summary>
    /// Combines multiple bitmaps into one .ico stream and returns an Icon.
    /// Uses the standard ICO file format: header → entries → PNG data.
    /// </summary>
    private static Icon CreateMultiSizeIcon(List<Bitmap> bitmaps, int[] sizes)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        int count = bitmaps.Count;

        // ICO header
        writer.Write((short)0);      // reserved
        writer.Write((short)1);      // type = ICO
        writer.Write((short)count);  // image count

        // We'll write the directory entries first, then the image data.
        // Collect PNG byte arrays.
        var pngData = new List<byte[]>();
        foreach (var bmp in bitmaps)
        {
            using var pngStream = new MemoryStream();
            bmp.Save(pngStream, System.Drawing.Imaging.ImageFormat.Png);
            pngData.Add(pngStream.ToArray());
        }

        // Directory entries start at offset 6 (header), each entry is 16 bytes
        int dataOffset = 6 + count * 16;

        for (int i = 0; i < count; i++)
        {
            byte w = (byte)(sizes[i] >= 256 ? 0 : sizes[i]);
            byte h = w;

            writer.Write(w);                          // width
            writer.Write(h);                          // height
            writer.Write((byte)0);                    // color palette
            writer.Write((byte)0);                    // reserved
            writer.Write((short)1);                   // color planes
            writer.Write((short)32);                  // bits per pixel
            writer.Write(pngData[i].Length);           // image data size
            writer.Write(dataOffset);                  // offset to image data

            dataOffset += pngData[i].Length;
        }

        // Image data
        foreach (var png in pngData)
            writer.Write(png);

        ms.Position = 0;
        return new Icon(ms);
    }
}
