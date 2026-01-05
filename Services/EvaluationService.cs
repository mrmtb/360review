using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using reviews.Models;

namespace reviews.Services;

public class EvaluationService : IEvaluationService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<EvaluationService> _logger;

    public EvaluationService(ApplicationDbContext db, ILogger<EvaluationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<Evaluation>> GetEvaluationsByClientAsync(Guid clientId)
    {
        return await _db.Evaluations
            .Include(e => e.Client)
            .AsNoTracking()
            .Where(r => r.ClientId == clientId)
            .ToListAsync();
    }

    public async Task<Evaluation?> GetEvaluationDetailsAsync(Guid evaluationId)
    {
        return await _db.Evaluations
            .Include(e => e.Client)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId);
    }

    public async Task<bool> SaveEvaluationAsync(Evaluation model, Guid clientId)
    {
        try
        {
            // Authorization check
            if (model.ClientId != clientId)
            {
                _logger.LogWarning("Unauthorized attempt to modify evaluation for different client");
                return false;
            }

            var evaluation = await _db.Evaluations
                .FirstOrDefaultAsync(e => e.EvaluationId == model.EvaluationId && e.ClientId == clientId);

            if (evaluation == null)
            {
                evaluation = new Evaluation
                {
                    EvaluationId = model.EvaluationId,
                    ClientId = model.ClientId
                };
            }

            // Update properties
            var properties = typeof(Evaluation).GetProperties();
            foreach (var prop in properties)
            {
                if (prop.Name == "EvaluationId" || prop.Name == "ClientId") continue;

                var newValue = prop.GetValue(model);
                var oldValue = prop.GetValue(evaluation);

                if (prop.PropertyType == typeof(DateTime?) && newValue != null)
                {
                    var dateValue = (DateTime?)newValue;
                    if (dateValue.HasValue)
                    {
                        if (!oldValue?.Equals(dateValue.Value) ?? true)
                        {
                            prop.SetValue(evaluation, dateValue.Value);
                        }
                    }
                }
                else if ((oldValue == null && newValue != null) || (oldValue != null && !oldValue.Equals(newValue)))
                {
                    prop.SetValue(evaluation, newValue);
                }
            }

            if (evaluation.EvaluationId == Guid.Empty)
            {
                _db.Evaluations.Add(evaluation);
            }
            else
            {
                _db.Evaluations.Update(evaluation);
            }

            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save evaluation {EvaluationId}", model.EvaluationId);
            return false;
        }
    }

    public async Task<List<vwAdminRatees>> GetRateesByClientAsync(Guid clientId)
    {
        return await _db.ViewAdminRatees
            .AsNoTracking()
            .Where(p => p.ClientId == clientId)
            .ToListAsync();
    }

    public Evaluation CreateNewEvaluation(Guid clientId)
    {
        return new Evaluation
        {
            EvaluationId = Guid.Empty,
            ClientId = clientId
        };
    }
}
