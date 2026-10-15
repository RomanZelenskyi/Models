using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
namespace Mdiapp.Core
{

// Unit2.BitBtn1Click: Delphi arrays are one-based; this API is zero-based.
public enum SeriesMode { Constant, Increment, Power }
public static class ParameterSeries
{
    public static double[] Calculate(double[] times, SeriesMode mode,
        double initial, double increment = 0, double linear = 0, double coefficient = 0, double exponent = 0)
    {
        if (times.Length > 10000) throw new ArgumentOutOfRangeException(nameof(times));
        var result = new double[times.Length];
        for (int i = 0; i < result.Length; i++)
            result[i] = mode switch
            {
                SeriesMode.Constant => initial,
                SeriesMode.Increment => i == 0 ? initial : result[i - 1] + increment,
                SeriesMode.Power => times[i] == 0 ? initial + coefficient
                    : initial + times[i] * linear + coefficient * Math.Exp(exponent * Math.Log(times[i])),
                _ => throw new ArgumentOutOfRangeException(nameof(mode))
            };
        return result;
    }
}

}
