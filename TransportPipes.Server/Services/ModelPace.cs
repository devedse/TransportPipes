namespace TransportPipes.Server.Services;

/// <summary>
/// The pace of the design's model player (docs/design-notes/v1.4/sim, the regular player of all five Eras: the mean of
/// its four runs in out/eras), for the admin's Pace next to real companies
/// </summary>
public static class ModelPace
{
    /// <summary>Gold earned in all at the start of each day since the company was founded (day 0 to 124)</summary>
    public static readonly IReadOnlyList<double> Earned =
    [
        0, 1.46e6, 2.69e7, 1.26e8, 3e8, 5.31e8, 7.91e8, 1.11e9, 1.45e9, 1.92e9,
        2.44e9, 3.01e9, 3.8e9, 4.71e9, 5.9e9, 7.67e9, 9.7e9, 1.19e10, 1.42e10, 1.69e10,
        1.96e10, 2.25e10, 2.57e10, 2.91e10, 3.27e10, 3.63e10, 3.98e10, 4.34e10, 4.7e10, 5.08e10,
        5.47e10, 5.86e10, 6.25e10, 6.65e10, 7.19e10, 8.01e10, 8.85e10, 9.72e10, 1.06e11, 1.15e11,
        1.25e11, 1.36e11, 1.47e11, 1.58e11, 1.7e11, 1.84e11, 1.98e11, 2.13e11, 2.27e11, 2.42e11,
        2.57e11, 2.72e11, 2.87e11, 3.02e11, 3.17e11, 3.32e11, 3.48e11, 3.64e11, 3.8e11, 3.97e11,
        4.14e11, 4.31e11, 4.52e11, 4.77e11, 5.06e11, 5.35e11, 5.67e11, 6.01e11, 6.39e11, 6.76e11,
        7.2e11, 7.63e11, 8.13e11, 8.63e11, 9.13e11, 9.65e11, 1.01e12, 1.06e12, 1.11e12, 1.17e12,
        1.22e12, 1.28e12, 1.34e12, 1.4e12, 1.46e12, 1.51e12, 1.57e12, 1.63e12, 1.68e12, 1.74e12,
        1.81e12, 1.9e12, 1.98e12, 2.07e12, 2.16e12, 2.24e12, 2.34e12, 2.44e12, 2.55e12, 2.66e12,
        2.78e12, 2.91e12, 3.05e12, 3.18e12, 3.31e12, 3.45e12, 3.58e12, 3.71e12, 3.85e12, 3.98e12,
        4.12e12, 4.26e12, 4.41e12, 4.55e12, 4.7e12, 4.84e12, 4.99e12, 5.13e12, 5.27e12, 5.42e12,
        5.56e12, 5.7e12, 5.84e12, 5.99e12, 6.14e12
    ];
}
