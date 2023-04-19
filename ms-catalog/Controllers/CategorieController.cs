using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ms_catalog.Data;
using ms_catalog.Models;
using System.Numerics;

namespace ms_catalog.Controllers
{
    [Route("api/categories")]
    [ApiController]
    public class CategorieController : Controller
    {

        private readonly ApiDBContext _context;
        private readonly ILogger _logger;

        //Task<List<Product>> _products;

        public CategorieController(ILogger<Categorie> logger, ApiDBContext context)
        {
            _logger = logger;
            _context = context;
        }
        // GET: CategorieController
        [HttpGet]
        public ActionResult Index()
        {
            try
            {
                return Ok(_context.Categorie.ToList());
            }
            catch
            {
                return BadRequest();
            }       
        }


        // GET: CategorieController/Details/5
        [HttpGet("{id}")]
        public ActionResult Details(int id)
        {
            try
            {
                return Ok(_context.Categorie.Where(c => c.Id == id).FirstOrDefault());
            }
            catch
            {
                return BadRequest();
            }         
        }

        [HttpPost()]
        public async Task<ActionResult<Categorie>> Create(Categorie categorie)
        {
            try
            {
                _context.Categorie.Add(categorie);
                await _context.SaveChangesAsync();
                return Ok(categorie);
            }
            catch
            {
                return BadRequest();
            }
        }

        // POST: CategorieController/Edit/5
        [HttpPut("{id}")]
        public ActionResult Edit(int id, string name)
        {
            try
            {
                Categorie? currentCategorie = _context.Categorie.FirstOrDefault(c => c.Id == id);
                if (currentCategorie != null)
                {
                    currentCategorie.Name = name;
                    _context.Categorie.Update(currentCategorie);
                    return Ok(currentCategorie);
                }
                else
                {
                    return NotFound();
                }
            }
            catch
            {
                return View();
            }
        }

        // GET: CategorieController/Delete/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Categorie>> Delete(int id)
        {
            try
            {
                Categorie? currentCategorie = _context.Categorie.Where(c => c.Id == id).FirstOrDefault();
                if(currentCategorie != null)
                {
                    _context.Categorie.Remove(currentCategorie);
                    await _context.SaveChangesAsync();
                    return Ok();
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
}
