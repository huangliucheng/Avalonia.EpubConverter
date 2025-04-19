using System;
using System.IO;
using System.Linq;
using System.Text;
using ImageMagick;

namespace Avalonia.EpubComic.Models;

public class ComicImage
{
    /// <summary>
    ///     从文件中读取漫画图片, 并指定保存路径(临时工作文件夹路径)
    /// </summary>
    /// <param name="source">源图像路径</param>
    /// <param name="savePathWithoutExtension">没有扩展名的漫画图片保存路径</param>
    public ComicImage(string source, string savePathWithoutExtension)
    {
        Image = new MagickImage(source);
        OutputPath = savePathWithoutExtension;
        ImageTitle = Path.GetFileName(OutputPath);
        SplitImage();
    }

    /// <summary>
    ///     从MagickImage类型创建漫画图像数据, 用于长页图像数据的创建
    /// </summary>
    /// <param name="image">子页图像数据</param>
    public ComicImage(MagickImage image)
    {
        Image = image;
        ChildImages = [];
    }

    public MagickImage Image { get; set; }
    public ComicImage[] ChildImages { get; set; }
    public uint Width => Image.Width;
    public uint Height => Image.Height;
    public bool HasChild => ChildImages is { Length: > 0 };
    public string ImageTitle { get; set; }
    public string ImageRelativePath { get; set; }
    public string OutputPath { get; set; }
    public string ImageExt { get; set; }

    // 用于修改图片的方法
    private void SplitImage()
    {
        if (Width <= Height) return;
        var halfWidth = Width / 2;
        var leftImage = (MagickImage)Image.Clone();
        leftImage.Crop(new MagickGeometry(0, 0, halfWidth, Height));
        leftImage.ResetPage();
        var rightImage = (MagickImage)Image.Clone();
        rightImage.Crop(new MagickGeometry((int)halfWidth, 0, halfWidth, Height));
        rightImage.ResetPage();

        ChildImages = new[]
        {
            new ComicImage(leftImage),
            new ComicImage(rightImage)
        };
    }

    public void MarginCrop()
    {
        using var tempImage = new MagickImage(Image);
        tempImage.Grayscale();
        tempImage.AutoThreshold(AutoThresholdMethod.OTSU);
        if (tempImage.BoundingBox == null) return;
        var boundingBox = tempImage.BoundingBox;

        // 如果boundingBox的面积小于图像面积的一半，则不进行边缘裁剪
        if (boundingBox.Width * boundingBox.Height < Width * Height / 2) return;

        boundingBox.Width = boundingBox.Width + 20 < Width ? boundingBox.Width + 20 : boundingBox.Width;
        boundingBox.Height = boundingBox.Height + 40 < Height ? boundingBox.Height + 40 : boundingBox.Height;
        boundingBox.X = boundingBox.X - 10 < 0 ? boundingBox.X : boundingBox.X - 10;
        boundingBox.Y = boundingBox.Y - 20 < 0 ? boundingBox.Y : boundingBox.Y - 20;
        Image.Crop(boundingBox);
    }

    public void ConvertToGray()
    {
        Image.Grayscale();
    }

    public void SetImageFormat(bool usePNG)
    {
        ImageExt = usePNG ? ".png" : ".jpg";
        var originalExt = Path.GetExtension(ImageRelativePath);
        if (string.IsNullOrEmpty(originalExt)) ImageRelativePath += ImageExt;
    }

    public void Rotate()
    {
        Image.Rotate(90);
    }

    private void BuildImageXhtml(int targetWidth, int targetHeight)
    {
        var str = string.Concat(Enumerable.Repeat("../", ImageRelativePath.Split('/').Length - 1));
        var topMargin = GetTopMargin(targetHeight);
        var src = str + ImageRelativePath;
        var stylePath = str + "Texts/style.css";
        var savePath = OutputPath.Replace("Images", "Texts") + ".xhtml";

        using (var writer = new StreamWriter(savePath, false, new UTF8Encoding(false)))
        {
            writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            writer.WriteLine("<!DOCTYPE html>");
            writer.WriteLine("<html xmlns=\"http://www.w3.org/1999/xhtml\" " +
                             "xmlns:epub=\"http://www.idpf.org/2007/ops\">");
            writer.WriteLine("<head>");
            writer.WriteLine($"<title>{ImageTitle}</title>");
            writer.WriteLine($"<link href=\"{stylePath}\" type=\"text/css\" rel=\"stylesheet\"/>");
            writer.WriteLine($"<meta name=\"viewport\" content=\"width={targetWidth}, height={targetHeight}\"/>");
            writer.WriteLine("</head>");
            writer.WriteLine("<body>");
            writer.WriteLine($"<div style=\"text-align:center;top:{topMargin}%;\">");
            writer.WriteLine($"<img width=\"{Width}\" height=\"{Height}\" src=\"{src}\"/>");
            writer.WriteLine("</div>");
            writer.WriteLine("</body>");
            writer.WriteLine("</html>");
            writer.Flush();
        }
    }

    private string GetTopMargin(int targetHeight)
    {
        var topMargin = (targetHeight - Height) / 2 * 100 / targetHeight;
        topMargin = topMargin < 0 ? 0 : topMargin;
        return topMargin.ToString("F1");
    }

    /// <summary>
    ///     保存处理后的图片, 并创建对应的xhtml文件
    /// </summary>
    /// <param name="targetWidth">xhtml文件中目标设备的宽度</param>
    /// <param name="targetHeight">xhtml文件中目标设备的高度</param>
    /// <exception cref="Exception">图片保存失败或者xhtml文件写入失败, 抛出异常</exception>
    public void Save(int targetWidth, int targetHeight)
    {
        try
        {
            Image.Write(OutputPath + ImageExt);
            BuildImageXhtml(targetWidth, targetHeight);
        }
        catch (Exception e)
        {
            throw new Exception($"图片处理失败: {OutputPath}", e);
        }
    }
}