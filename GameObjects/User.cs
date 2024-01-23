using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace rgueler_mtcg.GameObjects
{
    public class User
    {
        public string Username { get; set; }
        //[JsonIgnore]
        public string Password { get; set; }
        public string Name { get; set; }
        //[JsonIgnore]
        public string Bio { get; set; }
        public string Image { get; set; }
    }
}
