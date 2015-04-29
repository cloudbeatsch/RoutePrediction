using Geodesy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace GeoLocationClustering
{
    public class LocationClustering<T>
    {
        private ClusterRoot<T> rootCluster = null;
        private ClusterRoot<T> shiftedRootCluster = null;

        private long minZoom;

        private static SphericalMercatorProjection geoTransform = new SphericalMercatorProjection();
        protected SphericalMercatorProjection GeoTransform { get { return geoTransform; } }

        public LocationClustering(long minZoom)
        {
            this.minZoom = minZoom;

            // create the root cluster - we only go down to -85 because of 
            // problems with the Latutude translation of -90
            double left = GeoTransform.LongitudeToX(-180);
            double bottom = GeoTransform.LatitudeToY(-85);

            long maxZoom = (long)(Math.Abs(GeoTransform.LongitudeToX(180) * 2));

            // re-adjust the panelsize to ensure the requested leave size
            long rootPanelSize = minZoom;
            while ((rootPanelSize*2) < maxZoom)
            {
                rootPanelSize = rootPanelSize * 2;
            }
            // double the panelsize to ensure it covers the complete globe
            // altough it is now bigger than the globe, this will do no harm as we 
            // catch this when mapping the x/y coordinates back to lon/lat
            rootPanelSize = rootPanelSize * 2;

            this.rootCluster = new ClusterRoot<T>(minZoom, rootPanelSize, left, bottom);
            this.shiftedRootCluster = new ClusterRoot<T>(minZoom, rootPanelSize, left + (minZoom / 2), bottom + (minZoom / 2));
        }

        public LocationInstance<T> AddLocation(double lat, double lon, T locationInfo)
        {
            shiftedRootCluster.AddLocation(GeoTransform.LongitudeToX(lon), GeoTransform.LatitudeToY(lat), locationInfo);
            return rootCluster.AddLocation(GeoTransform.LongitudeToX(lon), GeoTransform.LatitudeToY(lat), locationInfo);
        }
        public ClusterLeaf<T> GetLeaf(double lat, double lon)
        {
            return rootCluster.GetLeaf(GeoTransform.LongitudeToX(lon), GeoTransform.LatitudeToY(lat));
        }

        public long GetNrOfLocations()
        {
            return rootCluster.GetNrOfLocations();
        }

        public IEnumerable<ClusterLeaf<T>> GetSortedLeafs(int nrOfItems)
        {
            List<ClusterLeaf<T>> resultLeafs = new List<ClusterLeaf<T>>();
            List<ClusterLeaf<T>> shiftedLeafs = shiftedRootCluster.GetLeafs();
            List<ClusterLeaf<T>> leafs = rootCluster.GetLeafs();
            shiftedLeafs.Sort();
            leafs.Sort();

            resultLeafs.AddRange(leafs.Take(nrOfItems));
            resultLeafs.AddRange(shiftedLeafs.Take(nrOfItems));
            resultLeafs.Sort();

            for (int i = 0; i < resultLeafs.Count; i++)
            {
                for (int j = 0; j < resultLeafs.Count; j++)
                {
                    // not comparing with self
                    if (i != j)
                    {
                        if (resultLeafs[i].Intersect(resultLeafs[j]))
                        {
                            if (resultLeafs[i].GetNrOfLocations() < resultLeafs[j].GetNrOfLocations())
                            {
                                resultLeafs.Remove(resultLeafs[i--]);
                            }
                            else
                            {
                                resultLeafs.Remove(resultLeafs[j--]);
                            }
                        }
                    }
                }

            }
            return resultLeafs.Take(nrOfItems);
        }

        public void Visit(IClusterVisitor<T> visitor)
        {
            rootCluster.Visit(visitor);
            //shiftedRootCluster.Visit(visitor);
        }
    }
}
