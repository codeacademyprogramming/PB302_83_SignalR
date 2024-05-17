using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Pustok.Models;
using Pustok.ViewModels;

namespace Pustok.Controllers
{
    public class ShopController : Controller
    {
        private readonly PustokDbContext _context;

        public ShopController(PustokDbContext context)
        {
            _context = context;
        }
        public IActionResult Index(int? genreId = null, List<int>? authorIds = null, List<int>? tagIds = null, decimal? minPrice = null, decimal? maxPrice = null, string sort = "AToZ")
        {
            ShopViewModel vm = new ShopViewModel
            {
                Authors = _context.Authors.Include(x => x.Books).ToList(),
                Genres = _context.Genres.Include(x => x.Books).ToList(),
                Tags = _context.Tags.ToList()
            };

            var query = _context.Books.Include(x => x.BookImages.Where(bi => bi.Status != null))
                                      .Include(x => x.Author).AsQueryable();

            if (genreId != null)
                query = query.Where(x => x.GenreId == genreId);
            if (authorIds != null)
                query = query.Where(x => authorIds.Contains(x.AuthorId));
            if (minPrice != null && maxPrice != null)
                query = query.Where(x => x.SalePrice >= minPrice && x.SalePrice <= maxPrice);
            if (tagIds != null)
                query = query.Where(x => x.BookTags.Any(bt => tagIds.Contains(bt.TagId)));

            switch (sort)
            {
                case "ZToA":
                    query = query.OrderByDescending(x => x.Name);
                    break;
                case "LowToHigh":
                    query = query.OrderBy(x => x.SalePrice);
                    break;
                case "HighToLow":
                    query = query.OrderByDescending(x => x.SalePrice);
                    break;
                default:
                    query = query.OrderBy(x => x.Name);
                    break;
            }

            vm.Books = query.ToList();

            ViewBag.GenreId = genreId;
            ViewBag.AuthorIds = authorIds;
            ViewBag.TagIds = tagIds;
            ViewBag.MinPrice = _context.Books.Where(x => !x.IsDeleted).Min(x => x.SalePrice);
            ViewBag.MaxPrice = _context.Books.Where(x => !x.IsDeleted).Max(x => x.SalePrice);
            ViewBag.SelectedMinPrice = minPrice ?? ViewBag.MinPrice;
            ViewBag.SelectedMaxPrice = maxPrice ?? ViewBag.MaxPrice;
            ViewBag.Sort = sort;
            ViewBag.SortItems = new List<SelectListItem>
            {
                new SelectListItem("Default Sorting (A - Z)","AToZ",sort == "AToZ"),
                 new SelectListItem("Sort By:Name (Z - A)","ZToA",sort == "ZToA"),
                  new SelectListItem(" Sort By:Price (Low &gt; High)","LowToHigh",sort == "LowToHigh"),
                   new SelectListItem("Sort By:Price (High &gt; Low)","HighToLow",sort == "HighToLow")
            };

         


            return View(vm);
        }
    }
}
