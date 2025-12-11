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
                    throw new UnauthorizedAccessException("user is not logged-in");

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
                    var book = _db.Books.Find(bookId);
                    cartItem = new CartDetail
                    {
                        BookId = bookId,
                        ShoppingCartID = cart.Id,
                        Quantity = qty,
                        UnitPrice = book.Price //new line added after the update
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
                    throw new UnauthorizedAccessException("user is not logged-in");

                var cart = await GetCart(userId);

                //the user should have a cart to be able to remove an item
                if (cart == null)
                    throw new InvalidOperationException("Invalid cart");

                //_db.SaveChanges();

                //cart detail section
                var cartItem = _db.CartDetails.FirstOrDefault(x => x.ShoppingCartID == cart.Id && x.BookId == bookId);
                
                if(cartItem is null)
                    throw new InvalidOperationException("Not items in cart");
                else if (cartItem.Quantity==1)
                    _db.CartDetails.Remove(cartItem);
                else
                    cartItem.Quantity = cartItem.Quantity - 1;

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
                throw new InvalidOperationException("Invalid userid");
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
            // If a userId wasn't provided, get it from the current context
            if (string.IsNullOrEmpty(userId))
            {
                userId = GetUserId();
            }

            if (string.IsNullOrEmpty(userId))
                return 0;

            var cart = await _db.ShoppingCarts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null)
                return 0;

            // Sum the Quantity values for that cart (returns 0 if no items)
            var total = await _db.CartDetails
                                 .Where(cd => cd.ShoppingCartID == cart.Id)
                                 .SumAsync(cd => (int?)cd.Quantity) ?? 0;

            return total;
        }


        public async Task<bool> DoCheckout(CheckoutModel model)
        {
            using var transaction = _db.Database.BeginTransaction();
            try
            {
                // logic
                // move data from cartDetail to order and order detail then we will remove cart detail
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                    throw new UnauthorizedAccessException("User is not logged-in");
                var cart = await GetCart(userId);
                if (cart is null)
                    throw new InvalidOperationException("Invalid cart");
                var cartDetail = _db.CartDetails
                                    .Where(a => a.ShoppingCartID == cart.Id).ToList();
                if (cartDetail.Count == 0)
                    throw new InvalidOperationException("Cart is empty");
                var pendingRecord = _db.OrderStatuses.FirstOrDefault(s => s.StatusName == "Pending");
                if (pendingRecord is null)
                    throw new InvalidOperationException("Order status does not have Pending status");
                var order = new Order
                {
                    UserId = userId,
                    CreateDate = DateTime.UtcNow,
                    OrderStatusId = pendingRecord.Id,
                    Name = model.Name,
                    Email = model.Email,
                    MobileNumber = model.MobileNumber,
                    PaymentMethod = model.PaymentMethod,
                    Address = model.Address,
                    IsPaid = false,
                    
                };
                _db.Orders.Add(order);
                _db.SaveChanges();
                foreach (var item in cartDetail)
                {
                    var orderDetail = new OrderDetail
                    {
                        BookId = item.BookId,
                        OrderId = order.Id,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    };
                    _db.OrderDetails.Add(orderDetail);

                    // update stock here

                    //    var stock = await _db.Stocks.FirstOrDefaultAsync(a => a.BookId == item.BookId);
                    //    if (stock == null)
                    //    {
                    //        throw new InvalidOperationException("Stock is null");
                    //    }

                    //    if (item.Quantity > stock.Quantity)
                    //    {
                    //        throw new InvalidOperationException($"Only {stock.Quantity} items(s) are available in the stock");
                    //    }
                    //    // decrease the number of quantity from the stock table
                    //    stock.Quantity -= item.Quantity;
                    //}
                    ////_db.SaveChanges();

                    //// removing the cartdetails
                    
                }
                _db.SaveChanges();

                _db.CartDetails.RemoveRange(cartDetail);
                _db.SaveChanges();
                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                throw;
                //return false;
            }
        }


        private string GetUserId()
        {
            var userId = _userManager.GetUserId(_httpContextAccessor.HttpContext.User);
            return userId;
        }

    }
}
