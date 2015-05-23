using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoutePredictionAlgorithm;
using System.IO;
using Geodesy;

namespace RoutePredictionTest
{
    class RoutePredictionTest
    {
        private SphericalMercatorProjection geoTransform; 
        private List<string> drivers;
        List<TripSummary> trips;

        public RoutePredictionTest()
        {
            drivers = new List<string>();
            trips = LoadTrips(drivers);
            geoTransform = new SphericalMercatorProjection();
        }

        private List<TripSummary> LoadTrips(List<String> drivers)
        {
            var trips = new List<TripSummary>();

            using (var rd = new StreamReader(@"..\..\testdata\TripSummaries.csv"))
            {
                // read the header
                rd.ReadLine();
                while (!rd.EndOfStream)
                {
                    var splits = rd.ReadLine().Split(',');
                    if (splits.Length == 11)
                    {
                        var trip = new TripSummary();

                        trip.DriverId = splits[0].Trim();
                        if (drivers.Find(item => item.Equals(trip.DriverId)) == null)
                        {
                            drivers.Add(trip.DriverId);
                        }
                        trip.TripId = splits[1].Trim();
                        // ignore [2] -> Timestamp
                        trip.Distance = double.Parse(splits[3].Trim(), CultureInfo.InvariantCulture);
                        trip.Duration = double.Parse(splits[4].Trim(), CultureInfo.InvariantCulture);
                        trip.EndLatitude = double.Parse(splits[5].Trim(), CultureInfo.InvariantCulture);
                        trip.EndLongitude = double.Parse(splits[6].Trim(), CultureInfo.InvariantCulture);
                        // ignore [7] -> EndTime
                        trip.StartLatitude = double.Parse(splits[8].Trim(), CultureInfo.InvariantCulture);
                        trip.StartLongitude = double.Parse(splits[9].Trim(), CultureInfo.InvariantCulture);
                        trip.StartTime = DateTime.ParseExact(splits[10].Trim(), "d.M.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                        trips.Add(trip);
                    }
                }
            }
            return trips;
        }

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

        public void PredictEndLocation()
        {
            foreach (var driver in drivers)
            {
                RoutePrediction routePrediction = new RoutePrediction(500);
                foreach (TripSummary trip in trips.Where(t => t.DriverId.Equals(driver)))
                {
                    routePrediction.AddTripSummary(trip);
                }

                double lat = 47.6583066666667;
                double lon = -122.14119;
                RoutePredictionResult predictionResult = routePrediction.PredictEndLocation(lat, lon, DateTime.Now);
                Console.WriteLine("driver: " + driver);
                Console.WriteLine("cluster level: " + predictionResult.ClusterLevel);
                
                foreach (var prediction in predictionResult.Predictions)
                {
                    Console.Write("Lat/Lon: " + prediction.Lat + "/" + prediction.Lon);
                    Console.Write("\tProbability: " + prediction.Probability);
                    Console.Write("\tEndpoints: " + prediction.NrOfEndPoints);
                    Console.Write("\tMatches T/D/W: " + prediction.NrOfTimeMatches);
                    Console.Write("/" + prediction.NrOfDayMatches);
                    Console.WriteLine("/" + prediction.NrOfWorkdayMatches);
                }
                Console.WriteLine();
            }
        }

        public void TrainAndValidate()
        {
            var clusterSizes = new int[] { 10, 100, 150, 200, 250, 300, 400, 500, 750, 1000, 2000 };

            foreach (var driver in drivers)
            {
                var currentTrips = trips.Where(t => t.DriverId.Equals(driver));

                List<TripSummary> trainingSet = new List<TripSummary>();
                List<TripSummary> validationSet = new List<TripSummary>();
                Random split = new Random();

                foreach (var trip in currentTrips)
                {
                    if (split.NextDouble() <= 0.6)
                    {
                        trainingSet.Add(trip);
                    }
                    else
                    {
                        validationSet.Add(trip);
                    }
                }
                Console.Write("driver: " + driver);
                Console.Write("\ttraining/validation trips: " + trainingSet.Count());
                Console.WriteLine("/" + validationSet.Count());

                foreach (var clusterSize in clusterSizes)
                {
                    RoutePrediction routePrediction = new RoutePrediction(clusterSize);

                    RoutePredictionValidationResult result = routePrediction.TrainAndValidate(trainingSet, validationSet);

                    int within100m = 0;
                    int within200m = 0;
                    int within500m = 0;
                    int within1000m = 0;
                    int within2000m = 0;
                    long totalTripDistance = 0;

                    foreach (var validation in result.Validations)
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

                    Console.Write("cluster size: " + clusterSize);
                    Console.Write("\tmean trip dist " + totalTripDistance / result.Validations.Count + "m");
                    Console.Write("\t predictions within 100m/200m/500m/1000m/2000m: ");
                    Console.Write((100 * within100m / result.Validations.Count) + "%/");
                    Console.Write((100 * within200m / result.Validations.Count) + "%/");
                    Console.Write((100 * within500m / result.Validations.Count) + "%/");
                    Console.Write((100 * within1000m / result.Validations.Count) + "%/");
                    Console.WriteLine((100 * within2000m / result.Validations.Count) + "%");
                }
                Console.WriteLine();
            }
        }
    }
}
