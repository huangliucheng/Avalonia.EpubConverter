using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.EpubComic.Tools;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Avalonia.EpubComic.Models;

public partial class ComicBookModel : ObservableObject
{
    private const string XhtmlNamespace = "http://www.w3.org/1999/xhtml";
    private const string EpubNamespace = "http://www.idpf.org/2007/ops";

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private string _bookID = string.Empty;
    [ObservableProperty] private string[]? _bookPath;
    [ObservableProperty] private string? _bookTitle;
    private string _creator = string.Empty;

    [ObservableProperty] private bool _isCompressing;
    private bool _isFinished;
    private string _modifiedTime = string.Empty;

    [ObservableProperty] private ComicBookProgressBarModel _progressBar = new()
    {
        CurrentValue = 0, MaxValue = 1
    };

    public string SourcePath { get; set; }
    public string SavaPath { get; set; }

    // 调用CreateEpub制作漫画书
    public void CreateEpub(SettingModel setting)
    {
        var token = _cancellationTokenSource.Token;
        UpdateProgress(isIndeterminate: true);
        _bookID = "urn:uuid:" + Guid.NewGuid();
        _modifiedTime = DateTime.Now.ToString("yyyy-MM-dd");
        _creator = "EpubComicConverter";

        // 创建临时工作目录
        var tempWorkPath =
            Path.Combine(SavaPath, string.Concat("ECC-", Guid.NewGuid().ToString("N").AsSpan(0, 8)));
        Directory.CreateDirectory(tempWorkPath);
        try
        {
            // 创建container.xml
            ContainerGenerator(tempWorkPath);

            token.ThrowIfCancellationRequested();
            // 创建OEBPS目录
            BuildOEBPS(tempWorkPath, setting);

            token.ThrowIfCancellationRequested();
            // 创建EPUB文件
            CompressEpub(tempWorkPath, Path.Combine(SavaPath, BookTitle + ".epub"));

            token.ThrowIfCancellationRequested();
            // 删除临时工作目录
            Directory.Delete(tempWorkPath, true);
            _isFinished = true;
        }
        catch (OperationCanceledException)
        {
            DeleteTempFiles(tempWorkPath);
            throw new OperationCanceledException();
        }
        catch (Exception e)
        {
            DeleteTempFiles(tempWorkPath);
            throw new Exception(e.Message);
        }
    }

    public void Cancel()
    {
        _cancellationTokenSource?.Cancel();
    }

    // 调用DeleteTempFiles取消创建漫画书
    private void DeleteTempFiles(string tempWorkPath)
    {
        if (_isFinished)
        {
            File.Delete(Path.Combine(SavaPath, BookTitle + ".epub"));
        }
        else
        {
            // 删除临时工作目录
            Directory.Delete(tempWorkPath, true);
            if (File.Exists(Path.Combine(SavaPath, BookTitle + ".epub")))
                File.Delete(Path.Combine(SavaPath, BookTitle + ".epub"));
            IsCompressing = false;
            UpdateProgress();
        }
    }

