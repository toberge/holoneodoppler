using Random = System.Random;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics.Interpolation;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Data.Matlab;
using DopplerSim.Tools;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra.Complex32;
using MathNet.Numerics.Statistics;

namespace DopplerSim
{
    public class DopplerSimulator
    {
        private int n_samples = 100;
        public int n_timepoints => 200;
        private double av_depth = 3.0D;

        public float SamplingDepth
        {
            get => (float)depth;
            set => depth = value;
        }

        private double depth;
        private double theta = 0.7853981633974483D;

        public float Angle
        {
            get => (float)(Mathf.Rad2Deg * theta);
            set => theta = value * Mathf.Deg2Rad;
        }

        private double vArt = 1.0D;

        public float ArterialVelocity
        {
            get => (float)vArt;
            set => vArt = value;
        }

        public float MaxVelocity // "Max Velocity: " + Math.round(max_vel * 10.0D) / 10.0D + " m/s"
        {
            get
            {
                if (theta < Math.PI / 2.0D) // 1.5707963267948966D
                {
                    return (float)(PRF * 1540.0D / (4.0D * f0 * Math.Cos(theta)));
                }

                return Mathf.Infinity;
            }
        }

        public float Overlap
        {
            get => overlap;
            set => overlap = value;
        }

        private float overlap = 0f;

        public bool IsVelocityOverMax => vArt > MaxVelocity;

        private double vArtSD;
        private double vVein = 0.4D;
        private double vVeinSD;
        private double f0 = 4000000.0D;
        private double PRF = 20000.0D;

        // New parameters!
        private const int Skip = 10;
        private const int T = 4;
        private const int WindowSize = 300;
        private const int VelocityResolution = 300;
        private const double TempPrf = 13e3D;
        private const double PeakSystolicVelocity = 40e-2;
        private const double EDV = 15e-2;

        private const int PulseLength = 20; // numHalf previously
        private const float UltrasoundFrequency = 6.933e6F; // Ultrasound frequency
        private const int SpeedOfLight = 1540; // Speed of light for calibration
        private const float NyquistVelocity = SpeedOfLight * (float)(TempPrf) / UltrasoundFrequency / 4;
        private const float SignalToNoiseRatio = 20;
        private const double Bdoppler = 1D / (PulseLength / 2D);

        private Vector<Complex32> previousIQ = Vector<Complex32>.Build.Dense(WindowSize);


        public float PulseRepetitionFrequency
        {
            get => (float)PRF / 1000;
            set => PRF = value * 1000D;
        }

        public float MaxPRF =>
            (1540.0f / (2.0f * ((float)depth * 7.0f / 100.0f))) /
            1000.0f; // "Max PRF: " + Math.round(PRFmax / 1000.0D) + " kHz

        // Plot1D pFreqF;
        // Plot1D pFreqR;
        // Plot1D pSampled;

        private double[][] timepoints;
        private MatrixPlot plotTime;

        public DopplerSimulator()
        {
            vArtSD = (0.1D * vArt);
            vVeinSD = (0.3D * vVein);
            depth = (av_depth / 7.0D + 0.0125D + 0.05D);

            //pSampled = new Plot1D(300, 100);
            //pFreqF = new Plot1D(300, 100);
            //pFreqR = new Plot1D(300, 100);
        }

        public Texture2D CreatePlot()
        {
            plotTime = new MatrixPlot(n_timepoints, n_samples);
            timepoints = MultiArray.New<Double>(n_timepoints, n_samples);
            for (int t = 0; t < n_timepoints; t++)
            {
                //Debug.Log(arterialPulse(1.0D, t));

                //timepoints[t] = generateDisplay(getVelocityComponents(depth), arterialPulse(1.0D, t));
                timepoints[t] = generateDisplay(new double[] { overlap, 0D, 1D }, arterialPulse(1.0D, t));
            }

            plotTime.setData(timepoints);

            var spectrum = CompleteSpectrogram();
            var (min, max, mean, std) = AnalyzeMatrix("spectrum", spectrum);
            // Normalize spectrum
            // spectrum = spectrum.Map((x) => (x - min) / (max - min));
            // TODO normalize 10 to 50, that's what caxis does!
            //spectrum = spectrum.Map((x) => (x - std) / (2 * std));
            (min, max, mean, std) = AnalyzeMatrix("spectrum after normalize", spectrum);
            min = 10;
            max = 50;
            spectrum = spectrum.Map((x) => (x - min) / (max - min));
            AnalyzeMatrix("spectrum after normalize 2", spectrum);
            // spectrum = Matrix<double>.Build.Random(100, 100);

            plotTime = new MatrixPlot(spectrum.RowCount, spectrum.ColumnCount);
            plotTime.setData(spectrum.ToRowArrays());
            return plotTime.texture;
        }

