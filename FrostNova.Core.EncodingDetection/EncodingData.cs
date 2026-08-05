namespace FrostNova.Core.EncodingDetection
{
    public class EncodingData
    {
        public int Code { get; set; }
        public string EncName { get; set; }

        public EncodingData(int c, string n)
        {
            Code = c;
            EncName = n;
        }
    }
}
