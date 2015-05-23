using GeoLocationClustering;
using Geodesy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoutePredictionAlgorithm
{
    public class RoutePrediction
    {
        private LocationClustering<LocationInstance<TripSummary>> startLocationRoot;
        private LocationClustering<TripSummary> endLocationRoot;

        private static SphericalMercatorProjection geoTransform = new SphericalMercatorProjection();

        private long clusterSize;

        public RoutePrediction(long clusterSize)
        {
            this.clusterSize = clusterSize;
        }

        public void AddTripSummary(TripSummary tripSummary)
        {
            if (startLocationRoot == null)
            {
                startLocationRoot = new LocationClustering<LocationInstance<TripSummary>>(clusterSize);
            }
            if (endLocationRoot == null)
            {
                endLocationRoot = new LocationClustering<TripSummary>(clusterSize);
            }
            var endLocation = endLocationRoot.AddLocation(tripSummary.EndLatitude, tripSummary.EndLongitude, tripSummary);
            startLocationRoot.AddLocation(tripSummary.StartLatitude, tripSummary.StartLongitude, endLocation);
        }

        public RoutePredictionValidationResult TrainAndValidate(IEnumerable<TripSummary> tripSummaries, double trainingSplit)
        {
            List<TripSummary> trainingSet = new List<TripSummary>();
            List<TripSummary> validationSet = new List<TripSummary>();
            Random split = new Random();

            foreach (var trip in tripSummaries)
            {
                if (split.NextDouble() <= trainingSplit)
                {
                    trainingSet.Add(trip);
                }
                else
                {
                    validationSet.Add(trip);
                }
            }
            return TrainAndValidate(trainingSet, validationSet);
        }
        public RoutePredictionValidationResult TrainAndValidate(IEnumerable<TripSummary> trainingTrips, IEnumerable<TripSummary> validationTrips)
        {
            // nulling the existing clusters to load a new table
            endLocationRoot = null;
            startLocationRoot = null;
            RoutePredictionValidationResult result = new RoutePredictionValidationResult();

            try
            {
                foreach (var trip in trainingTrips)
                {
                    AddTripSummary(trip);
                }
                result.NrOfTrainingSessions = trainingTrips.Count();

                foreach (var validationItem in validationTrips)
                {
                    result.Validations.Add(ValidateRoutePrediction(validationItem));
                }
            }
            catch
            {
                endLocationRoot = null;
                startLocationRoot = null;
            }
            return result;
        }
        private RoutePredictionValidation ValidateRoutePrediction(TripSummary trip)
        {
            RoutePredictionValidation validation = new RoutePredictionValidation() { 
                ActualEndLat = trip.EndLatitude,
                ActualEndLon = trip.EndLongitude,
                StartLat = trip.StartLatitude,
                StartLon = trip.StartLongitude,
            };

            var predictionResult = PredictEndLocation(trip.StartLatitude, trip.StartLongitude, trip.StartTime);
            validation.ClusterLevel = predictionResult.ClusterLevel;
            if (predictionResult.Predictions.Count > 0)
            {
                validation.PredictedEndLat = predictionResult.Predictions[0].Lat;
                validation.PredictedEndLon = predictionResult.Predictions[0].Lon;
                validation.PredictionAccuracyInMeter = CalculatePredictionAccuracyInMeter(
                        validation.ActualEndLat, validation.ActualEndLon, 
                        validation.PredictedEndLat, validation.PredictedEndLon);
                foreach (var prediction in predictionResult.Predictions)
                {
                    var scoredItem = new ScoredRoutePredictionItem()
                    {
                        Prediction = prediction,
                        PredictionAccuracyInMeter =
                            CalculatePredictionAccuracyInMeter(validation.ActualEndLat, validation.ActualEndLon, prediction.Lat, prediction.Lon)
                    };
                    validation.ScoredPredictions.Add(scoredItem);
                }
            }

            return validation;
        }

        private long CalculatePredictionAccuracyInMeter(double actualLat, double actualLon, double predictedLat, double predictedLon)
        {
                double actualX = geoTransform.LongitudeToX(actualLon);
                double predictedX = geoTransform.LongitudeToX(predictedLon);
                double actualY = geoTransform.LatitudeToY(actualLat);
                double predictedY = geoTransform.LatitudeToY(predictedLat);
                return (long) Math.Sqrt(
                    Math.Pow(Math.Abs(actualX - predictedX), 2) +
                    Math.Pow(Math.Abs(actualY - predictedY), 2));
        }

        public RoutePredictionResult PredictEndLocation(double lat, double lon, DateTime time)
        {
            var prediction = new RoutePredictionResult();

            // check how many start points we have within a cluster 
            // and iterate through the cluster hierachy to get at least 1 location
            // TODO: filter using time of the day (morning, midday, evening, night), week day, month,...
            ClusterItem<LocationInstance<TripSummary>> cluster = startLocationRoot.GetLeaf(lat, lon);
            prediction.ClusterLevel = 0;
            while (cluster.GetNrOfLocations() == 0)
            {
                var parentItem = cluster.GetParentItem();
                if (parentItem != null)
                {
                    if (parentItem is ClusterItem<LocationInstance<TripSummary>>)
                    {
                        cluster = parentItem as ClusterItem<LocationInstance<TripSummary>>;
                        prediction.ClusterLevel++;
                    }
                    else
                    {
                        // return an empty prediction result
                        return prediction;
                    }
                }
                else
                {
                    // return an empty prediction result
                    return prediction;
                }
            }

            Dictionary<ClusterLeaf<TripSummary>, RoutePredictionItem> leafs = new Dictionary<ClusterLeaf<TripSummary>, RoutePredictionItem>();
            // create a list of endLocation leafs and count how many times an endlocation is part of the leaf
            foreach (var location in cluster.GetLocations())
            {
                ClusterLeaf<TripSummary> leaf = location.LocationObject.GetParentItem() as ClusterLeaf<TripSummary>;
                if (!leafs.ContainsKey(leaf))
                {
                    leafs[leaf] = new RoutePredictionItem() { 
                        Lat = leaf.CenterLat, 
                        Lon = leaf.CenterLon, 
                        NrOfEndPoints = 0,
                        NrOfDayMatches = 0,
                        NrOfTimeMatches = 0,
                        NrOfWorkdayMatches = 0
                    };
                }
                leafs[leaf].NrOfEndPoints++;
                leafs[leaf].TripIds.Add(location.LocationObject.LocationObject.TripId);
                if (location.LocationObject.LocationObject.StartTime.DayOfWeek == time.DayOfWeek)
                {
                    leafs[leaf].NrOfDayMatches++;
                }
                ;
                if (IsWeekend(location.LocationObject.LocationObject.StartTime) == IsWeekend(time))
                {
                    leafs[leaf].NrOfWorkdayMatches++;
                }
                if (GetTimeSlot(location.LocationObject.LocationObject.StartTime) == GetTimeSlot(time))
                {
                    leafs[leaf].NrOfTimeMatches++;
                }
                leafs[leaf].CalculateProbability(cluster.GetNrOfLocations());
            }
            prediction.Predictions = leafs.Values.ToList();
            prediction.Predictions.Sort();
            return prediction;
        }
        private bool IsWeekend(DateTime time)
        {
           // TODO: needs to be localized for countries whith different working days
           return ((time.DayOfWeek == DayOfWeek.Saturday) || (time.DayOfWeek == DayOfWeek.Sunday));
        }
        private int GetTimeSlot(DateTime time)
        {
            if ((time.Hour >= 6) && (time.Hour <= 10))
            {
                // morning
                return 0;
            }
            if ((time.Hour >= 10) && (time.Hour <= 15))
            {
                // mid day
                return 1;
            }
            if ((time.Hour >= 15) && (time.Hour <= 20))
            {
                // evening
                return 2;
            }
            return 3;
        }
    }
}