        // public void UpdatePlot(int timepoint)
        // {
        //     // do the generate Display on a separate thread
        //     plotTime.data[timepoint] = generateDisplay(getVelocityComponents(depth), arterialPulse(1.0D, timepoint));
        //     plotTime.setOneDataRow(timepoint);
        // }

        /// <summary>
        /// Instead of calculating the overlap based on the depth, get the values
        /// </summary>
        /// <param name="velocityComponents"> expects {art_overlap_total, ven_overlap_total, stationary}</param>
        public void UpdatePlot(int timepoint)
        {
            return;
            // do the generate Display on a separate thread
            plotTime.data[timepoint] =
                generateDisplay(new double[] { overlap, 0D, 1D }, arterialPulse(1.0D, timepoint));
            plotTime.setOneDataRow(timepoint);
        }

        // protected double[] getVelocityComponents(double depth) {
        //     double avpos = av_depth / 7.0D;
        //
        //     if (depth + 0.1D < avpos - 0.0125D - 0.1D)
        //         return new double[] { 0.0D, 0.0D, 1.0D };
        //     if (depth - 0.1D > avpos + 0.0125D + 0.1D) {
        //         return new double[] { 0.0D, 0.0D, 1.0D };
        //     }
        //     double ven_overlap1 = Math.Max(depth - 0.05D, avpos - 0.0125D - 0.1D);
        //     double ven_overlap2 = Math.Min(depth + 0.05D, avpos - 0.0125D);
        //     double ven_overlap_total = (ven_overlap2 - ven_overlap1) / 0.1D;
        //     if (ven_overlap_total < 0.0D) {
        //         ven_overlap_total = 0.0D;
        //     }
        //     double art_overlap1 = Math.Max(depth - 0.05D, avpos + 0.0125D);
        //     double art_overlap2 = Math.Min(depth + 0.05D, avpos + 0.0125D + 0.1D);
        //     double art_overlap_total = (art_overlap2 - art_overlap1) / 0.1D;
        //     if (art_overlap_total < 0.0D) {
        //         art_overlap_total = 0.0D;
        //     }
        //     double stationary = Math.Max(1.0D - ven_overlap_total - art_overlap_total, 0.0D);
        //
        //     return new double[] { art_overlap_total, ven_overlap_total, stationary };
        // }

        public static double arterialPulse(double f, double t)
        {
            double n = 13.0D;
            double phi = 0.3141592653589793D;

            double omega_t = 6.283185307179586D * f * t / 200.0D;
            double Q1 = Math.Pow(Math.Sin(omega_t), n);
            double Q2 = Math.Cos(omega_t - phi);

            return Q1 * Q2 / 0.4D;
        }

        private (Vector<double> time, Vector<double> velocity) VelocityTrace(string filename)
        {
            Dictionary<string, Matrix<double>> trace = MatlabReader.ReadAll<double>(filename);
            if (!trace.TryGetValue("timeAx", out var timeMatrix) ||
                !trace.TryGetValue("velTrace", out var velocityMatrix))
            {
                throw new Exception("Invalid trace file :(");
            }

            Vector<double> time = timeMatrix.Row(0);
            Vector<double> velocity = velocityMatrix.Row(0);

            var dp0 = velocity.Maximum() - velocity.Minimum();
            var dt = (time.Last() - time.First()) / (time.Count - 1);
            var dp = PeakSystolicVelocity - EDV;

            // Edit velocity trace
            velocity = dp / dp0 * velocity;
            velocity = velocity - velocity.Minimum() + EDV;

            return (time, velocity);
        }

