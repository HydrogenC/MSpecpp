using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using MathNet.Numerics;
using MathNet.Numerics.Statistics;
using MSpecpp.ViewModels;

namespace MSpecpp;

public class Spectrum
{
    public float[] Masses { get; set; }

    public float[] Intensities { get; set; }

    public int[]? Peaks { get; set; }

    public string FilePath { get; set; }

    public int Length => Masses.Length;

    public static Spectrum ReadFromTextFormat(string path)
    {
        var lines = File.ReadAllLines(path);
        float[] massValues = new float[lines.Length];
        float[] intensitiesValues = new float[lines.Length];

        var index = 0;
        foreach (var line in lines)
        {
            int posSpace = line.IndexOf(' ');
            massValues[index] = float.Parse(line.Substring(0, posSpace));
            intensitiesValues[index] = float.Parse(line.Substring(posSpace + 1));
            index++;
        }

        return new Spectrum
        {
            Masses = massValues,
            Intensities = intensitiesValues,
            FilePath = path
        };
    }

    public float CalcMean()
    {
        float mean = 0;
        foreach (var item in Intensities)
        {
            mean += item / Intensities.Length;
        }

        return mean;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="halfWindowSize">Half window size, measured in array index</param>
    /// <param name="snr">Signal-to-noise ratio</param>
    /// <param name="mean">The mean value, calculated if not provided</param>
    public void FindPeaks(int halfWindowSize, float snr = 2, float? mean = null)
    {
        mean ??= CalcMean();

        // Assume that the span is equal
        var span = Masses[1] - Masses[0];

        // Calculate noise via MAD
        const float constant = 1.4826f;
        float noise = constant * Intensities.Select((x) => MathF.Abs(x - mean.Value)).Median();

        // Use monotone queue to speed up
        LinkedList<int> monoQueue = new();

        for (int i = 0; i < halfWindowSize; i++)
        {
            Enqueue(i);
        }

        List<int> peaks = [];
        for (int i = 0; i < Length; i++)
        {
            if (i == monoQueue.First.Value && Intensities[monoQueue.First.Value] > snr * noise)
            {
                peaks.Add(i);
            }

            if (i + halfWindowSize < Length)
            {
                Enqueue(i + halfWindowSize);
            }

            // Kick values that are out of window
            while (monoQueue.Count > 0 && monoQueue.First.Value <= i - halfWindowSize)
            {
                monoQueue.RemoveFirst();
            }
        }

        Peaks = peaks.ToArray();
        return;

        void Enqueue(int index)
        {
            while (monoQueue.Count > 0 && Intensities[monoQueue.Last.Value] < Intensities[index])
            {
                monoQueue.RemoveLast();
            }

            monoQueue.AddLast(index);
        }
    }

    public void ExportToTextFormat(string path)
    {
        File.WriteAllLines(path, Enumerable.Zip(Masses, Intensities).Select(
            (x) => $"{x.First:0.0000} {x.Second:0}"));
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

    public static bool ContainsBrukerFlex(string path)
    {
        string fidPath = Path.Combine(path, "1Ref", "fid");
        string acquPath = Path.Combine(path, "1Ref", "acqu");
        return File.Exists(fidPath) && File.Exists(acquPath);
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

        if (!File.Exists(fidPath) || !File.Exists(acquPath))
        {
            throw new FileNotFoundException($"{path} doesn't seem to contain bruker flex files! ");
        }

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
            Debug.Print("Bytes cannot be divided by 4, possible corruption! ");
        }

        int shouldBeLength = int.Parse(acquDict["$TD"]);
        if (shouldBeLength > intensities.Count)
        {
            Debug.Print("The number of tof/mass values reported in the acqu file is greater than in fid! ");
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

        // It's guaranteed that the length of the two arrays are equal
        return new Spectrum
        {
            Intensities = intensities.Select((x) => (float)x).ToArray(),
            Masses = tofs.Select((x) => (float)x).ToArray(),
            FilePath = path
        };
    }
}