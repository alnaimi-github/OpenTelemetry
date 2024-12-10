namespace RiskEvaluator.Services.Rules;

public class MembershipRule : IRule
{
    private readonly bool _premiumMembershipFailure;

    public MembershipRule(bool premiumMembershipFailure)
    {
        _premiumMembershipFailure = premiumMembershipFailure;
    }

    public int Evaluate(RiskEvaluationRequest request)
    {
        if (!_premiumMembershipFailure) return 0;

        return request.Membership switch
        {
            MembershipLevel.Premium => throw new Exception("Random failure in MembershipRule."),
            _ => 0
        };
    }
}