        private Matrix<double> CompleteSpectrogram()
        {
            var (time, velocity) = VelocityTrace("trace.mat");
            Debug.Log(time + " and " + velocity);

            int sliceCount = (int)((time.Maximum() - time.Minimum()) * 10 + 0.5D); // manual ceil
            List<Vector<double>> timeSlices = new List<Vector<double>>();
            List<Vector<double>> velocitySlices = new List<Vector<double>>();

            // TODO replace the slicing with actual feeds of subsequent baluba
            int last = 0;
            int millisecond = 0;
            for (int i = 0; i < time.Count; i++)
            {
                if (!(Math.Floor(time[i] * 10) > millisecond)) continue;

                timeSlices.Add(time.SubVector(last, i - last));
                velocitySlices.Add(velocity.SubVector(last, i - last));
                millisecond += 1;
                last = i;
            }

            timeSlices.Add(time.SubVector(last, time.Count - last));
            velocitySlices.Add(velocity.SubVector(last, time.Count - last));

            Debug.Log(timeSlices);

            double deltaTime = Skip / TempPrf;
            int spectrumSize = (int)Math.Round(T / deltaTime);
            double[] w = Window.Hamming(WindowSize); // This was Kaiser in the Matlab code
            Matrix<double> spectrum = Matrix<double>.Build.Dense(VelocityResolution, spectrumSize);

            int spectrumIndex = 0;
            for (int cycle = 0; cycle < 20; cycle++)
            {
                for (int i = 0; i < sliceCount; i++)
                {
                    var iq = SimulatePulsatileFlow(timeSlices[i], velocitySlices[i]);
                    var spectrumSlice = DopplerSpectrum(iq, w);
                    AnalyzeMatrix("spectrum slice", spectrumSlice);
                    // Integrate with existing spectrum
                    var columnsToInsert = spectrumSlice.ColumnCount;
                    spectrum.SetSubMatrix(0, spectrumIndex, spectrumSlice); // TODO this should be assignment?
                    spectrumIndex = (spectrumIndex + columnsToInsert) % (spectrumSize - columnsToInsert);
                }
            }

            return spectrum;
        }

        private Vector<Complex32> SimulatePulsatileFlow(Vector<double> sampleTime, Vector<double> sampledVelocities)
        {
            // Whatever this interpolation is :)
            var delta = sampleTime[1] - sampleTime[0];
            // Here, we don't need to repeat the velocity trace since we only use one cycle!
            // (Lo, how much of a pain that was to figure out)
            var linearSampleTime = sampledVelocities.MapIndexed((i, a) => i * delta, Zeros.Include);
            var outputTime = Generate.LinearRange(linearSampleTime.First(), 1 / TempPrf, linearSampleTime.Last())
                .ToArray();
            // Interpolate over this new time axis
            var interpolator = LinearSpline.Interpolate(linearSampleTime, sampledVelocities);
            var interpolatedVelocities =
                Vector<double>.Build.DenseOfEnumerable(outputTime.Select((t) => interpolator.Interpolate(t)));

            // Force positive average velocity
            double averageVelocity = interpolatedVelocities.Average();
            int averageVelocitySign = Math.Sign(averageVelocity);
            interpolatedVelocities *= averageVelocitySign;
            averageVelocity *= averageVelocitySign;
            // Modulate time vector based on velocity
            var positiveTime = outputTime.Select((t, i) =>
                i == 0 ? t : outputTime[i - 1] + interpolatedVelocities[i] / (TempPrf * averageVelocity));

            // Seems like noise amplitude becomes 1 since IMP is not given?
            // Generate base IQ values
            var iq = new Complex32(1 / Mathf.Sqrt(2), 0) *
                                   Vector<Complex32>.Build.Random(outputTime.Length,
                                       Normal.WithMeanStdDev(0, 1));


            // Note: No for loop here :)
            var iqd = sim1Range((float)(averageVelocity / (2 * NyquistVelocity)), outputTime.Length);
            // Interpolate real and imaginary part separately since we don't have complex spline interpolation here
            interpolator = LinearSpline.Interpolate(outputTime, iqd.Enumerate().Select((c) => (double)c.Real));
            var real =
                Vector<double>.Build.DenseOfEnumerable(positiveTime.Select((t) => interpolator.Interpolate(t)));
            interpolator = LinearSpline.Interpolate(outputTime, iqd.Enumerate().Select((c) => (double)c.Imaginary));
            var imaginary =
                Vector<double>.Build.DenseOfEnumerable(positiveTime.Select((t) => interpolator.Interpolate(t)));
            // Merge to complex
            var iq1 =
                Vector<Complex32>.Build.DenseOfEnumerable(real.Select((r, i) =>
                    new Complex32((float)r, (float)imaginary[i])));

            // Add velocity signal
            var signalAmplitude = Mathf.Pow(10, SignalToNoiseRatio / 20);
            iq += signalAmplitude / Mathf.Sqrt(2) * iq1;

            return iq;
        }

