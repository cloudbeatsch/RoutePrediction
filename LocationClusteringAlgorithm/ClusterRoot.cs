using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoLocationClustering
{
    public class ClusterRoot<T> : ClusterHierarchy<T>
    {
        public ClusterRoot(long minZoom, long clusterSize, double left, double bottom) :
            base(null, clusterSize, left, bottom)
        {
            MinZoom = minZoom;
            ClusterSize = clusterSize;
            // call CreateDebugHierarchy to create the cluster grids upfront
            // CreateDebugHierarchy();
        }

        public override void Visit(IClusterVisitor<T> visitor)
        {
            visitor.RootVisited(this);
            base.Visit(visitor);
        }
    }
}
