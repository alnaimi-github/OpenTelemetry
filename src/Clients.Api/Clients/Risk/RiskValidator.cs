using Clients.Contracts.Events;
using RiskEvaluator;

namespace Clients.Api.Clients.Risk;

public class RiskValidator : IRiskValidator
{
    private readonly Evaluator.EvaluatorClient _evaluatorClient;

    public RiskValidator(Evaluator.EvaluatorClient evaluatorClient)
    {
        _evaluatorClient = evaluatorClient;
    }
    
    public async Task<bool> HasAcceptableRiskLevelAsync(Client client)
    {
        var risk = await CalculateRisk(client);
        return risk != RiskLevel.High;
    }


    private async Task<RiskLevel> CalculateRisk(Client newClient)
    {
        var response = await _evaluatorClient.EvaluateAsync(new RiskEvaluationRequest()
        {
            Name = newClient.Name,
            Email = newClient.Email,
            Birthdate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(
                newClient.Birthdate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)),
            Membership = (RiskEvaluator.MembershipLevel)newClient.Membership,
            Address =
            {
                newClient.Addresses.Select(a => new RiskEvaluator.Address()
                {
                    Street = a.Street,
                    City = a.City,
                    State = a.State,
                    Country = a.Country,
                    ZipCode = a.ZipCode
                })
            }
        });

        return response.RiskLevel;
    }
}