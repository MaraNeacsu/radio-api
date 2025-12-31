using WebAPI.Data;
using WebAPI.Models;

namespace WebAPI.Services
{
    public interface IPaymentService
    {
        Task<PaymentRecord> GenerateMonthlyPaymentAsync(
            int contributorId,
            int year,
            int month,
            SchedulerContext db);
    }
}
