using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace WeeabuCalculator
{
    public class Batch
    {
        public string Name { get; private set; }
        public PlayerInfo Player { get; private set; }
        public string Job { get; private set; }
        public string Driver { get; private set; }
        public string[] DriverArguments { get; private set; }
        
        public static IEnumerable<Batch> ReadFile(string path)
        {
            using (var r = new StreamReader(path))
            {
                var d = new Deserializer();
                return d.Deserialize<Batches>(r).Root;
            }
        }

        private class Batches
        {
            public Batch[] Root { get; set; }
        }
    }
}
