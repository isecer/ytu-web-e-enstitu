namespace LisansUstuBasvuruSistemi.WebServiceData.ObsRestData.Models
{
    public class ObsProgramFullDto
    {
        public string FakulteKod { get; set; }
        public string FakulteAd { get; set; }

        public string BolumId { get; set; }
        public string BolumKod { get; set; }
        public string BolumAd { get; set; }

        public string ProgramId { get; set; }
        public string ProgramKod { get; set; }
        public string ProgramAd { get; set; }

        public string ProgramTur { get; set; }      
        public string ProgramTip { get; set; }      
        public string NormalSure { get; set; }
        public string AzamiSure { get; set; }
    }
}