using System;
using Avalonia.EpubComic.Models;

namespace Avalonia.EpubComic.Tools;

public class ImageProcess
{
    public static void Start(ComicImage image, SettingModel setting)
    {
        if (setting.DisProcess) return;

        image.SetImageFormat(setting.UsePNG);

        if (image.HasChild && setting.Split)
        {
            if (setting.MangeMode) Array.Reverse(image.ChildImages);

            var i = 1;
            foreach (var child in image.ChildImages)
            {
                child.ImageTitle = image.ImageTitle + $"-{i:D2}";
                child.OutputPath = image.OutputPath.Replace(image.ImageTitle, child.ImageTitle);
                child.ImageRelativePath = image.ImageRelativePath.Replace(image.ImageTitle, child.ImageTitle);
                Start(child, setting);
                i++;
            }
        }
        else
        {
            if (setting.GrayMode) image.ConvertToGray();
            if (setting.Rotate) image.Rotate();
            if (setting.MarginCrop) image.MarginCrop();

            var targetWidth = setting.TargetWidth;
            var targetHeight = setting.TargetHeight;

            var scale = Math.Min((double)targetWidth / image.Width, (double)targetHeight / image.Height);

            var newWidth = (uint)Math.Round(image.Width * scale);
            var newHeight = (uint)Math.Round(image.Height * scale);
            image.Image.Resize(newWidth, newHeight);
            image.Save(targetWidth, targetHeight);
        }
    }
}