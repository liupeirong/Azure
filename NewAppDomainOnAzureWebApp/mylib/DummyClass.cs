using System;
using System.IO;

namespace mylib
{
    public class DummyClass
    {
        public static void somehtml(string path, string msg)
        {
            using (StreamWriter outputFile = new StreamWriter(path + @"/somehtml.html"))
            {
                outputFile.WriteLine(@"<h1>hello " + msg + @"!</h1>");
            }
        }
    }
}
