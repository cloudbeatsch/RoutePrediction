**This repo implements an algorithm which can be used to predict end locations based on a start location.**

The approach is based on the assumption that drivers follow certain patterns, such as driving from the train station to the office, or bringing kids to school each weekday morning. The idea is to identify the drivers top drop-off locations based on its current pick-up location. 
To implement the location clustering, we created a hierarchical model and compute the cluster which contains the highest number of locations. In such a model, each cluster is divided into four child clusters until we arrive at the required cluster size, the so called leaf cluster. Each leaf cluster contains its list of locations. To limit the memory footprint and improve performance, we decided not to pre-create the clusters and leafs but to create them only when a location is added to the respective cluster/leaf. One issue with a fixed hierarchical cluster is that the actual top location might be located in the intersection of leafs, and therefore won’t be identified by the model. To address this issue, we simple create a second model which is shifted by half of the leaf size, both vertically and horizontally. We then query the two models for their top locations and remove intersecting leafs by only taking the one with the highest number of locations.
 
For the end location predictions based on the start location, we perform the following steps:

1.	We load all trips for the particular driver into the model and retrieve all trips which started in the very same cluster. For improved accuracy, we can filter trips based on day or time before loading them into the model. The solution could also be extended to run filter queries as part of the top location retrieval, so basically passing a LINQ expression as an argument.
2.	We retrieve all trips which started in the same leaf. In the case no previous trip started in the current leaf, we iteratively query the parent cluster, until we find a trip or reach a cluster level where the prediction becomes irrelevant.  
3.	We now iterate over all the trips which started in the same cluster and create a list of leafs where these trips ended. We then count how many of the trips end in a particular leaf and calculate the probability of the prediction. 

Limitations: It is clear that this route prediction only works for drivers which follow a certain pattern, such as frequently driving from the train station to the office.

**Getting started**
The solution contains three projects:

1. the location clustering algorithm
2. the route prediction algorithm
3. a test demonstrating how the prediction algorithm can be used

Dependencies: The clustering algorithm uses the geographical projections which are implemented the the Geodesy library [http://github.com/juergenpf/Geodesy](http://github.com/juergenpf/Geodesy) by Jürgen Pfeifer [https://github.com/juergenpf](https://github.com/juergenpf). 
just add https://home.familiepfeifer.de/nuFeed/nuget as package source ( TOOLS -> NuGet Package Manager -> Package Manager Settings -> Package Sources )


**Implementation details**

The hierarchical clustering algorithm is based on the composite pattern:

•	`LocationClustering` represents the service façade and implements the algorithm to query the top location 

•	Each model consists of a single root cluster of type` ClusterRoot`

•	Each hierarchy level represents a different zoom level and contains one to four child clusters (which are either of type `ClusterHierarchy` or `ClusterLeaf` in the case that the minimal cluster size has been reached)

•	Each leaf contains a collection of `LocationInstance` objects (of which each contains a location object of type T)

•	The cluster hierarchy can be traversed using a visitor which implements `IClusterVisitor `

**Clustering locations to identify top locations**

Using location clustering to identify top locations is straight forward: 

1.	Create an instance of `LocationClustering<T>` by typifying the location object and specifying the size of the leaf in meters

	`var locations = new LocationClustering<string>(200);`

2.	Add locations using the AddLocation method  

	`locations.AddLocation(latitude, longitude, “<tripId>”);`

4.	Locations can be added at any time. This allows the model to learn even after the first top leafs have been queried.

3.	Once the locations have been added, just query the top clusters using the `GetSortedLeafs` method. This returns an enumerable with the requested number of top leafs (the top clusters)

        
        foreach (var leaf in locations.GetSortedLeafs(maxNrTopClusters)) {
           foreach (var location in leaf.GetLocations())
           {
              var tripId = location.LocationObject;      
           }
        }
        
**Predict end locations based on start location**

To predict end locations, we build a single cluster per driver using previous trips. We add the start locations and store the trip’s end location as the LocationObject. In the example below, we store objects of type TripSummary (which contains latitude and longitude of the end location):

	var locations = new LocationClustering<TripSummary>(200);
	locations.AddLocation(startLat, startLon, tripSummary);
	
Once all the trips have been loaded, the model can be used to predict end locations based on start latitude and longitude. In its essence, this requires the following steps:

1.	Retrieve the leaf for the start location. In the case the retrieved leave contains no trip, we iterate over the parent cluster until we have a cluster which contains at least one trip.
2.	For each trip in the cluster, we retrieve its end location leaf and count how many trips end in the same leaf. 
3.	To calculate the probability of our prediction, we divide the number of locations in a leaf by the total number of locations.


		public List<RoutePredictionItem> PredictEndLocation(double startLat, double startLon)
		{
		   var cluster = startLocations.GetLeaf(startLat, startlon);
		   int clusterLevel = 0;
		   // check how many start points we have within a cluster 
		   // and iterate through the cluster hierarchy to get at least 1 location
		   while (cluster.GetNrOfLocations() == 0){
		      var parent = cluster.GetParentItem();
		      if (parent != null){
		         if (parent is ClusterItem<LocationInstance<TripSummary>>){
		            cluster = parent as ClusterItem<LocationInstance<TripSummary>>;
		            clusterLevel++;
		         }
		      }
		   }
	
		   Dictionary<ClusterLeaf<TripSummary>, RoutePredictionItem> leafs = 
				new Dictionary<ClusterLeaf<TripSummary>, RoutePredictionItem>();
		   // create a list of endLocation leafs and count how many times an end location
		   // is part of the leaf
		   foreach (var location in cluster.GetLocations()){
		      ClusterLeaf<TripSummary> leaf = 
		         location.LocationObject.GetParentItem() as ClusterLeaf<TripSummary>;
		
		      if (!leafs.ContainsKey(leaf)){
		         leafs[leaf] = new RoutePredictionItem() { 
		            Lat = leaf.CenterLat, 
		            Lon = leaf.CenterLon, 
		            NrOfEndPoints = 0,
		            Probability = 0.0 };
		      }
		      leafs[leaf].NrOfEndPoints++;
		      leafs[leaf].Probability = leafs[leaf].NrOfEndPoints /cluster. GetNrOfLocations();
		   }
		   var predictions = leafs.Values.ToList();
		   // sort the predictions on NrOfEndPoints
		   predictions.Sort();
		   return predictions;
		}

To test the model and measure its accuracy, we split the dataset into a training and a validation set. Now we only load the training data into the model and validate our algorithm by predicting the end locations of our validation dataset. We now simply compare the predictions with the actual values and calculate the distance in-between them.



