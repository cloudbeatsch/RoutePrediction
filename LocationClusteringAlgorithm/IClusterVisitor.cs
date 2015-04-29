using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoLocationClustering
{
    public interface IClusterVisitor<T>
    {
        void RootVisited(ClusterRoot<T> cluster);
        void HierarchyVisited(ClusterHierarchy<T> cluster);
        void LeafVisited(ClusterLeaf<T> cluster);
        void LocationVisited(LocationInstance<T> cluster);
    }
}
