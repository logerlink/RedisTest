using System.ComponentModel.DataAnnotations;

namespace RedisTest.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        [MaxLength(20)]
        public string? Name { get; set; }
        public int Age { get; set; }
    }
}