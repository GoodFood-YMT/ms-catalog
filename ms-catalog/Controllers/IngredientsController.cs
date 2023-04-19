using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ms_catalog.Data;
using ms_catalog.Models;

namespace ms_catalog.Controllers
{
    [Route("api/ingredients")]
    [ApiController]
    public class IngredientsController : Controller
    {

        private readonly ApiDBContext _context;
        private readonly ILogger _logger;

        //Task<List<Product>> _products;

        public IngredientsController(ILogger<Ingredient> logger, ApiDBContext context)
        {
            _logger = logger;
            _context = context;
        }

        // GET: IngredientsController
        [HttpGet]
        public ActionResult Index()
        {
            try
            {
                return Ok(_context.Ingredient.ToList());
            }
            catch
            {
                return BadRequest();
            }
           
        }

        // GET: IngredientsController/Details/5
        [HttpGet("{id}")]
        public ActionResult Details(int id)
        {
            try
            {
                return Ok(_context.Ingredient.Where(i => i.Id == id).FirstOrDefault());
            }
            catch
            {
                return BadRequest();
            }       
        }

        // GET: IngredientsController/Create
        [HttpPost]
        public async Task<ActionResult> Create(Ingredient ingredient)
        {
            try
            {
                _context.Ingredient.Add(ingredient);
                await _context.SaveChangesAsync();
                return Ok(ingredient);
            }
            catch
            {
                return BadRequest();
            }
        }

        // POST: IngredientsController/Edit/5
        [HttpPut("{id}")]
        public async Task<ActionResult> Edit(int id, string name)
        {
            try
            {
                Ingredient? currentIgredient = _context.Ingredient.FirstOrDefault(i => i.Id == id);

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

        // GET: IngredientsController/Delete/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                _context.Ingredient.Where(i => i.Id == id).ExecuteDelete();
                return Ok(id);
            }
            catch
            {
                return BadRequest();
            }
        }
    }
}
