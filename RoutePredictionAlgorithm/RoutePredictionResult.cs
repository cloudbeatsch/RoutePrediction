using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoutePredictionAlgorithm
{
    public class RoutePredictionResult
    {
        //Leaf being 0
        public long ClusterLevel { get; set; }
        public List<RoutePredictionItem> Predictions { get; set; }
    }
    public class RoutePredictionItem : IComparable<RoutePredictionItem>
    {
        public RoutePredictionItem()
        {
            tripIds = new List<string>();
        }
        public double Probability { get {return probability; } }
        private double probability;
        public void CalculateProbability(long totalNrOfEndPoints)
        {
            probability = (double) NrOfEndPoints / (double) totalNrOfEndPoints;
        }
        public long NrOfTimeMatches { get; set; }
        public long NrOfDayMatches { get; set; }
        public long NrOfWorkdayMatches { get; set; }
        public long NrOfEndPoints { get; set; }
        public double Lat { get; set;}
        public double Lon { get; set; }
        public List<string> TripIds { get { return tripIds; } }
        private List<string> tripIds;
        public int CompareTo(RoutePredictionItem other)
        {
            return (int)((other.Probability - this.Probability) * 10000);
        }
    }
}
