using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoLocationClustering
{
    public class LocationInstance<T> : Item
    {
        private T locationObject;

        public LocationInstance(Item parentCluster, double x, double y, T locationObject)
            : base(parentCluster, x, y)
        {
            this.locationObject = locationObject;
        }
        public T LocationObject { get { return locationObject; } }
    }
}
