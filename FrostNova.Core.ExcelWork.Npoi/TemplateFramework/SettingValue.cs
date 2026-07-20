namespace FrostNova.Core.ExcelWork.TemplateFramework
{
    internal class SettingValue
    {
        public object? Value { get; set; }

        public string? Format { get; set; }

        public string? align { get; set; }

        public bool IsFormula { get; set; }

        public bool IsR1C1 { get; set; }

        public string? Formula
        {
            get
            {
                if (IsFormula && Value is string formula)
                {
                    return formula;
                }
                return null;
            }
        }
    }
}
