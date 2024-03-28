using System;
using System.Collections.Generic;

namespace ConsoleApp2.Models
{
    public partial class InfoUser
    {
        public int Id { get; set; }
        public long Iduser { get; set; }
        public string Name { get; set; } = null!;
        public byte[] Image { get; set; } = null!;
    }
}