    private void CompressEpub(string sourcePath, string zipPath)
    {
        if (File.Exists(zipPath)) File.Delete(zipPath);
        try
        {
            IsCompressing = true;
            var filesPath = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);
            UpdateProgress(0, filesPath.Length);
            using (var fileStream = new FileStream(zipPath, FileMode.Create))
            {
                using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
                {
                    var mimetypeEntry = archive.CreateEntry("mimetype", CompressionLevel.NoCompression);
                    using (var writer = new StreamWriter(mimetypeEntry.Open()))
                    {
                        writer.Write("application/epub+zip");
                    }

                    var i = 1;
                    foreach (var file in filesPath)
                    {
                        var relativePath = Path.GetRelativePath(sourcePath, file).Replace('\\', '/');
                        archive.CreateEntryFromFile(file, relativePath);
                        UpdateProgress(i++, filesPath.Length);
                    }
                }
            }
        }
        catch (Exception e)
        {
            IsCompressing = false;
            throw new Exception("打包失败", e);
        }
    }

    private void BuildOEBPS(string savaPath, SettingModel setting)
    {
        var oebpsPath = Path.Combine(savaPath, "OEBPS");
        var imageSavePath = Path.Combine(oebpsPath, "Images");
        var textSavePath = Path.Combine(oebpsPath, "Texts");
        Directory.CreateDirectory(oebpsPath);
        Directory.CreateDirectory(imageSavePath);
        Directory.CreateDirectory(textSavePath);

        StyleSheetGenerator(textSavePath);

        List<string> sourceRelativePath = [];
        List<string> epubRelativePath = [];
        List<string> chapterNameList = [];
        List<string> chapterLinkList = [];

        var rootNode = new TreeNode("", 0);
        // 如果是模式二的分卷输出, 则根据BookPath列表中的路径建立文件结构, 然后整合到root节点
        // 否则, 则直接根据路径建立文件结构
        if (BookPath.Length > 1)
            foreach (var path in BookPath)
            {
                var node = ComicStructure.BuildTree(path);
                rootNode.AddChild(node);
            }
        else
            rootNode = ComicStructure.BuildTree(BookPath[0]);

        // 复制文件结构, 生成EPUB文件的文件结构
        var epubRootNode = rootNode.Clone();
        ComicStructure.NodeValueReplace(epubRootNode);
        ComicStructure.GeneratePathList(sourceRelativePath, rootNode); // 获得源文件的相对路径列表
        ComicStructure.GeneratePathList(epubRelativePath, epubRootNode); // 获得EPUB文件的相对路径列表
        epubRelativePath = epubRelativePath
            .Select((path, index) => string.Concat(path.AsSpan(0, path.Length - 4), $"{index + 1:D4}"))
            .ToList();

        ComicStructure.GetChapterNameList(rootNode, chapterNameList); // 获得章节名称列表

        // 确保所有父目录都已创建
        foreach (var path in epubRelativePath)
        {
            var savePath = Path.Combine(imageSavePath, path);
            var parentDirectory = Path.GetDirectoryName(savePath);
            if (!Directory.Exists(parentDirectory)) Directory.CreateDirectory(parentDirectory);

            savePath = Path.Combine(textSavePath, path);
            parentDirectory = Path.GetDirectoryName(savePath);
            if (!Directory.Exists(parentDirectory)) Directory.CreateDirectory(parentDirectory);
        }

        // 并行处理图片
        Parallel.For(0, sourceRelativePath.Count, i =>
        {
            var sourcePath = Path.Combine(SourcePath, sourceRelativePath[i]);
            var savePathWithOutExtension = Path.Combine(imageSavePath, epubRelativePath[i]);

            var image = new ComicImage(sourcePath, savePathWithOutExtension);
            image.ImageRelativePath = "Images" + "/" + epubRelativePath[i];
            ImageProcess.Start(image, setting);
        });

        // 读取OEBPS文件目录下所有的图片
        var imageFilesNode = ComicStructure.BuildTree(imageSavePath);
        // 获得Images下所有路径
        List<string> imageFilesRelativePath = [];
        ComicStructure.GeneratePathList(imageFilesRelativePath, imageFilesNode);
        // 获得每个章节首页的路径
        ComicStructure.GetChapterLinkList(imageFilesNode.Children[0], chapterLinkList, "Texts");

        var navListItem = GenerateNavListItem(chapterNameList, chapterLinkList);
        var ncxListItem = GenerateNcxListItem(chapterNameList, chapterLinkList);
        BuildNav(oebpsPath, navListItem);
        BuildNcx(oebpsPath, ncxListItem);
        BuildContent(oebpsPath, setting, imageFilesRelativePath);

        // 获得封面
        var coverSourcePath = Path.Combine(oebpsPath, imageFilesRelativePath[0]);
        var coverSavePath = Path.Combine(imageSavePath, "cover" + Path.GetExtension(coverSourcePath));
        File.Copy(coverSourcePath, coverSavePath, true);
    }

    private void BuildContent(string savePath, SettingModel setting, List<string> imageFilesRelativePath)
    {
        var resolution = setting.TargetWidth + "x" + setting.TargetHeight;
        var imageExt = setting.UsePNG ? "png" : "jpg";
        var imageType = setting.UsePNG ? "image/png" : "image/jpeg";
        var direction = setting.MangeMode ? "rtl" : "ltr";
        string[] properties = setting.MangeMode
            ? ["rendition:page-spread-right", "rendition:page-spread-left"]
            : ["rendition:page-spread-left", "rendition:page-spread-right"];

        var content = new StringBuilder();
        content.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        content.AppendLine(
            "<package version=\"3.0\" unique-identifier=\"BookID\" xmlns=\"http://www.idpf.org/2007/opf\">");
        content.AppendLine(
            "<metadata xmlns:opf=\"http://www.idpf.org/2007/opf\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\">");
        content.AppendLine($"<dc:title>{BookTitle}</dc:title>");
        content.AppendLine("<dc:language>zh-CN</dc:language>");
        content.AppendLine($"<dc:identifier id=\"BookID\">{_bookID}</dc:identifier>");
        content.AppendLine("<dc:contributor id=\"contributor\">EpubComicConverter</dc:contributor>");
        content.AppendLine($"<dc:creator>{_creator}</dc:creator>");
        content.AppendLine($"<meta property=\"dcterms:modified\">{_modifiedTime}</meta>");
        content.AppendLine("<meta name=\"cover\" content=\"cover\"/>");
        content.AppendLine("<meta property=\"rendition:spread\">landscape</meta>");
        content.AppendLine("<meta property=\"rendition:layout\">pre-paginated</meta>");
        content.AppendLine("</metadata>");
        content.AppendLine("<manifest>");
        content.AppendLine("<item id=\"ncx\" href=\"toc.ncx\" media-type=\"application/x-dtbncx+xml\"/>");
        content.AppendLine(
            "<item id=\"nav\" href=\"nav.xhtml\" properties=\"nav\" media-type=\"application/xhtml+xml\"/>");
        content.AppendLine(
            $"<item id=\"cover\" href=\"Images/cover.{imageExt}\" media-type=\"{imageType}\" properties=\"cover-image\"/>");

        List<string> TextIDs = [];
        for (var i = 0; i < imageFilesRelativePath.Count; i++)
        {
            var textFilesRelativePath = imageFilesRelativePath[i].Replace("Images", "Texts").Replace(imageExt, "xhtml");
            var imageID = imageFilesRelativePath[i].Replace('/', '-').Replace("." + imageExt, "");
            var textID = imageID.Replace("Images", "Texts").Replace("." + imageExt, "");
            TextIDs.Add(textID);
            content.AppendLine(
                $"<item id=\"{textID}\" href=\"{textFilesRelativePath}\" media-type=\"application/xhtml+xml\"/>");
            content.AppendLine(
                $"<item id=\"{imageID}\" href=\"{imageFilesRelativePath[i]}\" media-type=\"{imageType}\"/>");
        }

        content.AppendLine("<item id=\"css\" href=\"Texts/style.css\" media-type=\"text/css\"/>");
        content.AppendLine("</manifest>");
        content.AppendLine($"<spine page-progression-direction=\"{direction}\" toc=\"ncx\">");

        var index = 0;
        foreach (var textID in TextIDs)
        {
            var rendition = properties[index % 2];
            content.AppendLine($"<itemref idref=\"{textID}\" properties=\"{rendition}\"/>");
            index++;
        }

        content.AppendLine("</spine>");
        content.AppendLine("</package>");

        using (var writer = new StreamWriter(Path.Combine(savePath, "content.opf"), false, new UTF8Encoding(false)))
        {
            writer.Write(content.ToString());
        }
    }

    // 创建nav.xhtml
    private void BuildNav(string savaPath, List<string> navListItem)
    {
        try
        {
            using (var writer = new StreamWriter(Path.Combine(savaPath, "nav.xhtml"), false, new UTF8Encoding(false)))
            {
                writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                writer.WriteLine("<!DOCTYPE html>");
                writer.WriteLine($"<html xmlns=\"{XhtmlNamespace}\" xmlns:epub=\"{EpubNamespace}\">");
                writer.WriteLine("<head>");
                writer.WriteLine("<title>" + BookTitle + "</title>");
                writer.WriteLine("<meta charset=\"utf-8\"/>");
                writer.WriteLine("</head>");
                writer.WriteLine("<body>");
                writer.WriteLine($"<nav xmlns:epub=\"{EpubNamespace}\" epub:type=\"toc\" id=\"toc\">");
                writer.WriteLine("<ol>");
                foreach (var item in navListItem) writer.WriteLine(item);
                writer.WriteLine("</ol>");
                writer.WriteLine("</nav>");
                writer.WriteLine("<nav epub:type=\"page-list\">");
                writer.WriteLine("<ol>");
                foreach (var item in navListItem) writer.WriteLine(item);
                writer.WriteLine("</ol>");
                writer.WriteLine("</nav>");
                writer.WriteLine("</body>");
                writer.WriteLine("</html>");
                writer.Flush();
            }
        }
        catch (Exception e)
        {
            throw new Exception("Nav文件写入异常", e);
        }
    }

    // 创建toc.ncx
    private void BuildNcx(string savaPath, List<string> ncxListItem)
    {
        try
        {
            using (var writer = new StreamWriter(Path.Combine(savaPath, "toc.ncx"), false, new UTF8Encoding(false)))
            {
                writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                writer.WriteLine("<ncx xmlns=\"http://www.daisy.org/z3986/2005/ncx/\" version=\"2005-1\">");
                writer.WriteLine("<head>");
                writer.WriteLine("<meta name=\"dtb:uid\" content=\"" + _bookID + "\"/>");
                writer.WriteLine("<meta name=\"dtb:depth\" content=\"1\"/>");
                writer.WriteLine("<meta name=\"dtb:totalPageCount\" content=\"0\"/>");
                writer.WriteLine("<meta name=\"dtb:maxPageNumber\" content=\"0\"/>");
                writer.WriteLine("</head>");
                writer.WriteLine("<docTitle>");
                writer.WriteLine("<text>" + BookTitle + "</text>");
                writer.WriteLine("</docTitle>");
                writer.WriteLine("<navMap>");
                foreach (var item in ncxListItem) writer.WriteLine(item);
                writer.WriteLine("</navMap>");
                writer.WriteLine("</ncx>");
                writer.Flush();
            }
        }
        catch (Exception e)
        {
            throw new Exception("Toc文件写入异常", e);
        }
    }

    private static void ContainerGenerator(string savePath)
    {
        var dirPath = Path.Combine(savePath, "META-INF");
        Directory.CreateDirectory(dirPath);
        var content = new StringBuilder();
        content.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        content.AppendLine("<container version=\"1.0\" xmlns=\"urn:oasis:names:tc:opendocument:xmlns:container\">");
        content.AppendLine("<rootfiles>");
        content.AppendLine(
            "<rootfile full-path=\"OEBPS/content.opf\" media-type=\"application/oebps-package+xml\"/>");
        content.AppendLine("</rootfiles>");
        content.AppendLine("</container>");
        var filePath = Path.Combine(dirPath, "container.xml");
        using var writer = new StreamWriter(filePath, false, new UTF8Encoding(false));
        writer.Write(content.ToString());
    }

    private static void StyleSheetGenerator(string savePath)
    {
        var content = "@page {\r\nmargin: 0;\r\n}\r\nbody {\r\ndisplay: block;\r\nmargin: 0;\r\npadding: 0;\r\n}";
        File.WriteAllText(Path.Combine(savePath, "style.css"), content, new UTF8Encoding(false));
    }

    // 制作Nav.xhtml中的目录列表
    private static List<string> GenerateNavListItem(List<string> chapterNameList, List<string> chapterLinkList)
    {
        return chapterNameList.Select((t, i) => $"<li><a href=\"{chapterLinkList[i]}\">{t}</a></li>").ToList();
    }

    // 制作toc.ncx中的目录列表
    private static List<string> GenerateNcxListItem(List<string> chapterNameList, List<string> chapterLinkList)
    {
        var ncxListItem = new List<string>();
        for (var i = 0; i < chapterNameList.Count; i++)
        {
            var id = chapterLinkList[i].Replace("/", "-").Replace(".xhtml", "");
            var item = new StringBuilder();
            item.AppendLine($"<navPoint id=\"{id}\">");
            item.AppendLine("<navLabel>");
            item.AppendLine($"<text>{chapterNameList[i]}</text>");
            item.AppendLine("</navLabel>");
            item.AppendLine($"<content src=\"{chapterLinkList[i]}\"/>");
            item.AppendLine("</navPoint>");

            ncxListItem.Add(item.ToString());
        }

        return ncxListItem;
    }

    private void UpdateProgress(int current = 0, int max = 1, bool isIndeterminate = false)
    {
        ProgressBar.CurrentValue = current;
        ProgressBar.MaxValue = max;
        ProgressBar.IsIndeterminate = isIndeterminate;
        // 触发属性更改通知
        OnPropertyChanged(nameof(ProgressBar));
    }
}