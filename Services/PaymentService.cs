using Microsoft.EntityFrameworkCore;
using WebAPI.Data;
using WebAPI.Models;

namespace WebAPI.Services
{
    public class PaymentService : IPaymentService
    {
        private const decimal DefaultHourRate = 750m;
        private const decimal EventFee = 300m;
        private const decimal VatRate = 0.25m;

        public async Task<PaymentRecord> GenerateMonthlyPaymentAsync(
            int contributorId,
            int year,
            int month,
            SchedulerContext db)
        {
            var contributor = await db.Contributors
                .FirstOrDefaultAsync(c => c.Id == contributorId);

            if (contributor == null)
                throw new InvalidOperationException("Contributor not found.");

            var events = await db.Events
                .Where(e => e.HostContributorId == contributorId &&
                            e.StartTime.Year == year &&
                            e.StartTime.Month == month)
                .ToListAsync();

            if (!events.Any())
                throw new InvalidOperationException("No events for this contributor and month.");

            var hourRate = contributor.HourlyRate == 0 ? DefaultHourRate : contributor.HourlyRate;

            var totalHours = events.Sum(e => (e.EndTime - e.StartTime).TotalHours);
            var eventsCount = events.Count;

            var gross = (decimal)totalHours * hourRate + eventsCount * EventFee;
            var vat = gross * VatRate;
            var total = gross + vat;

            var record = new PaymentRecord
            {
                ContributorId = contributorId,
                Amount = total,
                PaymentDate = DateTime.UtcNow,
                Description =
                    $"Month {month}/{year} | Hours {totalHours:F2} | Events {eventsCount} | " +
                    $"Gross {gross:F2} | VAT {vat:F2} | Rate {hourRate:F0}"
            };

            db.Payments.Add(record);
            await db.SaveChangesAsync();

            return record;
        }
    }
}
