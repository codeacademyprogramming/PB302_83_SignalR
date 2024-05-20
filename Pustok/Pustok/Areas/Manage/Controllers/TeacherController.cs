using Microsoft.AspNetCore.Mvc;
using Pustok.Areas.Manage.ViewModels;
using Pustok.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Pustok.Areas.Manage.Controllers
{
    [Area("manage")]
    public class TeacherController : Controller
    {
        private readonly PustokDbContext _context;

        public TeacherController(PustokDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var query = _context.Teachers.AsQueryable();

            return View(PaginatedList<Teacher>.Create(query, 1, 10));
        }

        public IActionResult Create()
        {
            ViewBag.Skills = _context.Skills.ToList();
            return View();
        }

        [HttpPost]
        public IActionResult Create(Teacher teacher)
        {
            return Ok(teacher);
        }
    }
}
