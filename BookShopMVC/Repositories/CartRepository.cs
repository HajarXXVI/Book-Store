using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace BookShopMVC.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CartRepository(ApplicationDbContext db, UserManager<IdentityUser> userManager, IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<int> AddItem(int bookId, int qty)
        {
            string userId = GetUserId();
            using var transaction = _db.Database.BeginTransaction();
            try
            {
                if (string.IsNullOrEmpty(userId))
                    throw new Exception("User is not logged in");

                var cart = await GetCart(userId);
                if (cart == null)
                {
                    cart = new ShoppingCart
                    {
                        UserId = userId
                    };
                    _db.ShoppingCarts.Add(cart);
                }
                _db.SaveChanges();
                //cart detail section
                var cartItem = _db.CartDetails.FirstOrDefault(x => x.ShoppingCartID == cart.Id && x.BookId == bookId);
                if(cartItem is not null)
                {
                    cartItem.Quantity += qty;
                }
                else
                {
                    cartItem = new CartDetail
                    {
                        ShoppingCartID = cart.Id,
                        BookId = bookId,
                        Quantity = qty
                    };
                    _db.CartDetails.Add(cartItem);
                }
                _db.SaveChanges();
                transaction.Commit();
            }
            catch (Exception ex)
            {
            }
            var cartItemCount = await GetCartItemCount(userId);
            return cartItemCount;
        }

        public async Task<int> RemoveItem(int bookId)
        {
            string userId = GetUserId();
            //using var transaction = _db.Database.BeginTransaction();
            try
            {
                if (string.IsNullOrEmpty(userId))
                    throw new Exception("User is not logged in");

                var cart = await GetCart(userId);

                //the user should have a cart to be able to remove an item
                if (cart == null)
                    throw new Exception("Invalid Cart");

                _db.SaveChanges();

                //cart detail section
                var cartItem = _db.CartDetails.FirstOrDefault(x => x.ShoppingCartID == cart.Id && x.BookId == bookId);
                
                if(cartItem is null)
                    throw new Exception("No items in the cart");
                else if (cartItem.Quantity==1)
                    _db.CartDetails.Remove(cartItem);
                else
                    cartItem.Quantity -= 1;

                _db.SaveChanges();
                //transaction.Commit();
            }
            catch (Exception ex)
            {
            }
            var cartItemCount = await GetCartItemCount(userId);
            return cartItemCount;
        }

        public async Task<ShoppingCart> GetUserCart()
        {
            var userId = GetUserId();
            if(userId is null)
            {
                throw new Exception("User not found");
            }
            var shoppingCart = await _db.ShoppingCarts
                                .Include(c => c.CartDetails)
                                .ThenInclude(cd => cd.Book)
                                .ThenInclude(g => g.Genre)
                                .Where(c => c.UserId == userId)
                                .FirstOrDefaultAsync();
            return shoppingCart;
        }

        public async Task<ShoppingCart> GetCart(string userId)
        {
            var cart = await _db.ShoppingCarts.FirstOrDefaultAsync(x => x.UserId == userId);
            return cart;
        }

        public async Task<int> GetCartItemCount(string userId = "")
        {
            if(!string.IsNullOrEmpty(userId))
            {
                userId = GetUserId();
            }
            var data = await (from cart in _db.ShoppingCarts
                              join cartDetail in _db.CartDetails
                              on cart.Id equals cartDetail.ShoppingCartID
                              select new { cartDetail.Id }
                              ).ToListAsync();
            return data.Count;
        }

        private string GetUserId()
        {
            var userId = _userManager.GetUserId(_httpContextAccessor.HttpContext.User);
            return userId;
        }

    }
}
