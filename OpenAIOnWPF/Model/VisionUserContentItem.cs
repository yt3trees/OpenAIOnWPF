namespace OpenAIOnWPF.Model
{
    /// <summary>
    /// VisionAPIのUserMessage内のContent
    /// </summary>
    public class VisionUserContentItem
    {
        public string type { get; set; }
        public string text { get; set; }
        public Image_Url image_url { get; set; }
    }
    public class Image_Url
    {
        public string url { get; set; }
        public string detail { get; set; }
    }
}