        private Vector<Complex32> sim1Range(float relativeVelocity, int size)
        {
            int tukeyWindowSamples = Mathf.RoundToInt(relativeVelocity * size * (float)(1 + Bdoppler));
            Vector<Complex32> tukey = Vector<Complex32>.Build.DenseOfEnumerable(Window
                .Tukey(tukeyWindowSamples, 2 * Bdoppler).Concat(Enumerable.Repeat(0D, size - tukeyWindowSamples))
                .Select(r => new Complex32((float)r, 0)));

            // Apply Tukey window to random IQ values
            Vector<Complex32> iqRandom = Vector<Complex32>.Build.Random(size);
            Complex32[] iqArray = tukey.PointwiseMultiply(iqRandom).ToArray();

            // Fourier transform does not return result smh
            // Perform Fourier transform with Matlab scaling specifically (otherwise it ain't comparable)
            Fourier.Inverse(iqArray, FourierOptions.Matlab);

            // TODO figure out what you don't need to include of this:
            var iq = Vector.Build.DenseOfEnumerable(iqArray);
            return new Complex32(Mathf.Sqrt(size), 0) * iq;
        }

        private void PrintShape(string name, Matrix<Complex32> matrix)
        {
            Debug.Log(name + " shape: " + matrix.RowCount + "x" + matrix.ColumnCount);
        }

        private void PrintShape(string name, Matrix<double> matrix)
        {
            Debug.Log(name + " shape: " + matrix.RowCount + "x" + matrix.ColumnCount);
        }

        private (double min, double max, double avg, double std) AnalyzeMatrix(string name, Matrix<double> matrix)
        {
            var min = matrix.Enumerate().Minimum();
            var max = matrix.Enumerate().Maximum();
            var avg = matrix.Enumerate().Average();
            var std = matrix.Enumerate().StandardDeviation();
            Debug.Log($"{name}: {min} to {max} with avg {avg} +/- {std}");
            return (min, max, avg, std);
        }

        private (double min, double max, double avg, double std) AnalyzeMatrix(string name,
            IEnumerable<Matrix<double>> matrix)
        {
            var collected = matrix.Select(m => m.Enumerate()).SelectMany(m => m).ToArray();
            var min = collected.Minimum();
            var max = collected.Maximum();
            var avg = collected.Average();
            var std = collected.StandardDeviation();
            Debug.Log($"{name}: {min} to {max} with avg {avg} +/- {std}");
            return (min, max, avg, std);
        }

        private Vector<double> SwapHalves(Vector<double> vector)
        {
            var n = vector.Count;
            var left = vector.SubVector(0, n / 2);
            var right = vector.SubVector(n / 2, n - n / 2);
            vector.SetSubVector(0, n / 2, left);
            vector.SetSubVector(n / 2, n - n / 2, right);
            return vector;
        }

        private Matrix<double> DopplerSpectrum(Vector<Complex32> iq, IEnumerable<double> w)
        {
            var columns = iq.Count;

            // Build IQ matrix from previous values
            iq = Vector<Complex32>.Build.DenseOfEnumerable(previousIQ.Concat(iq));
            // Remember part of this new IQ matrix
            previousIQ = iq.SubVector(columns - WindowSize, WindowSize);

            var indices = Generate.LinearRangeInt32(1, Skip, columns - WindowSize);
            var rows = indices.Length;
            var extendedW = Vector<Complex32>.Build.DenseOfEnumerable(w.Select((r) => new Complex32((float)r, 0))) ;
            var matrix = Matrix<Complex32>.Build.Dense(rows, WindowSize);
            for (var i = 0; i < indices.Length; i++)
            {
                // Apply sliding window
                var row = iq.SubVector(indices[i], WindowSize).PointwiseMultiply(extendedW).ToArray();
                // Perform 2D Fourier through 1D Fourier-ing all rows
                Fourier.Forward(row, FourierOptions.Matlab);
                // Assign row
                matrix.SetRow(i, row);
            }

            var result = matrix.Map(Complex32.Abs).PointwisePower(2);
            
            // Blank out certain frequency components
            var blank = Enumerable.Repeat(1e-3, rows).ToArray();
            result.SetColumn(0, blank);
            result.SetColumn(1, blank);
            result.SetColumn(WindowSize - 1, blank);

            // Perform fftshift on all rows
            for (int i = 0; i < rows; i++)
            {
                result.SetRow(i, SwapHalves(result.Row(i)));
            }

            const double p = 2;
            result = result.PointwiseAbs().PointwisePower(p).Divide(p);
            result = result.PointwisePower(1 / p);
            
            // TODO since we have the orientation we have now. make this not needed plz.
            result = result.Transpose();

            return 10 * (result + 1e-6).PointwiseLog10();
        }

