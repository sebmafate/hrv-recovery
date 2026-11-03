# HRV Recovery

[Read in English](./README.en.md)

## À quoi ça sert

Une petite bibliothèque C# (.NET 10) qui transforme une série de mesures de variabilité de la fréquence cardiaque (VFC, SDNN en ms) en un **état de récupération** :

- une **référence personnelle**, qui est la médiane des matins passés ;
- un **score** du jour, en pourcentage de cette référence ;
- un **statut** : excellent, bon, normal, fatigué ou épuisé ;
- deux **garde-fous** : une mesure aberrante n'est pas qualifiée, et une élévation prolongée est signalée.

Elle est tirée du module de récupération de mon application Allure+. Elle n'a aucune dépendance externe.

## Fonctions

| Fonction | Rôle |
|---|---|
| `RecoveryAssessor.Assess` | Évaluation complète d'une journée : référence, score, statut, garde-fous. |
| `RecoveryAssessor.Score` / `StatusFor` | Score en % et statut correspondant. |
| `RecoveryAssessor.IsReadingUnusual` | Mesure doublement extrême, probablement parasitée. |
| `RecoveryAssessor.IsElevationSustained` | 4 matins sur 5 au-dessus de 115 % de la référence. |
| `RobustStatistics.Median` / `MedianAbsoluteDeviation` | Statistiques insensibles aux valeurs aberrantes. |

## Pourquoi la médiane

Le SDNN est très sensible à quelques battements ectopiques. Une seule journée parasitée (nuit courte, alcool, mesure après un effort) tirerait une moyenne, et donc le score de tous les jours suivants. La médiane l'ignore.

## Comment l'utiliser

```csharp
using HrvRecovery;

double[] lastMornings = [58, 60, 62, 59, 61, 60];   // SDNN des matins passés (ms)

var report = RecoveryAssessor.Assess(48, lastMornings);
// report.BaselineMs = 60, report.ScorePercent = 80, report.Status = Tired
```

## Le garde-fou

Une mesure est écartée seulement si elle est **doublement** extrême : hors de `[0,55 ; 1,8] × médiane` **et** à plus de 5 × MAD de la médiane. Le premier critère évite de rejeter une vraie belle journée à +30 %. Le second évite de crier au loup sur un historique naturellement dispersé. En dessous de 5 jours d'historique, aucun jugement n'est porté.

## Limites

- Les seuils en pourcentage sont des **repères pratiques**, pas des seuils cliniques.
- Seul le SDNN est utilisé. Le RMSSD, plus sensible au système parasympathique, serait complémentaire.
- Sans une à deux semaines de mesures régulières, la référence est instable.
- L'alcool, la maladie, le stress et le décalage horaire font varier la VFC sans rapport avec l'entraînement.
- Ce n'est pas un diagnostic. L'outil oriente une décision d'entraînement.

## Tests

```bash
dotnet test
```
