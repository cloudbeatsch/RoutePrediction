using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace GeoLocationClustering
{
    public class ClusterLeaf<T> : ClusterItem<T>, IComparable<ClusterLeaf<T>>
    {
        private List<LocationInstance<T>> locations;

        public override List<LocationInstance<T>> GetLocations()
        {
            return locations;
        }

        public long NrOfLocations { get { return GetNrOfLocations(); }  }

        public ClusterLeaf(Item parentCluster, long clusterSize, double x, double y) :
            base(parentCluster, clusterSize, x, y)
        {
            locations = new List<LocationInstance<T>>();
        }
        public override LocationInstance<T> AddLocation(double locationX, double locationY, T locationInfo)
        {
            var location = new LocationInstance<T>(this, locationX, locationY, locationInfo);
            locations.Add(location);
            return location;
        }
        public override long GetNrOfLocations()
        {
            return locations.Count(); ;
        }
        public int CompareTo(ClusterLeaf<T> other)
        {
            return (int)(other.GetNrOfLocations() - this.GetNrOfLocations());
        }
        public override void Visit(IClusterVisitor<T> visitor)
        {
            foreach (var l in locations)
            {
                visitor.LocationVisited(l);
            }
            visitor.LeafVisited(this);
        }
    }
}
