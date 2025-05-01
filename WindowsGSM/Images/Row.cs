namespace WindowsGSM.Images
{
    class Row
    {
        public string Image { get; set; }
        public string Name { get; set; }

        public async Task<string> GetRowDetailsAsync()
        {
            return await Task.Run(() =>
            {
                return $"Row ID: {ID}, Name: {Name}, Status: {Status}";
            });
        }
    }
}