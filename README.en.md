# HRV Recovery

[Lire en français](./README.md)

## What it is for

A small C# (.NET 10) library that turns a series of heart rate variability (HRV, SDNN in ms) readings into a **recovery state**:

- a **personal baseline**, the median of past morning readings;
- a **score** for the day, as a percentage of that baseline;
- a **status**: excellent, good, normal, tired or exhausted;
- two **guard rails**: an unusual reading is not judged, and a sustained elevation is flagged.

It is extracted from the recovery module of my iOS app, Allure+. It has no external dependencies.

## Functions

| Function | Role |
|---|---|
| `RecoveryAssessor.Assess` | Full assessment of a day: baseline, score, status, guard rails. |
| `RecoveryAssessor.Score` / `StatusFor` | Score as a percentage and matching status. |
| `RecoveryAssessor.IsReadingUnusual` | Doubly extreme reading, probably corrupted. |
| `RecoveryAssessor.IsElevationSustained` | 4 of the last 5 mornings above 115 % of baseline. |
| `RobustStatistics.Median` / `MedianAbsoluteDeviation` | Statistics that resist outliers. |

## Why the median

SDNN is very sensitive to a few ectopic beats. A single corrupted day (short night, alcohol, reading after exercise) would pull a mean, and with it the score of every day that follows. The median ignores it.

## Usage

```csharp
using HrvRecovery;

double[] lastMornings = [58, 60, 62, 59, 61, 60];   // SDNN of past mornings (ms)

var report = RecoveryAssessor.Assess(48, lastMornings);
// report.BaselineMs = 60, report.ScorePercent = 80, report.Status = Tired
```

## The guard rail

A reading is set aside only if it is **doubly** extreme: outside `[0.55 ; 1.8] × median` **and** more than 5 × MAD away from the median. The first criterion keeps a genuinely great day at +30 % from being rejected. The second avoids crying wolf on a naturally scattered history. Below 5 days of history, no judgement is made.

## Limits

- The percentage thresholds are **practical guidelines**, not clinical cut-offs.
- Only SDNN is used. RMSSD, more sensitive to the parasympathetic system, would complement it.
- Without one to two weeks of regular readings, the baseline is unstable.
- Alcohol, illness, stress and jet lag change HRV unrelated to training.
- This is not a diagnosis. The tool informs a training decision.

## Tests

```bash
dotnet test
```
