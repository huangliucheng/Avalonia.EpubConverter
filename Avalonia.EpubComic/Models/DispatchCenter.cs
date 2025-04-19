using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.EpubComic.Tools;

namespace Avalonia.EpubComic.Models;

public class DispatchCenter
{
    private readonly List<ComicBookModel> _bookList = [];

    private readonly SettingModel _setting;
    private List<string> _bookPaths = [];
    private string _comicTitle = string.Empty;
    private string _inputPath = string.Empty;

    public DispatchCenter(SettingModel setting)
    {
        _setting = setting;
        _setting.EveryChaptersNumChanged += ReceivedEveryChaptersNumbChanged;
        _setting.SelectedOutputModeChanged += ReceivedOutputModeChanged;
    }


    public void GetBooksPath(string sourcePath)
    {
        _inputPath = sourcePath;
        _bookPaths = Directory.GetDirectories(sourcePath).OrderBy(f => f, new NaturalStringComparer()).ToList();
        _setting.MaxEveryChaptersNum = _bookPaths.Count == 0 ? 1 : _bookPaths.Count;
        if (_bookPaths.Count == 0 || _setting.SelectedOutputMode == 0) _bookPaths = [sourcePath];
    }

    public async Task CreateParseFactorAsync(string comicTitle)
    {
        _comicTitle = comicTitle;
        try
        {
            await Task.Run(() =>
            {
                switch (_setting.SelectedOutputMode)
                {
                    case 0:
                        CreateBookList(comicTitle, _bookPaths[0]);
                        break;
                    case 1:
                        CreateBookList(comicTitle, _bookPaths, _setting.EveryChaptersNum);
                        break;
                    case 2:
                        CreateBookList(comicTitle, _bookPaths);
                        break;
                }
            });
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }

        OnBooksListChanged(_bookList);
    }

    // 单本模式
    private void CreateBookList(string comicTitle, string path)
    {
        _bookList.Add(
            new ComicBookModel
            {
                BookTitle = comicTitle,
                BookPath = [path],
                ProgressBar = new ComicBookProgressBarModel(),
                SourcePath = Path.GetDirectoryName(path)
            }
        );
    }

    // 连载模式
    private void CreateBookList(string comicTitle, List<string> pathList, int groupSize)
    {
        var bookNumber = 1;
        foreach (var pathGroup in ChunkArray(pathList.ToArray(), groupSize))
        {
            var book = new ComicBookModel
            {
                BookTitle = comicTitle + $"_{bookNumber++:D2}",
                BookPath = pathGroup,
                ProgressBar = new ComicBookProgressBarModel()
            };
            _bookList.Add(book);
        }
    }

    // 分卷模式
    private void CreateBookList(string comicTitle, List<string> pathList)
    {
        var bookNumber = 1;
        foreach (var path in pathList)
        {
            var book = new ComicBookModel
            {
                BookTitle = comicTitle + $"_{bookNumber++:D2}",
                BookPath = [path],
                ProgressBar = new ComicBookProgressBarModel()
            };
            _bookList.Add(book);
        }
    }

    // 开始创建漫画书
    public async Task BuildComicBookAsync(string sourcePath, string savePath)
    {
        if (_bookPaths.Count == 0) throw new Exception("没有添加书籍!");
        foreach (var book in _bookList)
        {
            book.SourcePath = string.IsNullOrEmpty(book.SourcePath) ? sourcePath : book.SourcePath;
            book.SavaPath = savePath;
            try
            {
                await Task.Run(() => book.CreateEpub(_setting));
            }
            catch (OperationCanceledException)
            {
                throw new OperationCanceledException();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
    }

    // 取消创建漫画书
    public async Task CancelComicBookAsync()
    {
        foreach (var book in _bookList)
            try
            {
                await Task.Run(() => book.Cancel());
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
    }

    // 修改漫画书标题
    public void ReplaceBookTitle(string newTitle)
    {
        for (var i = 0; i < _bookList.Count; i++) _bookList[i].BookTitle = newTitle + $"_{i + 1:D2}";
        OnBooksListChanged(_bookList);
    }

    public void ClearBookList()
    {
        _bookList.Clear();
        OnBooksListChanged(_bookList);
    }

    private static IEnumerable<string[]> ChunkArray(string[] pathArray, int chunkSize)
    {
        for (var i = 0; i < pathArray.Length; i += chunkSize)
            yield return pathArray[i..Math.Min(i + chunkSize, pathArray.Length)];
    }

    public event EventHandler<List<ComicBookModel>>? BooksListChanged;

    private void OnBooksListChanged(List<ComicBookModel> books)
    {
        BooksListChanged?.Invoke(this, books);
    }

    private void ReceivedOutputModeChanged(object? sender, int value)
    {
        if (_bookList.Count == 0) return;
        try
        {
            switch (value)
            {
                case 0:
                    if (_bookPaths.Count == 1) return;
                    _bookList.Clear();
                    GetBooksPath(_inputPath);
                    CreateBookList(_comicTitle, _bookPaths);
                    OnBooksListChanged(_bookList);
                    break;
                case 1:
                    _bookList.Clear();
                    GetBooksPath(_inputPath);
                    CreateBookList(_comicTitle, _bookPaths, _setting.EveryChaptersNum);
                    OnBooksListChanged(_bookList);
                    break;
                case 2:
                    _bookList.Clear();
                    GetBooksPath(_inputPath);
                    CreateBookList(_comicTitle, _bookPaths);
                    OnBooksListChanged(_bookList);
                    break;
            }
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }

    private void ReceivedEveryChaptersNumbChanged(object? sender, int value)
    {
        try
        {
            _bookList.Clear();
            CreateBookList(_comicTitle, _bookPaths, value);
            OnBooksListChanged(_bookList);
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }
}