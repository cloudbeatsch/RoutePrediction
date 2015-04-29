using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoLocationClustering
{
    public class ClusterHierarchy<T> : ClusterItem<T>
    {
        // implementation works only with 4 child clusters
        // as the idea is to split each cluster into 4 child clusters
        private const int NR_OF_CLUSTERS = 4;
        private ClusterItem<T>[] childClusters;
        private long childClusterSize;
        public ClusterHierarchy(Item parentCluster, long clusterSize, double x, double y) :
            base(parentCluster, clusterSize, x, y)
        {
            childClusters = new ClusterItem<T>[NR_OF_CLUSTERS];
            // to fit 4 child clusters into the parent cluster
            childClusterSize = clusterSize / 2;
        }

        // this is for debugging purposes only - it creates the clusters upfront
        public void CreateDebugHierarchy()
        {
            for (long xf = 0; xf < 2; xf++)
            {
                for (long yf = 0; yf < 2; yf++)
                {
                    double newClusterX = X + (childClusterSize * xf);
                    double newClusterY = Y + (childClusterSize * yf);

                    if (childClusterSize > MinZoom)
                    {
                        var h = new ClusterHierarchy<T>(this, childClusterSize, newClusterX, newClusterY);
                        childClusters[xf + (yf * 2)] = h;
                        h.CreateDebugHierarchy();
                    }
                    else
                    {
                        childClusters[xf + (yf * 2)] = new ClusterLeaf<T>(this, childClusterSize, newClusterX, newClusterY);
                    }
                }
            }
        }

        public override LocationInstance<T> AddLocation(double locationX, double locationY, T locationInfo)
        {
            ClusterLeaf<T> leaf = GetLeaf(locationX, locationY);
            return leaf.AddLocation(locationX, locationY, locationInfo);
        }

        public ClusterLeaf<T> GetLeaf(double locationX, double locationY)
        {
            // find the right child cluster
            int index = GetLocationIndex(locationX, locationY);
            if (childClusters[index] == null)
            {
                int xf = ((locationX - X) > (childClusterSize)) ? 1 : 0;
                int yf = ((locationY - Y) > (childClusterSize)) ? 1 : 0;
                double newClusterX = X + ((childClusterSize) * xf);
                double newClusterY = Y + ((childClusterSize) * yf);

                if (childClusterSize > MinZoom)
                {
                    childClusters[index] = new ClusterHierarchy<T>(this, childClusterSize, newClusterX, newClusterY);
                }
                else
                {
                    childClusters[index] = new ClusterLeaf<T>(this, childClusterSize, newClusterX, newClusterY);
                }
            }
            if (childClusters[index] is ClusterHierarchy<T>)
            {
                return ((ClusterHierarchy<T>)childClusters[index]).GetLeaf(locationX, locationY);
            }
            else if (childClusters[index] is ClusterLeaf<T>)
            {
                return ((ClusterLeaf<T>)childClusters[index]);
            }
            else
            {
                // something went wrong
                return null;
            }
        }

        private int GetLocationIndex(double locationX, double locationY)
        {
            int x = ((locationX - X) > childClusterSize) ? 1 : 0;
            int y = ((locationY - Y) > childClusterSize) ? 1 : 0;
            return x + (y * 2);;
        }
        public override long GetNrOfLocations()
        {
            long nrOfLocations = 0;
            for (int i=0; i < NR_OF_CLUSTERS; i++) 
            {
                if (childClusters[i] != null)
                {
                    nrOfLocations += childClusters[i].GetNrOfLocations();
                }
            }
            return nrOfLocations;
        }
        public override List<LocationInstance<T>> GetLocations()
        {
            List<LocationInstance<T>> locations = new List<LocationInstance<T>>();

            // since locations are child elements of leafs, 
            // we simply retrieve all leafs which belong to this cluster
            // and then create the list of all locations
            foreach (var leaf in GetLeafs())
            {
                locations.AddRange(leaf.GetLocations());
            }
            return locations;
        }
        public List<ClusterLeaf<T>> GetLeafs()
        {
            List<ClusterLeaf<T>> leafs = new List<ClusterLeaf<T>>();
            for (int i = 0; i < NR_OF_CLUSTERS; i++)
            {
                if (childClusters[i] != null)
                {
                    if (childClusters[i] is ClusterHierarchy<T>)
                    {
                        ClusterHierarchy<T> h = childClusters[i] as ClusterHierarchy<T>;
                        
                        leafs.AddRange(h.GetLeafs());
                    }
                    else if (childClusters[i] is ClusterLeaf<T>)
                    {
                        leafs.Add((ClusterLeaf<T>)childClusters[i]);
                    }
                }
            }
            return leafs;
        }

        public override void Visit(IClusterVisitor<T> visitor)
        {
            for (int i = 0; i < NR_OF_CLUSTERS; i++)
            {
                if (childClusters[i] != null)
                {
                    childClusters[i].Visit(visitor);
                }
            }
            visitor.HierarchyVisited(this);
        }
    }
}
