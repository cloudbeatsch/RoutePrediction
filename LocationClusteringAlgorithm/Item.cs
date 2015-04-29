using Geodesy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace GeoLocationClustering
{
    public abstract class Item
    {
        private double x;
        private double y;

        private static SphericalMercatorProjection geoTransform = new SphericalMercatorProjection();
        protected SphericalMercatorProjection GeoTransform { get { return geoTransform; } }

        internal double X { get { return x; } }
        internal double Y { get { return y; } }
        protected double Lat { get { return GeoTransform.YToLatitude(Y); } }
        protected double Lon { get { return GeoTransform.XToLongitude(X); } }

        private Item parentItem;
        public Item GetParentItem() 
        {
            return parentItem;
        }
        public Item(Item parentItem, double x, double y)
        {
            this.x = x;
            this.y = y;
            this.parentItem = parentItem;
        }
    }
}
