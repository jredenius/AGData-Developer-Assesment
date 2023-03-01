namespace RestAPI
{
    [Serializable]
    public class CustomerData
    {
        public string Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
    }
}