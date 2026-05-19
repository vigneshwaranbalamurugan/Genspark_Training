namespace UnderstandingEfCoreApp.Models
{
    public class Account
    {
        //[Key]
        public string AccountNumber { get; set; } = string.Empty;

        public int CustomerId { get; set; }

        public decimal Balance { get; set; }

        //[Column(TypeName = "timestamp without time zone")]
        public DateTime LastAccessed { get; set; }


        //[ForeignKey("CustomerId")]//Use the specified column and make it the foreign key for the bellow entity
        public Customer? Customer { get; set; } = null;//Navigation property

        public string Status { get; set; } = "Active";

    }
}

