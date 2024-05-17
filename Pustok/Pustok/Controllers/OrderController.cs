using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Pustok.Models;
using Pustok.ViewModels;
using System.Security.Claims;
using System.Text.Json;

namespace Pustok.Controllers
{
    public class OrderController : Controller
    {
        private readonly PustokDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IHubContext<PustokHub> _hubContext;

        public OrderController(PustokDbContext context, UserManager<AppUser> userManager, IHubContext<PustokHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
        }
        public IActionResult Checkout()
        {
			CheckoutViewModel vm = new CheckoutViewModel
			{
				BasketViewModel = getBasket()
			};
			
            return View(vm);
        }

		[Authorize(Roles = "member")]
		[HttpPost]
		[AutoValidateAntiforgeryToken]
		public async Task<IActionResult> Checkout(OrderCreateViewModel orderVM)
		{
			if (!ModelState.IsValid)
			{
                CheckoutViewModel vm = new CheckoutViewModel
                {
                    BasketViewModel = getBasket(),
					Order = orderVM
                };
				return View(vm);
            }

			AppUser user = await _userManager.GetUserAsync(User);

			if (user == null) return RedirectToAction("login", "account");

			Order order = new Order
			{
				Address = orderVM.Address,
				Phone = orderVM.Phone,
				CreatedAt = DateTime.Now,
				AppUserId = user.Id,
				Email = user.Email,
				FullName = user.FullName,
				Note= orderVM.Note,
				Status = Models.Enums.OrderStatus.Pending
			};

			var basketItems = _context.BasketItems.Include(x => x.Book).Where(x => x.AppUserId == user.Id).ToList();

            order.OrderItems = basketItems.Select(x => new OrderItem
			{
				BookId = x.BookId,
				Count = x.Count,
				SalePrice = x.Book.SalePrice,
				DiscountPercent = x.Book.DiscountPercent,
				CostPrice = x.Book.CostPrice,
			}).ToList();
            
			_context.Orders.Add(order);
			_context.BasketItems.RemoveRange(basketItems);
            
			_context.SaveChanges();

			return RedirectToAction("profile", "account", new { tab = "orders" });
		}

        private BasketViewModel getBasket()
        {
			BasketViewModel vm = new BasketViewModel();

			if (User.Identity.IsAuthenticated && User.IsInRole("member"))
			{
				var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

				var basketItems = _context.BasketItems
			   .Include(x => x.Book)
			   .Where(x => x.AppUserId == userId)
			   .ToList();

				vm.Items = basketItems.Select(x => new BasketItemViewModel
				{
					BookId = x.BookId,
					BookName = x.Book.Name,
					BookPrice = x.Book.DiscountPercent > 0 ? (x.Book.SalePrice * (100 - x.Book.DiscountPercent) / 100) : x.Book.SalePrice,
					Count = x.Count
				}).ToList();

				vm.TotalPrice = vm.Items.Sum(x => x.Count * x.BookPrice);
			}
			else
			{
				var cookieBasket = Request.Cookies["basket"];

				if (cookieBasket != null)
				{
					List<BasketCookieItemViewModel> cookieItemsVM = JsonSerializer.Deserialize<List<BasketCookieItemViewModel>>(cookieBasket);
					;
					foreach (var cookieItem in cookieItemsVM)
					{
						Book? book = _context.Books.Include(x => x.BookImages.Where(bi => bi.Status == true)).FirstOrDefault(x => x.Id == cookieItem.BookId && !x.IsDeleted);

						if (book != null)
						{
							BasketItemViewModel itemVM = new BasketItemViewModel
							{
								BookId = cookieItem.BookId,
								Count = cookieItem.Count,
								BookName = book.Name,
								BookPrice = book.DiscountPercent > 0 ? (book.SalePrice * (100 - book.DiscountPercent) / 100) : book.SalePrice,
							};
							vm.Items.Add(itemVM);
						}

					}

					vm.TotalPrice = vm.Items.Sum(x => x.Count * x.BookPrice);
				}
			}

			return vm;
		}

		private List<OrderBasketItemViewModel> getOrderBasket(string userId)
		{
			List<OrderBasketItemViewModel> items = new List<OrderBasketItemViewModel>();

            var basketItems = _context.BasketItems
           .Include(x => x.Book)
           .Where(x => x.AppUserId == userId)
           .ToList();

            items = basketItems.Select(x => new OrderBasketItemViewModel
            {
                BookId = x.BookId,
                Count = x.Count
            }).ToList();


			return items;
        }

		[Authorize(Roles = "member")]
		public IActionResult GetOrderItems(int orderId)
		{
			AppUser user = _userManager.GetUserAsync(User).Result;

			Order order = _context.Orders.Include(x => x.OrderItems).ThenInclude(oi=>oi.Book).FirstOrDefault(x => x.Id == orderId && x.AppUserId == user.Id);

			if (order == null) return RedirectToAction("notfound", "error");

			return PartialView("_OrderDetailPartial", order.OrderItems);
		}

        [Authorize(Roles = "member")]
        public IActionResult Cancel(int id)
		{
            AppUser user = _userManager.GetUserAsync(User).Result;

            Order order = _context.Orders.FirstOrDefault(x => x.Id == id && x.AppUserId == user.Id && x.Status == Models.Enums.OrderStatus.Pending);
            
			if (order == null) return RedirectToAction("notfound", "error");
				
			order.Status = Models.Enums.OrderStatus.Canceled;
			_context.SaveChanges();
			return RedirectToAction("profile", "account", new { tab = "orders" });
        }
    }
}
