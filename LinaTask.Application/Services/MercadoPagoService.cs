using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.DTOs;
using LinaTask.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using MercadoPago.Client.Preference;
using MercadoPago.Resource.Preference;
using System.Collections.Generic;

namespace LinaTask.Application.Services
{
    public class MercadoPagoService : IMercadoPagoService
    {
        private readonly IConfiguration _config;
        private readonly ISystemParameterRepository _params;

        public MercadoPagoService(IConfiguration config, ISystemParameterRepository paramRepo)
        {
            _config = config;
            _params = paramRepo;
        }

        public async Task<CreatePaymentPreferenceResult> CreatePreferenceAsync(CreatePaymentPreferenceDto dto)
        {
            var publicKeyParam = await _params.GetByKeyAsync("mercadopago.public_key");
            var publicKey = publicKeyParam?.ParamValue ?? string.Empty;

            var preferenceRequest = new PreferenceRequest
            {
                Items = new List<PreferenceItemRequest>
                {
                    new PreferenceItemRequest
                    {
                        Title = dto.Description,
                        Quantity = 1,
                        UnitPrice = dto.Amount,
                        CurrencyId = "COP"
                    }
                },

                Payer = new PreferencePayerRequest
                {
                    Email = dto.PayerEmail,
                    Name = dto.PayerName
                },

                ExternalReference = dto.ReferenceId,

                Metadata = new Dictionary<string, object>
                {
                    ["context"] = dto.Context,
                    ["reference_id"] = dto.ReferenceId
                },

                BackUrls = new PreferenceBackUrlsRequest
                {
                    Success = $"{dto.FrontendBaseUrl}/payment/success",
                    Failure = $"{dto.FrontendBaseUrl}/payment/failure",
                    Pending = $"{dto.FrontendBaseUrl}/payment/pending"
                },

                AutoReturn = "approved",

                NotificationUrl = $"{dto.BackendBaseUrl}/api/payments/webhook"
            };

            var client = new PreferenceClient();
            Preference result = await client.CreateAsync(preferenceRequest);

            return new CreatePaymentPreferenceResult(
                result.Id,
                result.InitPoint,
                publicKey
            );
        }

        public async Task<MercadoPagoPaymentInfo> GetPaymentInfoAsync(string externalPaymentId)
        {
            var client = new MercadoPago.Client.Payment.PaymentClient();
            var payment = await client.GetAsync(long.Parse(externalPaymentId));

            return new MercadoPagoPaymentInfo(
                payment.Id.ToString(),
                payment.Status,
                payment.StatusDetail,
                (decimal)(payment.TransactionAmount ?? 0)
            );
        }
    }
}