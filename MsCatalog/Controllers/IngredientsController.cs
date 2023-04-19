using Microsoft.AspNetCore.Mvc;
using MsCatalog.Data;
using MsCatalog.Models;

namespace MsCatalog.Controllers
{
    [Route("api/ingredients")]
    [ApiController]
    public class IngredientsController : Controller
    {

        private readonly ApiDbContext _context;
        private readonly ILogger _logger;

        public IngredientsController(ILogger<Ingredient> logger, ApiDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public ActionResult Index()
        {
            try
            {
                return Ok(_context.Ingredients.ToList());
            }
            catch
            {
                return BadRequest();
            }
           
        }

        [HttpPost]
        public async Task<ActionResult> Create(Ingredient ingredient)
        {
            try
            {
                _context.Ingredients.Add(ingredient);
                await _context.SaveChangesAsync();
                return Ok(ingredient);
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
                return Ok(_context.Ingredients.Where(i => i.Id == id).FirstOrDefault());
            }
            catch
            {
                return BadRequest();
            }       
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Edit(int id, string name)
        {
            try
            {
                Ingredient? currentIgredient = _context.Ingredients.FirstOrDefault(i => i.Id == id);

                if(currentIgredient == null)
                {
                    return NotFound();
                }

                currentIgredient.Name = name;
                await _context.SaveChangesAsync();

                return Ok(currentIgredient);
            }
            catch
            {
                return View();
            }
        }
    }
}
