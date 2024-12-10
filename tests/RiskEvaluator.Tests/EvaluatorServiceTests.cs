using RiskEvaluator.Services;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using FluentAssertions;
using RiskEvaluator.Services.Rules;

namespace RiskEvaluator.Tests
{
    public class EvaluatorServiceTests
    {
        private readonly EvaluatorService _evaluatorService;

        public EvaluatorServiceTests()
        {
            var timeProvider = new FakeTimeProvider(DateTimeOffset.Now);
            _evaluatorService = new EvaluatorService(Substitute.For<ILogger<EvaluatorService>>(), 
                new IRule[]{ new AgeRule(timeProvider), new EmailRule() });
        }

        [Fact]
        public async Task When_score_is_less_than_5_returns_low_risk()
        {
            var request = 
                CreateRequest(yearsToAdd: -30);

            var response = await _evaluatorService.Evaluate(request, null!);

            response.RiskLevel.Should().Be(RiskLevel.Low);
        }


        [Fact]
        public async Task When_score_is_between_5_and_20_returns_medium_risk()
        {
            var request = CreateRequest(yearsToAdd: -30, email: "john@bugmenot.com");

            var response = await _evaluatorService.Evaluate(request, null!);

            response.RiskLevel.Should().Be(RiskLevel.Medium);
        }

        [Fact]
        public async Task When_score_is_above_20_returns_high_risk()
        {
            var request = CreateRequest(yearsToAdd: -10);

            var response = await _evaluatorService.Evaluate(request, null!);

            response.RiskLevel.Should().Be(RiskLevel.High);
        }

        [Theory]
        [InlineData(-10, RiskLevel.High)]
        [InlineData(-20, RiskLevel.Low)]
        [InlineData(-30, RiskLevel.Low)]
        public async Task Given_age_returns_expected_score(int yearsToAdd, RiskLevel expectedLevel)
        {
            var request = CreateRequest(yearsToAdd);

            var response = await _evaluatorService.Evaluate(request, null!);

            response.RiskLevel.Should().Be(expectedLevel);
        }

        [Fact]
        public async Task When_birthdate_did_not_occur_yet_this_year_age_is_one_year_less()
        {
            var request = new RiskEvaluationRequest
            {
                Name = "John Doe",
                Email = "john@john.com",
                Birthdate = Timestamp.FromDateTime(DateTime.UtcNow.AddYears(-18).AddDays(1)),
                Membership = MembershipLevel.Regular
            };

            var response = await _evaluatorService.Evaluate(request, null!);

            response.RiskLevel.Should().Be(RiskLevel.High);
        }

        private static RiskEvaluationRequest CreateRequest(int yearsToAdd = 0, string email = "doe@mail.com")
        {
            return new RiskEvaluationRequest
            {
                Name = "John Doe",
                Email = email,
                Birthdate = Timestamp.FromDateTime(DateTime.UtcNow.AddYears(yearsToAdd)),
                Membership = MembershipLevel.Regular
            };
        }
    }
}