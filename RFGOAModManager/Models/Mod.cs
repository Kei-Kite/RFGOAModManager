using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RFGOAModManager.Models
{
    public class Mod
    {
        public string Name { get; }
        public List<string> Files { get; }

        public Mod(string name, List<string> files)
        {
            Name = name;
            Files = files;
        }

        public override string ToString() => Name;
    }
}
