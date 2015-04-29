using Geodesy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace GeoLocationClustering
{
    public abstract class ClusterItem<T> : Item
    {
        public double Bottom { get { return GeoTransform.YToLatitude(Y); } }
        public double Left { get { return GeoTransform.XToLongitude(X); } }
        public double Top { get { return GeoTransform.YToLatitude(Y + ClusterSize); } }
        public double Right { get { return GeoTransform.XToLongitude(X + ClusterSize); } }
        public double CenterLat { get { return GeoTransform.YToLatitude(Y + (ClusterSize / 2)); } }
        public double CenterLon { get { return GeoTransform.XToLongitude(X + (ClusterSize / 2)); } }

        public long ClusterSize { get; set; }
        protected static long MinZoom { get; set; }
        public ClusterItem(Item parentItem, long clusterSize, double x, double y)
            :base(parentItem, x, y)
        {
            this.ClusterSize = clusterSize;
        }
                    
        public bool Intersect (ClusterItem<T> other)
        {
            bool xIntersect = (X > other.X) ? (X < other.X + other.ClusterSize) : (other.X < X + ClusterSize);
            bool yIntersect = (Y > other.Y) ? (Y < other.Y + other.ClusterSize) : (other.Y < Y + ClusterSize);

            return xIntersect && yIntersect;
        }

        public abstract LocationInstance<T> AddLocation(double locationX, double locationY, T locationObject);
        public abstract long GetNrOfLocations();
        public abstract List<LocationInstance<T>> GetLocations();
        public abstract void Visit(IClusterVisitor<T> visitor);
    }
}
