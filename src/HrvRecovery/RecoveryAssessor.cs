namespace HrvRecovery;

/// <summary>État de récupération, déduit du score par rapport à la référence personnelle.</summary>
public enum RecoveryStatus
{
    /// <summary>Score ≥ 110 %.</summary>
    Excellent,
    /// <summary>Score de 100 % à 109 %.</summary>
    Good,
    /// <summary>Score de 90 % à 99 %.</summary>
    Normal,
    /// <summary>Score de 80 % à 89 %.</summary>
    Tired,
    /// <summary>Score sous 80 %.</summary>
    Exhausted
}

/// <summary>Synthèse d'une journée de mesure.</summary>
/// <param name="BaselineMs">Référence personnelle (médiane des matins passés), ou <c>null</c> si absente.</param>
/// <param name="ScorePercent">Mesure du jour en % de la référence (100 = dans la normale).</param>
/// <param name="Status">Statut, ou <c>null</c> si la mesure est jugée aberrante : on ne qualifie alors rien.</param>
/// <param name="UnusualReading">La mesure du jour est doublement extrême : probablement parasitée.</param>
/// <param name="SustainedElevation">VFC haute depuis plusieurs jours : note informative, sans verdict.</param>
public sealed record RecoveryReport(
    double? BaselineMs,
    double ScorePercent,
    RecoveryStatus? Status,
    bool UnusualReading,
    bool SustainedElevation);

/// <summary>
/// Évalue la récupération à partir de la VFC (SDNN, en ms) : référence personnelle, score,
/// statut et garde-fous contre les mesures parasitées.
/// </summary>
/// <remarks>
/// Une VFC n'a de sens que comparée à soi-même. Les seuils en pourcentage sont des repères
/// pratiques, pas des seuils cliniques : l'outil oriente une décision d'entraînement et ne pose
/// aucun diagnostic.
/// </remarks>
public static class RecoveryAssessor
{
    /// <summary>Nombre minimal de jours d'historique avant de juger une mesure.</summary>
    public const int MinimumHistoryForJudgement = 5;

    /// <summary>Score en % de la référence (100 si la référence est nulle ou négative).</summary>
    public static double Score(double latest, double baseline) =>
        baseline > 0 ? latest / baseline * 100.0 : 100.0;

    /// <summary>Statut déduit du score en %.</summary>
    public static RecoveryStatus StatusFor(double scorePercent) => scorePercent switch
    {
        >= 110 => RecoveryStatus.Excellent,
        >= 100 => RecoveryStatus.Good,
        >= 90 => RecoveryStatus.Normal,
        >= 80 => RecoveryStatus.Tired,
        _ => RecoveryStatus.Exhausted
    };

    /// <summary>
    /// La mesure du jour est-elle doublement extrême ? Elle doit à la fois sortir de
    /// <c>[0,55 ; 1,8] × médiane</c> et dépasser <c>5 × MAD</c> de la médiane. Le premier critère
    /// évite de rejeter une vraie belle journée à +30 % ; le second évite de crier au loup sur un
    /// historique naturellement dispersé.
    /// </summary>
    public static bool IsReadingUnusual(double latest, IReadOnlyCollection<double> history)
    {
        if (history.Count < MinimumHistoryForJudgement)
        {
            return false;
        }

        var center = RobustStatistics.Median(history);
        if (center is null or <= 0)
        {
            return false;
        }

        var ratioExtreme = latest > center.Value * 1.8 || latest < center.Value * 0.55;
        if (!ratioExtreme)
        {
            return false;
        }

        var mad = RobustStatistics.MedianAbsoluteDeviation(history);
        if (mad is null or <= 0)
        {
            return true;
        }

        return Math.Abs(latest - center.Value) > 5 * mad.Value;
    }

    /// <summary>
    /// Au moins 4 des 5 derniers matins au-dessus de 115 % de la référence. Souvent une bonne
    /// adaptation, mais une élévation prolongée peut aussi signer une sur-sollicitation.
    /// </summary>
    public static bool IsElevationSustained(IReadOnlyList<double> recentMornings, double baseline)
    {
        if (baseline <= 0 || recentMornings.Count < 5)
        {
            return false;
        }

        var elevated = recentMornings.TakeLast(5).Count(v => v > baseline * 1.15);
        return elevated >= 4;
    }

    /// <summary>
    /// Évaluation complète. <paramref name="history"/> contient les matins passés (la référence
    /// en est issue) ; <paramref name="latest"/> est la mesure du jour.
    /// </summary>
    public static RecoveryReport Assess(double latest, IReadOnlyList<double> history)
    {
        var baseline = RobustStatistics.Median(history);
        var score = baseline is { } b ? Score(latest, b) : 100.0;
        var unusual = IsReadingUnusual(latest, history);

        return new RecoveryReport(
            BaselineMs: baseline,
            ScorePercent: score,
            Status: unusual || baseline is null ? null : StatusFor(score),
            UnusualReading: unusual,
            SustainedElevation: baseline is { } bb && IsElevationSustained(history, bb));
    }
}
