using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookShopMVC.Models
{
    [Table("Genre")]
    public class Genre
    {
        public int Id { get; set; }
        [Required]
        [StringLength(40)]
        public string GenreName { get; set; }
        public List<Book> Books { get; set; }
    }
}
