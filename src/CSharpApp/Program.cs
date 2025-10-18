using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Collections;

namespace CSharpApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var CS_list = new List<double> { 1.0, -9.0, 25.0, -15.0, 10.0, -5.0, 12.0 }; 
            var FS_list = ListModule.OfSeq(CS_list); // Convert to F# list
            double result = SumFloat.sum(FS_list); // Call F# function 
            Console.WriteLine("Final result = {0:F1}", result);
        }
    }
}
