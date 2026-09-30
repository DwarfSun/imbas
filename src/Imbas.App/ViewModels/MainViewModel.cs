using System.Collections.ObjectModel;
using Imbas.Core;

namespace Imbas.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public MainViewModel()
        : this(new Library())
    {
    }

    public MainViewModel(Library library)
    {
        Books = new ObservableCollection<Book>(library.Books);
    }

    public ObservableCollection<Book> Books { get; }

    public bool IsEmpty => Books.Count == 0;
}
