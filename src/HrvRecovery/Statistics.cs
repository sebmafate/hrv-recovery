namespace HrvRecovery;

/// <summary>Statistiques robustes aux valeurs aberrantes.</summary>
public static class RobustStatistics
{
    /// <summary>
    /// Médiane d'un échantillon non ordonné, ou <c>null</c> s'il est vide.
    /// Une seule mesure parasitée ne la déplace pas, contrairement à la moyenne.
    /// </summary>
    public static double? Median(IEnumerable<double> values)
    {
        var sorted = values.OrderBy(v => v).ToArray();
        if (sorted.Length == 0)
        {
            return null;
        }

        var mid = sorted.Length / 2;
        return sorted.Length % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2.0
            : sorted[mid];
    }

    /// <summary>
    /// Écart absolu médian (MAD) : médiane de |xᵢ − médiane|. C'est l'équivalent robuste de
    /// l'écart-type. Il n'est pas tiré par un point aberrant.
    /// </summary>
    public static double? MedianAbsoluteDeviation(IEnumerable<double> values)
    {
        var list = values.ToArray();
        var center = Median(list);
        if (center is null)
        {
            return null;
        }

        return Median(list.Select(v => Math.Abs(v - center.Value)));
    }
}
