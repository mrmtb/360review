using Microsoft.AspNetCore.Mvc;
using reviews.Models;

public class ClientController : Controller
{
    private readonly ApplicationDBContext _db;

    public ClientController(ApplicationDBContext db)
    {
        _db = db;
    }

    public IActionResult Index()
    {
        var clients = _db.Clients.ToList();
        return View(clients);
    }

    public IActionResult Details(Guid id)
    {
        var client = _db.Clients.FirstOrDefault(c => c.ClientId == id);
        if (client == null) return NotFound();
        return View(client);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Client client)
    {
        if (ModelState.IsValid)
        {
            _db.Clients.Add(client);
            _db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
        return View(client);
    }

    public IActionResult Edit(Guid id)
    {
        var client = _db.Clients.FirstOrDefault(c => c.ClientId == id);
        if (client == null) return NotFound();
        return View(client);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(Client client)
    {
        if (ModelState.IsValid)
        {
            _db.Clients.Update(client);
            _db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
        return View(client);
    }

    public IActionResult Delete(Guid id)
    {
        var client = _db.Clients.FirstOrDefault(c => c.ClientId == id);
        if (client == null) return NotFound();
        return View(client);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(Guid id)
    {
        var client = _db.Clients.FirstOrDefault(c => c.ClientId == id);
        if (client != null)
        {
            _db.Clients.Remove(client);
            _db.SaveChanges();
        }
        return RedirectToAction(nameof(Index));
    }
}