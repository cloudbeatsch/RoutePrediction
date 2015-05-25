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
        private List<string> drivers;
        List<TripSummary> trips;

        public RoutePredictionTest()
        {
            drivers = new List<string>();
            trips = LoadTrips(drivers);
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
            CSVWriter csvWriter = new CSVWriter(@"..\..\testdata\validations.csv", ";");

            Console.WriteLine("Writing validation results to validations.csv");

            foreach (var driver in drivers)
            {
                var currentTrips = trips.Where(t => t.DriverId.Equals(driver));

                for (int i = 1; i < currentTrips.Count() - 1; i++)
                {

                    List<TripSummary> trainingSet = new List<TripSummary>();
                    List<TripSummary> validationSet = new List<TripSummary>();

                    int j = 0;
                    foreach (var trip in currentTrips)
                    {
                        if (j++<i)
                        {
                            trainingSet.Add(trip);
                        }
                        else
                        {
                            validationSet.Add(trip);
                        }
                    }

                    foreach (var clusterSize in clusterSizes)
                    {
                        RoutePrediction routePrediction = new RoutePrediction(clusterSize);

                        RoutePredictionValidationResult result = routePrediction.TrainAndValidate(trainingSet, validationSet);
                        csvWriter.WriteRow(driver, clusterSize, trainingSet.Count(), validationSet.Count(), result.Validations);
                    }
                }
            }
            Console.WriteLine("done");
        }
    }
}
