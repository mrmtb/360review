using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using reviews.Models;

public class GenericController : Controller
{
    private readonly ApplicationDBContext _context;

    public GenericController(ApplicationDBContext context)
    {
        _context = context;
    }

    public IActionResult Index(string modelName)
    {
        var type = Type.GetType($"reviews.Models.{modelName}");
        var dbSet = _context.GetType().GetProperty(modelName + "s")?.GetValue(_context);
        var items = dbSet != null ? ((IEnumerable<object>)dbSet).ToList() : new List<object>();

        return View("GenericList", items);
    }

    public IActionResult Create(string modelName)
    {
        var type = Type.GetType($"reviews.Models.{modelName}");
        var model = Activator.CreateInstance(type!);
        return View("GenericForm", model);
    }

    public IActionResult Edit(string modelName, string id)
    {
        var type = Type.GetType($"reviews.Models.{modelName}");
        var dbSet = _context.GetType().GetProperty(modelName + "s")?.GetValue(_context);
        var entityList = ((IEnumerable<object>?)dbSet)?.ToList() ?? new List<object>();

        var idProp = type?.GetProperties().FirstOrDefault(p =>
            p.PropertyType == typeof(Guid));

        object? match = null;
        foreach (var entity in entityList!)
        {
            var key = idProp?.GetValue(entity);
            if (key?.ToString() == id)
            {
                match = entity;
                break;
            }
        }

        return View("GenericForm", match);
    }

    [HttpPost]
    public IActionResult Edit(string modelName, IFormCollection form)
    {
        var type = Type.GetType($"reviews.Models.{modelName}");
        var entity = Activator.CreateInstance(type!);

        foreach (var prop in type!.GetProperties())
        {
            if (form.ContainsKey(prop.Name))
            {
                var raw = form[prop.Name].ToString();

                object? typedValue = null;

                if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?))
                {
                    if (string.IsNullOrEmpty(raw))
                    {
                        typedValue = null;
                    }
                    else if (DateTime.TryParse(raw, out var parsedDate))
                    {
                        // Ensure the DateTime is treated as local and converted to UTC
                        var utcDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Local).ToUniversalTime();
                        typedValue = utcDate;
                    }
                }
                else if (prop.PropertyType == typeof(bool) || prop.PropertyType == typeof(bool?))
                {
                    if (string.IsNullOrEmpty(raw))
                    {
                        typedValue = null;
                    }
                    else if (bool.TryParse(raw, out var parsedBool))
                    {
                        typedValue = parsedBool;
                    }
                }
                else if (prop.PropertyType == typeof(Guid) || prop.PropertyType == typeof(Guid?))
                {
                    if (string.IsNullOrEmpty(raw))
                    {
                        typedValue = null;
                    }
                    else if (Guid.TryParse(raw, out var parsedGuid))
                    {
                        typedValue = parsedGuid;
                    }
                }
                else if (prop.PropertyType.IsEnum)
                {
                    if (Enum.TryParse(prop.PropertyType, raw, out var parsedEnum))
                    {
                        typedValue = parsedEnum;
                    }
                }
                else if (prop.PropertyType == typeof(string))
                {
                    typedValue = string.IsNullOrEmpty(raw) ? null : raw.Trim();
                }
                else if (typeof(IEnumerable<Client>).IsAssignableFrom(prop.PropertyType) && prop.PropertyType.IsGenericType)
                {
                    // Handle collections (e.g., List<T>)
                    // This requires custom logic based on how the data is submitted
                    continue;
                }
                else if (!prop.PropertyType.IsPrimitive && prop.PropertyType != typeof(string))
                {
                    //var nestedEntity = Activator.CreateInstance(prop.PropertyType);
                    // Recursively populate nestedEntity properties
                    //prop.SetValue(entity, nestedEntity);
                    continue;
                }
                else
                {
                    typedValue = Convert.ChangeType(raw, prop.PropertyType);
                }

                prop.SetValue(entity, typedValue);
            }
        }

        var idProp = type.GetProperties().FirstOrDefault(p =>
            p.PropertyType == typeof(Guid) || p.PropertyType == typeof(int));
        var idVal = idProp?.GetValue(entity);

        // If the idProp is a Guid and the value is null or empty, assign a new Guid
        if (idProp != null && idProp.PropertyType == typeof(Guid) && (idVal == null || (Guid)idVal == Guid.Empty))
        {
            idProp.SetValue(entity, Guid.NewGuid());
            idVal = idProp.GetValue(entity); // Update idVal with the new Guid
        }
    // If the idProp is a Guid and the value is null or empty, assign Guid.Empty
        else if (idProp != null && idProp.PropertyType == typeof(int) && (idVal == null || (int)idVal == 0))
        {
        // Search for the highest Guid value in the Id field
            var dbSet2 = _context.GetType().GetProperty(modelName + "s")?.GetValue(_context);
            var list2 = ((IEnumerable<object>?)dbSet2)?.ToList() ?? new List<object>();

            var maxId = list2
                .Select(e => (int?)idProp.GetValue(e))
                .Where(id => id.HasValue)
                .Max() ?? 0;

            // Set the idProp to the highest value + 1
            idProp.SetValue(entity, maxId + 1);
            idVal = idProp.GetValue(entity); // Update idVal with the new int
        }

        var dbSet = _context.GetType().GetProperty(modelName + "s")?.GetValue(_context);
        var list = ((IEnumerable<object>?)dbSet)?.ToList() ?? new List<object>();

        var existing = list?.FirstOrDefault(e => idProp?.GetValue(e)?.ToString() == idVal?.ToString());

        if (existing != null && entity != null)
        {
            _context.Entry(existing).CurrentValues.SetValues(entity);
        }
        else if (entity != null)
        {
            _context.Add(entity);
        }
        else
        {
            return BadRequest("Entity could not be created or updated.");
        }

        _context.SaveChanges();
        return RedirectToAction("Index", new { modelName });
    }

    public IActionResult Delete(string modelName, string id)
    {
        var type = Type.GetType($"reviews.Models.{modelName}");
        var dbSet = _context.GetType().GetProperty(modelName + "s")?.GetValue(_context);
        var list = ((IEnumerable<object>?)dbSet)?.ToList() ?? new List<object>();

        var idProp = type?.GetProperties().FirstOrDefault(p =>
            p.PropertyType == typeof(Guid) || p.PropertyType == typeof(int));

        var entity = list?.FirstOrDefault(e => idProp?.GetValue(e)?.ToString() == id);
        if (entity != null)
        {
            _context.Remove(entity);
            _context.SaveChanges();
        }

        return RedirectToAction("Index", new { modelName });
    }
}
