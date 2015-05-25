using Geodesy;
using RoutePredictionAlgorithm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoutePredictionTest
{
    public class CSVWriter
    {
        private SphericalMercatorProjection geoTransform;

        private long GetTripDistance(RoutePredictionValidation validation)
        {
            double startX = geoTransform.LongitudeToX(validation.StartLon);
            double endX = geoTransform.LongitudeToX(validation.ActualEndLon);
            double startY = geoTransform.LatitudeToY(validation.StartLat);
            double endY = geoTransform.LatitudeToY(validation.ActualEndLat);
            return (long)Math.Sqrt(
                Math.Pow(Math.Abs(startX - endX), 2) +
                Math.Pow(Math.Abs(startY - endY), 2));
        }

        private string delim;
        private StreamWriter writer;
        public CSVWriter(string filename, string delim = ";")
        {
            this.delim = delim;
            geoTransform = new SphericalMercatorProjection();
            writer = new StreamWriter(filename);
            StringBuilder sb = new StringBuilder();
            sb.Append("driverId").Append(delim).
                Append("clusterSize").Append(delim).
                Append("trainingSetCount").Append(delim).
                Append("validationSetCount").Append(delim).
                Append("within100m").Append(delim).
                Append("within200m").Append(delim).
                Append("within500m").Append(delim).
                Append("within1000m").Append(delim).
                Append("within2000m").Append(delim).
                Append("meanTripDistance").Append(delim);
            writer.WriteLine(sb.ToString());
        }

        public void WriteRow(string driverId, int clusterSize, int trainingSetCount, int validationSetCount, List<RoutePredictionValidation> validations)
        {
            int within100m = 0;
            int within200m = 0;
            int within500m = 0;
            int within1000m = 0;
            int within2000m = 0;
            long totalTripDistance = 0;

            foreach (var validation in validations)
            {
                totalTripDistance += GetTripDistance(validation);
                if (validation.PredictionAccuracyInMeter < 2000)
                {
                    within2000m++;
                    if (validation.PredictionAccuracyInMeter < 1000)
                    {
                        within1000m++;
                        if (validation.PredictionAccuracyInMeter < 500)
                        {
                            within500m++;
                            if (validation.PredictionAccuracyInMeter < 200)
                            {
                                within200m++;
                                if (validation.PredictionAccuracyInMeter < 100)
                                {
                                    within100m++;
                                }
                            }
                        }
                    }
                }
            }
            StringBuilder sb = new StringBuilder();
            sb.Append(driverId).Append(delim).
                Append(clusterSize).Append(delim).
                Append(trainingSetCount).Append(delim).
                Append(validationSetCount).Append(delim).
                Append(100 * within100m / validations.Count).Append(delim).
                Append(100 * within200m / validations.Count).Append(delim).
                Append(100 * within500m / validations.Count).Append(delim).
                Append(100 * within1000m / validations.Count).Append(delim).
                Append(100 * within2000m / validations.Count).Append(delim).
                Append(totalTripDistance / validations.Count).Append(delim);
            writer.WriteLine(sb.ToString());
        }
    }
}
