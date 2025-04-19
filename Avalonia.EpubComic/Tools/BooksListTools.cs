using System.Collections.Generic;
using System.IO;
using Avalonia.EpubComic.Models;

namespace Avalonia.EpubComic.Tools;

public class BooksListTools
{
    public List<ComicBookModel> CreateBooksList(string inputPath, string[] booksPaths, SettingModel setting)
    {
        var booksList = new List<ComicBookModel>();
        foreach (var path in booksPaths)
        {
            var book = new ComicBookModel
            {
                BookTitle = Path.GetFileName(Path.TrimEndingDirectorySeparator(path)),
                BookPath = [path],
                IsCompressing = false,
                ProgressBar = new ComicBookProgressBarModel { MaxValue = 1, CurrentValue = 0 }
            };
            booksList.Add(book);
        }

        return booksList;
    }
}