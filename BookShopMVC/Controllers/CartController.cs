using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BookShopMVC.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ICartRepository _cartRepo;

        public CartController(ICartRepository cartRepo)
        {
            _cartRepo = cartRepo;
        }
        public async Task<IActionResult> AddItem(int bookId, int qty=1, int redirect = 0)
        {
            var cartItem = await _cartRepo.AddItem(bookId, qty);
            if (redirect == 0)
                return Ok(cartItem);
            return RedirectToAction("GetUserCart");
        }

        public async Task<IActionResult> RemoveItem(int bookId)
        {
            var cartCount = await _cartRepo.RemoveItem(bookId);
            return RedirectToAction("GetUserCart");
        }

        public async Task<IActionResult> GetUserCart()
        {
            var cart = await _cartRepo.GetUserCart();
            return View();
        }

        public async Task<IActionResult> GetTotalItemInCart()
        {
            int cartItemCount = await _cartRepo.GetCartItemCount();
            return Ok(cartItemCount);
        }
    }
}