        private double[] generateDisplay(double[] velComponents, double amplitude)
        {
            double[] samplesI = new double[n_samples];
            double[] samplesQ = new double[n_samples];
            double[] sampled_disp = new double[40];

            Random rand = new Random();
            double freq_ymax = 0.0D;

            for (int r = 0; r < 30; r++)
            {
                double vel;
                if (r < (int)Math.Round(velComponents[0] * 30.0D))
                {
                    vel = vArt - Math.Abs(rand.NextGaussian()) * vArtSD;
                    if (vel < 0.0D)
                        vel = 0.0D;
                    vel *= amplitude;
                }
                else if ((r >= (int)Math.Round(velComponents[0] * 30.0D))
                         && (r < (int)Math.Round((velComponents[0] + velComponents[1]) * 30.0D)))
                {
                    vel = -(vVein - Math.Abs(rand.NextGaussian()) * vVeinSD);
                    if (vel > 0.0D)
                    {
                        vel = 0.0D;
                    }
                }
                else
                {
                    vel = rand.NextGaussian() * 0.025D;
                }

                vel *= Math.Cos(theta);
                for (int i = 0; i < n_samples; i++)
                {
                    double[] IQ = sample(vel, i);
                    samplesI[i] += IQ[0];
                    samplesQ[i] += IQ[1];
                    if (i < 40)
                        sampled_disp[i] += samplesI[i];
                }
            }

            //pSampled.addData(sampled_disp);
            double sampl_max = FFTTools.max(sampled_disp);
            //pSampled.setYAxis(-sampl_max, sampl_max);

            double[] freqF = getFrequencies(samplesI, samplesQ, true);
            //pFreqF.addData(freqF);

            double[] freqR = getFrequencies(samplesI, samplesQ, false);
            //pFreqR.addData(freqR);

            freq_ymax = 188.49555921538757D;
            //pFreqF.setYAxis(0.0D, freq_ymax);
            //pFreqR.setYAxis(0.0D, freq_ymax);

            double[] frequencies = new double[n_samples];
            for (int i = 0; i < n_samples / 2; i++)
            {
                frequencies[i] = (freqR[(n_samples / 2 - i - 1)] / freq_ymax);
            }

            for (int i = n_samples / 2; i < n_samples; i++)
            {
                frequencies[i] = (freqF[(i - n_samples / 2)] / freq_ymax);
            }

            return frequencies;
        }

        private double[] sample(double vel, int i)
        {
            double I = 0.5D * Math.Cos(6.283185307179586D * f0 * 2.0D * vel / 1540.0D * i / PRF);
            double Q = 0.5D * Math.Sin(6.283185307179586D * f0 * 2.0D * vel / 1540.0D * i / PRF);

            return new[] { I, Q };
        }

        private double[] getFrequencies(double[] I, double[] Q, bool forward)
        {
            double[] hh = FFTTools.hamming(n_samples);

            double[] Ih = FFTTools.tmult(I, hh);
            double[] Qh = FFTTools.tmult(Q, hh);

            CplxMatrix res = FFTTools.hilbert(Qh);

            double[] hQi = res.im[0];

            double[] demod;

            if (forward)
            {
                demod = FFTTools.minus(Ih, hQi);
            }
            else
            {
                demod = FFTTools.add(Ih, hQi);
            }

            FFT fft = new FFT() { };
            fft.fft(n_samples, demod, FFTTools.zeroes(n_samples));

            double[] abs = FFTTools.abs(fft.yRe, fft.yIm);

            int wall_thresh = 5;
            double wall = 0.8D / wall_thresh;
            double[] outMat = new double[n_samples / 2];
            for (int i = 0; i < n_samples / 2; i++)
            {
                if (i <= wall_thresh)
                {
                    outMat[i] = ((0.2D + wall * i) * abs[i]);
                }
                else
                {
                    outMat[i] = abs[i];
                }
            }

            return outMat;
        }
    }
}