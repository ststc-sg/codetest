using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SharpApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var list=new List<int>();
            list.Add(0);
            list.Add(1);
            list.Add(2);
            var refspan= CollectionsMarshal.AsSpan(list);
            IcrementInThisThread(refspan);

            foreach(var pt in list)
                Console.Write(pt);
            Console.WriteLine();
            
            IcrementInOverThread(refspan);

            foreach (var pt in list)
                Console.Write(pt);
            Console.WriteLine();

            Console.ReadKey();
        }
        /// <summary>
        /// modify span in this thread
        /// </summary>
        /// <param name="span"></param>
        static void IcrementInThisThread(Span<int> span)
        {
            for(int i=0;i< span.Length;i++)
                span[i]++;
        }
        /// <summary>
        /// modify span in pool thread
        /// </summary>
        /// <param name="span"></param>
        static void IcrementInThread(Span<int> span)
        {
            Task.Run(() => IcrementInThisThread(span)).Wait();
        }
    }
}
