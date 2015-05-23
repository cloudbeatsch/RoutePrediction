using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace RoutePredictionTest
{
    class Program
    {

        static void Main(string[] args)
        {
            RoutePredictionTest test = new RoutePredictionTest();
            test.PredictEndLocation();
            test.TrainAndValidate();
            Console.ReadKey();
        }
    }
}
