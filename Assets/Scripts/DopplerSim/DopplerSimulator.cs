using Random = System.Random;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics.Interpolation;
using MathNet.Numerics.LinearAlgebra;
using DopplerSim.Tools;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra.Complex32;
using MathNet.Numerics.Statistics;
using MathNet.Numerics.Data.Matlab;

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
        private const int T = 4; // Timeframe of full spectrogram
        private const int WindowSize = 300; // Nw previously
        private const int VelocityResolution = WindowSize; // Nv previously
        private const double TempPrf = 13e3D; // Pulse repetition frequency
        private const double PeakSystolicVelocity = 40e-2; // PSV
        private const double EDV = 15e-2; // TODO what is this?
        private const double DeltaTime = Skip / TempPrf; // dtSpectrum previously
        private readonly int spectrumSize = (int)Math.Round(T / DeltaTime);

        private const int PulseLength = 20; // numHalf previously
        private const float UltrasoundFrequency = 6.933e6F; // Ultrasound frequency
        private const int SpeedOfLight = 1540; // Speed of light for calibration
        public const float NyquistVelocity = SpeedOfLight * (float)(TempPrf) / UltrasoundFrequency / 4;
        private const float SignalToNoiseRatio = 20; // SNR
        private const double Bandwidth = 1D / (PulseLength / 2D); // Bdoppler previously
        private static readonly double[] HammingWindow = Window.Hamming(WindowSize); // This was Kaiser in the Matlab code

        // State
        private Vector<Complex32> previousIQ = Vector<Complex32>.Build.Dense(WindowSize);
        private int currentSliceStart;
        public float linePosition => ((float)currentSliceStart / spectrumSize) * 200;
        private int currentSliceIndex = 0; // TODO this should not be necessary

        // TODO this data should not be needed when we have a function for all this stuff
        private readonly List<Vector<double>> timeSlices = new List<Vector<double>>();
        private readonly List<Vector<double>> velocitySlices = new List<Vector<double>>();

        private readonly double[] timeData =
        {
            0, 0.00245759975423998, 0.00491519950848018, 0.00737279926272016, 0.00983039901696015, 0.0122879987712001,
            0.0147455985254403, 0.0172031982796803, 0.0196607980339203, 0.0221183977881603, 0.0245759975424004,
            0.0270335972966403, 0.0294911970508803, 0.0319487968051204, 0.0344063965593604, 0.0368639963136004,
            0.0393215960678404, 0.0417791958220806, 0.0442367955763205, 0.0466943953305605, 0.0491519950848005,
            0.0516095948390407, 0.0540671945932807, 0.0565247943475207, 0.0589823941017607, 0.0614399938560009,
            0.0638975936102406, 0.0663551933644808, 0.0688127931187206, 0.0712703928729608, 0.0737279926272008,
            0.0761855923814407, 0.0786431921356807, 0.0811007918899209, 0.0835583916441609, 0.0860159913984009,
            0.0884735911526411, 0.0909311909068811, 0.0933887906611211, 0.0958463904153610, 0.0983039901696012,
            0.100761589923841, 0.103219189678081, 0.105676789432321, 0.108134389186561, 0.110591988940801,
            0.113049588695041, 0.115507188449281, 0.117964788203521, 0.120422387957761, 0.122879987712001,
            0.125337587466241, 0.127795187220481, 0.130252786974721, 0.132710386728961, 0.135167986483202,
            0.137625586237441, 0.140083185991682, 0.142540785745921, 0.144998385500162, 0.147455985254402,
            0.149913585008642, 0.152371184762881, 0.154828784517122, 0.157286384271362, 0.159743984025602,
            0.162201583779842, 0.164659183534082, 0.167116783288322, 0.169574383042562, 0.172031982796802,
            0.174489582551042, 0.176947182305282, 0.179404782059522, 0.181862381813762, 0.184319981568002,
            0.186777581322242, 0.189235181076482, 0.191692780830722, 0.194150380584962, 0.196607980339202,
            0.199065580093442, 0.201523179847682, 0.203980779601922, 0.206438379356162, 0.208895979110402,
            0.211353578864642, 0.213811178618882, 0.216268778373122, 0.218726378127362, 0.221183977881602,
            0.223641577635842, 0.226099177390082, 0.228556777144322, 0.231014376898562, 0.233471976652802,
            0.235929576407042, 0.238387176161283, 0.240844775915523, 0.243302375669763, 0.245759975424003,
            0.248217575178243, 0.250675174932483, 0.253132774686722, 0.255590374440963, 0.258047974195203,
            0.260505573949443, 0.262963173703683, 0.265420773457923, 0.267878373212163, 0.270335972966403,
            0.272793572720643, 0.275251172474883, 0.277708772229123, 0.280166371983363, 0.282623971737603,
            0.285081571491843, 0.287539171246083, 0.289996771000323, 0.292454370754563, 0.294911970508803,
            0.297369570263043, 0.299827170017283, 0.302284769771523, 0.304742369525763, 0.307199969280003,
            0.309657569034243, 0.312115168788483, 0.314572768542723, 0.317030368296963, 0.319487968051203,
            0.321945567805443, 0.324403167559683, 0.326860767313923, 0.329318367068163, 0.331775966822403,
            0.334233566576643, 0.336691166330883, 0.339148766085123, 0.341606365839364, 0.344063965593604,
            0.346521565347844, 0.348979165102084, 0.351436764856324, 0.353894364610564, 0.356351964364804,
            0.358809564119044, 0.361267163873284, 0.363724763627524, 0.366182363381764, 0.368639963136004,
            0.371097562890244, 0.373555162644484, 0.376012762398724, 0.378470362152964, 0.380927961907204,
            0.383385561661444, 0.385843161415684, 0.388300761169924, 0.390758360924164, 0.393215960678404,
            0.395673560432644, 0.398131160186884, 0.400588759941124, 0.403046359695364, 0.405503959449604,
            0.407961559203844, 0.410419158958084, 0.412876758712324, 0.415334358466564, 0.417791958220804,
            0.420249557975044, 0.422707157729284, 0.425164757483524, 0.427622357237764, 0.430079956992004,
            0.432537556746244, 0.434995156500485, 0.437452756254724, 0.439910356008965, 0.442367955763204,
            0.444825555517445, 0.447283155271685, 0.449740755025925, 0.452198354780164, 0.454655954534405,
            0.457113554288645, 0.459571154042885, 0.462028753797125, 0.464486353551365, 0.466943953305605,
            0.469401553059845, 0.471859152814085, 0.474316752568325, 0.476774352322565, 0.479231952076805,
            0.481689551831045, 0.484147151585285, 0.486604751339525, 0.489062351093765, 0.491519950848005,
            0.493977550602245, 0.496435150356485, 0.498892750110725, 0.501350349864965, 0.503807949619205,
            0.506265549373445, 0.508723149127685, 0.511180748881925, 0.513638348636165, 0.516095948390405,
            0.518553548144645, 0.521011147898885, 0.523468747653125, 0.525926347407365, 0.528383947161605,
            0.530841546915845, 0.533299146670085, 0.535756746424325, 0.538214346178565, 0.540671945932805,
            0.543129545687046, 0.545587145441286, 0.548044745195526, 0.550502344949766, 0.552959944704005
        };

        private readonly double[] velocityData =
        {
            0.0345895708786447, 0.0345401042319540, 0.0345153709086087, 0.0345153709160262, 0.0345318598130913,
            0.0345648375998042, 0.0346307931583948, 0.0347462153710937, 0.0349111042230657, 0.0351419485965411,
            0.0354305040504048, 0.0357602816950091, 0.0361395259863040, 0.0365435036009443, 0.0369557256566996,
            0.0373597032713398, 0.0377389475626348, 0.0380687252072390, 0.0383325473229225, 0.0385139250274548,
            0.0386128583208361, 0.0386458360852965, 0.0386458360852965, 0.0386293472030663, 0.0386128583208361,
            0.0386046138797210, 0.0386128583208361, 0.0386375916441814, 0.0386870582908721, 0.0387612582609080,
            0.0388601915542893, 0.0389756137447357, 0.0391157692881973, 0.0393053915376894, 0.0396021916106874,
            0.0400721250806171, 0.0407893919323493, 0.0418117032833597, 0.0431720368981087, 0.0448703927914313,
            0.0468985264925425, 0.0492399491340469, 0.0518616829069794, 0.0547142611349795, 0.0577399726709016,
            0.0608563730887597, 0.0639562845650479, 0.0669407737767149, 0.0697356407092201, 0.0722831742450878,
            0.0745668854575830, 0.0765950188174907, 0.0783758187659258, 0.0799257742296236, 0.0812778630323842,
            0.0824403296598274, 0.0834296629941835, 0.0842870852855328, 0.0850455743131707, 0.0857133745033773,
            0.0863234636206132, 0.0868923305471085, 0.0874199752531932, 0.0879063977091977, 0.0883598423414019,
            0.0887555758116258, 0.0890853536935891, 0.0893409315610115, 0.0895140649579432, 0.0895965094284340,
            0.0896047538547141, 0.0895387982367837, 0.0893986425746426, 0.0891925313539107, 0.0889369534864883,
            0.0886401534283252, 0.0883268644879319, 0.0880218199738188, 0.0877415087385462, 0.0874941752380641,
            0.0872880639283227, 0.0871149303830416, 0.0869665301759409, 0.0868346188807402, 0.0867027075855395,
            0.0865460629521586, 0.0863564405543172, 0.0861255959657352, 0.0858452847601325, 0.0855155069671789,
            0.0851445070131545, 0.0847322848683895, 0.0842870849591642, 0.0838089072854784, 0.0832977518473322,
            0.0827618631303454, 0.0822094855904681, 0.0816323747717504, 0.0810387750708024, 0.0804286864282845,
            0.0797938643289069, 0.0791260643463892, 0.0784252865104017, 0.0776832864243337, 0.0769083085738053,
            0.0761085974444365, 0.0752841529917222, 0.0744432196271080, 0.0735857973060888, 0.0727036415875496,
            0.0717885080452102, 0.0708486411646906, 0.0698840409459908, 0.0689029518598956, 0.0679053739064051,
            0.0668995515117997, 0.0658772402052940, 0.0648301955606082, 0.0637501731366269, 0.0626454173893003,
            0.0614994394215633, 0.0603204836893659, 0.0591085501927080, 0.0578718833727048, 0.0566187276408015,
            0.0553903052174086, 0.0542031049847563, 0.0530901047221400, 0.0520760377677398, 0.0511773930334560,
            0.0503941705341234, 0.0497428591816422, 0.0492234589760123, 0.0488277254761186, 0.0485391697997309,
            0.0483330586235038, 0.0481681697121921, 0.0480115252865002, 0.0478466364493631, 0.0476652587448308,
            0.0474673921580682, 0.0472612811301906, 0.0470551701023129, 0.0468490590744352, 0.0466429480465575,
            0.0464368370186798, 0.0462307259908022, 0.0460246149629245, 0.0458185039350468, 0.0456123929071691,
            0.0454062818792914, 0.0452001708514138, 0.0449858153824210, 0.0447549710311980, 0.0444993933566297,
            0.0442190823587160, 0.0439140380374571, 0.0436007492750830, 0.0432957049538240, 0.0430153939559104,
            0.0427598162813421, 0.0425289719301191, 0.0423146164611264, 0.0421167498743638, 0.0419353721698314,
            0.0417704833326943, 0.0416220833481176, 0.0414901722012663, 0.0413582609950752, 0.0412098608324793,
            0.0410449717283133, 0.0408635936974124, 0.0406657267546114, 0.0404513709147452, 0.0402287706337640,
            0.0399979259116676, 0.0397670812044061, 0.0395444809679297, 0.0393548585404184, 0.0392064583778225,
            0.0391075249360919, 0.0390498137592765, 0.0390333248473764, 0.0390498137592765, 0.0391075249360919,
            0.0392064583778225, 0.0393548585404184, 0.0395444809679297, 0.0397588367484561, 0.0399649480878674,
            0.0401463260742635, 0.0402782373546293, 0.0403441930318995, 0.0403277042238439, 0.0402205264893476,
            0.0400061709461802, 0.0396846375943416, 0.0392559264338320, 0.0387447708028316, 0.0381841484954707,
            0.0376070373058795, 0.0370381705870734, 0.0365105261331825, 0.0360405928412721, 0.0356366151376222,
            0.0353068374485131, 0.0350595042002250, 0.0348946153779228, 0.0348121709667717, 0.0348039265256566,
            0.0348616376134624, 0.0349688153479588, 0.0351007264058005, 0.0352326374636422, 0.0353480596392537,
            0.0354305040504047, 0.0354799706970954, 0.0355047040204407, 0.0354964595719082, 0.0354552373514977,
            0.0353810373592093, 0.0352656151539280, 0.0351089707356536, 0.0349440818836816, 0.0347874374802422,
            0.0346637708486806
        };


        public float PulseRepetitionFrequency
        {
            get => (float)PRF / 1000;
            set => PRF = value * 1000D;
        }

        public float MaxPRF =>
            (1540.0f / (2.0f * ((float)depth * 7.0f / 100.0f))) /
            1000.0f; // "Max PRF: " + Math.round(PRFmax / 1000.0D) + " kHz

        private MatrixPlot plotTime;

        public DopplerSimulator()
        {
            vArtSD = (0.1D * vArt);
            vVeinSD = (0.3D * vVein);
            depth = (av_depth / 7.0D + 0.0125D + 0.05D);

            CreateTimeSlices();
        }

        private void CreateTimeSlices()
        {
            var (time, velocity) = VelocityTrace();

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
        }

        public Texture2D CreatePlot()
        {
            Debug.Log("Generating spectrogram...");

            var spectrum = CompleteSpectrogram();
            const int min = 10;
            const int max = 50;
            spectrum = spectrum.Map((x) => (x - min) / (max - min));

            plotTime = new MatrixPlot(spectrum.ColumnCount, spectrum.RowCount);
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
        /// Generate slice for next update time
        /// </summary>
        public Matrix<double> GenerateNextSlice()
        {
            Vector<Complex32> iq =
                SimulatePulsatileFlow(timeSlices[currentSliceIndex], velocitySlices[currentSliceIndex]);
            currentSliceIndex = (currentSliceIndex + 1) % timeSlices.Count;

            var spectrumSlice = DopplerSpectrum(iq, HammingWindow);
            const int min = 10;
            const int max = 50;
            spectrumSlice = spectrumSlice.Map((x) => (x - min) / (max - min));

            return spectrumSlice;
        }

        public void AssignSlice(Matrix<double> slice)
        {
            plotTime.SetDataSlice(slice, currentSliceStart);
            currentSliceStart = (currentSliceStart + slice.ColumnCount) % spectrumSize;
            Debug.Log($"{currentSliceStart} of {spectrumSize} -> {linePosition}");
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

        private (Vector<double> time, Vector<double> velocity) VelocityTrace()
        {
            var time = Vector<double>.Build.DenseOfArray(timeData);
            var velocity = Vector<double>.Build.DenseOfArray(velocityData);

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
            Matrix<double> spectrum = Matrix<double>.Build.Dense(VelocityResolution, spectrumSize);

            int spectrumIndex = 0;
            for (int cycle = 0; cycle < 18; cycle++)
            {
                for (int i = 0; i < timeSlices.Count; i++)
                {
                    Vector<Complex32> iq = SimulatePulsatileFlow(timeSlices[i], velocitySlices[i]);

                    var spectrumSlice = DopplerSpectrum(iq, HammingWindow);
                    // Integrate with existing spectrum
                    var columnsToInsert = spectrumSlice.ColumnCount;
                    spectrum.SetSubMatrix(0, spectrumIndex, spectrumSlice); // TODO this should be assignment?
                    spectrumIndex = (spectrumIndex + columnsToInsert) % (spectrumSize - columnsToInsert);
                }
            }

            return spectrum;
        }

        private Vector<double> Interpolate(IEnumerable<double> inputTime, IEnumerable<double> inputValues,
            IEnumerable<double> outputTime)
        {
            var interpolator = LinearSpline.Interpolate(inputTime, inputValues);
            return Vector<double>.Build.DenseOfEnumerable(outputTime.Select((t) => interpolator.Interpolate(t)));
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
            var interpolatedVelocities = Interpolate(linearSampleTime, sampledVelocities, outputTime);

            // Force positive average velocity
            var averageVelocity = interpolatedVelocities.Average();
            var averageVelocitySign = Math.Sign(averageVelocity);
            interpolatedVelocities *= averageVelocitySign;
            averageVelocity *= averageVelocitySign;
            // Modulate time vector based on velocity (TODO cleaner clone)
            var positiveTime = outputTime.Select((t) => t).ToArray();
            for (int i = 1; i < positiveTime.Length; i++)
            {
                positiveTime[i] = positiveTime[i - 1] + interpolatedVelocities[i] / (TempPrf * averageVelocity);
            }

            // Seems like noise amplitude becomes 1 since IMP is not given?
            // Generate base IQ values
            var iq = 1 / Mathf.Sqrt(2) *
                     Vector<Complex32>.Build.Random(outputTime.Length,
                         Normal.WithMeanStdDev(0, 1));


            // Note: No for loop here :)
            var iqd = SimulateRange((float)(averageVelocity / (2 * NyquistVelocity)), outputTime.Length);
            // Interpolate real and imaginary part separately since we don't have complex spline interpolation here
            var real = Interpolate(outputTime, iqd.Enumerate().Select((c) => (double)c.Real), positiveTime);
            var imaginary = Interpolate(outputTime, iqd.Enumerate().Select((c) => (double)c.Imaginary), positiveTime);
            // Merge to complex
            var iq1 =
                Vector<Complex32>.Build.DenseOfEnumerable(real.Select((r, i) =>
                    new Complex32((float)r, (float)imaginary[i])));
            if (averageVelocitySign < 0)
            {
                iq1 = iq1.Conjugate();
            }

            // Add velocity signal
            var signalAmplitude = Mathf.Pow(10, SignalToNoiseRatio / 20);
            iq += signalAmplitude / Mathf.Sqrt(2) * iq1;

            return iq;
        }

        private Vector<Complex32> SimulateRange(float relativeVelocity, int size)
        {
            var tukeyWindowSamples = Mathf.RoundToInt(relativeVelocity * size * (float)(1 + Bandwidth));
            var tukey = Vector<Complex32>.Build.DenseOfEnumerable(Window
                .Tukey(tukeyWindowSamples, 2 * Bandwidth).Concat(Enumerable.Repeat(0D, size - tukeyWindowSamples))
                .Select(r => new Complex32((float)r, 0)));

            // Apply Tukey window to random IQ values
            var iqRandom = Vector<Complex32>.Build.Random(size);
            var iqArray = tukey.PointwiseMultiply(iqRandom).ToArray();

            // Fourier transform does not return result smh
            // Perform Fourier transform with Matlab scaling specifically (otherwise it ain't comparable)
            Fourier.Inverse(iqArray, FourierOptions.Matlab);

            var iq = Vector.Build.DenseOfEnumerable(iqArray);
            return Mathf.Sqrt(size) * iq;
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

        private Vector<double> SwapHalves(Vector<double> vector)
        {
            var n = vector.Count;
            var left = vector.SubVector(0, n / 2);
            var right = vector.SubVector(n / 2, n - n / 2);
            vector.SetSubVector(0, n / 2, right);
            vector.SetSubVector(n / 2, n - n / 2, left);
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
            var extendedW = Vector<Complex32>.Build.DenseOfEnumerable(w.Select((r) => new Complex32((float)r, 0)));
            var matrix = Matrix<Complex32>.Build.Dense(rows, WindowSize);
            for (var i = 0; i < rows; i++)
            {
                // Apply sliding window (emphasizes part of this current slice)
                var row = iq.SubVector(indices[i], WindowSize).PointwiseMultiply(extendedW).ToArray();
                // Perform 2D Fourier through 1D Fourier-ing all rows
                Fourier.Forward(row, FourierOptions.Matlab);
                // Assign row
                matrix.SetRow(i, row);
            }

            var result = matrix.Map(Complex32.Abs).PointwisePower(2);

            // Blank out certain frequency components
            // TODO why?
            var blank = Enumerable.Repeat(1e-3, rows).ToArray();
            result.SetColumn(0, blank);
            result.SetColumn(1, blank);
            result.SetColumn(WindowSize - 1, blank);

            // Perform fftshift on all rows
            for (int i = 0; i < rows; i++)
            {
                result.SetRow(i, SwapHalves(result.Row(i)));
            }

            // TODO why do we do this?
            const double p = 2;
            result = result.PointwiseAbs().PointwisePower(p).Divide(p);
            result = result.PointwisePower(1 / p);

            // TODO since we have the orientation we have now. make this not needed plz.
            result = result.Transpose();

            // Put into integer range (though we're not using integer spectrogram, so this should be rewritten)
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