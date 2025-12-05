using System.Diagnostics;
using BookShopMVC.Models;
using BookShopMVC.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BookShopMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHomeRepository _homeRepository;

        public HomeController(ILogger<HomeController> logger, IHomeRepository homeRepository)
        {
            _logger = logger;
            _homeRepository = homeRepository;
        }
        //the parameters should be the same in the method and in the view(index.cshtml(name and id))
        public async Task<IActionResult> Index(string sTerm = "", int genreId = 0)
        {
            IEnumerable<Book> books = await _homeRepository.GetBooks(sTerm, genreId);
            IEnumerable<Genre> genres = await _homeRepository.GetGenres();
            BookDisplayModel bookModal = new BookDisplayModel
            {
                Books = books,
                Genres = genres,
                STerm = sTerm,
                GenreId = genreId
            };
            return View(bookModal);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
