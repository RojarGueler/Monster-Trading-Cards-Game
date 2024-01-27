using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rgueler_mtcg.GameObjects
{
    public class Package
    {
        public int PackageId { get; set; }
        public bool Bought { get; set; }
        public List<Card> Cards { get; set; }

        public Package()
        {
            Cards = new List<Card>();
        }
    }
}
