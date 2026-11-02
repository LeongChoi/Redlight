namespace RedLight;

internal static class WarmthCurve
{
    // This approximates a temperature control; actual display white points
    // depend on monitor calibration. 3400 K follows f.lux's published RGB balance.
    public static double Kelvin(double sliderPosition) =>
        6500 - (3100 * Math.Clamp(sliderPosition, 0, 100) / 100);

    public static (float Green, float Blue) Channels(double sliderPosition)
    {
        double kelvin = Kelvin(sliderPosition);
        // Reciprocal temperature gives a gradual visual progression to warm white.
        double progress = ((1 / kelvin) - (1 / 6500.0)) / ((1 / 3400.0) - (1 / 6500.0));
        return ((float)(1 - 0.50 * progress), (float)(1 - 0.75 * progress));
    }
}
