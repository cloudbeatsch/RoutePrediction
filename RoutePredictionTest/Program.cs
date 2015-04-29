using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoutePredictionAlgorithm;

namespace RoutePredictionTest
{
    class Program
    {
        private static IEnumerable<TripSummary> GetTrips()
        {
            // TODO: Load trips from storage
            return null;
        }

        static void Main(string[] args)
        {
            RoutePrediction routePrediction = new RoutePrediction(500);

            IEnumerable<TripSummary> trips = GetTrips();

            foreach (TripSummary trip in trips)
            {
                routePrediction.AddTripSummary(trip);
            }
            double lat = 0.0;
            double lon = 0.0;
            RoutePredictionResult prediction = routePrediction.PredictEndLocation(lat, lon, DateTime.Now);

            double trainingSet = 0.6;
            RoutePredictionValidationResult validation = routePrediction.TrainAndValidate(trips, trainingSet);
        }
    }
}
