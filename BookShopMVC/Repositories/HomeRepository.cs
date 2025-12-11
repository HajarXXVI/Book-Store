
using Microsoft.EntityFrameworkCore;

namespace BookShopMVC.Repositories
{
    public class HomeRepository : IHomeRepository
    {
        private readonly ApplicationDbContext _db;

        public HomeRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Genre>> GetGenres()
        {
            return await _db.Genres.ToListAsync();
        }

        public async Task<IEnumerable<Book>> GetBooks(string sTerm = "", int genreId = 0)
        {
            sTerm = sTerm.ToLower();
            IEnumerable<Book> books = await (from book in _db.Books
                                             join genre in _db.Genres
                                             on book.GenreId equals genre.Id
                                             join stock in _db.Stocks
                                             on book.Id equals stock.BookId
                                             into book_stocks
                                             from bookWithStock in book_stocks.DefaultIfEmpty()
                                             where string.IsNullOrWhiteSpace(sTerm) || (book != null && book.BookName.ToLower().StartsWith(sTerm))
                                             select new Book
                                                {
                                                    Id = book.Id,
                                                    BookName = book.BookName,
                                                    AuthorName = book.AuthorName,
                                                    Price = book.Price,
                                                    Image = book.Image,
                                                    GenreId = book.GenreId,
                                                    GenreName = genre.GenreName,
                                                    Quantity = bookWithStock == null ? 0 : bookWithStock.Quantity
                                             }
                                             ).ToListAsync();
            if (genreId > 0)
            {
                books = [.. books.Where(b => b.GenreId == genreId)];
                //books = books.Where(b => b.GenreId == genreId).ToList();
            }
                return books;
        }
    }
}
