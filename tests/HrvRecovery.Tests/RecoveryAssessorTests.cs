namespace HrvRecovery.Tests;

public class RobustStatisticsTests
{
    [Fact]
    public void Median_OddCount_ReturnsMiddle()
    {
        Assert.Equal(60, RobustStatistics.Median([58, 60, 62]));
    }

    [Fact]
    public void Median_EvenCount_AveragesMiddleTwo()
    {
        Assert.Equal(61, RobustStatistics.Median([58, 60, 62, 64]));
    }

    [Fact]
    public void Median_IgnoresSingleOutlier_UnlikeMean()
    {
        // Une journée parasitée à 200 ms ne doit pas tirer la référence
        double[] values = [59, 60, 61, 62, 200];

        Assert.Equal(61, RobustStatistics.Median(values));
        Assert.True(values.Average() > 80);
    }

    [Fact]
    public void Median_Empty_ReturnsNull()
    {
        Assert.Null(RobustStatistics.Median([]));
    }

    [Fact]
    public void MedianAbsoluteDeviation_OfSymmetricSample()
    {
        // médiane = 60 ; écarts = [2, 1, 0, 1, 2] ; MAD = 1
        Assert.Equal(1, RobustStatistics.MedianAbsoluteDeviation([58, 59, 60, 61, 62]));
    }
}

public class RecoveryAssessorTests
{
    [Fact]
    public void Score_IsLatestOverBaselinePercent()
    {
        // SDNN du jour 48 ms, référence 60 ms : 80 %
        Assert.Equal(80, RecoveryAssessor.Score(48, 60), precision: 9);
    }

    [Fact]
    public void Score_NoBaseline_IsNeutral()
    {
        Assert.Equal(100, RecoveryAssessor.Score(48, 0));
    }

    [Theory]
    [InlineData(115, RecoveryStatus.Excellent)]
    [InlineData(110, RecoveryStatus.Excellent)]
    [InlineData(109.9, RecoveryStatus.Good)]
    [InlineData(100, RecoveryStatus.Good)]
    [InlineData(99.9, RecoveryStatus.Normal)]
    [InlineData(90, RecoveryStatus.Normal)]
    [InlineData(89.9, RecoveryStatus.Tired)]
    [InlineData(80, RecoveryStatus.Tired)]
    [InlineData(79.9, RecoveryStatus.Exhausted)]
    public void StatusFor_UsesDocumentedBoundaries(double score, RecoveryStatus expected)
    {
        Assert.Equal(expected, RecoveryAssessor.StatusFor(score));
    }

    [Fact]
    public void UnusualReading_Requires_FiveDaysOfHistory()
    {
        Assert.False(RecoveryAssessor.IsReadingUnusual(300, [60, 61, 59, 60]));
    }

    [Fact]
    public void UnusualReading_DoubleCriterion_FlagsParasiticSpike()
    {
        // Médiane 60, MAD 2 : 150 ms dépasse 1,8 × médiane et 5 × MAD
        double[] history = [58, 60, 62, 59, 61, 60];

        Assert.True(RecoveryAssessor.IsReadingUnusual(150, history));
    }

    [Fact]
    public void UnusualReading_GoodDayPlusThirtyPercent_IsNotFlagged()
    {
        // +30 % : physiologiquement normal, pas aberrant
        double[] history = [58, 60, 62, 59, 61, 60];

        Assert.False(RecoveryAssessor.IsReadingUnusual(78, history));
    }

    [Fact]
    public void UnusualReading_WideHistory_NotFlaggedByRatioAlone()
    {
        // Historique très dispersé : MAD = 20, donc 5 × MAD = 100. 115 dépasse 1,8 × médiane
        // (ratio 1,92) mais reste à 55 de la médiane : le second critère l'épargne.
        double[] history = [20, 40, 60, 80, 100];

        Assert.False(RecoveryAssessor.IsReadingUnusual(115, history));
    }

    [Fact]
    public void SustainedElevation_FourOfFiveAbove115Percent()
    {
        double[] mornings = [70, 70, 70, 70, 50];

        Assert.True(RecoveryAssessor.IsElevationSustained(mornings, 60));
    }

    [Fact]
    public void SustainedElevation_ThreeOfFive_IsNotSustained()
    {
        double[] mornings = [70, 70, 70, 50, 50];

        Assert.False(RecoveryAssessor.IsElevationSustained(mornings, 60));
    }

    [Fact]
    public void Assess_NormalDay_ReturnsStatusAndBaseline()
    {
        double[] history = [58, 60, 62, 59, 61, 60];

        var report = RecoveryAssessor.Assess(48, history);

        Assert.Equal(60, report.BaselineMs);
        Assert.Equal(80, report.ScorePercent, precision: 9);
        Assert.Equal(RecoveryStatus.Tired, report.Status);
        Assert.False(report.UnusualReading);
    }

    [Fact]
    public void Assess_UnusualReading_HasNoStatus()
    {
        double[] history = [58, 60, 62, 59, 61, 60];

        var report = RecoveryAssessor.Assess(150, history);

        Assert.True(report.UnusualReading);
        Assert.Null(report.Status);
    }

    [Fact]
    public void Assess_NoHistory_HasNoBaselineAndNoStatusNeutralScore()
    {
        var report = RecoveryAssessor.Assess(50, []);

        Assert.Null(report.BaselineMs);
        Assert.Equal(100, report.ScorePercent);
        Assert.Null(report.Status);
    }
}
