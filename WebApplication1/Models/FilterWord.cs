using System;

namespace WebApplication1.Models
{
    public class FilterWord
    {
        public FilterWord(string name)
        {
            Id = Guid.NewGuid();
            Name = name;
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}
