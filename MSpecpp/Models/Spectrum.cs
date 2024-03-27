using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using MathNet.Numerics;
using MSpecpp.ViewModels;

namespace MSpecpp;

/// <summary>
/// Represents a pair of a mass and an intensity. 
/// </summary>
public struct SpectrumValue(float m, float i)
{
    public float Mass = m;
    public float Intensity = i;
}

public class Spectrum
{
    public SpectrumValue[] Values { get; set; }

    public string FilePath { get; set; }

    public int Length => Values.Length;

    public float CalcRms()
    {
        float rms = 0;
        foreach (var val in Values)
        {
            rms += val.Intensity * val.Intensity;
        }

        return MathF.Sqrt(rms / Length);
    }

    public static Spectrum ReadFromTextFormat(string path)
    {
        var lines = File.ReadAllLines(path);
        List<SpectrumValue> spectrumValues = new(lines.Length);

        foreach (var line in lines)
        {
            int posSpace = line.IndexOf(' ');
            float mass = float.Parse(line.Substring(0, posSpace));
            float intens = float.Parse(line.Substring(posSpace + 1));
            spectrumValues.Add(new SpectrumValue(mass, intens));
        }

        return new Spectrum
        {
            Values = [.. spectrumValues],
            FilePath = path
        };
    }

    /// <summary>
    /// Reference: https://github.com/sgibb/readBrukerFlexData/blob/master/R/tof2mass-functions.R
    /// </summary>
    /// <param name="tof"></param>
    /// <param name="c1"></param>
    /// <param name="c2"></param>
    /// <param name="c3"></param>
    /// <returns></returns>
    private static double TofToMass(double tof, double c1, double c2, double c3)
    {
        var a = c3;
        var b = Math.Sqrt(1e+12 / c1);
        var c = c2 - tof;

        if (a == 0)
        {
            // linear: 0 = B * sqrt(m/z) + C(times)
            return (c * c) / (b * b);
        }

        // quadratic: 0 = A * (sqrt(m/z))^2 + B * sqrt(m/z) + C(times)
        double sqrtMass = (-b + Math.Sqrt((b * b) - (4 * a * c))) / (2 * a);
        return sqrtMass * sqrtMass;
    }

    /// <summary>
    /// Reference: https://github.com/sgibb/readBrukerFlexData/blob/master/R/readBrukerFlexFile-functions.R
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static Spectrum ReadFromBrukerFlex(string path)
    {
        string fidPath = Path.Combine(path, "1Ref", "fid");
        string acquPath = Path.Combine(path, "1Ref", "acqu");

        var acquLines = File.ReadAllLines(acquPath);
        Dictionary<string, string> acquDict = new();

        string currentKey = "";
        foreach (var line in acquLines)
        {
            if (line.StartsWith("##"))
            {
                // Position of `=`
                int eqPos = line.IndexOf('=');
                // Get name before `=` excluding `##`
                currentKey = line.Substring(2, eqPos - 2);
                acquDict[currentKey] = line.Substring(eqPos + 1).Trim();
            }
            else
            {
                // Append to previous line
                acquDict[currentKey] += ' ' + line.Trim();
            }
        }

        bool isBigEndian = int.Parse(acquDict["$BYTORDA"]) == 1;
        List<int> intensities = [];
        using var fidFileStream = new FileStream(fidPath, FileMode.Open);
        using var binaryReader = new BinaryReader(fidFileStream);
        byte[] buffer = new byte[4];

        int bytesRead = 0;
        while ((bytesRead = fidFileStream.Read(buffer)) == 4)
        {
            if (isBigEndian)
            {
                Array.Reverse(buffer);
            }

            intensities.Add(BitConverter.ToInt32(buffer));
        }

        if (bytesRead != 0)
        {
            MainViewModel.Instance.Information = "Bytes cannot be divided by 4, possible corruption! ";
        }

        int shouldBeLength = int.Parse(acquDict["$TD"]);
        if (shouldBeLength > intensities.Count)
        {
            MainViewModel.Instance.Information =
                "The number of tof/mass values reported in the acqu file is greater than in fid! ";
        }

        // ToF data
        var timeDelay = double.Parse(acquDict["$DELAY"]);
        var timeDelta = double.Parse(acquDict["$DW"]);

        bool usesCubicCalibration = acquDict["$NTBCal"].Contains("V1.0CTOF2CalibrationConstants");
        double[] tofs = Enumerable.Range(0, intensities.Count).Select((i) => timeDelay + i * timeDelta).ToArray();

        // Convert tofs to masses
        if (usesCubicCalibration)
        {
            string regex = "^.*V1.0CTOF2CalibrationConstants ([0-9. -]*) V1.0CTOF2CalibrationConstants.*$";
            var match = Regex.Match(acquDict["$NTBCal"], regex);
            var cali = match.Groups[1].ToString()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(double.Parse).ToArray();

            // Reference: https://github.com/sgibb/readBrukerFlexData/blob/master/R/ntbcal-functions.R
            var a = cali[5];
            var b = cali[4];
            var c = Math.Sqrt(1e12 / cali[3]);
            var d = cali[2];
            var e = cali[6];

            for (int i = 0; i < tofs.Length; i++)
            {
                double m = 0.0;

                var s = Math.Sign(tofs[i] - d);
                if (s >= 0)
                {
                    // Assuming cubic with A*x^3 + B*x^2 + C*x + (D - tof) where x = sqrt(mz)
                    var roots = FindRoots.Cubic(d - tofs[i], c, b, a);
                    m = roots.Item1.Real;
                }
                else
                {
                    // quadratic: 0 = B * (sqrt(m/z))^2 + C * sqrt(m/z) + D(times)
                    // same formula as tof2mass but instead of D(times) = ML3 - tof
                    // it seems to be necessary to use D(times) = tof - ML3
                    m = (-c + Math.Sqrt(Math.Abs(c * c - 4 * b * (tofs[i] - d)))) / (2 * b);
                }

                tofs[i] = m * m * s - e;
            }
        }
        else
        {
            // Calibration constants
            var c1 = double.Parse(acquDict["$ML1"]);
            var c2 = double.Parse(acquDict["$ML2"]);
            var c3 = double.Parse(acquDict["$ML3"]);

            for (int i = 0; i < tofs.Length; i++)
            {
                tofs[i] = TofToMass(tofs[i], c1, c2, c3);
            }
        }

        return new Spectrum
        {
            Values = intensities.Zip(tofs)
                .Select((tuple) => new SpectrumValue((float)tuple.Second, tuple.First)).ToArray(),
            FilePath = path
        };
    }
}