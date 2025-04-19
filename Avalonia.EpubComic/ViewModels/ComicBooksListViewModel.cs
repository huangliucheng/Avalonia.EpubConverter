using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.EpubComic.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Avalonia.EpubComic.ViewModels;

public partial class ComicBooksListViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<BookViewModel>? _booksList;


    public ComicBooksListViewModel(DispatchCenter dispatcher)
    {
        dispatcher.BooksListChanged += ReceiveBooksList;
    }

    private void ReceiveBooksList(object? sender, List<ComicBookModel> bookList)
    {
        if (BooksList == null)
            BooksList = [];
        else
            BooksList.Clear();

        foreach (var book in bookList) BooksList.Add(new BookViewModel(book));
    }
}

public partial class BookViewModel : ViewModelBase
{
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private ComicBookProgressBarModel? _progressBar;
    [ObservableProperty] private string? _title;

    public BookViewModel(ComicBookModel book)
    {
        Title = book.BookTitle;
        ProgressBar = book.ProgressBar;
        IsBusy = book.IsCompressing;
    }
}