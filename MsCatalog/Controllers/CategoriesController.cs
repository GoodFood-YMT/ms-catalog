using Microsoft.AspNetCore.Mvc;
using MsCatalog.Data;
using MsCatalog.Models;

namespace MsCatalog.Controllers
{
    [Route("api/categories")]
    [ApiController]
    public class CategoriesController : Controller
    {

        private readonly ApiDbContext _context;
        private readonly ILogger _logger;

        public CategoriesController(ILogger<Category> logger, ApiDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public ActionResult Index()
        {
            try
            {
                return Ok(_context.Categories.ToList());
            }
            catch
            {
                return BadRequest();
            }       
        }

        [HttpPost()]
        public async Task<ActionResult<Category>> Create([FromBody] CategoryRequestModel body)
        {
            try
            {
                Category newCategory = new Category(body.name);
                _context.Categories.Add(newCategory);
                await _context.SaveChangesAsync();
                return Ok(newCategory);
            }
            catch
            {
                return BadRequest();
            }
        }

        [HttpGet("{id}")]
        public ActionResult Details(int id)
        {
            try
            {
                return Ok(_context.Categories.Where(c => c.Id == id).FirstOrDefault());
            }
            catch
            {
                return BadRequest();
            }         
        }

        [HttpPut("{id}")]
        public ActionResult Edit(int id, [FromBody] CategoryRequestModel body)
        {
            try
            {
                Category? currentCategory = _context.Categories.FirstOrDefault(c => c.Id == id);
                if (currentCategory != null)
                {
                    currentCategory.Name = body.name;
                    _context.Categories.Update(currentCategory);
                    _context.SaveChanges();
                    return Ok(currentCategory);
                }
                else
                {
                    return NotFound();
                }
            }
            catch
            {
                return BadRequest();
            }
        }
    }

    public class CategoryRequestModel
    {
        public string name { get; set; }
    }
}
