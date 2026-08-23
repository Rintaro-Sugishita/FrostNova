namespace FrostNova.Core.ExcelWork.TemplateFramework
{
    internal class KeyCell
    {
        public string Key { get; set; } = "";

        public string? Format { get; set; }

        public string? Formula { get; set; }

        public int RelativeRow { get; set; }

        public int RelativeColumn { get; set; }
    }
}
