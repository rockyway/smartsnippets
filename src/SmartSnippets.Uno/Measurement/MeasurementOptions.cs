namespace SmartSnippets.Uno.Measurement;

internal static class MeasurementOptions
{
#if MEASURE_TRIM
    internal static bool Trim => true;
#else
    internal static bool Trim => false;
#endif
#if MEASURE_RELEASE
    internal static bool Release => true;
#else
    internal static bool Release => false;
#endif
#if MEASURE_BACKDROP_OFF
    internal static bool BackdropOff => true;
#else
    internal static bool BackdropOff => false;
#endif
}
