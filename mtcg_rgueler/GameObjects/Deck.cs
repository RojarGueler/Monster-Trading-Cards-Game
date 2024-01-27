using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rgueler_mtcg.GameObjects
{
    public class Deck
    {
        public string Username { get; set; }
        public List<Card> Cards { get; set; }

        public Deck(string username)
        {
            Username = username;
            Cards = new List<Card>();
        }

    }
}
