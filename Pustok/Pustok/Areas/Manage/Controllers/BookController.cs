using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pustok.Areas.Manage.ViewModels;
using Pustok.Helpers;
using Pustok.Models;

namespace Pustok.Areas.Manage.Controllers
{
    [Authorize(Roles = "admin,super_admin")]
    [Area("manage")]
    public class BookController : Controller
    {
        private readonly PustokDbContext _context;
        private readonly IWebHostEnvironment _env;

        public BookController(PustokDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public IActionResult setSession()
        {
            HttpContext.Session.SetString("name", "Abbas");
            return Content("");
        }

        public IActionResult getSession()
        {
            return Content(HttpContext.Session.GetString("name"));
        }

        public IActionResult setCookie()
        {
            CookieOptions option = new CookieOptions();
            option.Expires = DateTime.Now.AddMinutes(10);

            HttpContext.Response.Cookies.Append("name", "Hikmet",option);
            return Content("");
        }

        public IActionResult getCookie()
        {
            return Content(HttpContext.Request.Cookies["name"]);
        }

        public IActionResult Index(int page=1)
        {
            var query = _context.Books.Include(x => x.Author).Include(x => x.Genre).Include(x => x.BookImages.Where(x => x.Status == true)).Where(x=>!x.IsDeleted).OrderByDescending(x => x.Id);
            
            return View(PaginatedList<Book>.Create(query,page,2));
        }

        public IActionResult Create()
        {
            ViewBag.Authors = _context.Authors.ToList();
            ViewBag.Genres = _context.Genres.ToList();
            ViewBag.Tags = _context.Tags.ToList();

            return View();
        }

        [HttpPost]
        public IActionResult Create(Book book)
        {
            if (book.PosterFile == null) ModelState.AddModelError("PosterFile", "PosterFile is required");

            if (!ModelState.IsValid)
            {
                ViewBag.Authors = _context.Authors.ToList();
                ViewBag.Genres = _context.Genres.ToList();
                ViewBag.Tags = _context.Tags.ToList();

                return View(book);
            }

            if (!_context.Authors.Any(x => x.Id == book.AuthorId))
                return RedirectToAction("notfound", "error");

            if (!_context.Genres.Any(x => x.Id == book.GenreId))
                return RedirectToAction("notfound", "error");

            foreach (var tagId in book.TagIds)
            {
                if(!_context.Tags.Any(x=>x.Id == tagId)) return RedirectToAction("notfound", "error");

                //BookTag bookTag = new BookTag
                //{
                //    TagId = tagId,
                //    Book = book
                //};
                //_context.BookTags.Add(bookTag);
                BookTag bookTag = new BookTag
                {
                    TagId = tagId,
                };
                book.BookTags.Add(bookTag);
            }
    

            BookImage poster = new BookImage
            {
                Name = FileManager.Save(book.PosterFile, _env.WebRootPath, "uploads/book"),
                Status = true,
            };
            book.BookImages.Add(poster);

            foreach (var imgFile in book.ImageFiles)
            {
                BookImage bookImg = new BookImage
                {
                    Name = FileManager.Save(imgFile, _env.WebRootPath, "uploads/book"),
                    Status = null,
                };
                book.BookImages.Add(bookImg);
            }
            
            _context.Books.Add(book);

            _context.SaveChanges();

            return RedirectToAction("index");
        }
        public IActionResult Edit(int id)
        {
            Book book = _context.Books.Include(x => x.BookImages).Include(x=>x.BookTags).FirstOrDefault(x => x.Id == id);

            if(book == null) return RedirectToAction("notfound", "error");

            ViewBag.Authors = _context.Authors.ToList();
            ViewBag.Genres = _context.Genres.ToList();
            ViewBag.Tags = _context.Tags.ToList();
            book.TagIds = book.BookTags.Select(x => x.TagId).ToList();

            return View(book);
        }

        [HttpPost]
        public IActionResult Edit(Book book)
        {
            Book? existBook = _context.Books.Include(x=>x.BookImages).Include(x => x.BookTags).FirstOrDefault(x => x.Id == book.Id);

            if (existBook == null) return RedirectToAction("notfound", "error");

            if (book.AuthorId!=existBook.AuthorId && !_context.Authors.Any(x => x.Id == book.AuthorId))
                return RedirectToAction("notfound", "error");

            if (book.GenreId != existBook.GenreId && !_context.Genres.Any(x => x.Id == book.GenreId))
                return RedirectToAction("notfound", "error");

            existBook.BookTags.RemoveAll(x => !book.TagIds.Contains(x.TagId));

            foreach (var tagId in book.TagIds.FindAll(x => !existBook.BookTags.Any(bt => bt.TagId == x)))
            {
                if (!_context.Tags.Any(x => x.Id == tagId)) return RedirectToAction("notfound", "error");

                BookTag bookTag = new BookTag
                {
                    TagId = tagId,
                };
                existBook.BookTags.Add(bookTag);
            }

            List<string> removedFileNames = new List<string>();

            List<BookImage> removedImages = existBook.BookImages.FindAll(x =>x.Status==null && !book.BookImageIds.Contains(x.Id));
            removedFileNames = removedImages.Select(x=>x.Name).ToList();

            _context.BookImages.RemoveRange(removedImages);
            if (book.PosterFile != null)
            {
                BookImage poster = existBook.BookImages.FirstOrDefault(x => x.Status == true);
                if (poster == null)
                {
                    poster = new BookImage();
                    existBook.BookImages.Add(poster);
                }
                else
                    removedFileNames.Add(poster.Name);

               
                poster.Name = FileManager.Save(book.PosterFile, _env.WebRootPath, "uploads/book");
            }

            foreach (var imgFile in book.ImageFiles)
            {
                BookImage bookImg = new BookImage
                {
                    Name = FileManager.Save(imgFile, _env.WebRootPath, "uploads/book"),
                    Status = null,
                };
                existBook.BookImages.Add(bookImg);
            }



            existBook.Name = book.Name;
            existBook.Desc = book.Desc;
            existBook.SalePrice = book.SalePrice;
            existBook.CostPrice = book.CostPrice;
            existBook.DiscountPercent = book.DiscountPercent;
            existBook.IsNew = book.IsNew;   
            existBook.IsFeatured = book.IsFeatured;
            existBook.StockStatus = book.StockStatus;

            existBook.ModifiedAt = DateTime.UtcNow;

            _context.SaveChanges();


            foreach (var fileName in removedFileNames)
            {
                FileManager.Delete(_env.WebRootPath, "uploads/book", fileName);
            }

            return RedirectToAction("index");
        }

        public IActionResult Delete(int id)
        {
            Book book = _context.Books
                .Include(x => x.BookImages)
                .Include(x => x.BookTags).ThenInclude(bt => bt.Tag)
                .Include(x => x.Author)
                .Include(x => x.Genre)
                .FirstOrDefault(x => x.Id == id && !x.IsDeleted);

            if (book == null) return RedirectToAction("notfound", "error");

            return View(book);
        }

        [HttpPost]
        public IActionResult Delete(Book book)
        {
            Book existBook = _context.Books.FirstOrDefault(x => x.Id == book.Id && !x.IsDeleted);

            if (existBook == null) return RedirectToAction("notfound", "error");

            existBook.IsDeleted = true;
            existBook.ModifiedAt = DateTime.UtcNow;
            _context.SaveChanges();

            return RedirectToAction("index");
        }


    }
}
