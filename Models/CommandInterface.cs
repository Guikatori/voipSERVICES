namespace Models.CommandInterface
{
    public class CommandInterface
    {
        public required string ApiKey { get; set; }
        public required string Phone { get; set; }
        public int? DealId { get; set; }
        public int? ContactId { get; set; }
        public string AccountName {get; set;}
        public string AccountCnpj {get; set;}
        public string AccountEmail {get; set;}
    }
}