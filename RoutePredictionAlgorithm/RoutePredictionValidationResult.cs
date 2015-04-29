using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoutePredictionAlgorithm
{
    public class RoutePredictionValidationResult
    {
        public RoutePredictionValidationResult()
        {
            Validations = new List<RoutePredictionValidation>();
        }
        public long NrOfTrainingSessions { get; set; }
        public List<RoutePredictionValidation> Validations { get; set; }   
    }

    public class RoutePredictionValidation 
    {
        public RoutePredictionValidation()
        {
            ScoredPredictions = new List<ScoredRoutePredictionItem>();
        }
        public double StartLat { get; set; }
        public double StartLon { get; set; }
        public double ActualEndLat { get; set; }
        public double ActualEndLon { get; set; }
        public double PredictedEndLat { get; set; }
        public double PredictedEndLon { get; set; }
        public long PredictionAccuracyInMeter { get; set; }
        public long ClusterLevel { get; set; }

        public List<ScoredRoutePredictionItem> ScoredPredictions { get; set; }
    }
    public class ScoredRoutePredictionItem 
    {
        public long PredictionAccuracyInMeter { get; set; }
        public RoutePredictionItem Prediction { get; set; }
    }
}
