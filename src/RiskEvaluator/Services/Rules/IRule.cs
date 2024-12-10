namespace RiskEvaluator.Services.Rules;

public interface IRule
{
    public int Evaluate(RiskEvaluationRequest request);
}