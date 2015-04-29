using System;

namespace RoutePredictionAlgorithm
{
    public class TripSummary
    {

        public string TripId { get; set; }
        public string DriverId { get; set; }
        public double StartLatitude { get; set; }
        public double StartLongitude { get; set; }
        public double EndLatitude { get; set; }
        public double EndLongitude { get; set; }
        public double Duration { get; set; }
        public double Distance { get; set; }
        public DateTime StartTime { get; set; }
    }
}
